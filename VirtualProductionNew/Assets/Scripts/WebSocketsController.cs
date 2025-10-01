using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using NativeWebSocket;
using XRMultiplayer;
using Newtonsoft.Json;
using Unity.Netcode;
using System.Threading.Tasks;

public class WebSocketsController : MonoBehaviour
{
    private XRINetworkPlayer[] players;
    private GameObject[] playerGameObjects;

    private WebSocket webSocket;
    private bool isReconnecting = false;
    private bool isQuitting = false;

    public GameObject UIPrefab;
    private GameObject UI;

    public string URL = "wss://test.potentialdifference.org.uk/ws/fx/Unity";

    async void Start()
    {
        webSocket = new WebSocket(URL);

        webSocket.OnOpen += () =>
        {
            Debug.Log("WebSocket Connected");
            //StartCoroutine(SendData());
        };

        webSocket.OnError += (e) =>
        {
            Debug.LogError("WebSocket Error: " + e);
        };

        webSocket.OnClose += (e) =>
        {
            Debug.LogWarning($"WebSocket Closed: {e}");
            if (!isReconnecting && !isQuitting)
            {
                _ = Reconnect();
            }
        };

        webSocket.OnMessage += async (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("WebSocket Message: " + message);

            if(message.Contains("\"instruction\":\"keepalive\""))
            {
                Debug.Log("Responding to keepalive");
                await webSocket.SendText(message);
            }

        };

        await webSocket.Connect();
    }

    void Update()
    {
        webSocket?.DispatchMessageQueue();

        if (UI != null)
        {
            UI.transform.LookAt(Camera.main.transform);
            UI.transform.Rotate(0, 180, 0); // Flip to face camera
        }
    }

    public async void sendImpactPoint(ImpactPayload impactPayload)
    {
        if (webSocket == null || webSocket.State != WebSocketState.Open)
            return;

        string json = JsonConvert.SerializeObject(impactPayload);
        await webSocket.SendText(json);
        Debug.Log("Sent Impact Point " + json);
        
    }

    async void OnApplicationQuit()
    {
        isQuitting = true;
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            await webSocket.Close();
        }
    }

    void OnDestroy()
    {
        isQuitting = true;
    }

    public async void SendPlayerPositions()
    {
        if (webSocket == null || webSocket.State != WebSocketState.Open)
        {
            Debug.Log("WebSocket not open.");
            return;
        }

        players = GameObject.FindObjectsOfType<XRINetworkPlayer>();
        playerGameObjects = players.Select(p => p.gameObject).ToArray();

        List<Position> positions = new List<Position>();

        foreach (GameObject player in playerGameObjects)
        {
            Vector3 pos = player.transform.position;
            positions.Add(new Position
            {
                x = pos.x,
                y = pos.y,
                z = pos.z,
                gameObjectID = player.GetInstanceID().ToString(),
                tag = "playerAvatar"
            });
        }

        PositionPayload payload = new PositionPayload
        {
            instruction = "sendPositionData",
            positions = positions.ToArray()
        };

        string json = JsonConvert.SerializeObject(payload);
        Debug.Log("Sending Position Data: " + json);
        await webSocket.SendText(json);
    }

    public IEnumerator SendData()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton.IsServer && NetworkManager.Singleton.ConnectedClients.Count > 0);

        if (NetworkManager.Singleton.IsHost)
        {
            while (true)
            {
                SendPlayerPositions();
                yield return new WaitForSecondsRealtime(0.04f);
            }
        }
    }

    private async Task Reconnect()
    {
        isReconnecting = true;

        while (!isQuitting && (webSocket == null || webSocket.State == WebSocketState.Closed))
        {
            Debug.Log("Attempting to reconnect...");
            await Task.Delay(1000);

            if (isQuitting) break;

            await SetupWebSocket();

            if (webSocket.State == WebSocketState.Open)
            {
                Debug.Log("Reconnected successfully.");
                break;
            }
        }

        isReconnecting = false;
    }

    private async Task SetupWebSocket()
    {
        webSocket = new WebSocket("URL");

        webSocket.OnOpen += () =>
        {
            Debug.Log("Reconnected WebSocket");
            StartCoroutine(SendData());
        };

        webSocket.OnError += (e) =>
        {
            Debug.LogError("WebSocket Error (Reconnect): " + e);
        };

        webSocket.OnClose += (e) =>
        {
            Debug.LogWarning($"WebSocket Closed (Reconnect): {e}");
            if (!isReconnecting && !isQuitting)
            {
                _ = Reconnect();
            }
        };

        webSocket.OnMessage += async (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("WebSocket Message (Reconnect): " + message);

            if (message.Contains("\"instruction\":\"keepalive\""))
            {
                Debug.Log("Responding to keepalive");
                await webSocket.SendText(message);
            }
        };

        await webSocket.Connect();
    }
}

[System.Serializable]
public class Position
{
    public float x;
    public float y;
    public float z;
    public string gameObjectID;
    public string tag;
}

[System.Serializable]
public class PositionPayload
{
    public string instruction;
    public Position[] positions;
}

[System.Serializable]

public class ImpactPayload
{
    //  Format -> {"xDimension":0,"yDimension":0,"colour":null,"xPosition":-3,"yPosition":1,"instruction":"send2dClick"}
    public int xDimension;
    public int yDimension;
    public string colour;
    public int xPosition;
    public int yPosition;
    public string instruction;
    
}