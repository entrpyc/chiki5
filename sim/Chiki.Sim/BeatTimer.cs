using System;

namespace Chiki.Sim
{
    /// <summary>
    /// A duration measured in beats (PRD 3.3.1.4). Started through <see cref="Battle.StartTimer"/>,
    /// it counts down by one on every beat the battle starts after it was created and is expired
    /// at zero. Statuses, cooldowns and timed effects all run on this type; nothing takes seconds.
    /// </summary>
    public sealed class BeatTimer
    {
        public int TotalBeats { get; }

        public int RemainingBeats { get; private set; }

        public bool IsExpired => RemainingBeats == 0;

        internal BeatTimer(int beats)
        {
            if (beats < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(beats), "A duration must not be negative.");
            }

            TotalBeats = beats;
            RemainingBeats = beats;
        }

        internal void Tick()
        {
            if (RemainingBeats > 0)
            {
                RemainingBeats--;
            }
        }
    }
}
