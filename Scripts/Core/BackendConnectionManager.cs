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
    public const string IP = "192.168.8.100";

    private string baseUrl => $"http://{IP}:8000/session";
    private string activeSessionUrl => $"http://{IP}:8000/session/active";
    private string wsUrl => $"ws://{IP}:8000/ws/session";

    private ClientWebSocket webSocket;
    private CancellationTokenSource cancellationToken;

    private string currentSessionId;
    private string currentSessionToken;

    // Store scenario metadata
    public JObject SessionMetadata { get; private set; }

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
        StartCoroutine(GetActiveSession());
    }

    private void OnDestroy()
    {
        if (webSocket != null)
        {
            cancellationToken?.Cancel();
            webSocket.Dispose();
        }
    }

    // ---------------------------------------------------------
    // FETCH ACTIVE SESSION FROM BACKEND
    // ---------------------------------------------------------

    private IEnumerator GetActiveSession()
    {
        Debug.Log("Fetching active session from backend...");

        UnityWebRequest request = UnityWebRequest.Get(activeSessionUrl);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            Debug.Log("Active session received: " + responseText);

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
                    Debug.LogError("Active session response missing session_id.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to parse active session response: " + e.Message);
            }
        }
        else
        {
            Debug.LogError("Active session fetch failed: " + request.error);
        }
    }

    // ---------------------------------------------------------
    // FETCH SESSION METADATA
    // ---------------------------------------------------------

    private IEnumerator GetSessionMetadata(string sessionId)
    {
        Debug.Log("Fetching metadata for session: " + sessionId);

        string url = baseUrl + "/" + sessionId;

        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            Debug.Log("Metadata received: " + responseText);

            try
            {
                JObject parsed = JObject.Parse(responseText);

                if (parsed["scenario_metadata"] != null)
                {
                    SessionMetadata = parsed["scenario_metadata"] as JObject;
                    Debug.Log("Scenario metadata stored successfully.");
                }
                else
                {
                    Debug.LogError("scenario_metadata not found in response.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to parse metadata JSON: " + e.Message);
            }

            _ = ConnectWebSocket(currentSessionId, currentSessionToken);
        }
        else
        {
            Debug.LogError("Metadata fetch failed: " + request.error + " - " + request.downloadHandler.text);
        }
    }

    // ---------------------------------------------------------
    // WEBSOCKET CONNECTION
    // ---------------------------------------------------------

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

    // ---------------------------------------------------------
    // SEND WEBSOCKET EVENT
    // ---------------------------------------------------------

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

    // ---------------------------------------------------------
    // RECEIVE LOOP
    // ---------------------------------------------------------

    private async Task ReceiveLoop()
    {
        byte[] buffer = new byte[8192];

        try
        {
            while (webSocket.State == WebSocketState.Open &&
                   !cancellationToken.Token.IsCancellationRequested)
            {
                using (var ms = new MemoryStream())
                {
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await webSocket.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            cancellationToken.Token
                        );

                        ms.Write(buffer, 0, result.Count);

                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Debug.Log("WebSocket closed by server");

                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            string.Empty,
                            cancellationToken.Token
                        );

                        break;
                    }

                    ms.Seek(0, SeekOrigin.Begin);

                    string message = Encoding.UTF8.GetString(ms.ToArray());

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

    // ---------------------------------------------------------
    // REST STEP COMPLETION
    // ---------------------------------------------------------

    public IEnumerator CompleteStep(string stepName)
    {
        Debug.Log("Sending step completion request for: " + stepName);

        string url = baseUrl + "/complete-step";

        JObject body = new JObject
        {
            ["session_id"] = currentSessionId,
            ["step"] = stepName
        };

        byte[] bodyRaw = Encoding.UTF8.GetBytes(body.ToString());

        UnityWebRequest request = new UnityWebRequest(url, "POST");

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;

            Debug.Log("Step completion successful: " + responseText);

            try
            {
                JObject response = JObject.Parse(responseText);

                if (response["feedback_audio"] != null &&
                    response["feedback_audio"]["audio_base64"] != null)
                {
                    string base64Audio =
                        response["feedback_audio"]["audio_base64"].ToString();

                    if (TTSAudioManager.Instance != null)
                        TTSAudioManager.Instance.PlayTTS(base64Audio);
                }

                if (response["next_step"] != null)
                {
                    string nextStep = response["next_step"].ToString();

                    StepFlowController.Instance.AdvanceTo(nextStep);

                    Debug.Log("Advanced to next step: " + nextStep);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to parse completion response: " + e.Message);
            }
        }
        else
        {
            Debug.LogError("Step completion failed: " + request.error + " - " + request.downloadHandler.text);
        }
    }
}
