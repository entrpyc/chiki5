using Chiki.Sim;
using SimBeats = Chiki.Sim.Beats;
using Stats = Chiki.Sim.RunStats;

namespace Sim;

public class Powers
{
    // BPM 120: beat n sits at n * 500 ms. Offsets: +0 Perfect, +60 Good, +100 Miss (P2.6).
    private const int Perfect = 0;
    private const int Good = 60;
    private const int Miss = 100;

    private static int Ms(int beat) => beat * 500;

    private static int[] ArdLossByAction(Chiki.Sim.Battle battle, int actions)
    {
        return Enumerable.Range(0, actions)
            .Select(i => battle.Events.OfType<DamageTaken>().Where(e => e.ActionIndex == i).Sum(e => e.Amount))
            .ToArray();
    }

    [Test]
    public void rising_tempo_adds_3_per_hit()
    {
        // Attacks on beats 1, 2 and 3; the first two land unanswered, the third is Missed.
        var chart = TestContent.Chart(TestContent.Track(), 4, 8, 12);
        var battle = TestContent.BattleWith(TestContent.EnemyWith(chart, EnemyAbility.RisingTempo, damagePerHit: 10));

        battle.Press(TestContent.SlotD, TestContent.LeftAttack10, Ms(3) + Miss);
        battle.AdvanceToBeat(4);

        Assert.That(ArdLossByAction(battle, 3), Is.EqualTo(new[] { 10, 13, 16 }));
    }

    [Test]
    public void misstep_pain_by_grade()
    {
        var chart = TestContent.Chart(TestContent.Track(), TestContent.Buff(4), TestContent.Buff(8), TestContent.Buff(12));
        var battle = TestContent.BattleWith(TestContent.EnemyWith(chart, EnemyAbility.MisstepPain));

        battle.Press(TestContent.SlotD, TestContent.LeftAttack10, Ms(1) + Good);
        battle.Press(TestContent.SlotF, TestContent.LeftAttack10, Ms(2) + Miss);
        battle.AdvanceToBeat(4);

        Assert.That(ArdLossByAction(battle, 3), Is.EqualTo(new[] { 5, 10, 0 }));
    }

    [Test]
    public void pressure_doubles_after_no_input()
    {
        // A Buff on beat 1 goes unanswered; attacks on beats 2 and 3 are both Missed.
        var chart = TestContent.Chart(TestContent.Track(), TestContent.Buff(4), TestContent.Left(8), TestContent.Left(12));
        var battle = TestContent.BattleWith(TestContent.EnemyWith(chart, EnemyAbility.Pressure, damagePerHit: 10));

        battle.Press(TestContent.SlotD, TestContent.LeftAttack10, Ms(2) + Miss);
        battle.Press(TestContent.SlotF, TestContent.LeftAttack10, Ms(3) + Miss);
        battle.AdvanceToBeat(4);

        Assert.That(ArdLossByAction(battle, 3), Is.EqualTo(new[] { 0, 20, 10 }));
    }

    [Test]
    public void iron_veil_reduces_80_for_5_beats()
    {
        // The enemy Buffs on beat 2 (Iron Veil), then attacks on beats 4 and 8.
        var chart = TestContent.Chart(TestContent.Track(), TestContent.Buff(8), TestContent.Left(16), TestContent.Left(32));
        var battle = TestContent.BattleWith(TestContent.EnemyWith(chart, EnemyAbility.IronVeil));

        battle.AdvanceToBeat(3);
        bool activeAfterBuff = battle.IronVeilActive;
        battle.Press(TestContent.SlotD, TestContent.LeftAttack10, Ms(4) + Perfect);
        battle.Press(TestContent.SlotF, TestContent.LeftAttack10, Ms(8) + Perfect);
        battle.AdvanceToBeat(9);

        Assert.Multiple(() =>
        {
            Assert.That(activeAfterBuff, Is.True);
            Assert.That(battle.IronVeilActive, Is.False);
            Assert.That(battle.Events.OfType<DamageDealt>().Select(e => e.Amount), Is.EqualTo(new[] { 2, 10 }));
        });
    }

    [Test]
    public void charge_telegraphs_then_hits_double()
    {
        // Charge(4) starts at beat 2 and lands at beat 6.
        var chart = TestContent.Chart(TestContent.Track(), TestContent.Charge(8, 4));
        var battle = TestContent.BattleWith(TestContent.EnemyWith(chart, EnemyAbility.ChargeBuff, damagePerHit: 10));
        battle.AdvanceToBeat(2);

        var upcoming = battle.UpcomingActions(8).Single();
        battle.Press(TestContent.SlotD, TestContent.LeftAttack10, Ms(6) + Miss);
        battle.AdvanceToBeat(7);

        Assert.Multiple(() =>
        {
            Assert.That(upcoming.Action.Kind, Is.EqualTo(EnemyActionKind.Charge));
            Assert.That(upcoming.Beat, Is.EqualTo(6));
            Assert.That(upcoming.RemainingBeats, Is.EqualTo(4));
            Assert.That(upcoming.WindUpBeats, Is.EqualTo(4));
            Assert.That(upcoming.TelegraphQb, Is.EqualTo(SimBeats.ToQuarterBeats(2)));
            Assert.That(ArdLossByAction(battle, 1), Is.EqualTo(new[] { 20 }));
        });
    }

    [Test]
    public void stoneform_after_three_quiet_beats()
    {
        var chart = TestContent.Chart(TestContent.Track(), 8, 24);

        var quiet = TestContent.BattleWith(TestContent.EnemyWith(chart, trait: EnemyTrait.Stoneform));
        quiet.AdvanceToBeat(4);

        var hit = TestContent.BattleWith(TestContent.EnemyWith(chart, trait: EnemyTrait.Stoneform));
        hit.Press(TestContent.SlotD, TestContent.LeftAttack10, Ms(2) + Perfect);
        hit.AdvanceToBeat(4);

        Assert.Multiple(() =>
        {
            Assert.That(quiet.EnemyBlock, Is.EqualTo(10));
            Assert.That(hit.EnemyBlock, Is.EqualTo(0));
        });
    }

    [Test]
    public void guard_starts_with_30_block()
    {
        var chart = TestContent.Chart(TestContent.Track(), 4);
        var battle = TestContent.BattleWith(TestContent.EnemyWith(chart, trait: EnemyTrait.Guard), enemyHp: 100);
        int blockAtStart = battle.EnemyBlock;

        battle.Press(TestContent.SlotD, TestContent.LeftAttack(40), Ms(1) + Perfect);
        battle.AdvanceToBeat(2);

        Assert.Multiple(() =>
        {
            Assert.That(blockAtStart, Is.EqualTo(30));
            Assert.That(battle.EnemyBlock, Is.EqualTo(0));
            Assert.That(battle.EnemyHp, Is.EqualTo(90));
        });
    }
}
