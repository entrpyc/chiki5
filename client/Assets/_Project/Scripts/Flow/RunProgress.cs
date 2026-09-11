#nullable enable
using System;
using System.Collections.Generic;
using Chiki.Client.Profiles;
using Chiki.Sim;

namespace Chiki.Client.Flow
{
    /// <summary>
    /// Carries a run's permanent consequences onto the profile (PRD 3.9.2, 3.9.10): every
    /// BossDefeated event in the run's stream not yet applied raises the profile's bosses-defeated
    /// count and evaluates the Charm table, so a Charm whose milestone is now met is unlocked the
    /// moment the Boss falls (PRD 3.9.5). Reads the stream only, never poking run state, and
    /// applies each event once. The caller saves the profile when <see cref="Apply"/> reports a
    /// change.
    /// </summary>
    public sealed class RunProgress
    {
        private int _applied;

        public Profile Profile { get; }

        /// <summary>The Charm table whose unlock conditions are evaluated (PRD 4.9).</summary>
        public CharmSet Charms { get; }

        public RunProgress(Profile profile, CharmSet charms)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            Charms = charms ?? throw new ArgumentNullException(nameof(charms));
        }

        /// <summary>Applies the run events appended since the last call; returns the Charm ids newly unlocked, and reports through <paramref name="changed"/> whether the profile changed at all.</summary>
        public IReadOnlyList<string> Apply(Run run, out bool changed)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            var unlocked = new List<string>();
            changed = false;
            var events = run.Events;
            for (; _applied < events.Count; _applied++)
            {
                if (events[_applied] is BossDefeated)
                {
                    unlocked.AddRange(Profile.RecordBossDefeat(Charms.Charms));
                    changed = true;
                }
            }

            return unlocked;
        }

        public IReadOnlyList<string> Apply(Run run)
        {
            return Apply(run, out _);
        }
    }
}
