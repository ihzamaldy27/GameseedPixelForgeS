using UnityEngine;

public class BulletChase : MonoBehaviour
{
    private Transform target;
    private float speed;

    public void Initialize(Transform t, float s) { target = t; speed = s; }

    void Update()
    {
        if (target == null) return;
        Vector2 dir = (target.position - transform.position).normalized;
        transform.Translate(dir * speed * Time.unscaledDeltaTime); // Unscaled so slow-mo doesn't affect bullet direction
    }
}