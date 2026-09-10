using System;
using System.Collections.Generic;
using System.Linq;

namespace Chiki.Sim
{
    /// <summary>
    /// One charted action as authored (PRD 3.6.31), before validation: the kind by its content
    /// id and the position in thousandths of a beat, so a position off the quarter-beat grid
    /// can be named in a violation. The rest is as on <see cref="EnemyAction"/>.
    /// </summary>
    public sealed record ChartActionDocument(
        string KindId,
        int PositionBeatThousandths,
        int DefenseLevel = 0,
        int WindUpBeats = 0,
        IReadOnlyList<StatusApplication>? Applies = null)
    {
        /// <summary>Whether the position lies on a beat or a quarter-beat subdivision (PRD 3.3.1.8).</summary>
        public bool OnGrid => PositionBeatThousandths >= 0 && PositionBeatThousandths % QuarterBeatThousandths == 0;

        /// <summary>The position in quarter beats; only meaningful when <see cref="OnGrid"/>.</summary>
        public int PositionQb => PositionBeatThousandths / QuarterBeatThousandths;

        /// <summary>The position as a decimal number of beats, for messages.</summary>
        public string PositionText => Fixed.ToDecimalText(PositionBeatThousandths);

        public const int QuarterBeatThousandths = Fixed.One / Beats.QuarterBeatsPerBeat;
    }

    /// <summary>A chart as authored (PRD 4.16): the ids it names and its actions before validation.</summary>
    public sealed record ChartDocument(string Id, string EnemyId, string TrackId, IReadOnlyList<ChartActionDocument> Actions);

    /// <summary>One broken rule in one chart; <see cref="ActionIndex"/> is the offending action, or null for the chart as a whole.</summary>
    public sealed record ChartViolation(string ChartId, int? ActionIndex, string Rule, string Message)
    {
        public override string ToString()
        {
            return ActionIndex is null
                ? $"{ChartId}: {Message} ({Rule})"
                : $"{ChartId} action {ActionIndex}: {Message} ({Rule})";
        }
    }

    /// <summary>
    /// The rules every chart must obey (PRD 3.6.31), each a build-time failure on the shipped
    /// charts: at least one action, every action inside the track's length, every position on
    /// a beat or quarter beat, a known action kind in ascending order, and a Charge wind-up of
    /// 3–5 beats (PRD 3.6.16). Every violation is returned, not just the first.
    /// </summary>
    public static class ChartValidator
    {
        public const string RuleActions = "3.6.31";
        public const string RuleWindUp = "3.6.16";

        public static IReadOnlyList<ChartViolation> Validate(ChartDocument chart, Track track)
        {
            if (chart is null)
            {
                throw new ArgumentNullException(nameof(chart));
            }

            if (track is null)
            {
                throw new ArgumentNullException(nameof(track));
            }

            var violations = new List<ChartViolation>();
            if (chart.TrackId != track.Id)
            {
                violations.Add(new ChartViolation(chart.Id, null, RuleActions, $"written for track '{chart.TrackId}', validated against '{track.Id}'"));
            }

            if (chart.Actions.Count == 0)
            {
                violations.Add(new ChartViolation(chart.Id, null, RuleActions, "a chart has at least one action"));
            }

            int lengthThousandths = track.LengthBeats * Fixed.One;
            int lastPosition = -1;
            for (int i = 0; i < chart.Actions.Count; i++)
            {
                var action = chart.Actions[i];
                if (!KnownKinds.Contains(action.KindId, StringComparer.Ordinal))
                {
                    violations.Add(new ChartViolation(chart.Id, i, RuleActions, $"unknown action kind '{action.KindId}'"));
                }

                if (action.PositionBeatThousandths < 0 || action.PositionBeatThousandths >= lengthThousandths)
                {
                    violations.Add(new ChartViolation(chart.Id, i, RuleActions, $"position {action.PositionText} is outside the track's {track.LengthBeats} beats"));
                }
                else if (!action.OnGrid)
                {
                    violations.Add(new ChartViolation(chart.Id, i, RuleActions, $"position {action.PositionText} is not on a beat or quarter beat"));
                }

                if (action.PositionBeatThousandths <= lastPosition)
                {
                    violations.Add(new ChartViolation(chart.Id, i, RuleActions, $"position {action.PositionText} is not after the previous action"));
                }

                lastPosition = action.PositionBeatThousandths;

                if (action.DefenseLevel < 0 || action.DefenseLevel > Fixed.One)
                {
                    violations.Add(new ChartViolation(chart.Id, i, RuleActions, $"defense level {action.DefenseLevel} is outside 0–1000 thousandths"));
                }

                if (action.KindId == KindCharge)
                {
                    if (action.WindUpBeats < Tuning.ChargeWindUpMinBeats || action.WindUpBeats > Tuning.ChargeWindUpMaxBeats)
                    {
                        violations.Add(new ChartViolation(chart.Id, i, RuleWindUp, $"Charge wind-up {action.WindUpBeats} is outside {Tuning.ChargeWindUpMinBeats}–{Tuning.ChargeWindUpMaxBeats} beats"));
                    }
                    else
                    {
                        // The empowered move lands at the end of the wind-up, and is answered there (PRD 3.6.16).
                        int landing = action.PositionBeatThousandths + action.WindUpBeats * Fixed.One;
                        int? next = i + 1 < chart.Actions.Count ? chart.Actions[i + 1].PositionBeatThousandths : (int?)null;
                        if (landing >= lengthThousandths)
                        {
                            violations.Add(new ChartViolation(chart.Id, i, RuleWindUp, $"Charge lands at {Fixed.ToDecimalText(landing)}, outside the track's {track.LengthBeats} beats"));
                        }
                        else if (next != null && landing >= next.Value)
                        {
                            violations.Add(new ChartViolation(chart.Id, i, RuleWindUp, $"Charge lands at {Fixed.ToDecimalText(landing)}, not before the next action at {Fixed.ToDecimalText(next.Value)}"));
                        }
                    }
                }
                else if (action.WindUpBeats != 0)
                {
                    violations.Add(new ChartViolation(chart.Id, i, RuleWindUp, $"only a Charge has a wind-up, not '{action.KindId}'"));
                }
            }

            return violations;
        }

        public const string KindAttackLeft = "attack-left";
        public const string KindAttackRight = "attack-right";
        public const string KindDefend = "defend";
        public const string KindBuff = "buff";
        public const string KindCharge = "charge";

        public static readonly IReadOnlyList<string> KnownKinds = new[]
        {
            KindAttackLeft, KindAttackRight, KindDefend, KindBuff, KindCharge,
        };
    }
}
