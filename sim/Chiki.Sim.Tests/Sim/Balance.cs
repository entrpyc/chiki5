using Chiki.Sim;
using SimBalance = Chiki.Sim.Balance;
using SimBattle = Chiki.Sim.Battle;
using Stats = Chiki.Sim.RunStats;
using SimJudgment = Chiki.Sim.Judgment;

namespace Sim;

public class Balance
{
    /// <summary>The worked check of PRD 3.7.15 as balance inputs: AttackRatio 0.6, AvgCardDMG 10, all Perfect.</summary>
    private static readonly EncounterBalance WorkedCheck = new(1, 600, 10, JudgmentMix.AllPerfect);

    [Test]
    public void worked_check_360()
    {
        int hp = SimBalance.EnemyHp(
            actionsPerMinuteThousandths: 60 * Fixed.One,
            attackRatioThousandths: 600,
            avgCardDmg: 10,
            judgmentMix: JudgmentMix.AllPerfect,
            intendedSeconds: 60);

        Assert.That(hp, Is.EqualTo(360));
    }

    [Test]
    public void band_enforced_for_world_1()
    {
        var chart = TestContent.Chart(TestContent.Track(), 4);

        var over = EnemyValidator.Validate(TestContent.Enemy(chart, EncounterTier.Normal, EnemyRole.Aggressor, RhythmProfile.Fast, 45, damagePerHit: 16));
        var top = EnemyValidator.Validate(TestContent.Enemy(chart, EncounterTier.Normal, EnemyRole.Aggressor, RhythmProfile.Fast, 45, damagePerHit: 15));

        Assert.Multiple(() =>
        {
            Assert.That(over.Select(v => v.Rule), Is.EqualTo(new[] { EnemyValidator.RuleDamageBand }));
            Assert.That(top, Is.Empty, string.Join("\n", top));
        });
    }

    [Test]
    public void damage_rises_15_percent_per_world()
    {
        var enemy = TestContent.Enemy(TestContent.Chart(TestContent.Track(), 4), damagePerHit: 10);

        var world1 = new SimBattle(new Stats(), enemy, EncounterBalance.ForWorld(1), TestContent.Rng());
        var world3 = new SimBattle(new Stats(), enemy, EncounterBalance.ForWorld(3), TestContent.Rng());

        Assert.Multiple(() =>
        {
            Assert.That(world1.EnemyDamagePerHit, Is.EqualTo(10));
            Assert.That(world3.EnemyDamagePerHit, Is.EqualTo(13));
            Assert.That(SimBalance.DamageForWorld(10, 3), Is.EqualTo(13));
        });
    }

    [Test]
    public void tank_hp_is_1_2x_formula()
    {
        var chart = TestContent.SixtyPerMinuteChart();
        var tank = TestContent.Enemy(chart, EncounterTier.Normal, EnemyRole.Tank, RhythmProfile.Slow, 60, damagePerHit: 8);
        var aggressor = TestContent.Enemy(chart, EncounterTier.Normal, EnemyRole.Aggressor, RhythmProfile.Fast, 60, damagePerHit: 12);
        Assume.That(chart.ActionsPerMinuteThousandths, Is.EqualTo(60 * Fixed.One));

        var tankBattle = new SimBattle(new Stats(), tank, WorkedCheck, TestContent.Rng());
        var aggressorBattle = new SimBattle(new Stats(), aggressor, WorkedCheck, TestContent.Rng());

        Assert.Multiple(() =>
        {
            Assert.That(tankBattle.EnemyMaxHp, Is.EqualTo(432));
            Assert.That(tankBattle.EnemyHp, Is.EqualTo(432));
            Assert.That(aggressorBattle.EnemyMaxHp, Is.EqualTo(288));
            Assert.That(EncounterBalance.ForWorld(1).AvgCardDmg, Is.EqualTo(12));
            Assert.That(EncounterBalance.ForWorld(2).AvgCardDmg, Is.EqualTo(14));
            Assert.That(EncounterBalance.ForWorld(3).AvgCardDmg, Is.EqualTo(16));
        });
    }

    [Test]
    public void mistake_budget_lower_bounds()
    {
        var fixtures = TestContent.LoadFixtureEnemies();
        var budget = new Dictionary<EncounterTier, int>
        {
            [EncounterTier.Normal] = 10,
            [EncounterTier.Elite] = 6,
            [EncounterTier.Boss] = 4,
        };

        Assert.Multiple(() =>
        {
            foreach (var definition in fixtures.Enemies)
            {
                int maxDamage = SimBalance.DamageBand(definition.Role, definition.Tier).Max!.Value;
                var enemy = TestContent.WithDamage(definition, maxDamage);
                var stats = new Stats { Ard = Tuning.ArdBaseline };
                var battle = new SimBattle(stats, enemy, EncounterBalance.ForWorld(1), TestContent.Rng());

                int ardAfter = ArdAfterMistakes(battle, budget[definition.Tier]);

                Assert.That(ardAfter, Is.GreaterThan(0), $"{definition.Name} ({definition.Tier}) at damage {maxDamage}");
                Assert.That(battle.JudgmentLog.Where(e => e.Grade != null).Select(e => e.Grade), Has.All.EqualTo(SimJudgment.Miss), $"{definition.Name} answered better than a Miss");
            }
        });
    }

    /// <summary>
    /// Plays the battle taking only Misses: every attack action is answered with a press 100 ms
    /// late, a Miss inside the Judgment Window (PRD 3.3.3.1); where the window is clipped by a
    /// neighbouring action and the press is refused, the action goes unanswered, which costs the
    /// same as a Miss (PRD 3.3.4.2). Returns ARD after the given number of mistakes.
    /// </summary>
    private static int ArdAfterMistakes(SimBattle battle, int mistakes)
    {
        var leftSlots = new[] { TestContent.SlotE, TestContent.SlotR, new Slot(1, SlotKey.E), new Slot(1, SlotKey.R) };
        var rightSlots = new[] { TestContent.SlotU, new Slot(0, SlotKey.I), new Slot(1, SlotKey.U), new Slot(1, SlotKey.I) };
        int taken = 0;
        for (int index = 0; taken < mistakes && battle.Outcome is null; index++)
        {
            var opportunity = battle.OpportunityAt(index);
            if (!opportunity.Action.IsAttack)
            {
                battle.Advance(opportunity.CloseMs + 1);
                continue;
            }

            bool left = opportunity.Action.Kind == EnemyActionKind.AttackLeft;
            var slots = left ? leftSlots : rightSlots;
            var card = left ? TestContent.LeftAttack10 : TestContent.RightAttack(10);
            int pressAt = opportunity.CentreMs + 100;
            battle.Advance(pressAt - 1);
            var slot = slots.FirstOrDefault(s => battle.CooldownOf(s) == 0);
            if (slot != null)
            {
                battle.Press(slot, card, pressAt);
            }

            battle.Advance(opportunity.CloseMs + 1);
            taken++;
        }

        return battle.Stats.Ard;
    }
}
