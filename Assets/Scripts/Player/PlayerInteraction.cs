using UnityEngine;

[RequireComponent(typeof(InventorySystem))]
public class PlayerInteraction : MonoBehaviour
{
    private InventorySystem inventory;
    private PlayerController playerController;

    [Header("Raycast Settings")]
    [SerializeField] private float interactionDistance = 0.7f;
    [SerializeField] private Vector2 boxCastSize = new Vector2(0.5f, 0.5f);

    [SerializeField] private LayerMask interactableLayer;
    private Vector2 facingDirection = Vector2.down;

    // Pour le gizmo uniquement
    private IInteractable lastDetected;

    private void Awake()
    {
        inventory = GetComponent<InventorySystem>();
        if (inventory == null)
        {
            inventory = gameObject.AddComponent<InventorySystem>();
        }

        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        Vector2 movement = playerController.GetCurrentMovement();
        if (movement != Vector2.zero)
        {
            if (Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
                facingDirection = movement.x > 0 ? Vector2.right : Vector2.left;
            else
                facingDirection = movement.y > 0 ? Vector2.up : Vector2.down;
        }
    }

    private IInteractable FindBestInteractable()
    {
        // Priorité 1 : Raycast fin (précis)
        RaycastHit2D rayHit = Physics2D.Raycast(
            transform.position,
            facingDirection,
            interactionDistance,
            interactableLayer
        );

        if (rayHit.collider != null)
        {
            IInteractable interactable = rayHit.collider.GetComponent<IInteractable>();
            if (interactable != null)
                return interactable;
        }

        // Priorité 2 : BoxCast (fallback)
        RaycastHit2D boxHit = Physics2D.BoxCast(
            transform.position,
            boxCastSize,
            0f,
            facingDirection,
            interactionDistance,
            interactableLayer
        );

        if (boxHit.collider != null)
        {
            IInteractable interactable = boxHit.collider.GetComponent<IInteractable>();
            if (interactable != null)
                return interactable;
        }

        return null;
    }

    public void OnInteract()
    {
        // Malus objets collants : interactions bloquées
        if (EffectManager.Instance != null && EffectManager.Instance.ObjetsCollantsActif)
            return;

        // Raycast au moment exact de l'interaction
        IInteractable target = FindBestInteractable();
        lastDetected = target;

        if (target != null)
        {
            target.Interact(this);

            if (InventoryUI.Instance != null && playerController != null)
            {
                InventoryUI.Instance.UpdatePlayerInventory(playerController.playerId, inventory);
            }
        }
    }

    public InventorySystem GetInventory()
    {
        return inventory;
    }

    public IInteractable GetCurrentInteractable()
    {
        // Raycast à la demande pour le réseau
        return FindBestInteractable();
    }

    private void OnDrawGizmos()
    {
        Vector2 origin = transform.position;
        Vector2 end = origin + facingDirection * interactionDistance;

        Gizmos.color = lastDetected != null ? Color.green : Color.red;

        // Raycast line
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, end);

        // BoxCast fallback
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireCube(origin, boxCastSize);
        Gizmos.DrawWireCube((Vector3)end, boxCastSize);

        Vector2 halfSize = boxCastSize * 0.5f;
        Vector2[] corners = new Vector2[]
        {
            new Vector2(-halfSize.x, -halfSize.y),
            new Vector2( halfSize.x, -halfSize.y),
            new Vector2( halfSize.x,  halfSize.y),
            new Vector2(-halfSize.x,  halfSize.y),
        };

        for (int i = 0; i < corners.Length; i++)
        {
            Gizmos.DrawLine(origin + corners[i], end + corners[i]);
        }
    }
}
