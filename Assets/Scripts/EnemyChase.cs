using UnityEngine;
using PixelCrushers.DialogueSystem;

public class EnemyChase : MonoBehaviour
{
    public float chaseSpeed = 3f;
    public Transform player;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Check the Dialogue System's Lua variable every frame
        if (DialogueLua.GetVariable("isChasing").AsBool)
        {
            // Chase the player to the right
            //Vector2 targetPos = new Vector2(player.position.x, rb.position.y);
            //ector2 newPos = Vector2.MoveTowards(rb.position, targetPos, chaseSpeed * Time.deltaTime);
            //rb.MovePosition(newPos);
            rb.linearVelocity = new Vector2(chaseSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }
}