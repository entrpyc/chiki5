using System;
using System.Collections.Generic;

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
    /// incoming damage, player effect (PRD 3.3.4.1).
    /// </summary>
    public sealed class Battle
    {
        private readonly List<BattleEvent> _events = new List<BattleEvent>();
        private readonly List<JudgmentEntry> _judgmentLog = new List<JudgmentEntry>();
        private readonly List<BeatTimer> _timers = new List<BeatTimer>();
        private readonly List<string> _signatureChain = new List<string>();
        private readonly List<ActionOpportunity> _opportunities = new List<ActionOpportunity>();

        private int _nextBeat;
        private ActionOpportunity _pending;
        private PendingPress? _pendingPress;

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

        /// <summary>Total ARD lost this battle (PRD 3.3.9.4).</summary>
        public int DamageTaken { get; private set; }

        /// <summary>Perfect Defense: zero damage taken so far (PRD 3.3.9.4); final once the battle ends.</summary>
        public bool PerfectDefense => DamageTaken == 0;

        /// <summary>Won or Died once the battle has ended; null while it runs.</summary>
        public BattleOutcome? Outcome { get; private set; }

        /// <summary>Card ids banked in the Signature Chain (PRD 3.3.6.1).</summary>
        public IReadOnlyList<string> SignatureChain => _signatureChain;

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

        public Battle(RunStats stats, EnemyDefinition enemy, int enemyHp)
            : this(stats, new[] { enemy }, enemyHp)
        {
        }

        /// <summary>
        /// Constructs a battle; exactly one enemy is required (PRD 3.3.1.7). <paramref name="enemyHp"/>
        /// is the enemy's starting HP (PRD 3.7.15); P10.2 derives it at battle start.
        /// </summary>
        public Battle(RunStats stats, IReadOnlyList<EnemyDefinition> enemies, int enemyHp)
        {
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
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
        /// rejected, as is a press when no window is open (PRD 3.3.1.3). A rejected press
        /// consumes nothing and records nothing. The card's effect resolves when the window
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
        /// resolving its effect (PRD 3.3.6.1); incoming damage on the beat follows the grade as normal.
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
            if (Outcome != null || !_pending.Contains(audioTimeMs))
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
            return new PressResult(PressOutcome.Accepted, _pending.Index, grade);
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
            Emit(new BeatStarted(Beats.ToQuarterBeats(_nextBeat), _nextBeat));
            _nextBeat++;

            for (int i = _timers.Count - 1; i >= 0; i--)
            {
                _timers[i].Tick();
                if (_timers[i].IsExpired)
                {
                    _timers.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Resolves the pending enemy action in the fixed order of PRD 3.3.4.1: the grade was
        /// fixed at the press, then incoming damage (PRD 3.3.4.2), then the player's effect
        /// (PRD 3.3.4.3, 3.3.4.4). Block gained on this action is therefore available from the
        /// next enemy action on.
        /// </summary>
        private void ResolvePending()
        {
            var opportunity = _pending;
            var press = _pendingPress;
            _pendingPress = null;

            _judgmentLog.Add(new JudgmentEntry(
                opportunity.Index,
                opportunity.PositionQb,
                opportunity.Action.Kind,
                press?.Grade,
                press?.Slot,
                press?.Card.Id));

            if (opportunity.Action.IsAttack)
            {
                ResolveIncoming(opportunity, press?.Grade);
                if (Outcome != null)
                {
                    return;
                }
            }

            if (press != null)
            {
                ResolvePlayerEffect(opportunity, press);
                if (Outcome != null)
                {
                    return;
                }
            }

            _pending = OpportunityAt(opportunity.Index + 1);
        }

        private void ResolveIncoming(ActionOpportunity opportunity, Judgment? grade)
        {
            var incoming = Resolution.Incoming(Enemy.DamagePerHit, grade, Block);
            int ardLoss = Math.Min(incoming.ArdLoss, Stats.Ard);

            Block -= incoming.BlockAbsorbed;
            Stats.Ard -= ardLoss;
            DamageTaken += ardLoss;
            Emit(new DamageTaken(opportunity.PositionQb, opportunity.Index, ardLoss, incoming.BlockAbsorbed));

            if (Stats.Ard == 0)
            {
                End(BattleOutcome.Died, opportunity.PositionQb);
            }
        }

        private void ResolvePlayerEffect(ActionOpportunity opportunity, PendingPress press)
        {
            var card = press.Card;
            if (press.SignatureSend)
            {
                _signatureChain.Add(card.Id);
                Emit(new CardBanked(opportunity.PositionQb, press.Slot, card.Id));
                return;
            }

            switch (card.Category)
            {
                case CardCategory.Defense:
                {
                    int gained = Resolution.PlayerEffect(card.Value, press.Grade, 0);
                    Block += gained;
                    Emit(new BlockGained(opportunity.PositionQb, opportunity.Index, gained));
                    break;
                }

                case CardCategory.LeftAttack:
                case CardCategory.RightAttack:
                {
                    int efficacy = Resolution.AttackEfficacy(opportunity.Action, card.Category);
                    int damage = Resolution.PlayerEffect(card.Value, press.Grade, Stats.BaseDmg, efficacy);
                    damage = Math.Min(damage, EnemyHp);
                    EnemyHp -= damage;
                    Emit(new DamageDealt(opportunity.PositionQb, opportunity.Index, damage));
                    if (EnemyHp == 0)
                    {
                        End(BattleOutcome.Won, opportunity.PositionQb);
                    }

                    break;
                }

                case CardCategory.Ability:
                    // An Ability's effect resolves through the effect framework (P8.1); Phase 3
                    // has no effects to attach.
                    break;

                default:
                    throw new InvalidOperationException($"Unknown card category {card.Category}.");
            }
        }

        private void End(BattleOutcome outcome, int positionQb)
        {
            Outcome = outcome;
            Emit(new BattleEnded(positionQb, outcome));
        }

        private void Emit(BattleEvent battleEvent)
        {
            _events.Add(battleEvent);
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
    }
}
