using UnityEngine;
using System.Collections;

public class FragilePlatform : MonoBehaviour
{
    [Header("Pengaturan Fragile")]
    public float delayBeforeShake = 0.5f; // Waktu tunggu sebelum mulai goyang
    public float shakeDuration = 1.0f;    // Durasi goyang
    public float respawnTime = 3.0f;      // Waktu respawn kembali

    private Vector2 startPosition;
    private Collider2D col;
    private SpriteRenderer sr;
    private bool isShaking = false;

    void Start()
    {
        startPosition = transform.position;
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Hanya trigger jika yang menginjak adalah Player
        if (collision.gameObject.CompareTag("Player") && !isShaking)
        {
            StartCoroutine(FragileRoutine());
        }
    }

    private IEnumerator FragileRoutine()
    {
        isShaking = true;

        // 1. Jeda sebelum mulai goyang
        yield return new WaitForSeconds(delayBeforeShake);

        // 2. Efek goyang sederhana (menggeser posisi secara acak)
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float offsetX = Random.Range(-0.1f, 0.1f);
            float offsetY = Random.Range(-0.1f, 0.1f);
            transform.position = startPosition + new Vector2(offsetX, offsetY);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 3. Hancurkan / Hilangkan Platform
        transform.position = startPosition;
        col.enabled = false;
        sr.enabled = false;

        // 4. Tunggu waktu respawn
        yield return new WaitForSeconds(respawnTime);

        // 5. Respawn (Munculkan kembali)
        col.enabled = true;
        sr.enabled = true;
        isShaking = false;
    }
}