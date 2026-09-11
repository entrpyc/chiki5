using Chiki.Sim;
using Chiki.Sim.Data;
using SimRun = Chiki.Sim.Run;

namespace Sim;

public class Run
{
    private const string CleanVictory = "charm-clean-victory";
    private const string MomentumPlate = "charm-momentum-plate";

    private static CardSet Starter() => CardLoader.SetFromJson(TestContent.ReadData("sets/starter.json"));

    [Test]
    public void run_round_trips_to_json()
    {
        var fleetingDefinition = new CardDefinition("card-fleeting", "Fleeting", CardCategory.Defense, 8, cardClass: CardClass.Unstable, lifespan: 2);
        var content = TestContent.LoadRunContent(new CardSet("test", new[] { fleetingDefinition }));
        var setup = new RunSetup(new[] { CleanVictory, MomentumPlate });
        setup.Equip(MomentumPlate);
        var run = setup.Start(content, "chiki-1");
        run.Stats.Ard = 250;
        run.Stats.BaseDmg = 2;
        run.Stats.Essence = 40;
        run.Stats.Crp = 12;
        run.AcquireImprint("imprint-thick-hide");
        var fleeting = run.Binder.Add(fleetingDefinition);
        run.Binder.BattleEnded();
        run.Binder.Clear(new Slot(1, SlotKey.I));
        run.Binder.Assign(new Slot(1, SlotKey.P), fleeting);

        var json = RunSerializer.ToJson(run);
        var restored = RunSerializer.FromJson(json, content);

        Assert.Multiple(() =>
        {
            Assert.That(JsonValue.Parse(json)["schemaVersion"].AsInt(), Is.EqualTo(RunSerializer.SchemaVersion));
            Assert.That(restored.Seed, Is.EqualTo(run.Seed));
            Assert.That(restored.World, Is.EqualTo(run.World));
            Assert.That(restored.CurrentNodeId, Is.EqualTo(run.CurrentNodeId));
            Assert.That(restored.Status, Is.EqualTo(run.Status));
            Assert.That(restored.BattlesStarted, Is.EqualTo(run.BattlesStarted));
            Assert.That((restored.Stats.MaxArd, restored.Stats.Ard, restored.Stats.BaseDmg, restored.Stats.Essence, restored.Stats.Crp),
                Is.EqualTo((run.Stats.MaxArd, run.Stats.Ard, run.Stats.BaseDmg, run.Stats.Essence, run.Stats.Crp)));
            Assert.That(restored.Charms, Is.EqualTo(run.Charms));
            Assert.That(restored.Imprints, Is.EqualTo(run.Imprints));
            Assert.That(restored.Effects.Registered.Select(r => (r.OwnerId, r.Definition)), Is.EqualTo(run.Effects.Registered.Select(r => (r.OwnerId, r.Definition))));
            Assert.That(restored.ArmorUpgrades, Is.EqualTo(run.ArmorUpgrades));
            Assert.That(restored.DifficultyModifiers, Is.EqualTo(run.DifficultyModifiers));
            Assert.That(restored.Assist, Is.EqualTo(run.Assist));
            Assert.That(restored.Binder.NextId, Is.EqualTo(run.Binder.NextId));
            Assert.That(restored.Binder.Cards.Select(Snapshot), Is.EqualTo(run.Binder.Cards.Select(Snapshot)));
            Assert.That(restored.Loadout.Slots.Select(s => restored.Loadout[s]?.Id), Is.EqualTo(run.Loadout.Slots.Select(s => run.Loadout[s]?.Id)));
            Assert.That(RunSerializer.ToJson(restored), Is.EqualTo(json), "a second round trip writes the same document");
        });
    }

    [Test]
    public void custom_seed_stored_and_forks_rng()
    {
        var first = new RunSetup(Array.Empty<string>()).Start(Starter(), "chiki-1");
        var second = new RunSetup(Array.Empty<string>()).Start(Starter(), "chiki-1");

        var firstMap = first.Fork("map");
        var secondMap = second.Fork("map");
        var firstStream = Enumerable.Range(0, 8).Select(_ => firstMap.NextUInt64()).ToList();
        var secondStream = Enumerable.Range(0, 8).Select(_ => secondMap.NextUInt64()).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(first.Seed, Is.EqualTo("chiki-1"));
            Assert.That(second.Seed, Is.EqualTo("chiki-1"));
            Assert.That(firstStream, Is.EqualTo(secondStream));
        });
    }

    [Test]
    public void equip_up_to_two_owned_charms()
    {
        var setup = new RunSetup(new[] { CleanVictory, MomentumPlate, "charm-third" });

        var one = setup.Equip(CleanVictory);
        var two = setup.Equip(MomentumPlate);
        var third = setup.Equip("charm-third");
        var unowned = setup.Equip("charm-unowned");
        var run = setup.Start(Starter(), "chiki-1");
        var afterStart = setup.Equip("charm-third");
        var unequipAfterStart = setup.Unequip(CleanVictory);

        Assert.Multiple(() =>
        {
            Assert.That(one, Is.EqualTo(EquipResult.Equipped));
            Assert.That(two, Is.EqualTo(EquipResult.Equipped));
            Assert.That(third, Is.EqualTo(EquipResult.SlotsFull));
            Assert.That(unowned, Is.EqualTo(EquipResult.NotUnlocked));
            Assert.That(run.Charms, Is.EqualTo(new[] { CleanVictory, MomentumPlate }));
            Assert.That(afterStart, Is.EqualTo(EquipResult.RunStarted));
            Assert.That(unequipAfterStart, Is.False);
            Assert.That(run.Charms, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void start_state()
    {
        var setup = new RunSetup(new[] { CleanVictory });
        setup.Equip(CleanVictory);

        var run = setup.Start(Starter(), "chiki-1");

        Assert.Multiple(() =>
        {
            Assert.That(run.Loadout.IsComplete, Is.True);
            Assert.That(run.Charms, Has.Count.EqualTo(1));
            Assert.That(run.Imprints, Is.Empty);
            Assert.That(run.Stats.Ard, Is.EqualTo(300));
            Assert.That(run.Stats.MaxArd, Is.EqualTo(300));
            Assert.That(run.Stats.BaseDmg, Is.EqualTo(0));
            Assert.That(run.Stats.Essence, Is.EqualTo(0));
            Assert.That(run.Stats.Crp, Is.EqualTo(0));
        });
    }

    [Test]
    public void new_run_after_end_is_fresh()
    {
        var unlocked = new[] { CleanVictory };
        var ended = new RunSetup(unlocked).Start(TestContent.LoadRunContent(), "chiki-1");
        ended.Stats.Essence = 300;
        ended.Stats.Crp = 60;
        ended.AcquireImprint(ImprintTier.Common);
        ended.End(RunStatus.Won);

        var next = new RunSetup(unlocked).Start(TestContent.LoadRunContent(), "chiki-2");

        Assert.Multiple(() =>
        {
            Assert.That(ended.Status, Is.EqualTo(RunStatus.Won));
            Assert.That(next.Stats.Essence, Is.EqualTo(0));
            Assert.That(next.Stats.Crp, Is.EqualTo(0));
            Assert.That(next.Imprints, Is.Empty);
            Assert.That(next.Binder.Cards, Has.Count.EqualTo(Starter().Cards.Count));
            Assert.That(next.Stats.Ard, Is.EqualTo(next.Stats.MaxArd));
        });
    }

    [Test]
    public void battle_death_ends_run()
    {
        var run = new RunSetup(Array.Empty<string>()).Start(TestContent.LoadRunContent(), "chiki-1");
        run.Stats.Ard = 15;
        var battle = run.StartBattle(TestContent.Enemy(TestContent.Chart(TestContent.Track(), 4), damagePerHit: 20), TestContent.DefaultEnemyHp);

        battle.AdvanceToBeat(2);
        run.SettleBattle(battle);

        Assert.Multiple(() =>
        {
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.Died));
            Assert.That(run.Status, Is.EqualTo(RunStatus.Died));
            Assert.That(run.IsOver, Is.True);
            Assert.That(() => TestContent.RunBattle(run), Throws.InvalidOperationException);
            Assert.That(() => run.AcquireImprint(ImprintTier.Common), Throws.InvalidOperationException);
        });
    }

    [Test]
    public void three_worlds_then_won()
    {
        var run = new RunSetup(Array.Empty<string>()).Start(TestContent.LoadRunContent(), "chiki-1");
        var worldsSeen = new List<int> { run.World };
        var statuses = new List<RunStatus>();

        for (int world = 1; world <= 3; world++)
        {
            while (run.CurrentNode.Type != NodeType.Boss)
            {
                if (!run.CurrentNodeCompleted)
                {
                    run.CompleteNode();
                }

                Assume.That(run.MoveTo(run.ForwardNodes[0].Id), Is.EqualTo(MoveResult.Moved));
            }

            TestContent.WinNodeBattle(run);
            statuses.Add(run.Status);
            if (!run.IsOver)
            {
                worldsSeen.Add(run.World);
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(worldsSeen, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(statuses, Is.EqualTo(new[] { RunStatus.InProgress, RunStatus.InProgress, RunStatus.Won }));
            Assert.That(run.Events.OfType<WorldEntered>().Select(e => e.World), Is.EqualTo(new[] { 2, 3 }));
        });
    }

    private static (int, string, bool, string?, int?, int?) Snapshot(CardInstance card) =>
        (card.Id, card.Definition.Id, card.Upgraded, card.TraitId, card.BattlesRemaining, card.ShopPrice);
}
