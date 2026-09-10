namespace Chiki.Sim
{
    /// <summary>
    /// One judgment log entry per enemy action (PRD 4.8, 3.3.3.1): the grade of the press that
    /// answered it, or no input (<see cref="Grade"/> is null, PRD 3.3.3.2), the slot pressed and
    /// the card played. Feeds the run log (PRD 3.15.1).
    /// </summary>
    public sealed record JudgmentEntry(
        int ActionIndex,
        int PositionQb,
        EnemyActionKind Kind,
        Judgment? Grade,
        Slot? Slot,
        string? CardId)
    {
        public bool NoInput => Grade is null;
    }
}
