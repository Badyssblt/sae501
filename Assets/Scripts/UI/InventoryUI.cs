using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField]
    private Slot slot;

    [SerializeField]
    private InventorySystem inventorySystem;

    public static InventoryUI Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void UpdateInventory()
    {
        ItemData item = inventorySystem.currentItem;
        slot.SetItem(item); 
    }

}
