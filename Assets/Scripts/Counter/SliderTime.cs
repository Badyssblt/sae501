using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SliderTime : MonoBehaviour
{
    [SerializeField] private Slider timerSlider;
    [SerializeField] private GameObject sliderContainer;
    [SerializeField] private Image fillImage; // Image du fill du slider

    private Coroutine timerCoroutine;

    private void Awake()
    {
        if (timerSlider == null)
            timerSlider = GetComponent<Slider>();

        if (sliderContainer == null && timerSlider != null)
            sliderContainer = timerSlider.gameObject;

        if (fillImage == null && timerSlider != null && timerSlider.fillRect != null)
            fillImage = timerSlider.fillRect.GetComponent<Image>();
    }

    public void ResetSlider()
    {
        if (timerSlider != null)
        {
            timerSlider.value = 0f;
            UpdateSliderColor(0f);
        }
    }

    public void StopTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        ResetSlider();
        HideSlider();
    }

    public void StartTimer(float duration)
    {
        // Toujours reset avant de lancer un nouveau timer
        StopTimer();
        ResetSlider();

        timerCoroutine = StartCoroutine(TimerRoutine(duration));
    }

    private IEnumerator TimerRoutine(float duration)
    {
        if (sliderContainer != null)
            sliderContainer.SetActive(true);

        timerSlider.minValue = 0f;
        timerSlider.maxValue = duration;
        timerSlider.value = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            timerSlider.value = Mathf.Clamp(elapsed, 0f, duration);

            // Mettre à jour la couleur en fonction du pourcentage
            float percentage = timerSlider.value / duration;
            UpdateSliderColor(percentage);

            yield return null;
        }

        HideSlider();
    }

    private void UpdateSliderColor(float percentage)
    {
        if (fillImage == null) return;

        if (percentage <= 0.33f)
            fillImage.color = Color.red;
        else if (percentage <= 0.66f)
            fillImage.color = new Color(1f, 0.64f, 0f); // Orange
        else
            fillImage.color = Color.green;
    }

    public void HideSlider()
    {
        if (sliderContainer != null)
            sliderContainer.SetActive(false);

        timerCoroutine = null;
    }

    /// <summary>
    /// Définit directement la progression du slider (pour sync réseau)
    /// </summary>
    public void SetProgress(float progress)
    {
        if (timerSlider == null) return;

        // Afficher le slider
        if (sliderContainer != null)
            sliderContainer.SetActive(true);

        // Configurer comme pourcentage (0-1)
        timerSlider.minValue = 0f;
        timerSlider.maxValue = 1f;
        timerSlider.value = Mathf.Clamp01(progress);

        UpdateSliderColor(progress);
    }
}
