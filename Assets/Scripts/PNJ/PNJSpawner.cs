using UnityEngine;

public class PNJSpawner : MonoBehaviour
{
    [Header("Configuration Spawn")]
    [SerializeField] private GameObject pnjPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform[] cheminPoints;

    [Header("Espacement PNJ")]
    [SerializeField] private float offsetEntreClients = 0.5f; // Distance entre chaque client au comptoir

    [Header("Timing")]
    [SerializeField] private float intervalSpawn = 10f;
    [SerializeField] private float delaiAvantPremierSpawn = 3f;

    [Header("Limites")]
    [SerializeField] private int maxPNJSimultanes = 6;

    private float timerSpawn;
    private int pnjActifs = 0;
    private bool isSpawning = false;
    private bool[] positionsOccupees;

    public static PNJSpawner Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        timerSpawn = delaiAvantPremierSpawn;

        // Initialiser le tableau des positions occupées
        positionsOccupees = new bool[maxPNJSimultanes];
    }

    void Update()
    {
        if (!isSpawning) return;

        timerSpawn -= Time.deltaTime;

        if (timerSpawn <= 0 && pnjActifs < maxPNJSimultanes)
        {
            SpawnPNJ();
            timerSpawn = intervalSpawn;
        }
    }

    // Démarre le spawn des PNJ
    public void StartSpawning()
    {
        isSpawning = true;
        timerSpawn = delaiAvantPremierSpawn;
        Debug.Log("PNJSpawner démarré !");
    }

    // Arrête le spawn des PNJ
    public void StopSpawning()
    {
        isSpawning = false;
        Debug.Log("PNJSpawner arrêté !");
    }

    void SpawnPNJ()
    {
        if (pnjPrefab == null || spawnPoint == null || cheminPoints.Length == 0)
        {
            Debug.LogWarning("PNJSpawner: Configuration incomplète !");
            return;
        }

        // Trouver une position libre
        int positionIndex = TrouverPositionLibre();
        if (positionIndex == -1)
        {
            Debug.Log("Aucune position libre !");
            return;
        }

        GameObject newPNJ = Instantiate(pnjPrefab, spawnPoint.position, Quaternion.identity);
        PNJClient client = newPNJ.GetComponent<PNJClient>();

        if (client != null)
        {
            // Assigner le chemin et la position
            client.chemin = cheminPoints;
            client.positionIndex = positionIndex;
            client.offsetEntreClients = offsetEntreClients;

            // Marquer la position comme occupée
            positionsOccupees[positionIndex] = true;
            pnjActifs++;

            // S'abonner à la destruction du PNJ pour libérer la position
            StartCoroutine(SurveillerPNJ(newPNJ, positionIndex));

            Debug.Log("PNJ spawné : " + newPNJ.name + " à la position " + positionIndex + " (" + pnjActifs + "/" + maxPNJSimultanes + ")");
        }
        else
        {
            Debug.LogError("Le prefab PNJ n'a pas de composant PNJClient !");
            Destroy(newPNJ);
        }
    }

    // Trouve la première position libre
    int TrouverPositionLibre()
    {
        for (int i = 0; i < positionsOccupees.Length; i++)
        {
            if (!positionsOccupees[i])
            {
                return i;
            }
        }
        return -1; // Aucune position libre
    }

    System.Collections.IEnumerator SurveillerPNJ(GameObject pnj, int positionIndex)
    {
        // Attendre que le PNJ soit détruit
        while (pnj != null)
        {
            yield return null;
        }

        // Libérer la position
        positionsOccupees[positionIndex] = false;
        pnjActifs--;
        Debug.Log("PNJ détruit. Position " + positionIndex + " libérée. PNJ actifs : " + pnjActifs);
    }

    // Méthode pour dessiner le chemin dans l'éditeur
    private void OnDrawGizmos()
    {
        if (spawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.3f);
        }

        if (cheminPoints != null && cheminPoints.Length > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < cheminPoints.Length; i++)
            {
                if (cheminPoints[i] != null)
                {
                    Gizmos.DrawWireSphere(cheminPoints[i].position, 0.2f);

                    // Dessiner les lignes entre les points
                    if (i > 0 && cheminPoints[i - 1] != null)
                    {
                        Gizmos.DrawLine(cheminPoints[i - 1].position, cheminPoints[i].position);
                    }
                }
            }

            // Dessiner les positions potentielles des clients au dernier point
            if (cheminPoints[cheminPoints.Length - 1] != null)
            {
                Vector3 dernierPoint = cheminPoints[cheminPoints.Length - 1].position;
                Gizmos.color = Color.yellow;
                for (int i = 0; i < maxPNJSimultanes; i++)
                {
                    Vector3 offset = new Vector3(0, i * offsetEntreClients, 0);
                    Gizmos.DrawWireSphere(dernierPoint + offset, 0.15f);
                }
            }
        }
    }
}
