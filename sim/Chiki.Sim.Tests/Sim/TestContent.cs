using Chiki.Sim;
using SimBattle = Chiki.Sim.Battle;
using SimChart = Chiki.Sim.Chart;
using SimTrack = Chiki.Sim.Track;
using Stats = Chiki.Sim.RunStats;

namespace Sim;

/// <summary>Builders for the content the simulation tests run against, and access to data/ fixtures.</summary>
internal static class TestContent
{
    public const int DefaultEnemyDmg = 10;
    public const int DefaultEnemyHp = 1000;

    public static readonly Slot SlotA = new(0, SlotKey.A);
    public static readonly Slot SlotD = new(0, SlotKey.D);
    public static readonly Slot SlotJ = new(0, SlotKey.J);
    public static readonly Slot SlotL = new(0, SlotKey.L);

    /// <summary>A 10-damage Left Attack card for slot D.</summary>
    public static readonly CardDefinition LeftAttack10 = LeftAttack(10);

    public static CardDefinition LeftAttack(int damage) => new("card-left-" + damage, "Left " + damage, CardCategory.LeftAttack, damage);

    public static CardDefinition RightAttack(int damage) => new("card-right-" + damage, "Right " + damage, CardCategory.RightAttack, damage);

    public static CardDefinition Defense(int block) => new("card-defense-" + block, "Guard " + block, CardCategory.Defense, block);

    public static CardDefinition Ability() => new("card-ability", "Ability", CardCategory.Ability, 0);

    public static SimTrack Track(int bpm = 120, int lengthBeats = 32, int offsetMs = 0, params TempoChange[] changes)
    {
        return new SimTrack("track-test", 1, EncounterTier.Normal, lengthBeats, offsetMs, new TempoMap(bpm, changes));
    }

    public static EnemyAction Left(int positionQb) => new(EnemyActionKind.AttackLeft, positionQb);

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

    public static EnemyDefinition Enemy(SimChart chart, int damagePerHit = DefaultEnemyDmg)
    {
        return new EnemyDefinition("enemy-test", "Test Enemy", chart, damagePerHit);
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

    public static SimBattle Battle(Stats stats, SimChart chart, int enemyDmg = DefaultEnemyDmg, int enemyHp = DefaultEnemyHp)
    {
        return new SimBattle(stats, Enemy(chart, enemyDmg), enemyHp);
    }

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
}
