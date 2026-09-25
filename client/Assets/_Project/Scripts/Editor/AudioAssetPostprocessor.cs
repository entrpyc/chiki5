#nullable enable
using System;
using System.IO;
using Chiki.Client.Visuals;
using UnityEditor;
using UnityEngine;

namespace Chiki.Client.Editor
{
    /// <summary>
    /// Imports the sound by its name (P1.4). Under <c>Assets/_Project/Audio/</c> a sound effect
    /// (<c>sfx_*</c>) decompresses on load so a cue plays without decode latency, and a music
    /// track (<c>mus_*</c>) is compressed in memory with its audio data preloaded, so scheduling
    /// the track on the beat clock never waits on disk (PRD 3.3.1.6).
    /// </summary>
    public sealed class AudioAssetPostprocessor : AssetPostprocessor
    {
        public const string AudioRoot = "Assets/_Project/Audio/";

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(AudioRoot, StringComparison.Ordinal))
            {
                return;
            }

            string fileName = Path.GetFileName(assetPath);
            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;

            if (AudioNames.SoundId(fileName) != null)
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.preloadAudioData = true;
                settings.compressionFormat = AudioCompressionFormat.PCM;
            }
            else if (AudioNames.TryMusic(fileName, out _, out _))
            {
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.preloadAudioData = true;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
            }
            else
            {
                return;
            }

            importer.forceToMono = false;
            importer.defaultSampleSettings = settings;
        }
    }
}
