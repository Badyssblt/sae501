using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    // Le joueur ne peut avoir qu'un seul item sur lui.
    [SerializeField]
    private ItemData currentItem;

    public void AddItem(ItemData item)
    {
        currentItem = item;
        // Implémentation du drop de l'ancien item ?
    }

    public bool HasItem(ItemData item) => currentItem == item;

    public void RemoveItem(ItemData item)
    {
        currentItem = null;
    }
}
