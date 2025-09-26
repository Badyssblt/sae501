using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
using System.Text;

[Serializable]
public class NetworkMessage
{
    public string eventType;
}

[Serializable]
public class GameStateMessage
{
    public string eventType = "gameState";
    public float timeLeft;
    public int score;
    public string map;
    public List<PlayerStateData> players;
    public List<ObjectData> objects;
    public List<StationData> stations;
    public List<OrderData> orders;
}

[Serializable]
public class PlayerStateData
{
    public int id;
    public float x;
    public float y;
    public string carry;
    public string action;
}

[Serializable]
public class ObjectData
{
    public string id;
    public string type;
    public float x;
    public float y;
    public int? heldBy;
}

[Serializable]
public class StationData
{
    public string id;
    public string type;
    public string occupiedBy;
    public string cookingState;
    public float? timeRemaining;
}

[Serializable]
public class OrderData
{
    public string id;
    public List<string> ingredients;
    public string status;
    public float timeRemaining;
}

[Serializable]
public class InputMessage
{
    public int playerId;
    public float horizontal;
    public float vertical;
    public bool action;
}

[Serializable]
public class RegisterHostMessage
{
    public string eventType = "registerAsHost";
}

[Serializable]
public class SetupLobbyMessage
{
    public string eventType = "setupLobby";
    public string map;
}

[Serializable]
public class StartGameMessage
{
    public string eventType = "startGame";
    public string map;
}

public class NetworkManager : MonoBehaviour
{
    private static NetworkManager _instance;
    public static NetworkManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<NetworkManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("NetworkManager");
                    _instance = go.AddComponent<NetworkManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("Network Settings")]
    [SerializeField] private string serverUrl = "ws://localhost:4000";
    [SerializeField] private float gameStateSendRate = 30f; // 30 FPS

    private WebSocket websocket;
    private bool isConnected = false;
    private float lastGameStateSent = 0f;

    // Stockage temporaire des inputs reçus
    private Dictionary<int, InputMessage> remoteInputs = new Dictionary<int, InputMessage>();

    // Events
    public event Action<int, string> OnPlayerJoined;
    public event Action<int> OnPlayerLeft;
    public event Action<InputMessage> OnInputReceived;
    public event Action OnGameStarted;
    public event Action<int> OnGameEnded;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        await ConnectToServer();
    }

    private async System.Threading.Tasks.Task ConnectToServer()
    {
        try
        {
            websocket = new WebSocket(serverUrl);

            websocket.OnOpen += () =>
            {
                Debug.Log("WebSocket connecté!");
                isConnected = true;
                RegisterAsHost();
            };

            websocket.OnError += (e) =>
            {
                Debug.LogError($"WebSocket erreur: {e}");
            };

            websocket.OnClose += (e) =>
            {
                Debug.Log("WebSocket fermé");
                isConnected = false;
            };

            websocket.OnMessage += (bytes) =>
            {
                string message = Encoding.UTF8.GetString(bytes);
                ProcessMessage(message);
            };

            await websocket.Connect();
        }
        catch (Exception e)
        {
            Debug.LogError($"Erreur connexion WebSocket: {e}");
        }
    }

    private void ProcessMessage(string message)
    {
        try
        {
            // Socket.IO encapsule les messages dans un format spécial
            // Format: 42["eventName",{data}] ou 42{"eventName":"value"}

            // Retirer le préfixe Socket.IO si présent
            if (message.StartsWith("42"))
            {
                message = message.Substring(2);
            }

            // Si c'est un tableau Socket.IO
            if (message.StartsWith("["))
            {
                // Parser comme tableau ["eventName", {data}]
                var parts = message.TrimStart('[').TrimEnd(']').Split(new[] { ',' }, 2);
                if (parts.Length >= 2)
                {
                    string eventName = parts[0].Trim('"');
                    string data = parts[1];

                    switch (eventName)
                    {
                        case "input":
                            var inputMsg = JsonUtility.FromJson<InputMessage>(data);
                            remoteInputs[inputMsg.playerId] = inputMsg;
                            OnInputReceived?.Invoke(inputMsg);
                            Debug.Log($"Input reçu du joueur {inputMsg.playerId}: H={inputMsg.horizontal} V={inputMsg.vertical} Action={inputMsg.action}");
                            break;

                        case "playerJoined":
                            // Format: {"slot": 3, "name": "PlayerName"}
                            var joinData = JsonUtility.FromJson<PlayerJoinedData>(data);
                            OnPlayerJoined?.Invoke(joinData.slot, joinData.name);
                            break;

                        case "playerLeft":
                            // Format: {"slot": 3}
                            var leftData = JsonUtility.FromJson<PlayerLeftData>(data);
                            OnPlayerLeft?.Invoke(leftData.slot);
                            break;

                        case "gameStarted":
                            OnGameStarted?.Invoke();
                            Debug.Log("Partie démarrée!");
                            break;

                        case "gameEnded":
                            var endData = JsonUtility.FromJson<GameEndedData>(data);
                            OnGameEnded?.Invoke(endData.score);
                            break;

                        case "lobbyUpdate":
                            // Format: {"players": {...}}
                            Debug.Log($"Lobby update reçu: {data}");
                            break;

                        case "gameStatusUpdate":
                            // Format: {"status": "ready", "map": "italie"}
                            Debug.Log($"Status update reçu: {data}");
                            break;
                    }
                }
            }
            else
            {
                // Format direct d'objet (fallback)
                var baseMsg = JsonUtility.FromJson<NetworkMessage>(message);
                Debug.Log($"Message direct reçu: {message}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Erreur parsing message: {e}\nMessage: {message}");
        }
    }

    [Serializable]
    private class PlayerJoinedData
    {
        public int slot;
        public string name;
    }

    [Serializable]
    private class PlayerLeftData
    {
        public int slot;
    }

    [Serializable]
    private class GameEndedData
    {
        public int score;
    }

    public void RegisterAsHost()
    {
        if (!isConnected) return;

        // Format Socket.IO: 42["eventName",data]
        string message = "42[\"registerAsHost\"]";
        SendMessage(message);
        Debug.Log("Enregistré comme host");
    }

    public void SetupLobby(string mapName)
    {
        if (!isConnected) return;

        // Format Socket.IO: 42["setupLobby",{"map":"italie"}]
        var data = new { map = mapName };
        string json = JsonUtility.ToJson(data);
        string message = $"42[\"setupLobby\",{json}]";
        SendMessage(message);
        Debug.Log($"Lobby configuré avec map: {mapName}");
    }

    public void StartGame(string mapName)
    {
        if (!isConnected) return;

        // Format Socket.IO: 42["startGame",{"map":"italie"}]
        var data = new { map = mapName };
        string json = JsonUtility.ToJson(data);
        string message = $"42[\"startGame\",{json}]";
        SendMessage(message);
        Debug.Log("Partie lancée!");
    }

    public void SendGameState(GameStateMessage gameState)
    {
        if (!isConnected) return;

        // Limiter l'envoi à 30 FPS
        if (Time.time - lastGameStateSent < 1f / gameStateSendRate)
            return;

        // Format Socket.IO: 42["gameState",{...}]
        string json = JsonUtility.ToJson(gameState);
        string message = $"42[\"gameState\",{json}]";
        SendMessage(message);
        lastGameStateSent = Time.time;
    }

    public void EndGame(int finalScore)
    {
        if (!isConnected) return;

        // Format Socket.IO: 42["endGame",{"score":150}]
        var data = new { score = finalScore };
        string json = JsonUtility.ToJson(data);
        string message = $"42[\"endGame\",{json}]";
        SendMessage(message);
        Debug.Log($"Partie terminée envoyée avec score: {finalScore}");
    }

    public InputMessage GetRemoteInput(int playerId)
    {
        if (remoteInputs.TryGetValue(playerId, out InputMessage input))
        {
            return input;
        }
        return null;
    }

    private void SendMessage(string message)
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            websocket.SendText(message);
        }
    }

    private void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
        if (websocket != null)
            websocket.DispatchMessageQueue();
        #endif
    }

    private async void OnDestroy()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            await websocket.Close();
        }
    }

    private async void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus && websocket != null && websocket.State != WebSocketState.Open)
        {
            await ConnectToServer();
        }
    }
}