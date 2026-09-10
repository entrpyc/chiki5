using System.Collections.Generic;

namespace Chiki.Sim
{
    /// <summary>How a battle ended (PRD 4.8, 3.3.9.2, 3.3.9.3).</summary>
    public enum BattleOutcome
    {
        Won,
        Died,
    }

    /// <summary>Why a press gave disabled feedback (PRD 3.3.5.3, 3.3.6.1).</summary>
    public enum SlotDisabledReason
    {
        /// <summary>The slot is on cooldown (PRD 3.3.5.3).</summary>
        Cooldown,

        /// <summary>A Signature send while all chain slots are taken (PRD 3.3.6.1).</summary>
        SignatureChainFull,

        /// <summary>The player is Stunned, so presses are ignored (PRD 3.3.3.3).</summary>
        PlayerStunned,
    }

    /// <summary>
    /// One entry of the battle's event stream (PRD 4.8). Every state change appends exactly one
    /// typed event; presenters, Charm triggers, run logs and tests read the stream in order.
    /// <see cref="PositionQb"/> is the quarter-beat position the event belongs to.
    /// </summary>
    public abstract record BattleEvent(int PositionQb);

    /// <summary>A whole beat has started; durations tick here (PRD 3.3.1.4).</summary>
    public sealed record BeatStarted(int PositionQb, int Beat) : BattleEvent(PositionQb);

    /// <summary>
    /// A pressed input was graded against an enemy action (PRD 3.3.3.1). <see cref="SignatureSend"/>
    /// is true when the press was Space plus the slot key (PRD 3.3.2.3).
    /// </summary>
    public sealed record InputJudged(int PositionQb, int ActionIndex, Slot Slot, string CardId, Judgment Grade, int OffsetMs, bool SignatureSend) : BattleEvent(PositionQb);

    /// <summary>
    /// A press on a slot that cannot be played right now (PRD 3.3.5.3): the feedback hook for
    /// the disabled sound and flash. Nothing was consumed and no judgment recorded.
    /// <see cref="RemainingBeats"/> is the slot's cooldown left, 0 when the reason is not cooldown.
    /// </summary>
    public sealed record SlotDisabled(int PositionQb, Slot Slot, SlotDisabledReason Reason, int RemainingBeats) : BattleEvent(PositionQb);

    /// <summary>An accepted press started its slot's cooldown of <see cref="Beats"/> beats (PRD 3.3.5.1).</summary>
    public sealed record CooldownStarted(int PositionQb, Slot Slot, string CardId, int Beats) : BattleEvent(PositionQb);

    /// <summary>
    /// The player's attack resolved against the enemy (PRD 3.3.4.3, 3.3.4.4); <see cref="Amount"/> is
    /// the HP the enemy lost, 0 for a whiffed side. Also emitted for the Signature's damage (PRD 3.3.6.2).
    /// </summary>
    public sealed record DamageDealt(int PositionQb, int ActionIndex, int Amount) : BattleEvent(PositionQb);

    /// <summary>
    /// An enemy attack resolved against the player (PRD 3.3.4.2): <see cref="BlockAbsorbed"/> came
    /// off Block first and <see cref="Amount"/> is the ARD lost.
    /// </summary>
    public sealed record DamageTaken(int PositionQb, int ActionIndex, int Amount, int BlockAbsorbed) : BattleEvent(PositionQb);

    /// <summary>A Defense card gave the player Block (PRD 3.3.4.3, 3.3.4.4).</summary>
    public sealed record BlockGained(int PositionQb, int ActionIndex, int Amount) : BattleEvent(PositionQb);

    /// <summary>
    /// A status landed on one side (PRD 3.3.7.1): <see cref="Stacks"/> and <see cref="Value"/>
    /// are what this application brought, <see cref="TotalStacks"/> and
    /// <see cref="RemainingBeats"/> the state afterwards (null when it lasts until consumed).
    /// </summary>
    public sealed record StatusApplied(int PositionQb, StatusKind Kind, StatusTarget Target, int Stacks, int Value, int TotalStacks, int? RemainingBeats) : BattleEvent(PositionQb);

    /// <summary>
    /// A status acted (PRD 3.3.7.2). <see cref="Amount"/> is the HP or ARD the target's opponent
    /// or the target lost for Thorns and Bleed (with <see cref="BlockAbsorbed"/> off the player's
    /// Block first), the damage multiplier in thousandths for Weak, the doubled damage for Scar,
    /// and 0 for Stun, whose act is the skipped action.
    /// </summary>
    public sealed record StatusTriggered(int PositionQb, StatusKind Kind, StatusTarget Target, int Amount, int BlockAbsorbed) : BattleEvent(PositionQb);

    /// <summary>Stacks of a status left one side, by expiry or consumption (PRD 3.3.7.1); <see cref="StacksLeft"/> is the kind's total afterwards.</summary>
    public sealed record StatusRemoved(int PositionQb, StatusKind Kind, StatusTarget Target, int Stacks, int StacksLeft) : BattleEvent(PositionQb);

    /// <summary>A card was banked into the Signature Chain (PRD 3.3.6.1).</summary>
    public sealed record CardBanked(int PositionQb, Slot Slot, string CardId) : BattleEvent(PositionQb);

    /// <summary>
    /// The Signature Chain fired and emptied (PRD 3.3.6.2): <see cref="CardIds"/> are the cards
    /// that were banked and <see cref="Damage"/> the HP the enemy loses, which the following
    /// <see cref="DamageDealt"/> applies.
    /// </summary>
    public sealed record SignatureFired(int PositionQb, int ActionIndex, IReadOnlyList<string> CardIds, int Damage) : BattleEvent(PositionQb);

    /// <summary>
    /// The battle ended (PRD 3.3.9.2, 3.3.9.3) with the ARD lost over the whole battle and
    /// whether that was a Perfect Defense (PRD 3.3.9.4), the trigger Charms read (PRD 3.9.8).
    /// </summary>
    public sealed record BattleEnded(int PositionQb, BattleOutcome Outcome, int DamageTaken, bool PerfectDefense) : BattleEvent(PositionQb);
}
