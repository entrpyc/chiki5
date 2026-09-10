namespace Chiki.Sim
{
    /// <summary>
    /// One of the enemy's upcoming actions for the Rhythm Line (PRD 3.6.3): what it is, the
    /// absolute position it lands on and how far away that is from the battle's current
    /// position. <see cref="Index"/> counts actions from the start of the battle across laps.
    /// </summary>
    public sealed record UpcomingAction(int Index, EnemyAction Action, int PositionQb, int RemainingQb)
    {
        /// <summary>Whole beats until the action lands; the presenter shows quarters from <see cref="RemainingQb"/>.</summary>
        public int RemainingBeats => RemainingQb / Beats.QuarterBeatsPerBeat;

        /// <summary>The whole beat the action lands on or after.</summary>
        public int Beat => PositionQb / Beats.QuarterBeatsPerBeat;
    }
}
