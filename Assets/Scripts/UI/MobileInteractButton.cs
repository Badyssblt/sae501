using UnityEngine;

public class MobileInteractButton : MonoBehaviour
{
    private RectTransform rectTransform;
    private Canvas canvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        // Vérifier clic souris ou touch
        bool pressed = false;
        Vector2 inputPos = Vector2.zero;

        if (Input.GetMouseButtonDown(0))
        {
            pressed = true;
            inputPos = Input.mousePosition;
        }
        else
        {
            // Vérifier tous les doigts, pas seulement le premier
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began)
                {
                    pressed = true;
                    inputPos = t.position;
                    break;
                }
            }
        }

        if (!pressed) return;

        // Vérifier si le clic est dans les bounds du bouton
        if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, inputPos, canvas?.worldCamera))
            TriggerInteract();
    }

    private void TriggerInteract()
    {
        var allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var pc in allPlayers)
        {
            if (pc.isLocalPlayer)
            {
                pc.MobileInteract();
                return;
            }
        }
        Debug.LogWarning("[MobileInteractButton] Aucun joueur local trouvé");
    }
}
