using Newtonsoft.Json.Linq;
using UnityEngine;

public static class BackendEventRouter
{
    public static void Route(string message)
    {
        try
        {
            JObject obj = JObject.Parse(message);

            string type = obj["type"]?.ToString();
            string eventName = obj["event"]?.ToString();
            JObject data = obj["data"] as JObject;

            if (type == "server_event")
            {
                HandleServerEvent(eventName, data);
            }
            else if (type == "error")
            {
                Debug.LogError("Backend error: " + data?.ToString());
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Event routing failed: " + e.Message);
        }
    }

    private static void HandleServerEvent(string eventName, JObject data)
    {
        switch (eventName)
        {
            case "nurse_message":
                if (data != null)
                {
                    string text = data["text"]?.ToString();
                    string role = data["role"]?.ToString();
                    if (HistoryStepController.Instance != null)
                    {
                        HistoryStepController.Instance.HandleNurseMessage(text, role);
                    }
                }
                break;

            case "tts_audio":
                if (data != null && data["audio_bytes"] != null)
                {
                    if (TTSAudioManager.Instance != null)
                    {
                        TTSAudioManager.Instance.PlayTTS(data["audio_bytes"].ToString());
                    }
                    else
                    {
                        Debug.LogError("[BackendEventRouter] TTSAudioManager instance is null!");
                    }
                }
                break;

            case "transcription_result":
                if (data != null && data["is_final"]?.Value<bool>() == true)
                {
                    string transcript = data["text"]?.ToString();
                    Debug.Log("TRANSCRIPT: " + transcript);

                    if (PushToTalkController.Instance != null)
                    {
                        if (PushToTalkController.Instance.CurrentTarget == VoiceTarget.Patient)
                        {
                            BackendConnectionManager.Instance.SendEvent(
                                "text_message",
                                JObject.FromObject(new { text = transcript })
                            );
                        }
                        else if (PushToTalkController.Instance.CurrentTarget == VoiceTarget.Nurse)
                        {
                            BackendConnectionManager.Instance.SendEvent(
                                "nurse_message",
                                JObject.FromObject(new { text = transcript })
                            );
                        }
                        PushToTalkController.Instance.ResetTarget();
                    }
                }
                break;

            case "final_feedback":
                Debug.Log("Final Feedback Received");
                Debug.Log(data?.ToString());
                break;

            case "assessment_summary":
                Debug.Log("Assessment Summary Received");
                Debug.Log(data?.ToString());
                break;

            case "mcq_answer_result":
                Debug.Log("MCQ Answer Result Received");
                Debug.Log(data?.ToString());
                break;

            case "step_complete":
                Debug.Log("Step Complete Event Received");
                Debug.Log("Next Step: " + data?["next_step"]);
                break;

            case "session_end":
                Debug.Log("Session End Received");
                break;

            default:
                Debug.Log("Unhandled event: " + eventName);
                break;
        }
    }
}
