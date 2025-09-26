using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Vector2 movement;
    private Rigidbody2D rb;

    public string horizontalAxis = "P1_Horizontal";
    public string verticalAxis = "P1_Vertical";

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        
    }

    void FixedUpdate()
    {
        // Lire les axes définis dans Input Manager
        float moveX = Input.GetAxisRaw(horizontalAxis);
        float moveY = Input.GetAxisRaw(verticalAxis);

        // Normaliser pour éviter d’aller plus vite en diagonale
        movement = new Vector2(moveX, moveY).normalized;
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}
