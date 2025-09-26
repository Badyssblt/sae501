using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Movement State")]
    private Rigidbody2D rb;
    private Vector2 movement;

    // Pour compatibilité avec l'ancienne configuration
    [HideInInspector]
    public string horizontalAxis = "P1_Horizontal";
    [HideInInspector]
    public string verticalAxis = "P1_Vertical";

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            rb.linearVelocity = movement * moveSpeed;
        }
    }

    // Méthode appelée par PlayerController pour définir le mouvement
    public void SetMovement(Vector2 newMovement)
    {
        movement = newMovement;
    }

    // Méthode pour récupérer le mouvement actuel (utile pour l'animation ou le debug)
    public Vector2 GetMovement()
    {
        return movement;
    }

    // Méthode pour arrêter immédiatement le mouvement
    public void StopMovement()
    {
        movement = Vector2.zero;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}
