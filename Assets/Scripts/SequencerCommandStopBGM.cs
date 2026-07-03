using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandStopBGM : SequencerCommand
    {
        public void Start()
        {
            float decayTime = GetParameterAsFloat(0);
            AudioManager.instance.StopBGM(decayTime);
            Stop();
        }
    }
}
