using System;

namespace Chiki.Sim
{
    /// <summary>The ability pool (PRD 3.6.5–3.6.18), one member per row; A08 and A15 are retired and never reused.</summary>
    public enum EnemyAbility
    {
        /// <summary>A01 (PRD 3.6.5).</summary>
        RisingTempo,

        /// <summary>A02 (PRD 3.6.6).</summary>
        MisstepPain,

        /// <summary>A03 (PRD 3.6.7).</summary>
        Counterblade,

        /// <summary>A04 (PRD 3.6.8).</summary>
        Pressure,

        /// <summary>A05 (PRD 3.6.9).</summary>
        IronVeil,

        /// <summary>A06 (PRD 3.6.10).</summary>
        BackflashBarrier,

        /// <summary>A07 (PRD 3.6.11).</summary>
        FakeMove,

        /// <summary>A09 (PRD 3.6.12).</summary>
        HeavyHand,

        /// <summary>A10 (PRD 3.6.13).</summary>
        BeatRush,

        /// <summary>A11 (PRD 3.6.14).</summary>
        ChainBreaker,

        /// <summary>A12 (PRD 3.6.15).</summary>
        DoubleStep,

        /// <summary>A13 (PRD 3.6.16).</summary>
        ChargeBuff,

        /// <summary>A14 (PRD 3.6.17).</summary>
        FrenzyMode,

        /// <summary>A16 (PRD 3.6.18).</summary>
        CorruptionAegis,
    }

    /// <summary>The trait pool (PRD 3.6.19–3.6.25), one member per row.</summary>
    public enum EnemyTrait
    {
        /// <summary>T01 (PRD 3.6.19).</summary>
        AccuracyBet,

        /// <summary>T02 (PRD 3.6.20).</summary>
        Stoneform,

        /// <summary>T03 (PRD 3.6.21).</summary>
        AbsorbShell,

        /// <summary>T04 (PRD 3.6.22).</summary>
        PureHeart,

        /// <summary>T05 (PRD 3.6.23).</summary>
        BloodLeech,

        /// <summary>T06 (PRD 3.6.24).</summary>
        ThornsShell,

        /// <summary>T07 (PRD 3.6.25).</summary>
        Guard,
    }

    public static class EnemyPowers
    {
        /// <summary>The PRD table id of an ability (PRD 3.6.5–3.6.18).</summary>
        public static string CodeOf(EnemyAbility ability)
        {
            switch (ability)
            {
                case EnemyAbility.RisingTempo: return "A01";
                case EnemyAbility.MisstepPain: return "A02";
                case EnemyAbility.Counterblade: return "A03";
                case EnemyAbility.Pressure: return "A04";
                case EnemyAbility.IronVeil: return "A05";
                case EnemyAbility.BackflashBarrier: return "A06";
                case EnemyAbility.FakeMove: return "A07";
                case EnemyAbility.HeavyHand: return "A09";
                case EnemyAbility.BeatRush: return "A10";
                case EnemyAbility.ChainBreaker: return "A11";
                case EnemyAbility.DoubleStep: return "A12";
                case EnemyAbility.ChargeBuff: return "A13";
                case EnemyAbility.FrenzyMode: return "A14";
                case EnemyAbility.CorruptionAegis: return "A16";
                default: throw new ArgumentOutOfRangeException(nameof(ability), ability, "Unknown ability.");
            }
        }

        /// <summary>The PRD table id of a trait (PRD 3.6.19–3.6.25).</summary>
        public static string CodeOf(EnemyTrait trait)
        {
            switch (trait)
            {
                case EnemyTrait.AccuracyBet: return "T01";
                case EnemyTrait.Stoneform: return "T02";
                case EnemyTrait.AbsorbShell: return "T03";
                case EnemyTrait.PureHeart: return "T04";
                case EnemyTrait.BloodLeech: return "T05";
                case EnemyTrait.ThornsShell: return "T06";
                case EnemyTrait.Guard: return "T07";
                default: throw new ArgumentOutOfRangeException(nameof(trait), trait, "Unknown trait.");
            }
        }
    }
}
