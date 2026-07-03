using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandSetGirlPosition : SequencerCommand
    {
        public void Start()
        {
            float x = GetParameterAsFloat(0);
            float y = GetParameterAsFloat(1);
            bool isFlipped = GetParameterAsBool(2);
            GameObject _girl = GameObject.Find("MysteriousGirl");
            _girl.transform.position = new Vector2(x, y);
            _girl.GetComponent<SpriteRenderer>().flipX = isFlipped;
            Stop();
        }
    }
}