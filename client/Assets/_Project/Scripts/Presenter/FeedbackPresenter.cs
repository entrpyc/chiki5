#nullable enable
using System;
using Chiki.Client.Audio;
using Chiki.Client.Driver;
using Chiki.Sim;
using UnityEngine;

namespace Chiki.Client.Presenter
{
    /// <summary>
    /// Judgment feedback (PRD 3.3.8.1): a cue per grade and a key flash on every graded input,
    /// a glow on the cards that can be played while the next action's Judgment Window is open,
    /// and a light camera shake when a hit costs more than <see cref="HeavyHitThreshold"/> ARD.
    /// The Rhythm Line highlights its own incoming actions. Everything here reads the event
    /// stream and battle state; nothing changes them.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FeedbackPresenter : MonoBehaviour, IBattlePresenter
    {
        /// <summary>ARD lost in one hit above which the hit counts as heavy (PRD 3.3.8.1).</summary>
        public const int HeavyHitThreshold = 15;

        /// <summary>The shake offset on a heavy hit, in world units.</summary>
        public const float ShakeAmplitude = 0.12f;

        private Sim.Battle? _battle;
        private Func<Slot, CardDefinition?>? _cardInSlot;
        private int _answeredIndex = -1;

        public JudgmentCues Cues { get; private set; } = null!;

        public SlotRowsView? Slots { get; private set; }

        public CameraShake? Shake { get; private set; }

        public BeatClock? Clock { get; private set; }

        public static FeedbackPresenter Build(BattleDriver driver, GameObject host, JudgmentCues cues, SlotRowsView? slots, CameraShake? shake, Func<Slot, CardDefinition?>? cardInSlot)
        {
            if (driver == null)
            {
                throw new ArgumentNullException(nameof(driver));
            }

            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            var presenter = host.AddComponent<FeedbackPresenter>();
            presenter.Cues = cues != null ? cues : throw new ArgumentNullException(nameof(cues));
            presenter.Slots = slots;
            presenter.Shake = shake;
            presenter.Clock = driver.Clock;
            presenter._battle = driver.Battle;
            presenter._cardInSlot = cardInSlot;
            driver.AttachPresenter(presenter);
            return presenter;
        }

        public void OnBattleEvent(Sim.Battle battle, BattleEvent battleEvent)
        {
            _battle = battle;
            switch (battleEvent)
            {
                case InputJudged judged:
                    Cues.Play(judged.Grade);
                    Slots?.Flash(judged.Slot, NowMs(battle));
                    _answeredIndex = judged.ActionIndex;
                    break;

                case DamageTaken taken when taken.Amount > HeavyHitThreshold:
                    Shake?.Trigger(NowMs(battle), HudFactory.BeatMs(battle), ShakeAmplitude);
                    break;
            }
        }

        private int NowMs(Sim.Battle battle)
        {
            return Clock != null && Clock.IsScheduled ? Math.Max(0, Clock.NowMs) : battle.CurrentTimeMs;
        }

        private void LateUpdate()
        {
            if (_battle == null || Slots == null)
            {
                return;
            }

            int now = NowMs(_battle);
            var pending = _battle.PendingAction;
            bool open = _battle.Outcome == null
                && pending.Contains(now)
                && pending.Index != _answeredIndex
                && !_battle.PlayerStatuses.Has(StatusKind.Stun);

            foreach (var widget in Slots.Widgets)
            {
                bool playable = open && widget.CooldownBeats == 0 && _cardInSlot?.Invoke(widget.Slot) != null;
                widget.SetGlow(playable);
            }
        }
    }
}
