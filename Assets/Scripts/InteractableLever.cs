using UnityEngine;
using UnityEngine.InputSystem; // WAJIB DITAMBAHKAN untuk New Input System

public class InteractableLever : MonoBehaviour
{
    [Header("Target Pintu (Bisa Lebih Dari 1)")]
    [SerializeField] private SlidingDoor[] targetDoors; 

    [Header("Visual Visual Tuas")]
    [SerializeField] private Sprite leverOnSprite;
    [SerializeField] private Sprite leverOffSprite;

    [Header("Input Interaksi")]
    // Mengganti KeyCode dengan InputActionReference dari New Input System
    [SerializeField] private InputActionReference interactAction; 

    private SpriteRenderer sr;
    [SerializeField] private bool isActivated = false;
    private bool playerInRange = false;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null && leverOffSprite != null)
        {
            sr.sprite = leverOffSprite; 
        }
    }

    // WAJIB: Mengaktifkan pendeteksi input saat objek ini aktif
    private void OnEnable()
    {
        if (interactAction != null) interactAction.action.Enable();
    }

    // WAJIB: Mematikan pendeteksi input saat objek ini mati (mencegah memory leak)
    private void OnDisable()
    {
        if (interactAction != null) interactAction.action.Disable();
    }

    void Update()
    {
        // Mengecek apakah player di area DAN tombol interaksi ditekan pada frame ini
        if (playerInRange && interactAction != null && interactAction.action.WasPressedThisFrame())
        {
            ToggleLever();
        }
    }

    private void ToggleLever()
    {
        isActivated = !isActivated;

        // 1. Ubah visual sprite tuas
        if (sr != null)
        {
            sr.sprite = isActivated ? leverOnSprite : leverOffSprite;
        }

        // 2. Perintahkan seluruh pintu di dalam daftar untuk bergerak
        if (targetDoors != null)
        {
            foreach (SlidingDoor door in targetDoors)
            {
                if (door != null)
                {
                    door.ToggleDoor(isActivated); 
                }
            }
        }

        Debug.Log(isActivated ? "Tuas Aktif: Membuka Pintu!" : "Tuas Nonaktif: Menutup Pintu!");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("Dekat dengan tuas. Tekan tombol interaksi.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("Meninggalkan area tuas.");
        }
    }
}