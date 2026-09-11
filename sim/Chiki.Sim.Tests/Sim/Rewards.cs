using Chiki.Sim;
using SimRewards = Chiki.Sim.Rewards;
using SimRng = Chiki.Sim.Rng;

namespace Sim;

public class Rewards
{
    private const int Wins = 200;

    [Test]
    public void essence_inside_band()
    {
        var content = TestContent.LoadRunContent();

        var world1 = Enumerable.Range(0, Wins).Select(i => IncomeOfWin(content, 1, "w1-" + i + "-")).ToList();
        var world3 = Enumerable.Range(0, Wins).Select(i => IncomeOfWin(content, 3, "w3-" + i + "-")).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(world1, Has.All.InRange(8, 17));
            Assert.That(world3, Has.All.InRange(18, 37));
        });
    }

    [Test]
    public void pick_one_of_three_into_binder()
    {
        var content = TestContent.LoadRunContent();
        var run = TestContent.RunAtNormalNode(content);
        TestContent.WinNodeBattle(run);
        var offer = run.PendingReward;
        Assume.That(offer, Is.Not.Null);
        var offered = offer!.Cards;
        var countsBefore = offered.Select(c => CountOf(run, c)).ToList();

        var picked = run.PickReward(offered[0]);
        var countsAfter = offered.Select(c => CountOf(run, c)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(offered, Has.Count.EqualTo(3));
            Assert.That(offered.Select(c => c.Id), Is.Unique);
            Assert.That(offered.Select(c => c.Rarity), Has.All.EqualTo(CardRarity.Common).Or.EqualTo(CardRarity.Uncommon));
            Assert.That(run.Binder.Contains(picked), Is.True);
            Assert.That(picked.Definition, Is.EqualTo(offered[0]));
            Assert.That(countsAfter[0], Is.EqualTo(countsBefore[0] + 1));
            Assert.That(countsAfter[1], Is.EqualTo(countsBefore[1]), "the unpicked card did not enter the Binder");
            Assert.That(countsAfter[2], Is.EqualTo(countsBefore[2]), "the unpicked card did not enter the Binder");
            Assert.That(run.PendingReward, Is.Null);
        });
    }

    [Test]
    public void pool_is_normal_class()
    {
        var normal = new[]
        {
            new CardDefinition("card-normal-common", "Common", CardCategory.LeftAttack, 10),
            new CardDefinition("card-normal-uncommon", "Uncommon", CardCategory.RightAttack, 14, rarity: CardRarity.Uncommon),
            new CardDefinition("card-normal-rare", "Rare", CardCategory.Defense, 18, rarity: CardRarity.Rare),
        };
        var others = new[]
        {
            new CardDefinition("card-event", "Event", CardCategory.Ability, 0, cardClass: CardClass.Event),
            new CardDefinition("card-unstable", "Unstable", CardCategory.Defense, 8, cardClass: CardClass.Unstable, lifespan: 2),
        };
        var set = new CardSet("mixed", normal.Concat(others).ToArray());

        var pool = SimRewards.Pool(set.Cards);

        Assert.That(pool.Select(c => c.Id), Is.EquivalentTo(normal.Select(c => c.Id)));
    }

    [Test]
    public void event_class_never_in_offers()
    {
        var events = new[]
        {
            new CardDefinition("card-event-1", "Event 1", CardCategory.Ability, 0, cardClass: CardClass.Event),
            new CardDefinition("card-event-2", "Event 2", CardCategory.LeftAttack, 10, cardClass: CardClass.Event),
            new CardDefinition("card-event-3", "Event 3", CardCategory.Defense, 8, rarity: CardRarity.Uncommon, cardClass: CardClass.Event),
        };
        var content = TestContent.LoadRunContent(new CardSet("events", events));
        var eventIds = events.Select(e => e.Id).ToHashSet();

        var offeredEvents = Enumerable.Range(0, 500)
            .SelectMany(i => SimRewards.RollNormal(new SimRng("offer-" + i), content.Cards.Values, 1, "node").Cards)
            .Where(c => eventIds.Contains(c.Id) || c.Class == CardClass.Event)
            .ToList();

        Assert.That(offeredEvents, Is.Empty);
    }

    [Test]
    public void elite_reward_card_imprint_essence()
    {
        var content = TestContent.LoadRunContent(HighRaritySet());
        var run = TestContent.RunAtNode(content, NodeType.Elite);
        int imprintsBefore = run.Imprints.Count;
        int essenceBefore = run.Stats.Essence;

        TestContent.WinNodeBattle(run);
        var offer = run.PendingReward;
        Assume.That(offer, Is.Not.Null);
        var picked = run.PickReward(offer!.Cards[0]);

        Assert.Multiple(() =>
        {
            Assert.That(offer.Tier, Is.EqualTo(EncounterTier.Elite));
            Assert.That(offer.Cards, Has.Count.EqualTo(1));
            Assert.That(offer.Cards[0].Rarity, Is.EqualTo(CardRarity.Rare).Or.EqualTo(CardRarity.Legendary));
            Assert.That(run.Binder.Contains(picked), Is.True);
            Assert.That(run.Imprints, Has.Count.EqualTo(imprintsBefore + 1));
            Assert.That(run.Imprints[^1], Is.EqualTo(offer.ImprintId));
            Assert.That(run.Stats.Essence - essenceBefore, Is.InRange(25, 35));
            Assert.That(run.PendingReward, Is.Null);
        });
    }

    [Test]
    public void boss_reward_card_imprint_essence()
    {
        var content = TestContent.LoadRunContent(HighRaritySet());
        var run = TestContent.RunAtBoss(content);
        int imprintsBefore = run.Imprints.Count;
        int essenceBefore = run.Stats.Essence;

        TestContent.WinNodeBattle(run);
        var offer = run.PendingReward;
        Assume.That(offer, Is.Not.Null);
        var picked = run.PickReward(offer!.Cards[0]);

        Assert.Multiple(() =>
        {
            Assert.That(offer.Tier, Is.EqualTo(EncounterTier.Boss));
            Assert.That(offer.Cards, Has.Count.EqualTo(1));
            Assert.That(offer.Cards[0].Rarity, Is.EqualTo(CardRarity.Rare).Or.EqualTo(CardRarity.Legendary));
            Assert.That(run.Binder.Contains(picked), Is.True);
            Assert.That(run.Imprints, Has.Count.EqualTo(imprintsBefore + 1));
            Assert.That(run.Imprints[^1], Is.EqualTo(offer.ImprintId));
            Assert.That(run.Stats.Essence - essenceBefore, Is.InRange(40, 50));
            Assert.That(run.Events.OfType<BossDefeated>().Count(), Is.EqualTo(1));
        });
    }

    /// <summary>A set of Rare and Legendary Normal-class cards, the pool an Elite or Boss reward draws from (PRD 3.7.3, 3.7.4).</summary>
    private static CardSet HighRaritySet() => new("high", new[]
    {
        new CardDefinition("card-rare-left", "Rare Left", CardCategory.LeftAttack, 18, rarity: CardRarity.Rare),
        new CardDefinition("card-rare-guard", "Rare Guard", CardCategory.Defense, 18, rarity: CardRarity.Rare),
        new CardDefinition("card-legendary-right", "Legendary Right", CardCategory.RightAttack, 24, rarity: CardRarity.Legendary),
    });

    /// <summary>The Essence a fresh run holds after winning its first Normal node in the World: the one income it received.</summary>
    private static int IncomeOfWin(RunContent content, int world, string seedPrefix)
    {
        var run = TestContent.RunAtNormalNode(content, world: world, seedPrefix: seedPrefix);
        TestContent.WinNodeBattle(run);
        return run.Stats.Essence;
    }

    private static int CountOf(Chiki.Sim.Run run, CardDefinition definition) => run.Binder.Cards.Count(c => c.Definition.Id == definition.Id);
}
