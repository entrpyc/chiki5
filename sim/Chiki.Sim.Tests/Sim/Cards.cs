using Chiki.Sim;
using Chiki.Sim.Data;
using Chiki.Sim.Effects;
using Stats = Chiki.Sim.RunStats;

namespace Sim;

public class Cards
{
    private const string FullCardJson = """
        {
          "id": "card-ember-fang",
          "name": "Ember Fang",
          "category": "left-attack",
          "rarity": "rare",
          "class": "unstable",
          "value": 18,
          "cooldownBeats": 4,
          "effects": [
            { "trigger": "on-play", "modifier": "apply-status", "status": "bleed", "stacks": 2, "target": "enemy" },
            { "trigger": "on-play", "condition": "on-perfect", "modifier": "add-value", "amount": 5 },
            { "trigger": "damage-taken", "modifier": "apply-status", "status": "thorns", "statusValue": 3, "target": "player" },
            { "trigger": "on-play", "condition": "if-kills", "modifier": "change-stat", "stat": "ard", "amount": 6, "target": "player" },
            { "trigger": "passive", "modifier": "multiply-value", "value": "damage-dealt", "amount": 2000, "lifetime": "beats", "beats": 3 }
          ],
          "specialRules": "Bites twice on a Perfect.",
          "upgradeStep": 3,
          "lifespan": 2,
          "flavorText": "Griit lit the first fang from the last ember.",
          "unlockSource": "boss"
        }
        """;

    [Test]
    public void loads_definition_from_json()
    {
        var card = CardLoader.CardFromJson(JsonValue.Parse(FullCardJson));

        Assert.Multiple(() =>
        {
            Assert.That(card.Id, Is.EqualTo("card-ember-fang"));
            Assert.That(card.Name, Is.EqualTo("Ember Fang"));
            Assert.That(card.Category, Is.EqualTo(CardCategory.LeftAttack));
            Assert.That(card.Rarity, Is.EqualTo(CardRarity.Rare));
            Assert.That(card.Class, Is.EqualTo(CardClass.Unstable));
            Assert.That(card.Value, Is.EqualTo(18));
            Assert.That(card.CooldownBeats, Is.EqualTo(4));
            Assert.That(card.Effects, Has.Count.EqualTo(5));
            Assert.That(card.Effects[0], Is.EqualTo(new EffectDefinition(EffectTrigger.OnPlay, EffectModifier.ApplyStatus, status: new StatusApplication(StatusKind.Bleed, 2))));
            Assert.That(card.Effects[1], Is.EqualTo(new EffectDefinition(EffectTrigger.OnPlay, EffectModifier.AddValue, 5, EffectCondition.OnPerfect)));
            Assert.That(card.Effects[2], Is.EqualTo(new EffectDefinition(EffectTrigger.DamageTaken, EffectModifier.ApplyStatus, target: StatusTarget.Player, status: new StatusApplication(StatusKind.Thorns, 1, 3))));
            Assert.That(card.Effects[3], Is.EqualTo(new EffectDefinition(EffectTrigger.OnPlay, EffectModifier.ChangeStat, 6, EffectCondition.IfKills, StatusTarget.Player, stat: RunStat.Ard)));
            Assert.That(card.Effects[4], Is.EqualTo(new EffectDefinition(EffectTrigger.Passive, EffectModifier.MultiplyValue, 2000, value: EffectValue.DamageDealt, lifetime: EffectLifetime.Beats, lifetimeBeats: 3)));
            Assert.That(card.SpecialRules, Is.EqualTo("Bites twice on a Perfect."));
            Assert.That(card.UpgradeStep, Is.EqualTo(3));
            Assert.That(card.Lifespan, Is.EqualTo(2));
            Assert.That(card.FlavorText, Is.EqualTo("Griit lit the first fang from the last ember."));
            Assert.That(card.UnlockSource, Is.EqualTo(UnlockSource.Boss));
        });
    }

    [Test]
    public void category_to_slots()
    {
        var card = TestContent.Defense(8);

        var slots = card.LegalSlots;

        Assert.That(slots, Is.EquivalentTo(new[]
        {
            new Slot(0, SlotKey.L), new Slot(0, SlotKey.Semicolon),
            new Slot(1, SlotKey.L), new Slot(1, SlotKey.Semicolon),
        }));
    }

    [Test]
    public void rarity_band_enforced()
    {
        var tooHigh = CardValidator.Validate(Attack(damage: 14));
        var inBand = CardValidator.Validate(Attack(damage: 12));

        Assert.Multiple(() =>
        {
            Assert.That(tooHigh, Has.Count.EqualTo(1));
            Assert.That(tooHigh[0].Rule, Is.EqualTo(CardValidator.RuleRarityBand));
            Assert.That(tooHigh[0].Message, Does.Contain("8–12"));
            Assert.That(inBand, Is.Empty);
        });
    }

    [Test]
    public void cooldown_must_be_2_to_6()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CardValidator.Validate(Attack(cooldown: 7)).Select(v => v.Rule), Is.EqualTo(new[] { CardValidator.RuleAnatomy }));
            Assert.That(CardValidator.Validate(Attack(cooldown: 2)), Is.Empty);
            Assert.That(CardValidator.Validate(Attack(cooldown: 6)), Is.Empty);
        });
    }

    [Test]
    public void stun_needs_rare_attack()
    {
        var stun = new EffectDefinition(EffectTrigger.OnPlay, EffectModifier.ApplyStatus, status: new StatusApplication(StatusKind.Stun));

        var uncommon = CardValidator.Validate(Attack(damage: 14, rarity: CardRarity.Uncommon, effects: new[] { stun }));
        var rare = CardValidator.Validate(Attack(damage: 18, rarity: CardRarity.Rare, effects: new[] { stun }));

        Assert.Multiple(() =>
        {
            Assert.That(uncommon.Select(v => v.Rule), Is.EqualTo(new[] { CardValidator.RuleEffectCompatibility }));
            Assert.That(rare, Is.Empty);
        });
    }

    [Test]
    public void thorns_never_on_attack()
    {
        var thorns = new EffectDefinition(EffectTrigger.OnPlay, EffectModifier.ApplyStatus, target: StatusTarget.Player, status: new StatusApplication(StatusKind.Thorns, 1, 3));
        var damageByRarity = new Dictionary<CardRarity, int>
        {
            [CardRarity.Common] = 10,
            [CardRarity.Uncommon] = 14,
            [CardRarity.Rare] = 18,
            [CardRarity.Legendary] = 24,
        };

        Assert.Multiple(() =>
        {
            foreach (var (rarity, damage) in damageByRarity)
            {
                var violations = CardValidator.Validate(Attack(damage: damage, rarity: rarity, effects: new[] { thorns }));
                Assert.That(violations.Select(v => v.Rule), Is.EqualTo(new[] { CardValidator.RuleEffectCompatibility }), rarity.ToString());
            }
        });
    }

    [Test]
    public void unstable_requires_lifespan()
    {
        var unstableNoLifespan = CardValidator.Validate(Attack(cardClass: CardClass.Unstable));
        var normalWithLifespan = CardValidator.Validate(Attack(cardClass: CardClass.Normal, lifespan: 2));

        Assert.Multiple(() =>
        {
            Assert.That(unstableNoLifespan.Select(v => v.Rule), Is.EqualTo(new[] { CardValidator.RuleClass }));
            Assert.That(normalWithLifespan.Select(v => v.Rule), Is.EqualTo(new[] { CardValidator.RuleClass }));
        });
    }

    [Test]
    public void resolution_uses_cardvalue_only()
    {
        var card = CardLoader.CardFromJson(JsonValue.Parse("""
            { "id": "card-jab", "name": "Jab", "category": "left-attack", "rarity": "common", "class": "normal",
              "value": 10, "cooldownBeats": 2, "unlockSource": "starter" }
            """));
        var battle = TestContent.Battle(new Stats { BaseDmg = 2 }, TestContent.Chart(TestContent.Track(), 4));
        battle.Press(TestContent.SlotD, card, 500 + 60); // Good

        battle.AdvanceToBeat(2);

        // (10 + 2) x 50% = 6, computed by the battle from CardValue and Base DMG alone.
        Assert.That(battle.Events.OfType<DamageDealt>().Single().Amount, Is.EqualTo(6));
    }

    [Test]
    public void shipped_sets_validate()
    {
        var setsDir = Path.Combine(TestContent.RepoRoot, "data", "sets");
        var files = Directory.Exists(setsDir) ? Directory.GetFiles(setsDir, "*.json") : Array.Empty<string>();

        var violations = files
            .Select(f => CardLoader.SetFromJson(File.ReadAllText(f)))
            .SelectMany(s => CardValidator.Validate(s).Select(v => $"{s.Id}/{v}"))
            .ToList();

        Assert.That(violations, Is.Empty, string.Join("\n", violations));
    }

    [Test]
    public void validator_reports_all_violations()
    {
        var set = new CardSet("set-broken", new[]
        {
            Attack(id: "card-too-strong", damage: 30),
            Attack(id: "card-slow", cooldown: 9),
            Attack(id: "card-forever", cardClass: CardClass.Unstable),
            Attack(id: "card-fine"),
        });

        var violations = CardValidator.Validate(set);

        Assert.Multiple(() =>
        {
            Assert.That(violations, Has.Count.EqualTo(3));
            Assert.That(violations.Select(v => (v.CardId, v.Rule)), Is.EquivalentTo(new[]
            {
                ("card-too-strong", CardValidator.RuleRarityBand),
                ("card-slow", CardValidator.RuleAnatomy),
                ("card-forever", CardValidator.RuleClass),
            }));
        });
    }

    /// <summary>A Left Attack that is valid unless an argument says otherwise: Common, 10 damage, cooldown 2, Normal.</summary>
    private static CardDefinition Attack(
        string id = "card-attack",
        int damage = 10,
        int cooldown = 2,
        CardRarity rarity = CardRarity.Common,
        CardClass cardClass = CardClass.Normal,
        int? lifespan = null,
        IReadOnlyList<EffectDefinition>? effects = null)
    {
        return new CardDefinition(id, "Attack", CardCategory.LeftAttack, damage, cooldown, rarity, cardClass, effects, lifespan: lifespan);
    }
}
