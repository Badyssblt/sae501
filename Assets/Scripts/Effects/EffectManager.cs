using System.Collections;
using UnityEngine;
using CookMoiCa.Network;

public enum EffectType
{
    // Malus
    SolGlissant,
    VitesseReduite,
    // Bonus
    SprintBoost,
    MultiplicateurPoints,
    LivraisonInstantanee
}

/// <summary>
/// Gère les bonus/malus déclenchés par les séries de commandes réussies ou ratées.
/// Placer ce script sur un GameObject vide dans la scène.
/// </summary>
public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance;

    [Header("Paramètres")]
    [SerializeField] private int seuilConsecutif = 3;  // Nb de commandes d'affilée pour déclencher
    [SerializeField] private float dureeEffet = 5f;

    [Header("Sons")]
    [SerializeField] private AudioClip bonusSound;
    [SerializeField] private AudioClip malusSound;

    // --- États actifs ---
    public bool SolGlissantActif     { get; private set; }
    public bool VitesseReduiteActif { get; private set; }
    public bool SprintBoostActif     { get; private set; }
    public bool LivraisonInstantaneeActif { get; private set; }
    public int  ScoreMultiplier      { get; private set; } = 1;

    private int successConsecutifs = 0;
    private int echecConsecutifs   = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        // Tests rapides (à retirer en prod)
        if (Input.GetKeyDown(KeyCode.F1)) DeclencherMalusAleatoire();
        if (Input.GetKeyDown(KeyCode.F2)) DeclencherBonusAleatoire();
    }

    // ----------------------------------------------------------------
    // Appelé par OrderManager
    // ----------------------------------------------------------------

    public void NotifierSucces()
    {
        echecConsecutifs = 0;
        successConsecutifs++;
        if (successConsecutifs >= seuilConsecutif)
        {
            successConsecutifs = 0;
            DeclencherBonusAleatoire();
        }
    }

    public void NotifierEchec()
    {
        successConsecutifs = 0;
        echecConsecutifs++;
        if (echecConsecutifs >= seuilConsecutif)
        {
            echecConsecutifs = 0;
            DeclencherMalusAleatoire();
        }
    }

    // ----------------------------------------------------------------
    // Déclenchement
    // ----------------------------------------------------------------

    private void DeclencherBonusAleatoire()
    {
        if (bonusSound != null) AudioSource.PlayClipAtPoint(bonusSound, Camera.main.transform.position);
        EffectType[] bonus = { EffectType.SprintBoost, EffectType.MultiplicateurPoints, EffectType.LivraisonInstantanee };
        StartCoroutine(AppliquerEffet(bonus[Random.Range(0, bonus.Length)]));
    }

    private void DeclencherMalusAleatoire()
    {
        if (malusSound != null) AudioSource.PlayClipAtPoint(malusSound, Camera.main.transform.position);
        EffectType[] malus = { EffectType.SolGlissant, EffectType.VitesseReduite };
        StartCoroutine(AppliquerEffet(malus[Random.Range(0, malus.Length)]));
    }

    private IEnumerator AppliquerEffet(EffectType type, bool sendNetwork = true)
    {
        ActiverEffet(type, true);
        AfficherNotification(type);

        // Envoyer l'event au client distant
        if (sendNetwork && NetworkManager.Instance != null && NetworkManager.Instance.Role == NetworkRole.Host)
        {
            NetworkManager.Instance.SendGameEvent("effectActivated", new EffectActivatedEvent
            {
                effectType = type.ToString(),
                duration = dureeEffet
            });
        }

        yield return new WaitForSeconds(dureeEffet);
        ActiverEffet(type, false);
    }

    /// <summary>
    /// Appelé côté client quand le host notifie un effet
    /// </summary>
    public void ApplyNetworkEffect(string effectTypeName, float duration)
    {
        if (System.Enum.TryParse<EffectType>(effectTypeName, out EffectType type))
        {
            StartCoroutine(AppliquerEffetReseau(type, duration));
        }
    }

    private IEnumerator AppliquerEffetReseau(EffectType type, float duration)
    {
        ActiverEffet(type, true);
        AfficherNotification(type);
        yield return new WaitForSeconds(duration);
        ActiverEffet(type, false);
    }

    private void ActiverEffet(EffectType type, bool actif)
    {
        switch (type)
        {
            case EffectType.SolGlissant:
                SolGlissantActif = actif;
                break;
            case EffectType.VitesseReduite:
                VitesseReduiteActif = actif;
                break;
            case EffectType.SprintBoost:
                SprintBoostActif = actif;
                break;
            case EffectType.MultiplicateurPoints:
                ScoreMultiplier = actif ? 2 : 1;
                break;
            case EffectType.LivraisonInstantanee:
                LivraisonInstantaneeActif = actif;
                break;
        }
    }

    // ----------------------------------------------------------------
    // Notification
    // ----------------------------------------------------------------

    private void AfficherNotification(EffectType type)
    {
        bool isBonus = type == EffectType.SprintBoost
                    || type == EffectType.MultiplicateurPoints
                    || type == EffectType.LivraisonInstantanee;

        string message = type switch
        {
            EffectType.SolGlissant          => "MALUS : Sol glissant !",
            EffectType.VitesseReduite       => "MALUS : Vitesse réduite !",
            EffectType.SprintBoost           => "BONUS : Sprint x2 !",
            EffectType.MultiplicateurPoints  => "BONUS : Score x2 !",
            EffectType.LivraisonInstantanee  => "BONUS : Livraison instantanee !",
            _                                => ""
        };

        EffectNotificationUI.Instance?.ShowNotification(message, isBonus, dureeEffet);
    }
}
