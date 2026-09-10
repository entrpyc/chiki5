#nullable enable
using Chiki.Sim;

namespace Chiki.Client.Driver
{
    /// <summary>A press for the driver to forward at an audio time, already stamped: the test and replay form of an input (PRD 6.8).</summary>
    public sealed record ScriptedInput(int AudioTimeMs, Slot Slot, CardDefinition Card, bool SignatureSend = false);

    /// <summary>One beat the driver delivered to the simulation: which beat, its audio time on the beat map and the DSP time it was delivered at.</summary>
    public sealed record BeatTick(int Beat, int AudioTimeMs, double DeliveredDspTime);
}
