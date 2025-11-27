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
    [SerializeField] private Image[] mapHighlights; // Images pour surligner les maps

    [Header("Controller Navigation Colors")]
    [SerializeField] private ColorBlock selectedColors;
    [SerializeField] private ColorBlock normalColors;

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
    private int currentMapIndex = 0;
    private bool isInMapSelection = true;
    private bool isInWaitingRoom = false;
    private float inputCooldown = 0f;
    private const float INPUT_COOLDOWN_TIME = 0.2f;
    private bool isOnSetupButton = false; // true si le bouton "Préparer le lobby" est sélectionné

    private void Start()
    {
        // Configuration initiale
        ShowMapSelection();

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

        // Connecter les boutons de map (garde la compatibilité souris)
        for (int i = 0; i < mapButtons.Length && i < mapNames.Length; i++)
        {
            int index = i; // Capture pour la closure
            mapButtons[i].onClick.AddListener(() => SelectMapByIndex(index));
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

        // Initialiser la surbrillance de la première map
        UpdateMapHighlight();
        UpdateSelectionColors();
    }

    private void SelectMapByIndex(int index)
    {
        if (index >= 0 && index < mapNames.Length)
        {
            currentMapIndex = index;
            UpdateMapHighlight();
        }
    }

    private void ConfirmMapSelection()
    {
        if (currentMapIndex >= 0 && currentMapIndex < mapNames.Length)
        {
            selectedMap = mapNames[currentMapIndex];
            selectedMapText.text = $"Map sélectionnée : {selectedMap}";
            setupLobbyButton.interactable = true;

            // Mettre à jour le GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetMapName(selectedMap);
            }

            Debug.Log($"Map sélectionnée : {selectedMap}");
        }
    }

    private void UpdateMapHighlight()
    {
        // Mettre à jour les surbrillances visuelles
        if (mapHighlights != null && mapHighlights.Length > 0)
        {
            for (int i = 0; i < mapHighlights.Length; i++)
            {
                if (mapHighlights[i] != null)
                {
                    mapHighlights[i].enabled = (i == currentMapIndex && !isOnSetupButton);
                }
            }
        }

        // Mettre à jour le texte de prévisualisation
        if (selectedMapText != null && currentMapIndex < mapNames.Length)
        {
            selectedMapText.text = $"Map : {mapNames[currentMapIndex]}";
        }

        UpdateSelectionColors();
    }

    private void UpdateSelectionColors()
    {
        // Mettre à jour les couleurs des boutons de map
        if (mapButtons != null)
        {
            for (int i = 0; i < mapButtons.Length; i++)
            {
                if (mapButtons[i] != null)
                {
                    // Bouton jaune si c'est la map sélectionnée ET qu'on n'est pas sur le bouton Setup
                    mapButtons[i].colors = (i == currentMapIndex && !isOnSetupButton) ? selectedColors : normalColors;
                }
            }
        }

        // Mettre à jour la couleur du bouton "Préparer le lobby"
        if (setupLobbyButton != null)
        {
            setupLobbyButton.colors = isOnSetupButton ? selectedColors : normalColors;
        }
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
        isInMapSelection = true;
        isOnSetupButton = false;
        UpdateMapHighlight();
    }

    private void ShowWaitingRoom()
    {
        if (lobbyPanel) lobbyPanel.SetActive(true);
        if (mapSelectionPanel) mapSelectionPanel.SetActive(false);
        if (waitingRoomPanel) waitingRoomPanel.SetActive(true);
        if (gamePanel) gamePanel.SetActive(false);
        isInMapSelection = false;
        isInWaitingRoom = true;

        // Sélectionner le bouton "Lancer la partie" par défaut
        if (startGameButton != null)
        {
            startGameButton.colors = selectedColors;
        }
    }

    private void ShowGamePanel()
    {
        if (lobbyPanel) lobbyPanel.SetActive(false);
        if (mapSelectionPanel) mapSelectionPanel.SetActive(false);
        if (waitingRoomPanel) waitingRoomPanel.SetActive(false);
        if (gamePanel) gamePanel.SetActive(true);
        isInMapSelection = false;
        isInWaitingRoom = false;
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
        // Gestion du cooldown
        if (inputCooldown > 0)
        {
            inputCooldown -= Time.deltaTime;
        }

        // Gestion de la sélection de map avec les contrôleurs
        if (isInMapSelection && inputCooldown <= 0)
        {
            HandleMapSelectionInput();
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

    private void HandleMapSelectionInput()
    {
        // Validation avec P1_B1 ou P2_B1 (prioritaire)
        if (Input.GetButtonDown("P1_B1") || Input.GetButtonDown("P2_B1") ||
            Input.GetButtonDown("P1_Start") || Input.GetButtonDown("P2_Start"))
        {
            if (isOnSetupButton)
            {
                // Cliquer sur le bouton "Préparer le lobby"
                if (setupLobbyButton != null && setupLobbyButton.interactable)
                {
                    SetupLobby();
                }
            }
            else
            {
                // Confirmer la sélection de map
                ConfirmMapSelection();
            }
            inputCooldown = INPUT_COOLDOWN_TIME;
            return;
        }

        // Navigation avec les axes (P1 et P2)
        float p1Horizontal = Input.GetAxisRaw("P1_Horizontal");
        float p2Horizontal = Input.GetAxisRaw("P2_Horizontal");
        float p1Vertical = Input.GetAxisRaw("P1_Vertical");
        float p2Vertical = Input.GetAxisRaw("P2_Vertical");

        // Navigation verticale : descendre vers le bouton "Préparer le lobby"
        if ((p1Vertical < -0.5f || p2Vertical < -0.5f) && !isOnSetupButton)
        {
            // Aller sur le bouton "Préparer le lobby" s'il est activé
            if (setupLobbyButton != null && setupLobbyButton.interactable)
            {
                isOnSetupButton = true;
                UpdateSelectionColors();
                inputCooldown = INPUT_COOLDOWN_TIME;
                return;
            }
        }

        // Navigation verticale : remonter vers les maps
        if ((p1Vertical > 0.5f || p2Vertical > 0.5f) && isOnSetupButton)
        {
            isOnSetupButton = false;
            UpdateMapHighlight();
            inputCooldown = INPUT_COOLDOWN_TIME;
            return;
        }

        // Navigation horizontale (seulement si on n'est pas sur le bouton Setup)
        if (!isOnSetupButton)
        {
            // Déplacement vers la droite
            if (p1Horizontal > 0.5f || p2Horizontal > 0.5f)
            {
                currentMapIndex = (currentMapIndex + 1) % mapNames.Length;
                UpdateMapHighlight();
                inputCooldown = INPUT_COOLDOWN_TIME;
                return;
            }
            // Déplacement vers la gauche
            if (p1Horizontal < -0.5f || p2Horizontal < -0.5f)
            {
                currentMapIndex--;
                if (currentMapIndex < 0)
                    currentMapIndex = mapNames.Length - 1;
                UpdateMapHighlight();
                inputCooldown = INPUT_COOLDOWN_TIME;
                return;
            }
        }
    }

    private void HandleWaitingRoomInput()
    {
        // Validation avec P1_B1 ou P2_B1 pour lancer la partie
        if (Input.GetButtonDown("P1_B1") || Input.GetButtonDown("P2_B1") ||
            Input.GetButtonDown("P1_Start") || Input.GetButtonDown("P2_Start"))
        {
            if (startGameButton != null && startGameButton.interactable)
            {
                StartGame();
                inputCooldown = INPUT_COOLDOWN_TIME;
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