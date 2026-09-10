namespace Chiki.Sim
{
    /// <summary>
    /// A player action opportunity (PRD 3.3.1.8): one charted enemy action with its Judgment
    /// Window in audio time. <see cref="Index"/> counts actions from the start of the battle
    /// across laps; <see cref="PositionQb"/> is likewise absolute. The window is centred on the
    /// action's charted time and spans <see cref="OpenMs"/>..<see cref="CloseMs"/> inclusive.
    /// </summary>
    public sealed record ActionOpportunity(
        int Index,
        EnemyAction Action,
        int PositionQb,
        int Bpm,
        int CentreMs,
        int OpenMs,
        int CloseMs)
    {
        public bool Contains(int audioTimeMs)
        {
            return audioTimeMs >= OpenMs && audioTimeMs <= CloseMs;
        }
    }
}
