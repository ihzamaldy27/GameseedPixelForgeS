using UnityEngine;

public class IndoorTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player")
        {
            GameSceneManager.instance.LoadSceneAsync(GameSceneManager.SceneName.Indoor1);
        }
    }
}
