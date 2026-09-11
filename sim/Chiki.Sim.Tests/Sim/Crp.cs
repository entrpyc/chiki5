using Stats = Chiki.Sim.RunStats;

namespace Sim;

public class Crp
{
    [Test]
    public void clamped_0_to_100()
    {
        var high = new Stats { Crp = 98 };
        var low = new Stats { Crp = 2 };

        high.Crp += 5;
        low.Crp += -5;

        Assert.Multiple(() =>
        {
            Assert.That(high.Crp, Is.EqualTo(100));
            Assert.That(low.Crp, Is.EqualTo(0));
        });
    }
}
