using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CookMoiCa.Network;

public enum GameState
{
    Waiting,
    Ready,
    Loading,  // En attente que tous les joueurs soient prêts
    Playing,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public RecipeData[] recipes;
    public static GameManager Instance;

    [Header("Game Settings")]
    [SerializeField] private float gameTime = 120f;
    [SerializeField] private int maxPlayers = 4;

    [Header("Player Spawning")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints = new Transform[4];

    [Header("Countdown")]
    [SerializeField] private float countdownStepDuration = 0.5f;

    [Header("Game State")]
    private GameState currentState = GameState.Waiting;
    private float timeLeft;
    private int score = 0;
    private Dictionary<int, GameObject> activePlayers = new Dictionary<int, GameObject>();
    private Dictionary<int, PlayerController> playerControllers = new Dictionary<int, PlayerController>();
    private GameObject gameUI;
    [SerializeField] private GameObject highscoreUI;
    [SerializeField] private GamePanelUI gamePanelUI;

    [Header("Network")]
    private float lastDeltaSent = 0f;
    private float lastFullStateSent = 0f;

    // Référence aux counters pour la synchronisation
    private Counter[] allCounters;

    // PNJ réseau : mapping networkId → PNJClient (côté client, les PNJ spawned via events)
    private Dictionary<string, PNJClient> networkPNJs = new Dictionary<string, PNJClient>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Permet au jeu de tourner même quand la fenêtre n'a pas le focus
        // Important pour le multijoueur sur le même PC
        Application.runInBackground = true;

        // Collecter tous les counters de la scène
        allCounters = FindObjectsByType<Counter>(FindObjectsSortMode.None);
    }

    private void Start()
    {
        gameUI = GameObject.FindWithTag("GameUI");
        if (gameUI != null)
            gameUI.SetActive(false);

        // Vérifier si on est en mode Client
        if (NetworkManager.Instance != null && NetworkManager.Instance.Role == NetworkRole.Client)
        {
            // Mode Client: démarrer automatiquement
            StartAsClient();
        }
        else
        {
            // Mode Host: attendre le menu
            StartCoroutine(Anatidae.HighscoreManager.FetchHighscores());
        }
    }

    /// <summary>
    /// Démarrage en mode Client (navigateur web)
    /// </summary>
    private void StartAsClient()
    {
        Debug.Log("[GameManager] Démarrage en mode CLIENT");

        // Cacher le menu principal
        GameObject mainMenu = GameObject.FindWithTag("MainMenu");
        if (mainMenu != null)
            mainMenu.SetActive(false);

        if (gameUI != null)
            gameUI.SetActive(true);

        // Se désabonner d'abord pour éviter les doublons
        NetworkManager.Instance.OnPlayerJoined -= OnPlayerJoined;
        NetworkManager.Instance.OnPlayerLeft -= OnPlayerLeft;
        NetworkManager.Instance.OnStateReceived -= OnServerStateReceived;
        NetworkManager.Instance.OnGameEvent -= OnGameEvent;
        NetworkManager.Instance.OnAllPlayersReady -= OnAllPlayersReady;

        // S'abonner aux événements réseau
        NetworkManager.Instance.OnPlayerJoined += OnPlayerJoined;
        NetworkManager.Instance.OnPlayerLeft += OnPlayerLeft;
        NetworkManager.Instance.OnStateReceived += OnServerStateReceived;
        NetworkManager.Instance.OnGameEvent += OnGameEvent;
        NetworkManager.Instance.OnAllPlayersReady += OnAllPlayersReady;

        // Commencer en mode Loading (attente de tous les joueurs)
        // Les joueurs seront spawnés dans OnAllPlayersReady
        currentState = GameState.Loading;
        timeLeft = gameTime;
        score = 0;

        Debug.Log($"[GameManager] Client initialisé - En attente de allPlayersReady");
    }

    /// <summary>
    /// Callback quand tous les joueurs sont prêts
    /// </summary>
    private void OnAllPlayersReady()
    {
        Debug.Log("[GameManager] Tous les joueurs sont prêts - Spawn et countdown!");

        if (currentState == GameState.Loading || currentState == GameState.Ready)
        {
            if (NetworkManager.Instance?.Role == NetworkRole.Host)
            {
                // Host : spawner les joueurs locaux + distants
                InitializeGame(pendingLocalPlayerCount);

                foreach (var kvp in pendingRemotePlayers)
                {
                    if (!activePlayers.ContainsKey(kvp.Key))
                    {
                        SpawnPlayer(kvp.Key, false);
                    }
                }
                pendingRemotePlayers.Clear();
            }
            else if (NetworkManager.Instance?.Role == NetworkRole.Client)
            {
                // Client : spawner les joueurs locaux du host selon le nombre reçu
                int hostLocalCount = NetworkManager.Instance.HostLocalPlayerCount;
                SpawnPlayer(1, false);
                if (hostLocalCount >= 2)
                {
                    SpawnPlayer(2, false);
                }

                // Spawner les joueurs distants connus (autres que nous)
                foreach (var kvp in pendingRemotePlayers)
                {
                    int slot = kvp.Key;
                    if (slot != NetworkManager.Instance.LocalPlayerSlot && !activePlayers.ContainsKey(slot))
                    {
                        SpawnPlayer(slot, false);
                    }
                }

                // Spawner le joueur local
                int localSlot = NetworkManager.Instance.LocalPlayerSlot;
                if (localSlot >= 3 && localSlot <= 4 && !activePlayers.ContainsKey(localSlot))
                {
                    SpawnPlayer(localSlot, true);
                }
                pendingRemotePlayers.Clear();
            }

            StartCoroutine(CountdownCoroutine());
        }
    }

    private IEnumerator CountdownCoroutine()
    {
        // Freeze tous les joueurs et préparer le drop
        // La chute dure 2 steps (3 → 2 → atterrit au début du "1")
        float fallDuration = countdownStepDuration * 2f;

        foreach (var kvp in activePlayers)
        {
            GameObject playerObj = kvp.Value;
            if (playerObj == null) continue;

            PlayerMovement movement = playerObj.GetComponent<PlayerMovement>();
            if (movement != null)
                movement.Freeze();

            PlayerSpawnEffect spawnEffect = playerObj.GetComponent<PlayerSpawnEffect>();
            if (spawnEffect == null)
                spawnEffect = playerObj.AddComponent<PlayerSpawnEffect>();

            spawnEffect.PlayDropEffect(0f, fallDuration);
        }

        // 3
        UIManager.Instance?.ShowCountdown("3");
        yield return new WaitForSeconds(countdownStepDuration);

        // 2
        UIManager.Instance?.ShowCountdown("2");
        yield return new WaitForSeconds(countdownStepDuration);

        // 1 (les joueurs atterrissent ici)
        UIManager.Instance?.ShowCountdown("1");
        yield return new WaitForSeconds(countdownStepDuration);

        // GO!
        UIManager.Instance?.ShowCountdown("GO!");
        yield return new WaitForSeconds(0.4f);
        UIManager.Instance?.HideCountdown();

        // Unfreeze tous les joueurs
        foreach (var kvp in activePlayers)
        {
            GameObject playerObj = kvp.Value;
            if (playerObj == null) continue;

            PlayerMovement movement = playerObj.GetComponent<PlayerMovement>();
            if (movement != null)
                movement.Unfreeze();
        }

        // Démarrer la partie
        currentState = GameState.Playing;

        // Démarrer le spawn des PNJ
        if (PNJSpawner.Instance != null)
        {
            PNJSpawner.Instance.StartSpawning();
        }

        Debug.Log("[GameManager] Countdown terminé - Partie lancée!");
    }

    public void StartMenu()
    {
        // Mode Host seulement
        if (NetworkManager.Instance != null && NetworkManager.Instance.Role == NetworkRole.Client)
        {
            Debug.LogWarning("[GameManager] StartMenu appelé en mode Client, ignoré");
            return;
        }

        // Cacher le menu principal et afficher le lobby
        GameObject mainMenu = GameObject.FindWithTag("MainMenu");
        if (mainMenu != null)
            mainMenu.SetActive(false);

        if (gameUI != null)
            gameUI.SetActive(true);

        // Afficher la waiting room avec les slots
        LobbyUI lobby = LobbyUI.Instance ?? FindObjectOfType<LobbyUI>(true);
        Debug.Log($"[GameManager] StartMenu - gameUI active: {gameUI?.activeSelf}, LobbyUI found: {lobby != null}");
        if (lobby != null)
        {
            lobby.EnterLobby();
        }
        else
        {
            Debug.LogError("[GameManager] LobbyUI introuvable!");
        }

        // S'abonner aux événements réseau dès le lobby pour capter les joueurs qui rejoignent
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnPlayerJoined -= OnPlayerJoined;
            NetworkManager.Instance.OnPlayerLeft -= OnPlayerLeft;
            NetworkManager.Instance.OnPlayerJoined += OnPlayerJoined;
            NetworkManager.Instance.OnPlayerLeft += OnPlayerLeft;
        }

        // Ouvrir le lobby réseau pour que les joueurs distants puissent rejoindre
        SetupLobby();
    }

    /// <summary>
    /// Appelé quand le joueur valide dans le lobby pour lancer la partie
    /// </summary>
    private int pendingLocalPlayerCount = 1;

    public void LaunchGame(int localPlayerCount)
    {
        pendingLocalPlayerCount = localPlayerCount;

        // Se désabonner d'abord pour éviter les doublons (ex: restart)
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnPlayerJoined -= OnPlayerJoined;
            NetworkManager.Instance.OnPlayerLeft -= OnPlayerLeft;
            NetworkManager.Instance.OnGameStarted -= StartGame;
            NetworkManager.Instance.OnAllPlayersReady -= OnAllPlayersReady;
            NetworkManager.Instance.OnRemotePlayerReady -= OnRemotePlayerReady;

            // S'abonner aux événements réseau
            NetworkManager.Instance.OnPlayerJoined += OnPlayerJoined;
            NetworkManager.Instance.OnPlayerLeft += OnPlayerLeft;
            NetworkManager.Instance.OnGameStarted += StartGame;
            NetworkManager.Instance.OnAllPlayersReady += OnAllPlayersReady;
            NetworkManager.Instance.OnRemotePlayerReady += OnRemotePlayerReady;
        }

        StartGame();
    }

    /// <summary>
    /// Callback quand un joueur distant signale qu'il est prêt (Host seulement)
    /// </summary>
    private void OnRemotePlayerReady(int slot)
    {
        Debug.Log($"[GameManager] Joueur distant {slot} est prêt");
        // Optionnel: afficher un indicateur visuel
    }

    private void InitializeGame(int localPlayerCount)
    {
        // Host: Spawner les joueurs locaux selon le nombre dans le lobby
        SpawnPlayer(1, true);
        if (localPlayerCount >= 2)
        {
            SpawnPlayer(2, true);
        }
    }

    public void SetupLobby()
    {
        if (NetworkManager.Instance?.Role != NetworkRole.Host) return;

        currentState = GameState.Ready;
        NetworkManager.Instance?.SetupLobby();
        Debug.Log("Lobby configuré, en attente des joueurs distants...");
    }

    public void StartGame()
    {
        if (currentState != GameState.Ready) return;
        if (NetworkManager.Instance?.Role != NetworkRole.Host) return;

        // Passer en mode Loading (attente des joueurs distants)
        currentState = GameState.Loading;
        timeLeft = gameTime;
        score = 0;

        // Envoyer startGame au serveur avec le nombre de joueurs locaux
        NetworkManager.Instance?.StartGame(pendingLocalPlayerCount);
        Debug.Log("Partie en cours de lancement - Attente des joueurs distants...");

        // Note: Le spawn des PNJ sera déclenché par OnAllPlayersReady
    }

    private void SpawnPlayer(int playerId, bool isLocal)
    {
        if (activePlayers.ContainsKey(playerId))
        {
            Debug.LogWarning($"Le joueur {playerId} existe déjà!");
            return;
        }

        GameObject newPlayer;

        if (spawnPoints.Length < playerId || spawnPoints[playerId - 1] == null)
        {
            Debug.LogError($"Spawn point manquant pour le joueur {playerId}!");
            Vector3 fallbackPos = new Vector3((playerId - 1) * 2, 0, 0);
            newPlayer = Instantiate(playerPrefab, fallbackPos, Quaternion.identity);
        }
        else
        {
            Transform spawnPoint = spawnPoints[playerId - 1];
            newPlayer = Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);
        }

        ConfigurePlayer(newPlayer, playerId, isLocal);
    }

    private void ConfigurePlayer(GameObject player, int playerId, bool isLocal)
    {
        player.name = $"Player_{playerId}";
        activePlayers[playerId] = player;

        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller == null)
        {
            controller = player.AddComponent<PlayerController>();
        }

        controller.playerId = playerId;
        controller.isLocalPlayer = isLocal;
        playerControllers[playerId] = controller;

        if (isLocal)
        {
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                movement.horizontalAxis = $"P{playerId}_Horizontal";
                movement.verticalAxis = $"P{playerId}_Vertical";
            }
        }

        Debug.Log($"Joueur {playerId} spawné (Local: {isLocal})");

        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.ActivatePlayerSlot(playerId);
        }
    }

    // Joueurs distants en attente de spawn (rejoints pendant le lobby)
    private Dictionary<int, string> pendingRemotePlayers = new Dictionary<int, string>();

    private void OnPlayerJoined(int slot, string playerName)
    {
        if (slot >= 3 && slot <= 4)
        {
            // Si la partie n'a pas encore commencé, stocker pour spawn plus tard
            if (currentState == GameState.Ready || currentState == GameState.Loading)
            {
                pendingRemotePlayers[slot] = playerName;
                Debug.Log($"{playerName} a rejoint en slot {slot} (en attente de spawn)");
                return;
            }

            // Partie déjà en cours, spawner directement
            bool isLocal = NetworkManager.Instance?.Role == NetworkRole.Client &&
                          NetworkManager.Instance.LocalPlayerSlot == slot;

            if (!activePlayers.ContainsKey(slot))
            {
                SpawnPlayer(slot, isLocal);
            }
            else if (isLocal)
            {
                playerControllers[slot].isLocalPlayer = true;
            }

            Debug.Log($"{playerName} a rejoint en slot {slot}");
        }
    }

    private void OnPlayerLeft(int slot)
    {
        if (activePlayers.TryGetValue(slot, out GameObject player))
        {
            if (InventoryUI.Instance != null)
            {
                InventoryUI.Instance.DeactivatePlayerSlot(slot);
            }

            Destroy(player);
            activePlayers.Remove(slot);
            playerControllers.Remove(slot);
            Debug.Log($"Joueur du slot {slot} déconnecté");
        }
    }

    /// <summary>
    /// Callback quand le client reçoit l'état du serveur
    /// </summary>
    private void OnServerStateReceived(StateSnapshot serverState)
    {
        if (NetworkManager.Instance?.Role != NetworkRole.Client) return;

        // Mettre à jour le timer et score depuis le serveur
        timeLeft = serverState.TimeLeft;
        score = serverState.Score;

        // Mettre à jour l'UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetScore(score);
            UIManager.Instance.SetTimer(timeLeft);
        }

        int localSlot = NetworkManager.Instance.LocalPlayerSlot;

        // Mettre à jour les joueurs distants (carry/inventory)
        foreach (var kvp in serverState.Players)
        {
            int playerId = kvp.Key;
            PlayerState playerState = kvp.Value;

            // Ne pas mettre à jour le joueur local via le réseau (on utilise la prédiction)
            if (playerId == localSlot) continue;

            // Mettre à jour l'inventaire des joueurs distants
            if (activePlayers.TryGetValue(playerId, out GameObject playerObj))
            {
                var inventory = playerObj.GetComponent<InventorySystem>();
                if (inventory != null)
                {
                    string currentCarry = inventory.currentItem?.name;
                    if (currentCarry != playerState.carry)
                    {
                        inventory.SetItemByName(playerState.carry);

                        // Mettre à jour l'UI d'inventaire
                        if (InventoryUI.Instance != null)
                        {
                            InventoryUI.Instance.UpdatePlayerInventory(playerId, inventory);
                        }
                    }
                }
            }
        }

        // Mettre à jour les counters
        foreach (var kvp in serverState.Counters)
        {
            string counterId = kvp.Key;
            CounterState counterState = kvp.Value;

            Counter counter = Counter.GetCounterById(counterId);
            if (counter != null)
            {
                counter.ApplyNetworkState(counterState);
            }
        }

        // Synchroniser les commandes depuis le serveur (même si la liste est vide, pour nettoyer les orders expirées)
        if (serverState.Orders != null && OrderManager.Instance != null)
        {
            OrderManager.Instance.ApplyNetworkOrders(serverState.Orders);
        }

        // PNJ synchronisés via événements (pnjSpawn, pnjOrder, pnjServed, pnjExpired)
        // Plus besoin de sync position ici

        // Vérifier la désynchronisation pour le joueur local
        if (activePlayers.TryGetValue(localSlot, out GameObject localPlayer))
        {
            ReconcileLocalPlayer(serverState, localSlot, localPlayer);
        }
    }

    /// <summary>
    /// Réconciliation intelligente : compare la position locale avec la position serveur
    /// APRÈS replay des inputs pendants (évite les faux positifs dus à la latence)
    /// </summary>
    private const float TICK_RATE = 1f / 30f;
    private const float RECONCILIATION_THRESHOLD = 0.5f;

    private void ReconcileLocalPlayer(StateSnapshot serverState, int localPlayerId, GameObject player)
    {
        if (!serverState.Players.TryGetValue(localPlayerId, out PlayerState serverPlayer))
            return;

        var prediction = NetworkManager.Instance.PredictionSystem;

        // Mettre à jour le tick confirmé et supprimer les inputs confirmés
        prediction.ConfirmTick(serverState.Tick);

        Vector2 serverPos = new Vector2(serverPlayer.x, serverPlayer.y);
        Vector2 localPos = player.transform.position;

        // Récupérer la vitesse du joueur
        var movement = player.GetComponent<PlayerMovement>();
        float moveSpeed = movement != null ? movement.moveSpeed : 5f;

        // Rejouer les inputs non confirmés depuis la position serveur
        // C'est la position où le client DEVRAIT être si tout est synchronisé
        var inputsToReplay = prediction.GetInputsToReplay();
        Vector2 expectedPos = RollbackHelper.ReplayMovementInputs(
            serverPos, inputsToReplay, moveSpeed, TICK_RATE
        );

        // Comparer la position locale avec la position attendue (pas la position serveur brute)
        float desyncDist = Vector2.Distance(localPos, expectedPos);

        if (desyncDist > RECONCILIATION_THRESHOLD)
        {
            // Vraie desync détectée après compensation de la latence
            if (desyncDist > 3f)
            {
                // Trop loin → snap direct
                player.transform.position = (Vector3)expectedPos;
            }
            else
            {
                // Correction douce
                player.transform.position = Vector2.Lerp(localPos, expectedPos, 0.3f);
            }
        }

        // Synchroniser l'inventaire
        string localCarry = GetPlayerCarry(localPlayerId);
        string normalizedLocal = string.IsNullOrEmpty(localCarry) ? null : localCarry;
        string normalizedServer = string.IsNullOrEmpty(serverPlayer.carry) ? null : serverPlayer.carry;
        if (normalizedLocal != normalizedServer)
        {
            var inventory = player.GetComponent<InventorySystem>();
            if (inventory != null)
            {
                inventory.SetItemByName(serverPlayer.carry);
                if (InventoryUI.Instance != null)
                {
                    InventoryUI.Instance.UpdatePlayerInventory(localPlayerId, inventory);
                }
            }
        }
    }

    private void OnGameEvent(GameEventMessage evt)
    {
        switch (evt.eventName)
        {
            case "pnjSpawn":
                HandlePNJSpawn(evt.data);
                break;
            case "pnjOrder":
                HandlePNJOrder(evt.data);
                break;
            case "pnjServed":
                HandlePNJServed(evt.data);
                break;
            case "pnjExpired":
                HandlePNJExpired(evt.data);
                break;
        }
    }

    // ============================================================
    // PNJ EVENTS - Système événementiel côté client
    // ============================================================

    private void HandlePNJSpawn(string data)
    {
        if (PNJSpawner.Instance == null) return;

        var evt = JsonUtility.FromJson<CookMoiCa.Network.PNJSpawnEvent>(data);
        if (evt == null || string.IsNullOrEmpty(evt.networkId)) return;

        // Ne pas respawner un PNJ déjà existant
        if (networkPNJs.ContainsKey(evt.networkId) && networkPNJs[evt.networkId] != null)
            return;

        var spawner = PNJSpawner.Instance;
        if (spawner.PnjPrefab == null || spawner.CheminPoints == null) return;

        // Instancier un vrai PNJ avec le comportement complet
        GameObject newPNJ = Instantiate(spawner.PnjPrefab, spawner.SpawnPosition, Quaternion.identity);
        PNJClient client = newPNJ.GetComponent<PNJClient>();

        if (client != null)
        {
            // Configurer comme le host le fait
            client.chemin = spawner.CheminPoints;
            client.positionIndex = evt.positionIndex;
            client.offsetEntreClients = spawner.OffsetEntreClients;
            client.directionAlignement = spawner.DirectionAlignement;
            client.directionAttente = spawner.DirectionAttenteClients;
            client.networkId = evt.networkId;

            networkPNJs[evt.networkId] = client;
        }
        else
        {
            Destroy(newPNJ);
        }
    }

    private void HandlePNJOrder(string data)
    {
        var evt = JsonUtility.FromJson<CookMoiCa.Network.PNJOrderEvent>(data);
        if (evt == null || string.IsNullOrEmpty(evt.networkId)) return;

        if (networkPNJs.TryGetValue(evt.networkId, out PNJClient pnj) && pnj != null)
        {
            pnj.ApplyNetworkOrder(evt.recipeName);
        }
    }

    private void HandlePNJServed(string data)
    {
        var evt = JsonUtility.FromJson<CookMoiCa.Network.PNJServedEvent>(data);
        if (evt == null || string.IsNullOrEmpty(evt.networkId)) return;

        if (networkPNJs.TryGetValue(evt.networkId, out PNJClient pnj) && pnj != null)
        {
            pnj.ApplyNetworkServed();
        }
    }

    private void HandlePNJExpired(string data)
    {
        var evt = JsonUtility.FromJson<CookMoiCa.Network.PNJExpiredEvent>(data);
        if (evt == null || string.IsNullOrEmpty(evt.networkId)) return;

        if (networkPNJs.TryGetValue(evt.networkId, out PNJClient pnj) && pnj != null)
        {
            pnj.ApplyNetworkExpired();
        }
    }

    private string GetPlayerCarry(int playerId)
    {
        if (playerControllers.TryGetValue(playerId, out PlayerController controller))
        {
            var inventory = controller.GetComponent<InventorySystem>();
            if (inventory != null && inventory.currentItem != null)
            {
                return inventory.currentItem.name;
            }
        }
        return null;
    }

    private void Update()
    {
        if (currentState == GameState.Playing)
        {
            // Host: décrémenter le timer
            if (NetworkManager.Instance?.Role == NetworkRole.Host)
            {
                timeLeft -= Time.deltaTime;
                if (timeLeft <= 0)
                {
                    EndGame();
                }

                // Envoyer l'état aux clients
                SendGameStateToClients();
            }
            // Client: le timer est synchronisé via OnServerStateReceived
        }
    }

    /// <summary>
    /// Envoie l'état du jeu aux clients (Host seulement)
    /// </summary>
    private void SendGameStateToClients()
    {
        if (NetworkManager.Instance == null) return;
        if (NetworkManager.Instance.Role != NetworkRole.Host) return;

        var state = BuildCurrentState();

        // Envoyer delta fréquemment, full state moins souvent
        NetworkManager.Instance.SendDeltaState(state);
        NetworkManager.Instance.SendFullState(state);
    }

    /// <summary>
    /// Construit l'état complet actuel du jeu
    /// </summary>
    private FullStateMessage BuildCurrentState()
    {
        var state = new FullStateMessage
        {
            timeLeft = timeLeft,
            score = score,
            gameState = GetGameStateString()
        };

        // Ajouter les joueurs
        foreach (var kvp in activePlayers)
        {
            int playerId = kvp.Key;
            GameObject playerObj = kvp.Value;

            if (playerObj == null) continue;

            var movement = playerObj.GetComponent<PlayerMovement>();
            var inventory = playerObj.GetComponent<InventorySystem>();

            state.players.Add(new PlayerState
            {
                id = playerId,
                x = playerObj.transform.position.x,
                y = playerObj.transform.position.y,
                carry = inventory?.currentItem?.name,
                isFrozen = movement != null && movement.isFrozen
            });
        }

        // Ajouter les counters
        if (allCounters != null)
        {
            foreach (var counter in allCounters)
            {
                if (counter == null) continue;

                state.counters.Add(new CounterState
                {
                    id = counter.NetworkId,
                    type = counter.GetCounterType(),
                    currentItem = counter.GetCurrentItemName(),
                    cookingState = counter.GetCookingState(),
                    cookingProgress = counter.GetCookingProgress(),
                    lockedBy = counter.GetLockedByPlayer() ?? -1
                });
            }
        }

        // Ajouter les commandes
        if (OrderManager.Instance != null)
        {
            var orders = OrderManager.Instance.GetActiveOrders();
            foreach (var order in orders)
            {
                state.orders.Add(new OrderState
                {
                    id = order.id,
                    recipeName = order.recipeName,
                    timeRemaining = order.timeRemaining,
                    status = order.status
                });
            }
        }

        // PNJ synchronisés via événements (pnjSpawn/pnjOrder/pnjServed/pnjExpired)
        // Plus besoin de les inclure dans le state chaque frame

        return state;
    }

    // Les anciennes méthodes ApplyNetworkPNJs et FindOrderSpriteRobust ont été remplacées
    // par le système événementiel (HandlePNJSpawn/Order/Served/Expired) ci-dessus

    public void EndGame()
    {
        currentState = GameState.GameOver;

        if (PNJSpawner.Instance != null)
        {
            PNJSpawner.Instance.StopSpawning();
            PNJSpawner.Instance.DestroyAllPNJ();
        }

        if (OrderManager.Instance != null)
        {
            OrderManager.Instance.ClearAllOrders();
        }

        // Nettoyer les PNJ réseau (côté client)
        foreach (var kvp in networkPNJs)
        {
            if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        }
        networkPNJs.Clear();

        DisableAllPlayerControls();

        // Seul le host envoie endGame
        if (NetworkManager.Instance?.Role == NetworkRole.Host)
        {
            NetworkManager.Instance?.EndGame(score);
        }

        if (Anatidae.HighscoreManager.HasFetchedHighscores)
        {
            if (Anatidae.HighscoreManager.IsHighscore(score))
            {
                Anatidae.HighscoreManager.ShowHighscoreInput(score);
            }
            else
            {
                ShowRestartPanelAfterHighscore();
            }
        }
        else
        {
            StartCoroutine(CheckHighscoreAfterFetch());
        }
    }

    private IEnumerator CheckHighscoreAfterFetch()
    {
        yield return StartCoroutine(Anatidae.HighscoreManager.FetchHighscores());

        if (Anatidae.HighscoreManager.IsHighscore(score))
        {
            Anatidae.HighscoreManager.ShowHighscoreInput(score);
        }
        else
        {
            ShowRestartPanelAfterHighscore();
        }
    }

    public void ShowRestartPanelAfterHighscore()
    {
        if (gamePanelUI != null)
        {
            gamePanelUI.ShowRestartPanel(score);
        }
    }

    private void DisableAllPlayerControls()
    {
        foreach (var kvp in activePlayers)
        {
            GameObject playerObj = kvp.Value;
            if (playerObj != null)
            {
                PlayerMovement movement = playerObj.GetComponent<PlayerMovement>();
                if (movement != null)
                    movement.enabled = false;

                PlayerInteraction interaction = playerObj.GetComponent<PlayerInteraction>();
                if (interaction != null)
                    interaction.enabled = false;

                Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.linearVelocity = Vector2.zero;
            }
        }

        Debug.Log("Contrôles de tous les joueurs désactivés!");
    }

    public void AddScore(int points)
    {
        int multiplier = EffectManager.Instance != null ? EffectManager.Instance.ScoreMultiplier : 1;
        int totalPoints = points * multiplier;
        score += totalPoints;
        UIManager.Instance?.UpdateScore(totalPoints);

        // Envoyer l'événement aux clients
        if (NetworkManager.Instance?.Role == NetworkRole.Host)
        {
            NetworkManager.Instance.SendGameEvent("scoreUpdated", new ScoreUpdatedEvent { points = points, total = score });
        }
    }

    public PlayerController GetPlayerController(int playerId)
    {
        playerControllers.TryGetValue(playerId, out PlayerController controller);
        return controller;
    }

    public GameObject GetPlayerObject(int playerId)
    {
        activePlayers.TryGetValue(playerId, out GameObject player);
        return player;
    }

    public GameState GetCurrentState() => currentState;

    private string GetGameStateString()
    {
        switch (currentState)
        {
            case GameState.Waiting: return "waiting";
            case GameState.Ready: return "ready";
            case GameState.Loading: return "loading";
            case GameState.Playing: return "playing";
            case GameState.GameOver: return "gameover";
            default: return "waiting";
        }
    }
    public float GetTimeLeft() => timeLeft;
    public int GetScore() => score;


    public void RestartGame()
    {
        Debug.Log("Redémarrage de la partie...");

        currentState = GameState.Waiting;
        score = 0;
        timeLeft = gameTime;

        foreach (var player in activePlayers.Values)
        {
            if (player != null)
                Destroy(player);
        }
        activePlayers.Clear();
        playerControllers.Clear();

        if (PNJSpawner.Instance != null)
        {
            PNJSpawner.Instance.StopSpawning();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        // Se désabonner des événements réseau
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnPlayerJoined -= OnPlayerJoined;
            NetworkManager.Instance.OnPlayerLeft -= OnPlayerLeft;
            NetworkManager.Instance.OnGameStarted -= StartGame;
            NetworkManager.Instance.OnStateReceived -= OnServerStateReceived;
            NetworkManager.Instance.OnGameEvent -= OnGameEvent;
            NetworkManager.Instance.OnAllPlayersReady -= OnAllPlayersReady;
            NetworkManager.Instance.OnRemotePlayerReady -= OnRemotePlayerReady;
        }
    }
}
