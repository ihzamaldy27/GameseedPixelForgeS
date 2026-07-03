using UnityEngine;

public class MainMenu : MonoBehaviour
{

    public GameObject CreditPanel;

    public void PlayGame()
    {
        Debug.Log("Memulai Game! Memuat gameplay...");
        
        // Memanggil Singleton menggunakan Enum! Tidak perlu lagi mengetik "Indoor1" manual.
        GameSceneManager.instance.LoadScene(GameSceneManager.SceneName.Indoor2);
        
        // ATAU, jika kamu mau mencoba fitur transisi Async:
        // GameSceneManager.instance.LoadSceneAsync(GameSceneManager.SceneName.BlueprintGameplay);
    }

    public void CreditScene()
    {
        Debug.Log("Memuat Credit Scene...");
        

    }

    public void QuitGame()
    {
        Debug.Log("Game Keluar!");
        
        // Perintah ini akan menutup aplikasi saat game sudah di-build
        Application.Quit();
    }
}