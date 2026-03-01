using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class RequestHandler : MonoBehaviour
{
    private const string ENDPOINT = "http://127.0.0.1:8000/";
    [SerializeField] private ConversationHandler conversationHandler;
    public GameObject resultsPanel;
    public GameObject loadingSpinner;

    void Awake()
    {
        StartCoroutine(SendDeleteRequest());
    }

    void Start()
    {

    }

    void Update()
    {

    }

    public void MakeHelloRequest()
    {
        StartCoroutine(SendHelloRequest());
    }

    private IEnumerator SendHelloRequest()
    {

        using (UnityWebRequest webRequest = UnityWebRequest.Get(ENDPOINT + "/hello"))
        {
            // Set request timeout (optional)
            webRequest.timeout = 20;

            // Send the request and wait for response
            yield return webRequest.SendWebRequest();

            // Check for errors
            if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Request failed: " + webRequest.error);
                Debug.LogError("Response Code: " + webRequest.responseCode);
            }
            else
            {
                // Success - log the response
                Debug.Log("Request successful!");
                Debug.Log("Response: " + webRequest.downloadHandler.text);
            }
        }
    }

    private IEnumerator SendDeleteRequest()
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Delete(ENDPOINT + "/delete-logs"))
        {
            // Set request timeout (optional)
            webRequest.timeout = 10;

            // Send the request and wait for response
            yield return webRequest.SendWebRequest();

            // Check for errors
            if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("DELETE request failed: " + webRequest.error);
                Debug.LogError("Response Code: " + webRequest.responseCode);
            }
            else
            {
                // Success - log the response
                Debug.Log("DELETE request successful!");
                if (webRequest.downloadHandler != null)
                {
                    Debug.Log("Response Delete: " + webRequest.downloadHandler.text);
                }

            }
        }
    }


    public void SendApiRequest(string endpoint, string jsonBody)
    {
        StartCoroutine(SendAPIRequestAndPlayAudio(endpoint, jsonBody));
    }

    private IEnumerator SendAPIRequestAndPlayAudio(string endpoint, string jsonBody)
    {
        Debug.Log($"🚀 Sending POST request to: {ENDPOINT + endpoint}");

        using (UnityWebRequest webRequest = new UnityWebRequest(ENDPOINT + endpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);

            // ✅ FIX: Use DownloadHandlerBuffer from the start to get raw bytes
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Request failed: {webRequest.error}");
            }
            else
            {
                string contentType = webRequest.GetResponseHeader("Content-Type");
                string feedbackJson = webRequest.GetResponseHeader("X-Evaluation-Data");
                Debug.Log($"✅ Request successful! Content-Type: {contentType}");

                byte[] audioBytes = webRequest.downloadHandler.data;

                if (audioBytes != null && audioBytes.Length > 0)
                {
                    // Use ConversationHandler to process and play the audio
                    // Working with raw PCM audio data (can wrapped with WAV,  chnage the sample rate if needed)
                    if (conversationHandler != null)
                    {
                        Debug.Log($"feedbackJson: {feedbackJson}");
                        resultsPanel.SetActive(true);
                        loadingSpinner.SetActive(false);
                        Debug.Log($"🎵 Playing audio of length: {audioBytes.Length} bytes");
                        int sampleRate = 24000;
                        StartCoroutine(conversationHandler.PlayRawPCMAudio(audioBytes, sampleRate));
                    }
                    else
                    {
                        Debug.LogError("❌ ConversationHandler is null! Please assign it in the inspector.");
                    }
                }
                else
                {
                    Debug.LogError("❌ No audio data received");
                }
            }
        }
    }


    public void SendApiRequestAndGetJson(string endpoint, System.Action<string> onSuccess, System.Action<string> onError)
    {
        StartCoroutine(GetJsonFromEndpoint(endpoint, onSuccess, onError));
    }
    private IEnumerator GetJsonFromEndpoint(string endpoint, System.Action<string> onSuccess, System.Action<string> onError)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(ENDPOINT + endpoint))
        {
            // Send the request and wait for response
            yield return webRequest.SendWebRequest();

            // Check for errors
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Request to {endpoint} failed: {webRequest.error}");
                onError?.Invoke(webRequest.error); // Invoke the error callback
            }
            else
            {
                Debug.Log($"✅ Request to {endpoint} successful!");
                string jsonResponse = webRequest.downloadHandler.text;
                Debug.Log($"📦 JSON Response: {jsonResponse}");
                onSuccess?.Invoke(jsonResponse); // Invoke the success callback
            }
        }
    }

}
