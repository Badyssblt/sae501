using UnityEngine;
using UnityEngine.EventSystems;

public class MobileInteractButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public static MobileInteractButton Instance { get; private set; }

    public bool IsPressed { get; private set; }

    // Vrai pendant exactement une frame après l'appui (équivalent GetButtonDown)
    public bool WasPressed { get; private set; }
    private bool pendingPress = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        WasPressed = pendingPress;
    }

    private void LateUpdate()
    {
        // Remis à false après que tous les Update() aient pu le lire
        pendingPress = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsPressed = true;
        pendingPress = true;
        Debug.Log("[MobileInteractButton] OnPointerDown");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsPressed = false;
        Debug.Log("[MobileInteractButton] OnPointerUp");
    }
}
