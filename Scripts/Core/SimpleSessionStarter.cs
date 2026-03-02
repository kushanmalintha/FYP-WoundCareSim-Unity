using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SimpleSessionStarter : MonoBehaviour
{
    private const string studentId = "student_001";
    private const string scenarioId = "scenario_001";
    private const string url = "http://192.168.8.153:8000/session/start";

    private void Start()
    {
        StartCoroutine(SendSessionStartRequest());
    }

    private IEnumerator SendSessionStartRequest()
    {
        Debug.Log("Sending session start request...");

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
            Debug.Log("Session started: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Session start failed: " + request.error + " - " + request.downloadHandler.text);
        }
    }
}
