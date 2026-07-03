using UnityEngine;

public class CutSceneMove : MonoBehaviour
{
    public void MainMenuScene()
    {
        Debug.Log("Memuat Main Menu Scene...");
        GameSceneManager.instance.LoadScene(GameSceneManager.SceneName.MainMenu);
    }
}
