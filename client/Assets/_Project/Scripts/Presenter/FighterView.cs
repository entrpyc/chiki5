#nullable enable
using System;
using System.Collections.Generic;
using Chiki.Client.Audio;
using Chiki.Client.Visuals;
using Chiki.Sim;
using UnityEngine;

namespace Chiki.Client.Presenter
{
    /// <summary>
    /// The clip every fighter on the stage carries, by the names their sidecars use
    /// (docs/project/unity-setup.md): idle, two attacks, defend, hit and death for both, with the
    /// enemy's charge wind-up standing where the player's Ability is.
    /// </summary>
    public static class FighterClips
    {
        public const string Idle = "idle";
        public const string AttackLeft = "attack-left";
        public const string AttackRight = "attack-right";
        public const string Defend = "defend";
        public const string Charge = "charge";
        public const string Ability = "ability";
        public const string Hit = "hit";
        public const string Death = "death";

        /// <summary>The seven an enemy carries.</summary>
        public static readonly IReadOnlyList<string> Enemy = new[] { Idle, AttackLeft, AttackRight, Defend, Charge, Hit, Death };

        /// <summary>The seven the player carries: the enemy's set with Ability in place of the charge wind-up.</summary>
        public static readonly IReadOnlyList<string> Player = new[] { Idle, AttackLeft, AttackRight, Defend, Ability, Hit, Death };

        /// <summary>The clips whose strike frame lands on an action's beat.</summary>
        public static readonly IReadOnlyList<string> Striking = new[] { AttackLeft, AttackRight, Ability };

        /// <summary>The clip an enemy plays for a charted action; null for a kind the seven-clip set has none for.</summary>
        public static string? ForAction(EnemyActionKind kind)
        {
            switch (kind)
            {
                case EnemyActionKind.AttackLeft:
                    return AttackLeft;
                case EnemyActionKind.AttackRight:
                    return AttackRight;
                case EnemyActionKind.Defend:
                    return Defend;

                // A Charge resolves on the attack-left clip: the seven-clip set has no release
                // clip, and the wind-up loop covers the beats before it (docs/plan.md P5.1).
                case EnemyActionKind.Charge:
                    return AttackLeft;
                default:
                    return null;
            }
        }

        /// <summary>The clip the player plays for a press, by the Category the slot's key fixes (PRD 3.4.1).</summary>
        public static string ForSlot(Slot slot)
        {
            switch (CardCategories.ForKey(slot.Key))
            {
                case CardCategory.LeftAttack:
                    return AttackLeft;
                case CardCategory.RightAttack:
                    return AttackRight;
                case CardCategory.Defense:
                    return Defend;
                default:
                    return Ability;
            }
        }
    }

    /// <summary>
    /// One clip started on the stage at an audio time. <see cref="Hold"/> marks the one that is
    /// never handed back to the idle loop — a death holds its last frame (P5.1).
    /// </summary>
    public sealed record StageCue(string Variant, SpriteClip Clip, int StartMs, bool Hold);

    /// <summary>
    /// One fighter standing on the stage (P5.1, P5.2): a sprite on the floor line playing the
    /// subject's clips from the beat clock through a <see cref="BeatAnimator"/>.
    ///
    /// What is on screen is a timeline, not a live variable. Every clip started is appended as a
    /// <see cref="StageCue"/> at the audio time it began, which for a strike is early enough that
    /// its strike frame lands exactly on the action's beat. <see cref="VariantAt"/> and
    /// <see cref="FrameAt"/> answer for any audio time from that list, so the stage can be asked
    /// what it showed on a beat rather than being sampled on the frame that happened to follow it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FighterView : MonoBehaviour
    {
        private readonly List<StageCue> _cues = new List<StageCue>();
        private readonly Dictionary<string, SpriteClip> _clips = new Dictionary<string, SpriteClip>(StringComparer.Ordinal);
        private BeatClock? _clock;
        private BeatCursor? _cursor;
        private BeatMap? _map;
        private BeatAnimator _animator = null!;
        private int _applied = -1;

        public SpriteRenderer Renderer { get; private set; } = null!;

        public BeatAnimator Animator => _animator;

        /// <summary>The kind the subject's art is catalogued under: <c>player</c> or <c>enemy</c>.</summary>
        public string Kind { get; private set; } = "";

        /// <summary>The subject the clips belong to: the content id without its kind prefix, such as <c>ren</c>.</summary>
        public string Subject { get; private set; } = "";

        /// <summary>Which way the fighter looks; the shipped art is drawn facing its own side of the stage.</summary>
        public bool FacesLeft { get; private set; }

        /// <summary>Every clip started, in order; the first is the idle loop the fighter stands in.</summary>
        public IReadOnlyList<StageCue> Cues => _cues;

        /// <summary>Whether a clip that holds its last frame has started, after which nothing else plays.</summary>
        public bool Holding => _cues.Count > 0 && _cues[_cues.Count - 1].Hold;

        /// <summary>The audio time of the last render.</summary>
        public int RenderedAtMs { get; private set; }

        public bool HasRendered { get; private set; }

        /// <summary>Builds a fighter under the stage, standing with its feet on the floor line.</summary>
        public static FighterView Build(Transform parent, string name, string kind, string subject, Vector2 stand, bool facesLeft, BeatClock? clock, BeatMap? map, int sortingOrder)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            var host = new GameObject(name);
            host.transform.SetParent(parent, false);
            host.transform.localPosition = new Vector3(stand.x, stand.y, 0f);

            var view = host.AddComponent<FighterView>();
            view.Kind = kind;
            view.Subject = subject;
            view.FacesLeft = facesLeft;
            view._clock = clock;
            view._map = map;
            view.Renderer = host.AddComponent<SpriteRenderer>();
            view.Renderer.sortingOrder = sortingOrder;
            view._animator = BeatAnimator.On(host, clock);
            view._animator.Driven = true;

            foreach (string variant in kind == "player" ? FighterClips.Player : FighterClips.Enemy)
            {
                view._clips[variant] = VisualCatalogue.Active.Clip(kind, subject, variant);
            }

            view.Play(FighterClips.Idle, 0);
            return view;
        }

        /// <summary>The subject's clip for a variant; the catalogue's fallback when the art is owed.</summary>
        public SpriteClip Clip(string variant)
        {
            return _clips.TryGetValue(variant, out var clip) ? clip : VisualCatalogue.Active.Clip(Kind, Subject, variant);
        }

        /// <summary>Starts a clip at an audio time. A fighter that is holding its last frame starts nothing more.</summary>
        public void Play(string variant, int audioTimeMs, bool hold = false)
        {
            if (Holding)
            {
                return;
            }

            var clip = Clip(variant);
            var last = _cues.Count > 0 ? _cues[_cues.Count - 1] : null;
            if (last != null && last.Variant == variant && last.StartMs == audioTimeMs)
            {
                return;
            }

            _cues.Add(new StageCue(variant, clip, Math.Max(0, audioTimeMs), hold));
        }

        /// <summary>
        /// Starts a clip early enough that its strike frame is on screen on the action's beat
        /// (docs/project/unity-setup.md): one frame is one quarter beat, so a clip striking on
        /// frame <c>n</c> begins <c>n - 1</c> quarter beats before the landing.
        /// </summary>
        public void PlayStrikeAt(string variant, int landingQb)
        {
            var clip = Clip(variant);
            int strike = clip.StrikeFrame ?? 1;
            Play(variant, TimeAtQb(Math.Max(0, landingQb - (strike - 1))));
        }

        /// <summary>The cue on screen at an audio time; null before the first one started.</summary>
        public StageCue? CueAt(int audioTimeMs)
        {
            StageCue? found = null;
            foreach (var cue in _cues)
            {
                if (cue.StartMs > audioTimeMs)
                {
                    break;
                }

                found = cue;
            }

            return found;
        }

        /// <summary>The variant on screen at an audio time: the cue's, or the idle loop once a one-shot has run out.</summary>
        public string VariantAt(int audioTimeMs)
        {
            var cue = CueAt(audioTimeMs);
            if (cue == null)
            {
                return FighterClips.Idle;
            }

            return !cue.Hold && cue.Clip.FinishedAt(OrdinalOf(cue, audioTimeMs)) ? FighterClips.Idle : cue.Variant;
        }

        /// <summary>The frame on screen at an audio time, counting from 1 as the files are numbered.</summary>
        public int FrameAt(int audioTimeMs)
        {
            var cue = CueAt(audioTimeMs);
            if (cue == null)
            {
                return 0;
            }

            if (!cue.Hold && cue.Clip.FinishedAt(OrdinalOf(cue, audioTimeMs)))
            {
                return Clip(FighterClips.Idle).FrameIndexAt(QbAt(audioTimeMs)) + 1;
            }

            return cue.Clip.FrameIndexAt(OrdinalOf(cue, audioTimeMs)) + 1;
        }

        /// <summary>Shows the frame an audio time falls on, starting whichever cue owns that time.</summary>
        public void Render(int audioTimeMs)
        {
            int index = IndexAt(audioTimeMs);
            if (index >= 0 && index != _applied)
            {
                _applied = index;
                var cue = _cues[index];
                if (cue.Variant == FighterClips.Idle)
                {
                    _animator.PlayIdle(cue.Clip);
                }
                else
                {
                    _animator.Play(cue.Clip, cue.StartMs);
                }
            }

            _animator.Render(audioTimeMs);
            RenderedAtMs = audioTimeMs;
            HasRendered = true;
        }

        /// <summary>The quarter beats between a cue's start and an audio time: the clip's own frame ordinal.</summary>
        private int OrdinalOf(StageCue cue, int audioTimeMs)
        {
            return Math.Max(0, QbAt(audioTimeMs) - QbAt(cue.StartMs));
        }

        private int IndexAt(int audioTimeMs)
        {
            int index = -1;
            for (int i = 0; i < _cues.Count; i++)
            {
                if (_cues[i].StartMs > audioTimeMs)
                {
                    break;
                }

                index = i;
            }

            return index;
        }

        private BeatMap? Map()
        {
            return _map ?? (_clock != null && _clock.Track != null ? _clock.Track.BeatMap : null);
        }

        private int TimeAtQb(int positionQb)
        {
            var map = Map();
            return map == null ? 0 : map.TimeAtQb(Math.Max(0, positionQb));
        }

        private int QbAt(int audioTimeMs)
        {
            var map = Map();
            if (map == null)
            {
                return 0;
            }

            if (_cursor == null || !ReferenceEquals(_cursor.Map, map))
            {
                _cursor = new BeatCursor(map);
            }

            return _cursor.QbAt(audioTimeMs);
        }
    }
}
