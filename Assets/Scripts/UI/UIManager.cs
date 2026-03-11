using System.Collections;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI scoreTextAdded;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Score Juice")]
    [SerializeField] private float rollDuration = 0.4f;
    [SerializeField] private float punchScale = 1.4f;
    [SerializeField] private float punchDuration = 0.2f;

    private int displayedScore;
    private Coroutine scoreCoroutine;

    private void Awake()
    {
        Instance = this;
    }

    public void UpdateScore(int scoreToAdd)
    {
        ScoreEffect scoreEffect = scoreTextAdded.GetComponent<ScoreEffect>();
        scoreEffect.PlayText(scoreToAdd.ToString());

        int targetScore = GameManager.Instance.GetScore();
        if (scoreCoroutine != null)
            StopCoroutine(scoreCoroutine);
        scoreCoroutine = StartCoroutine(RollAndPunch(displayedScore, targetScore));
    }

    private IEnumerator RollAndPunch(int from, int to)
    {
        // Phase 1 : roll des chiffres
        for (float t = 0; t < rollDuration; t += Time.deltaTime)
        {
            float n = t / rollDuration;
            displayedScore = (int)Mathf.Lerp(from, to, n);
            scoreText.text = displayedScore.ToString();
            yield return null;
        }
        displayedScore = to;
        scoreText.text = to.ToString();

        // Phase 2 : punch scale
        Vector3 original = Vector3.one;
        Vector3 big = original * punchScale;
        scoreText.transform.localScale = big;

        for (float t = 0; t < punchDuration; t += Time.deltaTime)
        {
            float n = t / punchDuration;
            float eased = 1f - Mathf.Pow(1f - n, 3f); // ease-out cubic
            scoreText.transform.localScale = Vector3.Lerp(big, original, eased);
            yield return null;
        }
        scoreText.transform.localScale = original;

        scoreCoroutine = null;
    }

    /// <summary>
    /// Définit le score directement (pour sync réseau)
    /// </summary>
    public void SetScore(int score)
    {
        if (scoreText != null)
        {
            displayedScore = score;
            scoreText.text = score.ToString();
        }
    }

    /// <summary>
    /// Définit le timer directement (pour sync réseau)
    /// </summary>
    public void SetTimer(float timeLeft)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeLeft / 60);
            int seconds = Mathf.FloorToInt(timeLeft % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }
}
