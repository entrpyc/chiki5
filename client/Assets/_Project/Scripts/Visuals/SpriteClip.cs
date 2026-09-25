#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chiki.Client.Visuals
{
    /// <summary>The opaque part of one frame in pixels, recorded at import so a character's drawn height can be read without the texture.</summary>
    [Serializable]
    public struct OpaqueRect
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public OpaqueRect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public bool IsEmpty => Width <= 0 || Height <= 0;

        public override string ToString()
        {
            return X + "," + Y + " " + Width + "x" + Height;
        }
    }

    /// <summary>
    /// One drawn sequence: the frames of a <c>spr_&lt;kind&gt;_&lt;subject&gt;_&lt;variant&gt;</c>
    /// group in order, with the length in beats, the loop flag and the strike frame its sidecar
    /// gives (docs/project/unity-setup.md). Created by the sprite importer, played by
    /// <see cref="Chiki.Client.Visuals.BeatAnimator"/>. Frames are numbered from 1, as the
    /// files are; <see cref="StrikeFrame"/> is null for a clip that strikes nothing.
    /// </summary>
    public sealed class SpriteClip : ScriptableObject
    {
        [SerializeField] private string kind = "";
        [SerializeField] private string subject = "";
        [SerializeField] private string variant = "";
        [SerializeField] private Sprite[] frames = Array.Empty<Sprite>();
        [SerializeField] private int lengthBeats = 1;
        [SerializeField] private bool loop;
        [SerializeField] private int strikeFrame;
        [SerializeField] private OpaqueRect[] frameBounds = Array.Empty<OpaqueRect>();

        public string Kind => kind;

        public string Subject => subject;

        public string Variant => variant;

        /// <summary>The catalogue id: the subject alone for a static variant, subject and variant otherwise.</summary>
        public string Id => variant == SpriteNames.StaticVariant ? subject : subject + "-" + variant;

        public IReadOnlyList<Sprite> Frames => frames;

        public int FrameCount => frames.Length;

        /// <summary>The clip's authored length in whole beats; a looping clip wraps on it.</summary>
        public int LengthBeats => lengthBeats;

        public bool Loop => loop;

        /// <summary>The frame, counting from 1, that lands on the action's beat; null when the clip strikes nothing.</summary>
        public int? StrikeFrame => strikeFrame >= 1 ? strikeFrame : (int?)null;

        public Sprite? Frame(int index)
        {
            return index >= 0 && index < frames.Length ? frames[index] : null;
        }

        /// <summary>The opaque part of a frame, counting frames from 0; empty when it was not recorded.</summary>
        public OpaqueRect Bounds(int index)
        {
            return index >= 0 && index < frameBounds.Length ? frameBounds[index] : default;
        }

        /// <summary>Builds a clip in memory; the importer and the tests both use it.</summary>
        public static SpriteClip Create(string kind, string subject, string variant, IReadOnlyList<Sprite> frames, int lengthBeats, bool loop, int? strikeFrame, IReadOnlyList<OpaqueRect>? bounds = null)
        {
            if (frames is null)
            {
                throw new ArgumentNullException(nameof(frames));
            }

            var clip = CreateInstance<SpriteClip>();
            clip.Fill(kind, subject, variant, frames, lengthBeats, loop, strikeFrame, bounds);
            return clip;
        }

        /// <summary>Refills an existing clip asset in place, so a reimport keeps the references that point at it.</summary>
        public void Fill(string kind, string subject, string variant, IReadOnlyList<Sprite> frames, int lengthBeats, bool loop, int? strikeFrame, IReadOnlyList<OpaqueRect>? bounds = null)
        {
            if (frames is null)
            {
                throw new ArgumentNullException(nameof(frames));
            }

            if (lengthBeats <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lengthBeats), "A clip lasts at least one beat.");
            }

            if (strikeFrame.HasValue && (strikeFrame.Value < 1 || strikeFrame.Value > frames.Count))
            {
                throw new ArgumentOutOfRangeException(nameof(strikeFrame), "The strike frame must name one of the clip's " + frames.Count + " frames.");
            }

            this.kind = kind;
            this.subject = subject;
            this.variant = variant;
            this.frames = new Sprite[frames.Count];
            for (int i = 0; i < frames.Count; i++)
            {
                this.frames[i] = frames[i];
            }

            this.lengthBeats = lengthBeats;
            this.loop = loop;
            this.strikeFrame = strikeFrame ?? 0;
            this.frameBounds = new OpaqueRect[frames.Count];
            if (bounds != null)
            {
                for (int i = 0; i < frames.Count && i < bounds.Count; i++)
                {
                    this.frameBounds[i] = bounds[i];
                }
            }

            name = "clip_" + kind + "_" + subject + "_" + variant;
        }
    }
}
