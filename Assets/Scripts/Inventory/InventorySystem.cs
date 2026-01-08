using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    // Le joueur ne peut avoir qu'un seul item sur lui.
    public ItemData currentItem;
    [SerializeField] private AudioClip pickupSound;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

    }

    public void AddItem(ItemData item)
    {
        currentItem = item;
        if (audioSource != null && pickupSound != null)
        {
            audioSource.PlayOneShot(pickupSound);
        }
    }

    public bool HasItem(ItemData item) => currentItem == item;

    public void RemoveItem(ItemData item)
    {
        currentItem = null;
    }

    /// <summary>
    /// Définit l'item par son nom (pour sync réseau)
    /// </summary>
    public void SetItemByName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            currentItem = null;
            return;
        }

        if (ItemDatabase.Instance != null)
        {
            currentItem = ItemDatabase.Instance.GetItemByName(itemName);
        }
    }
}
