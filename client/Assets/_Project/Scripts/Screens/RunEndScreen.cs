#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Chiki.Client.Presenter;
using Chiki.Client.Text;
using Chiki.Client.Visuals;
using Chiki.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Chiki.Client.Screens
{
    /// <summary>
    /// One permanent unlock as the run-end screen shows it (PRD 3.9.11, P10.3): the kind and
    /// content id its icon is looked up by, and the name the player reads, never the raw id.
    /// </summary>
    public sealed record RunEndUnlock(string Kind, string Id, string Name)
    {
        /// <summary>The icon's catalogue id: the content id without its kind prefix (<c>clean-victory</c> for <c>charm-clean-victory</c>).</summary>
        public string Subject => Id.StartsWith(Kind + "-", StringComparison.Ordinal) ? Id.Substring(Kind.Length + 1) : Id;
    }

    /// <summary>What the run-end screen shows (PRD 3.9.11): the outcome, the seed, the run stats and every permanent unlock granted this run.</summary>
    public sealed record RunEndSummary(
        RunStatus Outcome,
        string Seed,
        int Battles,
        int PerfectDefenses,
        int EssenceEarned,
        int CrpPeak,
        IReadOnlyList<RunEndUnlock> Unlocks);

    /// <summary>
    /// The run-end screen (PRD 3.9.11): shown when the run's status becomes Won or Died, with
    /// the outcome word on its plate, the seed, the run stats and every unlock granted this run
    /// as its icon and name (P10.3); Continue returns to the pre-run screen, the profile already
    /// saved (PRD 3.1.8).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RunEndScreen : MonoBehaviour
    {
        /// <summary>The outcome plates' drawn size (P10.3).</summary>
        public static readonly Vector2 PlateSize = new Vector2(1200f, 240f);

        public const float UnlockIconSize = 64f;
        public const float UnlockWidth = 360f;

        private readonly List<Image> _unlockIcons = new List<Image>();
        private readonly List<UnityEngine.UI.Text> _unlockNames = new List<UnityEngine.UI.Text>();
        private UnityEngine.UI.Text _outcome = null!;
        private UnityEngine.UI.Text _seed = null!;
        private UnityEngine.UI.Text _stats = null!;
        private UnityEngine.UI.Text _unlocks = null!;
        private Button _continue = null!;

        public RunEndSummary Summary { get; private set; } = null!;

        /// <summary>The plate the outcome word sits on.</summary>
        public Image OutcomePlate { get; private set; } = null!;

        public string OutcomeText => _outcome.text;

        public string SeedText => _seed.text;

        public string StatsText => _stats.text;

        /// <summary>The unlocks as read on screen: the heading, then every unlock's name.</summary>
        public string UnlocksText => string.Join(" ", new[] { _unlocks.text }.Concat(_unlockNames.Select(n => n.text)));

        /// <summary>Each unlock's icon, in the order of <see cref="RunEndSummary.Unlocks"/>.</summary>
        public IReadOnlyList<Image> UnlockIcons => _unlockIcons;

        /// <summary>Each unlock's name label, in the same order.</summary>
        public IReadOnlyList<UnityEngine.UI.Text> UnlockNames => _unlockNames;

        public event Action? Continued;

        /// <summary>The catalogue id of an outcome's plate: <c>outcome-won</c>, <c>outcome-died</c>, <c>outcome-abandoned</c>.</summary>
        public static string PlateId(RunStatus outcome)
        {
            return "outcome-" + outcome.ToString().ToLowerInvariant();
        }

        public static RunEndScreen Build(Transform? parent, RunEndSummary summary)
        {
            if (summary is null)
            {
                throw new ArgumentNullException(nameof(summary));
            }

            var canvas = ScreenFactory.Canvas("RunEndScreen", parent, 60);
            var screen = canvas.gameObject.AddComponent<RunEndScreen>();
            screen.Summary = summary;
            var root = canvas.transform;
            var catalogue = VisualCatalogue.Active;
            ScreenFactory.BackdropImage(root);

            string plateId = PlateId(summary.Outcome);
            var plate = catalogue.Sprite(ScreenFactory.UiKind, plateId);
            bool drawn = catalogue.Has(ScreenFactory.UiKind, plateId);
            screen.OutcomePlate = HudFactory.Image("OutcomePlate", root, drawn ? Color.white : ScreenFactory.Panel, new Vector2(0f, 300f), PlateSize, drawn ? plate : null);
            screen._outcome = ScreenFactory.Label("Outcome", screen.OutcomePlate.transform, Labels.RunOutcome(summary.Outcome), 96, new Vector2(0f, -8f), new Vector2(820f, 140f), TextAnchor.MiddleCenter, drawn ? ScreenFactory.TextColor : ScreenFactory.Accent);
            screen._outcome.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(3f, -4f);

            screen._seed = ScreenFactory.Label("Seed", root, Strings.Format("runend.seed", summary.Seed), 36, new Vector2(0f, 150f), new Vector2(1200f, 60f), TextAnchor.MiddleCenter, ScreenFactory.MutedText);
            screen._stats = ScreenFactory.Label("Stats", root, Strings.Format("runend.stats", summary.Battles, summary.PerfectDefenses, summary.EssenceEarned, summary.CrpPeak), 34, new Vector2(0f, 60f), new Vector2(1500f, 100f), TextAnchor.MiddleCenter);
            screen._unlocks = ScreenFactory.Label("Unlocks", root, Strings.Get(summary.Unlocks.Count == 0 ? "runend.no_unlocks" : "runend.unlocked"), 34, new Vector2(0f, -40f), new Vector2(1500f, 60f), TextAnchor.MiddleCenter);
            screen.BuildUnlocks(root, summary.Unlocks);
            screen._continue = ScreenFactory.Button("Continue", root, Strings.Get("runend.continue"), new Vector2(0f, -330f), new Vector2(420f, 96f), () => screen.Continued?.Invoke());
            return screen;
        }

        public bool ChooseContinue()
        {
            return ScreenFactory.Submit(_continue);
        }

        /// <summary>One row of unlocks under the heading, centred: each its icon with its name beneath.</summary>
        private void BuildUnlocks(Transform root, IReadOnlyList<RunEndUnlock> unlocks)
        {
            var catalogue = VisualCatalogue.Active;
            float left = -(unlocks.Count - 1) * UnlockWidth / 2f;
            for (int i = 0; i < unlocks.Count; i++)
            {
                var unlock = unlocks[i];
                var cell = HudFactory.Rect("Unlock " + unlock.Id, root, new Vector2(left + i * UnlockWidth, -150f), new Vector2(UnlockWidth, 140f));
                var icon = HudFactory.Image("Icon", cell, Color.white, new Vector2(0f, 30f), new Vector2(UnlockIconSize, UnlockIconSize), catalogue.Sprite(unlock.Kind, unlock.Subject));
                var name = HudFactory.Text("Name", cell, 30, ScreenFactory.TextColor, TextAnchor.MiddleCenter);
                name.rectTransform.anchoredPosition = new Vector2(0f, -35f);
                name.rectTransform.sizeDelta = new Vector2(UnlockWidth, 44f);
                name.text = unlock.Name;
                _unlockIcons.Add(icon);
                _unlockNames.Add(name);
            }
        }
    }
}
