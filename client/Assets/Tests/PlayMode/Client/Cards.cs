#nullable enable
using System.Linq;
using Chiki.Client.Content;
using Chiki.Client.Presenter;
using Chiki.Client.Text;
using Chiki.Client.Visuals;
using Chiki.Sim;
using Chiki.Sim.Data;
using NUnit.Framework;
using UnityEngine;

namespace Client
{
    /// <summary>The card face that shows a card's anatomy over its illustration (P7.1).</summary>
    public class Cards
    {
        private GameObject? _host;

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
            {
                Object.DestroyImmediate(_host);
            }

            ClientTestContent.ClearCatalogues();
        }

        [Test]
        public void card_face_shows_anatomy()
        {
            var catalogue = ScriptableObject.CreateInstance<VisualCatalogue>();
            var illustration = ClientTestContent.TestSprite("spr_card_rend_static_01");
            var frame = ClientTestContent.TestSprite("spr_ui_card-frame_attack_01");
            var common = ClientTestContent.TestSprite("spr_ui_card-rarity_common_01");
            var bleed = ClientTestContent.TestSprite("spr_status_bleed_static_01");
            catalogue.Put(CardFace.CardKind, "rend", illustration);
            catalogue.Put(CardFace.UiKind, "card-frame-attack", frame);
            catalogue.Put(CardFace.UiKind, "card-rarity-common", common);
            catalogue.Put(StatusIconWidget.StatusKindName, "bleed", bleed);
            VisualCatalogue.Use(catalogue);

            var set = CardLoader.SetFromJson(ContentFiles.ReadText("sets/starter.json"));
            var rend = set.Cards.Single(c => c.Id == "card-rend");
            var jab = set.Cards.Single(c => c.Id == "card-jab");

            _host = new GameObject("cards", typeof(RectTransform), typeof(Canvas));
            var rendFace = CardFace.Create("Rend", _host.transform, CardFaceSize.Full);
            rendFace.Show(rend);
            var jabFace = CardFace.Create("Jab", _host.transform, CardFaceSize.Full);
            jabFace.Show(jab);

            Assert.That(rendFace.Rect.sizeDelta, Is.EqualTo(CardFace.FullSize), "the full face is not 256 by 360");
            Assert.That(rendFace.Illustration.sprite, Is.SameAs(illustration), "the face does not show Rend's illustration");
            Assert.That(rendFace.Illustration.gameObject.activeSelf, Is.True);
            Assert.That(rendFace.Frame.sprite, Is.SameAs(frame), "the face does not carry the attack frame");
            Assert.That(rendFace.Rarity.sprite, Is.SameAs(common), "the face does not carry the Common treatment");
            Assert.That(rendFace.NameText, Is.EqualTo("Rend"));
            Assert.That(rendFace.ValueText, Is.EqualTo("10"));
            Assert.That(rendFace.ValueShown, Is.True);
            Assert.That(rendFace.CooldownText, Is.EqualTo(Strings.Format("card.cooldown", 3)));
            Assert.That(rendFace.CooldownText, Is.EqualTo("3 beats"));
            Assert.That(rendFace.StatusKinds, Is.EqualTo(new[] { StatusKind.Bleed }), "the face does not show exactly the Bleed it applies");
            Assert.That(rendFace.StatusIcons.Single().sprite, Is.SameAs(bleed), "the Bleed icon is not the catalogue's");
            Assert.That(rendFace.RulesText, Is.EqualTo("Applies 1 Bleed."));
            Assert.That(rendFace.FlavourShown, Is.False, "Rend has no flavour, yet a flavour line shows");

            Assert.That(jabFace.FlavourShown, Is.True, "Jab's flavour line does not show");
            Assert.That(jabFace.FlavourText, Is.EqualTo(jab.FlavorText));
        }
    }
}
