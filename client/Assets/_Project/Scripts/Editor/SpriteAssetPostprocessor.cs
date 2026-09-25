#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Chiki.Client.Visuals;
using UnityEditor;
using UnityEngine;

namespace Chiki.Client.Editor
{
    /// <summary>
    /// Imports the art by its name (P1.2). Every PNG under <c>Assets/_Project/Art/</c> is read as
    /// <c>spr_&lt;kind&gt;_&lt;subject&gt;_&lt;variant&gt;_&lt;nn&gt;.png</c> and imported as a
    /// single sprite at 100 pixels per unit with bilinear filtering and no mipmaps, its pivot at
    /// bottom centre for the kinds that stand on the floor line — enemy, player and vfx — and
    /// centred otherwise. A file whose name does not follow the convention, or whose kind is not
    /// one of the listed ones, fails the import with an error naming it, and nothing is built
    /// from it.
    ///
    /// The frames of one kind, subject and variant of an animating kind become a
    /// <see cref="SpriteClip"/> beside them, in <c>nn</c> order, with the length in beats, the
    /// loop flag and the strike frame read from the subject's
    /// <c>&lt;kind&gt;_&lt;subject&gt;.clips.json</c>; a clip with no sidecar entry, or a strike
    /// frame past its last frame, fails the import. Every subject's sprites pack into one atlas
    /// beside them (see <see cref="SubjectAtlases"/>).
    /// </summary>
    public sealed class SpriteAssetPostprocessor : AssetPostprocessor
    {
        public const string ArtRoot = "Assets/_Project/Art/";
        public const string SidecarSuffix = ".clips.json";
        public const string ClipAssetPrefix = "clip_";

        /// <summary>The import errors logged since <see cref="ClearErrors"/>, newest last; the tests read them.</summary>
        private static readonly List<string> _errors = new List<string>();

        public static IReadOnlyList<string> Errors => _errors;

        public static void ClearErrors()
        {
            _errors.Clear();
        }

        /// <summary>Whether a project path is a PNG the conventions govern.</summary>
        public static bool IsArtSprite(string assetPath)
        {
            return assetPath.StartsWith(ArtRoot, StringComparison.Ordinal)
                && assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Bumped whenever the import settings this postprocessor applies change, so Unity
        /// reimports the art already in the project rather than leaving it on the old settings.
        /// Version 2 caps a background at 8192 px instead of the 2048 default (P5.3).
        /// </summary>
        public override uint GetVersion()
        {
            return 2;
        }

        private void OnPreprocessTexture()
        {
            if (!IsArtSprite(assetPath))
            {
                return;
            }

            string fileName = Path.GetFileName(assetPath);
            if (!SpriteNames.TryParse(fileName, out var name, out string reason))
            {
                Fail(assetPath, reason);
                return;
            }

            var importer = (TextureImporter)assetImporter;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.textureType = TextureImporterType.Sprite;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spritePixelsPerUnit = SpriteNames.PixelsPerUnit;
            settings.spriteAlignment = (int)(SpriteNames.IsClipKind(name!.Kind) ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
            settings.spriteBorder = BorderOf(Path.GetDirectoryName(assetPath)!.Replace('\\', '/'), name);
            settings.alphaIsTransparency = true;
            settings.mipmapEnabled = false;
            settings.filterMode = FilterMode.Bilinear;
            settings.wrapMode = TextureWrapMode.Clamp;
            importer.SetTextureSettings(settings);
            importer.maxTextureSize = SpriteNames.MaxTextureSize(name.Kind);
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var subjects = new HashSet<string>(StringComparer.Ordinal);
            Collect(importedAssets, subjects);
            Collect(deletedAssets, subjects);
            Collect(movedAssets, subjects);
            Collect(movedFromAssetPaths, subjects);
            if (subjects.Count == 0)
            {
                return;
            }

            foreach (string key in subjects)
            {
                int separator = key.LastIndexOf('|');
                RebuildSubject(key.Substring(0, separator), key.Substring(separator + 1));
            }
        }

        /// <summary>Adds the folder and subject every touched sprite or sidecar belongs to.</summary>
        private static void Collect(string[] paths, HashSet<string> subjects)
        {
            foreach (string path in paths)
            {
                if (!path.StartsWith(ArtRoot, StringComparison.Ordinal))
                {
                    continue;
                }

                string folder = Path.GetDirectoryName(path)!.Replace('\\', '/');
                string fileName = Path.GetFileName(path);
                if (IsArtSprite(path) && SpriteNames.TryParse(fileName, out var name, out _))
                {
                    subjects.Add(folder + "|" + name!.Subject);
                    continue;
                }

                string? suffix = fileName.EndsWith(SidecarSuffix, StringComparison.Ordinal) ? SidecarSuffix
                    : fileName.EndsWith(SliceSidecar.Suffix, StringComparison.Ordinal) ? SliceSidecar.Suffix
                    : null;
                if (suffix != null)
                {
                    string stem = fileName.Substring(0, fileName.Length - suffix.Length);
                    int underscore = stem.IndexOf('_');
                    if (underscore > 0)
                    {
                        subjects.Add(folder + "|" + stem.Substring(underscore + 1));
                    }
                }
            }
        }

        /// <summary>Rebuilds every clip of one subject inside one folder, then that subject's atlas.</summary>
        private static void RebuildSubject(string folder, string subject)
        {
            string fullFolder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", folder));
            if (!Directory.Exists(fullFolder))
            {
                SubjectAtlases.Update(folder, subject, Array.Empty<string>());
                return;
            }

            var framesByClip = new Dictionary<string, List<(int Index, string Path)>>(StringComparer.Ordinal);
            var kinds = new HashSet<string>(StringComparer.Ordinal);
            var all = new List<string>();

            foreach (string file in Directory.GetFiles(fullFolder, "*.png"))
            {
                string fileName = Path.GetFileName(file);
                if (!SpriteNames.TryParse(fileName, out var name, out _) || name!.Subject != subject)
                {
                    continue;
                }

                string assetPath = folder + "/" + fileName;
                all.Add(assetPath);
                if (!SpriteNames.IsClipKind(name.Kind))
                {
                    continue;
                }

                kinds.Add(name.Kind);
                if (!framesByClip.TryGetValue(name.ClipKey, out var frames))
                {
                    frames = new List<(int, string)>();
                    framesByClip[name.ClipKey] = frames;
                }

                frames.Add((name.Index, assetPath));
            }

            all.Sort(StringComparer.Ordinal);
            ApplyBorders(folder, all);
            var wanted = new HashSet<string>(StringComparer.Ordinal);
            foreach (string kind in kinds)
            {
                string sidecarName = SpriteNames.SidecarFileName(kind, subject);
                if (!ClipSidecar.TryRead(Path.Combine(fullFolder, sidecarName), out var specs, out string reason))
                {
                    Fail(folder + "/" + sidecarName, reason);
                    continue;
                }

                foreach (var pair in framesByClip)
                {
                    if (!pair.Key.StartsWith(kind + "_", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string variant = pair.Key.Substring(pair.Key.LastIndexOf('_') + 1);
                    if (BuildClip(folder, kind, subject, variant, pair.Value, specs, sidecarName, out string clipPath))
                    {
                        wanted.Add(clipPath);
                    }
                }
            }

            RemoveStaleClips(folder, fullFolder, subject, wanted);
            SubjectAtlases.Update(folder, subject, all);
        }

        /// <summary>Writes one clip asset beside its frames; false when the sidecar refuses it.</summary>
        private static bool BuildClip(string folder, string kind, string subject, string variant, List<(int Index, string Path)> frames, IReadOnlyDictionary<string, ClipSpec> specs, string sidecarName, out string clipPath)
        {
            clipPath = folder + "/" + ClipAssetPrefix + kind + "_" + subject + "_" + variant + ".asset";
            if (!specs.TryGetValue(variant, out var spec))
            {
                Fail(folder + "/" + sidecarName, "clip '" + variant + "' of " + kind + " '" + subject + "' has no entry");
                return false;
            }

            frames.Sort((a, b) => a.Index.CompareTo(b.Index));
            var sprites = new List<Sprite>(frames.Count);
            var bounds = new List<OpaqueRect>(frames.Count);
            foreach (var frame in frames)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(frame.Path);
                if (sprite == null)
                {
                    return false;
                }

                sprites.Add(sprite);
                bounds.Add(SpriteOpaqueBounds.Of(frame.Path));
            }

            if (spec.StrikeFrame.HasValue && spec.StrikeFrame.Value > sprites.Count)
            {
                Fail(folder + "/" + sidecarName, "clip '" + variant + "' strikes on frame " + spec.StrikeFrame.Value + ", past its last frame " + sprites.Count);
                return false;
            }

            var clip = AssetDatabase.LoadAssetAtPath<SpriteClip>(clipPath);
            if (clip == null)
            {
                clip = ScriptableObject.CreateInstance<SpriteClip>();
                clip.Fill(kind, subject, variant, sprites, spec.Beats, spec.Loop, spec.StrikeFrame, bounds);
                AssetDatabase.CreateAsset(clip, clipPath);
            }
            else
            {
                clip.Fill(kind, subject, variant, sprites, spec.Beats, spec.Loop, spec.StrikeFrame, bounds);
                EditorUtility.SetDirty(clip);
            }

            return true;
        }

        /// <summary>Deletes the clip assets of a subject whose frames or sidecar entry are gone.</summary>
        private static void RemoveStaleClips(string folder, string fullFolder, string subject, HashSet<string> wanted)
        {
            foreach (string file in Directory.GetFiles(fullFolder, ClipAssetPrefix + "*.asset"))
            {
                string fileName = Path.GetFileName(file);
                string stem = fileName.Substring(ClipAssetPrefix.Length, fileName.Length - ClipAssetPrefix.Length - ".asset".Length);
                string[] parts = stem.Split('_');
                if (parts.Length != 3 || parts[1] != subject)
                {
                    continue;
                }

                string assetPath = folder + "/" + fileName;
                if (!wanted.Contains(assetPath))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
            }
        }

        /// <summary>
        /// The 9-slice border a sprite carries, from its subject's slice sidecar (P2.1). A
        /// subject with no sidecar, or a variant the sidecar does not name, has no border; a
        /// sidecar that cannot be read fails the import of the sprite that asked for it.
        /// </summary>
        private static Vector4 BorderOf(string folder, SpriteName name)
        {
            string sidecarName = SpriteNames.SliceSidecarFileName(name.Kind, name.Subject);
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", folder, sidecarName));
            if (!SliceSidecar.TryRead(fullPath, out var borders, out string reason))
            {
                Fail(folder + "/" + sidecarName, reason);
                return Vector4.zero;
            }

            return borders.TryGetValue(name.Variant, out var spec)
                ? new Vector4(spec.Left, spec.Bottom, spec.Right, spec.Top)
                : Vector4.zero;
        }

        /// <summary>
        /// Reimports every sprite of a subject whose border no longer matches its sidecar, so
        /// editing a sidecar reslices the art it governs. Only a sprite that differs is touched,
        /// which is what stops the reimport it triggers from starting another one.
        /// </summary>
        private static void ApplyBorders(string folder, IReadOnlyList<string> spritePaths)
        {
            foreach (string assetPath in spritePaths)
            {
                if (!SpriteNames.TryParse(Path.GetFileName(assetPath), out var name, out _))
                {
                    continue;
                }

                if (!(AssetImporter.GetAtPath(assetPath) is TextureImporter importer))
                {
                    continue;
                }

                if (importer.spriteBorder == BorderOf(folder, name!))
                {
                    continue;
                }

                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
        }

        private static void Fail(string assetPath, string reason)
        {
            string message = "Chiki: " + assetPath + " failed to import: " + reason + ".";
            _errors.Add(message);
            Debug.LogError(message);
        }
    }
}
