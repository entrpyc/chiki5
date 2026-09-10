using Chiki.Sim;
using SimBattle = Chiki.Sim.Battle;
using SimRng = Chiki.Sim.Rng;
using Stats = Chiki.Sim.RunStats;

namespace Sim;

public class Status
{
    // BPM 120: beat n sits at n * 500 ms. Offsets: +0 Perfect, +120 Miss (P2.6).
    private const int Perfect = 0;
    private const int Miss = 120;

    private const int Weak25 = 250;

    [Test]
    public void stacks_add_and_refresh_resets()
    {
        // Bleed 2 on beat 0 lasts 8; five beat ends later 3 remain.
        var battle = TestContent.Battle(4);
        battle.ApplyStatus(StatusTarget.Enemy, StatusKind.Bleed, stacks: 2);
        battle.AdvanceToBeat(5);
        Assume.That(battle.EnemyStatuses.Stacks(StatusKind.Bleed), Is.EqualTo(2));
        Assume.That(battle.EnemyStatuses.RemainingBeats(StatusKind.Bleed), Is.EqualTo(3));

        battle.ApplyStatus(StatusTarget.Enemy, StatusKind.Bleed, stacks: 1);

        Assert.Multiple(() =>
        {
            Assert.That(battle.EnemyStatuses.Stacks(StatusKind.Bleed), Is.EqualTo(3));
            Assert.That(battle.EnemyStatuses.RemainingBeats(StatusKind.Bleed), Is.EqualTo(8));
        });
    }

    [Test]
    public void resolves_after_actions()
    {
        // Enemy HP 10 with Bleed; a 10-damage Perfect on beat 1 kills it before beat 1 ends.
        var battle = TestContent.Battle(new Stats(), TestContent.Chart(TestContent.Track(), 4), enemyHp: 10);
        battle.AdvanceToBeat(1);
        battle.ApplyStatus(StatusTarget.Enemy, StatusKind.Bleed);
        battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 500 + Perfect);

        battle.AdvanceToBeat(3);

        var events = battle.Events.ToList();
        int endedAt = events.FindIndex(e => e is BattleEnded);
        var bleedTicks = events
            .Select((e, i) => (Event: e, Index: i))
            .Where(x => x.Event is StatusTriggered { Kind: StatusKind.Bleed })
            .Select(x => x.Index);
        Assert.Multiple(() =>
        {
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Won));
            Assert.That(endedAt, Is.GreaterThan(events.FindIndex(e => e is DamageDealt)));
            Assert.That(bleedTicks, Has.All.GreaterThan(endedAt), "a Bleed tick was recorded before the kill");
        });
    }

    [Test]
    public void priority_order()
    {
        // The enemy attacks on beat 1: the Stunned player gives no input, Weak on the enemy
        // reduces the hit, Thorns on the player answers it, and Bleed on the enemy ticks at beat end.
        var battle = TestContent.Battle(4);
        battle.AdvanceToBeat(1);
        battle.ApplyStatus(StatusTarget.Enemy, StatusKind.Bleed);
        battle.ApplyStatus(StatusTarget.Player, StatusKind.Thorns, value: 5);
        battle.ApplyStatus(StatusTarget.Enemy, StatusKind.Weak, value: Weak25);
        battle.ApplyStatus(StatusTarget.Player, StatusKind.Stun);

        battle.AdvanceToBeat(2);

        Assert.That(
            battle.Events.OfType<StatusTriggered>().Select(e => e.Kind),
            Is.EqualTo(new[] { StatusKind.Stun, StatusKind.Weak, StatusKind.Thorns, StatusKind.Bleed }));
    }

    [Test]
    public void scar_doubles_by_seeded_roll()
    {
        ulong lowSeed = SeedWhoseFirstRoll(roll => roll < 100);
        ulong highSeed = SeedWhoseFirstRoll(roll => roll >= 100);

        Assert.Multiple(() =>
        {
            Assert.That(PerfectHitWithScar5(lowSeed), Is.EqualTo(20));
            Assert.That(PerfectHitWithScar5(highSeed), Is.EqualTo(10));
        });
    }

    [Test]
    public void scar_stack_lasts_10_beats()
    {
        var battle = TestContent.Battle(4);
        battle.ApplyStatus(StatusTarget.Enemy, StatusKind.Scar);

        battle.AdvanceToBeat(9);
        bool afterNine = battle.EnemyStatuses.Has(StatusKind.Scar);
        battle.AdvanceToBeat(10);

        Assert.Multiple(() =>
        {
            Assert.That(afterNine, Is.True);
            Assert.That(battle.EnemyStatuses.Has(StatusKind.Scar), Is.False);
        });
    }

    [Test]
    public void weak_reduces_enemy_damage()
    {
        var stats = new Stats();
        var battle = TestContent.Battle(stats, TestContent.Chart(TestContent.Track(), 4), enemyDmg: 20);
        battle.ApplyStatus(StatusTarget.Enemy, StatusKind.Weak, value: Weak25);
        battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 500 + Miss);

        battle.AdvanceToBeat(2);

        Assert.That(300 - stats.Ard, Is.EqualTo(15));
    }

    [Test]
    public void weak_expires_after_8()
    {
        var battle = TestContent.Battle(4);
        battle.ApplyStatus(StatusTarget.Player, StatusKind.Weak, value: Weak25);

        battle.AdvanceToBeat(7);
        bool afterSeven = battle.PlayerStatuses.Has(StatusKind.Weak);
        battle.AdvanceToBeat(8);

        Assert.Multiple(() =>
        {
            Assert.That(afterSeven, Is.True);
            Assert.That(battle.PlayerStatuses.Has(StatusKind.Weak), Is.False);
        });
    }

    [Test]
    public void stunned_enemy_skips_action()
    {
        var battle = TestContent.Battle(4);
        battle.ApplyStatus(StatusTarget.Enemy, StatusKind.Stun);

        battle.AdvanceToBeat(2);

        Assert.Multiple(() =>
        {
            Assert.That(battle.Events.OfType<DamageTaken>(), Is.Empty);
            Assert.That(battle.EnemyStatuses.Has(StatusKind.Stun), Is.False);
        });
    }

    [Test]
    public void stunned_player_action_is_no_input()
    {
        var stats = new Stats();
        var battle = TestContent.Battle(stats, TestContent.Chart(TestContent.Track(), 4), enemyDmg: 12);
        battle.ApplyStatus(StatusTarget.Player, StatusKind.Stun);

        var press = battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 500 + Perfect);
        battle.AdvanceToBeat(2);

        Assert.Multiple(() =>
        {
            Assert.That(press.Accepted, Is.False);
            Assert.That(battle.JudgmentLog.Single().NoInput, Is.True);
            Assert.That(Slot.All.Select(battle.CooldownOf), Has.All.EqualTo(0));
            Assert.That(300 - stats.Ard, Is.EqualTo(12));
        });
    }

    [Test]
    public void bleed_ticks_per_stack()
    {
        var battle = TestContent.Battle(new Stats(), TestContent.Chart(TestContent.Track(), 4), enemyHp: 50);
        battle.ApplyStatus(StatusTarget.Enemy, StatusKind.Bleed, stacks: 3);

        battle.AdvanceToBeat(2);

        Assert.That(battle.EnemyHp, Is.EqualTo(44));
    }

    [Test]
    public void thorns_consumed_by_next_attack()
    {
        // Left attacks on beats 1 and 3, both unanswered.
        var battle = TestContent.Battle(4, 12);
        battle.ApplyStatus(StatusTarget.Player, StatusKind.Thorns, value: 5);
        battle.ApplyStatus(StatusTarget.Player, StatusKind.Thorns, value: 5);

        battle.AdvanceToBeat(4);

        Assert.Multiple(() =>
        {
            Assert.That(
                battle.Events.OfType<StatusTriggered>().Where(e => e.Kind == StatusKind.Thorns).Select(e => e.Amount),
                Is.EqualTo(new[] { 5, 5 }));
            Assert.That(battle.EnemyHp, Is.EqualTo(battle.EnemyMaxHp - 10));
            Assert.That(battle.PlayerStatuses.Has(StatusKind.Thorns), Is.False);
        });
    }

    /// <summary>A 10-damage Perfect against an enemy carrying 5 Scar stacks, rolled on the given seed.</summary>
    private static int PerfectHitWithScar5(ulong seed)
    {
        var battle = TestContent.Battle(new Stats(), TestContent.Chart(TestContent.Track(), 4), rng: new SimRng(seed));
        battle.ApplyStatus(StatusTarget.Enemy, StatusKind.Scar, stacks: 5);
        battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 500 + Perfect);
        battle.AdvanceToBeat(2);
        return battle.Events.OfType<DamageDealt>().Single().Amount;
    }

    /// <summary>The first seed whose first roll in [0, 1000) satisfies the predicate.</summary>
    private static ulong SeedWhoseFirstRoll(Func<int, bool> predicate)
    {
        for (ulong seed = 1; seed < 10_000; seed++)
        {
            if (predicate(new SimRng(seed).NextInt(0, Fixed.One)))
            {
                return seed;
            }
        }

        throw new InvalidOperationException("No seed found.");
    }
}
