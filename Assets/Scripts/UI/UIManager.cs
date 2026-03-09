using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI scoreTextAdded;
    [SerializeField] private TextMeshProUGUI timerText;


    private void Awake()
    {
        Instance = this;
    }

    public void UpdateScore(int scoreToAdd)
    {
        ScoreEffect scoreEffect = scoreTextAdded.GetComponent<ScoreEffect>();
        scoreEffect.PlayText(scoreToAdd.ToString());
        scoreText.text = GameManager.Instance.GetScore().ToString();
    }

    /// <summary>
    /// Définit le score directement (pour sync réseau)
    /// </summary>
    public void SetScore(int score)
    {
        if (scoreText != null)
        {
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
