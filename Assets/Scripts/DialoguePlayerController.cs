using UnityEngine;

public class DialoguePlayerController : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Rigidbody2D rb;

    public void DisablePlayer()
    {
        playerController.enabled = false;
    }

    public void EnablePlayer()
    {
        playerController.enabled = true;
    }

    public void StopMovement()
    {
        rb.linearVelocity = new Vector2(0f, 0f);
    }
}