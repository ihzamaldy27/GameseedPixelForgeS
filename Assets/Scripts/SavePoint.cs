using UnityEngine;

public class SavePoint : MonoBehaviour
{
    // Variabel statis agar tersimpan selama game berjalan
    public static Vector2 lastCheckpointPosition = new Vector2(0, 0); 
    public Animator animator; // Referensi ke Animator untuk memicu animasi "Aktif"
    public bool isFirstSavePoint = false;

    void Start()
    {
        // Jika ini adalah save point awal, set posisi default
        if (isFirstSavePoint) lastCheckpointPosition = transform.position;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            lastCheckpointPosition = transform.position;
            Debug.Log("Checkpoint Saved at: " + lastCheckpointPosition);
            animator.enabled = true; // Tambahkan pemicu animasi "Aktif" di sini jika ada
        }
    }
}