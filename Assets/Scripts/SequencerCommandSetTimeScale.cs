using UnityEngine;
using PixelCrushers.DialogueSystem;
using DG.Tweening;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandSetTimeScale : SequencerCommand
    {
        public void Start()
        {
            float _timeScale = GetParameterAsFloat(0);
            float _duration = GetParameterAsFloat(1);

            DOTween.To(() => Time.timeScale, x => Time.timeScale = x, _timeScale, _duration);

            Stop();
        }
    }
}
