using UnityEngine;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    [SerializeField] private ItemData item;

    [SerializeField] private Image itemIcon;

    private void Awake()
    {
        if (itemIcon == null)
        {
            // Cherche automatiquement un enfant nomm� "ItemIcon"
            itemIcon = transform.Find("ItemIcon")?.GetComponent<Image>();
        }
    }

    private void Start()
    {
        // Masquer l'icône au démarrage si aucun item n'est défini
        if (item == null)
        {
            Clear();
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
