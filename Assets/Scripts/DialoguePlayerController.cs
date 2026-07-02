using UnityEngine;

public class DialoguePlayerController : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;

    public void DisablePlayer()
    {
        playerController.enabled = false;
    }

    public void EnablePlayer()
    {
        playerController.enabled = true;
    }
}