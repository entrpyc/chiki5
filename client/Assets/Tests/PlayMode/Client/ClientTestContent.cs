#nullable enable
using System.Collections;
using System.Linq;
using Chiki.Client.Audio;
using Chiki.Client.Content;
using Chiki.Client.Driver;
using Chiki.Client.Scene;
using Chiki.Sim;
using Chiki.Sim.Data;
using UnityEngine;
using SimBattle = Chiki.Sim.Battle;

namespace Client
{
    /// <summary>A beat clock and battle driver on one GameObject, the rig every client test drives.</summary>
    internal sealed class Rig
    {
        public GameObject Root { get; }
        public BeatClock Clock { get; }
        public BattleDriver Driver { get; }
        public AudioSource Source => Root.GetComponent<AudioSource>();

        public Rig(string name)
        {
            Root = new GameObject(name);
            Root.AddComponent<AudioListener>();
            Root.AddComponent<AudioSource>();
            Clock = Root.AddComponent<BeatClock>();
            Driver = Root.AddComponent<BattleDriver>();
        }

        public void Destroy()
        {
            Object.Destroy(Root);
        }

        /// <summary>Waits, frame by frame, until the clock's audio time reaches the given millisecond or the timeout passes.</summary>
        public IEnumerator WaitUntilAudioMs(int audioTimeMs, float timeoutSeconds = 30f)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (Clock.NowMs < audioTimeMs && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }
    }

    /// <summary>Content the client tests run against: the fixture track from data/, a silent clip of its length, and a simple enemy.</summary>
    internal static class ClientTestContent
    {
        public const int DefaultEnemyHp = 1000;
        public const int SampleRate = 48000;

        public static readonly Slot SlotE = new Slot(0, SlotKey.E);
        public static readonly Slot SlotR = new Slot(0, SlotKey.R);
        public static readonly Slot SlotELine2 = new Slot(1, SlotKey.E);
        public static readonly CardDefinition LeftAttack10 = new CardDefinition("card-left-10", "Left 10", CardCategory.LeftAttack, 10, Tuning.CooldownMinBeats);

        /// <summary>data/tracks/fixture-120.json: 64 beats at BPM 120, offset 0.</summary>
        /// <summary>The run content the flow starts a run on (P17.5): the fixture cards, Charms, Imprints and enemies from data/.</summary>
        public static RunContent LoadRunContent()
        {
            var content = BattleContent.LoadFixtures();
            return new RunContent(
                content.Cards,
                CharmLoader.SetFromJson(ContentFiles.ReadText("charms/fixtures.json")),
                ImprintLoader.SetFromJson(ContentFiles.ReadText("imprints/fixtures.json")),
                enemies: new EnemySet("fixtures", content.Enemies.Values.ToList()));
        }

        /// <summary>Walks the run along the first forward node at every step to the current World's Boss node without fighting: every stop on the way is completed as arrived at.</summary>
        public static void WalkToBoss(Run run)
        {
            while (run.CurrentNode.Type != NodeType.Boss)
            {
                if (!run.CurrentNodeCompleted)
                {
                    run.CompleteNode();
                }

                if (run.MoveTo(run.ForwardNodes[0].Id) != MoveResult.Moved)
                {
                    throw new System.InvalidOperationException("The run could not move forward.");
                }
            }
        }

        /// <summary>Fights the current node's battle to a win: the enemy has 1 HP and every charted action is answered (see <see cref="FightToWin"/>). The battle is returned ended, not settled.</summary>
        public static SimBattle FightNodeBattle(Run run)
        {
            var battle = run.StartNodeBattle(enemyHp: 1);
            FightToWin(battle);
            return battle;
        }

        /// <summary>
        /// Plays a battle to its end: every charted action is answered with a Perfect press, an
        /// attack slot of the action's side first so the hit lands through any Block the enemy
        /// holds (Guard, PRD 3.6.25), any other line-0 slot otherwise so no damage comes in.
        /// With the enemy at 1 HP the battle is won.
        /// </summary>
        public static void FightToWin(SimBattle battle)
        {
            var others = new[] { new Slot(0, SlotKey.Q), new Slot(0, SlotKey.W), new Slot(0, SlotKey.O), new Slot(0, SlotKey.P) };
            var left = new[] { SlotE, SlotR, new Slot(0, SlotKey.U), new Slot(0, SlotKey.I) };
            var right = new[] { new Slot(0, SlotKey.U), new Slot(0, SlotKey.I), SlotE, SlotR };
            foreach (var action in battle.Chart.Actions.OrderBy(a => a.LandingQb))
            {
                if (battle.Outcome != null)
                {
                    break;
                }

                var attacks = action.Kind == EnemyActionKind.AttackRight ? right : left;
                battle.AdvanceToPosition(action.LandingQb);
                foreach (var slot in attacks.Concat(others))
                {
                    if (battle.Press(slot, battle.BeatMap.TimeAtQb(action.LandingQb)).Accepted)
                    {
                        break;
                    }
                }
            }

            if (battle.Outcome == null)
            {
                battle.AdvanceToBeat(battle.Track.LengthBeats + 1);
            }
        }

        public static Track FixtureTrack()
        {
            return TrackLoader.FromJson(ContentFiles.ReadText("tracks/fixture-120.json"));
        }

        /// <summary>A silent mono clip exactly as long as one lap of the track plus its offset, so looping lines up with the beat map.</summary>
        public static AudioClip SilentClip(Track track)
        {
            int samples = checked((int)((long)(track.OffsetMs + track.BeatMap.LengthMs) * SampleRate / 1000));
            var clip = AudioClip.Create("clip-" + track.Id, samples, 1, SampleRate, false);
            clip.SetData(new float[samples], 0);
            return clip;
        }

        /// <summary>A chart of left attacks at the given quarter-beat positions on the track.</summary>
        public static Chart Chart(Track track, params int[] positionsQb)
        {
            return new Chart("chart-client-test", "enemy-client-test", track, positionsQb.Select(p => new EnemyAction(EnemyActionKind.AttackLeft, p)).ToArray());
        }

        /// <summary>A Normal Fast Aggressor whose one ability registers nothing, so only the chart acts.</summary>
        public static EnemyDefinition Enemy(Chart chart, int damagePerHit = 10)
        {
            return new EnemyDefinition(chart.EnemyId, "Client Test Enemy", EncounterTier.Normal, EnemyRole.Aggressor, RhythmProfile.Fast, 45, chart, damagePerHit, new[] { EnemyAbility.ChargeBuff });
        }

        public static SimBattle Battle(Chart chart, int enemyHp = DefaultEnemyHp)
        {
            return new SimBattle(new RunStats(), Enemy(chart), enemyHp, new Rng(1));
        }

        /// <summary>A rig with the fixture track scheduled and a battle on the given chart positions bound to it.</summary>
        public static Rig ScheduledRig(string name, params int[] positionsQb)
        {
            var track = FixtureTrack();
            var rig = new Rig(name);
            rig.Clock.Schedule(track, SilentClip(track));
            rig.Driver.Bind(rig.Clock, Battle(Chart(track, positionsQb)));
            return rig;
        }
    }
}
