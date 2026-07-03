using UnityEngine;

public class SpaceshipTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player")
        {
            GameSceneManager.instance.LoadSceneAsync(GameSceneManager.SceneName.SpaceShooterScene);
        }
    }
}
