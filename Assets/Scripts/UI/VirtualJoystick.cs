using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform handle;

    private RectTransform background;
    private float radius;

    public Vector2 Direction { get; private set; }

    private void Awake()
    {
        background = GetComponent<RectTransform>();
        radius = background.sizeDelta.x * 0.5f;
        Debug.Log($"[VirtualJoystick] Awake - background: {background.name}, sizeDelta: {background.sizeDelta}, radius: {radius}, handle: {(handle != null ? handle.name : "NULL")}");

        // Vérifier les composants nécessaires aux pointer events
        var img = GetComponent<UnityEngine.UI.Image>();
        var graphic = GetComponent<UnityEngine.UI.Graphic>();
        Debug.Log($"[VirtualJoystick] Image: {(img != null ? $"trouvée, RaycastTarget={img.raycastTarget}" : "NULL")} | Graphic: {(graphic != null ? "trouvée" : "NULL")}");

        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            Debug.Log($"[VirtualJoystick] Canvas: {canvas.name}, renderMode: {canvas.renderMode} | GraphicRaycaster: {(raycaster != null ? "présent" : "MANQUANT !")}");
        }
        else
        {
            Debug.LogError("[VirtualJoystick] Aucun Canvas parent trouvé !");
        }

        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        Debug.Log($"[VirtualJoystick] EventSystem: {(eventSystem != null ? eventSystem.name : "MANQUANT !")}");
    }

    private void Update()
    {
        // Détecte si Unity reçoit des touches ou clics, indépendamment du UI
        if (Input.GetMouseButtonDown(0))
            Debug.Log($"[VirtualJoystick] Input.GetMouseButtonDown(0) détecté à {Input.mousePosition}");
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            Debug.Log($"[VirtualJoystick] Touch détecté à {Input.GetTouch(0).position}");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"[VirtualJoystick] OnPointerDown - position écran: {eventData.position}");
        UpdateDirection(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateDirection(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("[VirtualJoystick] OnPointerUp - direction remise à zéro");
        Direction = Vector2.zero;
        if (handle != null)
            handle.localPosition = Vector2.zero;
    }

    private void UpdateDirection(PointerEventData eventData)
    {
        Camera cam = eventData.pressEventCamera;
        bool converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, cam, out Vector2 localPoint);

        if (!converted)
        {
            Debug.LogWarning($"[VirtualJoystick] ScreenPointToLocalPointInRectangle a échoué ! background null: {background == null}, cam: {cam}");
            return;
        }

        radius = background.sizeDelta.x * 0.5f;
        Vector2 input = localPoint / radius;
        Direction = input.magnitude > 1f ? input.normalized : input;

        if (handle != null)
            handle.localPosition = Direction * radius * 0.7f;

        Debug.Log($"[VirtualJoystick] Direction: {Direction} | localPoint: {localPoint} | radius: {radius}");
    }
}
