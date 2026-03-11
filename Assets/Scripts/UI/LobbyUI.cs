using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance;

    [Header("UI Elements")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject waitingRoomPanel;
    [SerializeField] private GameObject gamePanel;

    [Header("Controller Navigation Colors")]
    [SerializeField] private ColorBlock selectedColors;
    [SerializeField] private ColorBlock normalColors;

    [Header("Waiting Room")]
    [SerializeField] private Button setupLobbyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private GameObject[] playerSlots = new GameObject[4]; // Les GameObjects "Slot" parents (contiennent le texte + image Player)
    [SerializeField] private TextMeshProUGUI gameStatusText;

    [Header("Game UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI scoreText;

    private bool isInWaitingRoom = false;
    private float inputCooldown = 0f;
    private const float INPUT_COOLDOWN_TIME = 0.2f;

    // Tracking des joueurs locaux dans le lobby
    private bool p1Joined = false;
    private bool p2Joined = false;
    private int localPlayerCount = 0;
    private bool isOnStartButton = false; // navigation sur le bouton Start

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Si on est en mode Client, on cache tout le lobby UI et on va direct au jeu
        if (NetworkManager.Instance != null && NetworkManager.Instance.Role == CookMoiCa.Network.NetworkRole.Client)
        {
            Debug.Log("[LobbyUI] Mode Client détecté - Skip du lobby");
            ShowGamePanel();
            return;
        }

        // Initialiser les couleurs si non définies
        if (normalColors.normalColor == Color.clear)
        {
            normalColors = ColorBlock.defaultColorBlock;
        }
        if (selectedColors.normalColor == Color.clear)
        {
            selectedColors = ColorBlock.defaultColorBlock;
            selectedColors.normalColor = Color.yellow;
            selectedColors.highlightedColor = Color.yellow;
            selectedColors.selectedColor = Color.yellow;
        }

        // Connecter les boutons principaux
        startGameButton.onClick.AddListener(StartGame);

        // État initial des boutons
        startGameButton.interactable = false;

        // S'abonner aux événements du NetworkManager
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnPlayerJoined += OnPlayerJoined;
            NetworkManager.Instance.OnPlayerLeft += OnPlayerLeft;
        }
    }

    /// <summary>
    /// Appelé par GameManager.StartMenu() pour afficher la waiting room directement
    /// </summary>
    public void EnterLobby()
    {
        Debug.Log($"[LobbyUI] EnterLobby - lobbyPanel: {lobbyPanel != null}, waitingRoomPanel: {waitingRoomPanel != null}");

        ShowWaitingRoom();

        // P1 rejoint automatiquement
        p1Joined = true;
        p2Joined = false;
        localPlayerCount = 1;
        isOnStartButton = false;
        startGameButton.interactable = true;
        UpdateLobbySlots();
        UpdateSelectionColors();

        // Cooldown pour éviter que le même appui bouton lance immédiatement la partie
        inputCooldown = 0.5f;

        Debug.Log($"[LobbyUI] EnterLobby done - lobbyPanel active: {lobbyPanel?.activeSelf}, waitingRoom active: {waitingRoomPanel?.activeSelf}");
    }

    private void StartGame()
    {
        // Lancer la partie via GameManager avec le nombre de joueurs locaux
        GameManager.Instance?.LaunchGame(localPlayerCount);

        ShowGamePanel();
    }

    private void ShowWaitingRoom()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (lobbyPanel) lobbyPanel.SetActive(true);
        if (waitingRoomPanel) waitingRoomPanel.SetActive(true);
        if (gamePanel) gamePanel.SetActive(false);
        isInWaitingRoom = true;
    }

    private void ShowGamePanel()
    {
        if (lobbyPanel) lobbyPanel.SetActive(false);
        if (waitingRoomPanel) waitingRoomPanel.SetActive(false);
        if (gamePanel) gamePanel.SetActive(true);
        isInWaitingRoom = false;
    }

    private void UpdateLobbySlots()
    {
        bool[] joined = { p1Joined, p2Joined, false, false };

        for (int i = 0; i < 4; i++)
        {
            if (playerSlots[i] == null) continue;

            // Trouver le texte et l'image Player dans le slot
            var slotText = playerSlots[i].GetComponentInChildren<TextMeshProUGUI>();
            var playerImage = playerSlots[i].transform.Find("Player");

            if (slotText != null)
            {
                if (joined[i])
                {
                    slotText.text = $"Joueur {i + 1} - Prêt";
                    slotText.color = Color.green;
                }
                else
                {
                    slotText.text = i < 2
                        ? $"Slot {i + 1} - Appuyez sur P{i + 1}"
                        : $"Slot {i + 1} - En attente (réseau)";
                    slotText.color = Color.gray;
                }
            }

            // Afficher/masquer l'image du joueur
            if (playerImage != null)
            {
                playerImage.gameObject.SetActive(joined[i]);
            }
        }

        // Mettre à jour le texte de statut
        if (gameStatusText != null)
        {
            gameStatusText.text = $"{localPlayerCount} joueur(s) connecté(s)";
        }
    }

    private void OnPlayerJoined(int slot, string playerName)
    {
        if (slot - 1 < playerSlots.Length && playerSlots[slot - 1] != null)
        {
            var slotText = playerSlots[slot - 1].GetComponentInChildren<TextMeshProUGUI>();
            var playerImage = playerSlots[slot - 1].transform.Find("Player");

            if (slotText != null)
            {
                slotText.text = $"Joueur {slot}: {playerName}";
                slotText.color = Color.green;
            }
            if (playerImage != null)
            {
                playerImage.gameObject.SetActive(true);
            }
        }

        if (gameStatusText != null)
            gameStatusText.text = $"{playerName} a rejoint la partie!";
    }

    private void OnPlayerLeft(int slot)
    {
        if (slot - 1 < playerSlots.Length && playerSlots[slot - 1] != null)
        {
            var slotText = playerSlots[slot - 1].GetComponentInChildren<TextMeshProUGUI>();
            var playerImage = playerSlots[slot - 1].transform.Find("Player");

            if (slotText != null)
            {
                slotText.text = $"Slot {slot} - Vide";
                slotText.color = Color.gray;
            }
            if (playerImage != null)
            {
                playerImage.gameObject.SetActive(false);
            }
        }

        if (gameStatusText != null)
            gameStatusText.text = $"Joueur {slot} a quitté la partie";
    }

    private void Update()
    {
        // Gestion du cooldown
        if (inputCooldown > 0)
        {
            inputCooldown -= Time.deltaTime;
        }

        // Gestion de la WaitingRoom avec les contrôleurs
        if (isInWaitingRoom && inputCooldown <= 0)
        {
            HandleWaitingRoomInput();
        }

        // Mettre à jour le timer et le score pendant la partie
        if (GameManager.Instance != null && GameManager.Instance.GetCurrentState() == GameState.Playing)
        {
            if (timerText != null)
            {
                float timeLeft = GameManager.Instance.GetTimeLeft();
                int minutes = Mathf.FloorToInt(timeLeft / 60);
                int seconds = Mathf.FloorToInt(timeLeft % 60);
                timerText.text = $"{minutes:00}:{seconds:00}";
            }

            if (scoreText != null)
            {
                scoreText.text = $"Score: {GameManager.Instance.GetScore()}";
            }
        }
    }

    private void HandleWaitingRoomInput()
    {
        // P2 rejoint le lobby
        if (!p2Joined && (Input.GetButtonDown("P2_B1") || Input.GetButtonDown("P2_Start")))
        {
            p2Joined = true;
            localPlayerCount = 2;
            UpdateLobbySlots();
            inputCooldown = INPUT_COOLDOWN_TIME;
            return;
        }

        // Navigation verticale avec P1
        float p1Vertical = Input.GetAxisRaw("P1_Vertical");

        // Bas → aller sur le bouton Start
        if (p1Vertical < -0.5f && !isOnStartButton)
        {
            isOnStartButton = true;
            UpdateSelectionColors();
            inputCooldown = INPUT_COOLDOWN_TIME;
            return;
        }

        // Haut → remonter sur les slots
        if (p1Vertical > 0.5f && isOnStartButton)
        {
            isOnStartButton = false;
            UpdateSelectionColors();
            inputCooldown = INPUT_COOLDOWN_TIME;
            return;
        }

        // Valider avec P1_B1 quand on est sur le bouton Start
        if (isOnStartButton && (Input.GetButtonDown("P1_B1") || Input.GetButtonDown("P1_Start")))
        {
            StartGame();
            inputCooldown = INPUT_COOLDOWN_TIME;
        }
    }

    private void UpdateSelectionColors()
    {
        if (startGameButton != null)
        {
            startGameButton.colors = isOnStartButton ? selectedColors : normalColors;
        }
    }

    private void OnDestroy()
    {
        // Se désabonner des événements
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnPlayerJoined -= OnPlayerJoined;
            NetworkManager.Instance.OnPlayerLeft -= OnPlayerLeft;
        }
    }
}