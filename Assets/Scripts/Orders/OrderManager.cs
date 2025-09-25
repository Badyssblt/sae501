using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class OrderManager : MonoBehaviour
{
    private RecipeData[] recipes;
    private List<RecipeData> currentOrders = new List<RecipeData>();
    private HorizontalLayoutGroup hb;

    [SerializeField] private float maxOrders = 3;
    [SerializeField] private float orderInterval = 10f;

    [SerializeField] private GameObject orderPrefab;

    private void Start()
    {
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

    private void GenerateRandomOrders(float numberOfOrders)
    {
        currentOrders.Clear();

        if (recipes.Length == 0) return;

        List<RecipeData> tempList = new List<RecipeData>(recipes);

        for (int i = 0; i < numberOfOrders; i++)
        {
            if (tempList.Count == 0) break;
            int randomIndex = Random.Range(0, tempList.Count);
            currentOrders.Add(tempList[randomIndex]);
            tempList.RemoveAt(randomIndex); // Pour éviter les doublons
        }

        // Debug : affiche les nouvelles commandes
        foreach (RecipeData order in currentOrders)
        {
            var newOrderGO = Instantiate(orderPrefab, hb.transform);
            OrderUI orderGO = newOrderGO.GetComponent<OrderUI>();
            orderGO.recipe = order;
            orderGO.maxDelay = Random.Range(10f, 20f);
            orderGO.UpdateRecipe();
        }
    }

    public List<RecipeData> GetCurrentOrders()
    {
        return currentOrders;
    }
}
