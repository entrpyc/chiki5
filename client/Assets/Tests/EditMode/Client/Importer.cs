#nullable enable
using System.Linq;
using Chiki.Client.Editor;
using Chiki.Client.Visuals;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Client
{
    /// <summary>The sprite importer and the clip sidecar (P1.2).</summary>
    public class Importer
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
            LogAssert.ignoreFailingMessages = false;
            ArtFixture.Delete();
        }

        [Test]
        public void sequence_becomes_clip()
        {
            string first = ArtFixture.Png("spr_enemy_test_attack-left_01.png", 8, 16, Color.red);
            ArtFixture.Png("spr_enemy_test_attack-left_02.png", 8, 16, Color.green);
            ArtFixture.Png("spr_enemy_test_attack-left_03.png", 8, 16, Color.blue);
            ArtFixture.Sidecar("enemy", "test", ("attack-left", 1, false, 2));
            ArtFixture.Import();

            Assert.That(SpriteAssetPostprocessor.Errors, Is.Empty, string.Join("\n", SpriteAssetPostprocessor.Errors));

            var clip = AssetDatabase.LoadAssetAtPath<SpriteClip>(ArtFixture.Folder + "/clip_enemy_test_attack-left.asset");
            Assert.That(clip, Is.Not.Null, "the three frames did not become a clip");
            Assert.That(clip!.FrameCount, Is.EqualTo(3));
            Assert.That(clip.Frames.Select(f => f.name), Is.EqualTo(new[] { "spr_enemy_test_attack-left_01", "spr_enemy_test_attack-left_02", "spr_enemy_test_attack-left_03" }), "the frames are not in nn order");
            Assert.That(clip.LengthBeats, Is.EqualTo(1));
            Assert.That(clip.Loop, Is.False);
            Assert.That(clip.StrikeFrame, Is.EqualTo(2));

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(first);
            Assert.That(sprite, Is.Not.Null, "the first frame did not import as a sprite");
            Assert.That(sprite!.pixelsPerUnit, Is.EqualTo(100f));
            Assert.That(sprite.texture.filterMode, Is.EqualTo(FilterMode.Bilinear));
            Assert.That(sprite.pivot.x / sprite.rect.width, Is.EqualTo(0.5f).Within(0.001f), "the pivot is not centred horizontally");
            Assert.That(sprite.pivot.y / sprite.rect.height, Is.EqualTo(0f).Within(0.001f), "an enemy frame's pivot is not at the bottom");

            var importer = (TextureImporter)AssetImporter.GetAtPath(first);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(importer.mipmapEnabled, Is.False);
        }

        [Test]
        public void misnamed_file_rejected()
        {
            // The import error is asserted through the importer's own record, so the count of
            // logged errors never decides the test.
            LogAssert.ignoreFailingMessages = true;

            ArtFixture.Png("spr_enemy_test.png", 8, 16, Color.red);
            ArtFixture.Import();

            Assert.That(SpriteAssetPostprocessor.Errors, Is.Not.Empty, "the misnamed file imported without an error");
            Assert.That(SpriteAssetPostprocessor.Errors.Any(e => e.Contains("spr_enemy_test.png")), Is.True, "no import error names the file: " + string.Join("\n", SpriteAssetPostprocessor.Errors));

            Assert.That(AssetDatabase.FindAssets("t:" + nameof(SpriteClip), new[] { ArtFixture.Folder }), Is.Empty, "a clip was built from the misnamed file");

            var catalogue = ScriptableObject.CreateInstance<VisualCatalogue>();
            CatalogueRebuild.FillVisuals(catalogue);
            Assert.That(catalogue.Sprites, Is.Empty, "the misnamed file reached the catalogue");
            Assert.That(catalogue.Clips, Is.Empty, "the misnamed file reached the catalogue as a clip");
            Object.DestroyImmediate(catalogue);
        }
    }
}
