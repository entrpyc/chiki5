using SimBattle = Chiki.Sim.Battle;
using Stats = Chiki.Sim.RunStats;

namespace Sim;

public class RunStats
{
    [Test]
    public void ard_defaults_to_300()
    {
        var stats = new Stats();

        Assert.Multiple(() =>
        {
            Assert.That(stats.MaxArd, Is.EqualTo(300));
            Assert.That(stats.Ard, Is.EqualTo(300));
        });
    }

    [Test]
    public void ard_carries_between_battles()
    {
        var stats = new Stats { Ard = 120 };

        var enemy = TestContent.Enemy(TestContent.Track(), 4);
        var first = new SimBattle(stats, enemy, TestContent.DefaultEnemyHp, TestContent.Rng());
        var second = new SimBattle(stats, enemy, TestContent.DefaultEnemyHp, TestContent.Rng());

        Assert.That(second.Stats.Ard, Is.EqualTo(120));
    }

    [Test]
    public void raising_max_ard_adds_to_current()
    {
        var stats = new Stats { Ard = 250 };

        stats.RaiseMaxArd(20);

        Assert.Multiple(() =>
        {
            Assert.That(stats.Ard, Is.EqualTo(270));
            Assert.That(stats.MaxArd, Is.EqualTo(320));
        });
    }
}
