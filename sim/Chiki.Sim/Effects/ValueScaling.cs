using System;

namespace Chiki.Sim.Effects
{
    /// <summary>What a card's value may scale with (PRD 3.4.9, 3.8.8): a run stat, the Block held, or a status's stacks on the player.</summary>
    public enum ScalingSource
    {
        Block,
        Ard,
        BaseDmg,
        Essence,
        Crp,
        Status,
    }

    /// <summary>
    /// A declared scaling of a card's value (PRD 3.4.9): base plus <see cref="Amount"/> for every
    /// <see cref="Per"/> whole units of the source held when the card is played. CRP scaling is
    /// only ever this declaration (PRD 3.8.8).
    /// </summary>
    public sealed record ValueScaling
    {
        public ScalingSource Source { get; }

        public int Amount { get; }

        public int Per { get; }

        /// <summary>The status counted when <see cref="Source"/> is <see cref="ScalingSource.Status"/>.</summary>
        public StatusKind? Status { get; }

        public ValueScaling(ScalingSource source, int amount, int per = 1, StatusKind? status = null)
        {
            if (amount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "A scaling adds at least 1.");
            }

            if (per < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(per), "A scaling counts at least 1 unit per step.");
            }

            if (source == ScalingSource.Status ? status is null : status != null)
            {
                throw new ArgumentException("Exactly a status scaling names its status.", nameof(status));
            }

            Source = source;
            Amount = amount;
            Per = per;
            Status = status;
        }

        /// <summary>The bonus for the given units of the source held.</summary>
        public int BonusFor(int unitsHeld)
        {
            return unitsHeld <= 0 ? 0 : (unitsHeld / Per) * Amount;
        }
    }
}
