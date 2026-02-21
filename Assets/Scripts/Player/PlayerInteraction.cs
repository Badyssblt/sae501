using UnityEngine;

[RequireComponent(typeof(InventorySystem))]
public class PlayerInteraction : MonoBehaviour
{
    private IInteractable currentInteractable;
    private IInteractable triggerInteractable; // Pour les PNJ qui utilisent des triggers
    private InventorySystem inventory;
    private PlayerController playerController;

    [Header("Raycast Settings")]
    [SerializeField] private float interactionDistance = 1.5f;
    [SerializeField] private float proximityRadius = 0.8f; // Rayon de détection par proximité
    [SerializeField] private LayerMask interactableLayer;
    private Vector2 lastFacingDirection = Vector2.down; // Direction par défaut

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
        // Mettre à jour la direction du regard
        Vector2 movement = playerController.GetCurrentMovement();
        if (movement != Vector2.zero)
        {
            lastFacingDirection = movement.normalized;
        }

        // Détecter l'interactable avec raycast
        DetectInteractable();
    }

    private void DetectInteractable()
    {
        // Priorité 1 : Raycast (pour les Counters)
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            lastFacingDirection,
            interactionDistance,
            interactableLayer
        );

        if (hit.collider != null)
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                currentInteractable = interactable;
                return;
            }
        }

        // Priorité 2 : Détection par proximité (si raycast ne trouve rien)
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, proximityRadius, interactableLayer);
        if (nearbyColliders.Length > 0)
        {
            // Trouver le plus proche
            float closestDistance = float.MaxValue;
            IInteractable closestInteractable = null;

            foreach (Collider2D col in nearbyColliders)
            {
                IInteractable interactable = col.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    float dist = Vector2.Distance(transform.position, col.transform.position);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closestInteractable = interactable;
                    }
                }
            }

            if (closestInteractable != null)
            {
                currentInteractable = closestInteractable;
                return;
            }
        }

        // Priorité 3 : Trigger (pour les PNJ)
        if (triggerInteractable != null)
        {
            currentInteractable = triggerInteractable;
            return;
        }

        currentInteractable = null;
    }

    public void OnInteract()
    {
        if (currentInteractable != null)
        {
            currentInteractable.Interact(this);

            // Mettre à jour l'UI pour ce joueur spécifique
            if (InventoryUI.Instance != null && playerController != null)
            {
                InventoryUI.Instance.UpdatePlayerInventory(playerController.playerId, inventory);
            }
        }
    }

    // Permet aux objets interactables d'accéder à l'inventaire du joueur
    public InventorySystem GetInventory()
    {
        return inventory;
    }

    // Retourne l'interactable actuellement ciblé
    public IInteractable GetCurrentInteractable()
    {
        return currentInteractable;
    }

    // Méthodes pour les triggers (utilisées par les PNJ)
    public void SetCurrentInteractable(IInteractable interactable)
    {
        triggerInteractable = interactable;
    }

    public void ClearCurrentInteractable(IInteractable interactable)
    {
        if (triggerInteractable == interactable)
            triggerInteractable = null;
    }
}
