using UnityEngine;

public class ItemGiver : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData itemToGive;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = itemToGive.sprite;
    }

    public void Interact(PlayerInteraction player)
    {
        // Récupérer l'inventaire du joueur qui interagit
        var inventory = player.GetInventory();
        if (inventory != null && itemToGive != null)
        {
            // Bloquer si le joueur tient un item crafté (pas un ingrédient de base)
            if (inventory.currentItem != null && !inventory.itemIsFromGiver)
                return;

            inventory.AddItem(itemToGive, fromGiver: true);

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
