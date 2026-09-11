#nullable enable
using System;
using System.Collections.Generic;
using Chiki.Client.Presenter;
using Chiki.Client.Text;
using Chiki.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Chiki.Client.Screens
{
    /// <summary>What the run-end screen shows (PRD 3.9.11): the outcome, the seed, the run stats and every permanent unlock granted this run.</summary>
    public sealed record RunEndSummary(
        RunStatus Outcome,
        string Seed,
        int Battles,
        int PerfectDefenses,
        int EssenceEarned,
        int CrpPeak,
        IReadOnlyList<string> Unlocks);

    /// <summary>
    /// The run-end screen (PRD 3.9.11): shown when the run's status becomes Won or Died, with
    /// the outcome, the seed, the run stats and every unlock granted this run; Continue returns
    /// to the pre-run screen, the profile already saved (PRD 3.1.8).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RunEndScreen : MonoBehaviour
    {
        private UnityEngine.UI.Text _outcome = null!;
        private UnityEngine.UI.Text _seed = null!;
        private UnityEngine.UI.Text _stats = null!;
        private UnityEngine.UI.Text _unlocks = null!;
        private Button _continue = null!;

        public RunEndSummary Summary { get; private set; } = null!;

        public string OutcomeText => _outcome.text;

        public string SeedText => _seed.text;

        public string StatsText => _stats.text;

        public string UnlocksText => _unlocks.text;

        public event Action? Continued;

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
            ScreenFactory.Fill("Backdrop", root, ScreenFactory.Backdrop);
            screen._outcome = ScreenFactory.Label("Outcome", root, Labels.RunOutcome(summary.Outcome), 96, new Vector2(0f, 300f), new Vector2(1200f, 140f), TextAnchor.MiddleCenter, ScreenFactory.Accent);
            screen._seed = ScreenFactory.Label("Seed", root, Strings.Format("runend.seed", summary.Seed), 36, new Vector2(0f, 190f), new Vector2(1200f, 60f), TextAnchor.MiddleCenter, ScreenFactory.MutedText);
            screen._stats = ScreenFactory.Label("Stats", root, Strings.Format("runend.stats", summary.Battles, summary.PerfectDefenses, summary.EssenceEarned, summary.CrpPeak), 34, new Vector2(0f, 80f), new Vector2(1500f, 120f), TextAnchor.MiddleCenter);
            screen._unlocks = ScreenFactory.Label("Unlocks", root, summary.Unlocks.Count == 0 ? Strings.Get("runend.no_unlocks") : Strings.Format("runend.unlocks", string.Join(", ", summary.Unlocks)), 34, new Vector2(0f, -60f), new Vector2(1500f, 120f), TextAnchor.MiddleCenter);
            screen._continue = ScreenFactory.Button("Continue", root, Strings.Get("runend.continue"), new Vector2(0f, -260f), new Vector2(420f, 96f), () => screen.Continued?.Invoke());
            return screen;
        }

        public bool ChooseContinue()
        {
            return ScreenFactory.Submit(_continue);
        }
    }
}
