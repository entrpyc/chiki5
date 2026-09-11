using System.Reflection;
using Chiki.Sim;
using Chiki.Sim.Data;
using SimBattle = Chiki.Sim.Battle;
using SimBinder = Chiki.Sim.Binder;
using SimLoadout = Chiki.Sim.Loadout;
using Stats = Chiki.Sim.RunStats;

namespace Sim;

public class Loadout
{
    private static readonly string[] HiddenZoneWords = { "draw", "discard", "hand", "deck", "pile" };

    [Test]
    public void sixteen_slots_and_no_hidden_zone()
    {
        var loadout = new SimLoadout();

        var slots = loadout.Slots;
        var hiddenZoneMembers = typeof(SimBattle).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Concat(typeof(SimLoadout).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Select(m => m.Name)
            .Where(name => HiddenZoneWords.Any(word => name.Contains(word, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(slots, Has.Count.EqualTo(16));
            Assert.That(slots, Is.Unique);
            Assert.That(slots.Select(s => s.Line).Distinct(), Is.EquivalentTo(new[] { 0, 1 }));
            Assert.That(slots.Where(s => s.Line == 0).Select(s => s.Key), Is.EquivalentTo(Enum.GetValues<SlotKey>()));
            Assert.That(slots.Where(s => s.Line == 1).Select(s => s.Key), Is.EquivalentTo(Enum.GetValues<SlotKey>()));
            Assert.That(slots.All(s => loadout[s] is null), Is.True, "a new loadout is empty");
            Assert.That(hiddenZoneMembers, Is.Empty, "the battle and loadout expose no draw, hand or discard operation");
        });
    }

    [Test]
    public void category_slot_rejected()
    {
        var loadout = new SimLoadout();
        var defense = new CardInstance(1, TestContent.Defense(8));
        var keyE = new Slot(0, SlotKey.E);
        var keyO = new Slot(0, SlotKey.O);

        var inLeftAttackKey = loadout.Assign(keyE, defense);
        var inDefenseKey = loadout.Assign(keyO, defense);

        Assert.Multiple(() =>
        {
            Assert.That(inLeftAttackKey, Is.EqualTo(Placement.WrongCategory));
            Assert.That(loadout[keyE], Is.Null);
            Assert.That(inDefenseKey, Is.EqualTo(Placement.Accepted));
            Assert.That(loadout[keyO], Is.SameAs(defense));
        });
    }

    [Test]
    public void instance_in_one_slot_only()
    {
        var loadout = new SimLoadout();
        var ability = new CardInstance(1, TestContent.Ability());
        var line1KeyQ = new Slot(0, SlotKey.Q);
        var line2KeyW = new Slot(1, SlotKey.W);
        loadout.Assign(line1KeyQ, ability);

        var second = loadout.Assign(line2KeyW, ability);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(Placement.AlreadySlotted));
            Assert.That(loadout[line1KeyQ], Is.SameAs(ability));
            Assert.That(loadout[line2KeyW], Is.Null);
            Assert.That(loadout.SlotOf(ability), Is.EqualTo(line1KeyQ));
        });
    }

    [Test]
    public void empty_slot_blocks_battle()
    {
        var binder = SimBinder.Starter(CardLoader.SetFromJson(TestContent.ReadData("sets/starter.json")));
        binder.AutoFill();
        var line2KeyI = new Slot(1, SlotKey.I);
        var enemy = TestContent.Enemy(TestContent.Chart(TestContent.Track(), 4));
        var removed = binder.Clear(line2KeyI)!;

        var rejected = Assert.Throws<LoadoutIncompleteException>(() => new SimBattle(new Stats(), enemy, TestContent.DefaultEnemyHp, binder.Loadout, TestContent.Rng()))!;
        binder.Assign(line2KeyI, removed);
        var battle = new SimBattle(new Stats(), enemy, TestContent.DefaultEnemyHp, binder.Loadout, TestContent.Rng());

        Assert.Multiple(() =>
        {
            Assert.That(rejected.EmptySlots, Is.EqualTo(new[] { line2KeyI }));
            Assert.That(rejected.Message, Does.Contain("(2, I)"));
            Assert.That(battle.Loadout, Is.SameAs(binder.Loadout));
            Assert.That(battle.Press(line2KeyI, 500).Accepted, Is.True, "the battle plays the card the slot holds");
        });
    }
}
