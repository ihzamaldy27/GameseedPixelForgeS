using UnityEngine;

public class AfterimageFade : MonoBehaviour
{
    [Header("Settings Bayangan")]
    [SerializeField] private float activeTime = 0.1f; // Berapa lama bayangan muncul sebelum mulai pudar
    [SerializeField] private float fadeSpeed = 0.1f; // Kecepatan memudar (1 = instant, < 1 = lambat)
    [SerializeField] private float startingAlpha = 0.4f; // Transparansi awal (0-1)

    private SpriteRenderer sr;
    private float timeActive;
    private Color color;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    // Fungsi ini dipanggil setiap kali bayangan diaktifkan (dari Pool)
    public void SetAfterimage(Sprite sprite, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        transform.position = position;
        transform.rotation = rotation;
        transform.localScale = scale;
        sr.sprite = sprite;

        color = Color.white; // Reset warna ke putih
        color.a = startingAlpha; // Set alpha awal
        sr.color = color;

        timeActive = 0f;
        gameObject.SetActive(true); // Aktifkan objek
    }

    private void Update()
    {
        timeActive += Time.deltaTime;

        if (timeActive >= activeTime)
        {
            // Mulai memudar
            color.a -= fadeSpeed * Time.deltaTime;
            sr.color = color;

            if (color.a <= 0f)
            {
                // Sudah tidak terlihat, matikan untuk dikembalikan ke Pool
                gameObject.SetActive(false);
            }
        }
    }
}