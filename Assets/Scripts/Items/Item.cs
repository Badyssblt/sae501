using UnityEngine;

public class Item : MonoBehaviour, IInteractable
{
    [SerializeField]
    private ItemData item;

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
        if (string.IsNullOrEmpty(networkId))
        {
            networkId = $"item_{transform.position.x:F1}_{transform.position.y:F1}_{item?.name ?? "unknown"}";
        }
    }

    public void Interact(PlayerInteraction player)
    {
        InventorySystem inventory = player.GetComponent<InventorySystem>();
        if (inventory != null && inventory.currentItem == null)
        {
            inventory.AddItem(item);
            var playerController = player.GetComponent<PlayerController>();
            if (InventoryUI.Instance != null && playerController != null)
            {
                InventoryUI.Instance.UpdatePlayerInventory(playerController.playerId, inventory);
            }
            Destroy(gameObject);
        }
    }
}
