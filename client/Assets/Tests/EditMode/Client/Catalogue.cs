#nullable enable
using Chiki.Client.Editor;
using Chiki.Client.Visuals;
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
