using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class OrderUI : MonoBehaviour
{
    [SerializeField] private Image itemResult;
    [SerializeField] private Slider timer;
    [SerializeField] private GameObject sliderContainer;
    [SerializeField] private HorizontalLayoutGroup hb;
    [SerializeField] private GameObject ingredientPrefab;
    [SerializeField] private Color colorGreen = Color.green;
    [SerializeField] private Color colorOrange = new Color(1f, 0.65f, 0f); // orange
    [SerializeField] private Color colorRed = Color.red;

    public float maxDelay = 10;
    public RecipeData recipe;

    private Image timerFill; // l'image du fill du slider
    private Coroutine timerCoroutine;

    private void Awake()
    {
        if (timer == null)
            timer = GetComponent<Slider>();

        if (sliderContainer == null && timer != null)
            sliderContainer = timer.gameObject;

        if (timerFill == null && timer != null && timer.fillRect != null)
            timerFill = timer.fillRect.GetComponent<Image>();
    }

    public void UpdateRecipe()
    {
        // Met à jour le sprite du résultat
        itemResult.sprite = recipe.result.sprite;

        // Supprime les anciens ingrédients affichés
        foreach (Transform child in hb.transform)
        {
            Destroy(child.gameObject);
        }

        // Instancie un prefab pour chaque ingrédient
        foreach (ItemData ingredient in recipe.ingredients)
        {
            GameObject go = Instantiate(ingredientPrefab, hb.transform);
            Image img = go.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = ingredient.sprite;
            }
        }

        // Lance le timer de décompte
        StartTimer(maxDelay);
    }

    public void ResetSlider()
    {
        if (timer != null)
        {
            timer.value = timer.maxValue; // Commence à droite (plein)
            UpdateSliderColor(1f); // 100% = vert
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

        timer.minValue = 0f;
        timer.maxValue = duration;
        timer.value = duration; // Commence plein (à droite)

        float remaining = duration;

        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            timer.value = Mathf.Clamp(remaining, 0f, duration);

            // Mettre à jour la couleur en fonction du pourcentage restant
            float percentage = remaining / duration;
            UpdateSliderColor(percentage);

            yield return null;
        }

        // Forcer affichage final à 0 une dernière fois
        timer.value = 0f;
        UpdateSliderColor(0f);

        // Petite attente d'une frame pour que le 0 s'affiche bien
        yield return null;

    }

    private void UpdateSliderColor(float percentage)
    {
        if (timerFill == null) return;

        if (percentage > 0.66f)
            timerFill.color = colorGreen;
        else if (percentage > 0.33f)
            timerFill.color = colorOrange;
        else
            timerFill.color = colorRed;
    }

    public void HideSlider()
    {
        if (sliderContainer != null)
            sliderContainer.SetActive(false);
        timerCoroutine = null;
    }

    /// <summary>
    /// Retourne le temps restant pour cette commande (pour le réseau)
    /// </summary>
    public float GetTimeRemaining()
    {
        if (timer != null)
            return timer.value;
        return 0f;
    }
}