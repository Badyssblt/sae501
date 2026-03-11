using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CookMoiCa.Network;

public class Counter : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData currentItem;
    [SerializeField] private CounterTypeScriptable counterData;
    private List<ItemData> ingredientsOnCounter = new List<ItemData>();
    private SpriteRenderer itemToDisplay;
    private Coroutine transformCoroutine;

    // L'objet sur le comptoir (friteuse etc...)
    [SerializeField] private SpriteRenderer counterObject;

    // L'icône "prêt" qui s'affiche quand l'item est fini
    private SpriteRenderer readyIcon;
    private Vector3 readyIconInitialPosition;
    private Coroutine bounceCoroutine;
    [SerializeField] private AudioClip readySound;

    // Paramètres de l'animation bounce
    [SerializeField] private float bounceHeight = 0.3f;
    [SerializeField] private float bounceSpeed = 2f;

    private bool wasItemCrafted = false;
    private bool wasReadyIconShown = false;

    // Son de pose d'item
    [SerializeField] private AudioClip placeItemSound;
    [SerializeField] [Range(0f, 1f)] private float placeSoundVolume = 0.5f;
    [SerializeField] private float placeSoundPitchMin = 0.9f;
    [SerializeField] private float placeSoundPitchMax = 1.1f;
    private AudioSource counterAudioSource;

    // Effet de fumée pendant la cuisson
    private CookingSmokeEffect cookingSmokeEffect;

    // ============================================================
    // NETWORK - ID et état pour synchronisation
    // ============================================================

    [Header("Network")]
    [SerializeField] private string networkId;
    private static int counterIdCounter = 0;

    // Registre statique de tous les counters pour synchronisation
    private static Dictionary<string, Counter> counterRegistry = new Dictionary<string, Counter>();

    // État de cuisson pour synchronisation
    private string cookingState = "idle"; // idle, cooking, done
    private float cookingProgress = 0f;
    private float cookingDuration = 0f;

    // Système de lock pour conflits
    private int? lockedByPlayer = null;
    private uint lockTick = 0;

    // Flag pour mode client (pas de logique locale)
    private bool isNetworkControlled = false;

    /// <summary>
    /// ID unique pour la synchronisation réseau
    /// </summary>
    public string NetworkId
    {
        get
        {
            if (string.IsNullOrEmpty(networkId))
            {
                // Utiliser le même format position que Awake pour la cohérence
                networkId = $"counter_{transform.position.x:F1}_{transform.position.y:F1}";
            }
            return networkId;
        }
    }

    private void Awake()
    {
        // Générer un ID basé sur la position si pas défini
        if (string.IsNullOrEmpty(networkId))
        {
            networkId = $"counter_{transform.position.x:F1}_{transform.position.y:F1}";
        }

        // Enregistrer dans le registre statique
        counterRegistry[NetworkId] = this;

        Transform itemTransform = transform.Find("ItemDisplayed");
        if (itemTransform != null)
        {
            itemToDisplay = itemTransform.gameObject.GetComponent<SpriteRenderer>();
        }
        else
        {
            itemToDisplay = null;
        }

        Transform readyIconTransform = transform.Find("ReadyIcon");
        if (readyIconTransform != null)
        {
            readyIcon = readyIconTransform.gameObject.GetComponent<SpriteRenderer>();
            readyIconInitialPosition = readyIconTransform.localPosition;
            readyIcon.enabled = false; // Masquer par défaut
        }
        else
        {
            readyIcon = null;
        }

        cookingSmokeEffect = GetComponentInChildren<CookingSmokeEffect>();

        counterAudioSource = GetComponent<AudioSource>();
        if (counterAudioSource == null)
            counterAudioSource = gameObject.AddComponent<AudioSource>();

        if (counterObject != null && counterData != null)
            counterObject.sprite = counterData.counterSprite;


        // Vérifier si on est en mode client
        if (NetworkManager.Instance != null && NetworkManager.Instance.Role == NetworkRole.Client)
        {
            isNetworkControlled = true;
        }
    }

    private void OnDestroy()
    {
        // Retirer du registre
        if (counterRegistry.ContainsKey(NetworkId))
        {
            counterRegistry.Remove(NetworkId);
        }
    }

    // ============================================================
    // NETWORK - Méthodes statiques pour accès aux counters
    // ============================================================

    /// <summary>
    /// Retrouve un counter par son ID réseau
    /// </summary>
    public static Counter GetCounterById(string counterId)
    {
        if (string.IsNullOrEmpty(counterId))
            return null;

        counterRegistry.TryGetValue(counterId, out Counter counter);
        return counter;
    }

    /// <summary>
    /// Retourne tous les counters enregistrés
    /// </summary>
    public static IEnumerable<Counter> GetAllCounters()
    {
        return counterRegistry.Values;
    }

    /// <summary>
    /// Applique l'état réseau reçu du serveur (mode client uniquement)
    /// </summary>
    public void ApplyNetworkState(CounterState state)
    {
        if (state == null) return;

        // Mettre à jour l'item sur le counter
        if (ItemDatabase.Instance != null)
        {
            string newItemName = state.currentItem;
            if (string.IsNullOrEmpty(newItemName))
            {
                currentItem = null;
            }
            else if (currentItem == null || currentItem.name != newItemName)
            {
                var foundItem = ItemDatabase.Instance.GetItemByName(newItemName);
                Debug.Log($"[Counter] ApplyNetworkState {NetworkId}: item='{newItemName}' → found={foundItem != null}, sprite={foundItem?.sprite != null}, itemToDisplay={itemToDisplay != null}, hidden={counterData?.itemNeedHidden}");
                currentItem = foundItem;
            }
        }
        else
        {
            Debug.LogWarning($"[Counter] ItemDatabase.Instance est NULL!");
        }

        // Mettre à jour l'état de cuisson
        cookingState = state.cookingState ?? "idle";
        cookingProgress = state.cookingProgress;

        // Mettre à jour le visuel
        UpdateVisual();

        // Mettre à jour le slider si en cours de cuisson
        SliderTime sliderTime = GetComponent<SliderTime>();
        if (sliderTime != null)
        {
            if (cookingState == "cooking" && cookingProgress > 0 && cookingProgress < 1)
            {
                sliderTime.SetProgress(cookingProgress);
            }
            else if (cookingState == "idle" || cookingState == "done")
            {
                sliderTime.HideSlider();
            }
        }

        // Dériver l'état de craft depuis le cookingState pour les visuels (icône prêt)
        bool shouldBeItemCrafted = (cookingState == "done");
        if (shouldBeItemCrafted != wasItemCrafted)
        {
            wasItemCrafted = shouldBeItemCrafted;
            UpdateReadyIcon();
        }

        // Gérer l'effet de fumée selon l'état de cuisson
        if (cookingSmokeEffect != null)
        {
            if (cookingState == "cooking")
                cookingSmokeEffect.Play();
            else
                cookingSmokeEffect.Stop();
        }
    }

    private void UpdateVisual()
    {
        if (itemToDisplay == null) return;

        if (currentItem == null)
        {
            itemToDisplay.sprite = null;
        }
        else
        {
            if(counterData != null && !counterData.itemNeedHidden)
            {
                itemToDisplay.sprite = currentItem.sprite;
                PunchItemScale();
            }else
            {
                itemToDisplay.sprite = null;
            }
        }
    }

    private void PlayPlaceSound()
    {
        if (placeItemSound == null || counterAudioSource == null) return;
        counterAudioSource.pitch = Random.Range(placeSoundPitchMin, placeSoundPitchMax);
        counterAudioSource.PlayOneShot(placeItemSound, placeSoundVolume);
    }

    private Coroutine punchCoroutine;

    private void PunchItemScale()
    {
        if (itemToDisplay == null) return;
        if (punchCoroutine != null) StopCoroutine(punchCoroutine);
        punchCoroutine = StartCoroutine(PunchScaleCoroutine(itemToDisplay.transform, 1.4f, 0.2f));
    }

    private IEnumerator PunchScaleCoroutine(Transform target, float intensity, float duration)
    {
        Vector3 original = target.localScale;
        Vector3 big = original * intensity;
        target.localScale = big;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float n = t / duration;
            float eased = 1f - Mathf.Pow(1f - n, 3f);
            target.localScale = Vector3.Lerp(big, original, eased);
            yield return null;
        }
        target.localScale = original;
        punchCoroutine = null;
    }

    private void UpdateReadyIcon()
    {
        if (readyIcon != null)
        {
            // L'icône ne s'affiche que si l'item est crafté ET que ce n'est pas un comptoir d'assemblage
            bool shouldShowIcon = wasItemCrafted && counterData != null && counterData.type != CounterType.Assemblage;
            readyIcon.enabled = shouldShowIcon;

            if (shouldShowIcon)
            {
                // Jouer le son si l'icône vient d'apparaître
                if (!wasReadyIconShown && readySound != null)
                {
                    AudioSource.PlayClipAtPoint(readySound, transform.position);
                }
                wasReadyIconShown = true;

                // Démarrer l'animation de bounce
                if (bounceCoroutine != null)
                    StopCoroutine(bounceCoroutine);
                bounceCoroutine = StartCoroutine(BounceAnimation());
            }
            else
            {
                wasReadyIconShown = false;
                // Arrêter l'animation et réinitialiser la position
                if (bounceCoroutine != null)
                {
                    StopCoroutine(bounceCoroutine);
                    bounceCoroutine = null;
                }
                readyIcon.transform.localPosition = readyIconInitialPosition;
            }
        }
    }

    IEnumerator BounceAnimation()
    {
        float time = 0f;
        while (true)
        {
            time += Time.deltaTime * bounceSpeed;
            float yOffset = Mathf.Abs(Mathf.Sin(time)) * bounceHeight;
            readyIcon.transform.localPosition = readyIconInitialPosition + new Vector3(0, yOffset, 0);
            yield return null;
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
            // Vider les ingrédients quand le joueur prend l'item
            ingredientsOnCounter.Clear();
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

            Debug.Log($"[Crafting] Test avec {testIngredients.Count} ingrédients:");
            foreach (var item in testIngredients)
            {
                Debug.Log($"  - {item.name}");
            }

            foreach (var recipe in GameManager.Instance.recipes)
            {
                Debug.Log($"[Crafting] Test recette: {recipe.name} ({recipe.ingredients.Length} ingrédients)");
                if (recipe.Matches(testIngredients))
                {
                    newResult = recipe.result;
                    Debug.Log($"[Crafting] ✓ Recette trouvée! Résultat: {newResult.name}");
                    break;
                }
            }

            if (newResult == null)
            {
                Debug.Log("[Crafting] ✗ Aucune recette ne correspond");
            }



            if (newResult != null)
            {
                // Recette trouvée - le résultat devient le nouvel ingrédient de base
                ingredientsOnCounter.Clear();
                ingredientsOnCounter.Add(newResult);
                currentItem = newResult;
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

            PlayPlaceSound();
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
            // Bloquer si le joueur a déjà un item en main
            if (playerInventory.currentItem != null) return;

            playerInventory.AddItem(currentItem);
            currentItem = null;
            UpdateVisual();
            // Mettre à jour l'UI pour ce joueur
            if (InventoryUI.Instance != null && playerController != null)
            {
                InventoryUI.Instance.UpdatePlayerInventory(playerController.playerId, playerInventory);
            }
            wasItemCrafted = false;
            UpdateReadyIcon(); // Masquer l'icône "prêt"
            return;
        }

        // Vérifier si le joueur a un item avant d'essayer de le transformer
        if (itemToTransform == null)
        {
            Debug.Log("Aucun item à transformer !");
            return;
        }

        if (itemToTransform.counterType != counterData.type)
        {
            Debug.Log("Mauvais comptoir !");
            return;
        }

        currentItem = itemToTransform;
        playerInventory.RemoveItem(itemToTransform);
        if (InventoryUI.Instance != null)
            InventoryUI.Instance.UpdatePlayerInventory(playerId, playerInventory);
        PlayPlaceSound();
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

        // Mettre à jour l'état réseau
        cookingState = "cooking";
        cookingDuration = itemToTransform.secondsToTransform;
        cookingProgress = 0f;

        if (counterData.needPlayerFreeze)
        {
            playerMovement.Freeze();
        }

        // Démarrer le slider timer
        sliderTime.StartTimer(itemToTransform.secondsToTransform);
        Debug.Log($"[Counter] WaitForTransform - smokeEffect: {(cookingSmokeEffect != null ? "TROUVÉ" : "NULL")}");
        cookingSmokeEffect?.Play();
        AudioSource audioSource = playerMovement.GetComponent<AudioSource>();
        if (audioSource != null && itemToTransform.soundToTransform != null)
        {
            audioSource.clip = itemToTransform.soundToTransform;
            audioSource.loop = true;
            audioSource.Play();
        }

        float elapsed = 0f;
        while (elapsed < itemToTransform.secondsToTransform)
        {
            elapsed += Time.deltaTime;
            cookingProgress = elapsed / itemToTransform.secondsToTransform;
            yield return null;
        }

        // Arrêter le son de cuisson
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            audioSource.loop = false;
            audioSource.clip = null;
        }

        // Transformation finie
        cookingSmokeEffect?.Stop();
        sliderTime.HideSlider();
        currentItem = itemToTransform.itemCrafted;
        wasItemCrafted = true;
        UpdateVisual();
        UpdateReadyIcon(); // Afficher l'icône "prêt"
        playerMovement.Unfreeze();

        // Mettre à jour état réseau
        cookingState = "done";
        cookingProgress = 1f;

        transformCoroutine = null;
    }




    public void Interact(PlayerInteraction player)
    {
        InventorySystem playerInventory = player.GetComponent<InventorySystem>();
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();

        // Bonus livraison instantanée : n'importe quel comptoir peut servir un plat
        if (EffectManager.Instance != null && EffectManager.Instance.LivraisonInstantaneeActif
            && counterData.type != CounterType.Service
            && playerInventory.currentItem != null)
        {
            foreach (RecipeData order in OrderManager.Instance.currentOrders)
            {
                if (order.result == playerInventory.currentItem)
                {
                    OrderManager.Instance.CompletePNJOrder(order, playerInventory, player);
                    return;
                }
            }
        }

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

    // ============================================================
    // GETTERS pour la synchronisation réseau
    // ============================================================

    public string GetCounterType()
    {
        return counterData != null ? counterData.type.ToString() : "Unknown";
    }

    public string GetCurrentItemName()
    {
        return currentItem != null ? currentItem.name : null;
    }

    public string GetCookingState()
    {
        return cookingState;
    }

    public float GetCookingProgress()
    {
        return cookingProgress;
    }

    public int? GetLockedByPlayer()
    {
        return lockedByPlayer;
    }

}
