using UnityEngine;

public class Item : MonoBehaviour, IInteractable
{
    [SerializeField]
    private ItemData item;

    private BoxCollider2D collider;
    private bool inRange = false;

    private PlayerInteraction player;

    private void Start()
    {
        collider = GetComponent<BoxCollider2D>();
    }

    public void Interact(PlayerInteraction player)
    {
        InventorySystem inventory = player.GetComponent<InventorySystem>();
        if (inventory != null && inRange && inventory.currentItem == null)
        {
            inventory.AddItem(item);
            // Mettre à jour l'UI pour ce joueur
            var playerController = player.GetComponent<PlayerController>();
            if (InventoryUI.Instance != null && playerController != null)
            {
                InventoryUI.Instance.UpdatePlayerInventory(playerController.playerId, inventory);
            }
            Destroy(gameObject);
        }
    }

    // Plus besoin d'Update, l'interaction se fait via PlayerInteraction.OnInteract()

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            inRange = true;
            player = collision.GetComponent<PlayerInteraction>();
            player.SetCurrentInteractable(this);
           
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            inRange = false;
            PlayerInteraction player = collision.GetComponent<PlayerInteraction>();
            if (player != null)
            {
                player.ClearCurrentInteractable(this);
            }
        }
    }
}
