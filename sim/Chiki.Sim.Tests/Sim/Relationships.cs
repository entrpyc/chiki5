using Chiki.Sim;
using SimRelationships = Chiki.Sim.Relationships;

namespace Sim;

public class Relationships
{
    [Test]
    public void level_up_costs_n_plus_1()
    {
        var relationships = new SimRelationships();
        var flowerGirl = relationships[Npc.FlowerGirl];

        relationships.GrantRp(Npc.FlowerGirl, 2, RpSource.Dialogue);
        int levelAfterTwo = flowerGirl.Level;
        int towardAfterTwo = flowerGirl.RpTowardNext;
        relationships.GrantRp(Npc.FlowerGirl, 2, RpSource.Dialogue);

        Assert.Multiple(() =>
        {
            Assert.That(levelAfterTwo, Is.EqualTo(2));
            Assert.That(towardAfterTwo, Is.EqualTo(0));
            Assert.That(flowerGirl.Level, Is.EqualTo(2));
            Assert.That(flowerGirl.RpTowardNext, Is.EqualTo(2));
            Assert.That(flowerGirl.NextLevelCost, Is.EqualTo(3));
        });
    }

    [Test]
    public void secondary_caps_at_5()
    {
        var relationships = new SimRelationships(new[] { new Relationship(Npc.Bjorn, 5, 0, Array.Empty<string>(), null) });

        int gained = relationships.GrantRp(Npc.Bjorn, 20, RpSource.Interaction);

        Assert.Multiple(() =>
        {
            Assert.That(gained, Is.EqualTo(0));
            Assert.That(relationships[Npc.Bjorn].Level, Is.EqualTo(5));
            Assert.That(relationships[Npc.Bjorn].AtCap, Is.True);
        });
    }

    [Test]
    public void grant_records_source()
    {
        // The Gambler at level 3, where the next level costs 4, so a grant of 3 stays toward the next level.
        var relationships = new SimRelationships(new[] { new Relationship(Npc.Gambler, 3, 0, Array.Empty<string>(), null) });

        relationships.GrantRp(Npc.Gambler, 3, RpSource.Sacrifice);

        Assert.Multiple(() =>
        {
            Assert.That(relationships[Npc.Gambler].RpTowardNext, Is.EqualTo(3));
            Assert.That(relationships[Npc.Gambler].LastSource, Is.EqualTo(RpSource.Sacrifice));
            Assert.That(relationships.Grants, Is.EqualTo(new[] { new RpGrant(Npc.Gambler.Id, 3, RpSource.Sacrifice) }));
        });
    }
}
