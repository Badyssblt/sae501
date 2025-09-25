using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    [SerializeField] private ItemData item;

    [SerializeField] private Image itemIcon; // assigner dans l'inspecteur, l'enfant "ItemIcon"

    private void Awake()
    {
        if (itemIcon == null)
        {
            // Cherche automatiquement un enfant nommé "ItemIcon"
            itemIcon = transform.Find("ItemIcon")?.GetComponent<Image>();
        }
    }

    public void SetItem(ItemData newItem)
    {
        item = newItem;

        if (item != null && itemIcon != null)
        {
            itemIcon.sprite = item.sprite;
            itemIcon.enabled = true;
        }
        else
        {
            Clear();
        }
    }

    public void Clear()
    {
        item = null;
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }
    }

    public ItemData GetItem()
    {
        return item;
    }
}
