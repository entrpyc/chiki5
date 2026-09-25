namespace Chiki.Sim
{
    /// <summary>What a slot press did (PRD 3.3.1.3, 3.3.5.3, 3.3.5.5, 3.3.6.1).</summary>
    public enum PressOutcome
    {
        /// <summary>The press was graded against the open enemy action.</summary>
        Accepted,

        /// <summary>
        /// The open enemy action was already answered, so the press answers nothing: a wasted
        /// press (PRD 3.3.5.5), graded a Miss and burning the slot's cooldown.
        /// </summary>
        ActionAlreadyAnswered,

        /// <summary>
        /// No enemy action's Judgment Window is open at the press time: a wasted press
        /// (PRD 3.3.5.5), graded a Miss and burning the slot's cooldown.
        /// </summary>
        NoOpenWindow,

        /// <summary>The slot is on cooldown (PRD 3.3.5.3): disabled feedback, nothing consumed, no judgment.</summary>
        Disabled,

        /// <summary>A Signature send while the chain is full (PRD 3.3.6.1): rejected like a disabled press.</summary>
        SignatureChainFull,

        /// <summary>The player is Stunned, so the press is ignored (PRD 3.3.3.3): nothing consumed, no cooldown.</summary>
        PlayerStunned,

        /// <summary>The battle has already ended (PRD 3.3.9.2, 3.3.9.3): nothing consumed, no cooldown.</summary>
        BattleOver,
    }

    /// <summary>
    /// The result of <see cref="Battle.Press"/> or <see cref="Battle.Send"/>. On a
    /// <see cref="PressOutcome.Disabled"/> press <see cref="CooldownRemainingBeats"/> carries
    /// the beats left on the slot (PRD 3.3.5.4). A wasted press (PRD 3.3.5.5) carries
    /// <see cref="Judgment.Miss"/> as its grade, since the press is recorded as a Miss.
    /// </summary>
    public sealed record PressResult(PressOutcome Outcome, int? ActionIndex, Judgment? Grade, int CooldownRemainingBeats)
    {
        public bool Accepted => Outcome == PressOutcome.Accepted;

        public bool Rejected => !Accepted;

        /// <summary>
        /// The press answered no enemy action and so applied nothing, but is a recorded Miss and
        /// started the slot's cooldown (PRD 3.3.5.5): pressed between actions, or on an action
        /// already answered.
        /// </summary>
        public bool Wasted => Outcome == PressOutcome.NoOpenWindow || Outcome == PressOutcome.ActionAlreadyAnswered;

        /// <summary>The press gave disabled feedback: a cooling slot, a full chain or a Stunned player (PRD 3.3.5.3, 3.3.3.3).</summary>
        public bool Disabled => Outcome == PressOutcome.Disabled || Outcome == PressOutcome.SignatureChainFull || Outcome == PressOutcome.PlayerStunned;

        /// <summary>A press after the battle ended: it does nothing at all.</summary>
        public static readonly PressResult BattleOver = new PressResult(PressOutcome.BattleOver, null, null, 0);

        public static readonly PressResult ChainFull = new PressResult(PressOutcome.SignatureChainFull, null, null, 0);

        public static readonly PressResult Stunned = new PressResult(PressOutcome.PlayerStunned, null, null, 0);

        /// <summary>A wasted press between enemy actions (PRD 3.3.5.5): a Miss, and the slot cools down.</summary>
        public static readonly PressResult BetweenActions = new PressResult(PressOutcome.NoOpenWindow, null, Judgment.Miss, 0);

        /// <summary>A wasted press on an action already answered (PRD 3.3.5.5): a Miss, and the slot cools down.</summary>
        public static PressResult AlreadyAnswered(int actionIndex)
        {
            return new PressResult(PressOutcome.ActionAlreadyAnswered, actionIndex, Judgment.Miss, 0);
        }

        public static PressResult OnCooldown(int remainingBeats)
        {
            return new PressResult(PressOutcome.Disabled, null, null, remainingBeats);
        }
    }
}
