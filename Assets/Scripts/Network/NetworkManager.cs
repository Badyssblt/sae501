using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using NativeWebSocket;
using CookMoiCa.Network;

/// <summary>
/// NetworkManager - Gère toute la communication réseau
/// Supporte deux modes :
/// - Host (borne d'arcade) : Source de vérité, broadcast l'état
/// - Client (navigateur web) : Prediction locale, réconciliation avec serveur
/// </summary>
public class NetworkManager : MonoBehaviour
{
    private static NetworkManager _instance;
    public static NetworkManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<NetworkManager>();
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

    // ============================================================
    // CONFIGURATION
    // ============================================================

    [Header("Network Settings")]
    [SerializeField] private string serverUrl = "wss://cook.wevora.fr";
    [SerializeField] private float deltaSendRate = 20f; // 20 FPS pour les deltas
    [SerializeField] private float fullStateSendRate = 2f; // Full state toutes les 500ms

    [Header("Interpolation Settings")]
    [SerializeField] private float interpolationDelay = 0.1f; // 100ms

    // ============================================================
    // ÉTAT RÉSEAU
    // ============================================================

    private WebSocket websocket;
    public bool IsConnected { get; private set; } = false;

    // Mode (déterminé au démarrage)
    public NetworkRole Role { get; private set; } = NetworkRole.Host;

    // Paramètres client (si mode Client)
    public int LocalPlayerSlot { get; private set; } = -1;
    public string LocalPlayerName { get; private set; } = "";
    public string RequestedMap { get; private set; } = "";

    // ============================================================
    // SYSTÈME DE TICKS
    // ============================================================

    public uint CurrentTick { get; private set; } = 0;
    private const float TICK_RATE = 1f / 30f; // 30 ticks par seconde
    private float tickAccumulator = 0f;

    // ============================================================
    // INTERPOLATION & PREDICTION (Client seulement)
    // ============================================================

    public InterpolationBuffer InterpolationBuffer { get; private set; }
    public PredictionSystem PredictionSystem { get; private set; }

    // ============================================================
    // DELTA SYNC (Host seulement)
    // ============================================================

    private StateSnapshot lastSentState;
    private float lastDeltaSent = 0f;
    private float _cleanTimer = 0f;
    private float lastFullStateSent = 0f;

    // ============================================================
    // INPUTS DISTANTS (Host seulement)
    // ============================================================

    private Dictionary<int, InputMessage> remoteInputs = new Dictionary<int, InputMessage>();

    // ============================================================
    // EVENTS
    // ============================================================

    public event Action<int, string> OnPlayerJoined;
    public event Action<int> OnPlayerLeft;
    public event Action<InputMessage> OnInputReceived;
    public event Action OnGameStarted;
    public event Action<int> OnGameEnded;

    // Nombre de joueurs locaux sur le host (reçu via gameStarted)
    public int HostLocalPlayerCount { get; private set; } = 1;
    public event Action<StateSnapshot> OnStateReceived; // Client: reçoit état serveur
    public event Action<GameEventMessage> OnGameEvent; // Events instantanés
    public event Action OnAllPlayersReady; // Tous les joueurs distants sont prêts
    public event Action<int> OnRemotePlayerReady; // Un joueur distant est prêt (slot)

    // ============================================================
    // UNITY LIFECYCLE
    // ============================================================

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialiser les systèmes
        InterpolationBuffer = new InterpolationBuffer(interpolationDelay);
        PredictionSystem = new PredictionSystem();

        // Déterminer le rôle
        DetermineNetworkRole();
    }

    private async void Start()
    {
        await ConnectToServer();
    }

    private void FixedUpdate()
    {
        // Incrémenter les ticks
        tickAccumulator += Time.fixedDeltaTime;
        while (tickAccumulator >= TICK_RATE)
        {
            CurrentTick++;
            tickAccumulator -= TICK_RATE;
        }
    }

    private void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
        if (websocket != null)
            websocket.DispatchMessageQueue();
        #endif

        // Nettoyer périodiquement (pas chaque frame)
        if (Role == NetworkRole.Client)
        {
            _cleanTimer += Time.deltaTime;
            if (_cleanTimer >= 1f)
            {
                InterpolationBuffer.CleanOldSnapshots(Time.time);
                PredictionSystem.CleanOldInputs(CurrentTick);
                _cleanTimer = 0f;
            }
        }
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

    // ============================================================
    // DÉTERMINATION DU RÔLE
    // ============================================================

    private void DetermineNetworkRole()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        // En WebGL, vérifier l'URL pour les paramètres client
        string url = Application.absoluteURL;
        Debug.Log($"[Network] URL: {url}");

        if (url.Contains("slot="))
        {
            Role = NetworkRole.Client;
            ParseClientParams(url);
            Debug.Log($"[Network] Mode CLIENT - Slot={LocalPlayerSlot}, Name={LocalPlayerName}, Map={RequestedMap}");
        }
        else
        {
            Role = NetworkRole.Host;
            Debug.Log("[Network] Mode HOST");
        }
        #else
        // En éditeur/standalone, toujours host
        Role = NetworkRole.Host;
        Debug.Log("[Network] Mode HOST (Editor/Standalone)");
        #endif
    }

    private void ParseClientParams(string url)
    {
        try
        {
            // Parse: ?slot=3&name=Player&map=italie
            Uri uri = new Uri(url);
            string query = uri.Query;

            if (string.IsNullOrEmpty(query)) return;

            query = query.TrimStart('?');
            string[] pairs = query.Split('&');

            foreach (string pair in pairs)
            {
                string[] kv = pair.Split('=');
                if (kv.Length != 2) continue;

                string key = kv[0].ToLower();
                string value = Uri.UnescapeDataString(kv[1]);

                switch (key)
                {
                    case "slot":
                        int.TryParse(value, out int slot);
                        LocalPlayerSlot = slot;
                        break;
                    case "name":
                        LocalPlayerName = value;
                        break;
                    case "map":
                        RequestedMap = value;
                        break;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Network] Erreur parsing URL: {e.Message}");
        }
    }

    // ============================================================
    // CONNEXION WEBSOCKET
    // ============================================================

    private async System.Threading.Tasks.Task ConnectToServer()
    {
        try
        {
            websocket = new WebSocket(serverUrl);

            websocket.OnOpen += () =>
            {
                Debug.Log("[Network] WebSocket connecté!");
                IsConnected = true;

                if (Role == NetworkRole.Host)
                {
                    RegisterAsHost();
                }
                else if (Role == NetworkRole.Client)
                {
                    RegisterAsPlayer();
                    // Signaler immédiatement qu'on est prêt après l'enregistrement
                    SendPlayerReady();
                }
            };

            websocket.OnError += (e) =>
            {
                Debug.LogError($"[Network] WebSocket erreur: {e}");
            };

            websocket.OnClose += (e) =>
            {
                Debug.Log("[Network] WebSocket fermé");
                IsConnected = false;
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
            Debug.LogError($"[Network] Erreur connexion: {e}");
        }
    }

    // ============================================================
    // TRAITEMENT DES MESSAGES
    // ============================================================

    private void ProcessMessage(string rawMessage)
    {
        try
        {
            string message = rawMessage;

            // Parser le JSON standard
            var baseMsg = JsonUtility.FromJson<NetworkMessageBase>(message);
            string eventName = baseMsg?.type ?? "";
            string data = message;

            // Router vers le bon handler
            switch (eventName)
            {
                case "input":
                    HandleInput(data);
                    break;

                case "playerJoined":
                    HandlePlayerJoined(data);
                    break;

                case "playerLeft":
                    HandlePlayerLeft(data);
                    break;

                case "gameStarted":
                    HandleGameStarted(data);
                    break;

                case "allPlayersReady":
                    OnAllPlayersReady?.Invoke();
                    Debug.Log("[Network] Tous les joueurs sont prêts!");
                    break;

                case "playerReady":
                    HandleRemotePlayerReady(data);
                    break;

                case "gameEnded":
                    HandleGameEnded(data);
                    break;

                case "fullState":
                    HandleFullState(data);
                    break;

                case "delta":
                    HandleDelta(data);
                    break;

                case "event":
                    HandleGameEvent(data);
                    break;

                case "gameStatusUpdate":
                case "lobbyUpdate":
                    // Ces messages sont gérés par le lobby UI
                    Debug.Log($"[Network] {eventName}: {data}");
                    break;

                default:
                    Debug.Log($"[Network] Message inconnu: {eventName}");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Network] Erreur parsing: {e}\nMessage: {rawMessage}");
        }
    }

    [Serializable]
    private class NetworkMessageBase
    {
        public string type;
    }

    private void HandleInput(string data)
    {
        var input = JsonUtility.FromJson<InputMessage>(data);
        if (input != null)
        {
            remoteInputs[input.playerId] = input;
            OnInputReceived?.Invoke(input);
        }
    }

    private void HandleGameStarted(string data)
    {
        var msg = JsonUtility.FromJson<GameStartedMessage>(data);
        if (msg != null)
        {
            HostLocalPlayerCount = msg.localPlayerCount > 0 ? msg.localPlayerCount : 1;
            Debug.Log($"[Network] Game started - Host a {HostLocalPlayerCount} joueur(s) local/locaux");
        }
        OnGameStarted?.Invoke();
    }

    [Serializable]
    private class GameStartedMessage
    {
        public int localPlayerCount;
    }

    private void HandlePlayerJoined(string data)
    {
        var msg = JsonUtility.FromJson<PlayerJoinedMessage>(data);
        if (msg != null)
        {
            OnPlayerJoined?.Invoke(msg.slot, msg.name);
            Debug.Log($"[Network] Joueur rejoint: slot={msg.slot}, name={msg.name}");
        }
    }

    private void HandlePlayerLeft(string data)
    {
        var msg = JsonUtility.FromJson<PlayerLeftMessage>(data);
        if (msg != null)
        {
            OnPlayerLeft?.Invoke(msg.slot);
            remoteInputs.Remove(msg.slot);
            Debug.Log($"[Network] Joueur parti: slot={msg.slot}");
        }
    }

    private void HandleRemotePlayerReady(string data)
    {
        var msg = JsonUtility.FromJson<PlayerReadyMessage>(data);
        if (msg != null)
        {
            OnRemotePlayerReady?.Invoke(msg.slot);
            Debug.Log($"[Network] Joueur distant prêt: slot={msg.slot}");
        }
    }

    private void HandleGameEnded(string data)
    {
        var msg = JsonUtility.FromJson<GameEndedData>(data);
        OnGameEnded?.Invoke(msg?.score ?? 0);
    }

    [Serializable]
    private class GameEndedData
    {
        public int score;
    }

    private void HandleFullState(string data)
    {
        if (Role != NetworkRole.Client) return;

        var state = JsonUtility.FromJson<FullStateMessage>(data);
        if (state == null) return;

        // Synchroniser le tick du client avec le serveur (+ un peu d'avance pour la latence)
        if (state.tick > CurrentTick || CurrentTick - state.tick > 100)
        {
            CurrentTick = state.tick + 3; // 3 ticks d'avance (~100ms à 30 ticks/s)
            Debug.Log($"[Network] Tick synchronisé: {CurrentTick}");
        }

        // Convertir en StateSnapshot
        var snapshot = new StateSnapshot(state.tick, Time.time)
        {
            TimeLeft = state.timeLeft,
            Score = state.score
        };

        foreach (var player in state.players)
        {
            snapshot.Players[player.id] = player;
        }

        foreach (var counter in state.counters)
        {
            snapshot.Counters[counter.id] = counter;
        }

        if (state.orders != null)
        {
            foreach (var order in state.orders)
            {
                snapshot.Orders[order.id] = order;
            }
        }

        if (state.pnjs != null)
        {
            foreach (var pnj in state.pnjs)
            {
                snapshot.PNJs[pnj.id] = pnj;
            }
        }

        // Ajouter au buffer d'interpolation
        InterpolationBuffer.AddSnapshot(snapshot);

        // Notifier pour réconciliation
        OnStateReceived?.Invoke(snapshot);
    }

    private void HandleDelta(string data)
    {
        if (Role != NetworkRole.Client) return;

        var delta = JsonUtility.FromJson<DeltaStateMessage>(data);
        if (delta == null) return;

        // Synchroniser le tick si nécessaire
        if (delta.tick > CurrentTick || CurrentTick - delta.tick > 100)
        {
            CurrentTick = delta.tick + 3;
        }

        // Récupérer le dernier snapshot et le mettre à jour
        var latest = InterpolationBuffer.GetLatestSnapshot();
        var snapshot = latest?.Clone() ?? new StateSnapshot();
        snapshot.Tick = delta.tick;
        snapshot.Timestamp = Time.time;

        if (delta.hasTimeLeft)
            snapshot.TimeLeft = delta.timeLeft;

        if (delta.hasScore)
            snapshot.Score = delta.score;

        if (delta.players != null)
        {
            foreach (var player in delta.players)
            {
                snapshot.Players[player.id] = player;
            }
        }

        if (delta.counters != null)
        {
            foreach (var counter in delta.counters)
            {
                snapshot.Counters[counter.id] = counter;
            }
        }

        if (delta.orders != null)
        {
            snapshot.Orders.Clear();
            foreach (var order in delta.orders)
            {
                snapshot.Orders[order.id] = order;
            }
        }

        if (delta.pnjs != null)
        {
            snapshot.PNJs.Clear();
            foreach (var pnj in delta.pnjs)
            {
                snapshot.PNJs[pnj.id] = pnj;
            }
        }

        InterpolationBuffer.AddSnapshot(snapshot);
        OnStateReceived?.Invoke(snapshot);
    }

    private void HandleGameEvent(string data)
    {
        var evt = JsonUtility.FromJson<GameEventMessage>(data);
        if (evt != null)
        {
            OnGameEvent?.Invoke(evt);
        }
    }

    // ============================================================
    // ENVOI DE MESSAGES (HOST)
    // ============================================================

    public void RegisterAsHost()
    {
        if (!IsConnected || Role != NetworkRole.Host) return;

        SendJSON(new RegisterHostMessage());
        Debug.Log("[Network] Enregistré comme host");
    }

    public void RegisterAsPlayer()
    {
        if (!IsConnected || Role != NetworkRole.Client) return;
        if (LocalPlayerSlot < 0) return;

        var data = new RegisterAsPlayerMessage { slot = LocalPlayerSlot, name = LocalPlayerName };
        SendJSON(data);
        Debug.Log($"[Network] Enregistré comme joueur: slot={LocalPlayerSlot}, name={LocalPlayerName}");
    }

    /// <summary>
    /// Signale au serveur que ce client est prêt à jouer (Client seulement)
    /// </summary>
    public void SendPlayerReady()
    {
        if (!IsConnected || Role != NetworkRole.Client) return;
        if (LocalPlayerSlot < 0) return;

        var data = new PlayerReadyMessage { slot = LocalPlayerSlot };
        SendJSON(data);
        Debug.Log($"[Network] Signalé prêt: slot={LocalPlayerSlot}");
    }

    public void SetupLobby()
    {
        if (!IsConnected || Role != NetworkRole.Host) return;

        SendJSON(new SetupLobbyMessage());
        Debug.Log("[Network] Lobby configuré");
    }

    public void StartGame(int localPlayerCount = 1)
    {
        if (!IsConnected || Role != NetworkRole.Host) return;

        SendJSON(new StartGameMessage { localPlayerCount = localPlayerCount });
        Debug.Log($"[Network] Partie lancée! ({localPlayerCount} joueur(s) local/locaux)");
    }

    public void EndGame(int finalScore)
    {
        if (!IsConnected || Role != NetworkRole.Host) return;

        var data = new EndGameMessage { score = finalScore };
        SendJSON(data);
        Debug.Log($"[Network] Partie terminée: {finalScore}");
    }

    /// <summary>
    /// Envoie l'état complet du jeu (appelé périodiquement par GameManager)
    /// </summary>
    public void SendFullState(FullStateMessage state)
    {
        if (!IsConnected || Role != NetworkRole.Host) return;

        // Limiter la fréquence
        if (Time.time - lastFullStateSent < 1f / fullStateSendRate)
            return;

        state.tick = CurrentTick;
        SendJSON(state);
        lastFullStateSent = Time.time;

        // Stocker pour le delta
        lastSentState = ConvertToSnapshot(state);
    }

    /// <summary>
    /// Envoie les changements depuis le dernier état (delta sync)
    /// </summary>
    public void SendDeltaState(FullStateMessage currentState)
    {
        if (!IsConnected || Role != NetworkRole.Host) return;

        // Limiter la fréquence
        if (Time.time - lastDeltaSent < 1f / deltaSendRate)
            return;

        var delta = new DeltaStateMessage
        {
            tick = CurrentTick
        };

        bool hasChanges = false;

        // Comparer avec le dernier état envoyé
        if (lastSentState != null)
        {
            // Time/Score changes
            if (Mathf.Abs(currentState.timeLeft - lastSentState.TimeLeft) > 0.1f)
            {
                delta.timeLeft = currentState.timeLeft;
                delta.hasTimeLeft = true;
                hasChanges = true;
            }

            if (currentState.score != lastSentState.Score)
            {
                delta.score = currentState.score;
                delta.hasScore = true;
                hasChanges = true;
            }

            // Player changes
            delta.players = new List<PlayerState>();
            foreach (var player in currentState.players)
            {
                if (lastSentState.Players.TryGetValue(player.id, out var lastPlayer))
                {
                    // Vérifier si changement significatif
                    float posDiff = Vector2.Distance(
                        new Vector2(player.x, player.y),
                        new Vector2(lastPlayer.x, lastPlayer.y)
                    );

                    bool directionChanged = player.moveX != lastPlayer.moveX || player.moveY != lastPlayer.moveY
                        || player.lastMoveX != lastPlayer.lastMoveX || player.lastMoveY != lastPlayer.lastMoveY;

                    if (posDiff > 0.01f || player.carry != lastPlayer.carry || player.isFrozen != lastPlayer.isFrozen || directionChanged)
                    {
                        delta.players.Add(player);
                        hasChanges = true;
                    }
                }
                else
                {
                    // Nouveau joueur
                    delta.players.Add(player);
                    hasChanges = true;
                }
            }

            // Counter changes
            delta.counters = new List<CounterState>();
            foreach (var counter in currentState.counters)
            {
                if (lastSentState.Counters.TryGetValue(counter.id, out var lastCounter))
                {
                    if (counter.currentItem != lastCounter.currentItem ||
                        counter.cookingState != lastCounter.cookingState ||
                        Mathf.Abs(counter.cookingProgress - lastCounter.cookingProgress) > 0.01f)
                    {
                        delta.counters.Add(counter);
                        hasChanges = true;
                    }
                }
                else
                {
                    delta.counters.Add(counter);
                    hasChanges = true;
                }
            }

            // Orders - toujours envoyer la liste complète (petite taille, changements fréquents)
            if (currentState.orders != null && currentState.orders.Count > 0)
            {
                delta.orders = currentState.orders;
                hasChanges = true;
            }
            else if (lastSentState.Orders.Count > 0)
            {
                // Les commandes ont toutes été supprimées, envoyer une liste vide
                delta.orders = new List<OrderState>();
                hasChanges = true;
            }

            // PNJ - toujours envoyer la liste complète (positions changent souvent)
            if (currentState.pnjs != null && currentState.pnjs.Count > 0)
            {
                delta.pnjs = currentState.pnjs;
                hasChanges = true;
            }
            else if (lastSentState.PNJs.Count > 0)
            {
                delta.pnjs = new List<PNJState>();
                hasChanges = true;
            }
        }
        else
        {
            // Premier envoi, tout est un changement
            delta.timeLeft = currentState.timeLeft;
            delta.hasTimeLeft = true;
            delta.score = currentState.score;
            delta.hasScore = true;
            delta.players = currentState.players;
            delta.counters = currentState.counters;
            delta.orders = currentState.orders;
            delta.pnjs = currentState.pnjs;
            hasChanges = true;
        }

        if (hasChanges)
        {
            SendJSON(delta);
            lastDeltaSent = Time.time;
            lastSentState = ConvertToSnapshot(currentState);
        }
    }

    /// <summary>
    /// Envoie un événement instantané
    /// </summary>
    public void SendGameEvent(string eventName, object eventData)
    {
        if (!IsConnected || Role != NetworkRole.Host) return;

        var evt = new GameEventMessage(CurrentTick, eventName, eventData);
        SendJSON(evt);
    }

    private StateSnapshot ConvertToSnapshot(FullStateMessage state)
    {
        var snapshot = new StateSnapshot(state.tick, Time.time)
        {
            TimeLeft = state.timeLeft,
            Score = state.score
        };

        foreach (var player in state.players)
        {
            snapshot.Players[player.id] = player;
        }

        foreach (var counter in state.counters)
        {
            snapshot.Counters[counter.id] = counter;
        }

        if (state.orders != null)
        {
            foreach (var order in state.orders)
            {
                snapshot.Orders[order.id] = order;
            }
        }

        if (state.pnjs != null)
        {
            foreach (var pnj in state.pnjs)
            {
                snapshot.PNJs[pnj.id] = pnj;
            }
        }

        return snapshot;
    }

    // ============================================================
    // ENVOI DE MESSAGES (CLIENT)
    // ============================================================

    /// <summary>
    /// Envoie les inputs du joueur local (mode Client)
    /// </summary>
    public void SendInput(Vector2 movement, ActionType action, string targetId = null)
    {
        if (!IsConnected || Role != NetworkRole.Client) return;
        if (LocalPlayerSlot < 0) return;

        var input = new InputMessage(CurrentTick, LocalPlayerSlot, movement.x, movement.y, action, targetId);

        // Stocker pour prediction
        PredictionSystem.AddPendingInput(new InputSnapshot(
            CurrentTick,
            LocalPlayerSlot,
            movement,
            action,
            targetId
        ));

        // Envoyer au serveur
        SendJSON(input);
    }

    // ============================================================
    // RÉCUPÉRATION DES INPUTS DISTANTS (HOST)
    // ============================================================

    public InputMessage GetRemoteInput(int playerId)
    {
        if (remoteInputs.TryGetValue(playerId, out InputMessage input))
        {
            return input;
        }
        return null;
    }

    public void ClearRemoteInput(int playerId)
    {
        remoteInputs.Remove(playerId);
    }

    // ============================================================
    // HELPERS ENVOI
    // ============================================================

    private void SendJSON(object data)
    {
        if (websocket == null || websocket.State != WebSocketState.Open) return;

        string json = JsonUtility.ToJson(data);
        websocket.SendText(json);
    }

// ============================================================
    // INTERPOLATION HELPERS (Client)
    // ============================================================

    /// <summary>
    /// Retourne la position interpolée d'un joueur distant
    /// </summary>
    public Vector2? GetInterpolatedPosition(int playerId)
    {
        if (Role != NetworkRole.Client) return null;
        return InterpolationBuffer.GetInterpolatedPosition(playerId, Time.time);
    }

    /// <summary>
    /// Retourne l'état interpolé d'un counter
    /// </summary>
    public CounterState GetInterpolatedCounterState(string counterId)
    {
        if (Role != NetworkRole.Client) return null;
        return InterpolationBuffer.GetInterpolatedCounterState(counterId, Time.time);
    }

    /// <summary>
    /// Vérifie si un joueur est le joueur local (pour ce client)
    /// </summary>
    public bool IsLocalPlayer(int playerId)
    {
        if (Role == NetworkRole.Host)
        {
            return playerId == 1 || playerId == 2; // Slots locaux sur la borne
        }
        else
        {
            return playerId == LocalPlayerSlot;
        }
    }
}
