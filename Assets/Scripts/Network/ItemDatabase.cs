using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base de données des items pour la synchronisation réseau.
/// Permet de retrouver un ItemData par son nom.
/// </summary>
public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    [Header("All Items")]
    [SerializeField] private ItemData[] allItems;

    private Dictionary<string, ItemData> itemsByName = new Dictionary<string, ItemData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Construire le dictionnaire
        BuildDatabase();
    }

    private void BuildDatabase()
    {
        itemsByName.Clear();

        // Ajouter les items sérialisés
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

        // Charger aussi tous les ItemData des Resources si disponibles
        var resourceItems = Resources.LoadAll<ItemData>("");
        foreach (var item in resourceItems)
        {
            if (item != null && !string.IsNullOrEmpty(item.name) && !itemsByName.ContainsKey(item.name))
            {
                itemsByName[item.name] = item;
            }
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
