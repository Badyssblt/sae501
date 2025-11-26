using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Menu Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    [Header("Joystick Navigation")]
    [SerializeField] private float inputDelay = 0.2f;
    [SerializeField] private ColorBlock selectedColors;
    [SerializeField] private ColorBlock normalColors;

    private int currentButtonIndex = 0;
    private float lastInputTime = 0f;
    private Button[] menuButtons;

    private void Start()
    {
        // Initialiser le tableau de boutons
        menuButtons = new Button[] { startButton, quitButton };

        // Connecter les boutons
        if (startButton != null)
            startButton.onClick.AddListener(OnStartGame);
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

        // Initialiser la sélection
        UpdateButtonSelection();
    }

    private void Update()
    {
        HandleJoystickInput();
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
                if (currentButtonIndex >= menuButtons.Length)
                    currentButtonIndex = 0;
            }
            else
            {
                // Haut - aller au bouton précédent
                currentButtonIndex--;
                if (currentButtonIndex < 0)
                    currentButtonIndex = menuButtons.Length - 1;
            }
            lastInputTime = Time.time;
            UpdateButtonSelection();
        }

        // Confirmation avec P1_B1
        if (Input.GetButtonDown("P1_B1"))
        {
            if (menuButtons[currentButtonIndex] != null && menuButtons[currentButtonIndex].interactable)
            {
                menuButtons[currentButtonIndex].onClick.Invoke();
            }
        }
    }

    private void UpdateButtonSelection()
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] != null)
            {
                menuButtons[i].colors = (i == currentButtonIndex) ? selectedColors : normalColors;
            }
        }
    }

    private void OnStartGame()
    {
        Debug.Log("Start Game button pressed");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartMenu();
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
