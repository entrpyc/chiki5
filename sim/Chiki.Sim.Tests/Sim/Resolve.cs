using Chiki.Sim;
using SimBattle = Chiki.Sim.Battle;
using Stats = Chiki.Sim.RunStats;

namespace Sim;

public class Resolve
{
    // BPM 120: beat n sits at n * 500 ms. Offsets: +0 Perfect, +60 Good, +120 Miss (P2.6).
    private const int Perfect = 0;
    private const int Good = 60;
    private const int Miss = 120;

    [Test]
    public void incoming_by_judgment()
    {
        // Enemy DMG 20, no Block; left attacks on beats 1, 3, 5 and 7.
        var stats = new Stats();
        var battle = TestContent.Battle(stats, TestContent.Chart(TestContent.Track(), 4, 12, 20, 28), enemyDmg: 20);
        battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 500 + Perfect);
        battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 1500 + Good);
        battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 2500 + Miss);

        battle.AdvanceToBeat(8);

        Assert.Multiple(() =>
        {
            Assert.That(battle.Events.OfType<DamageTaken>().Select(e => e.Amount), Is.EqualTo(new[] { 0, 10, 20, 20 }));
            Assert.That(stats.Ard, Is.EqualTo(300 - 50));
        });
    }

    [Test]
    public void block_absorbs_first()
    {
        // Perfect 6-Block Defense on beat 1 gives Block 6; Good against DMG 20 on beat 3 is 10 incoming.
        var stats = new Stats();
        var battle = TestContent.Battle(stats, TestContent.Chart(TestContent.Track(), 4, 12), enemyDmg: 20);
        battle.Press(TestContent.SlotO, TestContent.Defense(6), 500 + Perfect);
        battle.AdvanceToBeat(2);
        Assume.That(battle.Block, Is.EqualTo(6));
        battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 1500 + Good);

        battle.AdvanceToBeat(4);

        var taken = battle.Events.OfType<DamageTaken>().Last();
        Assert.Multiple(() =>
        {
            Assert.That(battle.Block, Is.EqualTo(0));
            Assert.That(taken.BlockAbsorbed, Is.EqualTo(6));
            Assert.That(taken.Amount, Is.EqualTo(4));
            Assert.That(stats.Ard, Is.EqualTo(300 - 4));
        });
    }

    [Test]
    public void effect_by_judgment()
    {
        var battle = TestContent.Battle(4, 12, 20);
        battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 500 + Perfect);
        battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 1500 + Good);
        battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 2500 + Miss);

        battle.AdvanceToBeat(6);

        Assert.That(battle.Events.OfType<DamageDealt>().Select(e => e.Amount), Is.EqualTo(new[] { 10, 5, 0 }));
    }

    [Test]
    public void defense_block_by_judgment()
    {
        var battle = TestContent.Battle(4);
        battle.Press(TestContent.SlotO, TestContent.Defense(8), 500 + Good);

        battle.AdvanceToBeat(2);

        Assert.Multiple(() =>
        {
            Assert.That(battle.Block, Is.EqualTo(4));
            Assert.That(battle.Events.OfType<BlockGained>().Single().Amount, Is.EqualTo(4));
        });
    }

    [Test]
    public void base_dmg_adds_flat()
    {
        var stats = new Stats { BaseDmg = 3 };
        var battle = TestContent.Battle(stats, TestContent.Chart(TestContent.Track(), 4, 12));
        battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 500 + Perfect);
        battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 1500 + Good);

        battle.AdvanceToBeat(4);

        // Perfect: 10 + 3 = 13. Good: 13 x 50% = 6.5, halves round up (P3.6) = 7.
        Assert.That(battle.Events.OfType<DamageDealt>().Select(e => e.Amount), Is.EqualTo(new[] { 13, 7 }));
    }

    [Test]
    public void base_dmg_ignores_defense()
    {
        var stats = new Stats { BaseDmg = 3 };
        var battle = TestContent.Battle(stats, TestContent.Chart(TestContent.Track(), 4));
        battle.Press(TestContent.SlotO, TestContent.Defense(8), 500 + Perfect);

        battle.AdvanceToBeat(2);

        Assert.That(battle.Block, Is.EqualTo(8));
    }

    [Test]
    public void wrong_side_whiffs()
    {
        var battle = TestContent.Battle(TestContent.Left(4));
        battle.Press(TestContent.SlotU, TestContent.RightAttack(10), 500 + Perfect);

        battle.AdvanceToBeat(2);

        Assert.Multiple(() =>
        {
            Assert.That(battle.Events.OfType<DamageDealt>().Single().Amount, Is.EqualTo(0));
            Assert.That(battle.EnemyHp, Is.EqualTo(battle.EnemyMaxHp));
        });
    }

    [Test]
    public void no_wrong_side_on_buff()
    {
        var battle = TestContent.Battle(TestContent.Buff(4));
        battle.Press(TestContent.SlotU, TestContent.RightAttack(10), 500 + Perfect);

        battle.AdvanceToBeat(2);

        Assert.Multiple(() =>
        {
            Assert.That(battle.Events.OfType<DamageDealt>().Single().Amount, Is.EqualTo(10));
            Assert.That(battle.EnemyHp, Is.EqualTo(battle.EnemyMaxHp - 10));
        });
    }

    [Test]
    public void attack_into_defend_reduced()
    {
        var battle = TestContent.Battle(TestContent.Defend(4, 500));
        battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 500 + Perfect);

        battle.AdvanceToBeat(2);

        Assert.That(battle.Events.OfType<DamageDealt>().Single().Amount, Is.EqualTo(5));
    }

    [Test]
    public void incoming_independent_of_category()
    {
        // The enemy attacks Left for 20 on beat 1; each play answers it with the given offset.
        var plays = new (string Name, Action<SimBattle, int> Play)[]
        {
            ("Defense", (b, t) => b.Press(TestContent.SlotO, TestContent.Defense(8), t)),
            ("wrong-side Attack", (b, t) => b.Press(TestContent.SlotU, TestContent.RightAttack(10), t)),
            ("Ability", (b, t) => b.Press(TestContent.SlotQ, TestContent.Ability(), t)),
            ("Signature send", (b, t) => b.Send(TestContent.SlotE, TestContent.LeftAttack10, t)),
        };

        Assert.Multiple(() =>
        {
            foreach (var (name, play) in plays)
            {
                Assert.That(ArdLossAfter(play, Perfect), Is.EqualTo(0), $"{name} with Perfect");
                Assert.That(ArdLossAfter(play, Good), Is.EqualTo(10), $"{name} with Good");
            }
        });
    }

    [Test]
    public void debuff_lands_on_perfect()
    {
        var stats = new Stats();
        var battle = TestContent.Battle(stats, TestContent.Chart(TestContent.Track(), TestContent.LeftApplying(4, TestContent.Weak(250))), enemyDmg: 20);
        battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 500 + Perfect);

        battle.AdvanceToBeat(2);

        Assert.Multiple(() =>
        {
            Assert.That(300 - stats.Ard, Is.EqualTo(0));
            Assert.That(battle.PlayerStatuses.Has(StatusKind.Weak), Is.True);
        });
    }

    [Test]
    public void immunity_blocks_status()
    {
        var battle = TestContent.Battle(new Stats(), TestContent.Chart(TestContent.Track(), TestContent.LeftApplying(4, TestContent.Weak(250))), enemyDmg: 20);
        battle.GrantImmunity(StatusTarget.Player, StatusKind.Weak);
        battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 500 + Perfect);

        battle.AdvanceToBeat(2);

        Assert.Multiple(() =>
        {
            Assert.That(battle.PlayerStatuses.Has(StatusKind.Weak), Is.False);
            Assert.That(battle.Events.OfType<StatusApplied>(), Is.Empty);
        });
    }

    [Test]
    public void true_dmg_ignores_block_and_reductions()
    {
        var battle = TestContent.Battle(4);
        battle.GrantBlock(StatusTarget.Enemy, 10);
        battle.ReduceEnemyDamageTaken(800, beats: 5);

        battle.DealTrueDamage(StatusTarget.Enemy, 7);

        Assert.Multiple(() =>
        {
            Assert.That(battle.EnemyHp, Is.EqualTo(battle.EnemyMaxHp - 7));
            Assert.That(battle.EnemyBlock, Is.EqualTo(10));
        });
    }

    [Test]
    public void rounds_to_nearest()
    {
        var battle = TestContent.Battle(4, 12);
        battle.Press(TestContent.SlotE, TestContent.LeftAttack(7), 500 + Good);
        battle.Press(TestContent.SlotE, TestContent.LeftAttack(9), 1500 + Good);

        battle.AdvanceToBeat(4);

        Assert.That(battle.Events.OfType<DamageDealt>().Select(e => e.Amount), Is.EqualTo(new[] { 4, 5 }));
    }

    private static int ArdLossAfter(Action<SimBattle, int> play, int offsetMs)
    {
        var stats = new Stats();
        var battle = TestContent.Battle(stats, TestContent.Chart(TestContent.Track(), TestContent.Left(4)), enemyDmg: 20);
        play(battle, 500 + offsetMs);
        battle.AdvanceToBeat(2);
        return 300 - stats.Ard;
    }
}
