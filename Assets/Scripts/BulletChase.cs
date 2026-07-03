using UnityEngine;
using System.Collections;

public class BulletChase : MonoBehaviour
{
    private Transform target;
    private float speed;

    public void Initialize(Transform t, float s) { target = t; speed = s; }

    void Start()
    {
        StartCoroutine(DestroyTimer());
    }

    void Update()
    {
        if (target == null) return;
        Vector2 _targetPos = new Vector2(target.position.x, target.position.y - 0.5f);
        Vector2 _pos = new Vector2(transform.position.x, transform.position.y);
        Vector2 dir = (_targetPos - _pos).normalized;
        transform.Translate(dir * speed * Time.unscaledDeltaTime); // Unscaled so slow-mo doesn't affect bullet direction
    }

    IEnumerator DestroyTimer()
    {
        while (true)
        {
            yield return new WaitForSeconds(3);
            SelfDestruct();
        }
    }

    private void SelfDestruct()
    {
        StopCoroutine(DestroyTimer());
        Destroy(this.gameObject);
    }
}