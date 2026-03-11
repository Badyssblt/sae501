using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Affiche les messages de bonus/malus avec une animation.
/// Setup Unity : Canvas > Panel (Image + CanvasGroup) > Text (TMP)
/// Assigner les champs messageText et background dans l'Inspector.
/// </summary>
public class EffectNotificationUI : MonoBehaviour
{
    public static EffectNotificationUI Instance;

    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Image background;

    [Header("Couleurs")]
    [SerializeField] private Color bonusColor = new Color(0.15f, 0.75f, 0.15f, 0.92f);
    [SerializeField] private Color malusColor = new Color(0.8f, 0.15f, 0.15f, 0.92f);

    private CanvasGroup canvasGroup;
    private Coroutine notificationCoroutine;

    private void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        gameObject.SetActive(false);
    }

    public void ShowNotification(string message, bool isBonus, float duration)
    {
        if (notificationCoroutine != null)
            StopCoroutine(notificationCoroutine);
        gameObject.SetActive(true); // Activer avant de démarrer la coroutine
        notificationCoroutine = StartCoroutine(Animer(message, isBonus, duration));
    }

    private IEnumerator Animer(string message, bool isBonus, float duration)
    {
        canvasGroup.alpha = 1f;

        if (messageText != null)
            messageText.text = message;
        if (background != null)
            background.color = isBonus ? bonusColor : malusColor;

        // Apparition : punch scale
        transform.localScale = Vector3.zero;
        const float apparitionDuree = 0.25f;
        for (float t = 0; t < apparitionDuree; t += Time.deltaTime)
        {
            float n = t / apparitionDuree;
            float eased = 1f - Mathf.Pow(1f - n, 3f);
            transform.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one * 1.12f, eased);
            yield return null;
        }

        // Retour à l'échelle normale
        const float settleDuree = 0.1f;
        for (float t = 0; t < settleDuree; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(Vector3.one * 1.12f, Vector3.one, t / settleDuree);
            yield return null;
        }
        transform.localScale = Vector3.one;

        // Affichage
        yield return new WaitForSeconds(duration - 0.5f);

        // Disparition : fade out
        const float fadeDuree = 0.5f;
        for (float t = 0; t < fadeDuree; t += Time.deltaTime)
        {
            canvasGroup.alpha = 1f - (t / fadeDuree);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        gameObject.SetActive(false);
        notificationCoroutine = null;
    }
}
