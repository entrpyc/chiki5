#nullable enable
using System;
using Chiki.Client.Driver;
using Chiki.Client.Text;
using Chiki.Sim;
using UnityEngine;

namespace Chiki.Client.Presenter
{
    /// <summary>
    /// The CRP readout of the battle HUD (PRD 3.8.1): the run's CRP, refreshed from the
    /// driver's event stream whenever a stat changes, so it is visible for the whole battle.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CrpView : MonoBehaviour, IBattlePresenter
    {
        private UnityEngine.UI.Text _label = null!;

        public string Text => _label.text;

        public static CrpView Build(BattleDriver driver, RectTransform parent, Vector2 position)
        {
            if (driver == null)
            {
                throw new ArgumentNullException(nameof(driver));
            }

            var rect = HudFactory.Rect("Crp", parent, position, new Vector2(320f, 50f));
            var view = rect.gameObject.AddComponent<CrpView>();
            view._label = HudFactory.StretchedText("Label", rect, 30, new Color(0.95f, 0.75f, 0.25f, 1f), TextAnchor.MiddleCenter);
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
