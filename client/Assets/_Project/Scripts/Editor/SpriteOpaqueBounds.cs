#nullable enable
using System.IO;
using Chiki.Client.Visuals;
using UnityEngine;

namespace Chiki.Client.Editor
{
    /// <summary>
    /// The opaque part of a sprite in pixels, read from the PNG on disk rather than from the
    /// imported texture, so it works whatever the import settings are and needs no readable
    /// copy in memory. Bounds are recorded on every clip frame and on every catalogue entry, and
    /// the character specs are stated in them: Lulu and the cast stand 360 to 440 px tall
    /// (docs/project/unity-setup.md).
    /// </summary>
    public static class SpriteOpaqueBounds
    {
        /// <summary>The smallest rectangle holding every pixel with any alpha; empty when the file cannot be read or is wholly transparent.</summary>
        public static OpaqueRect Of(string assetPath)
        {
            var texture = Load(assetPath);
            if (texture == null)
            {
                return default;
            }

            try
            {
                return Of(texture);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        /// <summary>The smallest rectangle of a readable texture holding every pixel with any alpha, with y counted from the bottom.</summary>
        public static OpaqueRect Of(Texture2D texture)
        {
            if (texture == null)
            {
                return default;
            }

            var pixels = texture.GetPixels32();
            int width = texture.width;
            int height = texture.height;
            int minX = width;
            int minY = height;
            int maxX = -1;
            int maxY = -1;

            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    if (pixels[row + x].a == 0)
                    {
                        continue;
                    }

                    if (x < minX) { minX = x; }
                    if (x > maxX) { maxX = x; }
                    if (y < minY) { minY = y; }
                    if (y > maxY) { maxY = y; }
                }
            }

            return maxX < 0 ? default : new OpaqueRect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        /// <summary>The PNG at a project-relative path as a readable texture the caller destroys, or null.</summary>
        private static Texture2D? Load(string assetPath)
        {
            string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            if (!File.Exists(full))
            {
                return null;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (texture.LoadImage(File.ReadAllBytes(full), markNonReadable: false))
            {
                return texture;
            }

            Object.DestroyImmediate(texture);
            return null;
        }
    }
}
