using Chiki.Sim;
using Chiki.Sim.Data;
using Chiki.Sim.Effects;

namespace Sim;

public class Charms
{
    [Test]
    public void fixtures_load()
    {
        var set = CharmLoader.SetFromJson(TestContent.ReadData("charms/fixtures.json"));
        var battle = TestContent.Battle(4);

        var cleanVictory = set.Charms.Single(c => c.Id == "charm-clean-victory");
        var momentumPlate = set.Charms.Single(c => c.Id == "charm-momentum-plate");
        var registered = set.Charms.SelectMany(c => c.Effects.Select(e => battle.RegisterEffect(c.Id, e))).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(set.Charms, Has.Count.EqualTo(2));
            Assert.That(set.Charms.Select(c => c.Trigger), Has.All.EqualTo(CharmTrigger.PerfectDefense));
            Assert.That(set.Charms.SelectMany(c => c.Effects).Select(e => (e.Trigger, e.Condition)),
                Has.All.EqualTo((EffectTrigger.BattleEnded, EffectCondition.IfPerfectDefense)));
            Assert.That(set.Charms.Select(c => c.Unlock.Fact), Has.All.EqualTo(UnlockFact.BossesDefeated));
            Assert.That(set.Charms.Select(c => c.Unlock.Amount), Has.All.EqualTo(1));
            Assert.That(set.Charms.Select(c => c.Unlock.IsMet(new ProfileFacts { BossesDefeated = 0 })), Has.All.False);
            Assert.That(set.Charms.Select(c => c.Unlock.IsMet(new ProfileFacts { BossesDefeated = 1 })), Has.All.True);
            Assert.That(set.Charms.Select(c => c.EndingAltering), Has.All.False);

            Assert.That(cleanVictory.Name, Is.EqualTo("Clean Victory"));
            Assert.That(cleanVictory.Effects.Single(), Is.EqualTo(new EffectDefinition(
                EffectTrigger.BattleEnded, EffectModifier.ChangeStat, 10, EffectCondition.IfPerfectDefense, StatusTarget.Player, stat: RunStat.Essence)));
            Assert.That(cleanVictory.RunCap, Is.Null);

            Assert.That(momentumPlate.Name, Is.EqualTo("Momentum Plate"));
            Assert.That(momentumPlate.Effects.Single(), Is.EqualTo(new EffectDefinition(
                EffectTrigger.BattleEnded, EffectModifier.ChangeStat, 5, EffectCondition.IfPerfectDefense, StatusTarget.Player, stat: RunStat.MaxArd)));
            Assert.That(momentumPlate.RunCap, Is.EqualTo(50));

            Assert.That(registered, Has.Count.EqualTo(2), "every Charm effect registers with the framework");
        });
    }
}
