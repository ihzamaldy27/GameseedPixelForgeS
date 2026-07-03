using UnityEngine;
using PixelCrushers.DialogueSystem;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    public class SequencerCommandPlayerCombatAnim : SequencerCommand
    {
        public void Start()
        {
            Animator playerAnim = GameObject.Find("MainPlayer").GetComponent<Animator>();
            RuntimeAnimatorController controller = Resources.Load<RuntimeAnimatorController>("MainCharacter");
            playerAnim.runtimeAnimatorController = controller;
            Stop();
        }
    }
}

