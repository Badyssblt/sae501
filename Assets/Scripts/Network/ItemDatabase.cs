using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base de données des items pour la synchronisation réseau.
/// Permet de retrouver un ItemData par son nom.
/// </summary>
public class ItemDatabase : MonoBehaviour
{
    private static ItemDatabase _instance;
    public static ItemDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                // Chercher dans la scène
                _instance = FindFirstObjectByType<ItemDatabase>();

                // Si toujours null, créer automatiquement
                if (_instance == null)
                {
                    GameObject go = new GameObject("ItemDatabase (Auto)");
                    _instance = go.AddComponent<ItemDatabase>();
                    _instance.BuildDatabase();
                    Debug.Log("[ItemDatabase] Créé automatiquement");
                }
            }
            return _instance;
        }
    }

    [Header("All Items")]
    [SerializeField] private ItemData[] allItems;

    private Dictionary<string, ItemData> itemsByName = new Dictionary<string, ItemData>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        // Construire le dictionnaire
        BuildDatabase();
    }

    private void BuildDatabase()
    {
        itemsByName.Clear();

        // Ajouter les items sérialisés dans l'Inspector
        if (allItems != null)
        {
            foreach (var item in allItems)
            {
                if (item != null && !string.IsNullOrEmpty(item.name))
                {
                    itemsByName[item.name] = item;
                }
            }
        }

        // Charger depuis Resources
        var resourceItems = Resources.LoadAll<ItemData>("");
        foreach (var item in resourceItems)
        {
            if (item != null && !string.IsNullOrEmpty(item.name) && !itemsByName.ContainsKey(item.name))
            {
                itemsByName[item.name] = item;
            }
        }

        // Fallback: chercher TOUS les ItemData chargés en mémoire (ScriptableObjects référencés dans la scène)
        if (itemsByName.Count == 0)
        {
            var allLoadedItems = Resources.FindObjectsOfTypeAll<ItemData>();
            foreach (var item in allLoadedItems)
            {
                if (item != null && !string.IsNullOrEmpty(item.name) && !itemsByName.ContainsKey(item.name))
                {
                    itemsByName[item.name] = item;
                }
            }
            Debug.Log($"[ItemDatabase] Fallback: {allLoadedItems.Length} ItemData trouvés en mémoire");
        }

        Debug.Log($"[ItemDatabase] {itemsByName.Count} items chargés");
    }

    /// <summary>
    /// Retrouve un ItemData par son nom
    /// </summary>
    public ItemData GetItemByName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return null;

        if (itemsByName.TryGetValue(itemName, out ItemData item))
        {
            return item;
        }

        Debug.LogWarning($"[ItemDatabase] Item non trouvé: {itemName}");
        return null;
    }

    /// <summary>
    /// Vérifie si un item existe
    /// </summary>
    public bool HasItem(string itemName)
    {
        return !string.IsNullOrEmpty(itemName) && itemsByName.ContainsKey(itemName);
    }
}
