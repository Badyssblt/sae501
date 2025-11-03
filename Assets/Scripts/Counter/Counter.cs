using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Counter : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData currentItem;
    [SerializeField] private CounterTypeScriptable counterData;
    private List<ItemData> ingredientsOnCounter = new List<ItemData>();
    private SpriteRenderer itemToDisplay;
    private bool inRange = false;
    private PlayerInteraction player;
    private Coroutine transformCoroutine;

    // L'objet sur le comptoir (friteuse etc...)
    [SerializeField] private SpriteRenderer counterObject;


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
        counterObject.sprite = counterData.counterSprite;
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
            if(!counterData.itemNeedHidden)
            {
                itemToDisplay.sprite = currentItem.sprite;
            }else
            {
                itemToDisplay.sprite = null;
            }
        }
    }

    private void Serve(InventorySystem playerInventory, PlayerInteraction player)
    {
        var orders = OrderManager.Instance.currentOrders;

        foreach(RecipeData order in orders)
        {
            if (order.result == playerInventory.currentItem)
            {
                // Utiliser CompletePNJOrder qui gère automatiquement les commandes PNJ et normales
                OrderManager.Instance.CompletePNJOrder(order, playerInventory, player);
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

        PlayerController playerController = player.GetComponent<PlayerController>();
        int playerId = playerController.playerId;

        // Vérifie si le précédent item a été finit de craft
        if (wasItemCrafted)
        {
            playerInventory.AddItem(currentItem);
            currentItem = null;
            UpdateVisual();
            // Mettre à jour l'UI pour ce joueur
            if (InventoryUI.Instance != null && playerController != null)
            {
                InventoryUI.Instance.UpdatePlayerInventory(playerController.playerId, playerInventory);
            }
            wasItemCrafted = false;
            return;
        }
        if (itemToTransform.counterType != counterData.type)
        {
            Debug.Log("Mauvais comptoir !");
            return;
        }

        currentItem = itemToTransform;
        playerInventory.RemoveItem(itemToTransform);
        InventoryUI.Instance.UpdatePlayerInventory(playerId, playerInventory);
        UpdateVisual();

        if (itemToTransform && itemToTransform.counterType == CounterType.Assemblage) return;

        // Si une coroutine était déjà en cours, on l’arrête
        if (transformCoroutine != null)
            StopCoroutine(transformCoroutine);

        transformCoroutine = StartCoroutine(WaitForTransform(itemToTransform, playerInventory));
    }

    IEnumerator WaitForTransform(ItemData itemToTransform, InventorySystem playerInventory)
    {
        PlayerMovement playerMovement = playerInventory.GetComponent<PlayerMovement>();
        SliderTime sliderTime = GetComponent<SliderTime>();
        PlayerController playerController = playerInventory.GetComponent<PlayerController>();
        if (counterData.needPlayerFreeze)
        {
            playerMovement.Freeze();
        }
        // Démarrer le slider timer
        sliderTime.StartTimer(itemToTransform.secondsToTransform);
        AudioSource audioClip = playerMovement.GetComponent<AudioSource>();
        audioClip.PlayOneShot(itemToTransform.soundToTransform);
        float elapsed = 0f;
        while (elapsed < itemToTransform.secondsToTransform)
        {
            if (!inRange && counterData.needPlayerFreeze) // joueur est sorti → on stoppe
            {
                sliderTime.StopTimer();
                playerInventory.AddItem(itemToTransform);
                currentItem = null;
                UpdateVisual();
                InventoryUI.Instance.UpdatePlayerInventory(playerController.playerId, playerInventory);
                transformCoroutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Transformation finie
        sliderTime.HideSlider();
        currentItem = itemToTransform.itemCrafted;
        wasItemCrafted = true;
        UpdateVisual();
        playerMovement.Unfreeze();

        transformCoroutine = null;
    }




    public void Interact(PlayerInteraction player)
    {
        if (!inRange) return;

        InventorySystem playerInventory = player.GetComponent<InventorySystem>();
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();

        if (counterData.type == CounterType.Assemblage)
        {
            Crafting(playerInventory, player);
            return;
        }else if(counterData.type == CounterType.Service)
        {
            Serve(playerInventory, player);
        }
        else
        {
            TransformItem(playerInventory, player);
        }

    }

}
