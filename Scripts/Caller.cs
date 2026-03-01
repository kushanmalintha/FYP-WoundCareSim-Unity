using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Caller : MonoBehaviour
{
    [SerializeField] private RequestHandler requestHandler;
    [SerializeField] private WebSocketHandler webSocketHandler;

    void Start()
    {
        // If RequestHandler is not assigned, try to find it
        if (requestHandler == null)
        {
            requestHandler = FindObjectOfType<RequestHandler>();
        }

        // Make the call on start (you can call this from anywhere)
        if (requestHandler != null)
        {
            CallHelloEndpoint();
        }
        else
        {
            Debug.LogError("RequestHandler not found! Please assign it in the inspector or ensure it exists in the scene.");
        }

        webSocketHandler.InitializeWebSockets();

    }

    void Update()
    {
        
    }

    public void CallHelloEndpoint()
    {
        requestHandler.MakeHelloRequest();
    }

}
