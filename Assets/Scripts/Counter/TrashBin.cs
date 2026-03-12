using UnityEngine;

public class TrashBin : MonoBehaviour, IInteractable
{
    [SerializeField] private AudioClip trashSound;
    [SerializeField] private SpriteRenderer trashSprite;

    public void Interact(PlayerInteraction player)
    {
        InventorySystem playerInventory = player.GetComponent<InventorySystem>();

        // Si le joueur a un item, on le jette
        if (playerInventory.currentItem != null)
        {
            ItemData itemToTrash = playerInventory.currentItem;
            playerInventory.RemoveItem(itemToTrash);

            // Jouer un son de poubelle si défini
            if (trashSound != null)
            {
                AudioSource.PlayClipAtPoint(trashSound, transform.position);
            }

            // Mettre à jour l'UI pour ce joueur
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (InventoryUI.Instance != null && playerController != null)
            {
                InventoryUI.Instance.UpdatePlayerInventory(playerController.playerId, playerInventory);
            }

            Debug.Log("Item jeté à la poubelle : " + itemToTrash.name);
        }
    }
}
