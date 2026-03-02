using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AudioUIController — networking reset stub.
/// Recording still works locally; audio is no longer sent to backend WebSocket.
/// </summary>
public class AudioUIController : MonoBehaviour
{
    [SerializeField] private ConversationHandler conversationHandler;

    public void OnRecordButtonDown()
    {
        conversationHandler.StartRecording();
    }

    public void OnRecordButtonUp(string webSocket)
    {
        AudioClip recordedClip = conversationHandler.StopRecordingAndGetClip();
        if (recordedClip != null)
        {
            byte[] wavBytes = AudioClipConverter.AudioClipToWavBytes(recordedClip);
            if (wavBytes != null)
            {
                Debug.Log("Backend call removed - audio recorded but not sent (socket: " + webSocket + ", bytes: " + wavBytes.Length + ")");
            }
        }
        else
        {
            Debug.LogWarning("No audio was recorded.");
        }
    }
}