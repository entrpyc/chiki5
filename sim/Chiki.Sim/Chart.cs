using System;
using System.Collections.Generic;

namespace Chiki.Sim
{
    /// <summary>Kinds of charted enemy action (PRD 3.6.31).</summary>
    public enum EnemyActionKind
    {
        AttackLeft,
        AttackRight,
        Defend,
        Buff,
        Charge,
    }

    /// <summary>
    /// One charted enemy action (PRD 3.6.31, 4.16) at a position in quarter beats within the
    /// track. <see cref="DefenseLevel"/> is in thousandths and only meaningful for Defend;
    /// <see cref="WindUpBeats"/> only for Charge.
    /// </summary>
    public sealed record EnemyAction(EnemyActionKind Kind, int PositionQb, int DefenseLevel = 0, int WindUpBeats = 0)
    {
        public bool IsAttack => Kind == EnemyActionKind.AttackLeft || Kind == EnemyActionKind.AttackRight;
    }

    /// <summary>
    /// An enemy's chart (PRD 4.16, 3.6.31): the ordered actions on that enemy's track. It is the
    /// only source of enemy actions and therefore of player action opportunities (PRD 3.3.1.8).
    /// </summary>
    public sealed record Chart
    {
        public string Id { get; }
        public string EnemyId { get; }
        public Track Track { get; }
        public string TrackId => Track.Id;

        /// <summary>Actions in strictly ascending position order.</summary>
        public IReadOnlyList<EnemyAction> Actions { get; }

        /// <summary>Equals the track's length; the loop point (PRD 3.6.32).</summary>
        public int LengthBeats => Track.LengthBeats;

        /// <summary>Length of one lap in quarter beats.</summary>
        public int LengthQb => Track.LengthQb;

        /// <summary>
        /// Action count divided by length in minutes, exact in thousandths (PRD 4.16); feeds the
        /// HP formula (PRD 3.7.15).
        /// </summary>
        public int ActionsPerMinuteThousandths { get; }

        /// <summary>Actions per minute as a whole number; the fractional part is dropped.</summary>
        public int ActionsPerMinute => ActionsPerMinuteThousandths / Fixed.One;

        public Chart(string id, string enemyId, Track track, IReadOnlyList<EnemyAction> actions)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Chart id is required.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(enemyId))
            {
                throw new ArgumentException("Enemy id is required.", nameof(enemyId));
            }

            Track = track ?? throw new ArgumentNullException(nameof(track));
            if (actions is null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            int lastPosition = -1;
            foreach (var action in actions)
            {
                if (action.PositionQb <= lastPosition)
                {
                    throw new ArgumentException("Chart actions must be in strictly ascending position order.", nameof(actions));
                }

                if (action.PositionQb >= track.LengthQb)
                {
                    throw new ArgumentException("Chart actions must lie inside the track's length.", nameof(actions));
                }

                lastPosition = action.PositionQb;
            }

            Id = id;
            EnemyId = enemyId;
            Actions = actions;

            // count / minutes = count * 60000 / lengthMs, kept in thousandths.
            long lengthMs = track.BeatMap.LengthMs;
            ActionsPerMinuteThousandths = checked((int)(actions.Count * 60_000L * Fixed.One / lengthMs));
        }
    }
}
