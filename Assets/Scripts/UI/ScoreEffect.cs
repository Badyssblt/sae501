using System.Collections;
using TMPro;
using UnityEngine;

public class ScoreEffect : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI tmpText;
    [SerializeField] float duration = 0.5f; // plus petit = plus rapide
    [SerializeField] float moveUp = 50f;

    public void PlayText(string message)
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(Animate(message));
    }

    IEnumerator Animate(string message)
    {
        tmpText.text = message;
        tmpText.alpha = 1f;

        Vector3 start = transform.localPosition;
        Vector3 end = start + Vector3.up * moveUp;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float n = t / duration;
            transform.localPosition = Vector3.Lerp(start, end, n);
            tmpText.alpha = 1 - n; // fondu progressif
            yield return null;
        }

        transform.localPosition = start;
        tmpText.alpha = 0f;
        gameObject.SetActive(false);
    }
}
