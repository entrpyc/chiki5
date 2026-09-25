#nullable enable
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Chiki.Client.Editor
{
    /// <summary>
    /// One Sprite Atlas per subject beside its frames (P1.2). This settles the contradiction in
    /// docs/project/unity-setup.md between one atlas per enemy and one per World in favour of
    /// one per subject: a battle touches one enemy, so a subject's frames are what belongs on a
    /// page together. Pages are 2048 px, doubled up to 8192 when the frames do not fit.
    ///
    /// The project packs with Sprite Atlas V2 (ProjectSettings/EditorSettings.asset), so the
    /// asset written is a <c>.spriteatlasv2</c> through <see cref="SpriteAtlasAsset"/>.
    /// </summary>
    public static class SubjectAtlases
    {
        public const int SmallPage = 2048;
        public const int LargePage = 4096;
        public const int LargestPage = 8192;
        public const int Padding = 4;

        /// <summary>The platform key of an atlas's default texture settings.</summary>
        public const string DefaultPlatform = "DefaultTexturePlatform";

        /// <summary>The atlas of a subject inside one art folder.</summary>
        public static string PathFor(string folder, string subject)
        {
            return folder + "/atlas_" + subject + ".spriteatlasv2";
        }

        /// <summary>
        /// Writes the atlas holding every given sprite, or removes it when the subject has no
        /// sprites left. Never throws: an atlas is packing, and a packing problem must not fail
        /// the import of the art itself.
        /// </summary>
        public static void Update(string folder, string subject, IReadOnlyList<string> spritePaths)
        {
            string path = PathFor(folder, subject);
            try
            {
                if (spritePaths.Count == 0)
                {
                    if (File.Exists(FullPath(path)))
                    {
                        AssetDatabase.DeleteAsset(path);
                    }

                    return;
                }

                var packables = new List<Object>(spritePaths.Count);
                long area = 0;
                int widest = 0;
                foreach (string spritePath in spritePaths)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    if (sprite == null)
                    {
                        continue;
                    }

                    packables.Add(sprite);
                    int width = Mathf.CeilToInt(sprite.rect.width) + Padding * 2;
                    int height = Mathf.CeilToInt(sprite.rect.height) + Padding * 2;
                    area += (long)width * height;
                    widest = Mathf.Max(widest, Mathf.Max(width, height));
                }

                if (packables.Count == 0)
                {
                    return;
                }

                // Full-screen art (the menu backdrop, the map) keeps its source size up to 8192
                // (SpriteNames.MaxTextureSize), so a page of 4096 is not always enough either.
                int page = SmallPage;
                while (page < LargestPage && (widest > page || area > (long)page * page))
                {
                    page *= 2;
                }

                // The atlas is written whole every time: its contents are exactly the subject's
                // sprites, so there is nothing in an older one worth merging.
                var atlas = new SpriteAtlasAsset();
                var packing = atlas.GetPackingSettings();
                packing.enableTightPacking = false;
                packing.enableRotation = false;
                packing.padding = Padding;
                atlas.SetPackingSettings(packing);

                var texture = atlas.GetTextureSettings();
                texture.filterMode = FilterMode.Bilinear;
                texture.generateMipMaps = false;
                atlas.SetTextureSettings(texture);

                atlas.Add(packables.ToArray());
                SpriteAtlasAsset.Save(atlas, path);
                AssetDatabase.ImportAsset(path);

                // The page belongs to the importer beside the asset, not to the asset: a V2 atlas
                // saves with no platform entries, so a page set on the asset is dropped and packing
                // falls back to the 2048 default, which rejects full-screen frames. The atlas's own
                // texture settings cannot carry it either — their maxTextureSize is read only.
                if (AssetImporter.GetAtPath(path) is SpriteAtlasImporter importer)
                {
                    var platform = importer.GetPlatformSettings(DefaultPlatform);
                    if (platform.maxTextureSize != page)
                    {
                        platform.maxTextureSize = page;
                        importer.SetPlatformSettings(platform);
                        importer.SaveAndReimport();
                    }
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("Chiki: the atlas for subject '" + subject + "' in " + folder + " could not be written: " + exception.Message);
            }
        }

        private static string FullPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }
    }
}
