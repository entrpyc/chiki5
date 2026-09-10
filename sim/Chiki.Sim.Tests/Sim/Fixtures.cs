using Chiki.Sim;
using Chiki.Sim.Data;

namespace Sim;

public class Fixtures
{
    [Test]
    public void starter_set_validates_and_fills_two_lines()
    {
        var set = CardLoader.SetFromJson(TestContent.ReadData("sets/starter.json"));
        Assume.That(() => TrackLoader.FromJson(TestContent.ReadData("tracks/fixture-120.json")), Throws.Nothing);

        var violations = CardValidator.Validate(set);
        var perCategory = set.Cards.GroupBy(c => c.Category).ToDictionary(g => g.Key, g => g.Count());

        Assert.Multiple(() =>
        {
            Assert.That(violations, Is.Empty, string.Join("\n", violations));
            foreach (CardCategory category in Enum.GetValues(typeof(CardCategory)))
            {
                Assert.That(perCategory.GetValueOrDefault(category), Is.GreaterThanOrEqualTo(4), category.ToString());
            }
        });
    }
}
