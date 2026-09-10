using Chiki.Sim;
using Chiki.Sim.Effects;
using SimBattle = Chiki.Sim.Battle;
using Stats = Chiki.Sim.RunStats;

namespace Sim;

/// <summary>The four card mechanic kinds of PRD 3.4.9, each through the effect framework.</summary>
public class Mechanics
{
    // BPM 120: beat n sits at n * 500 ms. Offsets: +0 Perfect, +60 Good, +120 Miss (P2.6).
    private const int Perfect = 0;
    private const int Good = 60;
    private const int Miss = 120;

    [Test]
    public void direct_damage()
    {
        var battle = TestContent.Battle(TestContent.Left(4));
        battle.Press(TestContent.SlotE, TestContent.LeftAttack10, 500 + Perfect);

        battle.AdvanceToBeat(2);

        Assert.That(battle.EnemyHp, Is.EqualTo(battle.EnemyMaxHp - 10));
    }

    [Test]
    public void scales_with_block()
    {
        var card = LeftAttack("card-scaling-block", 6, scaling: new ValueScaling(ScalingSource.Block, 1, per: 2));
        var battle = TestContent.Battle(TestContent.Left(4));
        battle.GrantBlock(StatusTarget.Player, 8);
        battle.Press(TestContent.SlotE, card, 500 + Perfect);

        battle.AdvanceToBeat(2);

        Assert.That(Dealt(battle), Is.EqualTo(10));
    }

    [Test]
    public void scales_with_crp_when_declared()
    {
        var declared = LeftAttack("card-scaling-crp", 8, scaling: new ValueScaling(ScalingSource.Crp, 1, per: 10));
        var plain = LeftAttack("card-plain", 8);

        Assert.Multiple(() =>
        {
            Assert.That(DealtAtCrp(declared, 40), Is.EqualTo(12));
            Assert.That(DealtAtCrp(plain, 40), Is.EqualTo(8));
            Assert.That(DealtAtCrp(plain, 0), Is.EqualTo(8));
        });
    }

    [Test]
    public void applies_bleed_on_perfect_not_miss()
    {
        var card = LeftAttack("card-bleeder", 10, new EffectDefinition(
            EffectTrigger.OnPlay,
            EffectModifier.ApplyStatus,
            status: new StatusApplication(StatusKind.Bleed, 2)));

        Assert.Multiple(() =>
        {
            Assert.That(Play(card, Perfect).EnemyStatuses.Stacks(StatusKind.Bleed), Is.EqualTo(2));
            Assert.That(Play(card, Miss).EnemyStatuses.Has(StatusKind.Bleed), Is.False);
        });
    }

    [Test]
    public void on_perfect_bonus()
    {
        var card = LeftAttack("card-precise", 10, new EffectDefinition(
            EffectTrigger.OnPlay,
            EffectModifier.AddValue,
            5,
            EffectCondition.OnPerfect));

        Assert.Multiple(() =>
        {
            Assert.That(Dealt(Play(card, Perfect)), Is.EqualTo(15));
            Assert.That(Dealt(Play(card, Good)), Is.EqualTo(5));
        });
    }

    [Test]
    public void if_kills_grants_block()
    {
        var card = LeftAttack("card-finisher", 10, new EffectDefinition(
            EffectTrigger.OnPlay,
            EffectModifier.GainBlock,
            10,
            EffectCondition.IfKills,
            StatusTarget.Player));

        var kill = Play(card, Perfect, enemyHp: 10);
        var noKill = Play(card, Perfect, enemyHp: 1000);

        Assume.That(kill.Outcome, Is.EqualTo(BattleOutcome.Won));
        Assert.Multiple(() =>
        {
            Assert.That(kill.Events.OfType<BlockGained>().Where(b => b.Target == StatusTarget.Player).Select(b => b.Amount), Is.EqualTo(new[] { 10 }));
            Assert.That(noKill.Events.OfType<BlockGained>(), Is.Empty);
        });
    }

    [Test]
    public void if_enemy_attacking()
    {
        var card = LeftAttack("card-riposte", 10, new EffectDefinition(
            EffectTrigger.OnPlay,
            EffectModifier.MultiplyValue,
            2000,
            EffectCondition.IfEnemyAttacking,
            value: EffectValue.CardValue));

        Assert.Multiple(() =>
        {
            Assert.That(Dealt(Play(card, Perfect, action: TestContent.Left(4))), Is.EqualTo(20));
            Assert.That(Dealt(Play(card, Perfect, action: TestContent.Buff(4))), Is.EqualTo(10));
        });
    }

    private static CardDefinition LeftAttack(string id, int value, EffectDefinition? effect = null, ValueScaling? scaling = null)
    {
        return new CardDefinition(id, id, CardCategory.LeftAttack, value, effects: effect is null ? null : new[] { effect }, scaling: scaling);
    }

    /// <summary>Answers the enemy's beat-1 action (a Left attack unless given) with the card at the offset, and resolves it.</summary>
    private static SimBattle Play(CardDefinition card, int offsetMs, int enemyHp = TestContent.DefaultEnemyHp, EnemyAction? action = null)
    {
        var battle = TestContent.Battle(new Stats(), TestContent.Chart(TestContent.Track(), action ?? TestContent.Left(4)), enemyHp: enemyHp);
        battle.Press(TestContent.SlotE, card, 500 + offsetMs);
        battle.AdvanceToBeat(2);
        return battle;
    }

    private static int DealtAtCrp(CardDefinition card, int crp)
    {
        var battle = TestContent.Battle(new Stats { Crp = crp }, TestContent.Chart(TestContent.Track(), TestContent.Left(4)));
        battle.Press(TestContent.SlotE, card, 500 + Perfect);
        battle.AdvanceToBeat(2);
        return Dealt(battle);
    }

    private static int Dealt(SimBattle battle) => battle.EnemyMaxHp - battle.EnemyHp;
}
