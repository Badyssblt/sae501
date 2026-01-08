using UnityEngine;

public class SprintSmokeEffect : MonoBehaviour
{
    [Header("Smoke Sprites")]
    [SerializeField] private Sprite[] smokeSprites; // Smoke_0, Smoke_1, Smoke_2, etc.

    [Header("Smoke Settings")]
    [SerializeField] private GameObject smokePrefab; // Prefab avec SpriteRenderer
    [SerializeField] private float smokeSpawnInterval = 0.1f; // Intervalle entre chaque fumée
    [SerializeField] private int maxSmokes = 5; // Nombre max de fumées affichées
    [SerializeField] private float smokeAnimationSpeed = 0.1f; // Vitesse d'animation des frames
    [SerializeField] private Vector2 smokeOffset = new Vector2(0, -0.3f); // Offset par rapport au joueur
    [SerializeField] private float smokeScale = 0.5f; // Taille des fumées (1 = taille normale)

    private PlayerMovement playerMovement;
    private float spawnTimer = 0f;
    private SmokeParticle[] activeSmokes;
    private int currentSmokeIndex = 0;

    private class SmokeParticle
    {
        public GameObject gameObject;
        public SpriteRenderer spriteRenderer;
        public float lifeTime;
        public int currentFrame;
        public float frameTimer;
    }

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        activeSmokes = new SmokeParticle[maxSmokes];
    }

    void Update()
    {
        if (playerMovement == null) return;

        // Spawner de la fumée si le joueur sprinte
        if (playerMovement.IsSprinting())
        {
            spawnTimer += Time.deltaTime;

            if (spawnTimer >= smokeSpawnInterval)
            {
                SpawnSmoke();
                spawnTimer = 0f;
            }
        }

        // Mettre à jour les fumées actives
        UpdateSmokes();
    }

    void SpawnSmoke()
    {
        if (smokeSprites == null || smokeSprites.Length == 0) return;

        // Réutiliser un slot de fumée existant (système circulaire)
        if (activeSmokes[currentSmokeIndex] != null && activeSmokes[currentSmokeIndex].gameObject != null)
        {
            Destroy(activeSmokes[currentSmokeIndex].gameObject);
        }

        // Créer une nouvelle fumée à la position du joueur
        Vector3 smokePosition = transform.position + (Vector3)smokeOffset;
        GameObject smokeObj = new GameObject("Smoke");
        smokeObj.transform.position = smokePosition;
        smokeObj.transform.localScale = Vector3.one * smokeScale; // Appliquer la taille

        SpriteRenderer sr = smokeObj.AddComponent<SpriteRenderer>();
        sr.sprite = smokeSprites[0]; // Commence avec Smoke_0
        sr.sortingLayerName = "Main"; // Layer Main
        sr.sortingOrder = -1; // Derrière le joueur

        // Créer l'objet SmokeParticle
        SmokeParticle smoke = new SmokeParticle
        {
            gameObject = smokeObj,
            spriteRenderer = sr,
            lifeTime = smokeSprites.Length * smokeAnimationSpeed,
            currentFrame = 0,
            frameTimer = 0f
        };

        activeSmokes[currentSmokeIndex] = smoke;
        currentSmokeIndex = (currentSmokeIndex + 1) % maxSmokes;
    }

    void UpdateSmokes()
    {
        for (int i = 0; i < activeSmokes.Length; i++)
        {
            if (activeSmokes[i] == null || activeSmokes[i].gameObject == null) continue;

            SmokeParticle smoke = activeSmokes[i];
            smoke.lifeTime -= Time.deltaTime;
            smoke.frameTimer += Time.deltaTime;

            // Changer de frame d'animation
            if (smoke.frameTimer >= smokeAnimationSpeed && smoke.currentFrame < smokeSprites.Length - 1)
            {
                smoke.currentFrame++;
                smoke.spriteRenderer.sprite = smokeSprites[smoke.currentFrame];
                smoke.frameTimer = 0f;
            }

            // Faire disparaître progressivement
            float alpha = Mathf.Clamp01(smoke.lifeTime / (smokeSprites.Length * smokeAnimationSpeed));
            Color color = smoke.spriteRenderer.color;
            color.a = alpha;
            smoke.spriteRenderer.color = color;

            // Détruire si la vie est écoulée
            if (smoke.lifeTime <= 0f)
            {
                Destroy(smoke.gameObject);
                activeSmokes[i] = null;
            }
        }
    }

    void OnDestroy()
    {
        // Nettoyer toutes les fumées
        for (int i = 0; i < activeSmokes.Length; i++)
        {
            if (activeSmokes[i] != null && activeSmokes[i].gameObject != null)
            {
                Destroy(activeSmokes[i].gameObject);
            }
        }
    }
}
