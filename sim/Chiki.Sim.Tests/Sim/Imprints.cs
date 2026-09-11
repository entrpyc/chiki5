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

    [Test]
    public void no_limit_and_lost_at_end()
    {
        var run = new RunSetup(Array.Empty<string>()).Start(TestContent.LoadRunContent(), "chiki-1");
        var tiers = new[]
        {
            ImprintTier.Common, ImprintTier.Uncommon, ImprintTier.Rare,
            ImprintTier.Common, ImprintTier.Uncommon, ImprintTier.Rare, ImprintTier.Common,
        };

        var acquired = tiers.Select(run.AcquireImprint).ToList();
        var held = run.Imprints.ToList();
        var registered = run.Effects.Registered.ToList();
        run.End(RunStatus.Won);
        var next = new RunSetup(Array.Empty<string>()).Start(TestContent.LoadRunContent(), "chiki-2");

        Assert.Multiple(() =>
        {
            Assert.That(held, Is.EqualTo(acquired.Select(i => i.Id)), "every Imprint acquired is held; there is no slot limit");
            Assert.That(held, Has.Count.EqualTo(7));
            int expected = acquired.GroupBy(i => i.Id).Sum(g => (g.First().Stackable ? g.Count() : 1) * g.First().Effects.Count);
            Assert.That(registered, Has.Count.EqualTo(expected), "every held Imprint's effects are registered, stackable ones per copy");
            Assert.That(registered.Select(r => r.OwnerId).Distinct(), Is.EquivalentTo(held.Distinct()));
            Assert.That(run.Imprints, Is.Empty, "the ended run lost its Imprints");
            Assert.That(run.Effects.Registered, Is.Empty);
            Assert.That(next.Imprints, Is.Empty);
            Assert.That(next.Effects.Registered, Is.Empty);
        });
    }

    [Test]
    public void stackable_stacks()
    {
        var run = new RunSetup(Array.Empty<string>()).Start(TestContent.LoadRunContent(), "chiki-1");
        var keenEdge = run.Content.FindImprint("imprint-keen-edge")!;

        run.AcquireImprint(keenEdge.Id);
        run.AcquireImprint(keenEdge.Id);

        Assert.Multiple(() =>
        {
            Assert.That(keenEdge.Stackable, Is.True);
            Assert.That(keenEdge.Effects.Single().Stat, Is.EqualTo(RunStat.BaseDmg));
            Assert.That(keenEdge.Effects.Single().Amount, Is.EqualTo(2));
            Assert.That(run.Stats.BaseDmg, Is.EqualTo(4));
            Assert.That(run.Effects.Registered.Count(r => r.OwnerId == keenEdge.Id), Is.EqualTo(2));
        });
    }
}
