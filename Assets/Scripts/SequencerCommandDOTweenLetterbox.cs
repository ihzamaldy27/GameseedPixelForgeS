using UnityEngine;
using UnityEngine.UI;
using PixelCrushers.DialogueSystem;
using DG.Tweening;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandDOTweenLetterbox : SequencerCommand
    {
        public void Start()
        {
            float targetHeight = GetParameterAsFloat(0);
            float duration = GetParameterAsFloat(1);
            
            // Find your letterbox UI images (top and bottom bars)
            RectTransform topBar = GameObject.Find("TopLetterbox").GetComponent<RectTransform>();
            RectTransform bottomBar = GameObject.Find("BottomLetterbox").GetComponent<RectTransform>();
            
            // Animate the height using DOTween
            topBar.DOSizeDelta(new Vector2(topBar.sizeDelta.x, targetHeight), duration);
            bottomBar.DOSizeDelta(new Vector2(bottomBar.sizeDelta.x, targetHeight), duration);
            
            Stop();
        }
    }
}
