using System.Collections.Generic;
using UnityEngine;

public class Counter : MonoBehaviour, IInteractable
{
    [SerializeField] private List<ItemData> items = new List<ItemData>(); // pour l'affichage
    private List<ItemData> ingredientsOnCounter = new List<ItemData>();     // tous les ingrédients
    private SpriteRenderer itemToDisplay;
    private bool inRange = false;
    private PlayerInteraction player;

    private void Awake()
    {
        itemToDisplay = transform.Find("ItemDisplayed").gameObject.GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (inRange && Input.GetButtonDown("P1_B1"))
        {
            Interact(player);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            inRange = true;
            player = collision.GetComponent<PlayerInteraction>();
            if (player != null)
                player.SetCurrentInteractable(this);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            inRange = false;
            PlayerInteraction player = collision.GetComponent<PlayerInteraction>();
            if (player != null)
                player.ClearCurrentInteractable(this);
        }
    }

    private void UpdateVisual()
    {
        if (items.Count == 0)
            itemToDisplay.sprite = null;
        else
            itemToDisplay.sprite = items[items.Count - 1].sprite;
    }

    public void Interact(PlayerInteraction player)
    {
        if (!inRange) return;

        InventorySystem playerInventory = player.GetComponent<InventorySystem>();

        // Prendre le dernier item affiché
        if (playerInventory.currentItem == null && items.Count > 0)
        {
            ItemData lastItem = items[items.Count - 1];
            playerInventory.AddItem(lastItem);
            // Ne touche pas ingredientsOnCounter pour garder la trace
            items.RemoveAt(items.Count - 1);
            UpdateVisual();
            InventoryUI.Instance.UpdateInventory();
            return;
        }

        // Poser un nouvel ingrédient
        if (playerInventory.currentItem != null)
        {
            ItemData newIngredient = playerInventory.currentItem;
            playerInventory.RemoveItem(newIngredient);

            // Si le comptoir a déjà un résultat affiché et qu'aucune recette ne correspond avec le nouvel ingrédient
            ItemData newResult = null;
            foreach (var recipe in GameManager.Instance.recipes)
            {
                // On teste avec tous les ingrédients actuels + le nouveau
                List<ItemData> testIngredients = new List<ItemData>(ingredientsOnCounter) { newIngredient };
                if (recipe.Matches(testIngredients))
                {
                    newResult = recipe.result;
                    break;
                }
            }

            if (newResult != null)
            {
                ingredientsOnCounter.Add(newIngredient);
                if (items.Count == 0)
                    items.Add(newResult);
                else
                    items[0] = newResult; // remplace le résultat actuel
            }
            else
            {
                if (items.Count > 0)
                {
                    // On rend l'item actuel au joueur
                    ItemData currentDisplayed = items[0];
                    playerInventory.AddItem(currentDisplayed);

                    // On remplace l'affichage par le nouvel ingrédient
                    items[0] = newIngredient;
                    // On remplace aussi la liste d'ingrédients pour garder cohérence
                    ingredientsOnCounter.Clear();
                    ingredientsOnCounter.Add(newIngredient);
                }
                else
                {
                    ingredientsOnCounter.Add(newIngredient);
                    items.Add(newIngredient);
                }
            }

            UpdateVisual();
            InventoryUI.Instance.UpdateInventory();
        }
    }

}
