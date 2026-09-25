#nullable enable
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Client
{
    /// <summary>
    /// Art the importer tests generate: PNGs and clip sidecars written into a folder of their own
    /// under <c>Art/</c> and imported through the asset database, then deleted. Phase 1 needs
    /// nothing from the operator, so every sprite these tests read is made here.
    /// </summary>
    internal static class ArtFixture
    {
        public const string Folder = "Assets/_Project/Art/_Fixtures";

        public static string FullFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", Folder));

        /// <summary>Removes the fixture folder and everything in it.</summary>
        public static void Delete()
        {
            if (AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.DeleteAsset(Folder);
            }

            if (Directory.Exists(FullFolder))
            {
                Directory.Delete(FullFolder, true);
            }

            string meta = FullFolder + ".meta";
            if (File.Exists(meta))
            {
                File.Delete(meta);
            }

            AssetDatabase.Refresh();
        }

        /// <summary>Writes a solid PNG of the given size into the fixture folder and returns its project path.</summary>
        public static string Png(string fileName, int width, int height, Color color)
        {
            Directory.CreateDirectory(FullFolder);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            var value = (Color32)color;
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = value;
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(FullFolder, fileName), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            return Folder + "/" + fileName;
        }

        /// <summary>Writes a text file, a clip sidecar among them, into the fixture folder.</summary>
        public static string Text(string fileName, string contents)
        {
            Directory.CreateDirectory(FullFolder);
            File.WriteAllText(Path.Combine(FullFolder, fileName), contents);
            return Folder + "/" + fileName;
        }

        /// <summary>A sidecar naming one clip's beats, loop flag and strike frame.</summary>
        public static string Sidecar(string kind, string subject, params (string Variant, int Beats, bool Loop, int? Strike)[] clips)
        {
            var lines = new List<string>();
            foreach (var clip in clips)
            {
                string strike = clip.Strike.HasValue ? ", \"strikeFrame\": " + clip.Strike.Value : "";
                lines.Add("    \"" + clip.Variant + "\": { \"beats\": " + clip.Beats + ", \"loop\": " + (clip.Loop ? "true" : "false") + strike + " }");
            }

            string json = "{\n  \"clips\": {\n" + string.Join(",\n", lines) + "\n  }\n}\n";
            return Text(kind + "_" + subject + ".clips.json", json);
        }

        /// <summary>Imports everything written so far, in one synchronous pass, so the postprocessor sees the whole set.</summary>
        public static void Import()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
