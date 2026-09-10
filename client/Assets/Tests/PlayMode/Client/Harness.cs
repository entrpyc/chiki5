#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Chiki.Client.Driver;
using Chiki.Sim;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Client
{
    public class Harness
    {
        /// <summary>Collects the events the driver hands to presenters, in order.</summary>
        private sealed class RecordingPresenter : IBattlePresenter
        {
            public List<BattleEvent> Events { get; } = new List<BattleEvent>();

            public void OnBattleEvent(Chiki.Sim.Battle battle, BattleEvent battleEvent)
            {
                Events.Add(battleEvent);
            }
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator scripted_inputs_reach_sim()
        {
            // Attacks on beats 1, 2 and 3 answered by presses stamped 20 ms late on three different slots.
            var rig = ClientTestContent.ScheduledRig("harness", 4, 8, 12);
            var battle = rig.Driver.Battle!;
            var presenter = new RecordingPresenter();
            rig.Driver.AttachPresenter(presenter);
            rig.Driver.Script(new[]
            {
                new ScriptedInput(520, ClientTestContent.SlotD, ClientTestContent.LeftAttack10),
                new ScriptedInput(1020, ClientTestContent.SlotF, ClientTestContent.LeftAttack10),
                new ScriptedInput(1520, ClientTestContent.SlotDLine2, ClientTestContent.LeftAttack10),
            });

            yield return rig.WaitUntilAudioMs(1800);

            Assert.That(battle.JudgmentLog, Has.Count.EqualTo(3));
            Assert.That(battle.JudgmentLog.Select(e => e.Grade), Has.All.Not.Null, "an action went unanswered");
            Assert.That(battle.JudgmentLog.Select(e => e.Grade), Has.All.EqualTo(Judgment.Perfect));
            Assert.That(battle.Events.OfType<InputJudged>().Select(e => e.OffsetMs), Is.EqualTo(new[] { 20, 20, 20 }), "presses were not stamped with their scripted times");
            Assert.That(presenter.Events, Is.EqualTo(battle.Events), "the presenter did not receive the whole stream in order");

            rig.Destroy();
        }
    }
}
