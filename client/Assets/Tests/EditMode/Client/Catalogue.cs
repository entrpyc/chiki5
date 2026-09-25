#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        /// <summary>P4.1: the three recorded cues exist, are short, decompress on load and land on their first millisecond.</summary>
        [Test]
        public void judgment_cues_complete_and_tight()
        {
            var catalogue = ShippedAudio();
            foreach (var grade in new[] { Judgment.Perfect, Judgment.Good, Judgment.Miss })
            {
                string id = JudgmentCues.IdOf(grade);
                var clip = catalogue.Sound(id);
                Assert.That(clip, Is.Not.Null, id + " has no recording in the shipped audio catalogue");

                float milliseconds = clip!.length * 1000f;
                Assert.That(milliseconds, Is.LessThan(150f), id + " lasts " + milliseconds + " ms, at or over the 150 ms the plan allows");

                string assetPath = AssetDatabase.GetAssetPath(clip);
                var importer = (AudioImporter)AssetImporter.GetAtPath(assetPath);
                Assert.That(
                    importer.defaultSampleSettings.loadType,
                    Is.EqualTo(AudioClipLoadType.DecompressOnLoad),
                    id + " does not import as Decompress On Load, so it would decode on the beat");

                var samples = new float[clip.samples * clip.channels];
                Assert.That(clip.GetData(samples, 0), Is.True, id + " could not be read back");

                float peak = 0f;
                foreach (float sample in samples)
                {
                    peak = Mathf.Max(peak, Mathf.Abs(sample));
                }

                Assert.That(peak, Is.GreaterThan(0f), id + " is silent");

                int first = -1;
                for (int i = 0; i < samples.Length; i++)
                {
                    if (Mathf.Abs(samples[i]) >= peak / 2f)
                    {
                        first = i / clip.channels;
                        break;
                    }
                }

                Assert.That(first, Is.GreaterThanOrEqualTo(0));
                float attackMs = first * 1000f / clip.frequency;
                Assert.That(attackMs, Is.LessThan(5f), id + " reaches half its peak after " + attackMs + " ms, so the cue would drag behind the beat");
            }

            Assert.That(catalogue.Missing, Is.Empty, "a judgment cue fell back: " + string.Join(", ", catalogue.Missing));
        }

        /// <summary>P4.3: one icon per status, each at 64 by 64 and each a different image.</summary>
        [Test]
        public void status_icons_complete()
        {
            var catalogue = Shipped();
            var images = new Dictionary<string, byte[]>();
            foreach (StatusKind kind in System.Enum.GetValues(typeof(StatusKind)))
            {
                string id = StatusIconWidget.IdOf(kind);
                Assert.That(catalogue.Has(StatusIconWidget.StatusKindName, id), Is.True, id + " has no icon in the shipped catalogue");
                var sprite = catalogue.Sprite(StatusIconWidget.StatusKindName, id);
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(64f, 64f)), id + " is not a 64 by 64 icon");
                images[id] = File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite));
            }

            Assert.That(images, Has.Count.EqualTo(6), "the six statuses of PRD 3.3.7.1 were not all looked up");

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

            Assert.That(catalogue.Missing, Is.Empty, "a status icon fell back: " + string.Join(", ", catalogue.Missing));
        }

        /// <summary>P5.4: Lulu's seven clips, at the frame counts, loops and heights the plan states.</summary>
        [Test]
        public void lulu_art_complete()
        {
            AssertFighterArtComplete(StageView.PlayerKind, StageView.PlayerSubject, FighterClips.Player);
        }

        /// <summary>P5.5: Ren's seven clips, with the four-frame charge wind-up looping over one beat.</summary>
        [Test]
        public void enemy_ren_art_complete()
        {
            AssertEnemyArtComplete("ren");
        }

        /// <summary>P6.1: Kess's seven clips, on every condition Ren's are held to.</summary>
        [Test]
        public void enemy_kess_art_complete()
        {
            AssertEnemyArtComplete("kess");
        }

        /// <summary>P6.2: Vey's seven clips, on every condition Ren's are held to.</summary>
        [Test]
        public void enemy_vey_art_complete()
        {
            AssertEnemyArtComplete("vey");
        }

        /// <summary>P6.3: Orm's seven clips, on every condition Ren's are held to.</summary>
        [Test]
        public void enemy_orm_art_complete()
        {
            AssertEnemyArtComplete("orm");
        }

        /// <summary>P6.4: Malk's seven clips, on every condition Ren's are held to.</summary>
        [Test]
        public void enemy_malk_art_complete()
        {
            AssertEnemyArtComplete("malk");
        }

        /// <summary>
        /// One enemy's clip set (P5.5, and P6.1 to P6.4 on the same conditions): the seven clips
        /// every fighter carries, plus the four-frame charge wind-up that loops over one beat.
        /// </summary>
        private static void AssertEnemyArtComplete(string subject)
        {
            AssertFighterArtComplete(StageView.EnemyKind, subject, FighterClips.Enemy);
            var charge = Shipped().Clip(StageView.EnemyKind, subject, FighterClips.Charge);
            Assert.That(charge.FrameCount, Is.EqualTo(4), subject + "'s charge wind-up is not four frames");
            Assert.That(charge.Loop, Is.True, subject + "'s charge wind-up does not loop");
            Assert.That(charge.LengthBeats, Is.EqualTo(1), subject + "'s charge wind-up does not last one beat");
        }

        /// <summary>
        /// One fighter's clip set as the plan specifies it (P5.4, P5.5): all seven catalogued,
        /// an eight-frame idle looping over two beats, every other clip three to eight frames,
        /// a strike frame on each clip that lands on a beat, and a drawn height of 360 to 440 px.
        /// </summary>
        private static void AssertFighterArtComplete(string kind, string subject, IReadOnlyList<string> variants)
        {
            var catalogue = Shipped();
            foreach (string variant in variants)
            {
                string id = subject + "-" + variant;
                Assert.That(catalogue.HasClip(kind, id), Is.True, id + " is not in the shipped catalogue");
                var clip = catalogue.Clip(kind, subject, variant);
                Assert.That(clip.FrameCount, Is.InRange(3, 8), id + " has " + clip.FrameCount + " frames, outside the 3 to 8 the plan allows");
                if (FighterClips.Striking.Contains(variant))
                {
                    Assert.That(clip.StrikeFrame, Is.Not.Null, id + " has no strike frame");
                    Assert.That(clip.StrikeFrame!.Value, Is.InRange(1, clip.FrameCount), id + "'s strike frame is not one of its frames");
                }
            }

            var idle = catalogue.Clip(kind, subject, FighterClips.Idle);
            Assert.That(idle.FrameCount, Is.EqualTo(8), subject + "'s idle is not eight frames");
            Assert.That(idle.Loop, Is.True, subject + "'s idle does not loop");
            Assert.That(idle.LengthBeats, Is.EqualTo(2), subject + "'s idle does not last two beats");
            Assert.That(
                idle.Bounds(0).Height,
                Is.InRange(360, 440),
                subject + " stands " + idle.Bounds(0).Height + " px tall in the first idle frame, outside the 360 to 440 the plan states");

            Assert.That(
                catalogue.Missing.Where(id => id.StartsWith(kind + "/" + subject)),
                Is.Empty,
                subject + "'s art fell back: " + string.Join(", ", catalogue.Missing));
        }

        /// <summary>The catalogue the game ships with, with nothing recorded as missing yet.</summary>
        private static VisualCatalogue Shipped()
        {
            var catalogue = AssetDatabase.LoadAssetAtPath<VisualCatalogue>(VisualCatalogue.AssetPath);
            Assert.That(catalogue, Is.Not.Null, "no catalogue at " + VisualCatalogue.AssetPath + "; run Chiki > Rebuild Visual Catalogue");
            catalogue.ClearMissing();
            return catalogue;
        }

        /// <summary>The audio catalogue the game ships with, with nothing recorded as missing yet.</summary>
        private static AudioCatalogue ShippedAudio()
        {
            var catalogue = AssetDatabase.LoadAssetAtPath<AudioCatalogue>(AudioCatalogue.AssetPath);
            Assert.That(catalogue, Is.Not.Null, "no catalogue at " + AudioCatalogue.AssetPath + "; run Chiki > Rebuild Visual Catalogue");
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
