namespace Chiki.Sim
{
    /// <summary>What a slot press did (PRD 3.3.1.3, 3.3.5.3, 3.3.6.1).</summary>
    public enum PressOutcome
    {
        /// <summary>The press was graded against the open enemy action.</summary>
        Accepted,

        /// <summary>The open enemy action was already answered; nothing was consumed.</summary>
        ActionAlreadyAnswered,

        /// <summary>No enemy action's Judgment Window is open at the press time; nothing was consumed.</summary>
        NoOpenWindow,

        /// <summary>The slot is on cooldown (PRD 3.3.5.3): disabled feedback, nothing consumed, no judgment.</summary>
        Disabled,

        /// <summary>A Signature send while the chain is full (PRD 3.3.6.1): rejected like a disabled press.</summary>
        SignatureChainFull,
    }

    /// <summary>
    /// The result of <see cref="Battle.Press"/> or <see cref="Battle.Send"/>. On a
    /// <see cref="PressOutcome.Disabled"/> press <see cref="CooldownRemainingBeats"/> carries
    /// the beats left on the slot (PRD 3.3.5.4).
    /// </summary>
    public sealed record PressResult(PressOutcome Outcome, int? ActionIndex, Judgment? Grade, int CooldownRemainingBeats)
    {
        public bool Accepted => Outcome == PressOutcome.Accepted;

        public bool Rejected => !Accepted;

        /// <summary>The press gave disabled feedback: a cooling slot or a full chain (PRD 3.3.5.3).</summary>
        public bool Disabled => Outcome == PressOutcome.Disabled || Outcome == PressOutcome.SignatureChainFull;

        public static readonly PressResult NoWindow = new PressResult(PressOutcome.NoOpenWindow, null, null, 0);

        public static readonly PressResult ChainFull = new PressResult(PressOutcome.SignatureChainFull, null, null, 0);

        public static PressResult AlreadyAnswered(int actionIndex)
        {
            return new PressResult(PressOutcome.ActionAlreadyAnswered, actionIndex, null, 0);
        }

        public static PressResult OnCooldown(int remainingBeats)
        {
            return new PressResult(PressOutcome.Disabled, null, null, remainingBeats);
        }
    }
}
