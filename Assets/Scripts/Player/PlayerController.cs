using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInteraction))]
public class PlayerController : MonoBehaviour
{
    [Header("Player Identity")]
    public int playerId = 1; // 1-4
    public bool isLocalPlayer = true; // true pour slots 1-2, false pour slots 3-4

    [Header("Input Settings")]
    private string horizontalAxis;
    private string verticalAxis;
    private string actionButton;

    [Header("Input State")]
    private Vector2 currentMovement;
    private bool actionPressed = false;
    private bool actionPreviousFrame = false;

    [Header("Components")]
    private PlayerMovement playerMovement;
    private PlayerInteraction playerInteraction;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerInteraction = GetComponent<PlayerInteraction>();
    }

    private void Start()
    {
        // Configurer les axes selon le joueur
        if (isLocalPlayer)
        {
            horizontalAxis = $"P{playerId}_Horizontal";
            verticalAxis = $"P{playerId}_Vertical";
            actionButton = $"P{playerId}_B1";  // Changé pour correspondre à votre Input Manager

            Debug.Log($"Player {playerId} configuré avec axes: {horizontalAxis}, {verticalAxis}, {actionButton}");
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

        // Récupérer les inputs selon le type de joueur
        if (isLocalPlayer)
        {
            // Joueurs locaux (slots 1-2) : lire l'Input Manager
            ProcessLocalInput();
        }
        else
        {
            // Joueurs distants (slots 3-4) : lire depuis le réseau
            ProcessRemoteInput();
        }

        // Transmettre le mouvement à PlayerMovement
        if (playerMovement != null)
        {
            playerMovement.SetMovement(currentMovement);
        }

        // Gérer l'action (appui sur le bouton)
        if (actionPressed && !actionPreviousFrame)
        {
            playerInteraction?.OnInteract();
        }

        actionPreviousFrame = actionPressed;
    }

    private void ProcessLocalInput()
    {
        // Lire les axes configurés dans l'Input Manager
        float horizontal = 0f;
        float vertical = 0f;

        try
        {
            horizontal = Input.GetAxisRaw(horizontalAxis);
            vertical = Input.GetAxisRaw(verticalAxis);
            actionPressed = Input.GetButton(actionButton);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Axes non configurés pour le joueur {playerId}: {e.Message}");
            Debug.LogWarning($"Assurez-vous que {horizontalAxis}, {verticalAxis} et {actionButton} sont définis dans l'Input Manager");
        }

        currentMovement = new Vector2(horizontal, vertical).normalized;
    }

    private void ProcessRemoteInput()
    {
        // Récupérer les inputs depuis le NetworkManager
        if (NetworkManager.Instance != null)
        {
            var input = NetworkManager.Instance.GetRemoteInput(playerId);
            if (input != null)
            {
                currentMovement = new Vector2(input.horizontal, input.vertical).normalized;
                actionPressed = input.action;
            }
            else
            {
                // Pas d'input reçu, mettre à zéro
                currentMovement = Vector2.zero;
                actionPressed = false;
            }
        }
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

        // Reconfigurer les axes si nécessaire
        if (isLocal)
        {
            horizontalAxis = $"P{playerId}_Horizontal";
            verticalAxis = $"P{playerId}_Vertical";
            actionButton = $"P{playerId}_B1";  // Changé pour correspondre à votre Input Manager
        }
    }
}
