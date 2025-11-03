using UnityEngine;

public class TrashBin : MonoBehaviour, IInteractable
{
    [SerializeField] private AudioClip trashSound;
    [SerializeField] private SpriteRenderer trashSprite;

    private bool inRange = false;
    private PlayerInteraction player;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            inRange = true;
            player = collision.GetComponent<PlayerInteraction>();
            if (player != null)
                player.SetCurrentInteractable(this);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            inRange = false;
            PlayerInteraction exitPlayer = collision.GetComponent<PlayerInteraction>();
            if (exitPlayer != null)
                exitPlayer.ClearCurrentInteractable(this);
        }
    }

    public void Interact(PlayerInteraction player)
    {
        if (!inRange) return;

        InventorySystem playerInventory = player.GetComponent<InventorySystem>();

        // Si le joueur a un item, on le jette
        if (playerInventory.currentItem != null)
        {
            ItemData itemToTrash = playerInventory.currentItem;
            playerInventory.RemoveItem(itemToTrash);

            // Jouer un son de poubelle si défini
            if (trashSound != null)
            {
                AudioSource audioSource = GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(trashSound);
                }
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
