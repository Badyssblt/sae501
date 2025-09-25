using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private IInteractable currentInteractable;

    public void OnInteract()
    {
        if (currentInteractable != null)
        {
            currentInteractable.Interact(this);
            InventoryUI.Instance.UpdateInventory();
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
}
