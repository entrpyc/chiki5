#nullable enable
using System.Collections.Generic;
using System.IO;
using Chiki.Client.Editor;
using Chiki.Client.Presenter;
using Chiki.Client.Visuals;
using Chiki.Sim;
using Chiki.Sim.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Client
{
    /// <summary>The visual catalogue and the rebuild that fills it from the imported art (P1.3).</summary>
    public class Catalogue
    {
        [SetUp]
        public void SetUp()
        {
            ArtFixture.Delete();
            SpriteAssetPostprocessor.ClearErrors();
        }

        [TearDown]
        public void TearDown()
        {
            ArtFixture.Delete();
            VisualCatalogue.Clear();
        }

        [Test]
        public void lookup_returns_sprite_or_fallback()
        {
            var catalogue = ScriptableObject.CreateInstance<VisualCatalogue>();
            var bleed = TestSprite("bleed");
            catalogue.Put("status", "bleed", bleed);

            Assert.That(catalogue.Sprite("status", "bleed"), Is.SameAs(bleed));
            Assert.That(catalogue.Sprite("status", "nothing"), Is.SameAs(VisualCatalogue.FallbackSprite), "an unknown id did not fall back");
            Assert.That(catalogue.Missing, Is.EqualTo(new[] { "status/nothing" }), "Missing does not hold exactly the unknown id");
            Assert.That(catalogue.Clip("vfx", "nothing").FrameCount, Is.EqualTo(1), "an unknown clip id did not fall back to a one-frame clip");
            Assert.That(catalogue.Missing, Is.EqualTo(new[] { "status/nothing", "vfx/nothing" }));

            Object.DestroyImmediate(catalogue);
        }

        [Test]
        public void rebuild_collects_imported_sprites()
        {
            ArtFixture.Png("spr_status_bleed_static_01.png", 8, 8, Color.red);
            ArtFixture.Png("spr_node_elite_static_01.png", 8, 8, Color.yellow);
            ArtFixture.Import();
            Assert.That(SpriteAssetPostprocessor.Errors, Is.Empty, string.Join("\n", SpriteAssetPostprocessor.Errors));

            var catalogue = ScriptableObject.CreateInstance<VisualCatalogue>();
            CatalogueRebuild.FillVisuals(catalogue);

            Assert.That(catalogue.Has("status", "bleed"), Is.True, "the rebuild did not collect the status sprite");
            Assert.That(catalogue.Has("node", "elite"), Is.True, "the rebuild did not collect the node sprite");
            Assert.That(catalogue.Sprite("status", "bleed").name, Is.EqualTo("spr_status_bleed_static_01"));
            Assert.That(catalogue.Sprite("node", "elite").name, Is.EqualTo("spr_node_elite_static_01"));
            Assert.That(catalogue.Missing, Is.Empty, "a shipped id was recorded as missing");

            Object.DestroyImmediate(catalogue);
        }

        /// <summary>P2.1: the six sprites the Rhythm Line is drawn from, at the sizes the plan states, sliced where they stretch.</summary>
        [Test]
        public void rhythm_line_kit_complete()
        {
            var catalogue = Shipped();
            var kit = new (string Id, int Width, int Height)[]
            {
                (RhythmLineView.BackgroundId, 400, 180),
                (RhythmLineView.BeatTickId, 4, 126),
                (RhythmLineView.QuarterTickId, 2, 54),
                (RhythmLineView.PlayheadId, 8, 180),
                (RhythmLineView.WindowId, 64, 144),
                (RhythmLineView.WindowOpenId, 64, 144),
            };

            foreach (var piece in kit)
            {
                Assert.That(catalogue.Has(RhythmLineView.UiKind, piece.Id), Is.True, piece.Id + " is not in the shipped catalogue");
                var sprite = catalogue.Sprite(RhythmLineView.UiKind, piece.Id);
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(piece.Width, piece.Height)), piece.Id + " is not drawn at its stated size");
            }

            foreach (string id in new[] { RhythmLineView.BackgroundId, RhythmLineView.WindowId, RhythmLineView.WindowOpenId })
            {
                var border = catalogue.Sprite(RhythmLineView.UiKind, id).border;
                Assert.That(border.x, Is.GreaterThan(0f), id + " has no left 9-slice border");
                Assert.That(border.z, Is.GreaterThan(0f), id + " has no right 9-slice border");
            }

            Assert.That(catalogue.Missing, Is.Empty, "a Rhythm Line sprite fell back: " + string.Join(", ", catalogue.Missing));
        }

        /// <summary>P2.2: one icon per action kind, each at 80 by 80 and each a different image.</summary>
        [Test]
        public void telegraph_icons_complete()
        {
            var catalogue = Shipped();
            var kinds = new[]
            {
                EnemyActionKind.AttackLeft,
                EnemyActionKind.AttackRight,
                EnemyActionKind.Defend,
                EnemyActionKind.Buff,
                EnemyActionKind.Charge,
            };

            var images = new Dictionary<string, byte[]>();
            foreach (var kind in kinds)
            {
                string id = ChartLoader.KindToId(kind);
                Assert.That(catalogue.Has(ActionMarker.ActionKindName, id), Is.True, id + " has no icon in the shipped catalogue");
                var sprite = catalogue.Sprite(ActionMarker.ActionKindName, id);
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(80f, 80f)), id + " is not an 80 by 80 icon");
                images[id] = File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite));
            }

            foreach (var one in images)
            {
                foreach (var other in images)
                {
                    if (one.Key != other.Key)
                    {
                        Assert.That(one.Value, Is.Not.EqualTo(other.Value), one.Key + " and " + other.Key + " are the same image");
                    }
                }
            }

            Assert.That(catalogue.Missing, Is.Empty, "a telegraph icon fell back: " + string.Join(", ", catalogue.Missing));
        }

        /// <summary>P3.1: a frame and an icon per Category, the frames sliced and the icons told apart in greyscale.</summary>
        [Test]
        public void category_art_complete()
        {
            var catalogue = Shipped();
            var categories = new[] { CardCategory.Ability, CardCategory.LeftAttack, CardCategory.Defense };

            var images = new Dictionary<string, byte[]>();
            foreach (var category in categories)
            {
                string id = SlotWidget.CategoryId(category);

                string frameId = SlotWidget.FrameIdPrefix + id;
                Assert.That(catalogue.Has(SlotWidget.UiKind, frameId), Is.True, frameId + " is not in the shipped catalogue");
                var border = catalogue.Sprite(SlotWidget.UiKind, frameId).border;
                Assert.That(border.x, Is.GreaterThan(0f), frameId + " has no left 9-slice border");
                Assert.That(border.y, Is.GreaterThan(0f), frameId + " has no bottom 9-slice border");
                Assert.That(border.z, Is.GreaterThan(0f), frameId + " has no right 9-slice border");
                Assert.That(border.w, Is.GreaterThan(0f), frameId + " has no top 9-slice border");

                Assert.That(catalogue.Has(SlotWidget.CategoryKind, id), Is.True, id + " has no Category icon in the shipped catalogue");
                var icon = catalogue.Sprite(SlotWidget.CategoryKind, id);
                Assert.That(icon.rect.size, Is.EqualTo(new Vector2(64f, 64f)), id + " is not a 64 by 64 icon");
                images[id] = File.ReadAllBytes(AssetDatabase.GetAssetPath(icon));
            }

            foreach (var one in images)
            {
                foreach (var other in images)
                {
                    if (one.Key != other.Key)
                    {
                        Assert.That(one.Value, Is.Not.EqualTo(other.Value), one.Key + " and " + other.Key + " are the same image");
                    }
                }
            }

            Assert.That(catalogue.Missing, Is.Empty, "a Category's art fell back: " + string.Join(", ", catalogue.Missing));
        }

        /// <summary>The catalogue the game ships with, with nothing recorded as missing yet.</summary>
        private static VisualCatalogue Shipped()
        {
            var catalogue = AssetDatabase.LoadAssetAtPath<VisualCatalogue>(VisualCatalogue.AssetPath);
            Assert.That(catalogue, Is.Not.Null, "no catalogue at " + VisualCatalogue.AssetPath + "; run Chiki > Rebuild Visual Catalogue");
            catalogue.ClearMissing();
            return catalogue;
        }

        private static Sprite TestSprite(string name)
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { name = name };
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }
    }
}
