using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 10f;

    private void Update()
    {
        transform.Translate(Vector3.right * speed * Time.deltaTime);
    }

    // Optional: destroy when off-screen
    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}
