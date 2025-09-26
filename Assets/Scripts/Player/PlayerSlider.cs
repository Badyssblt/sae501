using UnityEngine;
using UnityEngine.UI;

public class PlayerSlider : MonoBehaviour
{
    [Header("Slider Timer Settings")]
    public Slider slider;
    [HideInInspector] public float currentTime; // mis à jour par la coroutine
    private RectTransform sliderRect;

    void Start()
    {
        if (slider == null)
        {
            Debug.LogError("Slider UI non assigné !");
            return;
        }

        sliderRect = slider.GetComponent<RectTransform>();
        slider.gameObject.SetActive(false); // caché par défaut
    }

    void Update()
    {
       
    }

    // Initialiser et afficher le slider pour une durée donnée
    public void StartTimer(float duration)
    {
        slider.maxValue = duration;
        currentTime = duration;
        slider.gameObject.SetActive(true);
    }

    // Cacher le slider
    public void HideSlider()
    {
        slider.gameObject.SetActive(false);
    }
}
