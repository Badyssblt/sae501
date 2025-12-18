using UnityEngine;

public class Item : MonoBehaviour, IInteractable
{
    [SerializeField]
    private ItemData item;

    private BoxCollider2D collider;
    private bool inRange = false;

    private PlayerInteraction player;

    // ============================================================
    // NETWORK - ID pour synchronisation
    // ============================================================

    [Header("Network")]
    [SerializeField] private string networkId;
    private static int itemIdCounter = 0;

    /// <summary>
    /// ID unique pour la synchronisation réseau
    /// </summary>
    public string NetworkId
    {
        get
        {
            if (string.IsNullOrEmpty(networkId))
            {
                networkId = $"item_{itemIdCounter++}_{item?.name ?? "unknown"}";
            }
            return networkId;
        }
    }

    /// <summary>
    /// Retourne les données de l'item
    /// </summary>
    public ItemData ItemData => item;

    private void Start()
    {
        collider = GetComponent<BoxCollider2D>();

        // Générer un ID basé sur la position et le type si pas défini
        if (string.IsNullOrEmpty(networkId))
        {
            networkId = $"item_{transform.position.x:F1}_{transform.position.y:F1}_{item?.name ?? "unknown"}";
        }
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
