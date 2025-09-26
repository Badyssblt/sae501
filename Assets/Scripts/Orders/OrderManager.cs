using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class OrderManager : MonoBehaviour
{
    private RecipeData[] recipes;
    public List<RecipeData> currentOrders = new List<RecipeData>();
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
        StartCoroutine(OrderRoutine());
    }

    private IEnumerator OrderRoutine()
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
            orderGO.maxDelay = Random.Range(10f, 20f);
            orderGO.UpdateRecipe();
        }
    }

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
}
