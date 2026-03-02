using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using System.IO;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ConversationHandler — networking reset.
/// All WebSocket event handlers and backend configuration methods have been removed.
/// Microphone recording and AudioClip playback utilities are fully preserved.
/// </summary>
public class ConversationHandler : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;

    [Header("PCM Playback Configuration")]
    [SerializeField] private int blobChannels = 1;       // Mono
    [SerializeField] private int blobSampleRate = 24000; // 24kHz

    [SerializeField] private Button submitButton;

    // Recording state
    private string currentMicName;
    private AudioClip recordingClip;
    private int recordingFrequency = 44100;

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    void Update()
    {
    }

    // -------------------------------------------------------------------------
    // WebSocket event handler stubs (backend removed — placeholders only)
    // -------------------------------------------------------------------------

    public void HandleAudioData(byte[] audioData)
    {
        Debug.Log("Backend call removed - placeholder (ConversationHandler.HandleAudioData)");
    }

    public void HandleTextMessage(string message)
    {
        Debug.Log("Backend call removed - placeholder (ConversationHandler.HandleTextMessage)");
    }

    public void HandleWebSocketConnected(string connectionName)
    {
        Debug.Log("Backend call removed - placeholder (ConversationHandler.HandleWebSocketConnected: " + connectionName + ")");
    }

    public void HandleWebSocketDisconnected(string connectionName)
    {
        Debug.Log("Backend call removed - placeholder (ConversationHandler.HandleWebSocketDisconnected: " + connectionName + ")");
    }

    public void HandleWebSocketError(string errorMessage)
    {
        Debug.Log("Backend call removed - placeholder (ConversationHandler.HandleWebSocketError: " + errorMessage + ")");
    }

    public void MarkRequestStart(string connectionName)
    {
        Debug.Log("Backend call removed - placeholder (ConversationHandler.MarkRequestStart: " + connectionName + ")");
    }

    public void ConfigureForBackendExact()
    {
        Debug.Log("Backend call removed - placeholder (ConversationHandler.ConfigureForBackendExact)");
    }

    // -------------------------------------------------------------------------
    // Microphone recording
    // -------------------------------------------------------------------------

    public void StartRecording()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone detected!");
            return;
        }
        currentMicName = Microphone.devices[0];
        recordingFrequency = 44100;
        recordingClip = Microphone.Start(currentMicName, false, 300, recordingFrequency);
        Debug.Log("Recording started...");
    }

    public AudioClip StopRecordingAndGetClip()
    {
        if (currentMicName == null || recordingClip == null)
        {
            Debug.LogWarning("No recording in progress.");
            return null;
        }

        int samplePosition = Microphone.GetPosition(currentMicName);
        Microphone.End(currentMicName);
        Debug.Log("Recording stopped.");

        if (samplePosition <= 0)
        {
            Debug.LogWarning("No audio recorded.");
            return null;
        }

        float[] samples = new float[samplePosition * recordingClip.channels];
        recordingClip.GetData(samples, 0);

        AudioClip trimmedClip = AudioClip.Create("RecordedClip", samplePosition, recordingClip.channels, recordingFrequency, false);
        trimmedClip.SetData(samples, 0);

        return trimmedClip;
    }

    // -------------------------------------------------------------------------
    // AudioClip playback (raw PCM / blob / WAV) — fully preserved
    // -------------------------------------------------------------------------

    public IEnumerator PlayAudioFromBlob(byte[] blobData, int sampleRate = 48000)
    {
        Debug.Log($"🎵 Processing audio blob, size: {blobData.Length} bytes");

        AudioClip audioClip = CreateAudioClipFromBytes(blobData, sampleRate);

        if (audioClip != null)
        {
            yield return StartCoroutine(PlayClip(audioClip));
        }
        else
        {
            Debug.LogError("❌ Failed to create AudioClip from blob data");
        }
    }

    public IEnumerator PlayRawPCMAudio(byte[] pcmData, int sampleRate = 48000)
    {
        Debug.Log($"🎵 Processing raw PCM audio, size: {pcmData.Length} bytes");

        AudioClip audioClip = CreateAudioClipFromRawPCM(pcmData, sampleRate);

        if (audioClip != null)
        {
            yield return StartCoroutine(PlayClip(audioClip));
        }
        else
        {
            Debug.LogError("❌ Failed to create AudioClip from raw PCM data");
        }
    }

    private IEnumerator PlayClip(AudioClip audioClip)
    {
        if (audioSource == null)
        {
            Debug.LogError("AudioSource is null! Cannot play audio.");
            yield break;
        }

        if (audioSource.isPlaying)
            audioSource.Stop();

        audioSource.clip = audioClip;
        audioSource.volume = 1.0f;
        audioSource.pitch = 1.0f;

        try
        {
            audioSource.Play();
            if (submitButton != null)
                submitButton.GetComponentInChildren<TextMeshProUGUI>().text = "Playing";
            StartCoroutine(WaitForAudioToFinish());
            Debug.Log($"🔊 Audio playback started - Length: {audioClip.length}s");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error starting audio playback: {ex.Message}");
            if (submitButton != null)
                submitButton.interactable = true;
            yield break;
        }

        yield return new WaitForEndOfFrame();

        float startTime = Time.time;
        while (audioSource.isPlaying && Time.time - startTime < audioClip.length + 1.0f)
        {
            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log("🔇 Audio playback finished");

        try
        {
            if (audioSource != null && audioSource.clip == audioClip)
                audioSource.clip = null;
            UnityEngine.Object.DestroyImmediate(audioClip);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during audio cleanup: {ex.Message}");
        }
    }

    private IEnumerator WaitForAudioToFinish()
    {
        yield return new WaitWhile(() => audioSource.isPlaying);

        if (submitButton != null)
        {
            submitButton.GetComponentInChildren<TextMeshProUGUI>().text = "Submit";
            submitButton.interactable = true;
            Debug.Log("🔇 Audio playback finished. Button text updated to 'Submit'.");
        }
    }

    // -------------------------------------------------------------------------
    // AudioClip creation utilities (WAV / PCM blob / float blob) — fully preserved
    // -------------------------------------------------------------------------

    public AudioClip CreateAudioClipFromRawPCM(byte[] audioBytes, int sampleRate)
    {
        Debug.Log($"🎯 Force processing as raw PCM: {audioBytes.Length} bytes");
        return CreateAudioClipFromPCMBlob(audioBytes, blobChannels, sampleRate);
    }

    private AudioClip CreateAudioClipFromBytes(byte[] audioBytes, int sampleRate)
    {
        try
        {
            if (audioBytes.Length >= 44 && IsWavFormat(audioBytes))
            {
                Debug.Log("✅ Detected valid WAV format, using WAV parser");
                return CreateAudioClipFromWav(audioBytes);
            }
            else
            {
                Debug.Log("✅ No valid WAV header detected, treating as raw PCM blob");
                return CreateAudioClipFromPCMBlob(audioBytes, blobChannels, sampleRate);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error creating AudioClip: {ex.Message}");
            return null;
        }
    }

    private bool IsWavFormat(byte[] audioBytes)
    {
        if (audioBytes.Length < 12) return false;

        try
        {
            string riffHeader = System.Text.Encoding.ASCII.GetString(audioBytes, 0, 4);
            string waveHeader = System.Text.Encoding.ASCII.GetString(audioBytes, 8, 4);
            bool isWav = riffHeader == "RIFF" && waveHeader == "WAVE";

            if (isWav)
            {
                bool hasFmtChunk = FindChunk(audioBytes, "fmt ") != -1;
                bool hasDataChunk = FindChunk(audioBytes, "data") != -1;
                if (!hasFmtChunk || !hasDataChunk) return false;

                int dataChunkPos = FindChunk(audioBytes, "data");
                if (dataChunkPos != -1 && dataChunkPos + 4 < audioBytes.Length)
                {
                    int dataSize = BitConverter.ToInt32(audioBytes, dataChunkPos + 4);
                    if (dataSize < 0 || dataSize > audioBytes.Length) return false;
                }
            }

            return isWav;
        }
        catch
        {
            return false;
        }
    }

    private AudioClip CreateAudioClipFromWav(byte[] audioBytes)
    {
        int fmtChunkPos = FindChunk(audioBytes, "fmt ");
        if (fmtChunkPos == -1) { Debug.LogError("fmt chunk not found"); return null; }

        int audioFormat   = BitConverter.ToInt16(audioBytes, fmtChunkPos + 8);
        int channels      = BitConverter.ToInt16(audioBytes, fmtChunkPos + 10);
        int sampleRate    = BitConverter.ToInt32(audioBytes, fmtChunkPos + 12);
        int bitsPerSample = BitConverter.ToInt16(audioBytes, fmtChunkPos + 22);

        if (channels <= 0 || channels > 8)     { Debug.LogError($"Invalid channels: {channels}"); return null; }
        if (sampleRate <= 0 || sampleRate > 192000) { Debug.LogError($"Invalid sample rate: {sampleRate}"); return null; }
        if (audioFormat != 1)  { Debug.LogError($"Unsupported format: {audioFormat}"); return null; }
        if (bitsPerSample != 16) { Debug.LogError($"Unsupported bits: {bitsPerSample}"); return null; }

        int dataChunkPos = FindChunk(audioBytes, "data");
        if (dataChunkPos == -1) { Debug.LogError("data chunk not found"); return null; }

        int dataSize     = BitConverter.ToInt32(audioBytes, dataChunkPos + 4);
        int dataStartPos = dataChunkPos + 8;

        if (dataStartPos + dataSize > audioBytes.Length || dataSize <= 0) { Debug.LogError("Invalid data chunk"); return null; }

        int bytesPerSample = channels * (bitsPerSample / 8);
        int sampleCount    = dataSize / bytesPerSample;

        AudioClip audioClip = AudioClip.Create("WebSocketAudio", sampleCount, channels, sampleRate, false);
        float[] audioData   = new float[sampleCount * channels];
        for (int i = 0; i < sampleCount * channels; i++)
        {
            int byteIndex = dataStartPos + i * 2;
            if (byteIndex + 1 < audioBytes.Length)
            {
                short sample = BitConverter.ToInt16(audioBytes, byteIndex);
                audioData[i] = sample / 32768.0f;
            }
        }
        audioClip.SetData(audioData, 0);
        return audioClip;
    }

    private AudioClip CreateAudioClipFromPCMBlob(byte[] audioBytes, int channels, int sampleRate)
    {
        int bytesPerSample = channels * 2;
        int sampleCount    = audioBytes.Length / bytesPerSample;

        if (sampleCount <= 0)
        {
            Debug.LogError($"Invalid sample count for PCM blob: {sampleCount}");
            return null;
        }

        Debug.Log($"📊 Creating PCM AudioClip - Channels: {channels}, Sample Rate: {sampleRate}, Samples: {sampleCount}");

        AudioClip audioClip = AudioClip.Create("BackendPCMAudio", sampleCount, channels, sampleRate, false);
        float[] audioData   = new float[sampleCount * channels];
        for (int i = 0; i < sampleCount * channels; i++)
        {
            int byteIndex = i * 2;
            if (byteIndex + 1 < audioBytes.Length)
            {
                short sample = BitConverter.ToInt16(audioBytes, byteIndex);
                audioData[i] = sample / 32768.0f;
            }
        }
        audioClip.SetData(audioData, 0);
        return audioClip;
    }

    private int FindChunk(byte[] audioBytes, string chunkId)
    {
        byte[] chunkIdBytes = System.Text.Encoding.ASCII.GetBytes(chunkId);

        for (int i = 12; i <= audioBytes.Length - 8; i++)
        {
            bool found = true;
            for (int j = 0; j < chunkIdBytes.Length; j++)
            {
                if (i + j >= audioBytes.Length || audioBytes[i + j] != chunkIdBytes[j])
                {
                    found = false;
                    break;
                }
            }
            if (found) return i;
        }
        return -1;
    }
}
