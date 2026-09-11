#nullable enable
using System;
using System.Collections.Generic;
using Chiki.Client.Audio;
using Chiki.Sim;
using UnityEngine;

namespace Chiki.Client.Driver
{
    /// <summary>
    /// Owns one <see cref="Sim.Battle"/> for the client (P12.5): the beat clock ticks it from
    /// audio time, timestamped inputs are forwarded to it, and every event it appends is handed
    /// to the attached presenters in order. Rules never live here; the driver only moves time
    /// and inputs in and events out. In tests and replays a scripted input list drives it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleDriver : MonoBehaviour
    {
        private readonly List<IBattlePresenter> _presenters = new List<IBattlePresenter>();
        private readonly List<ScriptedInput> _script = new List<ScriptedInput>();
        private readonly List<BeatTick> _ticks = new List<BeatTick>();
        private readonly List<SlotPress> _inputs = new List<SlotPress>();
        private BeatClock? _clock;
        private int _dispatched;
        private int _scriptNext;

        public Sim.Battle? Battle { get; private set; }

        public BeatClock? Clock => _clock;

        /// <summary>Every beat delivered to the simulation so far, in order.</summary>
        public IReadOnlyList<BeatTick> Ticks => _ticks;

        /// <summary>Every slot press received from the keys so far, in order, whether or not a card sat in the slot.</summary>
        public IReadOnlyList<SlotPress> Inputs => _inputs;

        /// <summary>
        /// The profile's calibration offset in milliseconds (PRD 3.12.1), subtracted from the
        /// stamp of every live input before it is graded (PRD 3.3.8.2). Scripted inputs carry
        /// stamps that are already calibrated and are forwarded as they are.
        /// </summary>
        public int CalibrationOffsetMs { get; set; }

        /// <summary>
        /// A slot press from the keys (PRD 3.3.2.1): recorded, then forwarded to the battle with
        /// the card the slot holds and the calibration offset taken off its stamp; an empty slot
        /// forwards nothing. Returns the battle's answer, or null when nothing was forwarded.
        /// </summary>
        public PressResult? Receive(SlotPress press, CardDefinition? card)
        {
            if (press is null)
            {
                throw new ArgumentNullException(nameof(press));
            }

            _inputs.Add(press);
            if (Battle == null || card is null)
            {
                return null;
            }

            return PressAt(press.Slot, card, Calibrated(press.AudioTimeMs), press.SignatureSend);
        }

        /// <summary>A live input's stamp with the calibration offset taken off (PRD 3.3.8.2).</summary>
        public int Calibrated(int audioTimeMs)
        {
            return audioTimeMs - CalibrationOffsetMs;
        }

        /// <summary>
        /// Binds the driver to a battle and the clock that ticks it. Nothing is delivered until
        /// the clock reaches audio time 0: the simulation starts its beat 0 when constructed, and
        /// the events of that start go to presenters on the first tick after the track starts.
        /// </summary>
        public void Bind(BeatClock clock, Sim.Battle battle)
        {
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            if (Battle != null)
            {
                throw new InvalidOperationException("The driver already owns a battle.");
            }

            _clock = clock;
            Battle = battle ?? throw new ArgumentNullException(nameof(battle));
            _clock.Ticked += OnTick;
        }

        public void AttachPresenter(IBattlePresenter presenter)
        {
            if (presenter is null)
            {
                throw new ArgumentNullException(nameof(presenter));
            }

            _presenters.Add(presenter);
        }

        /// <summary>Queues presses to forward when the clock reaches their audio time; each is stamped with its own time, not the frame's.</summary>
        public void Script(IEnumerable<ScriptedInput> inputs)
        {
            if (inputs is null)
            {
                throw new ArgumentNullException(nameof(inputs));
            }

            _script.AddRange(inputs);
            _script.Sort((a, b) => a.AudioTimeMs.CompareTo(b.AudioTimeMs));
        }

        /// <summary>
        /// Forwards a live press stamped with the audio time of its input event, from the
        /// event's realtime timestamp (P12.3), less the calibration offset (PRD 3.3.8.2). Space
        /// plus the key is a Signature send (PRD 3.3.2.3).
        /// </summary>
        public PressResult Press(Slot slot, CardDefinition card, double inputTime, bool signatureSend = false)
        {
            var clock = RequireClock();
            return PressAt(slot, card, Calibrated(clock.AudioTimeMsAt(inputTime)), signatureSend);
        }

        /// <summary>Forwards a press already stamped with its calibrated audio time.</summary>
        public PressResult PressAt(Slot slot, CardDefinition card, int audioTimeMs, bool signatureSend = false)
        {
            var battle = RequireBattle();
            var result = signatureSend ? battle.Send(slot, card, audioTimeMs) : battle.Press(slot, card, audioTimeMs);
            Dispatch();
            return result;
        }

        /// <summary>Moves the battle to an audio time: due scripted inputs go first, each at its own stamp, then the beats up to now.</summary>
        public void Advance(int audioTimeMs)
        {
            var battle = RequireBattle();
            while (_scriptNext < _script.Count && _script[_scriptNext].AudioTimeMs <= audioTimeMs)
            {
                var input = _script[_scriptNext++];
                PressAt(input.Slot, input.Card, input.AudioTimeMs, input.SignatureSend);
            }

            battle.Advance(audioTimeMs);
            Dispatch();
        }

        private void OnTick(int audioTimeMs)
        {
            if (Battle == null || Battle.Outcome != null || audioTimeMs < 0)
            {
                return;
            }

            Advance(audioTimeMs);
        }

        private void Dispatch()
        {
            var battle = RequireBattle();
            var events = battle.Events;
            while (_dispatched < events.Count)
            {
                var battleEvent = events[_dispatched++];
                if (battleEvent is BeatStarted beat)
                {
                    _ticks.Add(new BeatTick(beat.Beat, battle.BeatMap.TimeAtQb(beat.PositionQb), AudioSettings.dspTime));
                }

                foreach (var presenter in _presenters)
                {
                    presenter.OnBattleEvent(battle, battleEvent);
                }
            }
        }

        private Sim.Battle RequireBattle()
        {
            return Battle ?? throw new InvalidOperationException("The driver has no battle; call Bind first.");
        }

        private BeatClock RequireClock()
        {
            return _clock ?? throw new InvalidOperationException("The driver has no clock; call Bind first.");
        }

        private void OnDestroy()
        {
            if (_clock != null)
            {
                _clock.Ticked -= OnTick;
            }
        }
    }
}
