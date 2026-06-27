using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameSceneManager : MonoBehaviour
{
    // --- SINGLETON ---
    public static GameSceneManager instance;

    // 1. KUMPULAN NAMA SCENE (Bebas Typo!)
    // Pastikan nama di sini sama persis dengan nama file Scene di Unity
    public enum SceneName 
    {
        MainMenu,
        BlueprintGameplay,
        Indoor1,
        SpaceShooterScene
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Jangan hancurkan saat pindah scene
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // --- FUNGSI LOAD SCENE SEDERHANA ---
    public void LoadScene(SceneName sceneToLoad)
    {
        // Enum diubah menjadi string secara otomatis
        SceneManager.LoadScene(sceneToLoad.ToString());
    }

    // --- FUNGSI LOAD SCENE ASYNC (Untuk Loading Screen) ---
    public void LoadSceneAsync(SceneName sceneToLoad)
    {
        StartCoroutine(LoadSceneCoroutine(sceneToLoad.ToString()));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        // TODO: Nanti kamu bisa menyalakan UI Panel "Loading..." di sini

        // Memuat scene di latar belakang
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        
        // Mencegah scene langsung berganti sebelum kita izinkan
        // operation.allowSceneActivation = false; 

        while (!operation.isDone)
        {
            // Menghitung progress dari 0.0 sampai 1.0
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            
            // TODO: Update slider UI loading bar menggunakan nilai 'progress' di sini
            
            yield return null;
        }

        // TODO: Matikan UI Panel "Loading..." di sini
    }
}