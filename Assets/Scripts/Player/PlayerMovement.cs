using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;          // Vitesse normale
    public float sprintSpeed = 8f;        // Vitesse pendant le sprint

    [Header("Sprint Settings")]
    public float sprintDuration = 2f;     // Durée max du sprint (si on garde la touche)
    public float sprintCooldown = 3f;     // Temps de recharge
    public KeyCode sprintKey = KeyCode.LeftShift; // Touche configurable pour sprinter

    [Header("Movement State")]
    private Rigidbody2D rb;
    private Vector2 movement;
    private bool isSprinting = false;
    private bool isOnCooldown = false;
    private float sprintTimer = 0f;
    private float cooldownTimer = 0f;
    public bool isFrozen = false; // Pour geler le joueur (ex: dans un counter)

    // Axes pour compatibilité
    [HideInInspector] public string horizontalAxis = "P1_Horizontal";
    [HideInInspector] public string verticalAxis = "P1_Vertical";

    private PlayerController playerController;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        HandleSprintInput();
        HandleSprintTimers();
    }

    public void Freeze()
    {
        isFrozen = true;
        playerController.animator.SetFloat("MoveX", 0);
        playerController.animator.SetFloat("MoveY", 0);
        StopMovement();
    }

    public void Unfreeze()
    {
        isFrozen = false;
    }

    void FixedUpdate()
    {
        if (rb != null && !isFrozen)
        {
            float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
            rb.linearVelocity = movement * currentSpeed;
        }
    }

    // --- Gestion Input Sprint ---
    private void HandleSprintInput()
    {
        // Début sprint
        if (Input.GetKeyDown(sprintKey) && !isSprinting && !isOnCooldown)
        {
            isSprinting = true;
            sprintTimer = sprintDuration;
        }

        // Si le joueur lâche la touche sprint → arrêt immédiat et cooldown
        if (Input.GetKeyUp(sprintKey) && isSprinting)
        {
            StopSprintAndCooldown();
        }
    }

    private void HandleSprintTimers()
    {
        if (isSprinting)
        {
            sprintTimer -= Time.deltaTime;

            // Si la durée est écoulée → stop + cooldown
            if (sprintTimer <= 0f)
            {
                StopSprintAndCooldown();
            }
        }
        else if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
            }
        }
    }

    private void StopSprintAndCooldown()
    {
        isSprinting = false;
        isOnCooldown = true;
        cooldownTimer = sprintCooldown;
    }

    // --- Méthodes pour mouvement de base ---
    public void SetMovement(Vector2 newMovement)
    {
        movement = newMovement;
    }

    public Vector2 GetMovement()
    {
        return movement;
    }

    public void StopMovement()
    {
        movement = Vector2.zero;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    // --- Debug/feedback ---
    public bool CanSprint() => !isSprinting && !isOnCooldown;
    public bool IsSprinting() => isSprinting;
    public float GetCooldownRemaining() => isOnCooldown ? cooldownTimer : 0f;
}
