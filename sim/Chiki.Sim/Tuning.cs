namespace Chiki.Sim
{
    /// <summary>
    /// The one home for every tuning constant. Each constant cites its PRD number.
    /// </summary>
    public static class Tuning
    {
        /// <summary>Baseline maximum ARD at the start of a run (PRD 3.2.3).</summary>
        public const int ArdBaseline = 300;

        /// <summary>
        /// Half-width of the Perfect window in thousandths of a beat (PRD 3.3.3.1, 3.3.1.5).
        /// 80 thousandths of a beat is +/-40 ms at BPM 120.
        /// </summary>
        public const int PerfectWindowBeatThousandths = 80;

        /// <summary>
        /// Half-width of the Good window in thousandths of a beat (PRD 3.3.3.1, 3.3.1.5).
        /// 180 thousandths of a beat is +/-90 ms at BPM 120, narrower than a quarter beat.
        /// </summary>
        public const int GoodWindowBeatThousandths = 180;

        /// <summary>
        /// Half-width of the Judgment Window itself, the region in which a press is accepted
        /// for an enemy action at all (PRD 3.3.1.8, 3.3.3.1), in thousandths of a beat. A press
        /// inside it but outside the Good window is a Miss. One quarter beat, so an action's
        /// window always closes before the next beat starts; neighbouring actions split the
        /// region at their midpoint (see <see cref="Battle"/>).
        /// </summary>
        public const int JudgmentWindowBeatThousandths = 250;

        /// <summary>IncomingMult on a Perfect, in thousandths: 0% (PRD 3.3.4.2).</summary>
        public const int IncomingMultPerfectThousandths = 0;

        /// <summary>IncomingMult on a Good, in thousandths: 50% (PRD 3.3.4.2).</summary>
        public const int IncomingMultGoodThousandths = 500;

        /// <summary>IncomingMult on a Miss, in thousandths: 100% (PRD 3.3.4.2).</summary>
        public const int IncomingMultMissThousandths = 1000;

        /// <summary>IncomingMult on no input, in thousandths: 100% (PRD 3.3.4.2, 3.3.3.2).</summary>
        public const int IncomingMultNoInputThousandths = 1000;

        /// <summary>JudgmentMult on a Perfect, in thousandths: 100% (PRD 3.3.4.3).</summary>
        public const int JudgmentMultPerfectThousandths = 1000;

        /// <summary>JudgmentMult on a Good, in thousandths: 50% (PRD 3.3.4.3).</summary>
        public const int JudgmentMultGoodThousandths = 500;

        /// <summary>JudgmentMult on a Miss, in thousandths: 0% (PRD 3.3.4.3).</summary>
        public const int JudgmentMultMissThousandths = 0;
    }
}
