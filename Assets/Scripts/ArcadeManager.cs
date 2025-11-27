using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// Gère les fonctionnalités spécifiques à la borne d'arcade :
/// - Retour au menu principal avec le bouton blanc (ECHAP)
/// - Retour automatique après 60 secondes d'inactivité
/// </summary>
public class ArcadeManager : MonoBehaviour
{
    [Header("Arcade Settings")]
    [SerializeField] private bool enableArcadeFeatures = true;
    [SerializeField] private float inactivityTimeout = 60f; // Temps en secondes avant retour automatique
    [SerializeField] private float whiteButtonLongPressTime = 1.5f; // Temps max pour appui long
    [SerializeField] private bool useInstantReturn = true; // Si false, utilise l'appui long

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private float lastInputTime;
    private float whiteButtonPressTime = 0f;
    private bool isWhiteButtonPressed = false;

    // Import de la fonction BackToMenu depuis le plugin de la borne
    [DllImport("__Internal")]
    public static extern void BackToMenu();

    public static ArcadeManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        ResetInactivityTimer();
    }

    private void Update()
    {
        if (!enableArcadeFeatures)
            return;

        // Vérifier l'inactivité
        CheckInactivity();

        // Vérifier l'appui sur le bouton blanc (ECHAP)
        CheckWhiteButton();

        // Détecter tout input pour réinitialiser le timer d'inactivité
        if (DetectAnyInput())
        {
            ResetInactivityTimer();
        }
    }

    /// <summary>
    /// Détecte tout input (joysticks, boutons) pour réinitialiser le timer d'inactivité
    /// </summary>
    private bool DetectAnyInput()
    {
        // Vérifier les axes des 4 joueurs
        for (int i = 1; i <= 4; i++)
        {
            if (Mathf.Abs(Input.GetAxisRaw($"P{i}_Horizontal")) > 0.1f ||
                Mathf.Abs(Input.GetAxisRaw($"P{i}_Vertical")) > 0.1f)
            {
                return true;
            }

            // Vérifier les boutons des 4 joueurs
            if (Input.GetButton($"P{i}_B1") ||
                Input.GetButton($"P{i}_B2") ||
                Input.GetButton($"P{i}_B3") ||
                Input.GetButton($"P{i}_B4") ||
                Input.GetButton($"P{i}_B5") ||
                Input.GetButton($"P{i}_B6"))
            {
                return true;
            }
        }

        // Vérifier toutes les touches du clavier (sauf ECHAP qui est géré séparément)
        if (Input.anyKeyDown && !Input.GetKeyDown(KeyCode.Escape))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Vérifie si le temps d'inactivité est dépassé
    /// </summary>
    private void CheckInactivity()
    {
        if (Time.time - lastInputTime >= inactivityTimeout)
        {
            if (showDebugLogs)
                Debug.Log("ArcadeManager: Inactivité détectée, retour au menu...");

            ReturnToMenu();
        }
    }

    /// <summary>
    /// Vérifie l'appui sur le bouton blanc (ECHAP)
    /// </summary>
    private void CheckWhiteButton()
    {
        // Appui instantané
        if (useInstantReturn)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (showDebugLogs)
                    Debug.Log("ArcadeManager: Bouton blanc pressé (instantané), retour au menu...");

                ReturnToMenu();
            }
        }
        // Appui long
        else
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                isWhiteButtonPressed = true;
                whiteButtonPressTime = Time.time;

                if (showDebugLogs)
                    Debug.Log("ArcadeManager: Bouton blanc pressé, attente appui long...");
            }

            if (isWhiteButtonPressed && Input.GetKey(KeyCode.Escape))
            {
                float pressDuration = Time.time - whiteButtonPressTime;

                // Si l'appui dépasse le temps max, retourner au menu
                if (pressDuration >= whiteButtonLongPressTime)
                {
                    if (showDebugLogs)
                        Debug.Log($"ArcadeManager: Bouton blanc maintenu {pressDuration:F2}s, retour au menu...");

                    isWhiteButtonPressed = false;
                    ReturnToMenu();
                }
            }

            if (Input.GetKeyUp(KeyCode.Escape))
            {
                isWhiteButtonPressed = false;

                if (showDebugLogs)
                    Debug.Log("ArcadeManager: Bouton blanc relâché avant le temps requis.");
            }
        }
    }

    /// <summary>
    /// Réinitialise le timer d'inactivité
    /// </summary>
    public void ResetInactivityTimer()
    {
        lastInputTime = Time.time;
    }

    /// <summary>
    /// Appelle la fonction BackToMenu de la borne d'arcade
    /// </summary>
    private void ReturnToMenu()
    {
        if (showDebugLogs)
            Debug.Log("ArcadeManager: Appel de BackToMenu()");

        try
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
                BackToMenu();
            #else
                // En mode éditeur ou autre plateforme, simuler le retour au menu
                Debug.Log("ArcadeManager: BackToMenu() appelé (simulation en dehors de WebGL)");
                SimulateBackToMenu();
            #endif
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ArcadeManager: Erreur lors de l'appel à BackToMenu(): {e.Message}");

            // Fallback : simuler le retour au menu
            SimulateBackToMenu();
        }
    }

    /// <summary>
    /// Simule le retour au menu pour les tests en éditeur
    /// </summary>
    private void SimulateBackToMenu()
    {
        // Recharger la scène du menu principal ou quitter l'application
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    /// <summary>
    /// Active/Désactive les fonctionnalités arcade
    /// </summary>
    public void SetArcadeFeaturesEnabled(bool enabled)
    {
        enableArcadeFeatures = enabled;
        if (enabled)
            ResetInactivityTimer();
    }

    /// <summary>
    /// Obtient le temps restant avant inactivité
    /// </summary>
    public float GetTimeUntilInactivity()
    {
        return Mathf.Max(0, inactivityTimeout - (Time.time - lastInputTime));
    }

    /// <summary>
    /// Obtient si les fonctionnalités arcade sont activées
    /// </summary>
    public bool IsArcadeEnabled()
    {
        return enableArcadeFeatures;
    }
}
