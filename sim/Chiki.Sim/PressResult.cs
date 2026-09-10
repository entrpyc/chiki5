namespace Chiki.Sim
{
    /// <summary>What a slot press did (PRD 3.3.1.3).</summary>
    public enum PressOutcome
    {
        /// <summary>The press was graded against the open enemy action.</summary>
        Accepted,

        /// <summary>The open enemy action was already answered; nothing was consumed.</summary>
        ActionAlreadyAnswered,

        /// <summary>No enemy action's Judgment Window is open at the press time; nothing was consumed.</summary>
        NoOpenWindow,
    }

    /// <summary>The result of <see cref="Battle.Press"/>.</summary>
    public sealed record PressResult(PressOutcome Outcome, int? ActionIndex, Judgment? Grade)
    {
        public bool Accepted => Outcome == PressOutcome.Accepted;

        public bool Rejected => !Accepted;

        public static readonly PressResult NoWindow = new PressResult(PressOutcome.NoOpenWindow, null, null);

        public static PressResult AlreadyAnswered(int actionIndex)
        {
            return new PressResult(PressOutcome.ActionAlreadyAnswered, actionIndex, null);
        }
    }
}
