#nullable enable
using System;
using Chiki.Client.Driver;
using Chiki.Client.Keys;
using Chiki.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Chiki.Client.Presenter
{
    /// <summary>
    /// The minimal combat presenter (Phase 14): a world-space canvas holding the Rhythm Line,
    /// both slot rows, the two bars with their status icons, and the judgment feedback, every
    /// one attached to the driver's event stream. Built in code until the prefabs and visual
    /// catalogues carry art; the layout is a 1920 by 1080 canvas at 100 pixels per unit.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleHud : MonoBehaviour
    {
        public const float CanvasWidth = 1920f;
        public const float CanvasHeight = 1080f;
        public const float PixelsPerUnit = 100f;

        public Canvas Canvas { get; private set; } = null!;

        public RhythmLineView RhythmLine { get; private set; } = null!;

        public SlotRowsView Slots { get; private set; } = null!;

        public StatusIconsView Statuses { get; private set; } = null!;

        public JudgmentCues Cues { get; private set; } = null!;

        public FeedbackPresenter Feedback { get; private set; } = null!;

        /// <summary>The CRP readout (PRD 3.8.1).</summary>
        public CrpView Crp { get; private set; } = null!;

        /// <summary>The shake on the battle camera; null when the HUD was built without a camera.</summary>
        public CameraShake? Shake { get; private set; }

        public static BattleHud Build(BattleDriver driver, Camera? camera, BattleInput? input, Func<Slot, CardDefinition?>? cardInSlot)
        {
            if (driver == null)
            {
                throw new ArgumentNullException(nameof(driver));
            }

            var root = new GameObject("BattleHud", typeof(RectTransform));
            var hud = root.AddComponent<BattleHud>();
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            root.AddComponent<GraphicRaycaster>();
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
            rect.localScale = Vector3.one / PixelsPerUnit;
            rect.position = Vector3.zero;
            hud.Canvas = canvas;

            hud.RhythmLine = RhythmLineView.Build(driver, rect, new Vector2(0f, 330f), new Vector2(1600f, 180f));
            hud.Statuses = StatusIconsView.Build(driver, rect, new Vector2(520f, 150f), new Vector2(-520f, -150f));
            hud.Slots = SlotRowsView.Build(driver, rect, new Vector2(0f, -360f), cardInSlot, input);
            hud.Crp = CrpView.Build(driver, rect, new Vector2(0f, 500f));

            if (camera != null)
            {
                hud.Shake = camera.GetComponent<CameraShake>();
                if (hud.Shake == null)
                {
                    hud.Shake = camera.gameObject.AddComponent<CameraShake>();
                }

                hud.Shake.Clock = driver.Clock;
            }

            hud.Cues = root.AddComponent<JudgmentCues>();
            hud.Feedback = FeedbackPresenter.Build(driver, root, hud.Cues, hud.Slots, hud.Shake, cardInSlot);
            return hud;
        }
    }
}
