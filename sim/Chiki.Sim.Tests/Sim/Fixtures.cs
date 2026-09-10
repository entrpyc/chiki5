using Chiki.Sim;
using Chiki.Sim.Data;
using SimChart = Chiki.Sim.Chart;
using SimTrack = Chiki.Sim.Track;

namespace Sim;

public class Fixtures
{
    [Test]
    public void starter_set_validates_and_fills_two_lines()
    {
        var set = CardLoader.SetFromJson(TestContent.ReadData("sets/starter.json"));
        Assume.That(() => TrackLoader.FromJson(TestContent.ReadData("tracks/fixture-120.json")), Throws.Nothing);

        var violations = CardValidator.Validate(set);
        var perCategory = set.Cards.GroupBy(c => c.Category).ToDictionary(g => g.Key, g => g.Count());

        Assert.Multiple(() =>
        {
            Assert.That(violations, Is.Empty, string.Join("\n", violations));
            foreach (CardCategory category in Enum.GetValues(typeof(CardCategory)))
            {
                Assert.That(perCategory.GetValueOrDefault(category), Is.GreaterThanOrEqualTo(4), category.ToString());
            }
        });
    }

    /// <summary>Every shipped chart and enemy set under data/ obeys its rules (PRD 3.6.31, 3.6).</summary>
    [Test]
    public void shipped_charts_and_enemies_validate()
    {
        string dataRoot = Path.Combine(TestContent.RepoRoot, "data");
        var tracks = Directory.GetFiles(Path.Combine(dataRoot, "tracks"), "*.json")
            .Select(f => TrackLoader.FromJson(File.ReadAllText(f)))
            .ToDictionary(t => t.Id);

        var chartViolations = new List<ChartViolation>();
        var charts = new Dictionary<string, SimChart>();
        foreach (var file in Directory.GetFiles(Path.Combine(dataRoot, "charts"), "*.json"))
        {
            var document = ChartLoader.Read(File.ReadAllText(file));
            Assert.That(tracks, Does.ContainKey(document.TrackId), $"{Path.GetFileName(file)} names an unknown track");
            var violations = ChartValidator.Validate(document, tracks[document.TrackId]);
            chartViolations.AddRange(violations);
            if (violations.Count == 0)
            {
                charts[document.Id] = ChartLoader.Build(document, tracks[document.TrackId]);
            }
        }

        var enemyViolations = new List<EnemyViolation>();
        foreach (var file in Directory.GetFiles(Path.Combine(dataRoot, "enemies"), "*.json"))
        {
            var set = EnemyLoader.SetFromJson(File.ReadAllText(file), charts);
            enemyViolations.AddRange(EnemyValidator.Validate(set));
        }

        Assert.Multiple(() =>
        {
            Assert.That(chartViolations, Is.Empty, string.Join("\n", chartViolations));
            Assert.That(enemyViolations, Is.Empty, string.Join("\n", enemyViolations));
        });
    }
}
