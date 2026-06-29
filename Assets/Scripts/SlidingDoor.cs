using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    public enum MoveDirection { Up, Down, Left, Right, Custom }
    public enum DoorState { Closed, Open }

    [Header("Pengaturan Pergerakan")]
    public MoveDirection moveDirection = MoveDirection.Up;
    public DoorState doorState = DoorState.Closed;
    public SpriteRenderer doorSR;
    public Sprite doorOpen;         // Opsional: untuk mengubah sprite saat pintu terbuka/tutup
    public Sprite doorClosed;       // Opsional: untuk mengubah sprite saat pintu terbuka/tutup
    public float moveDistance = 3f; // Jarak bergeser (satuan unit grid Unity)
    public float moveSpeed = 3f;    // Kecepatan bergeser pintu
    public Vector2 customOffset;    // Hanya dipakai jika memilih MoveDirection.Custom

    private Vector2 startPosition;
    private Vector2 targetPosition;
    private bool shouldOpen = false;

    void Start()
    {
        // Simpan posisi awal pintu saat game dimulai
        startPosition = transform.position;
        
        // Hitung posisi target berdasarkan arah dan jarak yang ditentukan
        //CalculateTargetPosition();

        UpdateDoorSprite();
    }

    void Update()
    {
        if (doorSR != null)
        {
            doorSR.sprite = shouldOpen ? doorOpen : doorClosed; // Ganti sprite sesuai status pintu
        }

        // Tentukan posisi tujuan saat ini (jika open ke targetPosition, jika close kembali ke startPosition)
        //Vector2 currentTarget = shouldOpen ? targetPosition : startPosition;
        
        // Gerakkan pintu secara halus menggunakan MoveTowards
        //transform.position = Vector2.MoveTowards(transform.position, currentTarget, moveSpeed * Time.deltaTime);
    }

    private void CalculateTargetPosition()
    {
        switch (moveDirection)
        {
            case MoveDirection.Up:
                targetPosition = startPosition + Vector2.up * moveDistance;
                break;
            case MoveDirection.Down:
                targetPosition = startPosition + Vector2.down * moveDistance;
                break;
            case MoveDirection.Left:
                targetPosition = startPosition + Vector2.left * moveDistance;
                break;
            case MoveDirection.Right:
                targetPosition = startPosition + Vector2.right * moveDistance;
                break;
            case MoveDirection.Custom:
                targetPosition = startPosition + customOffset;
                break;
        }
    }

    private void UpdateDoorSprite()
    {
        switch (doorState)
        {
            case DoorState.Closed:
                if (doorSR != null) doorSR.sprite = doorClosed; // Atau sprite pintu tertutup
                break;
            case DoorState.Open:
                if (doorSR != null) doorSR.sprite = doorOpen; // Atau sprite pintu terbuka
                break;
        }
    }

    // Fungsi ini akan dipanggil oleh script Tuas
    public void ToggleDoor(bool open)
    {
        shouldOpen = open;

    }
}