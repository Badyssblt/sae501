using UnityEngine;

public class TrashBin : MonoBehaviour, IInteractable
{
    [SerializeField] private AudioClip trashSound;
    [SerializeField] private SpriteRenderer trashSprite;

    private void Awake()
    {
        // S'assurer que la poubelle s'affiche au-dessus du tilemap (sortingOrder 1)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Main";
            sr.sortingOrder = 2;
        }
    }

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

            Debug.Log("Item jeté à la poubelle : " + itemToTrash.name);
        }
    }
}
