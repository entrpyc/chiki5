using Chiki.Sim;
using SimBattle = Chiki.Sim.Battle;
using SimJudgment = Chiki.Sim.Judgment;
using Stats = Chiki.Sim.RunStats;

namespace Sim;

public class Judgment
{
    [Test]
    public void perfect_good_miss_by_offset()
    {
        // BPM 120: actions on beats 1, 3 and 5 (500, 1500, 2500 ms)
        var battle = TestContent.Battle(4, 12, 20);

        var plus20 = battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 520);
        var plus60 = battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 1560);
        var plus120 = battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 2620);

        Assert.Multiple(() =>
        {
            Assert.That(plus20.Grade, Is.EqualTo(SimJudgment.Perfect));
            Assert.That(plus60.Grade, Is.EqualTo(SimJudgment.Good));
            Assert.That(plus120.Grade, Is.EqualTo(SimJudgment.Miss));
        });
    }

    [Test]
    public void miss_only_from_press()
    {
        var battle = TestContent.Battle(4); // action at 500 ms, window closes before beat 2

        battle.AdvanceToBeat(2);

        Assert.Multiple(() =>
        {
            Assert.That(battle.Events.OfType<InputJudged>(), Is.Empty);
            Assert.That(battle.JudgmentLog.Select(e => e.Grade), Has.All.Null);
        });
    }

    [Test]
    public void no_input_starts_nothing()
    {
        var battle = TestContent.Battle(4); // action at 500 ms, window closes before beat 2

        battle.AdvanceToBeat(2);

        Assert.Multiple(() =>
        {
            Assert.That(Slot.All, Has.Count.EqualTo(16));
            Assert.That(Slot.All.Select(battle.CooldownOf), Has.All.EqualTo(0));
            Assert.That(battle.Events.OfType<CooldownStarted>(), Is.Empty);
            Assert.That(battle.JudgmentLog.Single().NoInput, Is.True);
        });
    }

    [Test]
    public void no_input_takes_full_damage()
    {
        var stats = new Stats();
        var battle = TestContent.Battle(stats, TestContent.Chart(TestContent.Track(), 4), enemyDmg: 12);

        battle.AdvanceToBeat(2);

        Assert.That(300 - stats.Ard, Is.EqualTo(12));
    }

    [Test]
    public void windows_scale_with_bpm()
    {
        // At BPM 240 a beat is 250 ms, so beat 2 sits at 500 ms; at BPM 120 beat 1 does.
        var fast = new SimBattle(new Stats(), TestContent.Enemy(TestContent.Track(bpm: 240), 8), TestContent.DefaultEnemyHp, TestContent.Rng());
        var slow = new SimBattle(new Stats(), TestContent.Enemy(TestContent.Track(bpm: 120), 4), TestContent.DefaultEnemyHp, TestContent.Rng());

        var atFast = fast.Press(TestContent.SlotE, TestContent.LeftAttack10, 530);
        var atSlow = slow.Press(TestContent.SlotE, TestContent.LeftAttack10, 530);

        Assert.Multiple(() =>
        {
            Assert.That(atFast.Grade, Is.EqualTo(SimJudgment.Good));
            Assert.That(atSlow.Grade, Is.EqualTo(SimJudgment.Perfect));
        });
    }
}
