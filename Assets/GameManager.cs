using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    Waiting,
    Ready,
    Playing,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public RecipeData[] recipes;
    public static GameManager Instance;

    [Header("Game Settings")]
    [SerializeField] private string mapName = "italie";
    [SerializeField] private float gameTime = 120f; // Temps de jeu en secondes
    [SerializeField] private int maxPlayers = 4;

    [Header("Player Spawning")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints = new Transform[4];

    [Header("Game State")]
    private GameState currentState = GameState.Waiting;
    private float timeLeft;
    private int score = 0;
    private Dictionary<int, GameObject> activePlayers = new Dictionary<int, GameObject>();
    private Dictionary<int, PlayerController> playerControllers = new Dictionary<int, PlayerController>();
    private GameObject gameUI;
    [SerializeField] private GameObject highscoreUI;


    [Header("Network")]
    private float lastStateSent = 0f;
    private float stateSendRate = 0.033f; // 30 FPS

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        gameUI = GameObject.FindWithTag("GameUI");
        gameUI.SetActive(false);
    }

    public void StartMenu()
    {
        InitializeGame();

        // S'abonner aux événements réseau
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnPlayerJoined += OnPlayerJoined;
            NetworkManager.Instance.OnPlayerLeft += OnPlayerLeft;
            NetworkManager.Instance.OnGameStarted += StartGame;
        }

        GameObject mainMenu = GameObject.FindWithTag("MainMenu");
        mainMenu.SetActive(false);

        gameUI.SetActive(true);
    }

    private void InitializeGame()
    {
        // Spawner les joueurs locaux (slots 1-2) au démarrage
        SpawnPlayer(1, true);
        SpawnPlayer(2, true);

        // Ne plus auto-setup le lobby, attendre l'UI
        // SetupLobby();
    }

    public void SetupLobby()
    {
        currentState = GameState.Ready;
        NetworkManager.Instance?.SetupLobby(mapName);
        Debug.Log("Lobby configuré, en attente des joueurs distants...");
    }

    public void StartGame()
    {
        if (currentState != GameState.Ready) return;

        currentState = GameState.Playing;
        timeLeft = gameTime;
        score = 0;

        StartCoroutine(OrderManager.Instance.OrderRoutine());

        NetworkManager.Instance?.StartGame(mapName);
        Debug.Log("Partie démarrée!");
    }

    private void SpawnPlayer(int playerId, bool isLocal)
    {
        if (activePlayers.ContainsKey(playerId))
        {
            Debug.LogWarning($"Le joueur {playerId} existe déjà!");
            return;
        }

        GameObject newPlayer;

        // Vérifier qu'on a un spawn point
        if (spawnPoints.Length < playerId || spawnPoints[playerId - 1] == null)
        {
            Debug.LogError($"Spawn point manquant pour le joueur {playerId}! Création à l'origine.");
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

        // Configurer le PlayerController
        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller == null)
        {
            controller = player.AddComponent<PlayerController>();
        }

        controller.playerId = playerId;
        controller.isLocalPlayer = isLocal;
        playerControllers[playerId] = controller;

        // Configurer PlayerMovement avec les bons axes pour les joueurs locaux
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

        // Activer le slot d'inventaire UI pour ce joueur
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.ActivatePlayerSlot(playerId);
        }
    }

    private void OnPlayerJoined(int slot, string playerName)
    {
        if (slot >= 3 && slot <= 4)
        {
            SpawnPlayer(slot, false);
            Debug.Log($"{playerName} a rejoint en slot {slot}");
        }
    }

    private void OnPlayerLeft(int slot)
    {
        if (activePlayers.TryGetValue(slot, out GameObject player))
        {
            // Désactiver le slot d'inventaire UI avant de détruire le joueur
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

    private void Update()
    {
        if (currentState == GameState.Playing)
        {
            // Mise à jour du timer
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0)
            {
                EndGame();
            }

            // Envoyer l'état du jeu aux clients
            if (Time.time - lastStateSent > stateSendRate)
            {
                SendGameState();
                lastStateSent = Time.time;
            }
        }
    }

    private void SendGameState()
    {
        if (NetworkManager.Instance == null) return;

        var gameState = new GameStateMessage
        {
            eventType = "gameState",
            timeLeft = timeLeft,
            score = score,
            map = mapName,
            players = new List<PlayerStateData>(),
            objects = new List<ObjectData>(), // À implémenter plus tard
            stations = new List<StationData>(), // À implémenter plus tard
            orders = new List<OrderData>() // À implémenter plus tard
        };

        // Ajouter les données des joueurs
        foreach (var kvp in activePlayers)
        {
            int playerId = kvp.Key;
            GameObject playerObj = kvp.Value;

            var playerData = new PlayerStateData
            {
                id = playerId,
                x = playerObj.transform.position.x,
                y = playerObj.transform.position.y,
                carry = null, // À implémenter avec l'inventaire
                action = "idle" // À implémenter avec les actions
            };

            gameState.players.Add(playerData);
        }

        NetworkManager.Instance.SendGameState(gameState);
    }

    public void EndGame()
    {
        currentState = GameState.GameOver;
        NetworkManager.Instance?.EndGame(score);
        if (Anatidae.HighscoreManager.IsHighscore(score))
        {
            Anatidae.HighscoreManager.ShowHighscoreInput(score);
        }
    }

    public void AddScore(int points)
    {
        score += points;
        UIManager.Instance.UpdateScore(points);
    }

    public PlayerController GetPlayerController(int playerId)
    {
        playerControllers.TryGetValue(playerId, out PlayerController controller);
        return controller;
    }

    public GameState GetCurrentState()
    {
        return currentState;
    }

    public float GetTimeLeft()
    {
        return timeLeft;
    }

    public int GetScore()
    {
        return score;
    }

    public void SetMapName(string newMapName)
    {
        mapName = newMapName;
    }
}
