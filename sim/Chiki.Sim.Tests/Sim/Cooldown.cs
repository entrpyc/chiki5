using Chiki.Sim;
using SimJudgment = Chiki.Sim.Judgment;

namespace Sim;

public class Cooldown
{
    // BPM 120: beat n sits at n * 500 ms. Offsets: +0 Perfect, +120 Miss (P2.6).
    private const int Perfect = 0;
    private const int Miss = 120;

    [Test]
    public void missed_card_still_cools()
    {
        // One left attack on beat 1; the press lands a Miss after beat 1 has started.
        var battle = TestContent.Battle(4);

        var press = battle.Press(TestContent.SlotD, TestContent.LeftAttack(10, cooldownBeats: 4), 500 + Miss);
        Assume.That(press.Grade, Is.EqualTo(SimJudgment.Miss));
        int atPress = battle.CooldownOf(TestContent.SlotD);
        battle.AdvanceToBeat(4);
        int afterThreeTicks = battle.CooldownOf(TestContent.SlotD);
        battle.AdvanceToBeat(5);

        Assert.Multiple(() =>
        {
            Assert.That(atPress, Is.EqualTo(4));
            Assert.That(afterThreeTicks, Is.EqualTo(1));
            Assert.That(battle.CooldownOf(TestContent.SlotD), Is.EqualTo(0));
        });
    }

    [Test]
    public void slots_independent_across_lines()
    {
        var battle = TestContent.Battle(4);

        battle.Press(TestContent.SlotD, TestContent.LeftAttack(10, cooldownBeats: 4), 500 + Perfect);

        Assume.That(battle.CooldownOf(TestContent.SlotD), Is.EqualTo(4));
        Assert.That(battle.CooldownOf(TestContent.SlotDLine2), Is.EqualTo(0));
    }

    [Test]
    public void disabled_press_is_not_a_miss()
    {
        // Left attacks on beats 1 and 2; a cooldown-3 card played on beat 1 leaves slot D with 2 beats on beat 2.
        var battle = TestContent.Battle(4, 8);
        battle.Press(TestContent.SlotD, TestContent.LeftAttack(10, cooldownBeats: 3), 500 + Perfect);
        battle.AdvanceToBeat(2);
        Assume.That(battle.CooldownOf(TestContent.SlotD), Is.EqualTo(2));
        int judgedBefore = battle.Events.OfType<InputJudged>().Count();
        int logBefore = battle.JudgmentLog.Count;

        var disabled = battle.Press(TestContent.SlotD, TestContent.LeftAttack10, 1000 + Perfect);
        var other = battle.Press(TestContent.SlotF, TestContent.LeftAttack10, 1000 + Perfect);
        battle.AdvanceToBeat(3);

        Assert.Multiple(() =>
        {
            Assert.That(disabled.Outcome, Is.EqualTo(PressOutcome.Disabled));
            Assert.That(disabled.CooldownRemainingBeats, Is.EqualTo(2));
            Assert.That(battle.Events.OfType<SlotDisabled>().Single().Slot, Is.EqualTo(TestContent.SlotD));
            Assert.That(battle.Events.OfType<InputJudged>().Count(), Is.EqualTo(judgedBefore + 1), "the disabled press must not be judged");
            Assert.That(battle.JudgmentLog, Has.Count.EqualTo(logBefore + 1));
            Assert.That(battle.JudgmentLog.Last().Slot, Is.EqualTo(TestContent.SlotF), "the disabled press must not appear in the log");
            Assert.That(other.Accepted, Is.True, "another slot must still be playable this beat");
        });
    }
}
