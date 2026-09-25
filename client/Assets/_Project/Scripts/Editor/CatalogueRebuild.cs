#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Chiki.Client.Content;
using Chiki.Client.Visuals;
using Chiki.Sim.Data;
using UnityEditor;
using UnityEngine;

namespace Chiki.Client.Editor
{
    /// <summary>
    /// Fills the visual and audio catalogues from the imported assets (P1.3, P1.4), so the
    /// operator never edits them by hand. Every sprite under <c>Assets/_Project/Art/</c> named by
    /// the conventions becomes an entry under its kind and content id; the frames of the kinds
    /// that animate are reached through the <see cref="SpriteClip"/> the importer wrote beside
    /// them. Every sound under <c>Assets/_Project/Audio/</c> becomes a sound-effect entry, and
    /// every music file the entry of the track whose sidecar under <c>data/tracks/</c> names the
    /// same World and subject. The font family under <c>Assets/_Project/UI/Fonts/</c> rides on the
    /// visual catalogue (P10.1).
    /// </summary>
    public static class CatalogueRebuild
    {
        public const string MenuPath = "Chiki/Rebuild Visual Catalogue";

        [MenuItem(MenuPath)]
        public static void Rebuild()
        {
            var visuals = LoadOrCreate<VisualCatalogue>(VisualCatalogue.AssetPath);
            var audio = LoadOrCreate<AudioCatalogue>(AudioCatalogue.AssetPath);
            int sprites = FillVisuals(visuals);
            int sounds = FillAudio(audio);
            EditorUtility.SetDirty(visuals);
            EditorUtility.SetDirty(audio);
            AssetDatabase.SaveAssets();
            Debug.Log("Chiki: the catalogues hold " + sprites + " visual and " + sounds + " audio entries.");
        }

        /// <summary>Empties a visual catalogue and fills it from every imported sprite and clip; returns the number of entries.</summary>
        public static int FillVisuals(VisualCatalogue catalogue)
        {
            if (catalogue == null)
            {
                throw new ArgumentNullException(nameof(catalogue));
            }

            catalogue.RemoveAll();
            int count = 0;
            var regular = AssetDatabase.LoadAssetAtPath<Font>(VisualCatalogue.FontFolder + "/" + VisualCatalogue.RegularFontFile);
            var bold = AssetDatabase.LoadAssetAtPath<Font>(VisualCatalogue.FontFolder + "/" + VisualCatalogue.BoldFontFile);
            catalogue.PutFonts(regular, bold);
            count += (regular != null ? 1 : 0) + (bold != null ? 1 : 0);
            foreach (string assetPath in ArtFiles("*.png"))
            {
                if (!SpriteNames.TryParse(Path.GetFileName(assetPath), out var name, out _))
                {
                    continue;
                }

                if (SpriteNames.IsClipKind(name!.Kind))
                {
                    continue;
                }

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite == null)
                {
                    continue;
                }

                catalogue.Put(name.Kind, name.Id, sprite, name.Subject, name.Variant, SpriteOpaqueBounds.Of(assetPath));
                count++;
            }

            foreach (string assetPath in ArtFiles(SpriteAssetPostprocessor.ClipAssetPrefix + "*.asset"))
            {
                var clip = AssetDatabase.LoadAssetAtPath<SpriteClip>(assetPath);
                if (clip == null || clip.FrameCount == 0)
                {
                    continue;
                }

                catalogue.PutClip(clip.Kind, clip.Id, clip, clip.Subject, clip.Variant);
                count++;
            }

            return count;
        }

        /// <summary>Empties an audio catalogue and fills it from every imported sound and track; returns the number of entries.</summary>
        public static int FillAudio(AudioCatalogue catalogue)
        {
            if (catalogue == null)
            {
                throw new ArgumentNullException(nameof(catalogue));
            }

            catalogue.RemoveAll();
            var trackIds = TrackIds();
            var oggTracks = new HashSet<string>(StringComparer.Ordinal);
            int count = 0;
            foreach (string assetPath in Files(AudioAssetPostprocessor.AudioRoot, "*.*"))
            {
                string fileName = Path.GetFileName(assetPath);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                if (clip == null)
                {
                    continue;
                }

                string? soundId = AudioNames.SoundId(fileName);
                if (soundId != null)
                {
                    catalogue.PutSound(soundId, clip);
                    count++;
                    continue;
                }

                if (!AudioNames.TryMusic(fileName, out int world, out string subject))
                {
                    continue;
                }

                string? trackId = MatchTrack(trackIds, world, subject);
                if (trackId == null)
                {
                    Debug.LogWarning("Chiki: " + fileName + " names no track under data/tracks; no catalogue entry was made for it.");
                    continue;
                }

                // A recording (OGG) wins over a generated stand-in (WAV) of the same name, so a
                // real track replaces the stand-in by being added beside it (docs/plan.md, Generators).
                bool isOgg = fileName.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase);
                if (oggTracks.Contains(trackId) && !isOgg)
                {
                    continue;
                }

                if (isOgg)
                {
                    oggTracks.Add(trackId);
                }

                if (!catalogue.HasTrack(trackId))
                {
                    count++;
                }

                catalogue.PutTrack(trackId, clip);
            }

            return count;
        }

        private static string? MatchTrack(IReadOnlyList<(string Id, int World)> tracks, int world, string subject)
        {
            foreach (var track in tracks)
            {
                if (track.World == world && AudioNames.TrackIdNames(track.Id, subject))
                {
                    return track.Id;
                }
            }

            return null;
        }

        /// <summary>Every track sidecar under data/tracks by id and World.</summary>
        private static IReadOnlyList<(string Id, int World)> TrackIds()
        {
            var tracks = new List<(string, int)>();
            string folder = Path.Combine(ContentFiles.DataRoot, "tracks");
            if (!Directory.Exists(folder))
            {
                return tracks;
            }

            foreach (string file in Directory.GetFiles(folder, "*.json"))
            {
                try
                {
                    var track = TrackLoader.FromJson(File.ReadAllText(file));
                    tracks.Add((track.Id, track.World));
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Chiki: " + Path.GetFileName(file) + " could not be read as a track sidecar: " + exception.Message);
                }
            }

            return tracks;
        }

        private static IEnumerable<string> ArtFiles(string pattern)
        {
            return Files(SpriteAssetPostprocessor.ArtRoot, pattern);
        }

        private static IEnumerable<string> Files(string assetFolder, string pattern)
        {
            string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetFolder));
            if (!Directory.Exists(full))
            {
                yield break;
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            foreach (string file in Directory.GetFiles(full, pattern, SearchOption.AllDirectories))
            {
                string relative = Path.GetFullPath(file).Substring(projectRoot.Length + 1).Replace('\\', '/');
                if (!relative.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    yield return relative;
                }
            }
        }

        private static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                return asset;
            }

            string folder = Path.GetDirectoryName(assetPath)!.Replace('\\', '/');
            Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "..", folder)));
            AssetDatabase.Refresh();
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }
    }
}
