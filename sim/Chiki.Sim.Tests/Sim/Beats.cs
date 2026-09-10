using Chiki.Sim;

namespace Sim;

public class Beats
{
    [Test]
    public void second_press_same_action_rejected()
    {
        var battle = TestContent.Battle(4); // one action at beat 1 = 500 ms
        var first = battle.Press(TestContent.SlotD, 500);
        Assume.That(first.Accepted, Is.True);

        var second = battle.Press(TestContent.SlotJ, 520);

        Assert.Multiple(() =>
        {
            Assert.That(second.Rejected, Is.True);
            Assert.That(second.Outcome, Is.EqualTo(PressOutcome.ActionAlreadyAnswered));
            Assert.That(battle.Events.OfType<InputJudged>().Count(), Is.EqualTo(1), "the second press consumed something");
        });
    }

    [Test]
    public void press_between_actions_rejected()
    {
        var battle = TestContent.Battle(4, 12); // actions at 500 ms and 1500 ms

        var result = battle.Press(TestContent.SlotD, 1000);

        Assert.Multiple(() =>
        {
            Assert.That(result.Rejected, Is.True);
            Assert.That(result.Outcome, Is.EqualTo(PressOutcome.NoOpenWindow));
            Assert.That(battle.Events.OfType<InputJudged>(), Is.Empty);
            Assert.That(battle.JudgmentLog.Where(e => e.Grade != null), Is.Empty, "a judgment was recorded");
        });
    }

    [Test]
    public void durations_tick_per_beat()
    {
        var battle = TestContent.Battle(4);
        var timer = battle.StartTimer(3);

        battle.AdvanceToBeat(2);
        bool expiredAfterTwo = timer.IsExpired;
        battle.AdvanceToBeat(3);
        bool expiredAfterThree = timer.IsExpired;

        Assert.Multiple(() =>
        {
            Assert.That(expiredAfterTwo, Is.False);
            Assert.That(expiredAfterThree, Is.True);
        });
    }

    [Test]
    public void opportunities_equal_chart_actions()
    {
        var battle = TestContent.Battle(4, 10, 17); // 1, 2.5 and 4.25 beats

        var opportunities = battle.Opportunities;

        Assert.Multiple(() =>
        {
            Assert.That(opportunities, Has.Count.EqualTo(3));
            Assert.That(opportunities.Select(o => o.PositionQb), Is.EqualTo(new[] { 4, 10, 17 }));
            Assert.That(opportunities.Select(o => o.CentreMs), Is.EqualTo(new[] { 500, 1250, 2125 }));
            Assert.That(opportunities.Select(o => o.PositionQb), Has.None.EqualTo(12).And.None.EqualTo(20));
        });
    }
}
