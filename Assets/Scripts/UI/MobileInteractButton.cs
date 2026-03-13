using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Bouton d'interaction tactile pour mobile.
/// À attacher sur le bouton d'interaction (Image circulaire bas droite).
/// </summary>
public class MobileInteractButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public static MobileInteractButton Instance { get; private set; }

    /// <summary>Vrai tant que le doigt est appuyé sur le bouton</summary>
    public bool IsPressed { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsPressed = false;
    }
}
