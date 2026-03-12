using System.Collections;
using TMPro;
using UnityEngine;

public class LoadingScreenUI : MonoBehaviour
{
    public static LoadingScreenUI Instance;

    [Header("UI Elements")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TextMeshProUGUI loadingText;

    private Coroutine dotsCoroutine;
    private string baseText = "En attente des joueurs";

    private void Awake()
    {
        Instance = this;
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    public void Show(string message = null)
    {
        if (message != null)
            baseText = message;

        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        if (dotsCoroutine != null)
            StopCoroutine(dotsCoroutine);
        dotsCoroutine = StartCoroutine(AnimateDots());
    }

    public void Hide()
    {
        if (dotsCoroutine != null)
        {
            StopCoroutine(dotsCoroutine);
            dotsCoroutine = null;
        }

        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    private IEnumerator AnimateDots()
    {
        int dotCount = 0;
        while (true)
        {
            dotCount = (dotCount % 3) + 1;
            if (loadingText != null)
                loadingText.text = baseText + new string('.', dotCount);
            yield return new WaitForSeconds(0.5f);
        }
    }
}
