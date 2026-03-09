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
    [SerializeField] private float proximityRadius = 1.0f;

    // Angle max (en degrés) entre la direction du regard et la direction vers un interactable proche
    // pour qu'il soit considéré comme valide. 90° = devant + côtés, 120° = plus tolérant.
    [SerializeField] private float proximityAngleThreshold = 110f;

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
        // Mettre à jour la direction du regard uniquement quand le joueur se déplace
        Vector2 movement = playerController.GetCurrentMovement();
        if (movement != Vector2.zero)
        {
            lastFacingDirection = movement.normalized;
        }

        DetectInteractable();
    }

    private void DetectInteractable()
    {
        IInteractable best = FindBestInteractable();

        // Gérer le highlight : retirer le highlight de l'ancien, l'appliquer au nouveau
        if (best != currentInteractable)
        {
            (currentInteractable as IHighlightable)?.SetHighlight(false);
            (best as IHighlightable)?.SetHighlight(true);
            currentInteractable = best;
        }
    }

    private IInteractable FindBestInteractable()
    {
        // Priorité 1 : Trigger (PNJ) – ils se gèrent eux-mêmes
        if (triggerInteractable != null)
            return triggerInteractable;

        // Priorité 2 : Raycast dans la direction du regard (précis, longue portée)
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
                return interactable;
        }

        // Priorité 3 : Proximité avec filtre angulaire
        // Trouve le comptoir le plus proche devant le joueur (dans le cône de direction)
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, proximityRadius, interactableLayer);

        float bestScore = float.MaxValue; // score = distance pondérée par angle
        IInteractable closestInteractable = null;

        foreach (Collider2D col in nearbyColliders)
        {
            IInteractable interactable = col.GetComponent<IInteractable>();
            if (interactable == null) continue;

            Vector2 dirToTarget = ((Vector2)col.transform.position - (Vector2)transform.position).normalized;
            float angle = Vector2.Angle(lastFacingDirection, dirToTarget);

            // Ignorer les interactables hors du cône de tolérance angulaire
            if (angle > proximityAngleThreshold) continue;

            float dist = Vector2.Distance(transform.position, col.transform.position);

            // Score : combinaison distance + angle (favorise ce qui est devant ET proche)
            float score = dist + angle * 0.02f;

            if (score < bestScore)
            {
                bestScore = score;
                closestInteractable = interactable;
            }
        }

        return closestInteractable;
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

    /// <summary>
    /// Permet aux objets interactables d'accéder à l'inventaire du joueur.
    /// </summary>
    public InventorySystem GetInventory()
    {
        return inventory;
    }

    /// <summary>
    /// Retourne l'interactable actuellement ciblé.
    /// </summary>
    public IInteractable GetCurrentInteractable()
    {
        return currentInteractable;
    }

    /// <summary>
    /// Enregistre un interactable via trigger (PNJ).
    /// </summary>
    public void SetCurrentInteractable(IInteractable interactable)
    {
        triggerInteractable = interactable;
    }

    /// <summary>
    /// Retire un interactable enregistré via trigger (PNJ).
    /// </summary>
    public void ClearCurrentInteractable(IInteractable interactable)
    {
        if (triggerInteractable == interactable)
            triggerInteractable = null;
    }

    private void OnDisable()
    {
        // Nettoyer le highlight si le joueur est désactivé
        (currentInteractable as IHighlightable)?.SetHighlight(false);
        currentInteractable = null;
    }
}
