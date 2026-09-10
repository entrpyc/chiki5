namespace Chiki.Sim
{
    /// <summary>
    /// One of the enemy's upcoming actions for the Rhythm Line (PRD 3.6.3): what it is, the
    /// absolute position it lands on and how far away that is from the battle's current
    /// position. <see cref="Index"/> counts actions from the start of the battle across laps.
    /// For a Charge the landing is the end of its wind-up and <see cref="TelegraphQb"/> is where
    /// the wind-up starts (PRD 3.6.16).
    /// </summary>
    public sealed record UpcomingAction(int Index, EnemyAction Action, int PositionQb, int RemainingQb)
    {
        /// <summary>Whole beats until the action lands; the presenter shows quarters from <see cref="RemainingQb"/>.</summary>
        public int RemainingBeats => RemainingQb / Beats.QuarterBeatsPerBeat;

        /// <summary>The whole beat the action lands on or after.</summary>
        public int Beat => PositionQb / Beats.QuarterBeatsPerBeat;

        /// <summary>Beats of wind-up shown before a Charge lands (PRD 3.6.16); 0 for every other kind.</summary>
        public int WindUpBeats => Action.WindUpBeats;

        /// <summary>The absolute position the telegraph begins: the landing minus the wind-up.</summary>
        public int TelegraphQb => PositionQb - Beats.ToQuarterBeats(WindUpBeats);
    }
}
