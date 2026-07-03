using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement; // Wajib untuk fungsi pindah scene

public class PauseMenuManager : MonoBehaviour
{
    [Header("Status (Otomatis)")]
    public static bool GameIsPaused = false;

    [Header("UI References")]
    [Tooltip("Masukkan objek Panel Pause Menu dari Canvas ke sini")]
    public GameObject pauseMenuUI;

    [Header("Input Action")]
    [Tooltip("Pilih Action untuk Pause (misal: tombol ESC)")]
    [SerializeField] private InputActionReference pauseAction;

    private void Start()
    {
        // Pastikan menu pause mati saat game baru dimulai
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
    }

    private void OnEnable()
    {
        if (pauseAction != null) pauseAction.action.Enable();
    }

    private void OnDisable()
    {
        if (pauseAction != null) pauseAction.action.Disable();
    }

    private void Update()
    {
        // Cek apakah tombol pause ditekan pada frame ini
        if (pauseAction != null && pauseAction.action.WasPressedThisFrame())
        {
            if (GameIsPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    // --- FUNGSI UNTUK TOMBOL UI ---

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f; // Kembalikan waktu berjalan normal
        GameIsPaused = false;
        
        // Opsional: Jika kamu punya SFX klik tombol
        if (AudioManager.instance != null) AudioManager.instance.PlaySFX("Click"); 
    }

    public void PauseGame()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; // Hentikan seluruh waktu dan fisika di dalam game
        GameIsPaused = true;
        
        if (AudioManager.instance != null) AudioManager.instance.PlaySFX("Click");
    }

    public void RestartLevel()
    {
        if (AudioManager.instance != null) AudioManager.instance.PlaySFX("Click");
        
        // SANGAT PENTING: Waktu harus dikembalikan ke 1 sebelum pindah scene!
        // Jika tidak, level akan ikut ter-pause dan macet.
        Time.timeScale = 1f; 
        GameIsPaused = false;
        
        // Memuat ulang scene saat ini
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); 
    }

    public void LoadMainMenu()
    {
        if (AudioManager.instance != null) AudioManager.instance.PlaySFX("Click");
        
        // SANGAT PENTING: Waktu harus dikembalikan ke 1 sebelum pindah scene!
        // Jika tidak, Main Menu akan ikut ter-pause dan macet.
        Time.timeScale = 1f; 
        GameIsPaused = false;
        
        // Memuat scene Main Menu kamu
        SceneManager.LoadScene("MainMenu"); 
    }

    public void QuitGame()
    {
        if (AudioManager.instance != null) AudioManager.instance.PlaySFX("Click");
        
        Debug.Log("Keluar dari Game!");
        Application.Quit();
    }
}