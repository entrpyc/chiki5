using Chiki.Sim;
using Chiki.Sim.Data;
using SimBeats = Chiki.Sim.Beats;

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
            Assert.That(chart.Actions.Select(a => a.PositionQb), Is.EqualTo(source.Select(a => a["position"].AsThousandths() / ChartActionDocument.QuarterBeatThousandths)));
            Assert.That(chart.Actions.Select(a => ChartLoader.KindToId(a.Kind)), Is.EqualTo(source.Select(a => a["kind"].AsString())));
            Assert.That(chart.Actions.Select(a => a.DefenseLevel), Is.EqualTo(source.Select(a => a.Optional("defenseLevel")?.AsInt() ?? 0)));
            Assert.That(chart.Actions.Select(a => a.WindUpBeats), Is.EqualTo(source.Select(a => a.Optional("windUpBeats")?.AsInt() ?? 0)));
            Assert.That(chart.LengthBeats, Is.EqualTo(32));
            Assert.That(chart.ActionsPerMinute, Is.EqualTo(112));
        });
    }

    private static ChartDocument Document(params int[] positionsBeatThousandths)
    {
        return new ChartDocument(
            "chart-test",
            "enemy-test",
            "track-test",
            positionsBeatThousandths.Select(p => new ChartActionDocument(ChartValidator.KindAttackLeft, p)).ToArray());
    }

    [Test]
    public void rejects_off_grid_and_out_of_range()
    {
        var track = TestContent.Track(lengthBeats: 32);

        var violations = ChartValidator.Validate(Document(2_300, 40_000), track);
        var clean = ChartValidator.Validate(Document(2_250, 31_750), track);

        Assert.Multiple(() =>
        {
            Assert.That(violations, Has.Count.EqualTo(2), string.Join("\n", violations));
            Assert.That(violations.Select(v => v.ActionIndex), Is.EqualTo(new int?[] { 0, 1 }));
            Assert.That(violations[0].Message, Does.Contain("2.3"));
            Assert.That(violations[1].Message, Does.Contain("40"));
            Assert.That(clean, Is.Empty, string.Join("\n", clean));
        });
    }

    [Test]
    public void loops_from_start_seamlessly()
    {
        var battle = TestContent.Battle(4); // 32-beat chart, first action at beat 1
        battle.AdvanceToBeat(31);
        Assume.That(battle.Outcome, Is.Null);
        Assume.That(battle.Loop, Is.EqualTo(0));

        battle.AdvanceToBeat(32);

        Assert.Multiple(() =>
        {
            Assert.That(battle.Outcome, Is.Null);
            Assert.That(battle.Loop, Is.EqualTo(1));
            Assert.That(battle.Events.OfType<TrackLooped>().Single().PositionQb, Is.EqualTo(SimBeats.ToQuarterBeats(32)));
            Assert.That(battle.PendingAction.PositionQb, Is.EqualTo(SimBeats.ToQuarterBeats(33)));
            Assert.That(battle.PendingAction.Action, Is.SameAs(battle.Chart.Actions[0]));
            Assert.That(battle.UpcomingActions(2).Single().RemainingBeats, Is.EqualTo(1));
            Assert.That(battle.BeatMap.TimeAtBeat(32), Is.EqualTo(32 * 500));
            Assert.That(battle.BeatMap.TimeAtBeat(33), Is.EqualTo(battle.BeatMap.TimeAtBeat(32) + 500));
            Assert.That(battle.PendingAction.CentreMs, Is.EqualTo(battle.BeatMap.TimeAtBeat(33)));
        });
    }
}
