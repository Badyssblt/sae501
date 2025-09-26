using UnityEngine;

public class ItemGiver : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData itemToGive;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            var playerInteraction = collision.GetComponent<PlayerInteraction>();
            if (playerInteraction != null)
            {
                playerInteraction.SetCurrentInteractable(this);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            var playerInteraction = collision.GetComponent<PlayerInteraction>();
            if (playerInteraction != null)
            {
                playerInteraction.ClearCurrentInteractable(this);
            }
        }
    }

    public void Interact(PlayerInteraction player)
    {
        // Récupérer l'inventaire du joueur qui interagit
        var inventory = player.GetInventory();
        if (inventory != null && itemToGive != null)
        {
            inventory.AddItem(itemToGive);

            // Mettre à jour l'UI pour CE joueur spécifiquement
            var playerController = player.GetComponent<PlayerController>();
            if (playerController != null && InventoryUI.Instance != null)
            {
                InventoryUI.Instance.UpdatePlayerInventory(playerController.playerId, inventory);
            }

            Debug.Log($"Joueur {playerController?.playerId} a ramassé {itemToGive.name}");
        }
    }
}
