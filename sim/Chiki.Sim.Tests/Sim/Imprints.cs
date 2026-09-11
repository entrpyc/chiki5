using Chiki.Sim;
using Chiki.Sim.Data;
using Chiki.Sim.Effects;

namespace Sim;

public class Imprints
{
    [Test]
    public void fixtures_load_by_tier()
    {
        var set = ImprintLoader.SetFromJson(TestContent.ReadData("imprints/fixtures.json"));
        var battle = TestContent.Battle(4);

        var registered = new List<RegisteredEffect>();
        foreach (var imprint in set.Imprints)
        {
            foreach (var effect in imprint.Effects)
            {
                registered.Add(battle.RegisterEffect(imprint.Id, effect));
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(set.Imprints, Has.Count.EqualTo(6));
            Assert.That(set.Imprints.Select(i => i.Id), Is.Unique);
            foreach (ImprintTier tier in Enum.GetValues(typeof(ImprintTier)))
            {
                Assert.That(set.OfTier(tier), Has.Count.EqualTo(2), tier.ToString());
            }

            Assert.That(registered, Has.Count.EqualTo(set.Imprints.Sum(i => i.Effects.Count)));
            Assert.That(battle.Effects.Registered.Where(r => r.OwnerId.StartsWith("imprint-")), Is.EquivalentTo(registered));
            Assert.That(set.Imprints.Select(i => i.Source), Has.All.EqualTo(ImprintSource.Pool));
        });
    }
}
