using UnityEngine;

/// <summary>
/// Fournit les inputs mobiles (joystick + bouton) au PlayerController.
/// S'affiche automatiquement uniquement sur mobile / WebGL tactile.
/// </summary>
public class MobileInputProvider : MonoBehaviour
{
    public static MobileInputProvider Instance { get; private set; }

    [SerializeField] private VirtualJoystick joystick;
    [SerializeField] private MobileInteractButton interactButton;
    [SerializeField] private GameObject mobileUI; // Parent contenant tout le HUD mobile

    /// <summary>Direction du joystick (normalisée, magnitude 0-1)</summary>
    public Vector2 MoveInput => joystick != null ? joystick.Direction : Vector2.zero;

    /// <summary>Vrai tant que le bouton d'interaction est maintenu</summary>
    public bool InteractPressed => interactButton != null && interactButton.IsPressed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Afficher le HUD mobile uniquement sur les plateformes tactiles
        if (mobileUI != null)
            mobileUI.SetActive(IsTouchPlatform());
    }

    private bool IsTouchPlatform()
    {
        // Mobile natif
        if (Application.isMobilePlatform) return true;

        // WebGL sur mobile (détection via type d'appareil Unity)
        if (Application.platform == RuntimePlatform.WebGLPlayer
            && SystemInfo.deviceType == DeviceType.Handheld)
            return true;

        return false;
    }
}
