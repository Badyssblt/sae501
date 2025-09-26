using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("Player Inventory Slots")]
    [SerializeField] private Slot[] playerSlots = new Slot[4]; // 4 slots pour 4 joueurs

    public static InventoryUI Instance;

    private void Awake()
    {
        Instance = this;

        // Cacher tous les slots au départ
        foreach (var slot in playerSlots)
        {
            if (slot != null)
                slot.gameObject.SetActive(false);
        }
    }

    // Appelé quand un joueur est spawné
    public void ActivatePlayerSlot(int playerId)
    {
        if (playerId >= 1 && playerId <= 4 && playerSlots[playerId - 1] != null)
        {
            playerSlots[playerId - 1].gameObject.SetActive(true);
        }
    }

    // Appelé quand un joueur quitte
    public void DeactivatePlayerSlot(int playerId)
    {
        if (playerId >= 1 && playerId <= 4 && playerSlots[playerId - 1] != null)
        {
            playerSlots[playerId - 1].gameObject.SetActive(false);
            playerSlots[playerId - 1].Clear();
        }
    }

    // Mettre à jour l'inventaire d'un joueur spécifique
    public void UpdatePlayerInventory(int playerId, InventorySystem inventory)
    {
        if (playerId >= 1 && playerId <= 4 && playerSlots[playerId - 1] != null && inventory != null)
        {
            playerSlots[playerId - 1].SetItem(inventory.currentItem);
        }
    }

    // Pour compatibilité avec l'ancien code
    public void UpdateInventory()
    {
        Debug.LogWarning("UpdateInventory() sans playerId est déprécié. Utilisez UpdatePlayerInventory(playerId, inventory)");
    }
}
