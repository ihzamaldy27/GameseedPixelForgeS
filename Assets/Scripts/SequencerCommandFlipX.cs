using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandFlipX : SequencerCommand
    {
        private void Start()
        {
            Transform subject = GetSubject(0);
            bool flipX = GetParameterAsBool(1);
            SpriteRenderer _sprite = subject.GetComponent<SpriteRenderer>();
            _sprite.flipX = flipX;
            Stop();
        }
    }
}