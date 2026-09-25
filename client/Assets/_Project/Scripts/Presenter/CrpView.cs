#nullable enable
using System;
using Chiki.Client.Driver;
using Chiki.Client.Text;
using Chiki.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Chiki.Client.Presenter
{
    /// <summary>
    /// The CRP readout of the battle HUD (PRD 3.8.1): the run's CRP beside the CRP icon (P4.5),
    /// refreshed from the driver's event stream whenever a stat changes, so it is visible for the
    /// whole battle.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CrpView : MonoBehaviour, IBattlePresenter
    {
        /// <summary>The kind and id the CRP icon is catalogued under (P4.5).</summary>
        public const string UiKind = "ui";

        public const string CrpIconId = "crp";

        /// <summary>The icon's drawn size, and the gap between it and the value.</summary>
        public const float IconSize = 40f;

        public const float IconGap = 10f;

        private static readonly Color CrpColor = new Color(0.95f, 0.75f, 0.25f, 1f);

        private UnityEngine.UI.Text _label = null!;
        private Image _icon = null!;

        public string Text => _label.text;

        /// <summary>The CRP icon the value sits beside (P4.5).</summary>
        public Image Icon => _icon;

        /// <summary>The rect the value is drawn in, to the icon's right.</summary>
        public RectTransform Label => _label.rectTransform;

        public static CrpView Build(BattleDriver driver, RectTransform parent, Vector2 position)
        {
            if (driver == null)
            {
                throw new ArgumentNullException(nameof(driver));
            }

            var rect = HudFactory.Rect("Crp", parent, position, new Vector2(320f, 50f));
            var view = rect.gameObject.AddComponent<CrpView>();
            var piece = new LinePiece(UiKind, CrpIconId, CrpColor);
            view._icon = HudFactory.Image("Icon", rect, piece.Tint, new Vector2(-IconSize / 2f - IconGap / 2f, 0f), new Vector2(IconSize, IconSize), piece.Sprite);
            view._label = HudFactory.Text("Label", rect, 30, CrpColor, TextAnchor.MiddleLeft);
            view._label.rectTransform.anchoredPosition = new Vector2(IconGap / 2f + 110f, 0f);
            view._label.rectTransform.sizeDelta = new Vector2(220f, 50f);
            if (driver.Battle != null)
            {
                view.Refresh(driver.Battle);
            }

            driver.AttachPresenter(view);
            return view;
        }

        public void OnBattleEvent(Sim.Battle battle, BattleEvent battleEvent)
        {
            if (battleEvent is StatChanged || battleEvent is BattleStarted || battleEvent is BattleEnded)
            {
                Refresh(battle);
            }
        }

        public void Refresh(Sim.Battle battle)
        {
            _label.text = Strings.Format("crp.label", battle.Stats.Crp);
        }
    }
}
