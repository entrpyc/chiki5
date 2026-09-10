namespace Sim;

public class Architecture
{
    [Test]
    public void sim_references_no_engine()
    {
        var assembly = typeof(Chiki.Sim.RunStats).Assembly;

        var referenced = assembly.GetReferencedAssemblies().Select(n => n.Name ?? string.Empty).ToArray();

        Assert.That(
            referenced,
            Has.None.Matches<string>(n => n.StartsWith("UnityEngine") || n.StartsWith("UnityEditor")),
            "Chiki.Sim references: " + string.Join(", ", referenced));
    }
}
