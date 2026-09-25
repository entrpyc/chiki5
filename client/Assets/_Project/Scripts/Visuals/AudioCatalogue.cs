#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chiki.Client.Visuals
{
    /// <summary>One sound in the catalogue: a sound-effect id or a track id and the clip it names.</summary>
    [Serializable]
    public sealed class AudioEntry
    {
        public string id = "";
        public AudioClip? clip;
    }

    /// <summary>
    /// The map from a sound id to its recording, beside the visual catalogue (P1.4). Sound
    /// effects are keyed by the ids the presenters ask for (<c>cue-perfect</c>, <c>cue-good</c>,
    /// <c>cue-miss</c>, <c>press-disabled</c>, <c>click-beat</c>, <c>click-accent</c>); tracks by
    /// the track id their sidecar under <c>data/tracks/</c> carries.
    ///
    /// A lookup returns null for an id the catalogue does not hold and records it in
    /// <see cref="Missing"/>; the caller then generates the tone or click track it needs, which
    /// is how the suites run before recordings ship.
    /// </summary>
    public sealed class AudioCatalogue : ScriptableObject
    {
        /// <summary>Where the shipped catalogue lives, for the rebuild command and the operator.</summary>
        public const string AssetPath = "Assets/_Project/Data/AudioCatalogue.asset";

        /// <summary>The sound-effect ids the client asks for (P1.4, P3.4, P4.1, P10.4).</summary>
        public static readonly IReadOnlyList<string> SoundIds = new[]
        {
            "cue-perfect", "cue-good", "cue-miss", "press-disabled", "click-beat", "click-accent",
        };

        private static AudioCatalogue? _active;

        [SerializeField] private List<AudioEntry> sounds = new List<AudioEntry>();
        [SerializeField] private List<AudioEntry> tracks = new List<AudioEntry>();

        private Dictionary<string, AudioEntry>? _soundIndex;
        private Dictionary<string, AudioEntry>? _trackIndex;
        private readonly List<string> _missing = new List<string>();

        /// <summary>The catalogue the client reads; an empty one until Boot hands over the shipped asset.</summary>
        public static AudioCatalogue Active
        {
            get
            {
                if (_active == null)
                {
                    _active = CreateInstance<AudioCatalogue>();
                    _active.name = "AudioCatalogue (empty)";
                }

                return _active;
            }
        }

        /// <summary>Boot hands the shipped catalogue over once (P1.4).</summary>
        public static void Use(AudioCatalogue catalogue)
        {
            _active = catalogue != null ? catalogue : throw new ArgumentNullException(nameof(catalogue));
        }

        public static void Clear()
        {
            _active = null;
        }

        /// <summary>Every id looked up that the catalogue does not hold, in the order they were first asked for.</summary>
        public IReadOnlyList<string> Missing => _missing;

        public IReadOnlyList<AudioEntry> Sounds => sounds;

        public IReadOnlyList<AudioEntry> Tracks => tracks;

        public void ClearMissing()
        {
            _missing.Clear();
        }

        /// <summary>The recording of a sound-effect id, or null when none has shipped.</summary>
        public AudioClip? Sound(string id)
        {
            var entry = Find(SoundIndex(), id);
            if (entry?.clip != null)
            {
                return entry.clip;
            }

            RecordMissing("sfx/" + id);
            return null;
        }

        /// <summary>The recording of a track id, or null when none has shipped.</summary>
        public AudioClip? Track(string id)
        {
            var entry = Find(TrackIndex(), id);
            if (entry?.clip != null)
            {
                return entry.clip;
            }

            RecordMissing("track/" + id);
            return null;
        }

        public bool HasSound(string id)
        {
            return Find(SoundIndex(), id)?.clip != null;
        }

        public bool HasTrack(string id)
        {
            return Find(TrackIndex(), id)?.clip != null;
        }

        public void PutSound(string id, AudioClip clip)
        {
            Put(sounds, SoundIndex(), id, clip);
        }

        public void PutTrack(string id, AudioClip clip)
        {
            Put(tracks, TrackIndex(), id, clip);
        }

        /// <summary>Empties the catalogue before a rebuild fills it again.</summary>
        public void RemoveAll()
        {
            sounds.Clear();
            tracks.Clear();
            _soundIndex = null;
            _trackIndex = null;
            _missing.Clear();
        }

        private static void Put(List<AudioEntry> list, Dictionary<string, AudioEntry> index, string id, AudioClip clip)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("An id is required.", nameof(id));
            }

            if (clip == null)
            {
                throw new ArgumentNullException(nameof(clip));
            }

            if (!index.TryGetValue(id, out var entry))
            {
                entry = new AudioEntry { id = id };
                list.Add(entry);
                index[id] = entry;
            }

            entry.clip = clip;
        }

        private static AudioEntry? Find(Dictionary<string, AudioEntry> index, string id)
        {
            return id != null && index.TryGetValue(id, out var entry) ? entry : null;
        }

        private Dictionary<string, AudioEntry> SoundIndex()
        {
            return _soundIndex ??= Build(sounds);
        }

        private Dictionary<string, AudioEntry> TrackIndex()
        {
            return _trackIndex ??= Build(tracks);
        }

        private static Dictionary<string, AudioEntry> Build(List<AudioEntry> list)
        {
            var index = new Dictionary<string, AudioEntry>(StringComparer.Ordinal);
            foreach (var entry in list)
            {
                index[entry.id] = entry;
            }

            return index;
        }

        private void RecordMissing(string key)
        {
            if (!_missing.Contains(key))
            {
                _missing.Add(key);
            }
        }
    }
}
