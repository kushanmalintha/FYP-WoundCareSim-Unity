using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class WebSocketHandler : MonoBehaviour
{
    [Header("WebSocket Configuration")]
    [SerializeField] private string baseUrl = "ws://127.0.0.1:8000/"; // Local development WebSocket URL
    [SerializeField] private ConversationHandler conversationHandler;

    private ClientWebSocket wsAudioChat;
    private ClientWebSocket wsMCQ;
    private ClientWebSocket wsStuffNurse;

    private CancellationTokenSource cancellationTokenSource;

    public event Action<byte[]> OnAudioDataReceived;
    public event Action<string> OnTextMessageReceived;
    public event Action<string> OnWebSocketConnected;
    public event Action<string> OnWebSocketDisconnected;
    public event Action<string> OnWebSocketError;

    private readonly Queue<Action> mainThreadActions = new Queue<Action>();

    void Start()
    {
        // Get reference to ConversationHandler if not assigned
        if (conversationHandler == null)
        {
            conversationHandler = FindObjectOfType<ConversationHandler>();
        }

        // Subscribe to audio events
        if (conversationHandler != null)
        {
            OnAudioDataReceived += conversationHandler.HandleAudioData;
            OnTextMessageReceived += conversationHandler.HandleTextMessage;
            OnWebSocketConnected += conversationHandler.HandleWebSocketConnected;
            OnWebSocketDisconnected += conversationHandler.HandleWebSocketDisconnected;
            OnWebSocketError += conversationHandler.HandleWebSocketError;
        }

        // Configure for backend audio format (48kHz, Mono, 16-bit PCM)
        conversationHandler?.ConfigureForBackendExact();

        InitializeWebSockets();
    }

    void Update()
    {
        lock (mainThreadActions)
        {
            while (mainThreadActions.Count > 0)
            {
                mainThreadActions.Dequeue()?.Invoke();
            }
        }
    }

    /// <summary>
    /// Initialize all three WebSocket connections
    /// </summary>
    public async void InitializeWebSockets()
    {
        cancellationTokenSource = new CancellationTokenSource();

        try
        {
            // Initialize Audio Chat WebSocket
            wsAudioChat = await InitializeWebSocket("/history", "Audio Chat");

            // Initialize MCQ WebSocket
            wsMCQ = await InitializeWebSocket("/mcq", "MCQ");

            // Initialize Stuff Nurse WebSocket
            wsStuffNurse = await InitializeWebSocket("/stuff_nurse", "Stuff Nurse");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error initializing WebSockets: {ex.Message}");
            OnWebSocketError?.Invoke($"Initialization error: {ex.Message}");
        }
    }

    private async Task<ClientWebSocket> InitializeWebSocket(string endpoint, string connectionName)
    {
        try
        {
            ClientWebSocket webSocket = new ClientWebSocket();
            string uri = $"{baseUrl}{endpoint}";

            Debug.Log($"Connecting to {connectionName} WebSocket: {uri}");

            await webSocket.ConnectAsync(new Uri(uri), cancellationTokenSource.Token);

            Debug.Log($"{connectionName} WebSocket connected successfully");
            OnWebSocketConnected?.Invoke(connectionName);

            // Start listening for messages
            _ = Task.Run(() => ReceiveMessages(webSocket, connectionName));

            return webSocket;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error connecting {connectionName} WebSocket: {ex.Message}");
            OnWebSocketError?.Invoke($"{connectionName} connection error: {ex.Message}");
            return null;
        }
    }

    private async Task ReceiveMessages(ClientWebSocket webSocket, string connectionName)
    {
        byte[] buffer = new byte[1024 * 16]; // Increased to 16KB for better performance

        try
        {
            while (webSocket.State == WebSocketState.Open && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                // ?? Collect ALL fragments of large audio messages
                List<byte> completeMessage = new List<byte>();
                WebSocketReceiveResult result;
                int fragmentCount = 0;

                do
                {
                    result = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        cancellationTokenSource.Token
                    );

                    fragmentCount++;

                    // Add this fragment to our complete message
                    for (int i = 0; i < result.Count; i++)
                    {
                        completeMessage.Add(buffer[i]);
                    }

                    Debug.Log($"?? Fragment {fragmentCount}: {result.Count} bytes, EndOfMessage: {result.EndOfMessage}");

                } while (!result.EndOfMessage); // ?? Continue until complete message

                // Convert to final byte array
                byte[] finalMessage = completeMessage.ToArray();

                Debug.Log($"? Complete message received: {finalMessage.Length} bytes from {fragmentCount} fragments");

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    // Handle COMPLETE binary audio data
                    lock (mainThreadActions)
                    {
                        mainThreadActions.Enqueue(() => OnAudioDataReceived?.Invoke(finalMessage));
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    // Handle text message
                    string message = Encoding.UTF8.GetString(finalMessage);

                    lock (mainThreadActions)
                    {
                        mainThreadActions.Enqueue(() => OnTextMessageReceived?.Invoke(message));
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    Debug.Log($"{connectionName} WebSocket closed by server");
                    lock (mainThreadActions)
                    {
                        mainThreadActions.Enqueue(() => OnWebSocketDisconnected?.Invoke(connectionName));
                    }
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error receiving messages from {connectionName}: {ex.Message}");
            StartCoroutine(InvokeOnMainThread(() => OnWebSocketError?.Invoke($"{connectionName} receive error: {ex.Message}")));
        }
    }

    private IEnumerator InvokeOnMainThread(Action action)
    {
        action?.Invoke();
        yield return null;
    }

    /// <summary>
    /// Send audio data to the audio chat WebSocket
    /// </summary>
    public async Task SendAudioData(byte[] audioData)
    {
        conversationHandler?.MarkRequestStart("Audio Chat");
        await SendBinaryData(wsAudioChat, audioData, "Audio Chat");
    }

    /// <summary>
    /// Send MCQ data to the MCQ WebSocket
    /// </summary>
    public async Task SendMCQData(string jsonData)
    {
        conversationHandler?.MarkRequestStart("MCQ");
        await SendTextData(wsMCQ, jsonData, "MCQ");
    }

    /// <summary>
    /// Send MCQ data using MCQPayload structure (convenience method)
    /// </summary>
    public async Task SendMCQData(int questionId, string answer, string question)
    {
        MCQPayload payload = new MCQPayload(questionId, answer, question);
        await SendMCQData(payload.ToJson());
    }

    /// <summary>
    /// Send sample MCQ data (matching JavaScript example)
    /// </summary>
    public async Task SendSampleMCQData(int number, string answer)
    {
        await SendMCQData(number, answer, "What is the type of wound shown in the scenario? (Select all that apply)");
    }

    /// <summary>
    /// Send audio data to the Stuff Nurse WebSocket
    /// </summary>
    public async Task SendStuffNurseAudio(byte[] audioData)
    {
        conversationHandler?.MarkRequestStart("Stuff Nurse");
        await SendBinaryData(wsStuffNurse, audioData, "Stuff Nurse");
    }

    private async Task SendBinaryData(ClientWebSocket webSocket, byte[] data, string connectionName)
    {
        try
        {
            if (webSocket?.State == WebSocketState.Open)
            {
                await webSocket.SendAsync(
                    new ArraySegment<byte>(data),
                    WebSocketMessageType.Binary,
                    true,
                    cancellationTokenSource.Token
                );
                Debug.Log($"Sent binary data to {connectionName} WebSocket, size: {data.Length}");
            }
            else
            {
                Debug.LogWarning($"{connectionName} WebSocket is not connected");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error sending binary data to {connectionName}: {ex.Message}");
            OnWebSocketError?.Invoke($"{connectionName} send error: {ex.Message}");
        }
    }

    private async Task SendTextData(ClientWebSocket webSocket, string data, string connectionName)
    {
        try
        {
            if (webSocket?.State == WebSocketState.Open)
            {
                byte[] buffer = Encoding.UTF8.GetBytes(data);
                await webSocket.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    cancellationTokenSource.Token
                );
                Debug.Log($"Sent text data to {connectionName} WebSocket: {data}");
            }
            else
            {
                Debug.LogWarning($"{connectionName} WebSocket is not connected");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error sending text data to {connectionName}: {ex.Message}");
            OnWebSocketError?.Invoke($"{connectionName} send error: {ex.Message}");
        }
    }

    void OnDestroy()
    {
        // Cleanup WebSocket connections
        CloseAllWebSockets();
    }

    private async void CloseAllWebSockets()
    {
        cancellationTokenSource?.Cancel();

        try
        {
            if (wsAudioChat?.State == WebSocketState.Open)
                await wsAudioChat.CloseAsync(WebSocketCloseStatus.NormalClosure, "Application closing", CancellationToken.None);

            if (wsMCQ?.State == WebSocketState.Open)
                await wsMCQ.CloseAsync(WebSocketCloseStatus.NormalClosure, "Application closing", CancellationToken.None);

            if (wsStuffNurse?.State == WebSocketState.Open)
                await wsStuffNurse.CloseAsync(WebSocketCloseStatus.NormalClosure, "Application closing", CancellationToken.None);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error closing WebSockets: {ex.Message}");
        }
        finally
        {
            wsAudioChat?.Dispose();
            wsMCQ?.Dispose();
            wsStuffNurse?.Dispose();
            cancellationTokenSource?.Dispose();
        }
    }
}