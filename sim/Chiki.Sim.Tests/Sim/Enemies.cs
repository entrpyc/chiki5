using Chiki.Sim;
using Chiki.Sim.Data;
using SimBeats = Chiki.Sim.Beats;
using SimChart = Chiki.Sim.Chart;
using Stats = Chiki.Sim.RunStats;
using SimTrack = Chiki.Sim.Track;

namespace Sim;

public class Enemies
{
    private static (EnemySet Set, JsonValue Source, SimChart Chart) LoadTestSet()
    {
        var track = TrackLoader.FromJson(TestContent.ReadData("tracks/track-test-120.json"));
        var chart = ChartLoader.FromJson(TestContent.ReadData("charts/chart-test-thirty.json"), track);
        string json = TestContent.ReadData("enemies/test.json");
        var set = EnemyLoader.SetFromJson(json, new Dictionary<string, SimChart> { [chart.Id] = chart });
        return (set, JsonValue.Parse(json)["enemies"].Items[0], chart);
    }

    [Test]
    public void loads_definition_with_chart_reference()
    {
        var (set, source, chart) = LoadTestSet();

        var enemy = set.Enemies.Single();

        Assert.Multiple(() =>
        {
            Assert.That(enemy.Id, Is.EqualTo(source["id"].AsString()));
            Assert.That(enemy.Name, Is.EqualTo(source["name"].AsString()));
            Assert.That(TrackLoader.TierToId(enemy.Tier), Is.EqualTo(source["tier"].AsString()));
            Assert.That(EnemyLoader.RoleToId(enemy.Role), Is.EqualTo(source["role"].AsString()));
            Assert.That(EnemyLoader.ProfileToId(enemy.Profile!.Value), Is.EqualTo(source["profile"].AsString()));
            Assert.That(enemy.IntendedSeconds, Is.EqualTo(source["intendedSeconds"].AsInt()));
            Assert.That(enemy.TrackId, Is.EqualTo(source["track"].AsString()));
            Assert.That(enemy.ChartId, Is.EqualTo(source["chart"].AsString()));
            Assert.That(enemy.Chart, Is.SameAs(chart));
            Assert.That(enemy.Track.Id, Is.EqualTo(source["track"].AsString()));
            Assert.That(enemy.DamagePerHit, Is.EqualTo(source["damagePerHit"].AsInt()));
            Assert.That(enemy.Abilities.Select(EnemyLoader.AbilityToId), Is.EqualTo(source["abilities"].Items.Select(a => a.AsString())));
            Assert.That(enemy.Traits.Select(EnemyLoader.TraitToId), Is.EqualTo(source["traits"].Items.Select(t => t.AsString())));
            Assert.That(enemy.StatusesUsed.Select(ChartLoader.StatusToId), Is.EqualTo(source["statusesUsed"].Items.Select(s => s.AsString())));
            Assert.That(enemy.PortraitId, Is.EqualTo(source["portrait"].AsString()));
            Assert.That(enemy.QuoteLine, Is.EqualTo(source["quote"].AsString()));
            Assert.That(enemy.Phases, Is.Empty);
        });
    }

    [Test]
    public void intended_duration_inside_tier_band()
    {
        var chart = TestContent.Chart(TestContent.Track(), 4);

        var tooLong = EnemyValidator.Validate(TestContent.Enemy(chart, EncounterTier.Normal, EnemyRole.Aggressor, RhythmProfile.Fast, 75));
        var inBand = EnemyValidator.Validate(TestContent.Enemy(chart, EncounterTier.Normal, EnemyRole.Aggressor, RhythmProfile.Fast, 45));

        Assert.Multiple(() =>
        {
            Assert.That(tooLong.Select(v => v.Rule), Is.EqualTo(new[] { EnemyValidator.RuleDuration }));
            Assert.That(inBand, Is.Empty, string.Join("\n", inBand));
        });
    }

    [Test]
    public void role_multiplier()
    {
        Assert.Multiple(() =>
        {
            Assert.That(EnemyRole.Aggressor.HpMultiplierThousandths(), Is.EqualTo(800));
            Assert.That(EnemyRole.Tank.HpMultiplierThousandths(), Is.EqualTo(1200));
            Assert.That(EnemyRole.Mentalist.HpMultiplierThousandths(), Is.EqualTo(1000));
        });
    }

    [Test]
    public void profile_required()
    {
        var chart = TestContent.Chart(TestContent.Track(), 4);
        var enemy = TestContent.Enemy(chart, EncounterTier.Normal, EnemyRole.Aggressor, null, 45);

        var violations = EnemyValidator.Validate(enemy);

        Assert.That(violations.Select(v => v.Rule), Is.EqualTo(new[] { EnemyValidator.RuleProfile }));
    }

    [Test]
    public void upcoming_actions_with_countdown()
    {
        var battle = TestContent.Battle(16, 24); // AttackLeft at beat 4, another at beat 6
        battle.AdvanceToBeat(1);

        var upcoming = battle.UpcomingActions(horizonBeats: 8);

        Assert.Multiple(() =>
        {
            Assert.That(upcoming, Has.Count.EqualTo(2));
            Assert.That(upcoming[0].Action.Kind, Is.EqualTo(EnemyActionKind.AttackLeft));
            Assert.That(upcoming[0].PositionQb, Is.EqualTo(SimBeats.ToQuarterBeats(4)));
            Assert.That(upcoming[0].Beat, Is.EqualTo(4));
            Assert.That(upcoming[0].RemainingBeats, Is.EqualTo(3));
            Assert.That(upcoming[0].RemainingQb, Is.EqualTo(SimBeats.ToQuarterBeats(3)));
        });
    }

    [Test]
    public void normal_cannot_carry_two_abilities()
    {
        var chart = TestContent.Chart(TestContent.Track(), 4);
        var twoAbilities = new[] { EnemyAbility.RisingTempo, EnemyAbility.Pressure };

        var normal = EnemyValidator.Validate(TestContent.Enemy(chart, EncounterTier.Normal, EnemyRole.Aggressor, RhythmProfile.Fast, 45, twoAbilities));
        var boss = EnemyValidator.Validate(TestContent.Enemy(
            chart,
            EncounterTier.Boss,
            EnemyRole.Aggressor,
            RhythmProfile.Fast,
            120,
            twoAbilities,
            new[] { EnemyTrait.Guard, EnemyTrait.ThornsShell },
            new[] { StatusKind.Bleed }));

        Assert.Multiple(() =>
        {
            Assert.That(normal.Select(v => v.Rule), Is.EqualTo(new[] { EnemyValidator.RuleBudget }));
            Assert.That(normal[0].Message, Does.Contain("abilities"));
            Assert.That(boss, Is.Empty, string.Join("\n", boss));
        });
    }

    [Test]
    public void battle_tempo_from_enemy_track()
    {
        var track = new SimTrack("track-140", 1, EncounterTier.Normal, 32, 0, new TempoMap(140));
        var enemy = TestContent.Enemy(TestContent.Chart(track, 16)); // AttackLeft at beat 4

        var battle = TestContent.Battle(new Stats(), enemy.Chart);

        Assert.Multiple(() =>
        {
            Assert.That(battle.Track, Is.SameAs(track));
            Assert.That(battle.BeatMap.BpmAt(0), Is.EqualTo(140));
            Assert.That(battle.BeatMap.TimeAtBeat(4), Is.EqualTo(4 * 60_000 / 140));
            Assert.That(battle.Opportunities[0].CentreMs, Is.EqualTo(battle.BeatMap.TimeAtBeat(4)));
        });
    }

    [Test]
    public void chart_must_match_track()
    {
        var chart = TestContent.Chart(TestContent.Track(), 4); // written for "track-test"
        var enemy = TestContent.Enemy(chart, EncounterTier.Normal, EnemyRole.Aggressor, RhythmProfile.Fast, 45, trackId: "track-other");

        var violations = EnemyValidator.Validate(enemy);

        Assert.That(violations.Select(v => v.Rule), Is.EqualTo(new[] { EnemyValidator.RuleTrack }));
    }
}
