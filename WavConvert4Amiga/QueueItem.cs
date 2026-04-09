using System.Collections.Generic;

namespace WavConvert4Amiga
{
    internal enum QueueItemStatus
    {
        Queued,
        Processing,
        Done,
        Failed
    }

    internal sealed class QueueItem
    {
        public string FilePath { get; set; }
        public int TargetSampleRate { get; set; }
        public bool ApplyAmplify { get; set; }
        public float AmplificationFactor { get; set; }
        public bool ApplyLowPass { get; set; }
        public bool ApplyEffects { get; set; }
        public List<string> EffectsSnapshot { get; set; } = new List<string>();
        public QueueItemStatus Status { get; set; }
        public string OutputPath { get; set; }
        public string ProfileName { get; set; }
        public string ErrorMessage { get; set; }
        public bool AutoConvert { get; set; }
        public bool MoveOriginal { get; set; }
        public bool SaveAs8Svx { get; set; }
        public bool SaveAs16BitWav { get; set; }
    }
}
