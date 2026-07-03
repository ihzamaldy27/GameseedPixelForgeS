using UnityEngine;
using PixelCrushers.DialogueSystem;


namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandStopPlayer : SequencerCommand
    {
        public void Start()
        {
            DialoguePlayerController _player = GameObject.FindWithTag("Player").GetComponent<DialoguePlayerController>();
            _player.StopMovement();
            Stop();
        }
    }
}
