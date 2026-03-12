using UnityEngine;
using CookMoiCa.Network;

public class PNJClient : MonoBehaviour, IInteractable
{
    [Header("D�placement")]
    public Transform[] chemin;         // Points � suivre dans l'ordre
    public float vitesse = 2f;
    public float distanceArret = 0.1f;

    [Header("�tat du client")]
    public float tempsAttenteCommande = 2f;
    public float tempsPourManger = 15f;
    [Tooltip("Direction dans laquelle le PNJ regarde quand il attend au comptoir")]
    public Vector2 directionAttente = Vector2.up; // Par défaut, regarde vers le haut

    [Header("Son")]
    [SerializeField] private AudioClip servedSound;

    [HideInInspector]
    public int positionIndex = -1;     // Index de la position au comptoir (assign� par le spawner)
    [HideInInspector]
    public float offsetEntreClients = 0.5f; // Espacement entre les clients
    [HideInInspector]
    public DirectionAlignement directionAlignement = DirectionAlignement.Vertical; // Direction d'alignement des clients

    // ID unique pour la synchronisation réseau
    [HideInInspector]
    public string networkId;
    private static int nextNetworkId = 0;

    // Registre statique pour accès rapide (évite FindObjectsByType chaque frame)
    private static readonly System.Collections.Generic.List<PNJClient> allPNJs = new System.Collections.Generic.List<PNJClient>();
    public static System.Collections.Generic.List<PNJClient> AllPNJs => allPNJs;

    private int indexPoint = 0;
    private int indexRetour; // Index pour le chemin retour (parcours inversé)
    private Rigidbody2D rb;
    private Animator anim;

    private EtatClient etat = EtatClient.Arrive;
    private float timer;
    private bool commandeRecue = false;
    private Vector2 positionFinale; // Position finale avec offset appliqu�
    private Vector2 lastDirection = Vector2.right; // Dernière direction regardée (par défaut : droite)
    private PNJOrderDisplay orderDisplay; // Affichage de la commande au-dessus du PNJ
    private Vector2 spawnPosition; // Position de départ pour y retourner

    private enum EtatClient
    {
        Arrive,
        AttendCommande,
        AttendService,
        Satisfait,
        Insatisfait
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spawnPosition = transform.position;

        // Enregistrer dans le registre statique
        if (!allPNJs.Contains(this))
            allPNJs.Add(this);

        // Générer un ID réseau unique
        if (string.IsNullOrEmpty(networkId))
        {
            networkId = $"pnj_{nextNetworkId++}";
        }
        if (chemin.Length == 0)
        {
            Debug.LogWarning("Aucun point de chemin d�fini pour le PNJ " + name);
        }

        // Initialiser l'animation avec la direction par défaut
        if (anim != null)
        {
            anim.SetFloat("MoveX", lastDirection.x);
            anim.SetFloat("MoveY", lastDirection.y);
            anim.SetFloat("LastMoveX", lastDirection.x);
            anim.SetFloat("LastMoveY", lastDirection.y);
            anim.SetBool("IsMoving", false);
        }
    }

    void Update()
    {
        switch (etat)
        {
            case EtatClient.Arrive:
                DeplacerVersPoint();
                break;

            case EtatClient.AttendCommande:
                rb.linearVelocity = Vector2.zero;
                timer -= Time.deltaTime;
                if (timer <= 0)
                {
                    Commander();
                }
                break;

            case EtatClient.AttendService:
                rb.linearVelocity = Vector2.zero;
                timer -= Time.deltaTime;
                if (timer <= 0)
                {
                    // Timer expiré, le client part insatisfait
                    PartirInsatisfait();
                }
                break;

            case EtatClient.Satisfait:
                Partir(true);
                break;

            case EtatClient.Insatisfait:
                Partir(false);
                break;
        }

        // Mettre à jour les animations
        UpdateAnimations();
    }

    // === ANIMATIONS ===
    void UpdateAnimations()
    {
        if (anim == null) return;

        // Calculer la direction du mouvement normalisée
        Vector2 velocity = rb.linearVelocity;
        bool isMoving = velocity.magnitude > 0.1f;

        // Mettre à jour les paramètres d'animation
        if (isMoving)
        {
            // En mouvement : utiliser la direction du mouvement
            Vector2 direction = velocity.normalized;
            lastDirection = direction;

            anim.SetFloat("MoveX", direction.x);
            anim.SetFloat("MoveY", direction.y);
            anim.SetBool("IsMoving", true);
            anim.SetFloat("LastMoveX", direction.x);
            anim.SetFloat("LastMoveY", direction.y);
        }
        else
        {
            // À l'arrêt : mettre MoveX et MoveY à 0, garder LastMoveX et LastMoveY
            anim.SetFloat("MoveX", 0);
            anim.SetFloat("MoveY", 0);
            anim.SetBool("IsMoving", false);
            // LastMoveX et LastMoveY gardent leur dernière valeur
        }
    }

    // === MOUVEMENT ===
    void DeplacerVersPoint()
    {
        if (indexPoint >= chemin.Length)
        {
            // Arriv� au comptoir
            rb.linearVelocity = Vector2.zero;
            etat = EtatClient.AttendCommande;
            timer = tempsAttenteCommande;

            // Regarder dans la direction d'attente définie
            if (directionAttente != Vector2.zero)
            {
                lastDirection = directionAttente.normalized;

                // Mettre à jour immédiatement l'animation pour regarder dans cette direction
                if (anim != null)
                {
                    anim.SetFloat("MoveX", 0);
                    anim.SetFloat("MoveY", 0);
                    anim.SetFloat("LastMoveX", lastDirection.x);
                    anim.SetFloat("LastMoveY", lastDirection.y);
                    anim.SetBool("IsMoving", false);
                }
            }

            return;
        }

        Vector2 target = chemin[indexPoint].position;

        // Si c'est le dernier point, appliquer l'offset
        if (indexPoint == chemin.Length - 1 && positionIndex >= 0)
        {
            Vector2 offset = directionAlignement == DirectionAlignement.Vertical
                ? new Vector2(0, positionIndex * offsetEntreClients)
                : new Vector2(positionIndex * offsetEntreClients, 0);
            target += offset;
            positionFinale = target;
        }

        Vector2 pos = transform.position;
        Vector2 dir = (target - pos).normalized;

        rb.linearVelocity = dir * vitesse;

        float dist = Vector2.Distance(pos, target);
        if (dist < distanceArret)
        {
            indexPoint++;
        }
    }

    // === COMPORTEMENT ===
    void Commander()
    {
        etat = EtatClient.AttendService;
        timer = tempsPourManger;

        // Les commandes ne doivent être créées que côté Host
        // Côté Client, l'affichage arrive via le réseau (pnjOrder event)
        if (NetworkManager.Instance != null && NetworkManager.Instance.Role == NetworkRole.Client)
            return;

        // Cr�er une commande via OrderManager
        if (OrderManager.Instance != null)
        {
            RecipeData recipe = OrderManager.Instance.CreatePNJOrder(this);

            // Créer l'affichage de la commande au-dessus du PNJ
            if (recipe != null)
            {
                CreateOrderDisplay(recipe);

                // Notifier les clients distants de la commande
                if (NetworkManager.Instance != null && NetworkManager.Instance.Role == NetworkRole.Host)
                {
                    NetworkManager.Instance.SendGameEvent("pnjOrder", new PNJOrderEvent
                    {
                        networkId = networkId,
                        recipeName = recipe.result.name
                    });
                }
            }
        }
        else
        {
            Debug.LogWarning("OrderManager introuvable !");
        }
    }

    void CreateOrderDisplay(RecipeData recipe)
    {
        if (recipe == null)
        {
            Debug.LogError($"PNJ {name}: Impossible de créer l'affichage avec une recette null!");
            return;
        }

        // Créer un GameObject pour l'affichage
        GameObject displayObject = new GameObject("OrderDisplay");
        orderDisplay = displayObject.AddComponent<PNJOrderDisplay>();

        if (orderDisplay != null)
        {
            orderDisplay.Initialize(recipe, transform);
        }
        else
        {
            Debug.LogError($"PNJ {name}: Échec de l'ajout du composant PNJOrderDisplay!");
            Destroy(displayObject);
        }
    }

    void InitRetour()
    {
        indexRetour = chemin.Length - 1; // Commencer par le dernier point du chemin
    }

    // M�thode appel�e par OrderManager quand la commande est livr�e
    public void RecevoirCommande()
    {
        commandeRecue = true;
        etat = EtatClient.Satisfait;
        InitRetour();

        if (servedSound != null)
        {
            AudioSource.PlayClipAtPoint(servedSound, transform.position);
        }

        // Détruire l'affichage de la commande
        if (orderDisplay != null)
        {
            Destroy(orderDisplay.gameObject);
            orderDisplay = null;
        }

        // Notifier les clients distants
        if (NetworkManager.Instance != null && NetworkManager.Instance.Role == NetworkRole.Host)
        {
            NetworkManager.Instance.SendGameEvent("pnjServed", new PNJServedEvent
            {
                networkId = networkId
            });
        }
    }

    void PartirInsatisfait()
    {
        // Ne pas réagir si on est client et que le host nous a déjà dit de partir
        if (etat == EtatClient.Satisfait || etat == EtatClient.Insatisfait)
            return;

        etat = EtatClient.Insatisfait;
        InitRetour();

        // Détruire l'affichage de la commande
        if (orderDisplay != null)
        {
            Destroy(orderDisplay.gameObject);
            orderDisplay = null;
        }

        // Retirer la commande de l'OrderManager (host seulement)
        if (OrderManager.Instance != null && (NetworkManager.Instance == null || NetworkManager.Instance.Role == NetworkRole.Host))
        {
            OrderManager.Instance.RemoveExpiredPNJOrder(this);
        }

        // Notifier les clients distants
        if (NetworkManager.Instance != null && NetworkManager.Instance.Role == NetworkRole.Host)
        {
            NetworkManager.Instance.SendGameEvent("pnjExpired", new PNJExpiredEvent
            {
                networkId = networkId
            });
        }
    }

    // === MÉTHODES RÉSEAU (appelées par GameManager côté client) ===

    /// <summary>
    /// Appelé côté client quand le host notifie la recette commandée
    /// </summary>
    public void ApplyNetworkOrder(string recipeName)
    {
        if (string.IsNullOrEmpty(recipeName)) return;

        // Chercher le sprite de la recette
        Sprite sprite = FindRecipeSprite(recipeName);
        if (sprite != null)
        {
            // Créer l'affichage de la commande
            GameObject displayObject = new GameObject("OrderDisplay");
            displayObject.transform.SetParent(transform);
            orderDisplay = displayObject.AddComponent<PNJOrderDisplay>();
            orderDisplay.InitializeFromSprite(sprite, transform);
        }
    }

    /// <summary>
    /// Appelé côté client quand le host notifie que le PNJ est servi
    /// </summary>
    public void ApplyNetworkServed()
    {
        commandeRecue = true;
        etat = EtatClient.Satisfait;
        InitRetour();

        if (servedSound != null)
        {
            AudioSource.PlayClipAtPoint(servedSound, transform.position);
        }

        if (orderDisplay != null)
        {
            Destroy(orderDisplay.gameObject);
            orderDisplay = null;
        }
    }

    /// <summary>
    /// Appelé côté client quand le host notifie que le PNJ a expiré
    /// </summary>
    public void ApplyNetworkExpired()
    {
        if (etat == EtatClient.Satisfait) return; // Déjà servi

        etat = EtatClient.Insatisfait;
        InitRetour();

        if (orderDisplay != null)
        {
            Destroy(orderDisplay.gameObject);
            orderDisplay = null;
        }
    }

    private Sprite FindRecipeSprite(string recipeName)
    {
        // Chercher dans les recettes du GameManager
        if (GameManager.Instance?.recipes != null)
        {
            foreach (var recipe in GameManager.Instance.recipes)
            {
                if (recipe?.result != null && recipe.result.name == recipeName && recipe.result.sprite != null)
                    return recipe.result.sprite;
            }
        }

        // Fallback : ItemDatabase
        if (ItemDatabase.Instance != null)
        {
            var item = ItemDatabase.Instance.GetItemByName(recipeName);
            if (item?.sprite != null) return item.sprite;
        }

        // Dernier recours
        var allItems = Resources.FindObjectsOfTypeAll<ItemData>();
        foreach (var item in allItems)
        {
            if (item != null && item.name == recipeName && item.sprite != null)
                return item.sprite;
        }

        return null;
    }

    void Partir(bool satisfait)
    {
        // Suivre le chemin à l'envers
        Vector2 target;

        if (indexRetour >= 0)
        {
            target = chemin[indexRetour].position;
        }
        else
        {
            // Tous les points du chemin sont parcourus, retourner au spawn
            target = spawnPosition;
        }

        Vector2 pos = transform.position;
        Vector2 dir = (target - pos).normalized;
        rb.linearVelocity = dir * vitesse;

        float dist = Vector2.Distance(pos, target);
        if (dist < distanceArret)
        {
            if (indexRetour >= 0)
            {
                indexRetour--;
            }
            else
            {
                // Arrivé au point de départ, détruire le PNJ
                if (orderDisplay != null)
                {
                    Destroy(orderDisplay.gameObject);
                    orderDisplay = null;
                }

                Destroy(gameObject);

                if (!satisfait)
                {
                    Debug.Log(name + " est parti insatisfait !");
                }
            }
        }
    }

    private void OnDestroy()
    {
        allPNJs.Remove(this);
    }

    // === NETWORK ===

    /// <summary>
    /// Retourne l'état du PNJ pour la synchronisation réseau
    /// </summary>
    public PNJState GetNetworkState()
    {
        // Trouver le nom de la recette commandée si en attente de service
        string recipeName = null;
        if (etat == EtatClient.AttendService && OrderManager.Instance != null)
        {
            var pnjOrder = OrderManager.Instance.pnjOrders.Find(o => o.client == this);
            if (pnjOrder != null && pnjOrder.recipe != null && pnjOrder.recipe.result != null)
            {
                recipeName = pnjOrder.recipe.result.name;
                // Fallback: si le champ custom name est vide, utiliser le nom de l'asset
                if (string.IsNullOrEmpty(recipeName))
                {
                    recipeName = ((ScriptableObject)pnjOrder.recipe.result).name;
                }
            }
        }

        return new PNJState
        {
            id = networkId,
            x = transform.position.x,
            y = transform.position.y,
            etat = GetEtatString(),
            recipeName = recipeName,
            lastMoveX = lastDirection.x,
            lastMoveY = lastDirection.y,
            isMoving = rb != null && rb.linearVelocity.magnitude > 0.1f
        };
    }

    private string GetEtatString()
    {
        switch (etat)
        {
            case EtatClient.Arrive: return "arrive";
            case EtatClient.AttendCommande: return "attendCommande";
            case EtatClient.AttendService: return "attendService";
            case EtatClient.Satisfait: return "satisfait";
            case EtatClient.Insatisfait: return "insatisfait";
            default: return "arrive";
        }
    }

    // Interaction gérée par le raycast du PlayerInteraction

    public void Interact(PlayerInteraction player)
    {
        // V�rifier que le client attend son service
        if (etat != EtatClient.AttendService)
        {
            Debug.Log(name + " n'attend pas de commande pour le moment.");
            return;
        }

        InventorySystem playerInventory = player.GetInventory();

        // V�rifier que le joueur tient un objet
        if (playerInventory.currentItem == null)
        {
            Debug.Log("Le joueur n'a aucun objet � donner.");
            return;
        }

        // V�rifier si l'objet correspond � la commande du client
        if (OrderManager.Instance != null)
        {
            // Chercher la commande de ce PNJ dans la liste pnjOrders
            OrderManager.PNJOrder pnjOrder = OrderManager.Instance.pnjOrders.Find(o => o.client == this);

            if (pnjOrder != null && pnjOrder.recipe.result == playerInventory.currentItem)
            {
                // Commande correcte ! Livrer la commande
                // CompletePNJOrder g�re le retrait de l'item, l'ajout du score et la mise � jour de l'UI
                OrderManager.Instance.CompletePNJOrder(pnjOrder.recipe, playerInventory, player);
                Debug.Log(name + " a re�u le bon plat !");
            }
            else
            {
                Debug.Log("L'objet donn� ne correspond pas � la commande du client !");
            }
        }
    }
}
