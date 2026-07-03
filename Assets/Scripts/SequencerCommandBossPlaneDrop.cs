using UnityEngine;
using PixelCrushers.DialogueSystem;
using DG.Tweening;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandBossPlaneDrop : SequencerCommand
    {
        public void Start()
        {
            float initX = GetParameterAsFloat(0);
            float initY = GetParameterAsFloat(1);
            float targetX = GetParameterAsFloat(2);
            float targetY = GetParameterAsFloat(3);
            float duration = GetParameterAsFloat(4);
            Transform _plane = GameObject.Find("BossPlane").transform;
            _plane.position = new Vector2(initX, initY);
            DOTween.To(() => _plane.position, x => _plane.position = x, new Vector3(targetX, targetY, 0), duration);
            Stop();
        }
    }
}