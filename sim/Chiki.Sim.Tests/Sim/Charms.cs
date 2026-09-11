using Chiki.Sim;
using Chiki.Sim.Data;
using Chiki.Sim.Effects;

namespace Sim;

public class Charms
{
    private const string CleanVictory = "charm-clean-victory";

    [Test]
    public void fixtures_load()
    {
        var set = CharmLoader.SetFromJson(TestContent.ReadData("charms/fixtures.json"));
        var battle = TestContent.Battle(4);

        var cleanVictory = set.Charms.Single(c => c.Id == CleanVictory);
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

    [Test]
    public void clean_victory_pays_on_perfect_defense()
    {
        var setup = new RunSetup(new[] { CleanVictory });
        setup.Equip(CleanVictory);
        var run = setup.Start(TestContent.LoadRunContent(), "chiki-1");
        var slotE = new Slot(0, SlotKey.E);

        int before = run.Stats.Essence;
        var perfect = TestContent.RunBattle(run);
        perfect.Press(slotE, 500);
        perfect.AdvanceToBeat(2);
        run.SettleBattle(perfect);
        int afterPerfect = run.Stats.Essence;

        var hit = TestContent.RunBattle(run);
        hit.Press(slotE, 500 + 60); // Good: half the damage comes in, then the card kills
        hit.AdvanceToBeat(2);
        run.SettleBattle(hit);
        int afterHit = run.Stats.Essence;

        Assert.Multiple(() =>
        {
            Assert.That(perfect.Outcome, Is.EqualTo(BattleOutcome.Won));
            Assert.That(perfect.PerfectDefense, Is.True);
            Assert.That(afterPerfect - before, Is.EqualTo(10));
            Assert.That(hit.Outcome, Is.EqualTo(BattleOutcome.Won));
            Assert.That(hit.PerfectDefense, Is.False);
            Assert.That(afterHit, Is.EqualTo(afterPerfect));
        });
    }

    [Test]
    public void until_reset_lasts_the_run()
    {
        var untilReset = new CharmDefinition(
            "charm-lasting-fury",
            "Lasting Fury",
            CharmRarity.Rare,
            CharmTrigger.PerfectDefense,
            new[]
            {
                new EffectDefinition(
                    EffectTrigger.BattleEnded, EffectModifier.MultiplyValue, 1500, EffectCondition.IfPerfectDefense, StatusTarget.Player,
                    value: EffectValue.DamageDealt, lifetime: EffectLifetime.Run),
            },
            new UnlockCondition(UnlockFact.BossesDefeated, 1));
        var starter = CardLoader.SetFromJson(TestContent.ReadData("sets/starter.json"));
        var content = new RunContent(starter, new CharmSet("test", new[] { untilReset }));
        var setup = new RunSetup(new[] { untilReset.Id });
        setup.Equip(untilReset.Id);
        var run = setup.Start(content, "chiki-1");

        var battle1 = TestContent.RunBattle(run);
        battle1.Press(new Slot(0, SlotKey.E), 500);
        battle1.AdvanceToBeat(2);
        run.SettleBattle(battle1);
        var battle2 = TestContent.RunBattle(run);
        int inBattle2 = battle2.Effects.MultiplierFor(EffectValue.DamageDealt);
        battle2.Press(new Slot(0, SlotKey.E), 500);
        battle2.AdvanceToBeat(2);
        run.SettleBattle(battle2);
        run.End(RunStatus.Won);

        var nextSetup = new RunSetup(new[] { untilReset.Id });
        nextSetup.Equip(untilReset.Id);
        var next = nextSetup.Start(content, "chiki-2");
        var nextBattle = TestContent.RunBattle(next);

        Assert.Multiple(() =>
        {
            Assert.That(battle1.PerfectDefense, Is.True, "the effect fires on battle 1's Perfect Defense");
            Assert.That(inBattle2, Is.EqualTo(1500), "the until-reset modifier still applies when battle 2 starts");
            Assert.That(nextBattle.Effects.MultiplierFor(EffectValue.DamageDealt), Is.EqualTo(Fixed.One), "a new run starts without it");
        });
    }
}
