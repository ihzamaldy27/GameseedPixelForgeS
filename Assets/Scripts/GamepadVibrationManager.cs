using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class GamepadVibrationManager : MonoBehaviour
{
    // --- SINGLETON ---
    public static GamepadVibrationManager instance;

    [Header("Global Settings")]
    [Tooltip("Matikan ini jika ingin mematikan fitur getar di seluruh game")]
    public bool enableVibration = true;

    private void Awake()
    {
        // Setup Singleton agar tidak hancur saat pindah scene
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Memanggil getaran pada gamepad.
    /// lowFrequency = Getaran kasar/berat (kiri).
    /// highFrequency = Getaran halus/tajam (kanan).
    /// </summary>
    public void TriggerRumble(float lowFrequency, float highFrequency, float duration)
    {
        if (enableVibration && Gamepad.current != null)
        {
            // Hentikan timer getaran sebelumnya agar tidak bertabrakan
            StopAllCoroutines(); 
            
            // Nyalakan motor
            Gamepad.current.SetMotorSpeeds(lowFrequency, highFrequency);
            
            // Mulai hitung mundur untuk mematikan
            StartCoroutine(StopRumbleRoutine(duration));
        }
    }

    private IEnumerator StopRumbleRoutine(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        StopRumble();
    }

    // Fungsi manual untuk mematikan getaran seketika
    public void StopRumble()
    {
        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0f, 0f);
        }
    }

    // Jaring pengaman: Pastikan stik mati bergetar jika game/scene ditutup
    private void OnDisable()
    {
        StopRumble();
    }
}