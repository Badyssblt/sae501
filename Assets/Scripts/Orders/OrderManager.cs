using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class OrderManager : MonoBehaviour
{
    // Classe pour lier une commande à un PNJ
    [System.Serializable]
    public class PNJOrder
    {
        public RecipeData recipe;
        public PNJClient client;
        public GameObject orderUI;

        public PNJOrder(RecipeData recipe, PNJClient client, GameObject orderUI)
        {
            this.recipe = recipe;
            this.client = client;
            this.orderUI = orderUI;
        }
    }

    private RecipeData[] recipes;
    public List<RecipeData> currentOrders = new List<RecipeData>();
    public List<PNJOrder> pnjOrders = new List<PNJOrder>(); // Commandes des PNJ
    private HorizontalLayoutGroup hb;

    [SerializeField] private int maxOrders = 3;
    [SerializeField] private float orderInterval = 10f;

    [SerializeField] private GameObject orderPrefab;

    public static OrderManager Instance;

    private void Start()
    {
        Instance = this;
        recipes = GameManager.Instance.recipes;
        hb = GetComponent<HorizontalLayoutGroup>();
    }

    // Ces méthodes ne sont plus utilisées - les commandes sont créées par les PNJ
    /*
    public IEnumerator OrderRoutine()
    {
        while (true)
        {
            GenerateRandomOrders(maxOrders);
            yield return new WaitForSeconds(orderInterval);
        }
    }

    private void GenerateRandomOrders(int numberOfOrders)
    {
        // D'abord on nettoie les anciens UI
        foreach (Transform child in hb.transform)
        {
            Destroy(child.gameObject);
        }
        currentOrders.Clear();

        if (recipes.Length == 0) return;

        List<RecipeData> tempList = new List<RecipeData>(recipes);

        for (int i = 0; i < numberOfOrders; i++)
        {
            if (tempList.Count == 0) break;
            int randomIndex = Random.Range(0, tempList.Count);
            RecipeData order = tempList[randomIndex];
            currentOrders.Add(order);
            tempList.RemoveAt(randomIndex);

            // Cr�e l�UI pour chaque commande
            var newOrderGO = Instantiate(orderPrefab, hb.transform);
            OrderUI orderGO = newOrderGO.GetComponent<OrderUI>();
            orderGO.recipe = order;
            orderGO.maxDelay = 10f;
            orderGO.UpdateRecipe();
        }
    }
    */

    public void CompleteOrder(RecipeData order, InventorySystem playerInventory, PlayerInteraction player)
    {
        currentOrders.Remove(order);
        // Supprime le bon order correspondant
        foreach (Transform child in hb.transform)
        {
            OrderUI orderUI = child.GetComponent<OrderUI>();
            if (orderUI != null && orderUI.recipe == order)
            {
                Destroy(child.gameObject);
                GameManager.Instance.AddScore(playerInventory.currentItem.scoreCount);
                playerInventory.RemoveItem(playerInventory.currentItem);
                // Mettre à jour l'UI pour ce joueur
                var playerController = player.GetComponent<PlayerController>();
                if (InventoryUI.Instance != null && playerController != null)
                {
                    InventoryUI.Instance.UpdatePlayerInventory(playerController.playerId, playerInventory);
                }
                break;
            }
        }
    }

    public List<RecipeData> GetCurrentOrders()
    {
        return currentOrders;
    }

    // Crée une commande spécifique pour un PNJ
    public RecipeData CreatePNJOrder(PNJClient client)
    {
        if (recipes.Length == 0) return null;

        // Choisir une recette aléatoire
        RecipeData randomRecipe = recipes[Random.Range(0, recipes.Length)];

        // Créer l'UI pour cette commande
        var newOrderGO = Instantiate(orderPrefab, hb.transform);
        OrderUI orderUI = newOrderGO.GetComponent<OrderUI>();
        orderUI.recipe = randomRecipe;
        orderUI.maxDelay = client.tempsPourManger; // Utilise le temps d'attente du client
        orderUI.UpdateRecipe();

        // Ajouter à la liste des commandes
        currentOrders.Add(randomRecipe);
        pnjOrders.Add(new PNJOrder(randomRecipe, client, newOrderGO));

        Debug.Log("Commande créée pour " + client.name + " : " + randomRecipe.result.name);

        return randomRecipe;
    }

    // Complète une commande de PNJ
    public void CompletePNJOrder(RecipeData order, InventorySystem playerInventory, PlayerInteraction player)
    {
        // Chercher si cette commande appartient à un PNJ
        PNJOrder pnjOrder = pnjOrders.Find(o => o.recipe == order);

        if (pnjOrder != null)
        {
            // Notifier le PNJ qu'il a reçu sa commande
            pnjOrder.client.RecevoirCommande();

            // Retirer de la liste
            currentOrders.Remove(order);
            pnjOrders.Remove(pnjOrder);

            // Détruire l'UI
            if (pnjOrder.orderUI != null)
            {
                Destroy(pnjOrder.orderUI);
            }

            // Ajouter le score
            GameManager.Instance.AddScore(playerInventory.currentItem.scoreCount);
            playerInventory.RemoveItem(playerInventory.currentItem);

            // Mettre à jour l'UI pour ce joueur
            var playerController = player.GetComponent<PlayerController>();
            if (InventoryUI.Instance != null && playerController != null)
            {
                InventoryUI.Instance.UpdatePlayerInventory(playerController.playerId, playerInventory);
            }

            // Notifier l'EffectManager du succès
            EffectManager.Instance?.NotifierSucces();

            Debug.Log("Commande livrée au PNJ " + pnjOrder.client.name);
        }
        else
        {
            // Si ce n'est pas une commande PNJ, utiliser l'ancien système
            CompleteOrder(order, playerInventory, player);
        }
    }

    // Retire une commande PNJ expirée
    public void RemoveExpiredPNJOrder(PNJClient client)
    {
        PNJOrder pnjOrder = pnjOrders.Find(o => o.client == client);
        if (pnjOrder != null)
        {
            currentOrders.Remove(pnjOrder.recipe);
            pnjOrders.Remove(pnjOrder);

            if (pnjOrder.orderUI != null)
            {
                Destroy(pnjOrder.orderUI);
            }

            // Notifier l'EffectManager de l'échec
            EffectManager.Instance?.NotifierEchec();

            Debug.Log("Commande expirée pour " + client.name);
        }
    }

    // Nettoie toutes les commandes en cours (appelé à la fin de la partie)
    public void ClearAllOrders()
    {
        // Détruire tous les UI des commandes
        foreach (Transform child in hb.transform)
        {
            Destroy(child.gameObject);
        }

        // Vider les listes
        currentOrders.Clear();
        pnjOrders.Clear();

        Debug.Log("Toutes les commandes ont été nettoyées !");
    }

    // ============================================================
    // NETWORK - Retourne les commandes actives pour la synchronisation
    // ============================================================

    /// <summary>
    /// Structure pour les données de commande réseau
    /// </summary>
    public struct OrderInfo
    {
        public string id;
        public string recipeName;
        public float timeRemaining;
        public string status;
    }

    /// <summary>
    /// Retourne les commandes actives sous forme sérialisable pour le réseau
    /// </summary>
    public List<OrderInfo> GetActiveOrders()
    {
        var orders = new List<OrderInfo>();

        foreach (var pnjOrder in pnjOrders)
        {
            if (pnjOrder.orderUI != null)
            {
                var orderUI = pnjOrder.orderUI.GetComponent<OrderUI>();
                float timeRemaining = orderUI != null ? orderUI.GetTimeRemaining() : 0f;

                orders.Add(new OrderInfo
                {
                    id = $"order_{pnjOrder.client?.GetInstanceID() ?? 0}",
                    recipeName = pnjOrder.recipe?.result?.name ?? "Unknown",
                    timeRemaining = timeRemaining,
                    status = "pending"
                });
            }
        }

        return orders;
    }

    // ============================================================
    // NETWORK - Synchronisation des commandes côté client
    // ============================================================

    // Commandes réseau trackées par ID pour sync
    private Dictionary<string, GameObject> networkOrderUIs = new Dictionary<string, GameObject>();

    /// <summary>
    /// Applique l'état des commandes reçu du serveur (mode client uniquement)
    /// </summary>
    public void ApplyNetworkOrders(Dictionary<string, CookMoiCa.Network.OrderState> serverOrders)
    {
        if (recipes == null) recipes = GameManager.Instance.recipes;
        if (hb == null) hb = GetComponent<HorizontalLayoutGroup>();
        if (hb == null || orderPrefab == null) return;

        // Supprimer les commandes qui n'existent plus sur le serveur
        var toRemove = new List<string>();
        foreach (var kvp in networkOrderUIs)
        {
            if (!serverOrders.ContainsKey(kvp.Key))
            {
                if (kvp.Value != null) Destroy(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var key in toRemove)
        {
            networkOrderUIs.Remove(key);
        }

        // Ajouter ou mettre à jour les commandes du serveur
        foreach (var kvp in serverOrders)
        {
            string orderId = kvp.Key;
            var orderState = kvp.Value;

            if (!networkOrderUIs.ContainsKey(orderId) || networkOrderUIs[orderId] == null)
            {
                // Nouvelle commande - trouver la recette correspondante
                RecipeData matchingRecipe = FindRecipeByResultName(orderState.recipeName);
                if (matchingRecipe == null) continue;

                // Créer l'UI
                var newOrderGO = Instantiate(orderPrefab, hb.transform);
                OrderUI orderUI = newOrderGO.GetComponent<OrderUI>();
                orderUI.recipe = matchingRecipe;
                orderUI.maxDelay = orderState.timeRemaining;
                orderUI.UpdateRecipe();

                networkOrderUIs[orderId] = newOrderGO;

                // Ajouter à la liste des commandes courantes si pas déjà présent
                if (!currentOrders.Contains(matchingRecipe))
                {
                    currentOrders.Add(matchingRecipe);
                }
            }
            else
            {
                // Commande existante - mettre à jour le timer
                var existingUI = networkOrderUIs[orderId].GetComponent<OrderUI>();
                if (existingUI != null)
                {
                    existingUI.SetTimeRemaining(orderState.timeRemaining);
                }
            }
        }
    }

    /// <summary>
    /// Trouve une recette par le nom de son résultat
    /// </summary>
    public RecipeData FindRecipeByResultName(string resultName)
    {
        if (recipes == null || string.IsNullOrEmpty(resultName)) return null;

        foreach (var recipe in recipes)
        {
            if (recipe.result != null && recipe.result.name == resultName)
                return recipe;
        }
        return null;
    }
}
