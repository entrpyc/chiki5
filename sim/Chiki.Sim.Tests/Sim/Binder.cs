using Chiki.Sim;
using Chiki.Sim.Data;
using SimBinder = Chiki.Sim.Binder;

namespace Sim;

public class Binder
{
    [Test]
    public void starter_autofill_is_legal()
    {
        var starter = CardLoader.SetFromJson(TestContent.ReadData("sets/starter.json"));
        var binder = SimBinder.Starter(starter);

        var stillEmpty = binder.AutoFill();
        var loadout = binder.Loadout;

        Assert.Multiple(() =>
        {
            Assert.That(binder.Cards.Select(c => c.Definition.Id), Is.EqualTo(starter.Cards.Select(c => c.Id)), "one instance per starter card");
            Assert.That(binder.Cards.Select(c => c.Id), Is.Unique);
            Assert.That(stillEmpty, Is.Empty);
            Assert.That(loadout.IsComplete, Is.True);
            Assert.That(loadout.EmptySlots, Is.Empty);
            Assert.That(loadout.Cards, Has.Count.EqualTo(16));
            Assert.That(loadout.Cards, Is.Unique, "a Binder card occupies at most one slot");
            foreach (var slot in loadout.Slots)
            {
                Assert.That(loadout[slot]!.Definition.Category.Allows(slot.Key), Is.True, $"{slot} holds a card of another Category");
                Assert.That(binder.Contains(loadout[slot]!), Is.True, $"{slot} holds a card outside the Binder");
            }

            for (int line = 0; line < Slot.LineCount; line++)
            {
                foreach (CardCategory category in Enum.GetValues(typeof(CardCategory)))
                {
                    Assert.That(loadout.Count(line, category), Is.EqualTo(2), $"line {line + 1} {category}");
                }
            }
        });
    }

    [Test]
    public void unstable_destroyed_after_lifespan()
    {
        var binder = new SimBinder();
        var fleeting = binder.Add(new CardDefinition("card-fleeting", "Fleeting", CardCategory.LeftAttack, 10, cardClass: CardClass.Unstable, lifespan: 2));
        var keyE = new Slot(0, SlotKey.E);
        binder.Assign(keyE, fleeting);

        var afterFirst = binder.BattleEnded();
        int remainingAfterFirst = fleeting.BattlesRemaining!.Value;
        bool inBinderAfterFirst = binder.Contains(fleeting);
        var afterSecond = binder.BattleEnded();

        Assert.Multiple(() =>
        {
            Assert.That(afterFirst, Is.Empty);
            Assert.That(remainingAfterFirst, Is.EqualTo(1));
            Assert.That(inBinderAfterFirst, Is.True);
            Assert.That(afterSecond, Is.EqualTo(new[] { fleeting }));
            Assert.That(binder.Contains(fleeting), Is.False);
            Assert.That(binder.Cards, Is.Empty);
            Assert.That(binder.Loadout[keyE], Is.Null);
        });
    }
}
