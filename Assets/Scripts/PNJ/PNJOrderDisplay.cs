using UnityEngine;

/// <summary>
/// Affiche le plat commandé au-dessus du PNJ avec une animation
/// </summary>
public class PNJOrderDisplay : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float offsetY = 1.5f; // Distance au-dessus du PNJ
    [SerializeField] private float iconSize = 1.5f; // Taille de l'icône

    [Header("Animation")]
    [SerializeField] private float bounceHeight = 0.15f; // Hauteur du rebond
    [SerializeField] private float bounceSpeed = 3f; // Vitesse de l'animation

    private Transform pnjTransform;
    private RecipeData recipe;
    private GameObject iconObject;
    private bool isInitialized = false;
    private float animationTime = 0f;

    /// <summary>
    /// Initialise l'affichage avec une recette et le PNJ associé
    /// </summary>
    public void Initialize(RecipeData recipe, Transform pnjTransform)
    {
        if (recipe == null)
        {
            Debug.LogError("PNJOrderDisplay: Impossible d'initialiser avec une recette null!");
            return;
        }

        if (pnjTransform == null)
        {
            Debug.LogError("PNJOrderDisplay: Impossible d'initialiser avec un transform null!");
            return;
        }

        this.recipe = recipe;
        this.pnjTransform = pnjTransform;
        this.isInitialized = true;

        CreateResultIcon();
    }

    /// <summary>
    /// Initialise l'affichage directement avec un sprite (fallback quand la recette n'est pas trouvée)
    /// </summary>
    public void InitializeFromSprite(Sprite sprite, Transform pnjTransform)
    {
        if (sprite == null || pnjTransform == null) return;

        this.pnjTransform = pnjTransform;
        this.isInitialized = true;

        GameObject icon = new GameObject($"Result_{sprite.name}");
        icon.transform.SetParent(transform);
        icon.transform.localPosition = Vector3.zero;
        icon.transform.localScale = Vector3.one * iconSize;

        SpriteRenderer sr = icon.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Main";
        sr.sortingOrder = 100;

        iconObject = icon;
    }

    private void CreateResultIcon()
    {
        if (recipe == null || recipe.result == null)
        {
            Debug.LogWarning("PNJOrderDisplay: Recipe ou result null");
            return;
        }

        if (recipe.result.sprite == null)
        {
            Debug.LogWarning($"PNJOrderDisplay: Sprite de {recipe.result.name} est null");
            return;
        }

        // Créer un GameObject pour l'icône du résultat
        GameObject icon = new GameObject($"Result_{recipe.result.name}");
        icon.transform.SetParent(transform);
        icon.transform.localPosition = Vector3.zero;
        icon.transform.localScale = Vector3.one * iconSize;

        // Ajouter un SpriteRenderer
        SpriteRenderer sr = icon.AddComponent<SpriteRenderer>();
        sr.sprite = recipe.result.sprite;
        sr.sortingLayerName = "Main";
        sr.sortingOrder = 100;

        iconObject = icon;
    }

    private void LateUpdate()
    {
        // Ne rien faire tant qu'on n'est pas initialisé
        if (!isInitialized) return;

        // Suivre la position du PNJ
        if (pnjTransform != null)
        {
            // Animation de bounce
            animationTime += Time.deltaTime * bounceSpeed;
            float bounceOffset = Mathf.Sin(animationTime) * bounceHeight;

            transform.position = pnjTransform.position + new Vector3(0, offsetY + bounceOffset, 0);
        }
        else
        {
            // Si le PNJ n'existe plus, se détruire
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Nettoyer l'icône
        if (iconObject != null)
        {
            Destroy(iconObject);
        }
    }
}
