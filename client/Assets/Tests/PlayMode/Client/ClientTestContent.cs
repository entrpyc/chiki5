#nullable enable
using System.Collections;
using System.Linq;
using Chiki.Client.Audio;
using Chiki.Client.Content;
using Chiki.Client.Driver;
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
