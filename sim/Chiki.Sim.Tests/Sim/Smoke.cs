// Fixtures are named exactly as the plan's Suite: "Sim.Smoke / library_loads" is
// namespace Sim, class Smoke, method library_loads.
namespace Sim;

public class Smoke
{
    [Test]
    public void library_loads()
    {
        var stats = new Chiki.Sim.RunStats();

        Assert.That(stats, Is.Not.Null);
    }
}
