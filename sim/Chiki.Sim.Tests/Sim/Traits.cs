using Chiki.Sim;
using Chiki.Sim.Data;
using Chiki.Sim.Effects;

namespace Sim;

public class Traits
{
    [Test]
    public void fixtures_load_and_validate()
    {
        var set = TraitLoader.SetFromJson(TestContent.ReadData("traits/fixtures.json"));
        var battle = TestContent.Battle(4);

        var registered = set.Traits.Select(t => battle.RegisterEffect(t.Id, t.Effect)).ToList();
        const string noEffect = """
            { "id": "broken", "traits": [ { "id": "trait-broken", "name": "Broken", "source": "pool" } ] }
            """;

        Assert.Multiple(() =>
        {
            Assert.That(set.Traits, Has.Count.EqualTo(2));
            Assert.That(set.Traits.Select(t => t.Id), Is.Unique);
            Assert.That(battle.Effects.Registered.Where(r => r.OwnerId.StartsWith("trait-")), Is.EquivalentTo(registered), "a Trait's effect did not register with the framework");
            Assert.That(() => TraitLoader.SetFromJson(noEffect), Throws.TypeOf<JsonException>(), "a Trait with no effect passed validation");
        });
    }

    /// <summary>A card saved with a Trait comes back holding it, resolved against the pool; an unknown Trait id is refused.</summary>
    [Test]
    public void restored_card_resolves_trait()
    {
        var content = TestContent.LoadRunContent();
        var run = TestContent.RunInWorld(content, "chiki-1", 1);
        string json = RunSerializer.ToJson(run);
        int firstId = run.Binder.Cards[0].Id;
        var emptyTrait = new System.Text.RegularExpressions.Regex("\"trait\":\\s*null");
        Assume.That(emptyTrait.IsMatch(json), "the saved Binder does not record an empty Trait slot");
        string withTrait = emptyTrait.Replace(json, "\"trait\": \"trait-tempered\"", 1);
        string withUnknown = emptyTrait.Replace(json, "\"trait\": \"trait-nothing\"", 1);

        var restored = RunSerializer.FromJson(withTrait, content);
        var card = restored.Binder.Find(firstId)!;

        Assert.Multiple(() =>
        {
            Assert.That(card.TraitId, Is.EqualTo("trait-tempered"));
            Assert.That(content.TraitOf(card)?.Name, Is.EqualTo("Tempered"));
            Assert.That(() => RunSerializer.FromJson(withUnknown, content), Throws.TypeOf<JsonException>());
        });
    }
}
