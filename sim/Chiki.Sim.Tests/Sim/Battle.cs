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
            Assert.That(() => new SimBattle(new Stats(), new[] { enemy, enemy }, TestContent.DefaultEnemyHp, TestContent.Rng()), Throws.ArgumentException);
            Assert.That(() => new SimBattle(new Stats(), new[] { enemy }, TestContent.DefaultEnemyHp, TestContent.Rng()), Throws.Nothing);
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

    [Test]
    public void won_at_zero_hp()
    {
        // Enemy HP 10, left attacks on beats 1 and 3; a 10-damage Perfect on beat 1.
        var battle = TestContent.Battle(new Stats(), TestContent.Chart(TestContent.Track(), 4, 12), enemyHp: 10);
        battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 500);

        battle.AdvanceToBeat(2);
        int eventsAtEnd = battle.Events.Count;
        battle.AdvanceToBeat(3);

        Assert.Multiple(() =>
        {
            Assert.That(battle.EnemyHp, Is.EqualTo(0));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Won));
            Assert.That(battle.Events.OfType<BattleEnded>().Single().Outcome, Is.EqualTo(BattleOutcome.Won));
            Assert.That(battle.Events, Has.Count.EqualTo(eventsAtEnd), "the beat tick after the win was not a no-op");
            Assert.That(battle.Events.OfType<BeatStarted>().Select(b => b.Beat), Has.None.EqualTo(3));
        });
    }

    [Test]
    public void died_at_zero_ard()
    {
        var stats = new Stats { Ard = 15 };
        var battle = TestContent.Battle(stats, TestContent.Chart(TestContent.Track(), 4), enemyDmg: 20);

        battle.AdvanceToBeat(2);

        Assert.Multiple(() =>
        {
            Assert.That(stats.Ard, Is.EqualTo(0));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Died));
            Assert.That(battle.Events.OfType<BattleEnded>().Single().Outcome, Is.EqualTo(BattleOutcome.Died));
        });
    }

    [Test]
    public void perfect_defense_when_no_ard_lost()
    {
        var allPerfect = TwoAttacksWonByPlayer(firstOffsetMs: 0, enemyHp: 20);
        var oneGood = TwoAttacksWonByPlayer(firstOffsetMs: 60, enemyHp: 15);

        Assume.That(allPerfect.Outcome, Is.EqualTo(BattleOutcome.Won));
        Assume.That(oneGood.Outcome, Is.EqualTo(BattleOutcome.Won));
        Assert.Multiple(() =>
        {
            Assert.That(allPerfect.PerfectDefense, Is.True);
            Assert.That(allPerfect.Events.OfType<BattleEnded>().Single().PerfectDefense, Is.True);
            Assert.That(oneGood.PerfectDefense, Is.False);
            Assert.That(oneGood.Events.OfType<BattleEnded>().Single().PerfectDefense, Is.False);
        });
    }

    [Test]
    public void block_and_statuses_cleared_at_end()
    {
        // Enemy HP 10, left attacks on beats 1 and 3: a Perfect 12-Block Defense on beat 1, Bleed
        // applied on beat 3, then a 10-damage Perfect on beat 3 kills.
        var stats = new Stats();
        var battle = TestContent.Battle(stats, TestContent.Chart(TestContent.Track(), 4, 12), enemyHp: 10);
        battle.Press(TestContent.SlotO, TestContent.Defense(12), 500);
        battle.AdvanceToBeat(3);
        battle.ApplyStatus(StatusTarget.Player, StatusKind.Bleed);
        Assume.That(battle.Block, Is.EqualTo(12));
        battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 1500);
        battle.AdvanceToBeat(4);
        Assume.That(battle.Outcome, Is.EqualTo(BattleOutcome.Won));

        var next = TestContent.Battle(stats, TestContent.Chart(TestContent.Track(), 4));

        Assert.Multiple(() =>
        {
            Assert.That(battle.Block, Is.EqualTo(0));
            Assert.That(battle.PlayerStatuses.Any, Is.False);
            Assert.That(next.Block, Is.EqualTo(0));
            Assert.That(next.PlayerStatuses.Any, Is.False);
            Assert.That(next.EnemyStatuses.Any, Is.False);
        });
    }

    /// <summary>
    /// Left attacks on beats 1 and 3, each answered by a 10-damage Left Attack; the first with the
    /// given offset, the second Perfect. The enemy HP is chosen so the second attack wins.
    /// </summary>
    private static SimBattle TwoAttacksWonByPlayer(int firstOffsetMs, int enemyHp)
    {
        var battle = TestContent.Battle(new Stats(), TestContent.Chart(TestContent.Track(), 4, 12), enemyHp: enemyHp);
        battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 500 + firstOffsetMs);
        battle.Press(TestContent.SlotR, TestContent.LeftAttack10, 1500);
        battle.AdvanceToBeat(4);
        return battle;
    }

    /// <summary>Five left attacks on beats 1..5; beats 1 and 3 are answered on the beat.</summary>
    private static SimBattle FiveActionBattleAnsweringFirstAndThird()
    {
        var battle = TestContent.Battle(4, 8, 12, 16, 20);
        battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 500);
        battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 1500);
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
