using Chiki.Sim;
using Chiki.Sim.Data;

namespace Sim;

public class Chart
{
    [Test]
    public void loads_and_derives()
    {
        var track = TrackLoader.FromJson(TestContent.ReadData("tracks/track-test-120.json"));
        string chartJson = TestContent.ReadData("charts/chart-test-thirty.json");
        var source = JsonValue.Parse(chartJson)["actions"].Items;

        var chart = ChartLoader.FromJson(chartJson, track);

        Assert.Multiple(() =>
        {
            Assert.That(chart.Actions, Has.Count.EqualTo(30));
            Assert.That(chart.Actions.Select(a => a.PositionQb), Is.EqualTo(source.Select(a => a["position"].AsInt())));
            Assert.That(chart.Actions.Select(a => ChartLoader.KindToId(a.Kind)), Is.EqualTo(source.Select(a => a["kind"].AsString())));
            Assert.That(chart.Actions.Select(a => a.DefenseLevel), Is.EqualTo(source.Select(a => a.Optional("defenseLevel")?.AsInt() ?? 0)));
            Assert.That(chart.Actions.Select(a => a.WindUpBeats), Is.EqualTo(source.Select(a => a.Optional("windUpBeats")?.AsInt() ?? 0)));
            Assert.That(chart.LengthBeats, Is.EqualTo(32));
            Assert.That(chart.ActionsPerMinute, Is.EqualTo(112));
        });
    }
}
