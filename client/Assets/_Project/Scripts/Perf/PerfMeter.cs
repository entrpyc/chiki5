#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Chiki.Client.Perf
{
    /// <summary>One counted audio underrun: when in the window it happened and how far the DSP clock had fallen behind.</summary>
    public sealed record AudioUnderrun(double AtSeconds, double LagMs);

    /// <summary>What a measurement window recorded (PRD 6.2).</summary>
    public sealed record PerfReport(int Frames, double AverageMs, double OnePercentLowMs, double MaxMs, IReadOnlyList<AudioUnderrun> Underruns, int Width, int Height, double Seconds, double RingMs)
    {
        public int AudioUnderruns => Underruns.Count;

        public override string ToString()
        {
            var text = new StringBuilder();
            text.AppendFormat(CultureInfo.InvariantCulture,
                "frames={0} avg={1:F2}ms 1%low={2:F2}ms max={3:F2}ms underruns={4} ring={5:F1}ms res={6}x{7} seconds={8:F1}",
                Frames, AverageMs, OnePercentLowMs, MaxMs, AudioUnderruns, RingMs, Width, Height, Seconds);
            foreach (var underrun in Underruns)
            {
                text.AppendFormat(CultureInfo.InvariantCulture, " [underrun at {0:F2}s lag {1:F1}ms]", underrun.AtSeconds, underrun.LagMs);
            }

            return text.ToString();
        }
    }

    /// <summary>
    /// Measures frame time and audio underruns over a window (PRD 6.2). Frame time is the
    /// unscaled time between frames; the 1% low is the mean of the slowest 1% of frames. The
    /// DSP clock advances one output buffer at a time as the mixer fills the output ring, so it
    /// normally trails the realtime clock by up to one buffer; an audio underrun is counted
    /// whenever it falls behind by more than the whole ring plus one buffer, the point past which
    /// the device has nothing left to play. This is a measurement, so it is the one place in the client that
    /// reads frame time.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PerfMeter : MonoBehaviour
    {
        private readonly List<float> _frameMs = new List<float>();
        private readonly List<AudioUnderrun> _underruns = new List<AudioUnderrun>();
        private double _realStart;
        private double _dspStart;
        private double _realLast;
        private double _ringSeconds;
        private double _thresholdSeconds;
        private bool _skipNext;

        public bool Running { get; private set; }

        public int AudioUnderruns => _underruns.Count;

        public int Frames => _frameMs.Count;

        public void Begin()
        {
            _frameMs.Clear();
            _underruns.Clear();
            AudioSettings.GetDSPBufferSize(out int bufferLength, out int bufferCount);
            int rate = AudioSettings.outputSampleRate;
            double bufferSeconds = rate > 0 && bufferLength > 0 ? bufferLength / (double)rate : 0.0214;
            _ringSeconds = bufferSeconds * Math.Max(1, bufferCount);
            _thresholdSeconds = _ringSeconds + bufferSeconds;
            _realStart = Time.realtimeSinceStartupAsDouble;
            _dspStart = AudioSettings.dspTime;
            _realLast = _realStart;
            _skipNext = true;
            Running = true;
        }

        public PerfReport Stop()
        {
            Running = false;
            double seconds = Time.realtimeSinceStartupAsDouble - _realStart;
            var underruns = new List<AudioUnderrun>(_underruns);
            if (_frameMs.Count == 0)
            {
                return new PerfReport(0, 0, 0, 0, underruns, Screen.width, Screen.height, seconds, _ringSeconds * 1000.0);
            }

            var sorted = new List<float>(_frameMs);
            sorted.Sort();
            double sum = 0;
            foreach (var ms in sorted)
            {
                sum += ms;
            }

            int lowCount = Math.Max(1, sorted.Count / 100);
            double lowSum = 0;
            for (int i = sorted.Count - lowCount; i < sorted.Count; i++)
            {
                lowSum += sorted[i];
            }

            return new PerfReport(sorted.Count, sum / sorted.Count, lowSum / lowCount, sorted[sorted.Count - 1], underruns, Screen.width, Screen.height, seconds, _ringSeconds * 1000.0);
        }

        private void Update()
        {
            if (!Running)
            {
                return;
            }

            double real = Time.realtimeSinceStartupAsDouble;
            if (_skipNext)
            {
                _skipNext = false;
            }
            else
            {
                _frameMs.Add((float)((real - _realLast) * 1000.0));
            }

            _realLast = real;

            double lag = (real - _realStart) - (AudioSettings.dspTime - _dspStart);
            if (lag > _thresholdSeconds)
            {
                _underruns.Add(new AudioUnderrun(real - _realStart, lag * 1000.0));
                _dspStart -= lag;
            }
        }
    }
}
