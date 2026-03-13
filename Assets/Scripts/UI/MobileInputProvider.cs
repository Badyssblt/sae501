using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileInputProvider : MonoBehaviour
{
    public static MobileInputProvider Instance { get; private set; }

    [SerializeField] private VirtualJoystick joystick;
    [SerializeField] private MobileInteractButton interactButton;
    [SerializeField] private GameObject mobileUI;

    public Vector2 MoveInput => joystick != null ? joystick.Direction : Vector2.zero;
    public bool InteractPressed => interactButton != null && interactButton.IsPressed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Créer l'EventSystem s'il est absent (nécessaire pour tous les événements UI)
        if (EventSystem.current == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
            Debug.Log("[MobileInputProvider] EventSystem créé automatiquement");
        }

        bool isMobile = WebGLHelper.IsMobileDevice();
        Debug.Log($"[MobileInputProvider] isMobile: {isMobile} | platform: {Application.platform} | deviceType: {SystemInfo.deviceType} | isMobilePlatform: {Application.isMobilePlatform}");
        Debug.Log($"[MobileInputProvider] joystick: {(joystick != null ? joystick.name : "NULL")} | interactButton: {(interactButton != null ? interactButton.name : "NULL")} | mobileUI: {(mobileUI != null ? mobileUI.name : "NULL")}");

        if (mobileUI != null)
        {
            mobileUI.SetActive(isMobile);
            Debug.Log($"[MobileInputProvider] mobileUI SetActive({isMobile})");
        }
        else
        {
            Debug.LogWarning("[MobileInputProvider] mobileUI n'est pas assigné dans l'Inspector !");
        }

        if (isMobile)
            WebGLHelper.SetupMobileViewport();
    }

    private void Update()
    {
        // Log toutes les 2 secondes si un input mobile est détecté
        if (Time.frameCount % 120 == 0)
        {
            if (joystick != null && joystick.Direction != Vector2.zero)
                Debug.Log($"[MobileInputProvider] MoveInput actif: {MoveInput}");
            if (InteractPressed)
                Debug.Log("[MobileInputProvider] InteractPressed: true");
        }
    }
}
