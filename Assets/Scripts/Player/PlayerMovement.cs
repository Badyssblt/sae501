using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Vector2 movement;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Lire les axes définis dans Input Manager
        float moveX = Input.GetAxisRaw("P1_Horizontal");
        float moveY = Input.GetAxisRaw("P1_Vertical");

        // Normaliser pour éviter d’aller plus vite en diagonale
        movement = new Vector2(moveX, moveY).normalized;
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}
