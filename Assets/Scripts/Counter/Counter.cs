using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Counter : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData currentItem; // pour l'affichage
    private List<ItemData> ingredientsOnCounter = new List<ItemData>();     // tous les ingrédients
    private SpriteRenderer itemToDisplay;
    private bool inRange = false;
    private PlayerInteraction player;

    public CounterType type;

    private bool wasItemCrafted = false;

    private void Awake()
    {
        Transform itemTransform = transform.Find("ItemDisplayed");
        if (itemTransform != null)
        {
            itemToDisplay = itemTransform.gameObject.GetComponent<SpriteRenderer>();
        }
        else
        {
            itemToDisplay = null;
        }
    }


    // Plus besoin d'Update, l'interaction se fait via PlayerInteraction.OnInteract()

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
        if (currentItem == null)
        {
            itemToDisplay.sprite = null;
        }
        else
        {

            itemToDisplay.sprite = currentItem.sprite;
        }
    }

    private void Serve(InventorySystem playerInventory, PlayerInteraction player)
    {
        var orders = OrderManager.Instance.currentOrders;

        foreach(RecipeData order in orders)
        {
            if (order.result == playerInventory.currentItem)
            {
                OrderManager.Instance.CompleteOrder(order, playerInventory, player);
                break;
            }
        }
    }


    private void Crafting(InventorySystem playerInventory, PlayerInteraction player)
    {
        // Prendre l'item affiché si le joueur n'en a pas
        if (playerInventory.currentItem == null && currentItem != null)
        {
            playerInventory.AddItem(currentItem);
            // Ne touche pas ingredientsOnCounter pour garder la trace
            currentItem = null;
            UpdateVisual();
            // Mettre à jour l'UI pour ce joueur
            var playerController = player.GetComponent<PlayerController>();
            if (InventoryUI.Instance != null && playerController != null)
            {
                InventoryUI.Instance.UpdatePlayerInventory(playerController.playerId, playerInventory);
            }
            return;
        }



        // Poser un nouvel ingrédient
        if (playerInventory.currentItem != null)
        {
            ItemData newIngredient = playerInventory.currentItem;
            playerInventory.RemoveItem(newIngredient);

            // Vérifier si une recette correspond avec les ingrédients actuels + le nouveau
            List<ItemData> testIngredients = new List<ItemData>(ingredientsOnCounter) { newIngredient };
            ItemData newResult = null;

            foreach (var recipe in GameManager.Instance.recipes)
            {
                if (recipe.Matches(testIngredients))
                {
                    newResult = recipe.result;
                    break;
                }
            }



            if (newResult != null)
            {
                // Recette trouvée
                ingredientsOnCounter.Add(newIngredient);
                currentItem = newResult; // afficher le résultat de la recette
            }
            else
            {
                // Pas de recette correspondante
                if (currentItem != null)
                {
                    // Rendre l'ancien item affiché au joueur
                    playerInventory.AddItem(currentItem);

                    // Poser le nouvel ingrédient sur le comptoir
                    currentItem = newIngredient;

                    // Réinitialiser les ingrédients du comptoir
                    ingredientsOnCounter.Clear();
                    ingredientsOnCounter.Add(newIngredient);
                }
                else
                {

                    // Comptoir vide → poser le nouvel ingrédient
                    currentItem = newIngredient;
                    ingredientsOnCounter.Add(newIngredient);
                }
            }

            UpdateVisual();
            // Mettre à jour l'UI pour ce joueur
            var playerController = player.GetComponent<PlayerController>();
            if (InventoryUI.Instance != null && playerController != null)
            {
                InventoryUI.Instance.UpdatePlayerInventory(playerController.playerId, playerInventory);
            }
        }

        return;
    }


    private void TransformItem(InventorySystem playerInventory, PlayerInteraction player)
    {
        ItemData itemToTransform = playerInventory.currentItem;



        // Vérifie si le précédent item a été finit de craft
        if (wasItemCrafted)
        {
            playerInventory.AddItem(currentItem);
            currentItem = null;
            UpdateVisual();
            // Mettre à jour l'UI pour ce joueur
            var playerController = player.GetComponent<PlayerController>();
            if (InventoryUI.Instance != null && playerController != null)
            {
                InventoryUI.Instance.UpdatePlayerInventory(playerController.playerId, playerInventory);
            }
            wasItemCrafted = false;
            return;
        }

        if (itemToTransform.counterType != type)
        {
            Debug.Log("Mauvais comptoir !");
            return;
        }

        currentItem = itemToTransform;
        playerInventory.RemoveItem(itemToTransform);
        InventoryUI.Instance.UpdateInventory();
        UpdateVisual();

        // Essaye de mettre un objet que l'on doit crafter sur un Counter qui n'est pas fait pour ça
        if (itemToTransform && itemToTransform.counterType == CounterType.Assemblage) return;
        StartCoroutine(WaitForTransform(itemToTransform, playerInventory));



    }

    IEnumerator WaitForTransform(ItemData itemToTransform, InventorySystem playerInventory)
    {
        PlayerMovement playerMovement = playerInventory.GetComponent<PlayerMovement>();
        PlayerSlider playerSlider = playerInventory.GetComponent<PlayerSlider>();


        // Démarrer le slider timer
        playerSlider.StartTimer(itemToTransform.secondsToTransform);

        float elapsedTime = 0f;
        while (elapsedTime < itemToTransform.secondsToTransform)
        {
            elapsedTime += Time.deltaTime;
            playerSlider.currentTime = Mathf.Clamp(itemToTransform.secondsToTransform - elapsedTime, 0f, itemToTransform.secondsToTransform);
            yield return null;
        }

        playerSlider.HideSlider();

        currentItem = itemToTransform.itemCrafted;
        wasItemCrafted = true;
        UpdateVisual();
    }



    public void Interact(PlayerInteraction player)
    {
        Debug.Log(type);
        if (!inRange) return;

        InventorySystem playerInventory = player.GetComponent<InventorySystem>();
        if (type == CounterType.Assemblage)
        {
            Crafting(playerInventory, player);
            return;
        }else if(type == CounterType.Service)
        {
            Serve(playerInventory, player);
        }
        else
        {
            TransformItem(playerInventory, player);
        }

    }

}
