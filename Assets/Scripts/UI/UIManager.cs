using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI scoreTextAdded;


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
}
