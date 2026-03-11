using UnityEngine;
using System.Collections;

public class PlayerSpawnEffect : MonoBehaviour
{
    [Header("Drop Settings")]
    [SerializeField] private float dropHeight = 8f;
    [SerializeField] private float dropDuration = 2f;
    [SerializeField] private AnimationCurve dropCurve;

    [Header("Landing Effect")]
    [SerializeField] private float squashAmount = 0.3f;
    [SerializeField] private float squashDuration = 0.15f;
    [SerializeField] private float stretchBackDuration = 0.1f;

    [Header("Sounds")]
    [SerializeField] private AudioClip fallingSound;
    [SerializeField] private AudioClip landingSound;
    [SerializeField] [Range(0f, 1f)] private float soundVolume = 1f;

    [Header("Shadow (Optional)")]
    [SerializeField] private GameObject shadowPrefab;

    private void Awake()
    {
        // Courbe par défaut : départ lent puis accélération (gravité réaliste)
        if (dropCurve == null || dropCurve.length == 0)
        {
            dropCurve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(1f, 1f, 2f, 0f)
            );
        }
    }

    /// <summary>
    /// Place le joueur en hauteur immédiatement, puis attend delay secondes avant de commencer la chute.
    /// La chute dure fallDuration secondes. onLanded est appelé quand le joueur touche le sol.
    /// </summary>
    public void PlayDropEffect(float delay, float fallDuration, System.Action onLanded = null)
    {
        dropDuration = fallDuration;
        StartCoroutine(DropCoroutine(delay, onLanded));
    }

    /// <summary>
    /// Version simple sans délai (chute immédiate avec durée par défaut)
    /// </summary>
    public void PlayDropEffect(System.Action onComplete = null)
    {
        StartCoroutine(DropCoroutine(0f, onComplete));
    }

    private IEnumerator DropCoroutine(float delay, System.Action onLanded)
    {
        Vector3 targetPos = transform.position;
        Vector3 startPos = targetPos + Vector3.up * dropHeight;
        transform.position = startPos;

        // Optional growing shadow
        GameObject shadow = null;
        if (shadowPrefab != null)
        {
            shadow = Instantiate(shadowPrefab, targetPos, Quaternion.identity);
            shadow.transform.localScale = new Vector3(0.2f, 0.2f, 1f);
        }

        // Attendre avant de commencer la chute
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        // Play falling sound
        AudioSource audioSource = null;
        if (fallingSound != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = fallingSound;
            audioSource.volume = soundVolume;
            audioSource.Play();
        }

        // Drop animation
        float elapsed = 0f;
        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = dropCurve.Evaluate(Mathf.Clamp01(elapsed / dropDuration));
            transform.position = Vector3.Lerp(startPos, targetPos, t);

            if (shadow != null)
            {
                float scale = Mathf.Lerp(0.2f, 1f, t);
                shadow.transform.localScale = new Vector3(scale, scale, 1f);
            }

            yield return null;
        }

        transform.position = targetPos;

        if (shadow != null)
        {
            Destroy(shadow);
        }

        // Stop falling sound and play landing sound
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
        if (landingSound != null)
            AudioSource.PlayClipAtPoint(landingSound, transform.position, soundVolume);

        // Squash on landing
        yield return StartCoroutine(SquashAndStretch());

        onLanded?.Invoke();
    }

    private IEnumerator SquashAndStretch()
    {
        Vector3 originalScale = transform.localScale;

        // Squash (wide and short)
        Vector3 squashScale = new Vector3(
            originalScale.x * (1f + squashAmount),
            originalScale.y * (1f - squashAmount),
            originalScale.z
        );

        float elapsed = 0f;
        while (elapsed < squashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / squashDuration;
            transform.localScale = Vector3.Lerp(originalScale, squashScale, t);
            yield return null;
        }

        // Stretch back to normal
        elapsed = 0f;
        while (elapsed < stretchBackDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / stretchBackDuration;
            transform.localScale = Vector3.Lerp(squashScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
    }
}
