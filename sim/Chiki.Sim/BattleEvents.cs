namespace Chiki.Sim
{
    /// <summary>How a battle ended (PRD 4.8, 3.3.9.2, 3.3.9.3).</summary>
    public enum BattleOutcome
    {
        Won,
        Died,
    }

    /// <summary>
    /// One entry of the battle's event stream (PRD 4.8). Every state change appends exactly one
    /// typed event; presenters, Charm triggers, run logs and tests read the stream in order.
    /// <see cref="PositionQb"/> is the quarter-beat position the event belongs to.
    /// </summary>
    public abstract record BattleEvent(int PositionQb);

    /// <summary>A whole beat has started; durations tick here (PRD 3.3.1.4).</summary>
    public sealed record BeatStarted(int PositionQb, int Beat) : BattleEvent(PositionQb);

    /// <summary>A pressed input was graded against an enemy action (PRD 3.3.3.1).</summary>
    public sealed record InputJudged(int PositionQb, int ActionIndex, Slot Slot, Judgment Grade, int OffsetMs) : BattleEvent(PositionQb);

    /// <summary>The player dealt damage to the enemy (PRD 3.3.4.3).</summary>
    public sealed record DamageDealt(int PositionQb, int Amount) : BattleEvent(PositionQb);

    /// <summary>An enemy attack resolved against the player (PRD 3.3.4.2).</summary>
    public sealed record DamageTaken(int PositionQb, int ActionIndex, int Amount) : BattleEvent(PositionQb);

    /// <summary>A status landed on the player or the enemy (PRD 3.3.7.1).</summary>
    public sealed record StatusApplied(int PositionQb, string StatusId, bool OnPlayer, int Beats) : BattleEvent(PositionQb);

    /// <summary>A card was banked into the Signature Chain (PRD 3.3.6.1).</summary>
    public sealed record CardBanked(int PositionQb, Slot Slot, string CardId) : BattleEvent(PositionQb);

    /// <summary>The Signature Chain fired (PRD 3.3.6.2).</summary>
    public sealed record SignatureFired(int PositionQb, int CardCount) : BattleEvent(PositionQb);

    /// <summary>The battle ended (PRD 3.3.9.2, 3.3.9.3).</summary>
    public sealed record BattleEnded(int PositionQb, BattleOutcome Outcome) : BattleEvent(PositionQb);
}
