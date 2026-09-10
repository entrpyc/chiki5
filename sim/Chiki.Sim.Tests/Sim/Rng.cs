using SimRng = Chiki.Sim.Rng;

namespace Sim;

public class Rng
{
    [Test]
    public void same_seed_same_sequence()
    {
        var a = new SimRng(42);
        var b = new SimRng(42);

        var fromA = Enumerable.Range(0, 1000).Select(_ => a.NextInt(0, 1_000_000)).ToArray();
        var fromB = Enumerable.Range(0, 1000).Select(_ => b.NextInt(0, 1_000_000)).ToArray();

        Assert.That(fromA, Is.EqualTo(fromB));
    }

    [Test]
    public void fork_is_independent()
    {
        var rng = new SimRng(42);
        var map = rng.Fork("map");
        var shop = rng.Fork("shop");
        int expectedShopNext = new SimRng(42).Fork("shop").NextInt(0, 1_000_000);
        int expectedMapNext = new SimRng(42).Fork("map").NextInt(0, 1_000_000);

        for (int i = 0; i < 100; i++)
        {
            map.NextInt(0, 1_000_000);
        }
        int shopNext = shop.NextInt(0, 1_000_000);

        var map2 = new SimRng(42).Fork("map");
        var shop2 = new SimRng(42).Fork("shop");
        for (int i = 0; i < 100; i++)
        {
            shop2.NextInt(0, 1_000_000);
        }
        int mapNext = map2.NextInt(0, 1_000_000);

        Assert.Multiple(() =>
        {
            Assert.That(shopNext, Is.EqualTo(expectedShopNext), "drawing from the map fork changed the shop fork");
            Assert.That(mapNext, Is.EqualTo(expectedMapNext), "drawing from the shop fork changed the map fork");
        });
    }
}
