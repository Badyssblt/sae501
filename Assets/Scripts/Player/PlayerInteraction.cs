using UnityEngine;

[RequireComponent(typeof(InventorySystem))]
public class PlayerInteraction : MonoBehaviour
{
    private IInteractable currentInteractable;
    private InventorySystem inventory;
    private PlayerController playerController;

    private void Awake()
    {
        inventory = GetComponent<InventorySystem>();
        if (inventory == null)
        {
            inventory = gameObject.AddComponent<InventorySystem>();
        }

        playerController = GetComponent<PlayerController>();
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

    public void SetCurrentInteractable(IInteractable interactable)
    {
        currentInteractable = interactable;
    }

    public void ClearCurrentInteractable(IInteractable interactable)
    {
        if (currentInteractable == interactable)
            currentInteractable = null;
    }

    // Permet aux objets interactables d'accéder à l'inventaire du joueur
    public InventorySystem GetInventory()
    {
        return inventory;
    }
}
