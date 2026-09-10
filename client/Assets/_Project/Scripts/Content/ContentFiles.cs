#nullable enable
using System.IO;
using UnityEngine;

namespace Chiki.Client.Content
{
    /// <summary>
    /// Where the client finds the JSON content under <c>data/</c>: the repository folder while
    /// running in the editor, and <c>StreamingAssets/data</c> in a player, where a build step
    /// copies it. Definitions never reference art; visuals come from the catalogues.
    /// </summary>
    public static class ContentFiles
    {
        public static string DataRoot
        {
            get
            {
#if UNITY_EDITOR
                return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "data"));
#else
                return Path.Combine(Application.streamingAssetsPath, "data");
#endif
            }
        }

        /// <summary>The text of a content file named relative to <see cref="DataRoot"/>, such as <c>tracks/fixture-120.json</c>.</summary>
        public static string ReadText(string relativePath)
        {
            return File.ReadAllText(Path.Combine(DataRoot, relativePath));
        }
    }
}
