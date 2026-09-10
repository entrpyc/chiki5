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
    /// the HP the enemy lost, 0 for a whiffed side, after the enemy's damage reduction and with
    /// <see cref="BlockAbsorbed"/> taken off its Block first. Also emitted for the Signature's
    /// damage (PRD 3.3.6.2).
    /// </summary>
    public sealed record DamageDealt(int PositionQb, int ActionIndex, int Amount, int BlockAbsorbed = 0) : BattleEvent(PositionQb);

    /// <summary>
    /// An enemy attack resolved against the player (PRD 3.3.4.2): <see cref="BlockAbsorbed"/> came
    /// off Block first and <see cref="Amount"/> is the ARD lost.
    /// </summary>
    public sealed record DamageTaken(int PositionQb, int ActionIndex, int Amount, int BlockAbsorbed) : BattleEvent(PositionQb);

    /// <summary>
    /// True DMG landed on one side (PRD 3.3.4.7): <see cref="Amount"/> came straight off the
    /// enemy's HP or the player's ARD, past Block and every reduction.
    /// </summary>
    public sealed record TrueDamageDealt(int PositionQb, StatusTarget Target, int Amount) : BattleEvent(PositionQb);

    /// <summary>
    /// One side gained Block (PRD 3.3.4.3, 3.3.4.4 for the player's Defense cards; PRD 3.6.20,
    /// 3.6.25 for the enemy's): <see cref="Amount"/> was added and <see cref="Total"/> is the
    /// Block held afterwards.
    /// </summary>
    public sealed record BlockGained(int PositionQb, StatusTarget Target, int Amount, int Total) : BattleEvent(PositionQb);

    /// <summary>One side's Block was cleared at battle end (PRD 3.3.9.5); <see cref="Amount"/> is what it held.</summary>
    public sealed record BlockCleared(int PositionQb, StatusTarget Target, int Amount) : BattleEvent(PositionQb);

    /// <summary>
    /// The enemy takes <see cref="Thousandths"/> less damage for <see cref="Beats"/> beats
    /// (PRD 3.6.9); True DMG ignores it (PRD 3.3.4.7). A new reduction replaces the running one.
    /// </summary>
    public sealed record DamageReductionStarted(int PositionQb, int Thousandths, int Beats) : BattleEvent(PositionQb);

    /// <summary>The enemy's damage reduction ran out (PRD 3.6.9).</summary>
    public sealed record DamageReductionEnded(int PositionQb) : BattleEvent(PositionQb);

    /// <summary>A run stat changed by an effect (PRD 3.2.3): <see cref="Delta"/> was applied and <see cref="Total"/> is the stat afterwards.</summary>
    public sealed record StatChanged(int PositionQb, Effects.RunStat Stat, int Delta, int Total) : BattleEvent(PositionQb);

    /// <summary>
    /// A standing multiplier came alive (P8.1): <see cref="Thousandths"/> on every
    /// <see cref="Value"/> for <see cref="Beats"/> beats, or for the battle when null.
    /// </summary>
    public sealed record ModifierActivated(int PositionQb, int ModifierId, string OwnerId, Effects.EffectValue Value, int Thousandths, int? Beats) : BattleEvent(PositionQb);

    /// <summary>A standing multiplier's beats ran out (P8.1).</summary>
    public sealed record ModifierExpired(int PositionQb, int ModifierId, string OwnerId, Effects.EffectValue Value) : BattleEvent(PositionQb);

    /// <summary>
    /// An Ability card resolved (PRD 3.3.4.3, 3.3.4.4): its effect scales by
    /// <see cref="EffectMultThousandths"/>, the JudgmentMult of the press, which the effect
    /// framework (P8.1) applies to whatever the card declares.
    /// </summary>
    public sealed record AbilityResolved(int PositionQb, int ActionIndex, Slot Slot, string CardId, int EffectMultThousandths) : BattleEvent(PositionQb);

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

    /// <summary>Stacks of a status left one side, by expiry, consumption or battle end (PRD 3.3.7.1, 3.3.9.5); <see cref="StacksLeft"/> is the kind's total afterwards.</summary>
    public sealed record StatusRemoved(int PositionQb, StatusKind Kind, StatusTarget Target, int Stacks, int StacksLeft) : BattleEvent(PositionQb);

    /// <summary>An immunity on one side stopped a status from landing (PRD 3.3.4.6).</summary>
    public sealed record StatusBlocked(int PositionQb, StatusKind Kind, StatusTarget Target) : BattleEvent(PositionQb);

    /// <summary>One side became immune to a status (PRD 3.3.4.6).</summary>
    public sealed record ImmunityGranted(int PositionQb, StatusKind Kind, StatusTarget Target) : BattleEvent(PositionQb);

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
