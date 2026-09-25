using Chiki.Sim;

namespace Sim;

public class Beats
{
    [Test]
    public void second_press_same_action_rejected()
    {
        var battle = TestContent.Battle(4); // one action at beat 1 = 500 ms
        var first = battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 500);
        Assume.That(first.Accepted, Is.True);

        var second = battle.Press(TestContent.SlotU, TestContent.RightAttack(10), 520);

        Assert.Multiple(() =>
        {
            Assert.That(second.Rejected, Is.True);
            Assert.That(second.Outcome, Is.EqualTo(PressOutcome.ActionAlreadyAnswered));
            Assert.That(second.Wasted, Is.True, "the second press was not wasted");
            Assert.That(second.Grade, Is.EqualTo(Chiki.Sim.Judgment.Miss), "a wasted press is a Miss (PRD 3.3.5.5)");
            Assert.That(battle.JudgmentLog.Count(e => e.Grade != null), Is.EqualTo(0), "the action has not resolved yet");
        });
    }

    [Test]
    public void press_between_actions_rejected()
    {
        var battle = TestContent.Battle(4, 12); // actions at 500 ms and 1500 ms

        var result = battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 1000);

        Assert.Multiple(() =>
        {
            Assert.That(result.Rejected, Is.True);
            Assert.That(result.Outcome, Is.EqualTo(PressOutcome.NoOpenWindow));
            Assert.That(result.Wasted, Is.True);
            Assert.That(result.Grade, Is.EqualTo(Chiki.Sim.Judgment.Miss), "a wasted press is a Miss (PRD 3.3.5.5)");
            Assert.That(battle.JudgmentLog.Where(e => e.Grade != null), Is.Empty, "a wasted press answers no action");
        });
    }

    [Test]
    public void wasted_press_starts_the_cooldown()
    {
        var battle = TestContent.Battle(4, 12); // actions at 500 ms and 1500 ms
        var card = TestContent.LeftAttack(10, cooldownBeats: 4);

        var result = battle.Press(TestContent.SlotE, card, 1000); // between the two windows

        var judged = battle.Events.OfType<InputJudged>().Single();
        Assert.Multiple(() =>
        {
            Assert.That(result.Wasted, Is.True);
            Assert.That(battle.CooldownOf(TestContent.SlotE), Is.EqualTo(4), "the wasted press did not burn the cooldown");
            Assert.That(judged.Grade, Is.EqualTo(Chiki.Sim.Judgment.Miss));
            Assert.That(judged.ActionIndex, Is.EqualTo(InputJudged.NoAction), "the press answered no action");
            Assert.That(battle.Events.OfType<CooldownStarted>().Single().Beats, Is.EqualTo(4));
        });
    }

    [Test]
    public void wasted_press_applies_nothing()
    {
        var battle = TestContent.Battle(4, 12); // actions at 500 ms and 1500 ms
        int hpBefore = battle.EnemyHp;

        battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 1000);
        battle.AdvanceToBeat(4); // past the first window, which now closes with no input

        Assert.Multiple(() =>
        {
            Assert.That(battle.EnemyHp, Is.EqualTo(hpBefore), "the wasted card dealt damage");
            Assert.That(battle.JudgmentLog.Single(e => e.ActionIndex == 0).NoInput, Is.True, "the wasted press answered the next action");
        });
    }

    [Test]
    public void wasted_send_banks_nothing()
    {
        var battle = TestContent.Battle(4, 12); // actions at 500 ms and 1500 ms

        var result = battle.Send(TestContent.SlotE, TestContent.LeftAttack10, 1000);

        Assert.Multiple(() =>
        {
            Assert.That(result.Wasted, Is.True);
            Assert.That(battle.SignatureChain, Is.Empty, "a wasted send banked its card");
            Assert.That(battle.CooldownOf(TestContent.SlotE), Is.EqualTo(Tuning.CooldownMinBeats));
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
