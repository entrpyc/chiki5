using Chiki.Sim;
using Stats = Chiki.Sim.RunStats;

namespace Sim;

public class Crp
{
    /// <summary>Moves forward along the first connection until the given number of transitions is made; stops without a battle since every node this plan enters is a stop or a battle left unfought only when moving on is refused.</summary>
    private static int Transitions(Chiki.Sim.Run run, int count)
    {
        int made = 0;
        while (made < count)
        {
            if (!run.CurrentNodeCompleted)
            {
                run.CompleteNode();
            }

            if (run.MoveTo(run.ForwardNodes[0].Id) != MoveResult.Moved)
            {
                break;
            }

            made++;
        }

        return made;
    }

    [Test]
    public void plus_one_per_transition()
    {
        var run = new RunSetup(Array.Empty<string>()).Start(TestContent.LoadRunContent(), "chiki-1");
        int before = run.Stats.Crp;

        int made = Transitions(run, 5);

        Assert.Multiple(() =>
        {
            Assert.That(before, Is.EqualTo(0));
            Assert.That(made, Is.EqualTo(5));
            Assert.That(run.Stats.Crp, Is.EqualTo(5));
        });
    }

    [Test]
    public void change_event_has_source()
    {
        var run = new RunSetup(Array.Empty<string>()).Start(TestContent.LoadRunContent(), "chiki-1");

        run.MoveTo(run.ForwardNodes[0].Id);
        var last = run.Events.OfType<CrpChanged>().Last();

        Assert.Multiple(() =>
        {
            Assert.That(last.Amount, Is.EqualTo(1));
            Assert.That(last.Source, Is.EqualTo("node transition"));
            Assert.That(last.Total, Is.EqualTo(run.Stats.Crp));
        });
    }

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
