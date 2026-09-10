using Chiki.Sim;
using Chiki.Sim.Effects;
using Stats = Chiki.Sim.RunStats;

namespace Sim;

public class Effects
{
    // BPM 120: beat n sits at n * 500 ms. Offsets: +0 Perfect, +60 Good (P2.6).
    private const int Perfect = 0;
    private const int Good = 60;

    private static readonly EffectDefinition ThornsOnDamageTaken = new(
        EffectTrigger.DamageTaken,
        EffectModifier.ApplyStatus,
        target: StatusTarget.Player,
        status: new StatusApplication(StatusKind.Thorns, 1, 2));

    [Test]
    public void trigger_fires_on_event()
    {
        // The enemy attacks on beat 1 and the player takes it unanswered.
        var battle = TestContent.Battle(4);
        battle.RegisterEffect("test", ThornsOnDamageTaken);

        battle.AdvanceToBeat(2);

        var thorns = battle.PlayerStatuses.Instances.Where(i => i.Kind == StatusKind.Thorns).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(thorns, Has.Count.EqualTo(1));
            Assert.That(thorns[0].Value, Is.EqualTo(2));
        });
    }

    [Test]
    public void condition_gates_trigger()
    {
        var battle = TestContent.Battle(4);
        battle.RegisterEffect("test", new EffectDefinition(
            EffectTrigger.DamageTaken,
            EffectModifier.ApplyStatus,
            condition: EffectCondition.OnPerfect,
            target: StatusTarget.Player,
            status: new StatusApplication(StatusKind.Thorns, 1, 2)));
        battle.Press(TestContent.SlotD, TestContent.LeftAttack10, 500 + Good);

        battle.AdvanceToBeat(2);

        Assert.Multiple(() =>
        {
            Assert.That(battle.Events.OfType<DamageTaken>().Single().Amount, Is.GreaterThan(0), "damage must have been taken on the Good");
            Assert.That(battle.PlayerStatuses.Has(StatusKind.Thorns), Is.False);
        });
    }

    [Test]
    public void lifetime_in_beats_expires()
    {
        // Double damage dealt for 3 beats from beat 0; attacks on beats 1 and 3.
        var battle = TestContent.Battle(4, 12);
        battle.RegisterEffect("test", new EffectDefinition(
            EffectTrigger.Passive,
            EffectModifier.MultiplyValue,
            2000,
            value: EffectValue.DamageDealt,
            lifetime: EffectLifetime.Beats,
            lifetimeBeats: 3));
        battle.Press(TestContent.SlotD, TestContent.LeftAttack10, 500 + Perfect);
        battle.Press(TestContent.SlotF, TestContent.LeftAttack10, 1500 + Perfect);

        battle.AdvanceToBeat(4);

        Assert.That(battle.Events.OfType<DamageDealt>().Select(e => e.Amount), Is.EqualTo(new[] { 20, 10 }));
    }
}
