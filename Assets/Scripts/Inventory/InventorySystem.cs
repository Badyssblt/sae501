using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    // Le joueur ne peut avoir qu'un seul item sur lui.
    public ItemData currentItem;
    public bool itemIsFromGiver { get; private set; }
    [SerializeField] private AudioClip pickupSound;
    private AudioSource audioSource;
    private ItemHolder itemHolder;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        itemHolder = GetComponent<ItemHolder>();
    }

    public void AddItem(ItemData item, bool fromGiver = false)
    {
        currentItem = item;
        itemIsFromGiver = fromGiver;
        if (audioSource != null && pickupSound != null)
        {
            audioSource.PlayOneShot(pickupSound);
        }
        itemHolder?.ShowItem(currentItem);
    }

    public bool HasItem(ItemData item) => currentItem == item;

    public void RemoveItem(ItemData item)
    {
        currentItem = null;
        itemHolder?.Clear();
    }

    /// <summary>
    /// Définit l'item par son nom (pour sync réseau)
    /// </summary>
    public void SetItemByName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            currentItem = null;
            itemHolder?.Clear();
            return;
        }

        if (ItemDatabase.Instance != null)
        {
            currentItem = ItemDatabase.Instance.GetItemByName(itemName);
            itemHolder?.ShowItem(currentItem);
        }
    }
}
