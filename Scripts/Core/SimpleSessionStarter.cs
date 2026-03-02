using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class SessionResponse
{
    public string session_id;
    public string session_token;
}

public class SimpleSessionStarter : MonoBehaviour
{
    private const string studentId = "student_001";
    private const string scenarioId = "scenario_001";
    private const string baseUrl = "http://192.168.8.153:8000/session";

    private void Start()
    {
        StartCoroutine(SendSessionStartRequest());
    }

    private IEnumerator SendSessionStartRequest()
    {
        Debug.Log("Sending session start request...");

        string url = baseUrl + "/start";
        // Create the JSON body
        string jsonBody = "{\"scenario_id\": \"" + scenarioId + "\", \"student_id\": \"" + studentId + "\"}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        // Create the request
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Send the request
        yield return request.SendWebRequest();

        // Handle the response
        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            Debug.Log("Session started: " + responseText);

            try
            {
                SessionResponse response = JsonUtility.FromJson<SessionResponse>(responseText);
                if (!string.IsNullOrEmpty(response.session_id))
                {
                    StartCoroutine(GetSessionMetadata(response.session_id));
                }
                else
                {
                    Debug.LogError("Session ID is null or empty in response.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to parse session start response: " + e.Message);
            }
        }
        else
        {
            Debug.LogError("Session start failed: " + request.error + " - " + request.downloadHandler.text);
        }
    }

    private IEnumerator GetSessionMetadata(string sessionId)
    {
        Debug.Log("Fetching metadata for session: " + sessionId + "...");

        string url = baseUrl + "/" + sessionId;
        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Metadata received: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Metadata fetch failed: " + request.error + " - " + request.downloadHandler.text);
        }
    }
}
