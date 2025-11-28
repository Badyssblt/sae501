using UnityEngine;
using CookMoiCa.Network;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInteraction))]
public class PlayerController : MonoBehaviour
{
    [Header("Player Identity")]
    public int playerId = 1; // 1-4
    public bool isLocalPlayer = true;

    [Header("Input Settings")]
    private string horizontalAxis;
    private string verticalAxis;
    private string actionButton;

    [Header("Input State")]
    public Vector2 currentMovement;
    private bool actionPressed = false;
    private bool actionPreviousFrame = false;
    private ActionType currentAction = ActionType.None;

    [Header("Components")]
    private PlayerMovement playerMovement;
    private PlayerInteraction playerInteraction;
    public Animator animator;

    private float freezeHoldTimer = 0f;
    [SerializeField] private float holdToUnfreezeTime = 0.5f;

    // Interpolation pour joueurs distants (côté client)
    private Vector2 interpolatedPosition;
    private bool useInterpolation = false;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerInteraction = GetComponent<PlayerInteraction>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // Configurer les axes selon le joueur
        if (isLocalPlayer)
        {
            horizontalAxis = $"P{playerId}_Horizontal";
            verticalAxis = $"P{playerId}_Vertical";
            actionButton = $"P{playerId}_B1";
        }

        // Déterminer si on doit utiliser l'interpolation
        DetermineInterpolationMode();
    }

    private void DetermineInterpolationMode()
    {
        if (NetworkManager.Instance == null)
        {
            useInterpolation = false;
            return;
        }

        // En mode client, les joueurs qui ne sont pas le joueur local utilisent l'interpolation
        if (NetworkManager.Instance.Role == NetworkRole.Client)
        {
            useInterpolation = !isLocalPlayer;
        }
        else
        {
            // En mode host, les joueurs distants (slots 3-4) sont contrôlés par les inputs réseau
            useInterpolation = false;
        }
    }

    private void Update()
    {
        if (GameManager.Instance?.GetCurrentState() != GameState.Playing)
        {
            currentMovement = Vector2.zero;
            actionPressed = false;
            return;
        }

        // Lire les inputs selon le mode
        if (isLocalPlayer)
        {
            ProcessLocalInput();

            // En mode client, envoyer les inputs au serveur
            if (NetworkManager.Instance?.Role == NetworkRole.Client)
            {
                SendInputToServer();
            }
        }
        else
        {
            ProcessRemoteInput();
        }

        // Gestion du freeze
        if (playerMovement.isFrozen)
        {
            HandleFrozenState();
            return;
        }

        // Appliquer le mouvement
        ApplyMovement();

        // Gestion de l'action
        HandleAction();
    }

    private void ProcessLocalInput()
    {
        float horizontal = 0f;
        float vertical = 0f;

        // Essayer les axes arcade d'abord
        try
        {
            horizontal = Input.GetAxisRaw(horizontalAxis);
            vertical = Input.GetAxisRaw(verticalAxis);
            actionPressed = Input.GetButton(actionButton);
        }
        catch (System.Exception)
        {
            // Axes non configurés, on utilise le fallback clavier
        }

        // Fallback clavier (ZQSD/WASD + Espace) - UNIQUEMENT pour les clients web distants
        // Vérification à l'exécution : seulement si on est en mode Client (pas Host/borne arcade)
        if (NetworkManager.Instance != null &&
            NetworkManager.Instance.Role == NetworkRole.Client &&
            horizontal == 0f && vertical == 0f && !actionPressed)
        {
            // ZQSD (FR) et WASD (EN)
            if (Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.W)) vertical = 1f;
            if (Input.GetKey(KeyCode.S)) vertical = -1f;
            if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.A)) horizontal = -1f;
            if (Input.GetKey(KeyCode.D)) horizontal = 1f;

            // Aussi les flèches
            if (Input.GetKey(KeyCode.UpArrow)) vertical = 1f;
            if (Input.GetKey(KeyCode.DownArrow)) vertical = -1f;
            if (Input.GetKey(KeyCode.LeftArrow)) horizontal = -1f;
            if (Input.GetKey(KeyCode.RightArrow)) horizontal = 1f;

            // Espace ou E pour l'action
            actionPressed = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.E);
        }

        currentMovement = new Vector2(horizontal, vertical).normalized;

        // Déterminer l'action
        if (actionPressed && !actionPreviousFrame)
        {
            currentAction = ActionType.Interact;
        }
        else
        {
            currentAction = ActionType.None;
        }
    }

    private void ProcessRemoteInput()
    {
        if (NetworkManager.Instance == null)
        {
            currentMovement = Vector2.zero;
            actionPressed = false;
            return;
        }

        // En mode Host: utiliser les inputs reçus des clients distants
        if (NetworkManager.Instance.Role == NetworkRole.Host)
        {
            var input = NetworkManager.Instance.GetRemoteInput(playerId);
            if (input != null)
            {
                currentMovement = new Vector2(input.horizontal, input.vertical).normalized;
                actionPressed = input.action == "interact" || input.action == "grab";
            }
            else
            {
                currentMovement = Vector2.zero;
                actionPressed = false;
            }
        }
        // En mode Client: utiliser l'interpolation pour les autres joueurs
        else
        {
            if (useInterpolation)
            {
                ApplyInterpolation();
            }
        }
    }

    private void ApplyInterpolation()
    {
        Vector2? interpolatedPos = NetworkManager.Instance.GetInterpolatedPosition(playerId);

        if (interpolatedPos.HasValue)
        {
            // Déplacer directement vers la position interpolée
            transform.position = interpolatedPos.Value;

            // Calculer le mouvement apparent pour l'animation
            Vector2 movement = interpolatedPos.Value - interpolatedPosition;
            currentMovement = movement.normalized;
            interpolatedPosition = interpolatedPos.Value;

            // Mettre à jour l'animation
            if (animator != null)
            {
                animator.SetFloat("MoveX", currentMovement.x);
                animator.SetFloat("MoveY", currentMovement.y);
            }
        }
    }

    private void SendInputToServer()
    {
        if (NetworkManager.Instance == null) return;
        if (NetworkManager.Instance.Role != NetworkRole.Client) return;

        // Envoyer l'input au serveur
        NetworkManager.Instance.SendInput(currentMovement, currentAction, GetTargetId());
    }

    private string GetTargetId()
    {
        // Retourner l'ID de l'objet ciblé si interaction
        if (playerInteraction != null && playerInteraction.GetCurrentInteractable() != null)
        {
            var interactable = playerInteraction.GetCurrentInteractable();

            // Si c'est un Counter
            var counter = interactable as Counter;
            if (counter != null)
            {
                return counter.NetworkId;
            }

            // Si c'est un Item
            var item = interactable as Item;
            if (item != null)
            {
                return item.NetworkId;
            }
        }
        return null;
    }

    private void HandleFrozenState()
    {
        if (currentMovement != Vector2.zero)
        {
            freezeHoldTimer += Time.deltaTime;
            if (freezeHoldTimer >= holdToUnfreezeTime)
            {
                playerMovement.Unfreeze();
                freezeHoldTimer = 0f;
            }
        }
        else
        {
            freezeHoldTimer = 0f;
        }

        currentMovement = Vector2.zero;
        actionPressed = false;
    }

    private void ApplyMovement()
    {
        // Ne pas appliquer le mouvement si on utilise l'interpolation
        if (useInterpolation) return;

        if (playerMovement != null)
        {
            playerMovement.SetMovement(currentMovement);

            if (animator != null)
            {
                animator.SetFloat("MoveX", currentMovement.x);
                animator.SetFloat("MoveY", currentMovement.y);
            }
        }
    }

    private void HandleAction()
    {
        if (actionPressed && !actionPreviousFrame)
        {
            playerInteraction?.OnInteract();
        }

        actionPreviousFrame = actionPressed;
    }

    public Vector2 GetCurrentMovement()
    {
        return currentMovement;
    }

    public bool IsActionPressed()
    {
        return actionPressed;
    }

    public void SetPlayerIdentity(int id, bool isLocal)
    {
        playerId = id;
        isLocalPlayer = isLocal;

        if (isLocal)
        {
            horizontalAxis = $"P{playerId}_Horizontal";
            verticalAxis = $"P{playerId}_Vertical";
            actionButton = $"P{playerId}_B1";
        }

        DetermineInterpolationMode();
    }

    /// <summary>
    /// Force la position (utilisé lors du rollback)
    /// </summary>
    public void ForcePosition(Vector2 position)
    {
        transform.position = position;
        interpolatedPosition = position;
    }
}
