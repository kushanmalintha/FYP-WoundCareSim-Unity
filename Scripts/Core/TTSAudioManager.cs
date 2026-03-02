using System;
using UnityEngine;

public class TTSAudioManager : MonoBehaviour
{
    public static TTSAudioManager Instance { get; private set; }

    public AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlayTTS(string base64Audio)
    {
        if (string.IsNullOrEmpty(base64Audio))
        {
            return;
        }

        if (audioSource == null)
        {
            Debug.LogError("[TTSAudioManager] AudioSource is not assigned!");
            return;
        }

        try
        {
            byte[] audioBytes = Convert.FromBase64String(base64Audio);
            AudioClip clip = WavUtility.ToAudioClip(audioBytes, "TTSAudio");

            if (clip != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[TTSAudioManager] Failed to play TTS audio: {e.Message}");
        }
    }
}
