using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Musique")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.5f;

    [Header("Accélération fin de partie")]
    [SerializeField] private float seuilAcceleration = 10f;
    [SerializeField] private float pitchMax = 1.4f;

    private AudioSource audioSource;
    private float pitchNormal = 1f;
    private bool isAccelerating = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
        audioSource.pitch = pitchNormal;

        if (musicClip != null)
            audioSource.clip = musicClip;
    }

    public void Play()
    {
        if (audioSource.clip == null) return;
        audioSource.pitch = pitchNormal;
        isAccelerating = false;
        audioSource.Play();
    }

    public void Stop()
    {
        audioSource.Stop();
        audioSource.pitch = pitchNormal;
        isAccelerating = false;
    }

    /// <summary>
    /// Appelé chaque frame avec le temps restant pour gérer l'accélération progressive
    /// </summary>
    public void UpdateTimer(float timeLeft)
    {
        if (!audioSource.isPlaying) return;

        if (timeLeft <= seuilAcceleration && timeLeft > 0f)
        {
            if (!isAccelerating)
            {
                isAccelerating = true;
                Debug.Log("[MusicManager] Accélération de la musique !");
            }

            // Lerp progressif : plus le temps restant est bas, plus le pitch monte
            float t = 1f - (timeLeft / seuilAcceleration);
            audioSource.pitch = Mathf.Lerp(pitchNormal, pitchMax, t);
        }
        else if (timeLeft > seuilAcceleration && isAccelerating)
        {
            // Reset si le timer remonte (ex: bonus temps)
            audioSource.pitch = pitchNormal;
            isAccelerating = false;
        }
    }
}
