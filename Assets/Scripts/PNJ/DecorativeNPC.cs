using System.Collections;
using UnityEngine;

/// <summary>
/// PNJ purement décoratif qui se déplace entre des points de passage.
/// Aucune interaction possible, aucun lien avec le système de commandes.
/// </summary>
public class DecorativeNPC : MonoBehaviour
{
    public enum ModeDeplacement
    {
        AllerSimple,    // A → B → destroy
        Boucle,         // A → B → téléporte en A → recommence
        AllerRetour     // A → B → A → B → ...
    }

    [Header("Chemin")]
    [Tooltip("Points de passage dans l'ordre")]
    public Transform[] points;
    public float vitesse = 2f;
    public float distanceArret = 0.1f;

    [Header("Comportement")]
    public ModeDeplacement mode = ModeDeplacement.AllerRetour;
    [Tooltip("Temps de pause à chaque point d'arrivée (0 = sans pause)")]
    public float tempsPause = 0f;

    private Rigidbody2D rb;
    private Animator anim;

    private int indexPoint = 0;
    private int direction = 1;  // +1 = avant, -1 = retour (pour AllerRetour)
    private bool enPause = false;
    private Vector2 lastDirection = Vector2.right;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        if (points == null || points.Length < 2)
        {
            Debug.LogWarning($"[DecorativeNPC] {name} : il faut au moins 2 points de chemin.", this);
            enabled = false;
            return;
        }

        // Placer le NPC directement sur le premier point
        transform.position = points[0].position;

        InitAnimation();
    }

    void Update()
    {
        if (enPause || points == null || points.Length < 2) return;

        DeplacerVersPoint();
        MettreAJourAnimation();
    }

    void DeplacerVersPoint()
    {
        Transform cible = points[indexPoint];
        Vector2 pos = transform.position;
        Vector2 targetPos = cible.position;
        Vector2 dir = (targetPos - pos).normalized;

        rb.linearVelocity = dir * vitesse;

        if (Vector2.Distance(pos, targetPos) < distanceArret)
        {
            rb.linearVelocity = Vector2.zero;
            transform.position = targetPos;
            ArriverAuPoint();
        }
    }

    void ArriverAuPoint()
    {
        bool finDuChemin = false;

        switch (mode)
        {
            case ModeDeplacement.AllerSimple:
                indexPoint++;
                if (indexPoint >= points.Length)
                {
                    Destroy(gameObject);
                    return;
                }
                break;

            case ModeDeplacement.Boucle:
                indexPoint++;
                if (indexPoint >= points.Length)
                {
                    // Téléportation discrète au premier point
                    transform.position = points[0].position;
                    indexPoint = 1;
                    finDuChemin = true;
                }
                break;

            case ModeDeplacement.AllerRetour:
                indexPoint += direction;
                if (indexPoint >= points.Length)
                {
                    indexPoint = points.Length - 2;
                    direction = -1;
                    finDuChemin = true;
                }
                else if (indexPoint < 0)
                {
                    indexPoint = 1;
                    direction = 1;
                    finDuChemin = true;
                }
                break;
        }

        if (tempsPause > 0f && finDuChemin)
        {
            StartCoroutine(Pause());
        }
    }

    IEnumerator Pause()
    {
        enPause = true;
        rb.linearVelocity = Vector2.zero;
        SetAnimation(false);
        yield return new WaitForSeconds(tempsPause);
        enPause = false;
    }

    void MettreAJourAnimation()
    {
        if (anim == null) return;

        Vector2 vel = rb.linearVelocity;
        bool bouge = vel.magnitude > 0.1f;

        if (bouge)
        {
            lastDirection = vel.normalized;
            anim.SetFloat("MoveX", lastDirection.x);
            anim.SetFloat("MoveY", lastDirection.y);
            anim.SetFloat("LastMoveX", lastDirection.x);
            anim.SetFloat("LastMoveY", lastDirection.y);
            anim.SetBool("IsMoving", true);
        }
        else
        {
            anim.SetFloat("MoveX", 0f);
            anim.SetFloat("MoveY", 0f);
            anim.SetBool("IsMoving", false);
        }
    }

    void SetAnimation(bool bouge)
    {
        if (anim == null) return;
        anim.SetBool("IsMoving", bouge);
        if (!bouge)
        {
            anim.SetFloat("MoveX", 0f);
            anim.SetFloat("MoveY", 0f);
        }
    }

    void InitAnimation()
    {
        if (anim == null) return;
        anim.SetFloat("MoveX", lastDirection.x);
        anim.SetFloat("MoveY", lastDirection.y);
        anim.SetFloat("LastMoveX", lastDirection.x);
        anim.SetFloat("LastMoveY", lastDirection.y);
        anim.SetBool("IsMoving", false);
    }

    // Gizmos pour visualiser le chemin dans l'éditeur
    void OnDrawGizmos()
    {
        if (points == null || points.Length < 2) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null) continue;

            Gizmos.DrawSphere(points[i].position, 0.15f);

            if (i < points.Length - 1 && points[i + 1] != null)
            {
                Gizmos.DrawLine(points[i].position, points[i + 1].position);
            }
        }

        // Flèche pour indiquer le sens
        if (mode == ModeDeplacement.AllerRetour && points.Length >= 2
            && points[0] != null && points[points.Length - 1] != null)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
            Gizmos.DrawLine(points[points.Length - 1].position, points[0].position);
        }
    }
}
