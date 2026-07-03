using UnityEngine;

public class BGMInitiator : MonoBehaviour
{
    [SerializeField] private string bGMName;
    
    void Start()
    {
        AudioManager.instance.PlayBGM(bGMName);
    }
}
