using Chiki.Sim;
using SimBattle = Chiki.Sim.Battle;
using SimChart = Chiki.Sim.Chart;
using SimRng = Chiki.Sim.Rng;
using SimTrack = Chiki.Sim.Track;
using Stats = Chiki.Sim.RunStats;

namespace Sim;

/// <summary>Builders for the content the simulation tests run against, and access to data/ fixtures.</summary>
internal static class TestContent
{
    public const int DefaultEnemyDmg = 10;
    public const int DefaultEnemyHp = 1000;

    public static readonly Slot SlotQ = new(0, SlotKey.Q);
    public static readonly Slot SlotE = new(0, SlotKey.E);
    public static readonly Slot SlotR = new(0, SlotKey.R);
    public static readonly Slot SlotU = new(0, SlotKey.U);
    public static readonly Slot SlotO = new(0, SlotKey.O);

    /// <summary>Key E on the second line.</summary>
    public static readonly Slot SlotELine2 = new(1, SlotKey.E);

    /// <summary>A 10-damage Left Attack card for slot E or R, with the minimum 2-beat cooldown.</summary>
    public static readonly CardDefinition LeftAttack10 = LeftAttack(10);

    public static CardDefinition LeftAttack(int damage, int cooldownBeats = Tuning.CooldownMinBeats) =>
        new("card-left-" + damage, "Left " + damage, CardCategory.LeftAttack, damage, cooldownBeats);

    public static CardDefinition RightAttack(int damage) => new("card-right-" + damage, "Right " + damage, CardCategory.RightAttack, damage);

    public static CardDefinition Defense(int block) => new("card-defense-" + block, "Guard " + block, CardCategory.Defense, block);

    public static CardDefinition Ability() => new("card-ability", "Ability", CardCategory.Ability, 0);

    public static SimTrack Track(int bpm = 120, int lengthBeats = 32, int offsetMs = 0, params TempoChange[] changes)
    {
        return new SimTrack("track-test", 1, EncounterTier.Normal, lengthBeats, offsetMs, new TempoMap(bpm, changes));
    }

    public static EnemyAction Left(int positionQb) => new(EnemyActionKind.AttackLeft, positionQb);

    /// <summary>A left attack that lands the given statuses on the player when it resolves (PRD 3.3.4.6).</summary>
    public static EnemyAction LeftApplying(int positionQb, params StatusApplication[] applies) =>
        new(EnemyActionKind.AttackLeft, positionQb, applies: applies);

    /// <summary>Weak at the given percentage in thousandths, one stack.</summary>
    public static StatusApplication Weak(int thousandths) => new(StatusKind.Weak, 1, thousandths);

    public static EnemyAction Right(int positionQb) => new(EnemyActionKind.AttackRight, positionQb);

    public static EnemyAction Defend(int positionQb, int defenseLevelThousandths) => new(EnemyActionKind.Defend, positionQb, defenseLevelThousandths);

    public static EnemyAction Buff(int positionQb) => new(EnemyActionKind.Buff, positionQb);

    /// <summary>A chart of left attacks at the given quarter-beat positions.</summary>
    public static SimChart Chart(SimTrack track, params int[] positionsQb)
    {
        return Chart(track, positionsQb.Select(Left).ToArray());
    }

    public static SimChart Chart(SimTrack track, params EnemyAction[] actions)
    {
        return new SimChart("chart-test", "enemy-test", track, actions);
    }

    /// <summary>A valid Normal Fast Aggressor with one ability that registers nothing (Charge / Buff acts through the chart), 45 s intended, on the chart's own track.</summary>
    public static EnemyDefinition Enemy(SimChart chart, int damagePerHit = DefaultEnemyDmg)
    {
        return Enemy(chart, EncounterTier.Normal, EnemyRole.Aggressor, RhythmProfile.Fast, 45, damagePerHit: damagePerHit);
    }

    /// <summary>A Normal Fast Aggressor carrying the given powers, for the power tests (P11).</summary>
    public static EnemyDefinition EnemyWith(SimChart chart, EnemyAbility? ability = null, EnemyTrait? trait = null, int damagePerHit = DefaultEnemyDmg)
    {
        return Enemy(
            chart,
            EncounterTier.Normal,
            EnemyRole.Aggressor,
            RhythmProfile.Fast,
            45,
            ability is null ? null : new[] { ability.Value },
            trait is null ? null : new[] { trait.Value },
            damagePerHit: damagePerHit);
    }

    /// <summary>A telegraphed Charge (PRD 3.6.16): the wind-up starts at the position and the empowered move lands the wind-up later.</summary>
    public static EnemyAction Charge(int positionQb, int windUpBeats) => new(EnemyActionKind.Charge, positionQb, windUpBeats: windUpBeats);

    /// <summary>A battle in World 1 against the given definition with a fixed starting HP.</summary>
    public static SimBattle BattleWith(EnemyDefinition enemy, int enemyHp = DefaultEnemyHp, Stats? stats = null)
    {
        return new SimBattle(stats ?? new Stats(), enemy, enemyHp, Rng());
    }

    public static EnemyDefinition Enemy(
        SimChart chart,
        EncounterTier tier,
        EnemyRole role,
        RhythmProfile? profile,
        int intendedSeconds,
        IReadOnlyList<EnemyAbility>? abilities = null,
        IReadOnlyList<EnemyTrait>? traits = null,
        IReadOnlyList<StatusKind>? statusesUsed = null,
        string? trackId = null,
        int damagePerHit = DefaultEnemyDmg)
    {
        return new EnemyDefinition(
            chart.EnemyId,
            "Test Enemy",
            tier,
            role,
            profile,
            intendedSeconds,
            chart,
            damagePerHit,
            abilities ?? new[] { EnemyAbility.ChargeBuff },
            traits,
            statusesUsed,
            trackId: trackId);
    }

    public static EnemyDefinition Enemy(SimTrack track, params int[] positionsQb)
    {
        return Enemy(Chart(track, positionsQb));
    }

    /// <summary>A battle on a BPM 120, 32-beat track with left attacks at the given positions.</summary>
    public static SimBattle Battle(params int[] positionsQb)
    {
        return Battle(new Stats(), Chart(Track(), positionsQb));
    }

    /// <summary>A battle on a BPM 120, 32-beat track with the given enemy actions.</summary>
    public static SimBattle Battle(params EnemyAction[] actions)
    {
        return Battle(new Stats(), Chart(Track(), actions));
    }

    public static SimBattle Battle(Stats stats, SimChart chart, int enemyDmg = DefaultEnemyDmg, int enemyHp = DefaultEnemyHp, SimRng? rng = null)
    {
        return new SimBattle(stats, Enemy(chart, enemyDmg), enemyHp, rng ?? Rng());
    }

    /// <summary>A fresh generator with a fixed seed for tests that do not roll.</summary>
    public static SimRng Rng() => new(1);

    public static string RepoRoot
    {
        get
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "data")) && Directory.Exists(Path.Combine(dir.FullName, "sim")))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            throw new InvalidOperationException("Repository root with data/ and sim/ not found above " + TestContext.CurrentContext.TestDirectory);
        }
    }

    public static string ReadData(string relativePath)
    {
        return File.ReadAllText(Path.Combine(RepoRoot, "data", relativePath));
    }

    /// <summary>Every track under data/tracks by id.</summary>
    public static Dictionary<string, SimTrack> LoadTracks()
    {
        return Directory.GetFiles(Path.Combine(RepoRoot, "data", "tracks"), "*.json")
            .Select(f => Chiki.Sim.Data.TrackLoader.FromJson(File.ReadAllText(f)))
            .ToDictionary(t => t.Id);
    }

    /// <summary>Every chart under data/charts by id, built on its track.</summary>
    public static Dictionary<string, SimChart> LoadCharts(IReadOnlyDictionary<string, SimTrack> tracks)
    {
        return Directory.GetFiles(Path.Combine(RepoRoot, "data", "charts"), "*.json")
            .Select(f =>
            {
                var document = Chiki.Sim.Data.ChartLoader.Read(File.ReadAllText(f));
                return Chiki.Sim.Data.ChartLoader.Build(document, tracks[document.TrackId]);
            })
            .ToDictionary(c => c.Id);
    }

    /// <summary>The fixture enemies of data/enemies/fixtures.json (P10.4), with their charts and tracks loaded.</summary>
    public static EnemySet LoadFixtureEnemies()
    {
        var charts = LoadCharts(LoadTracks());
        return Chiki.Sim.Data.EnemyLoader.SetFromJson(ReadData("enemies/fixtures.json"), charts);
    }

    /// <summary>The same definition with another damage per hit.</summary>
    public static EnemyDefinition WithDamage(EnemyDefinition enemy, int damagePerHit)
    {
        return new EnemyDefinition(
            enemy.Id,
            enemy.Name,
            enemy.Tier,
            enemy.Role,
            enemy.Profile,
            enemy.IntendedSeconds,
            enemy.Chart,
            damagePerHit,
            enemy.Abilities,
            enemy.Traits,
            enemy.StatusesUsed,
            enemy.PortraitId,
            enemy.QuoteLine,
            enemy.Phases,
            enemy.TrackId);
    }

    /// <summary>A left attack at every odd beat of a 32-beat track: 16 actions in 16 s, 60 per minute (the worked check of PRD 3.7.15).</summary>
    public static SimChart SixtyPerMinuteChart(SimTrack? track = null)
    {
        return Chart(track ?? Track(), Enumerable.Range(0, 16).Select(i => Chiki.Sim.Beats.ToQuarterBeats(2 * i + 1)).ToArray());
    }
}
