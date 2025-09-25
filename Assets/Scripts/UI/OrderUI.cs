using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class OrderUI : MonoBehaviour
{
    [SerializeField] private Image itemResult;
    [SerializeField] private Slider timer;
    [SerializeField] private HorizontalLayoutGroup hb;
    [SerializeField] private GameObject ingredientPrefab;
    public float maxDelay = 10;
    public RecipeData recipe;


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

        // Lance la coroutine pour détruire l’OrderUI après un délai
        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        float elapsed = 0f;
        Debug.Log(elapsed);
        while (elapsed < maxDelay)
        {
            // Si tu as un Slider pour visualiser le temps restant
            if (timer != null)
            {
                timer.value = 1f - (elapsed / maxDelay);
            }

            elapsed += Time.deltaTime;
            yield return null; // attend la prochaine frame
        }

        Destroy(gameObject);
    }
}
