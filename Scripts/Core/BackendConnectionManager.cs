using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[Serializable]
public class SessionResponse
{
    public string session_id;
    public string session_token;
}

public class BackendConnectionManager : MonoBehaviour
{
    public static BackendConnectionManager Instance;

    private const string studentId = "student_001";
    private const string scenarioId = "scenario_001";
    private const string baseUrl = "http://192.168.8.153:8000/session";
    private const string wsUrl = "ws://192.168.8.153:8000/ws/session";

    private ClientWebSocket webSocket;
    private CancellationTokenSource cancellationToken;
    private string currentSessionId;
    private string currentSessionToken;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(SendSessionStartRequest());
    }

    private void OnDestroy()
    {
        if (webSocket != null)
        {
            cancellationToken?.Cancel();
            webSocket.Dispose();
        }
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
                    currentSessionId = response.session_id;
                    currentSessionToken = response.session_token;
                    StartCoroutine(GetSessionMetadata(currentSessionId));
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
            
            // Connect to WebSocket after metadata is received
            _ = ConnectWebSocket(currentSessionId, currentSessionToken);
        }
        else
        {
            Debug.LogError("Metadata fetch failed: " + request.error + " - " + request.downloadHandler.text);
        }
    }

    private async Task ConnectWebSocket(string sessionId, string sessionToken)
    {
        try
        {
            webSocket = new ClientWebSocket();
            cancellationToken = new CancellationTokenSource();

            string fullWsUrl = $"{wsUrl}/{sessionId}?token={sessionToken}";
            Debug.Log($"Connecting to WebSocket: {fullWsUrl}");

            await webSocket.ConnectAsync(new Uri(fullWsUrl), cancellationToken.Token);
            Debug.Log("Backend connection ready.");
            StepFlowController.Instance.SetInitialStep();

            _ = ReceiveLoop();
        }
        catch (Exception e)
        {
            Debug.LogError($"WebSocket connection error: {e.Message}");
        }
    }

    public async Task SendEvent(string eventName, JObject data)
    {
        if (webSocket == null || webSocket.State != WebSocketState.Open)
        {
            Debug.LogError("Cannot send event: WebSocket is not open.");
            return;
        }

        JObject payload = new JObject
        {
            ["type"] = "event",
            ["event"] = eventName,
            ["data"] = data
        };

        string json = JsonConvert.SerializeObject(payload);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        
        await webSocket.SendAsync(
            new ArraySegment<byte>(bytes), 
            WebSocketMessageType.Text, 
            true, 
            cancellationToken.Token
        );
    }

    private async Task ReceiveLoop()
    {
        byte[] buffer = new byte[8192];

        try
        {
            while (webSocket.State == WebSocketState.Open && !cancellationToken.Token.IsCancellationRequested)
            {
                using (var ms = new MemoryStream())
                {
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken.Token);
                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Debug.Log("WebSocket closed by server");
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken.Token);
                        break;
                    }

                    ms.Seek(0, SeekOrigin.Begin);
                    string message = Encoding.UTF8.GetString(ms.ToArray());

                    Debug.Log("Full WS message received, length: " + message.Length);
                    
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        BackendEventRouter.Route(message);
                    });
                }
            }
        }
        catch (Exception e)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                Debug.LogError($"WebSocket receive error: {e.Message}");
            }
        }
    }
}
