using UnityEngine;
using System;
using System.Collections;
using DG.Tweening;

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

    // Track currently running intro+loop coroutine
    private Coroutine bgmCoroutine;

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
            /*// CEK PENTING: Jika lagu yang diminta sudah sedang diputar, jangan di-restart!
            // (Agar musik tidak patah-patah kalau fungsinya terpanggil berulang kali)
            if (bgmSource.clip == s.clip && bgmSource.isPlaying) return;

            bgmSource.clip = s.clip;
            bgmSource.loop = true;
            bgmSource.Play();*/

            // Stop any ongoing intro-loop coroutine
            if (bgmCoroutine != null)
            {
                StopCoroutine(bgmCoroutine);
                bgmCoroutine = null;
            }

            // Stop current BGM to reset everything
            bgmSource.Stop();

            // --- CASE 1: BGM has an intro and a loop part ---
            if (s.introClip != null && s.loopClip != null)
            {
                bgmCoroutine = StartCoroutine(PlayBGMSequential(s));
            }
            // --- CASE 2: Regular single-clip BGM (original behavior) ---
            else
            {
                // If the same clip is already playing, don't restart it
                if (bgmSource.clip == s.clip && bgmSource.isPlaying) return;

                bgmSource.clip = s.clip;
                bgmSource.loop = true;
                bgmSource.Play();
            }
        }
        else
        {
            Debug.LogWarning($"BGM dengan nama '{bgmName}' tidak ditemukan! Cek ejaan di Inspector.");
        }
    }

    public void PlayBGMAfterDecay(string bgmName, float waitTime)
    {
        Sound s = Array.Find(bgmList, sound => sound.name == bgmName);
        
        if (s != null)
        {
            // CEK PENTING: Jika lagu yang diminta sudah sedang diputar, jangan di-restart!
            // (Agar musik tidak patah-patah kalau fungsinya terpanggil berulang kali)
            if (bgmSource.clip == s.clip && bgmSource.isPlaying) return;

            DOTween.To(() => bgmSource.volume, x => bgmSource.volume = x, 0, waitTime).OnComplete(() =>
            {
                PlayBGM(bgmName);
                bgmSource.volume = 1f;
            });
        }
        else
        {
            Debug.LogWarning($"BGM dengan nama '{bgmName}' tidak ditemukan! Cek ejaan di Inspector.");
        }
    }

    // Coroutine to handle intro → loop transition
    private IEnumerator PlayBGMSequential(Sound s)
    {
        // 1. Play the intro (non-looping)
        bgmSource.clip = s.introClip;
        bgmSource.loop = false;
        bgmSource.Play();

        // 2. Wait for the exact duration of the intro (unscaled, so it stays in sync even if game pauses)
        yield return new WaitForSecondsRealtime(s.introClip.length);

        // 3. Play the loop part (looping forever)
        bgmSource.clip = s.loopClip;
        bgmSource.loop = true;
        bgmSource.Play();

        // 4. Clear the coroutine reference (job done)
        bgmCoroutine = null;
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
    // For SFX or single‑clip BGM (keeping this for backward compatibilit
    public AudioClip clip;  

    // NEW: For BGM that has an intro + loop structure
    public AudioClip introClip;
    public AudioClip loopClip; 
}