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
    /// Essence paid, the Imprint granted when there is one, and the cards offered as full card
    /// faces side by side (P7.4). Left and Right move the highlight and Enter takes the
    /// highlighted card, or the number keys 1 to 3 take one directly; Skip declines on Esc. The
    /// run cannot move on until the offer is resolved (PRD 3.3.9.2).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RewardPanel : MonoBehaviour
    {
        /// <summary>The distance between the centres of two offered faces.</summary>
        private const float FaceSpacing = 320f;

        private readonly List<Button> _picks = new List<Button>();
        private readonly List<CardFace> _faces = new List<CardFace>();
        private Button _skip = null!;
        private ScreenKeys? _keys;
        private int _highlighted;

        public RewardOffer Offer { get; private set; } = null!;

        /// <summary>The offered cards as full faces, in offer order (P7.4).</summary>
        public IReadOnlyList<CardFace> Faces => _faces;

        /// <summary>The index of the face Enter would take.</summary>
        public int HighlightedIndex => _highlighted;

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
            var face = ScreenFactory.PanelImage("Panel", root, Vector2.zero, new Vector2(1100f, 800f));
            face.raycastTarget = true;
            ScreenFactory.Label("Title", face.transform, Strings.Format("reward.title", Labels.Tier(offer.Tier)), 52, new Vector2(0f, 340f), new Vector2(1000f, 80f), TextAnchor.MiddleCenter);
            string granted = Strings.Format("reward.essence", offer.Essence);
            if (offer.ImprintId != null)
            {
                granted += "   " + Strings.Format("reward.imprint", offer.ImprintId);
            }

            ScreenFactory.Label("Granted", face.transform, granted, 30, new Vector2(0f, 280f), new Vector2(1000f, 60f), TextAnchor.MiddleCenter, ScreenFactory.Accent);
            float left = -(offer.Cards.Count - 1) * FaceSpacing / 2f;
            for (int i = 0; i < offer.Cards.Count; i++)
            {
                var card = offer.Cards[i];
                var position = new Vector2(left + i * FaceSpacing, 20f);
                var cardFace = CardFace.Create("Offer " + (i + 1), face.transform, CardFaceSize.Full, position);
                cardFace.Show(card);
                panel._faces.Add(cardFace);

                // The face itself is the button, so a click takes it as Enter or its number key would.
                var hit = HudFactory.StretchedImage("Hit", cardFace.transform, new Color(1f, 1f, 1f, 0f));
                hit.raycastTarget = true;
                var button = hit.gameObject.AddComponent<Button>();
                button.targetGraphic = hit;
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => panel.CardPicked?.Invoke(card));
                panel._picks.Add(button);

                ScreenFactory.Label("Key", face.transform, Strings.Format("reward.key", i + 1), 30, position + new Vector2(0f, -CardFace.FullSize.y / 2f - 30f), new Vector2(200f, 44f), TextAnchor.MiddleCenter, ScreenFactory.MutedText);
            }

            if (offer.Cards.Count == 0)
            {
                ScreenFactory.Label("None", face.transform, Strings.Get("reward.no_cards"), 30, new Vector2(0f, 40f), new Vector2(900f, 60f), TextAnchor.MiddleCenter, ScreenFactory.MutedText);
            }
            else
            {
                ScreenFactory.Label("Hint", face.transform, Strings.Get("reward.hint"), 24, new Vector2(0f, -262f), new Vector2(1000f, 44f), TextAnchor.MiddleCenter, ScreenFactory.MutedText);
            }

            panel._skip = ScreenFactory.Button("Skip", face.transform, Strings.Get("reward.skip"), new Vector2(0f, -340f), new Vector2(400f, 80f), () => panel.Skipped?.Invoke());
            panel._keys = new ScreenKeys(
                new[] { Key.Digit1, Key.Digit2, Key.Digit3, Key.LeftArrow, Key.RightArrow, Key.Enter, Key.NumpadEnter, Key.Escape },
                panel.KeyDown);
            panel.Highlight(0);
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
                case Key.LeftArrow: Highlight(_highlighted - 1); break;
                case Key.RightArrow: Highlight(_highlighted + 1); break;
                case Key.Enter:
                case Key.NumpadEnter: ChoosePick(_highlighted); break;
                case Key.Escape: ChooseSkip(); break;
            }
        }

        /// <summary>Moves the highlight to a face, held at the first and last; nothing moves while no card is offered.</summary>
        private void Highlight(int index)
        {
            if (_faces.Count == 0)
            {
                return;
            }

            _highlighted = Mathf.Clamp(index, 0, _faces.Count - 1);
            for (int i = 0; i < _faces.Count; i++)
            {
                _faces[i].SetHighlighted(i == _highlighted);
            }
        }

        private void OnDestroy()
        {
            _keys?.Dispose();
            _keys = null;
        }
    }
}
