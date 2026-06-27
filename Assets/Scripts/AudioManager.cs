using UnityEngine;
using System; 

public class AudioManager : MonoBehaviour
{
    // --- SINGLETON ---
    public static AudioManager instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource; 
    [SerializeField] private AudioSource sfxSource; 

    [Header("Background Music")]
    [Tooltip("Ketik nama BGM yang ingin diputar otomatis saat game dimulai")]
    public string startingBGMName; 
    public Sound[] bgmList; // Daftar BGM yang bisa kamu isi di Inspector

    [Header("Sound Effects List")]
    public Sound[] sfxList; 

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // Menjaga agar AudioManager (dan musiknya) tidak hancur saat pindah Scene
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            if (!string.IsNullOrEmpty(startingBGMName))
            {
                instance.PlayBGM(startingBGMName);
            }
            Destroy(gameObject); 
            return;
        }
    }

    private void Start()
    {
        // Mainkan BGM awal jika kolom namanya diisi di Inspector
        if (!string.IsNullOrEmpty(startingBGMName))
        {
            PlayBGM(startingBGMName);
        }
    }

    // --- FUNGSI UNTUK MEMAINKAN BGM ---
    public void PlayBGM(string bgmName)
    {
        Sound s = Array.Find(bgmList, sound => sound.name == bgmName);
        
        if (s != null)
        {
            // CEK PENTING: Jika lagu yang diminta sudah sedang diputar, jangan di-restart!
            // (Agar musik tidak patah-patah kalau fungsinya terpanggil berulang kali)
            if (bgmSource.clip == s.clip && bgmSource.isPlaying) return;

            bgmSource.clip = s.clip;
            bgmSource.loop = true;
            bgmSource.Play();
        }
        else
        {
            Debug.LogWarning($"BGM dengan nama '{bgmName}' tidak ditemukan! Cek ejaan di Inspector.");
        }
    }

    // --- FUNGSI UNTUK MEMAINKAN SFX ---
    public void PlaySFX(string sfxName)
    {
        Sound s = Array.Find(sfxList, sound => sound.name == sfxName);
        
        if (s != null)
        {
            sfxSource.PlayOneShot(s.clip);
        }
        else
        {
            Debug.LogWarning($"SFX dengan nama '{sfxName}' tidak ditemukan! Cek ejaan di Inspector.");
        }
    }
}

[Serializable]
public class Sound
{
    public string name;      
    public AudioClip clip;   
}