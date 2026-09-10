#nullable enable
using Chiki.Sim;

namespace Chiki.Client.Driver
{
    /// <summary>A press for the driver to forward at an audio time, already stamped: the test and replay form of an input (PRD 6.8).</summary>
    public sealed record ScriptedInput(int AudioTimeMs, Slot Slot, CardDefinition Card, bool SignatureSend = false);

    /// <summary>A slot key went down (PRD 3.3.2.1): the slot on the active line, the audio time of the key event (P12.3) and whether Space was held (PRD 3.3.2.3).</summary>
    public sealed record SlotPress(Slot Slot, int AudioTimeMs, bool SignatureSend);

    /// <summary>One beat the driver delivered to the simulation: which beat, its audio time on the beat map and the DSP time it was delivered at.</summary>
    public sealed record BeatTick(int Beat, int AudioTimeMs, double DeliveredDspTime);
}
