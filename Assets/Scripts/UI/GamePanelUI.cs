using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GamePanelUI : MonoBehaviour
{
    [Header("Restart Panel")]
    [SerializeField] private GameObject restartPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;

    [Header("Restart Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    [Header("Joystick Navigation")]
    [SerializeField] private float inputDelay = 0.2f;
    [SerializeField] private ColorBlock selectedColors;
    [SerializeField] private ColorBlock normalColors;

    private int currentButtonIndex = 0;
    private float lastInputTime = 0f;
    private Button[] restartButtons;
    private bool isPanelActive = false;

    private void Start()
    {
        // Initialiser le tableau de boutons
        restartButtons = new Button[] { restartButton, quitButton };

        // Connecter les boutons
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartGame);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitGame);

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
        }

    }

    private void Update()
    {
        if (isPanelActive)
        {
            HandleJoystickInput();
        }
    }

    private void HandleJoystickInput()
    {
        // Éviter les entrées trop rapides
        if (Time.time - lastInputTime < inputDelay)
            return;

        float vertical = Input.GetAxisRaw("P1_Vertical");

        // Navigation verticale
        if (Mathf.Abs(vertical) > 0.5f)
        {
            if (vertical < 0)
            {
                // Bas - aller au bouton suivant
                currentButtonIndex++;
                if (currentButtonIndex >= restartButtons.Length)
                    currentButtonIndex = 0;
            }
            else
            {
                // Haut - aller au bouton précédent
                currentButtonIndex--;
                if (currentButtonIndex < 0)
                    currentButtonIndex = restartButtons.Length - 1;
            }
            lastInputTime = Time.time;
            UpdateButtonSelection();
        }

        // Confirmation avec P1_B1
        if (Input.GetButtonDown("P1_B1"))
        {
            if (restartButtons[currentButtonIndex] != null && restartButtons[currentButtonIndex].interactable)
            {
                restartButtons[currentButtonIndex].onClick.Invoke();
            }
        }
    }

    private void UpdateButtonSelection()
    {
        for (int i = 0; i < restartButtons.Length; i++)
        {
            if (restartButtons[i] != null)
            {
                restartButtons[i].colors = (i == currentButtonIndex) ? selectedColors : normalColors;
            }
        }
    }

    public void ShowRestartPanel(int finalScore)
    {
        // Le panel restart n'est affiché que sur le host
        if (NetworkManager.Instance?.Role == CookMoiCa.Network.NetworkRole.Client)
            return;

        if (restartPanel != null)
        {
            restartPanel.SetActive(true);
            isPanelActive = true;

            // Afficher le score final
            if (finalScoreText != null)
            {
                finalScoreText.text = "Score Final: " + finalScore;
            }

            // Réinitialiser la sélection des boutons
            currentButtonIndex = 0;
            UpdateButtonSelection();
        }
    }

    public void HideRestartPanel()
    {
        if (restartPanel != null)
        {
            restartPanel.SetActive(false);
            isPanelActive = false;
        }
    }

    private void OnRestartGame()
    {
        Debug.Log("Restart Game button pressed");
        if (GameManager.Instance != null)
        {
            HideRestartPanel();
            GameManager.Instance.RestartGame();
        }
    }

    private void OnQuitGame()
    {
        Debug.Log("Quit Game button pressed");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
