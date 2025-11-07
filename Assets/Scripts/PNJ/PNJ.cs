using UnityEngine;

public class PNJClient : MonoBehaviour, IInteractable
{
    [Header("D�placement")]
    public Transform[] chemin;         // Points � suivre dans l'ordre
    public float vitesse = 2f;
    public float distanceArret = 0.1f;

    [Header("�tat du client")]
    public float tempsAttenteCommande = 2f;
    public float tempsPourManger = 15f;

    [HideInInspector]
    public int positionIndex = -1;     // Index de la position au comptoir (assign� par le spawner)
    [HideInInspector]
    public float offsetEntreClients = 0.5f; // Espacement entre les clients

    private int indexPoint = 0;
    private Rigidbody2D rb;
    private Animator anim;

    private EtatClient etat = EtatClient.Arrive;
    private float timer;
    private bool commandeRecue = false;
    private Vector2 positionFinale; // Position finale avec offset appliqu�

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
        if (chemin.Length == 0)
        {
            Debug.LogWarning("Aucun point de chemin d�fini pour le PNJ " + name);
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

        if (anim) anim.SetBool("marche", rb.linearVelocity.magnitude > 0.1f);
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
            Debug.Log(name + " est arriv� au comptoir !");
            return;
        }

        Vector2 target = chemin[indexPoint].position;

        // Si c'est le dernier point, appliquer l'offset
        if (indexPoint == chemin.Length - 1 && positionIndex >= 0)
        {
            Vector2 offset = new Vector2(0, positionIndex * offsetEntreClients);
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

        // Cr�er une commande via OrderManager
        if (OrderManager.Instance != null)
        {
            OrderManager.Instance.CreatePNJOrder(this);
            Debug.Log(name + " a command� et attend d'�tre servi !");
        }
        else
        {
            Debug.LogWarning("OrderManager introuvable !");
        }
    }

    // M�thode appel�e par OrderManager quand la commande est livr�e
    public void RecevoirCommande()
    {
        commandeRecue = true;
        etat = EtatClient.Satisfait;
        Debug.Log(name + " a re�u son plat et est satisfait !");
    }

    void PartirInsatisfait()
    {
        etat = EtatClient.Insatisfait;
        Debug.Log(name + " n'a pas re�u sa commande et part m�content !");

        // Retirer la commande de l'OrderManager
        if (OrderManager.Instance != null)
        {
            OrderManager.Instance.RemoveExpiredPNJOrder(this);
        }
    }

    void Partir(bool satisfait)
    {
        rb.linearVelocity = Vector2.right * vitesse;

        // Quand il quitte l'�cran, on le d�truit
        if (transform.position.x > 10f)
        {
            Destroy(gameObject);

            if (!satisfait)
            {
                Debug.Log(name + " est parti insatisfait !");
                // Optionnel : p�nalit� de score
                // GameManager.Instance.AddScore(-10);
            }
        }
    }

    // === INTERACTION AVEC LE JOUEUR ===
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerInteraction player = collision.GetComponent<PlayerInteraction>();
            if (player != null)
            {
                player.SetCurrentInteractable(this);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerInteraction player = collision.GetComponent<PlayerInteraction>();
            if (player != null)
            {
                player.ClearCurrentInteractable(this);
            }
        }
    }

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
