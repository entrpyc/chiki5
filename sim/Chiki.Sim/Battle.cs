using System;
using System.Collections.Generic;
using Chiki.Sim.Effects;

namespace Chiki.Sim
{
    /// <summary>
    /// The battle aggregate (PRD 4.8): one player against one enemy (PRD 3.3.1.7) on the enemy's
    /// track and chart. It ticks in beats from the track's beat map, takes its action
    /// opportunities from the chart (PRD 3.3.1.8), grades pressed inputs (PRD 3.3.3.1), resolves
    /// every enemy action (PRD 3.3.4) and appends every state change to an ordered event stream
    /// that subscribers read.
    ///
    /// The client drives it with audio time: <see cref="Advance"/> processes everything up to a
    /// time and <see cref="Press"/> / <see cref="Send"/> stamp a slot press with the audio time of
    /// the key event. An enemy action resolves when its Judgment Window closes, with the accepted
    /// press if there was one and as no input otherwise (PRD 3.3.3.2), in a fixed order: grade,
    /// incoming damage, the action's statuses, player effect (PRD 3.3.4.1, 3.3.4.6).
    ///
    /// Every accepted press starts its slot's cooldown (PRD 3.3.5.1); a cooling slot cannot be
    /// played (PRD 3.3.5.3). A Signature send banks its card instead of playing it, and the
    /// Signature fires when the chain is full (PRD 3.3.6). Statuses on either side act in the
    /// order of <see cref="StatusPriority"/> (PRD 3.3.7.2) and tick down at beat end (PRD 3.3.7.1).
    /// </summary>
    public sealed partial class Battle
    {
        private readonly List<BattleEvent> _events = new List<BattleEvent>();
        private readonly List<JudgmentEntry> _judgmentLog = new List<JudgmentEntry>();
        private readonly List<BeatTimer> _timers = new List<BeatTimer>();
        private readonly Dictionary<Slot, BeatTimer> _cooldowns = new Dictionary<Slot, BeatTimer>();
        private readonly List<string> _signatureChain = new List<string>();
        private readonly List<ActionOpportunity> _opportunities = new List<ActionOpportunity>();
        private readonly Rng _rng;

        private int _nextBeat;
        private ActionOpportunity _pending;
        private PendingPress? _pendingPress;
        private BeatTimer? _enemyDamageReduction;
        private int _enemyDamageReductionThousandths;

        /// <summary>The run-wide stats this battle reads and writes; never a copy.</summary>
        public RunStats Stats { get; }

        public EnemyDefinition Enemy { get; }

        public Chart Chart => Enemy.Chart;

        public Track Track => Enemy.Track;

        public EncounterTier Tier => Track.Tier;

        public BeatMap BeatMap => Track.BeatMap;

        /// <summary>The enemy's HP at battle start (PRD 3.7.15; derived by P10.2, supplied by the caller until then).</summary>
        public int EnemyMaxHp { get; }

        /// <summary>The enemy's current HP; the battle is won when it reaches 0 (PRD 3.3.9.2).</summary>
        public int EnemyHp { get; private set; }

        /// <summary>The latest audio time, in milliseconds, the battle has processed.</summary>
        public int CurrentTimeMs { get; private set; }

        /// <summary>The last beat that started, or -1 before beat 0.</summary>
        public int CurrentBeat => _nextBeat - 1;

        /// <summary>The player's Block (PRD 3.3.4.2).</summary>
        public int Block { get; private set; }

        /// <summary>The enemy's Block (PRD 3.6.20, 3.6.25): absorbs damage before HP, except True DMG (PRD 3.3.4.7).</summary>
        public int EnemyBlock { get; private set; }

        /// <summary>
        /// How much less damage the enemy takes right now, in thousandths (PRD 3.6.9); 0 when no
        /// reduction runs. True DMG ignores it (PRD 3.3.4.7).
        /// </summary>
        public int EnemyDamageReductionThousandths =>
            _enemyDamageReduction != null && !_enemyDamageReduction.IsExpired ? _enemyDamageReductionThousandths : 0;

        /// <summary>Total ARD lost this battle (PRD 3.3.9.4).</summary>
        public int DamageTaken { get; private set; }

        /// <summary>Perfect Defense: zero damage taken so far (PRD 3.3.9.4); final once the battle ends.</summary>
        public bool PerfectDefense => DamageTaken == 0;

        /// <summary>Won or Died once the battle has ended; null while it runs.</summary>
        public BattleOutcome? Outcome { get; private set; }

        /// <summary>Card ids banked in the Signature Chain (PRD 3.3.6.1).</summary>
        public IReadOnlyList<string> SignatureChain => _signatureChain;

        /// <summary>The statuses on the player (PRD 3.3.7.1, 4.8).</summary>
        public StatusSet PlayerStatuses { get; } = new StatusSet();

        /// <summary>The statuses on the enemy (PRD 3.3.7.1, 4.8).</summary>
        public StatusSet EnemyStatuses { get; } = new StatusSet();

        /// <summary>One entry per resolved enemy action (PRD 4.8).</summary>
        public IReadOnlyList<JudgmentEntry> JudgmentLog => _judgmentLog;

        /// <summary>The ordered event stream (PRD 4.8).</summary>
        public IReadOnlyList<BattleEvent> Events => _events;

        /// <summary>
        /// The action opportunities of one lap of the chart, in order: exactly the chart's actions
        /// with their Judgment Windows (PRD 3.3.1.8). Later laps repeat them (PRD 3.6.32).
        /// </summary>
        public IReadOnlyList<ActionOpportunity> Opportunities => _opportunities;

        /// <summary>The next enemy action still to resolve, with its window.</summary>
        public ActionOpportunity PendingAction => _pending;

        public Battle(RunStats stats, EnemyDefinition enemy, int enemyHp, Rng rng)
            : this(stats, new[] { enemy }, enemyHp, rng)
        {
        }

        /// <summary>
        /// Constructs a battle; exactly one enemy is required (PRD 3.3.1.7). <paramref name="enemyHp"/>
        /// is the enemy's starting HP (PRD 3.7.15); P10.2 derives it at battle start.
        /// <paramref name="rng"/> is the battle's seeded generator (PRD 6.8), drawn from only by
        /// rules that roll: Scar (PRD 3.3.7.3).
        /// </summary>
        public Battle(RunStats stats, IReadOnlyList<EnemyDefinition> enemies, int enemyHp, Rng rng)
        {
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            if (enemies is null)
            {
                throw new ArgumentNullException(nameof(enemies));
            }

            if (enemies.Count != 1)
            {
                throw new ArgumentException("Every encounter is one player against one enemy.", nameof(enemies));
            }

            if (enemyHp <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(enemyHp), "Enemy HP must be positive.");
            }

            Enemy = enemies[0] ?? throw new ArgumentException("The enemy must not be null.", nameof(enemies));
            if (Enemy.Chart.Actions.Count == 0)
            {
                throw new ArgumentException("The enemy's chart has no actions.", nameof(enemies));
            }

            EnemyMaxHp = enemyHp;
            EnemyHp = enemyHp;

            for (int i = 0; i < Chart.Actions.Count; i++)
            {
                _opportunities.Add(OpportunityAt(i));
            }

            _pending = _opportunities[0];
            Process(0);
        }

        /// <summary>Remaining cooldown of a slot in beats; 0 when it can be played (PRD 3.3.5.1).</summary>
        public int CooldownOf(Slot slot)
        {
            if (slot is null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            return _cooldowns.TryGetValue(slot, out var timer) ? timer.RemainingBeats : 0;
        }

        /// <summary>The statuses on one side.</summary>
        public StatusSet StatusesOn(StatusTarget target)
        {
            return target == StatusTarget.Player ? PlayerStatuses : EnemyStatuses;
        }

        /// <summary>
        /// The opportunity for the action with the given index counted from the start of the
        /// battle; indices past the chart's length wrap into the next lap (PRD 3.6.32).
        /// </summary>
        public ActionOpportunity OpportunityAt(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index must not be negative.");
            }

            int count = Chart.Actions.Count;
            int lap = index / count;
            var action = Chart.Actions[index % count];
            int positionQb = checked(action.PositionQb + lap * Chart.LengthQb);
            int bpm = BeatMap.BpmAt(positionQb);
            int centre = BeatMap.TimeAtQb(positionQb);
            int halfWidth = JudgmentWindow.AcceptHalfWidthMs(bpm);
            int open = centre - halfWidth;
            int close = centre + halfWidth;

            // Neighbouring actions never share a window: split at the midpoint, the earlier action
            // owning the midpoint itself.
            if (index > 0)
            {
                int previousCentre = CentreOf(index - 1);
                open = Math.Max(open, FloorMidpoint(previousCentre, centre) + 1);
            }

            int nextCentre = CentreOf(index + 1);
            close = Math.Min(close, FloorMidpoint(centre, nextCentre));

            return new ActionOpportunity(index, action, positionQb, bpm, centre, open, close);
        }

        /// <summary>
        /// Processes beats and window closures up to the given audio time. A no-op once the
        /// battle has ended (PRD 3.3.9.2, 3.3.9.3).
        /// </summary>
        public void Advance(int audioTimeMs)
        {
            if (Outcome != null || audioTimeMs <= CurrentTimeMs)
            {
                return;
            }

            Process(audioTimeMs);
        }

        /// <summary>Advances to the audio time of a quarter-beat position.</summary>
        public void AdvanceToPosition(int positionQb)
        {
            Advance(BeatMap.TimeAtQb(positionQb));
        }

        /// <summary>Advances to the start of a whole beat.</summary>
        public void AdvanceToBeat(int beat)
        {
            Advance(BeatMap.TimeAtBeat(beat));
        }

        /// <summary>
        /// A slot press that plays the slot's card, stamped with the audio time of the key event
        /// (PRD 3.3.3.1). The press is graded against the enemy action whose Judgment Window
        /// contains the time; the first press for an action is accepted and any later one
        /// rejected, as is a press when no window is open (PRD 3.3.1.3), on a slot still on
        /// cooldown (PRD 3.3.5.3) or while the player is Stunned (PRD 3.3.3.3). A rejected press
        /// consumes nothing and records nothing. An accepted press starts the slot's cooldown at
        /// once, whatever its grade (PRD 3.3.5.1). The card's effect resolves when the window
        /// closes, by the efficacy matrix (PRD 3.3.4.4). Until the Loadout exists (P16) the
        /// caller supplies the card the slot holds; it must belong to the slot's Category (PRD 3.4.1).
        /// </summary>
        public PressResult Press(Slot slot, CardDefinition card, int audioTimeMs)
        {
            return Accept(slot, card, false, audioTimeMs);
        }

        /// <summary>
        /// Space plus a slot key (PRD 3.3.2.3): the press is accepted and graded exactly like
        /// <see cref="Press"/>, but the card is banked into the Signature Chain instead of
        /// resolving its effect (PRD 3.3.6.1); incoming damage on the beat follows the grade as
        /// normal, a Missed send still banks (PRD 3.3.6.3) and a send with every chain slot
        /// taken is rejected like a disabled press.
        /// </summary>
        public PressResult Send(Slot slot, CardDefinition card, int audioTimeMs)
        {
            return Accept(slot, card, true, audioTimeMs);
        }

        /// <summary>
        /// Starts a duration of the given number of beats (PRD 3.3.1.4). It ticks down by one on
        /// every beat the battle starts from now on.
        /// </summary>
        public BeatTimer StartTimer(int beats)
        {
            var timer = new BeatTimer(beats);
            if (!timer.IsExpired)
            {
                _timers.Add(timer);
            }

            return timer;
        }

        /// <summary>
        /// Lands a status on one side at the current time (PRD 3.3.7.1); a status lands
        /// regardless of judgment unless the side is immune (PRD 3.3.4.6). <paramref name="value"/>
        /// is the source's X: Weak's percentage in thousandths (PRD 3.3.7.4), Thorns' damage
        /// (PRD 3.3.7.7), and 0 for the rest. Until the effect framework (P8.6) attaches status
        /// effects to cards and enemy abilities, this is how a status lands. A no-op once the
        /// battle has ended.
        /// </summary>
        public void ApplyStatus(StatusTarget target, StatusKind kind, int stacks = 1, int value = 0)
        {
            var application = new StatusApplication(kind, stacks, value);
            if (Outcome != null)
            {
                return;
            }

            Land(target, application, PositionAt(CurrentTimeMs));
        }

        /// <summary>
        /// Makes one side immune to a status for the rest of the battle: applications of it are
        /// blocked (PRD 3.3.4.6). The hook a card effect flagged as blocking a status uses until
        /// the effect framework (P8.1) drives it. A no-op once the battle has ended.
        /// </summary>
        public void GrantImmunity(StatusTarget target, StatusKind kind)
        {
            if (Outcome != null)
            {
                return;
            }

            if (StatusesOn(target).AddImmunity(kind))
            {
                Emit(new ImmunityGranted(PositionAt(CurrentTimeMs), kind, target));
            }
        }

        /// <summary>
        /// Gives one side Block at the current time: the player's outside a Defense card
        /// (PRD 3.4.8 Block-on-attack, P8.1) or the enemy's (PRD 3.6.20, 3.6.25). A no-op once
        /// the battle has ended.
        /// </summary>
        public void GrantBlock(StatusTarget target, int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Block must not be negative.");
            }

            if (Outcome != null)
            {
                return;
            }

            AddBlock(target, amount, PositionAt(CurrentTimeMs));
        }

        /// <summary>
        /// The enemy takes <paramref name="thousandths"/> less damage for <paramref name="beats"/>
        /// beats from now, Iron Veil's shape (PRD 3.6.9); a new reduction replaces the running one.
        /// True DMG ignores it (PRD 3.3.4.7). A no-op once the battle has ended.
        /// </summary>
        public void ReduceEnemyDamageTaken(int thousandths, int beats)
        {
            if (thousandths < 1 || thousandths > Fixed.One)
            {
                throw new ArgumentOutOfRangeException(nameof(thousandths), "A reduction must be 1–1000 thousandths.");
            }

            if (beats < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(beats), "A reduction must last at least one beat.");
            }

            if (Outcome != null)
            {
                return;
            }

            _enemyDamageReduction = StartTimer(beats);
            _enemyDamageReductionThousandths = thousandths;
            Emit(new DamageReductionStarted(PositionAt(CurrentTimeMs), thousandths, beats));
        }

        /// <summary>
        /// True DMG (PRD 3.3.4.7): comes straight off the enemy's HP or the player's ARD, past
        /// Block, Weak, damage reductions and every other multiplier; it still ends the battle at
        /// 0 (PRD 3.3.9.2, 3.3.9.3) and counts toward damage taken (PRD 3.3.9.4). A no-op once
        /// the battle has ended.
        /// </summary>
        public void DealTrueDamage(StatusTarget target, int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "True DMG must not be negative.");
            }

            if (Outcome != null)
            {
                return;
            }

            int positionQb = PositionAt(CurrentTimeMs);
            TrueDamage(target, amount, positionQb);
            SettleOutcome(positionQb);
        }

        private void TrueDamage(StatusTarget target, int amount, int positionQb)
        {
            int taken = target == StatusTarget.Enemy ? TakeEnemyHp(amount) : TakeArd(amount);
            Emit(new TrueDamageDealt(positionQb, target, taken));
        }

        /// <summary>Ends the battle if a side has just reached 0; a kill takes precedence over a death on the same beat.</summary>
        private void SettleOutcome(int positionQb)
        {
            if (Outcome != null)
            {
                return;
            }

            if (EnemyHp == 0)
            {
                End(BattleOutcome.Won, positionQb);
            }
            else if (Stats.Ard == 0)
            {
                End(BattleOutcome.Died, positionQb);
            }
        }

        private void Land(StatusTarget target, StatusApplication application, int positionQb)
        {
            var set = StatusesOn(target);
            if (set.IsImmune(application.Kind))
            {
                Emit(new StatusBlocked(positionQb, application.Kind, target));
                return;
            }

            var instance = set.Apply(application.Kind, application.Stacks, application.Value);
            Emit(new StatusApplied(positionQb, application.Kind, target, application.Stacks, application.Value, set.Stacks(application.Kind), instance.RemainingBeats));
        }

        private void AddBlock(StatusTarget target, int amount, int positionQb)
        {
            if (target == StatusTarget.Player)
            {
                Block += amount;
                Emit(new BlockGained(positionQb, target, amount, Block));
            }
            else
            {
                EnemyBlock += amount;
                Emit(new BlockGained(positionQb, target, amount, EnemyBlock));
            }
        }

        private PressResult Accept(Slot slot, CardDefinition card, bool signatureSend, int audioTimeMs)
        {
            if (slot is null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            if (card is null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (!card.Category.Allows(slot.Key))
            {
                throw new ArgumentException($"A {card.Category} card cannot sit in slot {slot.Key}.", nameof(card));
            }

            if (Outcome != null)
            {
                return PressResult.NoWindow;
            }

            Advance(audioTimeMs);
            if (Outcome != null)
            {
                return PressResult.NoWindow;
            }

            // A Stunned player's presses are ignored (PRD 3.3.3.3).
            if (PlayerStatuses.Has(StatusKind.Stun))
            {
                Emit(new SlotDisabled(PositionAt(audioTimeMs), slot, SlotDisabledReason.PlayerStunned, 0));
                return PressResult.Stunned;
            }

            // A cooling slot gives disabled feedback whether or not a window is open (PRD 3.3.5.3).
            int remaining = CooldownOf(slot);
            if (remaining > 0)
            {
                Emit(new SlotDisabled(PositionAt(audioTimeMs), slot, SlotDisabledReason.Cooldown, remaining));
                return PressResult.OnCooldown(remaining);
            }

            if (signatureSend && _signatureChain.Count >= Tuning.SignatureChainSlots)
            {
                Emit(new SlotDisabled(PositionAt(audioTimeMs), slot, SlotDisabledReason.SignatureChainFull, 0));
                return PressResult.ChainFull;
            }

            if (!_pending.Contains(audioTimeMs))
            {
                return PressResult.NoWindow;
            }

            if (_pendingPress != null)
            {
                return PressResult.AlreadyAnswered(_pending.Index);
            }

            int offset = audioTimeMs - _pending.CentreMs;
            var grade = JudgmentWindow.Grade(offset, _pending.Bpm);
            _pendingPress = new PendingPress(slot, card, grade, signatureSend);
            Emit(new InputJudged(_pending.PositionQb, _pending.Index, slot, card.Id, grade, offset, signatureSend));
            StartCooldown(slot, card, _pending.PositionQb);
            return new PressResult(PressOutcome.Accepted, _pending.Index, grade, 0);
        }

        /// <summary>Any accepted press starts its slot's cooldown, regardless of grade (PRD 3.3.5.1).</summary>
        private void StartCooldown(Slot slot, CardDefinition card, int positionQb)
        {
            _cooldowns[slot] = StartTimer(card.CooldownBeats);
            Emit(new CooldownStarted(positionQb, slot, card.Id, card.CooldownBeats));
        }

        /// <summary>
        /// The position an event at an arbitrary time belongs to: the pending action's while its
        /// window is open, otherwise the quarter beat the time falls in, so the stream stays in order.
        /// </summary>
        private int PositionAt(int audioTimeMs)
        {
            if (_pending.Contains(audioTimeMs))
            {
                return _pending.PositionQb;
            }

            int position = Beats.ToQuarterBeats(Math.Max(CurrentBeat, 0));
            while (BeatMap.TimeAtQb(position + 1) <= audioTimeMs)
            {
                position++;
            }

            return position;
        }

        private void Process(int audioTimeMs)
        {
            while (Outcome is null)
            {
                int beatTime = BeatMap.TimeAtBeat(_nextBeat);
                int beatPosition = Beats.ToQuarterBeats(_nextBeat);
                bool beatDue = beatTime <= audioTimeMs;
                bool closeDue = _pending.CloseMs <= audioTimeMs;
                if (!beatDue && !closeDue)
                {
                    break;
                }

                bool resolveFirst = closeDue
                    && (!beatDue
                        || _pending.CloseMs < beatTime
                        || (_pending.CloseMs == beatTime && _pending.PositionQb <= beatPosition));
                if (resolveFirst)
                {
                    ResolvePending();
                }
                else
                {
                    StartBeat();
                }
            }

            if (audioTimeMs > CurrentTimeMs)
            {
                CurrentTimeMs = audioTimeMs;
            }
        }

        private void StartBeat()
        {
            int positionQb = Beats.ToQuarterBeats(_nextBeat);
            if (_nextBeat > 0)
            {
                EndBeat(positionQb);
                if (Outcome != null)
                {
                    return;
                }
            }

            Emit(new BeatStarted(positionQb, _nextBeat));
            _nextBeat++;

            for (int i = _timers.Count - 1; i >= 0; i--)
            {
                _timers[i].Tick();
                if (_timers[i].IsExpired)
                {
                    _timers.RemoveAt(i);
                }
            }

            if (_enemyDamageReduction != null && _enemyDamageReduction.IsExpired)
            {
                _enemyDamageReduction = null;
                Emit(new DamageReductionEnded(positionQb));
            }

            PruneExpiredModifiers(positionQb);
        }

        /// <summary>
        /// The end of the beat that is about to give way to the next: damage over time acts
        /// (PRD 3.3.7.2), then every timed status ticks down one beat (PRD 3.3.7.1).
        /// </summary>
        private void EndBeat(int positionQb)
        {
            RunStatusPhases(StatusMoment.BeatEnd, new BeatContext(positionQb, null, null));
            SettleOutcome(positionQb);
            if (Outcome != null)
            {
                return;
            }

            TickStatuses(StatusTarget.Player, positionQb);
            TickStatuses(StatusTarget.Enemy, positionQb);
        }

        private void TickStatuses(StatusTarget target, int positionQb)
        {
            var set = StatusesOn(target);
            foreach (var expired in set.Tick())
            {
                Emit(new StatusRemoved(positionQb, expired.Kind, target, expired.Stacks, set.Stacks(expired.Kind)));
            }
        }

        /// <summary>
        /// Resolves the pending enemy action in the fixed order of PRD 3.3.4.1: the grade was
        /// fixed at the press, then incoming damage (PRD 3.3.4.2) against the Block held as the
        /// action arrives, then the statuses the action applies, whatever the grade (PRD 3.3.4.6),
        /// then the player's effect (PRD 3.3.4.3, 3.3.4.4). Block gained on this action is
        /// therefore available from the next enemy action on (PRD 3.3.4.8). Statuses that act as
        /// the action arrives go first (PRD 3.3.7.2): Stun turns the player's press into no input
        /// (PRD 3.3.3.3) or skips the enemy's action (PRD 3.3.7.5), in which case neither its
        /// damage nor its statuses land; Weak sets the incoming multiplier (PRD 3.3.7.4).
        /// </summary>
        private void ResolvePending()
        {
            var opportunity = _pending;
            var context = new BeatContext(opportunity.PositionQb, opportunity, _pendingPress);
            _pendingPress = null;
            _resolving = context;
            try
            {
                RunStatusPhases(StatusMoment.ActionArrives, context);
                var press = context.Press;

                _judgmentLog.Add(new JudgmentEntry(
                    opportunity.Index,
                    opportunity.PositionQb,
                    opportunity.Action.Kind,
                    press?.Grade,
                    press?.Slot,
                    press?.Card.Id));

                if (!context.EnemySkipped)
                {
                    if (opportunity.Action.IsAttack)
                    {
                        ResolveIncoming(context);
                        if (Outcome != null)
                        {
                            return;
                        }
                    }

                    foreach (var application in opportunity.Action.Applies)
                    {
                        Land(StatusTarget.Player, application, opportunity.PositionQb);
                    }
                }

                if (press != null)
                {
                    ResolvePlayerEffect(opportunity, press);
                }

                SettleOutcome(opportunity.PositionQb);
                if (Outcome != null)
                {
                    return;
                }

                _pending = OpportunityAt(opportunity.Index + 1);
            }
            finally
            {
                _resolving = null;
            }
        }

        /// <summary>Walks the priority table (PRD 3.3.7.2) and lets each status that acts at this moment act.</summary>
        private void RunStatusPhases(StatusMoment moment, BeatContext context)
        {
            foreach (var kind in StatusPriority.SameBeatOrder)
            {
                if (StatusPriority.MomentOf(kind) != moment || Outcome != null)
                {
                    continue;
                }

                switch (kind)
                {
                    case StatusKind.Stun:
                        ResolveStun(context);
                        break;
                    case StatusKind.Weak:
                        ResolveWeak(context);
                        break;
                    case StatusKind.Thorns:
                        ResolveThorns(context);
                        break;
                    case StatusKind.Bleed:
                        ResolveBleed(context);
                        break;
                    default:
                        throw new InvalidOperationException($"{kind} is not a same-beat status.");
                }
            }
        }

        /// <summary>
        /// Stun as an enemy action arrives: a Stunned player's action is no input (PRD 3.3.3.3) and
        /// a Stunned enemy's action does not resolve (PRD 3.3.7.5); either way the Stun is consumed.
        /// </summary>
        private void ResolveStun(BeatContext context)
        {
            if (ConsumeStun(StatusTarget.Player, context.PositionQb))
            {
                context.Press = null;
            }

            if (ConsumeStun(StatusTarget.Enemy, context.PositionQb))
            {
                context.EnemySkipped = true;
            }
        }

        private bool ConsumeStun(StatusTarget target, int positionQb)
        {
            if (!StatusesOn(target).ConsumeOne(StatusKind.Stun, out _, out int left))
            {
                return false;
            }

            Emit(new StatusTriggered(positionQb, StatusKind.Stun, target, 0, 0));
            Emit(new StatusRemoved(positionQb, StatusKind.Stun, target, 1, left));
            return true;
        }

        /// <summary>Weak on the enemy reduces the damage of the attack about to land (PRD 3.3.7.4).</summary>
        private void ResolveWeak(BeatContext context)
        {
            if (context.Opportunity is null || !context.Opportunity.Action.IsAttack || context.EnemySkipped)
            {
                return;
            }

            if (EnemyStatuses.WeakThousandths == 0)
            {
                return;
            }

            context.IncomingMultThousandths = EnemyStatuses.DealtMultiplierThousandths;
            Emit(new StatusTriggered(context.PositionQb, StatusKind.Weak, StatusTarget.Enemy, context.IncomingMultThousandths, 0));
        }

        /// <summary>
        /// Thorns on the player: the enemy attack that just landed takes one stack's damage
        /// (PRD 3.3.7.7). Only Thorns held as the attack arrived answers it; a stack an effect
        /// lands in reaction to this very hit waits for the next attack.
        /// </summary>
        private void ResolveThorns(BeatContext context)
        {
            if (!context.ThornsAtArrival || !PlayerStatuses.ConsumeOne(StatusKind.Thorns, out int damage, out int left))
            {
                return;
            }

            int dealt = DamageEnemy(damage, out int absorbed);
            Emit(new StatusTriggered(context.PositionQb, StatusKind.Thorns, StatusTarget.Player, dealt, absorbed));
            Emit(new StatusRemoved(context.PositionQb, StatusKind.Thorns, StatusTarget.Player, 1, left));
            if (EnemyHp == 0)
            {
                End(BattleOutcome.Won, context.PositionQb);
            }
        }

        /// <summary>
        /// Bleed at beat end: one damage per stack (PRD 3.3.7.6), the enemy's first. On the player
        /// it bypasses nothing, so Block absorbs first, and timing mitigation does not apply.
        /// </summary>
        private void ResolveBleed(BeatContext context)
        {
            int enemyStacks = EnemyStatuses.Stacks(StatusKind.Bleed);
            if (enemyStacks > 0)
            {
                int dealt = DamageEnemy(enemyStacks * Tuning.BleedDamagePerStack, out int absorbed);
                Emit(new StatusTriggered(context.PositionQb, StatusKind.Bleed, StatusTarget.Enemy, dealt, absorbed));
                if (EnemyHp == 0)
                {
                    End(BattleOutcome.Won, context.PositionQb);
                    return;
                }
            }

            int playerStacks = PlayerStatuses.Stacks(StatusKind.Bleed);
            if (playerStacks > 0)
            {
                int damage = playerStacks * Tuning.BleedDamagePerStack;
                int absorbed = Math.Min(Block, damage);
                int ardLoss = TakeArd(damage - absorbed);
                Block -= absorbed;
                Emit(new StatusTriggered(context.PositionQb, StatusKind.Bleed, StatusTarget.Player, ardLoss, absorbed));
                if (Stats.Ard == 0)
                {
                    End(BattleOutcome.Died, context.PositionQb);
                }
            }
        }

        private void ResolveIncoming(BeatContext context)
        {
            var opportunity = context.Opportunity!;
            int statusMult = Fixed.Mul(context.IncomingMultThousandths, _effects.MultiplierFor(EffectValue.DamageTaken));
            var incoming = Resolution.Incoming(Enemy.DamagePerHit, context.Press?.Grade, Block, statusMult);
            int ardLoss = TakeArd(incoming.ArdLoss);

            Block -= incoming.BlockAbsorbed;
            context.ThornsAtArrival = PlayerStatuses.Has(StatusKind.Thorns);
            Emit(new DamageTaken(opportunity.PositionQb, opportunity.Index, ardLoss, incoming.BlockAbsorbed));

            if (Stats.Ard == 0)
            {
                End(BattleOutcome.Died, opportunity.PositionQb);
                return;
            }

            RunStatusPhases(StatusMoment.AttackLanded, context);
        }

        /// <summary>
        /// The player's card resolves (PRD 3.3.4.3, 3.3.4.4): a send banks it; otherwise its
        /// value in play (P8.4, P8.5, P8.7) drives its Category's own effect, then its on-play
        /// effects fire at the play's JudgmentMult (P8.6, P8.7). The outcome is settled by the
        /// caller once everything the play does has landed, so a kill still lets the card's
        /// "if this kills" effects act.
        /// </summary>
        private void ResolvePlayerEffect(ActionOpportunity opportunity, PendingPress press)
        {
            var card = press.Card;
            if (press.SignatureSend)
            {
                _signatureChain.Add(card.Id);
                Emit(new CardBanked(opportunity.PositionQb, press.Slot, card.Id));
                if (_signatureChain.Count >= Tuning.SignatureChainSlots)
                {
                    FireSignature(opportunity);
                }

                return;
            }

            int value = CardValueInPlay(card);
            switch (card.Category)
            {
                case CardCategory.Defense:
                    AddBlock(StatusTarget.Player, Resolution.PlayerEffect(value, press.Grade, 0), opportunity.PositionQb);
                    break;

                case CardCategory.LeftAttack:
                case CardCategory.RightAttack:
                {
                    int efficacy = Resolution.AttackEfficacy(opportunity.Action, card.Category);
                    int weakMult = PlayerStatuses.DealtMultiplierThousandths;
                    int statusMult = Fixed.Mul(weakMult, _effects.MultiplierFor(EffectValue.DamageDealt));
                    int damage = Resolution.PlayerEffect(value, press.Grade, Stats.BaseDmg, efficacy, statusMult);
                    if (PlayerStatuses.WeakThousandths > 0)
                    {
                        Emit(new StatusTriggered(opportunity.PositionQb, StatusKind.Weak, StatusTarget.Player, weakMult, 0));
                    }

                    if (press.Grade == Judgment.Perfect && damage > 0)
                    {
                        damage = RollScar(opportunity, damage);
                    }

                    DealDamage(opportunity, damage);
                    break;
                }

                case CardCategory.Ability:
                    // An Ability has no effect of its own beyond its on-play effects, which
                    // resolve at the scale of its grade: fully on a Perfect (PRD 3.3.4.8).
                    Emit(new AbilityResolved(opportunity.PositionQb, opportunity.Index, press.Slot, card.Id, Resolution.JudgmentMultiplier(press.Grade)));
                    break;

                default:
                    throw new InvalidOperationException($"Unknown card category {card.Category}.");
            }

            ResolveOnPlayEffects(card, press, opportunity);
        }

        /// <summary>
        /// Scar on the enemy (PRD 3.3.7.3): each stack adds 2% to the chance that this Perfect
        /// hit deals double damage. One roll on the battle's generator per Perfect hit that lands
        /// while the enemy carries Scar.
        /// </summary>
        private int RollScar(ActionOpportunity opportunity, int damage)
        {
            int stacks = EnemyStatuses.Stacks(StatusKind.Scar);
            if (stacks == 0)
            {
                return damage;
            }

            int chance = Math.Min(Fixed.One, stacks * Tuning.ScarChancePerStackThousandths);
            if (_rng.NextInt(0, Fixed.One) >= chance)
            {
                return damage;
            }

            int doubled = Fixed.Mul(damage, Tuning.ScarHitMultiplierThousandths);
            Emit(new StatusTriggered(opportunity.PositionQb, StatusKind.Scar, StatusTarget.Enemy, doubled, 0));
            return doubled;
        }

        /// <summary>
        /// The third banked card fires the Signature in the same beat (PRD 3.3.6.2): the chain
        /// empties and the enemy takes the flat Signature damage, untouched by grade, Base DMG
        /// or the efficacy matrix.
        /// </summary>
        private void FireSignature(ActionOpportunity opportunity)
        {
            var cards = _signatureChain.ToArray();
            _signatureChain.Clear();
            Emit(new SignatureFired(opportunity.PositionQb, opportunity.Index, cards, Tuning.SignatureDamage));
            DealDamage(opportunity, Tuning.SignatureDamage);
        }

        /// <summary>Damages the enemy for an action, never below 0; the caller settles the win at 0 (PRD 3.3.9.2).</summary>
        private void DealDamage(ActionOpportunity opportunity, int damage)
        {
            int dealt = DamageEnemy(damage, out int absorbed);
            Emit(new DamageDealt(opportunity.PositionQb, opportunity.Index, dealt, absorbed));
        }

        /// <summary>
        /// Damage to the enemy that is not True DMG: its running reduction takes its share first
        /// (PRD 3.6.9), then its Block absorbs (PRD 3.6.25), and the remainder comes off HP. The
        /// reduction is applied to the whole-number damage of the source, so a card's effect is
        /// still rounded once by its own formula (PRD 3.3.4.5).
        /// </summary>
        private int DamageEnemy(int amount, out int blockAbsorbed)
        {
            int reduced = Fixed.Mul(amount, Fixed.One - EnemyDamageReductionThousandths);
            blockAbsorbed = Math.Min(EnemyBlock, reduced);
            EnemyBlock -= blockAbsorbed;
            return TakeEnemyHp(reduced - blockAbsorbed);
        }

        private int TakeEnemyHp(int amount)
        {
            int taken = Math.Min(amount, EnemyHp);
            EnemyHp -= taken;
            return taken;
        }

        private int TakeArd(int amount)
        {
            int taken = Math.Min(amount, Stats.Ard);
            Stats.Ard -= taken;
            DamageTaken += taken;
            return taken;
        }

        /// <summary>
        /// Ends the battle: Block and every status on both sides are cleared so nothing carries
        /// to the next battle (PRD 3.3.9.5), then the closing event records Perfect Defense
        /// (PRD 3.3.9.4).
        /// </summary>
        private void End(BattleOutcome outcome, int positionQb)
        {
            Outcome = outcome;
            if (Block > 0)
            {
                Emit(new BlockCleared(positionQb, StatusTarget.Player, Block));
                Block = 0;
            }

            if (EnemyBlock > 0)
            {
                Emit(new BlockCleared(positionQb, StatusTarget.Enemy, EnemyBlock));
                EnemyBlock = 0;
            }

            ClearStatuses(StatusTarget.Player, positionQb);
            ClearStatuses(StatusTarget.Enemy, positionQb);
            Emit(new BattleEnded(positionQb, outcome, DamageTaken, PerfectDefense));
        }

        private void ClearStatuses(StatusTarget target, int positionQb)
        {
            var set = StatusesOn(target);
            foreach (var removed in set.Clear())
            {
                Emit(new StatusRemoved(positionQb, removed.Kind, target, removed.Stacks, set.Stacks(removed.Kind)));
            }
        }

        /// <summary>Appends an event to the stream and fires the registered effects it triggers (P8.1).</summary>
        private void Emit(BattleEvent battleEvent)
        {
            _events.Add(battleEvent);
            Dispatch(battleEvent);
        }

        private int CentreOf(int index)
        {
            int count = Chart.Actions.Count;
            int lap = index / count;
            var action = Chart.Actions[index % count];
            return BeatMap.TimeAtQb(checked(action.PositionQb + lap * Chart.LengthQb));
        }

        private static int FloorMidpoint(int a, int b)
        {
            long sum = (long)a + b;
            long half = sum / 2;
            if (sum < 0 && sum % 2 != 0)
            {
                half--;
            }

            return (int)half;
        }

        private sealed class PendingPress
        {
            public Slot Slot { get; }
            public CardDefinition Card { get; }
            public Judgment Grade { get; }
            public bool SignatureSend { get; }

            public PendingPress(Slot slot, CardDefinition card, Judgment grade, bool signatureSend)
            {
                Slot = slot;
                Card = card;
                Grade = grade;
                SignatureSend = signatureSend;
            }
        }

        /// <summary>What the status phases of one moment read and change: the action being resolved, if any, and its press.</summary>
        private sealed class BeatContext
        {
            public int PositionQb { get; }
            public ActionOpportunity? Opportunity { get; }
            public PendingPress? Press { get; set; }
            public bool EnemySkipped { get; set; }
            public int IncomingMultThousandths { get; set; } = Fixed.One;

            /// <summary>Whether the player held Thorns as the enemy's attack arrived (PRD 3.3.7.7).</summary>
            public bool ThornsAtArrival { get; set; }

            public BeatContext(int positionQb, ActionOpportunity? opportunity, PendingPress? press)
            {
                PositionQb = positionQb;
                Opportunity = opportunity;
                Press = press;
            }
        }
    }
}
