using Chiki.Sim;
using Chiki.Sim.Data;

namespace Sim;

public class Save
{
    [Test]
    public void migrates_old_version()
    {
        var content = TestContent.LoadRunContent();
        var run = new RunSetup(Array.Empty<string>()).Start(content, "chiki-1");
        run.Stats.Essence = 40;
        var current = JsonValue.Parse(RunSerializer.ToJson(run));
        Assume.That(current["schemaVersion"].AsInt(), Is.EqualTo(3));
        current.Set("schemaVersion", JsonValue.Of(1));
        // A version-1 file holds its Defense cards in O/P and Ability cards in Q/W; the version-2 step moves them.
        foreach (var slot in current["loadout"].Items)
        {
            slot.Set("key", JsonValue.Of(OldKey(slot["key"].AsString())));
        }
        current.Remove("route");
        current.Remove("battles");
        current.Remove("pendingReward");
        string oldFile = current.ToJson();
        Assume.That(oldFile, Does.Not.Contain("\"route\""));

        var loaded = RunSerializer.FromJson(oldFile, content);
        var migrated = JsonValue.Parse(RunSerializer.Migrate(oldFile));
        var rewritten = JsonValue.Parse(RunSerializer.ToJson(loaded));

        Assert.Multiple(() =>
        {
            Assert.That(loaded.Seed, Is.EqualTo("chiki-1"));
            Assert.That(loaded.Stats.Essence, Is.EqualTo(40));
            Assert.That(loaded.Battles, Is.Empty, "the battle records default to none");
            Assert.That(loaded.PendingReward, Is.Null, "the open offer defaults to none");
            Assert.That(loaded.Route, Is.EqualTo(new[] { loaded.CurrentNodeId }), "the route defaults to the node stood on");
            Assert.That(migrated["schemaVersion"].AsInt(), Is.EqualTo(3));
            Assert.That(migrated["route"].Items, Is.Empty);
            Assert.That(migrated["pendingReward"].IsNull, Is.True);
            Assert.That(rewritten["schemaVersion"].AsInt(), Is.EqualTo(3));
        });
    }

    /// <summary>A version-2 run keeps every card, with the Defense and Ability cards moved to their new keys (PRD 3.3.2.1).</summary>
    [Test]
    public void migrates_defense_and_ability_keys()
    {
        var content = TestContent.LoadRunContent();
        var run = new RunSetup(Array.Empty<string>()).Start(content, "chiki-1");
        var current = JsonValue.Parse(RunSerializer.ToJson(run));
        var before = current["loadout"].Items.ToDictionary(s => s["key"].AsString() + s["line"].AsInt(), s => s["card"].AsInt());
        Assume.That(before.ContainsKey("q0") && before.ContainsKey("o0"), "the starter loadout fills Q and O on line 1");
        current.Set("schemaVersion", JsonValue.Of(2));
        foreach (var slot in current["loadout"].Items)
        {
            slot.Set("key", JsonValue.Of(OldKey(slot["key"].AsString())));
        }

        var loaded = RunSerializer.FromJson(current.ToJson(), content);
        var after = JsonValue.Parse(RunSerializer.ToJson(loaded))["loadout"].Items.ToDictionary(s => s["key"].AsString() + s["line"].AsInt(), s => s["card"].AsInt());

        Assert.Multiple(() =>
        {
            Assert.That(after, Is.EquivalentTo(before), "a card changed slot or was lost in the migration");
            foreach (var slot in loaded.Loadout.Slots)
            {
                var card = loaded.Loadout[slot];
                Assert.That(card == null || card.Definition.Category.Allows(slot.Key), Is.True, slot + " holds a card of the wrong Category");
            }
        });
    }

    /// <summary>Where a card sat before the key swap of schema 3: Q↔O and W↔P.</summary>
    private static string OldKey(string key)
    {
        switch (key)
        {
            case "q": return "o";
            case "w": return "p";
            case "o": return "q";
            case "p": return "w";
            default: return key;
        }
    }

    [Test]
    public void refuses_newer_version()
    {
        var content = TestContent.LoadRunContent();
        var run = new RunSetup(Array.Empty<string>()).Start(content, "chiki-1");
        var document = JsonValue.Parse(RunSerializer.ToJson(run));
        document.Set("schemaVersion", JsonValue.Of(99));
        string folder = Path.Combine(TestContext.CurrentContext.WorkDirectory, "save-" + TestContext.CurrentContext.Random.GetString(8));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "run.json");
        File.WriteAllText(path, document.ToJson());
        var bytesBefore = File.ReadAllBytes(path);
        var writtenAt = File.GetLastWriteTimeUtc(path);

        try
        {
            Assert.Multiple(() =>
            {
                Assert.That(() => RunSerializer.FromFile(path, content), Throws.TypeOf<SaveVersionException>().With.Message.Contains("99"));
                Assert.That(() => RunSerializer.Migrate(File.ReadAllText(path)), Throws.TypeOf<SaveVersionException>());
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(bytesBefore), "the file was altered");
                Assert.That(File.GetLastWriteTimeUtc(path), Is.EqualTo(writtenAt), "the file was rewritten");
            });
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
