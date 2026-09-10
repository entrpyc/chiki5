namespace Chiki.Sim
{
    /// <summary>
    /// The one home for every tuning constant. Each constant cites its PRD number.
    /// </summary>
    public static class Tuning
    {
        /// <summary>Baseline maximum ARD at the start of a run (PRD 3.2.3).</summary>
        public const int ArdBaseline = 300;

        /// <summary>
        /// Half-width of the Perfect window in thousandths of a beat (PRD 3.3.3.1, 3.3.1.5).
        /// 80 thousandths of a beat is +/-40 ms at BPM 120.
        /// </summary>
        public const int PerfectWindowBeatThousandths = 80;

        /// <summary>
        /// Half-width of the Good window in thousandths of a beat (PRD 3.3.3.1, 3.3.1.5).
        /// 180 thousandths of a beat is +/-90 ms at BPM 120, narrower than a quarter beat.
        /// </summary>
        public const int GoodWindowBeatThousandths = 180;

        /// <summary>
        /// Half-width of the Judgment Window itself, the region in which a press is accepted
        /// for an enemy action at all (PRD 3.3.1.8, 3.3.3.1), in thousandths of a beat. A press
        /// inside it but outside the Good window is a Miss. One quarter beat, so an action's
        /// window always closes before the next beat starts; neighbouring actions split the
        /// region at their midpoint (see <see cref="Battle"/>).
        /// </summary>
        public const int JudgmentWindowBeatThousandths = 250;

        /// <summary>IncomingMult on a Perfect, in thousandths: 0% (PRD 3.3.4.2).</summary>
        public const int IncomingMultPerfectThousandths = 0;

        /// <summary>IncomingMult on a Good, in thousandths: 50% (PRD 3.3.4.2).</summary>
        public const int IncomingMultGoodThousandths = 500;

        /// <summary>IncomingMult on a Miss, in thousandths: 100% (PRD 3.3.4.2).</summary>
        public const int IncomingMultMissThousandths = 1000;

        /// <summary>IncomingMult on no input, in thousandths: 100% (PRD 3.3.4.2, 3.3.3.2).</summary>
        public const int IncomingMultNoInputThousandths = 1000;

        /// <summary>JudgmentMult on a Perfect, in thousandths: 100% (PRD 3.3.4.3).</summary>
        public const int JudgmentMultPerfectThousandths = 1000;

        /// <summary>JudgmentMult on a Good, in thousandths: 50% (PRD 3.3.4.3).</summary>
        public const int JudgmentMultGoodThousandths = 500;

        /// <summary>JudgmentMult on a Miss, in thousandths: 0% (PRD 3.3.4.3).</summary>
        public const int JudgmentMultMissThousandths = 0;

        /// <summary>Shortest slot cooldown a card may define, in beats (PRD 3.3.5.1, 3.4.7).</summary>
        public const int CooldownMinBeats = 2;

        /// <summary>Longest slot cooldown a card may define, in beats (PRD 3.3.5.1, 3.4.7).</summary>
        public const int CooldownMaxBeats = 6;

        /// <summary>Cards the Signature Chain holds; the Signature fires when it is full (PRD 3.3.6.1, 3.3.6.2).</summary>
        public const int SignatureChainSlots = 3;

        /// <summary>Damage the Signature deals to the enemy (PRD 3.3.6.2).</summary>
        public const int SignatureDamage = 30;

        /// <summary>Per Scar stack, the chance in thousandths that a Perfect hit deals double damage: 2% (PRD 3.3.7.3).</summary>
        public const int ScarChancePerStackThousandths = 20;

        /// <summary>The multiplier a Scar-triggered hit gets, in thousandths: double (PRD 3.3.7.3).</summary>
        public const int ScarHitMultiplierThousandths = 2000;

        /// <summary>Beats each Scar stack lasts (PRD 3.3.7.3).</summary>
        public const int ScarStackBeats = 10;

        /// <summary>Beats Weak lasts (PRD 3.3.7.4).</summary>
        public const int WeakBeats = 8;

        /// <summary>Beats Bleed lasts (PRD 3.3.7.6).</summary>
        public const int BleedBeats = 8;

        /// <summary>Damage per Bleed stack per beat (PRD 3.3.7.6).</summary>
        public const int BleedDamagePerStack = 1;

        /// <summary>Common attack damage band (PRD 3.4.4).</summary>
        public const int CommonDamageMin = 8;
        public const int CommonDamageMax = 12;

        /// <summary>Uncommon attack damage band (PRD 3.4.4).</summary>
        public const int UncommonDamageMin = 12;
        public const int UncommonDamageMax = 16;

        /// <summary>Rare attack damage band (PRD 3.4.4).</summary>
        public const int RareDamageMin = 16;
        public const int RareDamageMax = 22;

        /// <summary>Legendary attack damage band (PRD 3.4.4).</summary>
        public const int LegendaryDamageMin = 22;
        public const int LegendaryDamageMax = 26;

        /// <summary>Common Defense Block band (PRD 3.4.4).</summary>
        public const int CommonBlockMin = 5;
        public const int CommonBlockMax = 10;

        /// <summary>Uncommon Defense Block band (PRD 3.4.4).</summary>
        public const int UncommonBlockMin = 10;
        public const int UncommonBlockMax = 16;

        /// <summary>Rare Defense Block band (PRD 3.4.4).</summary>
        public const int RareBlockMin = 16;
        public const int RareBlockMax = 21;

        /// <summary>Legendary Defense Block band, open at the top (PRD 3.4.4).</summary>
        public const int LegendaryBlockMin = 22;

        /// <summary>Intended duration band of a Normal encounter in seconds (PRD 3.3.9.1).</summary>
        public const int NormalMinSeconds = 30;
        public const int NormalMaxSeconds = 60;

        /// <summary>Intended duration band of an Elite encounter in seconds (PRD 3.3.9.1).</summary>
        public const int EliteMinSeconds = 60;
        public const int EliteMaxSeconds = 90;

        /// <summary>Intended duration band of a Boss encounter in seconds (PRD 3.3.9.1).</summary>
        public const int BossMinSeconds = 90;
        public const int BossMaxSeconds = 180;

        /// <summary>Aggressor HP multiplier in thousandths: 0.8x (PRD 3.6.1).</summary>
        public const int AggressorHpMultiplierThousandths = 800;

        /// <summary>Tank HP multiplier in thousandths: 1.2x (PRD 3.6.1).</summary>
        public const int TankHpMultiplierThousandths = 1200;

        /// <summary>Mentalist HP multiplier in thousandths: 1.0x (PRD 3.6.1).</summary>
        public const int MentalistHpMultiplierThousandths = 1000;

        /// <summary>Normal tier capability budget: abilities, traits, statuses used (PRD 3.6.4).</summary>
        public const int NormalAbilitiesMin = 1;
        public const int NormalAbilitiesMax = 1;
        public const int NormalTraitsMin = 0;
        public const int NormalTraitsMax = 1;
        public const int NormalStatusesMin = 0;
        public const int NormalStatusesMax = 1;

        /// <summary>Elite tier capability budget (PRD 3.6.4).</summary>
        public const int EliteAbilitiesMin = 1;
        public const int EliteAbilitiesMax = 1;
        public const int EliteTraitsMin = 1;
        public const int EliteTraitsMax = 2;
        public const int EliteStatusesMin = 1;
        public const int EliteStatusesMax = 2;

        /// <summary>Boss tier capability budget (PRD 3.6.4).</summary>
        public const int BossAbilitiesMin = 1;
        public const int BossAbilitiesMax = 2;
        public const int BossTraitsMin = 2;
        public const int BossTraitsMax = 2;
        public const int BossStatusesMin = 1;
        public const int BossStatusesMax = 2;

        /// <summary>Shortest Charge wind-up in beats (PRD 3.6.16, 3.6.31).</summary>
        public const int ChargeWindUpMinBeats = 3;

        /// <summary>Longest Charge wind-up in beats (PRD 3.6.16, 3.6.31).</summary>
        public const int ChargeWindUpMaxBeats = 5;

        /// <summary>The typical AttackRatio of the HP formula in thousandths: 0.6 (PRD 3.7.15).</summary>
        public const int DefaultAttackRatioThousandths = 600;

        /// <summary>The expected average card damage the HP formula assumes per World (PRD 3.2.17, 3.7.15).</summary>
        public const int AvgCardDmgWorld1 = 12;
        public const int AvgCardDmgWorld2 = 14;
        public const int AvgCardDmgWorld3 = 16;

        /// <summary>Enemy damage per hit rises by this much per World above World 1, in thousandths: 15% (PRD 3.7.16).</summary>
        public const int DamageRisePerWorldThousandths = 150;

        /// <summary>Aggressor damage-per-hit bands by tier, World 1 base (PRD 3.7.16).</summary>
        public const int AggressorNormalDamageMin = 10;
        public const int AggressorNormalDamageMax = 15;
        public const int AggressorEliteDamageMin = 18;
        public const int AggressorEliteDamageMax = 25;
        public const int AggressorBossDamageMin = 35;
        public const int AggressorBossDamageMax = 45;

        /// <summary>Mentalist damage-per-hit bands by tier, World 1 base (PRD 3.7.16).</summary>
        public const int MentalistNormalDamageMin = 8;
        public const int MentalistNormalDamageMax = 12;
        public const int MentalistEliteDamageMin = 15;
        public const int MentalistEliteDamageMax = 20;
        public const int MentalistBossDamageMin = 30;
        public const int MentalistBossDamageMax = 40;

        /// <summary>Tank damage-per-hit bands by tier, World 1 base (PRD 3.7.16).</summary>
        public const int TankNormalDamageMin = 6;
        public const int TankNormalDamageMax = 10;
        public const int TankEliteDamageMin = 12;
        public const int TankEliteDamageMax = 18;
        public const int TankBossDamageMin = 25;
        public const int TankBossDamageMax = 35;
    }
}
