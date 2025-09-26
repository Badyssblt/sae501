using UnityEngine;
using UnityEngine.UI;

public class PlayerSlider : MonoBehaviour
{
    [Header("Slider Timer Settings")]
    public Slider slider;            // Le slider UI
    public Vector3 offset = new Vector3(0, 1.5f, 0); // Position au-dessus du joueur
    public float timerDuration = 5f; // Temps total du slider
    private float currentTime;

    void Start()
    {
        if (slider == null)
        {
            Debug.LogError("Slider UI non assigné !");
            return;
        }

        currentTime = timerDuration;
        slider.maxValue = timerDuration;
        slider.value = currentTime;
        slider.gameObject.SetActive(true); // Affiche le slider au début
    }

    void Update()
    {
        // 1️⃣ Mettre à jour la position au-dessus du joueur
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + offset);
        slider.GetComponent<RectTransform>().position = screenPos;

        // 2️⃣ Réduire le temps
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            slider.value = currentTime;
        }
        else
        {
            slider.value = 0;
            // Optionnel : cacher le slider quand le temps est écoulé
            // slider.gameObject.SetActive(false);
        }
    }

    // Réinitialiser le timer si nécessaire
    public void ResetTimer()
    {
        currentTime = timerDuration;
        slider.value = currentTime;
        slider.gameObject.SetActive(true);
    }
}
