#nullable enable
using System;
using System.Collections.Generic;
using Chiki.Client.Keys;
using Chiki.Client.Presenter;
using Chiki.Client.Text;
using Chiki.Sim;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Chiki.Client.Screens
{
    /// <summary>
    /// The reward panel opened over the map after a won battle node (PRD 3.7.2 to 3.7.4): the
    /// Essence paid, the Imprint granted when there is one, and the cards offered, one of
    /// which the number keys 1 to 3 take; Skip declines. The run cannot move on until the
    /// offer is resolved (PRD 3.3.9.2).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RewardPanel : MonoBehaviour
    {
        private readonly List<Button> _picks = new List<Button>();
        private Button _skip = null!;
        private ScreenKeys? _keys;

        public RewardOffer Offer { get; private set; } = null!;

        public event Action<CardDefinition>? CardPicked;

        public event Action? Skipped;

        public static RewardPanel Build(Transform? parent, RewardOffer offer)
        {
            if (offer is null)
            {
                throw new ArgumentNullException(nameof(offer));
            }

            var canvas = ScreenFactory.Canvas("RewardPanel", parent, 40);
            var panel = canvas.gameObject.AddComponent<RewardPanel>();
            panel.Offer = offer;
            var root = canvas.transform;
            ScreenFactory.Fill("Dim", root, new Color(0f, 0f, 0f, 0.6f));
            var face = HudFactory.Image("Panel", root, ScreenFactory.Panel, Vector2.zero, new Vector2(1000f, 640f));
            face.raycastTarget = true;
            ScreenFactory.Label("Title", face.transform, Strings.Format("reward.title", Labels.Tier(offer.Tier)), 52, new Vector2(0f, 260f), new Vector2(900f, 80f), TextAnchor.MiddleCenter);
            string granted = Strings.Format("reward.essence", offer.Essence);
            if (offer.ImprintId != null)
            {
                granted += "   " + Strings.Format("reward.imprint", offer.ImprintId);
            }

            ScreenFactory.Label("Granted", face.transform, granted, 30, new Vector2(0f, 190f), new Vector2(900f, 60f), TextAnchor.MiddleCenter, ScreenFactory.Accent);
            for (int i = 0; i < offer.Cards.Count; i++)
            {
                var card = offer.Cards[i];
                string label = Strings.Format("reward.pick", i + 1, card.Name, card.Rarity, card.Category, card.Value);
                panel._picks.Add(ScreenFactory.Button("Pick " + (i + 1), face.transform, label, new Vector2(0f, 90f - i * 100f), new Vector2(860f, 84f), () => panel.CardPicked?.Invoke(card)));
            }

            if (offer.Cards.Count == 0)
            {
                ScreenFactory.Label("None", face.transform, Strings.Get("reward.no_cards"), 30, new Vector2(0f, 40f), new Vector2(900f, 60f), TextAnchor.MiddleCenter, ScreenFactory.MutedText);
            }

            panel._skip = ScreenFactory.Button("Skip", face.transform, Strings.Get("reward.skip"), new Vector2(0f, -250f), new Vector2(400f, 80f), () => panel.Skipped?.Invoke());
            panel._keys = new ScreenKeys(new[] { Key.Digit1, Key.Digit2, Key.Digit3, Key.Escape }, panel.KeyDown);
            return panel;
        }

        /// <summary>Takes the card at the index the way the number key would; false when there is none.</summary>
        public bool ChoosePick(int index)
        {
            return index >= 0 && index < _picks.Count && ScreenFactory.Submit(_picks[index]);
        }

        public bool ChooseSkip()
        {
            return ScreenFactory.Submit(_skip);
        }

        public void KeyDown(Key key)
        {
            switch (key)
            {
                case Key.Digit1: ChoosePick(0); break;
                case Key.Digit2: ChoosePick(1); break;
                case Key.Digit3: ChoosePick(2); break;
                case Key.Escape: ChooseSkip(); break;
            }
        }

        private void OnDestroy()
        {
            _keys?.Dispose();
            _keys = null;
        }
    }
}
