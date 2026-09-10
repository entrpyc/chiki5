using System;
using System.Collections.Generic;

namespace Chiki.Sim
{
    /// <summary>
    /// The battle aggregate (PRD 4.8): one player against one enemy (PRD 3.3.1.7) on the enemy's
    /// track and chart. It ticks in beats from the track's beat map, takes its action
    /// opportunities from the chart (PRD 3.3.1.8), grades pressed inputs (PRD 3.3.3.1) and
    /// appends every state change to an ordered event stream that subscribers read.
    ///
    /// The client drives it with audio time: <see cref="Advance"/> processes everything up to a
    /// time and <see cref="Press"/> stamps a slot press with the audio time of the key event.
    /// An enemy action resolves when its Judgment Window closes, with the accepted press if there
    /// was one and as no input otherwise (PRD 3.3.3.2).
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

        /// <summary>The latest audio time, in milliseconds, the battle has processed.</summary>
        public int CurrentTimeMs { get; private set; }

        /// <summary>The last beat that started, or -1 before beat 0.</summary>
        public int CurrentBeat => _nextBeat - 1;

        /// <summary>The player's Block (PRD 3.3.4.2).</summary>
        public int Block { get; private set; }

        /// <summary>Total damage taken this battle (PRD 3.3.9.4).</summary>
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

        public Battle(RunStats stats, EnemyDefinition enemy)
            : this(stats, new[] { enemy })
        {
        }

        /// <summary>Constructs a battle; exactly one enemy is required (PRD 3.3.1.7).</summary>
        public Battle(RunStats stats, IReadOnlyList<EnemyDefinition> enemies)
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

            Enemy = enemies[0] ?? throw new ArgumentException("The enemy must not be null.", nameof(enemies));
            if (Enemy.Chart.Actions.Count == 0)
            {
                throw new ArgumentException("The enemy's chart has no actions.", nameof(enemies));
            }

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

        /// <summary>Processes beats and window closures up to the given audio time.</summary>
        public void Advance(int audioTimeMs)
        {
            if (audioTimeMs <= CurrentTimeMs)
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
        /// A slot press stamped with the audio time of the key event (PRD 3.3.3.1). The press is
        /// graded against the enemy action whose Judgment Window contains the time; the first
        /// press for an action is accepted and any later one rejected, as is a press when no
        /// window is open (PRD 3.3.1.3). A rejected press consumes nothing and records nothing.
        /// </summary>
        public PressResult Press(Slot slot, int audioTimeMs)
        {
            if (slot is null)
            {
                throw new ArgumentNullException(nameof(slot));
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
            _pendingPress = new PendingPress(slot, grade);
            Emit(new InputJudged(_pending.PositionQb, _pending.Index, slot, grade, offset));
            return new PressResult(PressOutcome.Accepted, _pending.Index, grade);
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

        private void ResolvePending()
        {
            var opportunity = _pending;
            var press = _pendingPress;

            _judgmentLog.Add(new JudgmentEntry(
                opportunity.Index,
                opportunity.PositionQb,
                opportunity.Action.Kind,
                press?.Grade,
                press?.Slot,
                null));

            if (opportunity.Action.IsAttack)
            {
                // P3.1 supplies the incoming-damage formula (PRD 3.3.4.2); until then an attack
                // resolves for 0.
                Emit(new DamageTaken(opportunity.PositionQb, opportunity.Index, 0));
            }

            _pendingPress = null;
            _pending = OpportunityAt(opportunity.Index + 1);
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
            public Judgment Grade { get; }

            public PendingPress(Slot slot, Judgment grade)
            {
                Slot = slot;
                Grade = grade;
            }
        }
    }
}
