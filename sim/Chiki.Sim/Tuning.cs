namespace Chiki.Sim
{
    /// <summary>
    /// The one home for every tuning constant. Each constant cites its PRD number.
    /// </summary>
    public static class Tuning
    {
        /// <summary>Baseline maximum ARD at the start of a run (PRD 3.2.3).</summary>
        public const int ArdBaseline = 300;

        /// <summary>CRP is a run-scoped stat clamped to 0..100 (PRD 3.8.1).</summary>
        public const int CrpMin = 0;

        /// <summary>CRP is a run-scoped stat clamped to 0..100 (PRD 3.8.1).</summary>
        public const int CrpMax = 100;

        /// <summary>A run is three Worlds played in order (PRD 3.2.1).</summary>
        public const int WorldCount = 3;

        /// <summary>The player equips 0–2 Charms into 2 slots before a run (PRD 3.9.6).</summary>
        public const int CharmSlots = 2;

        /// <summary>Armor upgrade slots on a run (PRD 3.7.13).</summary>
        public const int ArmorUpgradeSlots = 4;

        /// <summary>Every node transition adds +1 CRP (PRD 3.8.2).</summary>
        public const int CrpPerTransition = 1;

        /// <summary>A World graph holds 55–70 nodes (PRD 3.2.6).</summary>
        public const int MapMinNodes = 55;
        public const int MapMaxNodes = 70;

        /// <summary>Nodes on any entry-to-Boss path before the Boss, so a traversal visits about 15 (PRD 3.2.6).</summary>
        public const int MapMinPathNodes = 13;
        public const int MapMaxPathNodes = 17;

        /// <summary>The first layer branches into 2–3 routes (PRD 3.2.5).</summary>
        public const int MapMinRoutes = 2;
        public const int MapMaxRoutes = 3;

        /// <summary>Middle layers hold 3–5 nodes, adjusted to reach the node count.</summary>
        public const int MapMinLayerWidth = 3;
        public const int MapMaxLayerWidth = 5;

        /// <summary>No path runs more than 4 nodes without offering a choice (PRD 3.2.5).</summary>
        public const int MapMaxChoicelessRun = 4;

        /// <summary>Generation retries with the next fork until the constraints hold; beyond this it is a content error.</summary>
        public const int MapMaxAttempts = 1000;

        /// <summary>Node type shares per World as percent of generated nodes, target and band (PRD 3.2.6), in the order Normal, Elite, Shop, Event, Blacksmith, Forge; the Boss is the one final node.</summary>
        public static readonly int[][] MapTypeTargetPercent =
        {
            new[] { 52, 7, 9, 17, 7, 8 },
            new[] { 44, 12, 9, 17, 7, 11 },
            new[] { 44, 12, 9, 12, 8, 15 },
        };

        public static readonly int[][] MapTypeMinPercent =
        {
            new[] { 50, 5, 5, 15, 5, 5 },
            new[] { 40, 8, 5, 15, 5, 8 },
            new[] { 40, 10, 5, 10, 5, 10 },
        };

        public static readonly int[][] MapTypeMaxPercent =
        {
            new[] { 60, 10, 15, 20, 15, 15 },
            new[] { 50, 18, 15, 20, 15, 18 },
            new[] { 50, 15, 15, 15, 15, 20 },
        };

        /// <summary>Cards a Normal battle reward offers, of which the player takes one (PRD 3.7.2).</summary>
        public const int NormalRewardCardChoices = 3;

        /// <summary>Essence income band per battle, inclusive, indexed by tier (Normal, Elite, Boss) then World 1–3 (PRD 3.7.5).</summary>
        public static readonly int[][] EssenceIncomeMin =
        {
            new[] { 8, 12, 18 },
            new[] { 25, 37, 55 },
            new[] { 40, 60, 90 },
        };

        public static readonly int[][] EssenceIncomeMax =
        {
            new[] { 17, 25, 37 },
            new[] { 35, 52, 78 },
            new[] { 50, 75, 112 },
        };

        /// <summary>Every relationship starts at level 1 (PRD 3.10.3).</summary>
        public const int NpcStartLevel = 1;

        /// <summary>Main NPCs have levels 1–10 (PRD 3.10.3).</summary>
        public const int MainNpcMaxLevel = 10;

        /// <summary>Secondary NPCs have levels 1–5 (PRD 3.10.3).</summary>
        public const int SecondaryNpcMaxLevel = 5;

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

        /// <summary>Rising Tempo: Base DMG the enemy gains each time it deals damage (PRD 3.6.5).</summary>
        public const int RisingTempoBaseDmgPerHit = 3;

        /// <summary>Misstep Pain: damage the player takes on a Good (PRD 3.6.6).</summary>
        public const int MisstepPainGoodDamage = 5;

        /// <summary>Misstep Pain: damage the player takes on a Miss (PRD 3.6.6).</summary>
        public const int MisstepPainMissDamage = 10;

        /// <summary>Pressure: the multiplier on the enemy's next attack after a no-input action, in thousandths: 2x (PRD 3.6.8).</summary>
        public const int PressureMultiplierThousandths = 2000;

        /// <summary>Iron Veil: how much less damage the enemy takes, in thousandths: 80% (PRD 3.6.9).</summary>
        public const int IronVeilReductionThousandths = 800;

        /// <summary>Iron Veil: beats it lasts (PRD 3.6.9).</summary>
        public const int IronVeilBeats = 5;

        /// <summary>Charge: the multiplier on the empowered move's damage, in thousandths: 2x (PRD 3.6.16).</summary>
        public const int ChargeDamageMultiplierThousandths = 2000;

        /// <summary>Stoneform: consecutive beats without taking damage that grant Block (PRD 3.6.20).</summary>
        public const int StoneformQuietBeats = 3;

        /// <summary>Stoneform: Block granted (PRD 3.6.20).</summary>
        public const int StoneformBlock = 10;

        /// <summary>Guard: Block the enemy starts combat with (PRD 3.6.25).</summary>
        public const int GuardBlock = 30;

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
