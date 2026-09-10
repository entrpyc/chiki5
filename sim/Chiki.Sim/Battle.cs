using System;

namespace Chiki.Sim
{
    /// <summary>
    /// The battle aggregate. In Phase 1 it only holds a reference to the run's shared
    /// <see cref="RunStats"/>; later phases add the track, chart, judgment and the event stream.
    /// </summary>
    public sealed class Battle
    {
        /// <summary>The run-wide stats this battle reads and writes; never a copy.</summary>
        public RunStats Stats { get; }

        public Battle(RunStats stats)
        {
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
        }
    }
}
