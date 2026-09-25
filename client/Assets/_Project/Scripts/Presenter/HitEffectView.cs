#nullable enable
using Chiki.Client.Audio;
using Chiki.Client.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace Chiki.Client.Presenter
{
    /// <summary>
    /// The effect a heavy hit bursts over the player (PRD 3.3.8.1, P4.2): the catalogue's
    /// <c>hit-heavy</c> burst played through a <see cref="BeatAnimator"/>, so its frames step
    /// from audio time like everything else that moves with the music, and hidden again as soon
    /// as the clip's last frame is past. Nothing here decides what counts as heavy; the
    /// <see cref="FeedbackPresenter"/> reads that from the event stream and calls
    /// <see cref="Play"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitEffectView : MonoBehaviour
    {
        /// <summary>The kind the effect's frames are catalogued under.</summary>
        public const string VfxKind = "vfx";

        /// <summary>The subject and variant of the heavy-hit burst (P4.2).</summary>
        public const string HeavyHitSubject = "hit-heavy";

        public const string BurstVariant = "burst";

        /// <summary>The burst's drawn size, the art's own (P4.2).</summary>
        public const float Size = 256f;

        private Image _image = null!;
        private BeatAnimator _animator = null!;
        private BeatClock? _clock;

        public Image Image => _image;

        public BeatAnimator Animator => _animator;

        /// <summary>Whether a burst is on screen right now.</summary>
        public bool Shown => _image.gameObject.activeSelf;

        /// <summary>The clip the burst plays, from the visual catalogue (P1.3).</summary>
        public static SpriteClip Clip()
        {
            return VisualCatalogue.Active.Clip(VfxKind, HeavyHitSubject, BurstVariant);
        }

        public static HitEffectView Build(RectTransform parent, Vector2 anchoredPosition, BeatClock? clock)
        {
            var image = HudFactory.Image("HitEffect", parent, Color.white, anchoredPosition, new Vector2(Size, Size));
            var view = image.gameObject.AddComponent<HitEffectView>();
            view._image = image;
            view._clock = clock;
            view._animator = BeatAnimator.On(image.gameObject, clock);
            image.gameObject.SetActive(false);
            return view;
        }

        /// <summary>Bursts at an audio time, with the clip's strike frame on that beat (P1.5).</summary>
        public void Play(int audioTimeMs)
        {
            _image.gameObject.SetActive(true);
            _animator.PlayStrikeAt(Clip(), audioTimeMs);
            _animator.Render(audioTimeMs);
        }

        private void LateUpdate()
        {
            if (!Shown || _clock == null || !_clock.IsScheduled)
            {
                return;
            }

            if (_animator.FinishedAt(_clock.NowMs))
            {
                _image.gameObject.SetActive(false);
            }
        }
    }
}
