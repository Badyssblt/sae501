using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject mapSelectionPanel;
    [SerializeField] private GameObject waitingRoomPanel;
    [SerializeField] private GameObject gamePanel;

    [Header("Map Selection")]
    [SerializeField] private Button[] mapButtons;
    [SerializeField] private string[] mapNames = { "italie", "japon", "mexique" };
    [SerializeField] private TextMeshProUGUI selectedMapText;

    [Header("Waiting Room")]
    [SerializeField] private Button setupLobbyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TextMeshProUGUI[] playerSlotTexts = new TextMeshProUGUI[4];
    [SerializeField] private TextMeshProUGUI gameStatusText;

    [Header("Game UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI scoreText;

    private string selectedMap = "";
    private bool isLobbyReady = false;

    private void Start()
    {
        // Configuration initiale
        ShowMapSelection();

        // Connecter les boutons de map
        for (int i = 0; i < mapButtons.Length && i < mapNames.Length; i++)
        {
            int index = i; // Capture pour la closure
            mapButtons[i].onClick.AddListener(() => SelectMap(mapNames[index]));
        }

        // Connecter les boutons principaux
        setupLobbyButton.onClick.AddListener(SetupLobby);
        startGameButton.onClick.AddListener(StartGame);

        // État initial des boutons
        setupLobbyButton.interactable = false;
        startGameButton.interactable = false;

        // S'abonner aux événements du NetworkManager
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnPlayerJoined += OnPlayerJoined;
            NetworkManager.Instance.OnPlayerLeft += OnPlayerLeft;
        }
    }

    private void SelectMap(string mapName)
    {
        selectedMap = mapName;
        selectedMapText.text = $"Map sélectionnée : {mapName}";
        setupLobbyButton.interactable = true;

        // Mettre à jour le GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetMapName(mapName);
        }

        Debug.Log($"Map sélectionnée : {mapName}");
    }

    private void SetupLobby()
    {
        if (string.IsNullOrEmpty(selectedMap))
        {
            Debug.LogError("Aucune map sélectionnée!");
            return;
        }

        // Configurer le lobby
        GameManager.Instance?.SetupLobby();

        isLobbyReady = true;
        ShowWaitingRoom();

        // Activer le bouton de démarrage
        startGameButton.interactable = true;

        UpdatePlayerSlots();
        gameStatusText.text = "En attente des joueurs...";
    }

    private void StartGame()
    {
        if (!isLobbyReady)
        {
            Debug.LogError("Le lobby n'est pas prêt!");
            return;
        }

        // Démarrer la partie
        GameManager.Instance?.StartGame();

        ShowGamePanel();
        gameStatusText.text = "Partie en cours!";
    }

    private void ShowMapSelection()
    {
        if (lobbyPanel) lobbyPanel.SetActive(true);
        if (mapSelectionPanel) mapSelectionPanel.SetActive(true);
        if (waitingRoomPanel) waitingRoomPanel.SetActive(false);
        if (gamePanel) gamePanel.SetActive(false);
    }

    private void ShowWaitingRoom()
    {
        if (lobbyPanel) lobbyPanel.SetActive(true);
        if (mapSelectionPanel) mapSelectionPanel.SetActive(false);
        if (waitingRoomPanel) waitingRoomPanel.SetActive(true);
        if (gamePanel) gamePanel.SetActive(false);
    }

    private void ShowGamePanel()
    {
        if (lobbyPanel) lobbyPanel.SetActive(false);
        if (mapSelectionPanel) mapSelectionPanel.SetActive(false);
        if (waitingRoomPanel) waitingRoomPanel.SetActive(false);
        if (gamePanel) gamePanel.SetActive(true);
    }

    private void UpdatePlayerSlots()
    {
        // Mettre à jour l'affichage des slots
        for (int i = 0; i < 4; i++)
        {
            if (playerSlotTexts[i] != null)
            {
                var controller = GameManager.Instance?.GetPlayerController(i + 1);
                if (controller != null)
                {
                    if (i < 2)
                    {
                        playerSlotTexts[i].text = $"Joueur {i + 1} (Local)";
                        playerSlotTexts[i].color = Color.green;
                    }
                    else
                    {
                        playerSlotTexts[i].text = $"Joueur {i + 1} (En attente)";
                        playerSlotTexts[i].color = Color.yellow;
                    }
                }
                else
                {
                    playerSlotTexts[i].text = $"Slot {i + 1} - Vide";
                    playerSlotTexts[i].color = Color.gray;
                }
            }
        }
    }

    private void OnPlayerJoined(int slot, string playerName)
    {
        if (playerSlotTexts[slot - 1] != null)
        {
            playerSlotTexts[slot - 1].text = $"Joueur {slot}: {playerName}";
            playerSlotTexts[slot - 1].color = Color.green;
        }

        gameStatusText.text = $"{playerName} a rejoint la partie!";
    }

    private void OnPlayerLeft(int slot)
    {
        if (playerSlotTexts[slot - 1] != null)
        {
            playerSlotTexts[slot - 1].text = $"Slot {slot} - Vide";
            playerSlotTexts[slot - 1].color = Color.gray;
        }

        gameStatusText.text = $"Joueur {slot} a quitté la partie";
    }

    private void Update()
    {
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