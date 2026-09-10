using Chiki.Sim;
using SimBattle = Chiki.Sim.Battle;
using Stats = Chiki.Sim.RunStats;

namespace Sim;

/// <summary>One test per row of PRD 3.3.4.8, built from the public battle API.</summary>
public class EdgeCases
{
    // BPM 120: beat n sits at n * 500 ms. Offsets: +0 Perfect, +60 Good, +120 Miss (P2.6).
    private const int Perfect = 0;
    private const int Good = 60;
    private const int Miss = 120;

    private const int EnemyDmg = 20;
    private const int Weak25 = 250;

    [Test]
    public void perfect_wrong_side_while_attacked()
    {
        var stats = new Stats();
        var battle = AttackedLeft(stats);
        battle.Press(TestContent.SlotJ, TestContent.RightAttack(10), 500 + Perfect);

        battle.AdvanceToBeat(2);

        Assert.Multiple(() =>
        {
            Assert.That(Dealt(battle), Is.EqualTo(0));
            Assert.That(Taken(stats), Is.EqualTo(0));
        });
    }

    [Test]
    public void good_correct_counter()
    {
        var stats = new Stats();
        var battle = AttackedLeft(stats, block: 4);
        battle.Press(TestContent.SlotD, TestContent.LeftAttack10, 500 + Good);

        battle.AdvanceToBeat(2);

        Assert.Multiple(() =>
        {
            Assert.That(Dealt(battle), Is.EqualTo(5));
            Assert.That(Taken(stats), Is.EqualTo(6));
        });
    }

    [Test]
    public void miss_any_card()
    {
        var plays = new (string Name, Slot Slot, CardDefinition Card)[]
        {
            ("Defense", TestContent.SlotL, TestContent.Defense(8)),
            ("correct-side Attack", TestContent.SlotD, TestContent.LeftAttack10),
            ("wrong-side Attack", TestContent.SlotJ, TestContent.RightAttack(10)),
            ("Ability", TestContent.SlotA, TestContent.Ability()),
        };

        Assert.Multiple(() =>
        {
            foreach (var (name, slot, card) in plays)
            {
                var stats = new Stats();
                var battle = AttackedLeft(stats, block: 4);
                battle.Press(slot, card, 500 + Miss);
                battle.AdvanceToBeat(2);

                Assert.That(Dealt(battle), Is.EqualTo(0), $"{name}: dealt");
                Assert.That(Taken(stats), Is.EqualTo(16), $"{name}: taken");
                Assert.That(battle.CooldownOf(slot), Is.GreaterThan(0), $"{name}: cooldown");
            }
        });
    }

    [Test]
    public void perfect_ability_while_attacked()
    {
        var stats = new Stats();
        var battle = AttackedLeft(stats);
        battle.Press(TestContent.SlotA, TestContent.Ability(), 500 + Perfect);

        battle.AdvanceToBeat(2);

        Assert.Multiple(() =>
        {
            Assert.That(battle.Events.OfType<AbilityResolved>().Single().EffectMultThousandths, Is.EqualTo(Fixed.One));
            Assert.That(Taken(stats), Is.EqualTo(0));
        });
    }

    [Test]
    public void good_defense_while_attacked()
    {
        var stats = new Stats();
        var battle = AttackedLeft(stats, block: 4);
        battle.Press(TestContent.SlotL, TestContent.Defense(8), 500 + Good);

        battle.AdvanceToBeat(2);

        Assert.Multiple(() =>
        {
            Assert.That(battle.Events.OfType<BlockGained>().Last().Amount, Is.EqualTo(4));
            Assert.That(Taken(stats), Is.EqualTo(6));
        });
    }

    [Test]
    public void no_input_while_attacked()
    {
        var stats = new Stats();
        var battle = AttackedLeft(stats, block: 4);

        battle.AdvanceToBeat(2);

        Assert.Multiple(() =>
        {
            Assert.That(Taken(stats), Is.EqualTo(16));
            Assert.That(Slot.All.Select(battle.CooldownOf), Has.All.EqualTo(0));
            Assert.That(battle.Events.OfType<InputJudged>(), Is.Empty, "nothing a Miss-triggered effect could fire on");
            Assert.That(battle.JudgmentLog.Single().NoInput, Is.True);
        });
    }

    [Test]
    public void perfect_signature_send_while_attacked()
    {
        var stats = new Stats();
        var battle = AttackedLeft(stats);
        battle.Send(TestContent.SlotD, TestContent.LeftAttack10, 500 + Perfect);

        battle.AdvanceToBeat(2);

        Assert.Multiple(() =>
        {
            Assert.That(battle.SignatureChain, Is.EqualTo(new[] { TestContent.LeftAttack10.Id }));
            Assert.That(Taken(stats), Is.EqualTo(0));
        });
    }

    [Test]
    public void stun_when_enemy_action_arrives()
    {
        var stats = new Stats();
        var battle = AttackedLeft(stats);
        battle.ApplyStatus(StatusTarget.Player, StatusKind.Stun);
        battle.Press(TestContent.SlotD, TestContent.LeftAttack10, 500 + Perfect);

        battle.AdvanceToBeat(2);

        Assert.Multiple(() =>
        {
            Assert.That(battle.JudgmentLog.Single().NoInput, Is.True);
            Assert.That(Taken(stats), Is.EqualTo(EnemyDmg));
            Assert.That(Slot.All.Select(battle.CooldownOf), Has.All.EqualTo(0));
        });
    }

    [Test]
    public void debuff_on_perfect_beat()
    {
        var stats = new Stats();
        var battle = TestContent.Battle(stats, TestContent.Chart(TestContent.Track(), TestContent.LeftApplying(4, TestContent.Weak(Weak25))), enemyDmg: EnemyDmg);
        battle.Press(TestContent.SlotD, TestContent.LeftAttack10, 500 + Perfect);

        battle.AdvanceToBeat(2);

        Assert.Multiple(() =>
        {
            Assert.That(battle.PlayerStatuses.Has(StatusKind.Weak), Is.True);
            Assert.That(Taken(stats), Is.EqualTo(0));
        });
    }

    /// <summary>The enemy attacks Left for 20 on beat 1; the player holds the given Block as it arrives.</summary>
    private static SimBattle AttackedLeft(Stats stats, int block = 0)
    {
        var battle = TestContent.Battle(stats, TestContent.Chart(TestContent.Track(), TestContent.Left(4)), enemyDmg: EnemyDmg);
        if (block > 0)
        {
            battle.GrantBlock(StatusTarget.Player, block);
        }

        return battle;
    }

    private static int Dealt(SimBattle battle) => battle.EnemyMaxHp - battle.EnemyHp;

    private static int Taken(Stats stats) => Tuning.ArdBaseline - stats.Ard;
}
