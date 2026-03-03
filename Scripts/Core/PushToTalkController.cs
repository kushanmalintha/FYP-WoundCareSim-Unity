using System;
using UnityEngine;
using Newtonsoft.Json.Linq;

public enum VoiceTarget
{
    None,
    Patient,
    Nurse
}

public class PushToTalkController : MonoBehaviour
{
    public static PushToTalkController Instance { get; private set; }

    public VoiceTarget CurrentTarget { get; private set; } = VoiceTarget.None;

    private string _deviceName;
    private AudioClip _recording;
    private bool _isRecording;
    private int _lastSamplePosition;
    private float _timer;
    private const float ChunkDuration = 0.150f; // 150ms
    private const int SampleRate = 16000;

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

    private void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            _deviceName = Microphone.devices[0];
        }
        else
        {
            Debug.LogError("[PushToTalkController] No microphone devices found!");
        }
    }

    private void Update()
    {
        if (string.IsNullOrEmpty(_deviceName)) return;

        // Button One (A) for Patient Push-To-Talk
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            CurrentTarget = VoiceTarget.Patient;
            StartRecording();
        }
        // Button Three (X) for Nurse Push-To-Talk
        else if (OVRInput.GetDown(OVRInput.Button.Three))
        {
            CurrentTarget = VoiceTarget.Nurse;
            StartRecording();
        }
        else if (OVRInput.GetUp(OVRInput.Button.One) || OVRInput.GetUp(OVRInput.Button.Three))
        {
            StopRecording();
        }

        if (_isRecording)
        {
            _timer += Time.deltaTime;
            if (_timer >= ChunkDuration)
            {
                SendChunk();
                _timer = 0;
            }
        }
    }

    private void StartRecording()
    {
        _recording = Microphone.Start(_deviceName, true, 10, SampleRate);
        _lastSamplePosition = 0;
        _isRecording = true;
        _timer = 0;
        Debug.Log("[PushToTalkController] Started Recording");
    }

    private void SendChunk()
    {
        int currentPosition = Microphone.GetPosition(_deviceName);
        if (currentPosition == _lastSamplePosition) return;

        int samplesCount;
        if (currentPosition > _lastSamplePosition)
        {
            samplesCount = currentPosition - _lastSamplePosition;
        }
        else
        {
            // Wrapped around
            samplesCount = (_recording.samples - _lastSamplePosition) + currentPosition;
        }

        float[] samples = new float[samplesCount];
        _recording.GetData(samples, _lastSamplePosition);
        
        byte[] wavBytes = WavUtility.GetWavBytes(samples, SampleRate, _recording.channels);
        string base64 = Convert.ToBase64String(wavBytes);

        BackendConnectionManager.Instance.SendEvent("stt_chunk", JObject.FromObject(new
        {
            audio_chunk = base64,
            content_type = "audio/wav"
        }));

        _lastSamplePosition = currentPosition;
    }

    private void StopRecording()
    {
        if (!_isRecording) return;
        
        _isRecording = false;
        Microphone.End(_deviceName);
        
        // Send final chunk before complete
        SendChunk();

        BackendConnectionManager.Instance.SendEvent("stt_complete", JObject.FromObject(new
        {
            filename = "recording.wav",
            content_type = "audio/wav"
        }));

        Debug.Log("[PushToTalkController] Stopped Recording");
    }

    public void ResetTarget()
    {
        CurrentTarget = VoiceTarget.None;
    }
}
