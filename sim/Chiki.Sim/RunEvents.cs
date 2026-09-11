using System.Collections.Generic;

namespace Chiki.Sim
{
    /// <summary>The sources a CRP change names (PRD 3.8.6) besides a card, Imprint or Charm id.</summary>
    public static class CrpSources
    {
        /// <summary>Every node transition adds CRP (PRD 3.8.2).</summary>
        public const string NodeTransition = "node transition";
    }

    /// <summary>
    /// One typed entry of the run's ordered event stream: what happened outside battle, in
    /// order. Presenters, the save and the run log read it; nothing pokes run state from it.
    /// </summary>
    public abstract record RunEvent;

    /// <summary>A World's graph was entered at its entry node (PRD 3.2.1).</summary>
    public sealed record WorldEntered(int World, string EntryId) : RunEvent;

    /// <summary>The player committed to a forward node (PRD 3.2.7).</summary>
    public sealed record NodeTransition(int World, string FromId, string ToId, NodeType ToType) : RunEvent;

    /// <summary>A node's content is done: a battle won, a stop resolved (PRD 3.2.8–3.2.14).</summary>
    public sealed record NodeCompleted(int World, string NodeId, NodeType Type) : RunEvent;

    /// <summary>CRP changed by an amount from a source (PRD 3.8.6); <see cref="Total"/> is the clamped value after.</summary>
    public sealed record CrpChanged(int Amount, string Source, int Total) : RunEvent;

    /// <summary>Essence changed by an amount from a source (PRD 3.7.1); <see cref="Total"/> is the value after, never below zero.</summary>
    public sealed record EssenceChanged(int Amount, string Source, int Total) : RunEvent;

    /// <summary>A won battle node opened its reward offer (PRD 3.3.9.2, 3.7.2): the cards the player may choose one of.</summary>
    public sealed record RewardOffered(int World, string NodeId, EncounterTier Tier, IReadOnlyList<string> CardIds, string? ImprintId = null) : RunEvent;

    /// <summary>A World's Boss fell (PRD 3.2.10); each counts toward Charm unlock milestones on the profile (PRD 3.9.10).</summary>
    public sealed record BossDefeated(int World, string NodeId, string EnemyId) : RunEvent;

    /// <summary>The player picked a card from the offer, or skipped it (PRD 3.7.2); <see cref="CardId"/> is null on a skip.</summary>
    public sealed record RewardResolved(int World, string NodeId, string? CardId) : RunEvent;

    /// <summary>The run ended (PRD 3.9.11).</summary>
    public sealed record RunEnded(RunStatus Outcome) : RunEvent;
}
