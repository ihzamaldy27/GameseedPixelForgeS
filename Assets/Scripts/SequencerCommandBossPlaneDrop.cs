using UnityEngine;
using PixelCrushers.DialogueSystem;
using DG.Tweening;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandBossPlaneDrop : SequencerCommand
    {
        public void Start()
        {
            float targetY = GetParameterAsFloat(0);
            float duration = GetParameterAsFloat(1);
            Transform _plane = GameObject.Find("BossPlane").transform;
            DOTween.To(() => _plane.position, x => _plane.position = x, new Vector3(_plane.position.x, targetY, 0), duration);
            Stop();
        }
    }
}