using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

public class ConversationHandler : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float responseTimeThreshold = 5000f; // 5 seconds in milliseconds

    [Header("Blob Audio Configuration")]
    [SerializeField] private int blobChannels = 1;           // Backend: Mono (1 channel)
    [SerializeField] private int blobSampleRate = 24000;     // Backend: 24kHz
    [SerializeField] private bool blobIsFloat = false;       // Backend: 16-bit PCM
    [SerializeField] private bool autoDetectFormat = true;   // Try to auto-detect format

    [SerializeField] private Button submitButton;

    private Dictionary<string, float> requestStartTimes = new Dictionary<string, float>();

    // Recording state
    private string currentMicName;
    private AudioClip recordingClip;
    private int recordingFrequency = 44100;

    void Start()
    {
        // Get or create AudioSource component
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

    /// <summary>
    /// Handle incoming audio data from WebSocket
    /// </summary>
    /// <param name="audioData">Raw audio bytes</param>
    public void HandleAudioData(byte[] audioData)
    {

        Debug.Log($"📦 Received audio data, size: {audioData.Length} bytes");

        // Add null/empty check
        if (audioData == null || audioData.Length == 0)
        {
            Debug.LogError("Received empty or null audio data");
            return;
        }

        // Validate data size against expected backend format (48kHz, Mono, 16-bit)
        // ValidateBackendAudioData(audioData);

        // Log first few bytes to debug format - similar to JavaScript typeof check
        if (audioData.Length >= 12)
        {
            try
            {
                string headerInfo = $"Header bytes: {System.Text.Encoding.ASCII.GetString(audioData, 0, Math.Min(4, audioData.Length))}";
                Debug.Log(headerInfo);
            }
            catch
            {
                Debug.Log("Header contains non-ASCII data (likely raw audio blob)");
            }

            // Log raw bytes for debugging - similar to checking instanceof Blob
            string bytesInfo = $"First 8 bytes: ";
            for (int i = 0; i < Math.Min(8, audioData.Length); i++)
            {
                bytesInfo += $"{audioData[i]:X2} ";
            }
            Debug.Log(bytesInfo);
        }

        // Since backend sends raw PCM (not WAV), prefer direct PCM processing
        bool forceRawPCM = !autoDetectFormat; // If auto-detection is off, force raw PCM

        if (forceRawPCM)
        {
            Debug.Log("🎯 Force processing as raw PCM (backend format)");
            StartCoroutine(PlayRawPCMAudio(audioData));
        }
        else
        {
            // Handle audio blob data - equivalent to JavaScript playAudioFromBlob()
            Debug.Log("🔍 Auto-detecting format and processing");
            StartCoroutine(PlayAudioFromBlob(audioData));
        }
    }

    /// <summary>
    /// Validate received audio data against backend specifications
    /// </summary>
    private void ValidateBackendAudioData(byte[] audioData)
    {
        // Backend specs: 24kHz, Mono, 16-bit PCM, 2 bytes per sample
        int expectedBytesPerSample = 1 * 2; // 1 channel × 2 bytes

        if (audioData.Length % expectedBytesPerSample != 0)
        {
            Debug.LogWarning($"⚠️ Audio data size ({audioData.Length} bytes) is not aligned to expected sample size ({expectedBytesPerSample} bytes per sample)");
        }

        int sampleCount = audioData.Length / expectedBytesPerSample;
        float durationSeconds = (float)sampleCount / 24000f; // 24kHz sample rate

        Debug.Log($"🎵 Audio validation - Samples: {sampleCount}, Duration: {durationSeconds:F2}s, Format: 24kHz Mono 16-bit PCM");

        // Warn if duration seems unusual
        if (durationSeconds > 30f)
        {
            Debug.LogWarning($"⚠️ Unusually long audio: {durationSeconds:F1}s - verify this is correct");
        }
        else if (durationSeconds < 0.1f)
        {
            Debug.LogWarning($"⚠️ Very short audio: {durationSeconds:F3}s - might be incomplete");
        }
    }

    /// <summary>
    /// Handle incoming text messages from WebSocket
    /// </summary>
    /// <param name="message">Text message</param>
    public void HandleTextMessage(string message)
    {
        Debug.Log($"Received text message: {message}");

        // You can parse JSON here if needed
        // For example: MCQ responses, status updates, etc.
    }

    /// <summary>
    /// Handle WebSocket connection events
    /// </summary>
    /// <param name="connectionName">Name of the connected WebSocket</param>
    public void HandleWebSocketConnected(string connectionName)
    {
        Debug.Log($"🟢 {connectionName} WebSocket connected successfully");

        // Enable UI buttons or perform other connection-related tasks
        EnableUIForConnection(connectionName);
    }


    public void HandleWebSocketDisconnected(string connectionName)
    {
        Debug.Log($"🔴 {connectionName} WebSocket disconnected");

        // Disable UI buttons or perform cleanup
        DisableUIForConnection(connectionName);
    }


    public void HandleWebSocketError(string errorMessage)
    {
        Debug.LogError($"❌ WebSocket Error: {errorMessage}");

        // Show error UI or attempt reconnection
        ShowErrorNotification(errorMessage);
    }


    private IEnumerator PlayAudioFromBytes(byte[] audioBytes)
    {
        // This method is kept for backward compatibility
        // New blob handling uses PlayAudioFromBlob()
        return PlayAudioFromBlob(audioBytes);
    }

    /// <summary>
    /// Create AudioClip from byte array (supports both WAV format and raw blob data)
    /// </summary>
    /// <param name="audioBytes">Raw audio bytes</param>
    /// <returns>AudioClip or null if failed</returns>
    private AudioClip CreateAudioClipFromBytes(byte[] audioBytes, int sampleRate)
    {
        try
        {
            Debug.Log($"🔍 Analyzing audio data: {audioBytes.Length} bytes");

            // Log more header info for debugging
            if (audioBytes.Length >= 12)
            {
                string headerHex = "";
                for (int i = 0; i < Math.Min(12, audioBytes.Length); i++)
                {
                    headerHex += $"{audioBytes[i]:X2} ";
                }
                Debug.Log($"📊 Header bytes (hex): {headerHex}");

                try
                {
                    string possibleText = System.Text.Encoding.ASCII.GetString(audioBytes, 0, Math.Min(12, audioBytes.Length));
                    Debug.Log($"📊 Header as text: '{possibleText}'");
                }
                catch { /* Ignore non-ASCII data */ }
            }

            // First, try to detect if this is a WAV file
            if (audioBytes.Length >= 44 && IsWavFormat(audioBytes))
            {
                Debug.Log("✅ Detected valid WAV format, using WAV parser");
                return CreateAudioClipFromWav(audioBytes);
            }
            else
            {
                Debug.Log("✅ No valid WAV header detected, treating as raw blob audio data (backend format)");
                return CreateAudioClipFromBlob(audioBytes);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error creating AudioClip: {ex.Message}\nStack trace: {ex.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// Check if the byte array is a WAV file
    /// </summary>
    /// <param name="audioBytes">Audio data bytes</param>
    /// <returns>True if WAV format detected</returns>
    private bool IsWavFormat(byte[] audioBytes)
    {
        if (audioBytes.Length < 12) return false;

        try
        {
            string riffHeader = System.Text.Encoding.ASCII.GetString(audioBytes, 0, 4);
            string waveHeader = System.Text.Encoding.ASCII.GetString(audioBytes, 8, 4);

            bool isWav = riffHeader == "RIFF" && waveHeader == "WAVE";

            // Additional validation: check if we can find valid fmt and data chunks
            if (isWav)
            {
                bool hasFmtChunk = FindChunk(audioBytes, "fmt ") != -1;
                bool hasDataChunk = FindChunk(audioBytes, "data") != -1;

                if (!hasFmtChunk || !hasDataChunk)
                {
                    Debug.LogWarning("⚠️ RIFF/WAVE headers found but missing required chunks - treating as raw blob");
                    return false;
                }

                // Validate data chunk size
                int dataChunkPos = FindChunk(audioBytes, "data");
                if (dataChunkPos != -1 && dataChunkPos + 4 < audioBytes.Length)
                {
                    int dataSize = BitConverter.ToInt32(audioBytes, dataChunkPos + 4);
                    if (dataSize < 0 || dataSize > audioBytes.Length)
                    {
                        Debug.LogWarning($"⚠️ Invalid WAV data chunk size: {dataSize} - treating as raw blob");
                        return false;
                    }
                }
            }

            Debug.Log($"🔍 WAV format check: {isWav} (RIFF: {riffHeader}, WAVE: {waveHeader})");
            return isWav;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"⚠️ WAV format check failed: {ex.Message} - treating as raw blob");
            return false;
        }
    }

    /// <summary>
    /// Create AudioClip from WAV format bytes
    /// </summary>
    /// <param name="audioBytes">WAV format bytes</param>
    /// <returns>AudioClip or null if failed</returns>
    private AudioClip CreateAudioClipFromWav(byte[] audioBytes)
    {
        // Find the fmt chunk
        int fmtChunkPos = FindChunk(audioBytes, "fmt ");
        if (fmtChunkPos == -1)
        {
            Debug.LogError("fmt chunk not found in WAV file");
            return null;
        }

        // Extract audio parameters from fmt chunk
        int audioFormat = BitConverter.ToInt16(audioBytes, fmtChunkPos + 8);
        int channels = BitConverter.ToInt16(audioBytes, fmtChunkPos + 10);
        int sampleRate = BitConverter.ToInt32(audioBytes, fmtChunkPos + 12);
        int bitsPerSample = BitConverter.ToInt16(audioBytes, fmtChunkPos + 22);

        // Validate extracted values
        if (channels <= 0 || channels > 8)
        {
            Debug.LogError($"Invalid channel count: {channels}. Expected 1-8.");
            return null;
        }

        if (sampleRate <= 0 || sampleRate > 192000)
        {
            Debug.LogError($"Invalid sample rate: {sampleRate}. Expected positive value up to 192kHz.");
            return null;
        }

        if (audioFormat != 1) // PCM
        {
            Debug.LogError($"Unsupported audio format: {audioFormat}. Only PCM (1) is supported.");
            return null;
        }

        if (bitsPerSample != 16)
        {
            Debug.LogError($"Unsupported bits per sample: {bitsPerSample}. Only 16-bit is supported.");
            return null;
        }

        // Find the data chunk
        int dataChunkPos = FindChunk(audioBytes, "data");
        if (dataChunkPos == -1)
        {
            Debug.LogError("data chunk not found in WAV file");
            return null;
        }

        int dataSize = BitConverter.ToInt32(audioBytes, dataChunkPos + 4);
        int dataStartPos = dataChunkPos + 8;

        // Validate data chunk
        if (dataStartPos + dataSize > audioBytes.Length)
        {
            Debug.LogError($"Data chunk size ({dataSize}) exceeds available data ({audioBytes.Length - dataStartPos})");
            return null;
        }

        if (dataSize <= 0)
        {
            Debug.LogError($"Invalid data size: {dataSize}");
            return null;
        }

        int bytesPerSample = channels * (bitsPerSample / 8);
        int sampleCount = dataSize / bytesPerSample;

        if (sampleCount <= 0)
        {
            Debug.LogError($"Invalid sample count: {sampleCount}");
            return null;
        }

        Debug.Log($"Creating AudioClip from WAV - Channels: {channels}, Sample Rate: {sampleRate}, Sample Count: {sampleCount}, Data Size: {dataSize}");

        // Create AudioClip
        AudioClip audioClip = AudioClip.Create("WebSocketAudio", sampleCount, channels, sampleRate, false);

        // Convert bytes to float array
        float[] audioData = new float[sampleCount * channels];
        for (int i = 0; i < sampleCount * channels; i++)
        {
            int byteIndex = dataStartPos + i * 2;
            if (byteIndex + 1 < audioBytes.Length)
            {
                short sample = BitConverter.ToInt16(audioBytes, byteIndex);
                audioData[i] = sample / 32768.0f; // Convert to float [-1, 1]
            }
        }

        // Set audio data
        audioClip.SetData(audioData, 0);

        return audioClip;
    }

    /// <summary>
    /// Create AudioClip from raw blob audio data (enhanced for backend compatibility)
    /// </summary>
    /// <param name="audioBytes">Raw audio bytes</param>
    /// <returns>AudioClip or null if failed</returns>
    private AudioClip CreateAudioClipFromBlob(byte[] audioBytes)
    {
        Debug.Log($"🔧 Creating AudioClip from blob data - Size: {audioBytes.Length} bytes");

        // Use configured parameters (should be set to backend specs: 48kHz, Mono, 16-bit)
        int channels = blobChannels;
        int sampleRate = blobSampleRate;
        bool isFloat = blobIsFloat;

        Debug.Log($"🎯 Using configuration: {channels}ch, {sampleRate}Hz, {(isFloat ? "Float32" : "16-bit PCM")}");

        // For backend data, skip auto-detection unless explicitly enabled
        if (autoDetectFormat)
        {
            Debug.Log("🔍 Auto-detecting blob audio format...");

            // Check if it might be a compressed audio format (MP3, OGG, etc.)
            if (audioBytes.Length > 4)
            {
                // Check for common audio file signatures
                if (CheckForMP3Header(audioBytes))
                {
                    Debug.LogWarning("⚠️ MP3 format detected - Unity cannot directly play compressed audio from bytes. Consider using WAV or raw PCM.");
                    return null;
                }

                if (CheckForOGGHeader(audioBytes))
                {
                    Debug.LogWarning("⚠️ OGG format detected - Unity cannot directly play compressed audio from bytes. Consider using WAV or raw PCM.");
                    return null;
                }
            }

            // Try to detect if it's float32 data (common in web audio)
            if (audioBytes.Length >= 4 && audioBytes.Length % 4 == 0)
            {
                // Sample a few float values to see if they're in reasonable audio range
                int sampleCount = Math.Min(10, audioBytes.Length / 4);
                bool looksLikeFloat = true;

                for (int i = 0; i < sampleCount; i++)
                {
                    float testValue = BitConverter.ToSingle(audioBytes, i * 4);
                    if (float.IsNaN(testValue) || float.IsInfinity(testValue) || Math.Abs(testValue) > 2.0f)
                    {
                        looksLikeFloat = false;
                        break;
                    }
                }

                if (looksLikeFloat)
                {
                    isFloat = true;
                    Debug.Log("✅ Auto-detected: Float32 audio data");
                }
            }

            // Auto-detect channel count based on data size patterns (but backend should be mono)
            if (audioBytes.Length % 8 == 0 && blobChannels == 1)
            {
                // Don't auto-detect stereo for backend data since we know it's mono
                Debug.Log("🎯 Backend is specified as mono - keeping mono configuration");
            }
        }

        try
        {
            if (isFloat)
            {
                Debug.Log("🔄 Processing as Float32 blob");
                return CreateAudioClipFromFloatBlob(audioBytes, channels, sampleRate);
            }
            else
            {
                Debug.Log("🔄 Processing as 16-bit PCM blob (backend format)");
                return CreateAudioClipFromPCMBlob(audioBytes, channels, sampleRate);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error creating AudioClip from blob: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Check if the data contains MP3 header
    /// </summary>
    private bool CheckForMP3Header(byte[] audioBytes)
    {
        if (audioBytes.Length < 3) return false;

        // Check for MP3 sync word (0xFFE0 or 0xFFF0)
        return (audioBytes[0] == 0xFF && (audioBytes[1] & 0xE0) == 0xE0) ||
               // Check for ID3 tag
               (audioBytes[0] == 0x49 && audioBytes[1] == 0x44 && audioBytes[2] == 0x33);
    }

    /// <summary>
    /// Check if the data contains OGG header
    /// </summary>
    private bool CheckForOGGHeader(byte[] audioBytes)
    {
        if (audioBytes.Length < 4) return false;

        // Check for OGG signature "OggS"
        return audioBytes[0] == 0x4F && audioBytes[1] == 0x67 &&
               audioBytes[2] == 0x67 && audioBytes[3] == 0x53;
    }

    /// <summary>
    /// Create AudioClip from PCM blob data (16-bit)
    /// </summary>
    private AudioClip CreateAudioClipFromPCMBlob(byte[] audioBytes, int channels, int sampleRate)
    {
        // Backend sends: 24kHz, Mono, 16-bit PCM, 2 bytes per sample
        int bytesPerSample = channels * 2; // 16-bit = 2 bytes per sample
        int sampleCount = audioBytes.Length / bytesPerSample;

        if (sampleCount <= 0)
        {
            Debug.LogError($"Invalid sample count for PCM blob: {sampleCount}. Data size: {audioBytes.Length}, Expected format: {channels}ch, {sampleRate}Hz, 16-bit");
            return null;
        }

        // Validate expected data size for backend format
        int expectedDataSize = sampleCount * channels * 2;
        if (audioBytes.Length != expectedDataSize)
        {
            Debug.LogWarning($"⚠️ Data size mismatch: Got {audioBytes.Length} bytes, expected {expectedDataSize} bytes for {sampleCount} samples");
        }

        Debug.Log($"📊 Creating PCM AudioClip - Channels: {channels}, Sample Rate: {sampleRate}, Samples: {sampleCount}, Duration: {(float)sampleCount / sampleRate:F2}s");

        AudioClip audioClip = AudioClip.Create("BackendPCMAudio", sampleCount, channels, sampleRate, false);

        // Convert 16-bit PCM to Unity's float format
        float[] audioData = new float[sampleCount * channels];
        for (int i = 0; i < sampleCount * channels; i++)
        {
            int byteIndex = i * 2;
            if (byteIndex + 1 < audioBytes.Length)
            {
                // Little-endian 16-bit signed integer
                short sample = BitConverter.ToInt16(audioBytes, byteIndex);
                audioData[i] = sample / 32768.0f; // Convert to [-1, 1] range
            }
        }

        audioClip.SetData(audioData, 0);
        return audioClip;
    }

    /// <summary>
    /// Create AudioClip from float blob data
    /// </summary>
    /// <param name="audioBytes">Raw float audio bytes</param>
    /// <param name="channels">Number of channels</param>
    /// <param name="sampleRate">Sample rate</param>
    /// <returns>AudioClip or null if failed</returns>
    private AudioClip CreateAudioClipFromFloatBlob(byte[] audioBytes, int channels, int sampleRate)
    {
        int floatCount = audioBytes.Length / 4; // 4 bytes per float
        int sampleCount = floatCount / channels;

        if (sampleCount <= 0)
        {
            Debug.LogError($"Invalid sample count for float blob: {sampleCount}");
            return null;
        }

        Debug.Log($"Creating AudioClip from float blob - Channels: {channels}, Sample Rate: {sampleRate}, Sample Count: {sampleCount}");

        // Create AudioClip
        AudioClip audioClip = AudioClip.Create("FloatBlobAudio", sampleCount, channels, sampleRate, false);

        // Convert bytes directly to float array
        float[] audioData = new float[floatCount];
        for (int i = 0; i < floatCount; i++)
        {
            int byteIndex = i * 4;
            if (byteIndex + 3 < audioBytes.Length)
            {
                audioData[i] = BitConverter.ToSingle(audioBytes, byteIndex);
            }
        }

        // Set audio data
        audioClip.SetData(audioData, 0);

        return audioClip;
    }

    /// <summary>
    /// Find a specific chunk in WAV file
    /// </summary>
    /// <param name="audioBytes">WAV file bytes</param>
    /// <param name="chunkId">Chunk identifier (e.g., "fmt ", "data")</param>
    /// <returns>Position of chunk or -1 if not found</returns>
    private int FindChunk(byte[] audioBytes, string chunkId)
    {
        byte[] chunkIdBytes = System.Text.Encoding.ASCII.GetBytes(chunkId);

        for (int i = 12; i <= audioBytes.Length - 8; i++) // Start after RIFF header
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

            if (found)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Calculate and log response time
    /// </summary>
    private void CalculateResponseTime()
    {
        // Simple response time calculation
        // In a real implementation, you'd track request start times per connection
        float currentTime = Time.time * 1000f; // Convert to milliseconds

        // This is a placeholder - you'd need to implement proper request tracking
        Debug.Log($"🕒 Response received at: {currentTime}ms");
    }

    /// <summary>
    /// Enable UI elements for specific connection
    /// </summary>
    /// <param name="connectionName">Connection name</param>
    private void EnableUIForConnection(string connectionName)
    {
        // Find and enable relevant UI buttons
        switch (connectionName)
        {
            case "Audio Chat":
                EnableButton("recordBtn");
                break;
            case "MCQ":
                EnableButton("sendBtn");
                break;
            case "Stuff Nurse":
                EnableButton("recordAgent2Btn");
                break;
        }
    }

    /// <summary>
    /// Disable UI elements for specific connection
    /// </summary>
    /// <param name="connectionName">Connection name</param>
    private void DisableUIForConnection(string connectionName)
    {
        // Find and disable relevant UI buttons
        switch (connectionName)
        {
            case "Audio Chat":
                DisableButton("recordBtn");
                break;
            case "MCQ":
                DisableButton("sendBtn");
                break;
            case "Stuff Nurse":
                DisableButton("recordAgent2Btn");
                break;
        }
    }

    /// <summary>
    /// Enable a UI button by name
    /// </summary>
    /// <param name="buttonName">Button name</param>
    private void EnableButton(string buttonName)
    {
        // This would need to be implemented based on your UI system
        // For example, if using Unity UI:
        // GameObject.Find(buttonName)?.GetComponent<Button>()?.SetInteractable(true);
        Debug.Log($"Enabling button: {buttonName}");
    }

    /// <summary>
    /// Disable a UI button by name
    /// </summary>
    /// <param name="buttonName">Button name</param>
    private void DisableButton(string buttonName)
    {
        // This would need to be implemented based on your UI system
        // For example, if using Unity UI:
        // GameObject.Find(buttonName)?.GetComponent<Button>()?.SetInteractable(false);
        Debug.Log($"Disabling button: {buttonName}");
    }

    /// <summary>
    /// Show error notification to user
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    private void ShowErrorNotification(string errorMessage)
    {
        // Implement error notification UI
        Debug.LogError($"Showing error notification: {errorMessage}");

        // You could show a popup, notification, or update status text
    }

    /// <summary>
    /// Mark the start time for a request (for response time calculation)
    /// </summary>
    /// <param name="connectionName">Connection name</param>
    public void MarkRequestStart(string connectionName)
    {
        requestStartTimes[connectionName] = Time.time * 1000f;
        Debug.Log($"🚀 Request sent to {connectionName} at: {requestStartTimes[connectionName]}ms");
    }

    public void StartRecording()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone detected!");
            return;
        }
        currentMicName = Microphone.devices[0];
        recordingFrequency = 44100;
        recordingClip = Microphone.Start(currentMicName, false, 300, recordingFrequency); // 300s max, will be trimmed
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

        // Create a new AudioClip with the actual recorded length
        float[] samples = new float[samplePosition * recordingClip.channels];
        recordingClip.GetData(samples, 0);

        AudioClip trimmedClip = AudioClip.Create("RecordedClip", samplePosition, recordingClip.channels, recordingFrequency, false);
        trimmedClip.SetData(samples, 0);

        return trimmedClip;
    }

    /// <summary>
    /// Configure blob audio parameters at runtime
    /// </summary>
    /// <param name="channels">Number of channels (1=mono, 2=stereo)</param>
    /// <param name="sampleRate">Sample rate in Hz</param>
    /// <param name="isFloat">True if blob contains 32-bit float data, false for 16-bit PCM</param>
    public void ConfigureBlobAudio(int channels, int sampleRate, bool isFloat = false)
    {
        blobChannels = Mathf.Clamp(channels, 1, 8);
        blobSampleRate = Mathf.Clamp(sampleRate, 8000, 192000);
        blobIsFloat = isFloat;

        Debug.Log($"Blob audio configured - Channels: {blobChannels}, Sample Rate: {blobSampleRate}, Float: {blobIsFloat}");
    }

    /// <summary>
    /// Configure for web audio blob format (convenience method)
    /// </summary>
    public void ConfigureForWebAudioBlob()
    {
        // Common web audio blob settings
        blobChannels = 1;           // Usually mono for speech
        blobSampleRate = 44100;     // Standard web audio sample rate
        blobIsFloat = true;         // Web Audio API typically uses Float32
        autoDetectFormat = true;    // Enable smart detection

        Debug.Log("🌐 Configured for web audio blob format (Float32, 44.1kHz, Mono with auto-detection)");
    }

    /// <summary>
    /// Configure for backend audio format
    /// </summary>
    /// <param name="channels">Number of channels</param>
    /// <param name="sampleRate">Sample rate</param>
    /// <param name="isFloat">True for Float32, false for 16-bit PCM</param>
    /// <param name="enableAutoDetect">Enable format auto-detection</param>
    public void ConfigureForBackendAudio(int channels, int sampleRate, bool isFloat, bool enableAutoDetect = true)
    {
        blobChannels = Mathf.Clamp(channels, 1, 8);
        blobSampleRate = Mathf.Clamp(sampleRate, 8000, 192000);
        blobIsFloat = isFloat;
        autoDetectFormat = enableAutoDetect;

        string format = isFloat ? "Float32" : "16-bit PCM";
        string channelType = channels == 1 ? "Mono" : channels == 2 ? "Stereo" : $"{channels}-channel";

        Debug.Log($"🔧 Configured for backend audio: {format}, {sampleRate}Hz, {channelType}" +
                 (enableAutoDetect ? " (with auto-detection)" : ""));
    }

    /// <summary>
    /// Configure for backend's exact audio format
    /// </summary>
    public void ConfigureForBackendExact()
    {
        // Backend specifications:
        // Sample Rate: 24000 Hz
        // Channels: 1 (Mono)
        // Bit Depth: 16 bits
        // Sample Width: 2 bytes
        blobChannels = 1;           // Mono
        blobSampleRate = 24000;     // 24kHz
        blobIsFloat = false;        // 16-bit PCM
        autoDetectFormat = false;   // Use exact settings, no detection

        Debug.Log("🎯 Configured for backend exact format: 16-bit PCM, 24kHz, Mono (2 bytes per sample)");
    }

    /// <summary>
    /// Force processing as raw PCM blob (bypass WAV detection)
    /// </summary>
    /// <param name="audioBytes">Raw PCM audio bytes</param>
    /// <returns>AudioClip or null if failed</returns>
    public AudioClip CreateAudioClipFromRawPCM(byte[] audioBytes, int sampleRate)
    {
        Debug.Log($"🎯 Force processing as raw PCM: {audioBytes.Length} bytes");

        // Use exact backend specifications
        return CreateAudioClipFromPCMBlob(audioBytes, blobChannels, sampleRate);
    }

    /// <summary>
    /// Play audio from blob data - Unity equivalent of JavaScript playAudioFromBlob()
    /// </summary>
    /// <param name="blobData">Raw blob audio data</param>
    /// <returns></returns>
    public IEnumerator PlayAudioFromBlob(byte[] blobData, int sampleRate = 48000)
    {
        Debug.Log($"🎵 Processing audio blob, size: {blobData.Length} bytes");

        AudioClip audioClip = null;
        bool playbackStarted = false;
        bool hasError = false;

        // Create AudioClip from blob data (equivalent to URL.createObjectURL(audioBlob))
        audioClip = CreateAudioClipFromBytes(blobData, sampleRate);

        if (audioClip != null)
        {
            // Ensure AudioSource is ready
            if (audioSource == null)
            {
                Debug.LogError("AudioSource is null! Cannot play audio.");
                hasError = true;
            }
            else
            {
                // Stop any currently playing audio
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                }

                // Set the clip and play (equivalent to audio.play())
                audioSource.clip = audioClip;
                audioSource.volume = 1.0f;
                audioSource.pitch = 1.0f;

                try
                {
                    audioSource.Play();
                    playbackStarted = true;
                    submitButton.GetComponentInChildren<TextMeshProUGUI>().text = "Playing";
                    StartCoroutine(WaitForAudioToFinish());

                    Debug.Log($"🔊 Audio playback started - Length: {audioClip.length}s, Channels: {audioClip.channels}, Frequency: {audioClip.frequency}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Error starting audio playback: {ex.Message}");
                    hasError = true;
                    submitButton.interactable = true;
                }

                // Verify playback started (equivalent to .then() in JavaScript)
                yield return new WaitForEndOfFrame();

                if (playbackStarted && !audioSource.isPlaying)
                {
                    Debug.LogError("AudioSource failed to start playing!");
                    playbackStarted = false;
                }
            }
        }
        else
        {
            Debug.LogError("❌ Failed to create AudioClip from blob data");
            hasError = true;
        }

        // Only continue if no errors and playback started
        if (!hasError && audioClip != null && playbackStarted)
        {
            // Wait for audio to finish playing (equivalent to 'ended' event listener)
            float startTime = Time.time;
            while (audioSource.isPlaying && Time.time - startTime < audioClip.length + 1.0f)
            {
                yield return new WaitForSeconds(0.1f);
            }

            Debug.Log("🔇 Audio playback finished");
        }

        // Clean up (equivalent to URL.revokeObjectURL())
        if (audioClip != null)
        {
            try
            {
                if (audioSource != null && audioSource.clip == audioClip)
                {
                    audioSource.clip = null;
                }
                DestroyImmediate(audioClip);
                Debug.Log("🗑️ Audio clip cleaned up");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during audio cleanup: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Play raw PCM audio directly (for backend data)
    /// </summary>
    /// <param name="pcmData">Raw PCM audio data</param>
    /// <returns></returns>
    public IEnumerator PlayRawPCMAudio(byte[] pcmData, int sampleRate = 48000)
    {
        Debug.Log($"🎵 Processing raw PCM audio, size: {pcmData.Length} bytes");


        AudioClip audioClip = null;
        bool playbackStarted = false;
        bool hasError = false;

        // Create AudioClip directly from raw PCM data
        audioClip = CreateAudioClipFromRawPCM(pcmData, sampleRate);

        if (audioClip != null)
        {
            // Ensure AudioSource is ready
            if (audioSource == null)
            {
                Debug.LogError("AudioSource is null! Cannot play audio.");
                hasError = true;
            }
            else
            {
                // Stop any currently playing audio
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                }

                // Set the clip and play
                audioSource.clip = audioClip;
                audioSource.volume = 1.0f;
                audioSource.pitch = 1.0f;

                try
                {
                    audioSource.Play();
                    playbackStarted = true;
                    submitButton.GetComponentInChildren<TextMeshProUGUI>().text = "Playing";
                    StartCoroutine(WaitForAudioToFinish());

                    Debug.Log($"🔊 Raw PCM audio playback started - Length: {audioClip.length}s, Channels: {audioClip.channels}, Frequency: {audioClip.frequency}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Error starting audio playback: {ex.Message}");
                    hasError = true;
                    submitButton.interactable = true;
                }

                // Verify playback started
                yield return new WaitForEndOfFrame();

                if (playbackStarted && !audioSource.isPlaying)
                {
                    Debug.LogError("AudioSource failed to start playing!");
                    playbackStarted = false;
                }
            }
        }
        else
        {
            Debug.LogError("❌ Failed to create AudioClip from raw PCM data");
            hasError = true;
        }

        // Only continue if no errors and playback started
        if (!hasError && audioClip != null && playbackStarted)
        {
            // Wait for audio to finish playing
            float startTime = Time.time;
            while (audioSource.isPlaying && Time.time - startTime < audioClip.length + 1.0f)
            {
                yield return new WaitForSeconds(0.1f);
            }

            Debug.Log("🔇 Raw PCM audio playback finished");
        }

        // Clean up
        if (audioClip != null)
        {
            try
            {
                if (audioSource != null && audioSource.clip == audioClip)
                {
                    audioSource.clip = null;
                }
                DestroyImmediate(audioClip);
                Debug.Log("🗑️ Raw PCM audio clip cleaned up");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during audio cleanup: {ex.Message}");
            }
        }
    }

    private IEnumerator WaitForAudioToFinish()
    {
        yield return new WaitWhile(() => audioSource.isPlaying);

        // Change the button text after playback ends
        if (submitButton != null)
        {
            submitButton.GetComponentInChildren<TextMeshProUGUI>().text = "Submit";
            submitButton.interactable = true;
            Debug.Log("🔇 Audio playback finished. Button text updated to 'Submit'.");
        }
    }

}
