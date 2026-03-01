using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Threading.Tasks;

public class AudioUIController : MonoBehaviour
{
    [SerializeField] private ConversationHandler conversationHandler;
    [SerializeField] private WebSocketHandler webSocketHandler;

    public void OnRecordButtonDown()
    {
        conversationHandler.StartRecording();
    }

    public async void OnRecordButtonUp(string webSocket)
    {

        AudioClip recordedClip = conversationHandler.StopRecordingAndGetClip();
        if (recordedClip != null)
        {

            // Convert to WAV bytes and send
            byte[] wavBytes = AudioClipConverter.AudioClipToWavBytes(recordedClip);
            if (wavBytes != null)
            {
                // Assuming you have a reference to WebSocketHandler
                Debug.Log("Sending audio data to WebSocket" + wavBytes);
                if (webSocket == "PATIENT")
                {
                    await webSocketHandler.SendAudioData(wavBytes);
                }
                else if (webSocket == "NURSE")
                {
                    await webSocketHandler.SendStuffNurseAudio(wavBytes);
                }

            }
        }
        else
        {
            Debug.LogWarning("No audio was recorded.");
        }
    }
}