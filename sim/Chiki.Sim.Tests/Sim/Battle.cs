using Chiki.Sim;
using SimBattle = Chiki.Sim.Battle;
using Stats = Chiki.Sim.RunStats;

namespace Sim;

public class Battle
{
    [Test]
    public void exactly_one_enemy()
    {
        var enemy = TestContent.Enemy(TestContent.Track(), 4);

        Assert.Multiple(() =>
        {
            Assert.That(() => new SimBattle(new Stats(), new[] { enemy, enemy }), Throws.ArgumentException);
            Assert.That(() => new SimBattle(new Stats(), new[] { enemy }), Throws.Nothing);
        });
    }

    [Test]
    public void judgment_log_one_entry_per_enemy_action()
    {
        var battle = FiveActionBattleAnsweringFirstAndThird();

        battle.AdvanceToBeat(battle.Track.LengthBeats);

        Assert.Multiple(() =>
        {
            Assert.That(battle.JudgmentLog, Has.Count.EqualTo(5));
            Assert.That(battle.JudgmentLog.Count(e => e.Grade != null), Is.EqualTo(2));
            Assert.That(battle.JudgmentLog.Count(e => e.NoInput), Is.EqualTo(3));
        });
    }

    [Test]
    public void events_are_ordered()
    {
        var battle = FiveActionBattleAnsweringFirstAndThird();
        battle.AdvanceToBeat(battle.Track.LengthBeats);

        var events = battle.Events;

        Assert.Multiple(() =>
        {
            var positions = events.Select(e => e.PositionQb).ToArray();
            Assert.That(positions, Is.Ordered, "events are not in position order");

            var judged = events.OfType<InputJudged>().ToArray();
            Assert.That(judged, Has.Length.EqualTo(2));
            foreach (var judgment in judged)
            {
                int judgedAt = IndexOf(events, judgment);
                var taken = events.OfType<DamageTaken>().Single(d => d.ActionIndex == judgment.ActionIndex);
                Assert.That(IndexOf(events, taken), Is.GreaterThan(judgedAt), $"DamageTaken for action {judgment.ActionIndex} precedes its InputJudged");
            }
        });
    }

    /// <summary>Five left attacks on beats 1..5; beats 1 and 3 are answered on the beat.</summary>
    private static SimBattle FiveActionBattleAnsweringFirstAndThird()
    {
        var battle = TestContent.Battle(4, 8, 12, 16, 20);
        battle.Press(TestContent.SlotD, 500);
        battle.Press(TestContent.SlotD, 1500);
        return battle;
    }

    private static int IndexOf(IReadOnlyList<BattleEvent> events, BattleEvent target)
    {
        for (int i = 0; i < events.Count; i++)
        {
            if (ReferenceEquals(events[i], target))
            {
                return i;
            }
        }

        return -1;
    }
}
