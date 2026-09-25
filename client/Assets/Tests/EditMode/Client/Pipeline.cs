#nullable enable
using Chiki.Client.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Client
{
    /// <summary>
    /// The client test runner (P1.1). <c>tools/run-client-tests.ps1</c> runs this suite and then
    /// the PlayMode one, in two batch-mode invocations, writing <c>results-editmode.xml</c> and
    /// <c>results-playmode.xml</c>. This test passing in the first file is what proves the
    /// EditMode half runs at all.
    /// </summary>
    public class Pipeline
    {
        [Test]
        public void editmode_suite_runs()
        {
            Assert.That(GetType().Assembly.GetName().Name, Is.EqualTo("Chiki.Client.Tests.EditMode"), "this test does not live in the EditMode assembly");
            Assert.That(Application.isPlaying, Is.False, "the EditMode suite ran in play mode");
            Assert.That(typeof(SpriteAssetPostprocessor).Assembly.GetName().Name, Is.EqualTo("Chiki.Client.Editor"), "the EditMode assembly cannot see Chiki.Client.Editor, where the asset checks live");
        }
    }
}
