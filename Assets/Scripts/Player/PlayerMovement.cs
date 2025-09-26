using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 movement;
    private bool isFrozen = false; // si true, le joueur est gelé

    public string horizontalAxis = "P1_Horizontal";
    public string verticalAxis = "P1_Vertical";

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (!isFrozen)
        {
            float moveX = Input.GetAxisRaw(horizontalAxis);
            float moveY = Input.GetAxisRaw(verticalAxis);

            movement = new Vector2(moveX, moveY).normalized;
            rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
        }
        // si isFrozen = true, MovePosition n'est jamais appelé → joueur reste immobile
    }

    // Geler le joueur
    public void FreezePlayer()
    {
        isFrozen = true;

        // Optionnel : geler physiquement le Rigidbody2D pour éviter tout glitch
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    // Débloquer le joueur
    public void UnfreezePlayer()
    {
        isFrozen = false;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; // garde rotation si nécessaire
    }
}
