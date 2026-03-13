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
    private Vector2 targetPosition;
    private bool useInterpolation = false;
    private const float INTERPOLATION_SPEED = 15f;

    // Throttle des inputs envoyés au serveur (client)
    private Vector2 lastSentMovement = Vector2.zero;
    private ActionType lastSentAction = ActionType.None;
    private float lastInputSendTime = 0f;
    private const float MIN_INPUT_SEND_INTERVAL = 1f / 30f;

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

        // --- Si pas frozen ---
        if (playerMovement != null)
        {
            playerMovement.SetMovement(currentMovement);

            // Mettre à jour les paramètres d'animation
            if (currentMovement != Vector2.zero)
            {
                // En mouvement : utiliser la direction actuelle
                animator.SetFloat("MoveX", currentMovement.x);
                animator.SetFloat("MoveY", currentMovement.y);
                animator.SetBool("IsMoving", true);
                animator.SetFloat("LastMoveX", currentMovement.x);
                animator.SetFloat("LastMoveY", currentMovement.y);
            }
            else
            {
                // À l'arrêt : mettre MoveX et MoveY à 0, garder LastMoveX et LastMoveY
                animator.SetFloat("MoveX", 0);
                animator.SetFloat("MoveY", 0);
                animator.SetBool("IsMoving", false);
                // LastMoveX et LastMoveY gardent leur dernière valeur
            }
        }

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

        // Inputs web distants (joystick virtuel + clavier) — clients uniquement
        if (NetworkManager.Instance != null &&
            NetworkManager.Instance.Role == NetworkRole.Client &&
            horizontal == 0f && vertical == 0f && !actionPressed)
        {
            // Joystick virtuel (package Terresquall)
            if (Terresquall.VirtualJoystick.CountActiveInstances() > 0)
            {
                horizontal = Terresquall.VirtualJoystick.GetAxis("Horizontal");
                vertical   = Terresquall.VirtualJoystick.GetAxis("Vertical");
            }

            // Fallback clavier (ZQSD/WASD + flèches)
            if (horizontal == 0f && vertical == 0f)
            {
                if (Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.W)) vertical = 1f;
                if (Input.GetKey(KeyCode.S)) vertical = -1f;
                if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.A)) horizontal = -1f;
                if (Input.GetKey(KeyCode.D)) horizontal = 1f;
                if (Input.GetKey(KeyCode.UpArrow)) vertical = 1f;
                if (Input.GetKey(KeyCode.DownArrow)) vertical = -1f;
                if (Input.GetKey(KeyCode.LeftArrow)) horizontal = -1f;
                if (Input.GetKey(KeyCode.RightArrow)) horizontal = 1f;
            }

            if (!actionPressed && MobileInteractButton.Instance != null)
                actionPressed = MobileInteractButton.Instance.IsPressed;
            if (!actionPressed)
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
        // On ne clear PAS l'input après consommation : le dernier input reste actif
        // jusqu'à ce qu'un nouveau le remplace (envoyé à ~30fps par le client).
        // Cela évite le stutter causé par clear à 60fps vs réception à 30fps.
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

        // L'InterpolationBuffer fait déjà l'interpolation lisse entre snapshots
        // On applique directement sans lerp supplémentaire pour éviter le double lissage
        if (!interpolatedPos.HasValue) return;

        Vector2 newPos = interpolatedPos.Value;
        transform.position = newPos;

        // Calculer le mouvement apparent pour l'animation
        Vector2 movement = newPos - interpolatedPosition;
        currentMovement = movement.magnitude > 0.001f ? movement.normalized : Vector2.zero;
        interpolatedPosition = newPos;

        // Mettre à jour l'animation
        if (animator != null)
        {
            animator.SetFloat("MoveX", currentMovement.x);
            animator.SetFloat("MoveY", currentMovement.y);
        }
    }

    private void SendInputToServer()
    {
        if (NetworkManager.Instance == null) return;
        if (NetworkManager.Instance.Role != NetworkRole.Client) return;

        // Les actions (Interact, Grab, Drop) sont envoyées immédiatement sans throttle
        bool hasAction = currentAction != ActionType.None;
        bool inputChanged = currentMovement != lastSentMovement || currentAction != lastSentAction;
        bool intervalElapsed = Time.time - lastInputSendTime >= MIN_INPUT_SEND_INTERVAL;

        if (hasAction || (inputChanged && intervalElapsed))
        {
            NetworkManager.Instance.SendInput(currentMovement, currentAction, GetTargetId());
            lastSentMovement = currentMovement;
            lastSentAction = currentAction;
            lastInputSendTime = Time.time;
        }
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
        targetPosition = position;
    }
}
