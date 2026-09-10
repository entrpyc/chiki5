using System;

namespace Chiki.Sim
{
    /// <summary>
    /// One status application as content declares it (PRD 3.3.7.1): the status, the stacks it
    /// brings and the source's X where the status has one, Weak's percentage in thousandths
    /// (PRD 3.3.7.4) and Thorns' damage (PRD 3.3.7.7). An enemy action carries them for the
    /// player (PRD 3.3.4.6); card effects and enemy abilities carry them through the effect
    /// framework (P8.1). Validated on construction so content that breaks a rule fails at load.
    /// </summary>
    public sealed record StatusApplication
    {
        public StatusKind Kind { get; }

        public int Stacks { get; }

        public int Value { get; }

        public StatusApplication(StatusKind kind, int stacks = 1, int value = 0)
        {
            if (stacks < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(stacks), "At least one stack must be applied.");
            }

            switch (kind)
            {
                case StatusKind.Weak:
                    if (value < 1 || value > Fixed.One)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), "Weak's X must be 1–1000 thousandths.");
                    }

                    break;
                case StatusKind.Thorns:
                    if (value < 1)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), "Thorns' damage must be positive.");
                    }

                    break;
                case StatusKind.Scar:
                case StatusKind.Stun:
                case StatusKind.Bleed:
                case StatusKind.Disarmed:
                    if (value != 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), $"{kind} has no per-source value.");
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown status.");
            }

            Kind = kind;
            Stacks = stacks;
            Value = value;
        }
    }
}
