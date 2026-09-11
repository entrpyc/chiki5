#nullable enable
using System;
using System.Collections.Generic;
using Chiki.Client.Audio;
using Chiki.Client.Driver;
using Chiki.Client.Keys;
using Chiki.Client.Presenter;
using Chiki.Sim;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Chiki.Client.Scene
{
    /// <summary>
    /// Composes the Battle scene (P14.5): loads the fixture content, builds one battle against
    /// the chosen enemy at World 1 balance, schedules its track on the beat clock, wires the keys
    /// to the driver and builds the HUD. Until tracks ship with recordings the clock plays a
    /// generated click track. Runs on Start unless <see cref="Compose"/> was called first.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleScene : MonoBehaviour
    {
        public const string DefaultEnemyId = "enemy-malk";
        public const float CameraSize = BattleHud.CanvasHeight / BattleHud.PixelsPerUnit / 2f;

        [SerializeField] private string enemyId = DefaultEnemyId;
        [SerializeField] private int seed = 1;

        private IReadOnlyDictionary<Slot, CardDefinition> _slots = new Dictionary<Slot, CardDefinition>();

        public string EnemyId
        {
            get => enemyId;
            set => enemyId = value;
        }

        public bool Composed { get; private set; }

        public BattleContent? Content { get; private set; }

        public Camera? Camera { get; private set; }

        public BeatClock? Clock { get; private set; }

        public BattleDriver? Driver { get; private set; }

        public BattleInput? Input { get; private set; }

        public BattleHud? Hud { get; private set; }

        /// <summary>The card each slot holds; null for an empty slot.</summary>
        public CardDefinition? CardInSlot(Slot slot)
        {
            return _slots.TryGetValue(slot, out var card) ? card : null;
        }

        /// <summary>Builds the battle with fresh run stats.</summary>
        public void Compose()
        {
            Compose(new RunStats());
        }

        /// <summary>Builds the battle, the rig, the keys and the HUD for the configured enemy on the given run stats.</summary>
        public void Compose(RunStats stats)
        {
            if (stats is null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            if (Composed)
            {
                throw new InvalidOperationException("The Battle scene is already composed.");
            }

            Content = BattleContent.LoadFixtures();
            var enemy = Content.Enemy(enemyId);
            _slots = BattleContent.FillSlots(Content.Cards);
            var battle = new Sim.Battle(stats, enemy, EncounterBalance.ForWorld(enemy.Track.World), new Rng((ulong)seed));

            Camera = FindCamera();
            EnsureListener();
            EnsureEventSystem();

            var rig = new GameObject("BattleRig");
            rig.transform.SetParent(transform, false);
            rig.AddComponent<AudioSource>();
            Clock = rig.AddComponent<BeatClock>();
            Driver = rig.AddComponent<BattleDriver>();
            Input = rig.AddComponent<BattleInput>();

            Clock.Schedule(enemy.Track, PlaceholderAudio.ClickTrack(enemy.Track));
            Driver.Bind(Clock, battle);
            Input.Driver = Driver;
            Input.CardInSlot = CardInSlot;

            Hud = BattleHud.Build(Driver, Camera, Input, CardInSlot);
            Hud.transform.SetParent(transform, true);
            Composed = true;
        }

        private void Start()
        {
            if (!Composed)
            {
                Compose();
            }
        }

        private Camera FindCamera()
        {
            var existing = UnityEngine.Camera.main;
            if (existing != null)
            {
                return existing;
            }

            var host = new GameObject("BattleCamera");
            host.tag = "MainCamera";
            host.transform.SetParent(transform, false);
            host.transform.position = new Vector3(0f, 0f, -10f);
            var camera = host.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = CameraSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.04f, 0.06f, 1f);
            return camera;
        }

        private void EnsureListener()
        {
            if (FindAnyObjectByType<AudioListener>() != null)
            {
                return;
            }

            var host = Camera != null ? Camera.gameObject : gameObject;
            host.AddComponent<AudioListener>();
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var host = new GameObject("EventSystem");
            host.transform.SetParent(transform, false);
            host.AddComponent<EventSystem>();
            host.AddComponent<InputSystemUIInputModule>();
        }
    }
}
