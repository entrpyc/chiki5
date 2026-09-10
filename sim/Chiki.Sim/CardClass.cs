namespace Chiki.Sim
{
    /// <summary>The one class every card is of (PRD 3.4.13–3.4.16).</summary>
    public enum CardClass
    {
        /// <summary>Standard pool; normal upgrade and economy rules (PRD 3.4.14).</summary>
        Normal,

        /// <summary>Events and Sacrifice only; never offered in shops or battle rewards (PRD 3.4.15).</summary>
        Event,

        /// <summary>Events and Sacrifice only; lives for a lifespan of N battles in the Binder (PRD 3.4.16).</summary>
        Unstable,
    }

    /// <summary>Where a card definition enters the pool from (PRD 4.4).</summary>
    public enum UnlockSource
    {
        Starter,
        Pool,
        Boss,
        Relationship,
    }
}
