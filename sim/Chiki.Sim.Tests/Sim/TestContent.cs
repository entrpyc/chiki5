using Chiki.Sim;
using SimBattle = Chiki.Sim.Battle;
using SimChart = Chiki.Sim.Chart;
using SimTrack = Chiki.Sim.Track;
using Stats = Chiki.Sim.RunStats;

namespace Sim;

/// <summary>Builders for the content the Phase 2 tests run against, and access to data/ fixtures.</summary>
internal static class TestContent
{
    public static readonly Slot SlotD = new(0, SlotKey.D);
    public static readonly Slot SlotJ = new(0, SlotKey.J);

    public static SimTrack Track(int bpm = 120, int lengthBeats = 32, int offsetMs = 0, params TempoChange[] changes)
    {
        return new SimTrack("track-test", 1, EncounterTier.Normal, lengthBeats, offsetMs, new TempoMap(bpm, changes));
    }

    /// <summary>A chart of left attacks at the given quarter-beat positions.</summary>
    public static SimChart Chart(SimTrack track, params int[] positionsQb)
    {
        var actions = positionsQb.Select(p => new EnemyAction(EnemyActionKind.AttackLeft, p)).ToList();
        return new SimChart("chart-test", "enemy-test", track, actions);
    }

    public static EnemyDefinition Enemy(SimChart chart)
    {
        return new EnemyDefinition("enemy-test", "Test Enemy", chart, 10);
    }

    public static EnemyDefinition Enemy(SimTrack track, params int[] positionsQb)
    {
        return Enemy(Chart(track, positionsQb));
    }

    /// <summary>A battle on a BPM 120, 32-beat track with left attacks at the given positions.</summary>
    public static SimBattle Battle(params int[] positionsQb)
    {
        return new SimBattle(new Stats(), Enemy(Track(), positionsQb));
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
