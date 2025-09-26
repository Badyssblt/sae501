using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SliderTime : MonoBehaviour
{
    [SerializeField] private Slider timerSlider;
    [SerializeField] private GameObject sliderContainer; // si tu veux activer/désactiver l'objet parent du slider

    private Coroutine timerCoroutine;

    private void Awake()
    {
        if (timerSlider == null)
            timerSlider = GetComponent<Slider>();

        if (sliderContainer == null && timerSlider != null)
            sliderContainer = timerSlider.gameObject;
    }

    public void StartTimer(float duration)
    {
        // Si un timer est déjà en cours → l'arrêter
        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        timerCoroutine = StartCoroutine(TimerRoutine(duration));
    }

    private IEnumerator TimerRoutine(float duration)
    {
        if (sliderContainer != null)
            sliderContainer.SetActive(true);

        timerSlider.minValue = 0f;   // Début à 0
        timerSlider.maxValue = duration;
        timerSlider.value = 0f;       // Valeur initiale à 0

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            timerSlider.value = Mathf.Clamp(elapsed, 0f, duration); // Monte avec le temps
            yield return null;
        }

        HideSlider();
    }


    public void HideSlider()
    {
        if (sliderContainer != null)
            sliderContainer.SetActive(false);

        timerCoroutine = null;
    }
}
