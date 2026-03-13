using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Joystick virtuel fixe pour les contrôles mobiles.
/// À attacher sur le GameObject background du joystick (Image circulaire).
/// </summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;

    /// <summary>Direction normalisée du joystick (magnitude entre 0 et 1)</summary>
    public Vector2 Direction { get; private set; }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            return;

        float radius = background.sizeDelta.x * 0.5f;
        Vector2 input = localPoint / radius;

        // Limiter au cercle
        Direction = input.magnitude > 1f ? input.normalized : input;

        handle.localPosition = Direction * radius * 0.7f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Direction = Vector2.zero;
        handle.localPosition = Vector2.zero;
    }
}
