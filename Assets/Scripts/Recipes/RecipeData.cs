using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "RecipeData", menuName = "Scriptable Objects/RecipeData")]
public class RecipeData : ScriptableObject
{
    public ItemData[] ingredients;
    public ItemData result;

    public bool Matches(List<ItemData> itemsOnCounter)
    {
        // Les deux listes doivent avoir le m�me nombre d'�l�ments
        if (itemsOnCounter.Count != ingredients.Length)
        {
            Debug.Log($"  [Match] Nombre différent: {itemsOnCounter.Count} items vs {ingredients.Length} ingrédients requis");
            return false;
        }

        // Copies temporaires pour comparer sans modifier les originales
        List<ItemData> tempIngredients = new List<ItemData>(ingredients);
        List<ItemData> tempItems = new List<ItemData>(itemsOnCounter);

        Debug.Log($"  [Match] Recette requiert:");
        foreach (var ing in ingredients)
        {
            Debug.Log($"    - {ing.name}");
        }

        // V�rifie que chaque ingr�dient est pr�sent
        foreach (var ingredient in tempIngredients)
        {
            if (tempItems.Contains(ingredient))
            {
                tempItems.Remove(ingredient); // supprime pour g�rer les doublons
                Debug.Log($"  [Match] ✓ {ingredient.name} trouvé");
            }
            else
            {
                Debug.Log($"  [Match] ✗ {ingredient.name} manquant");
                return false; // ingr�dient manquant
            }
        }

        bool allMatched = tempItems.Count == 0;
        Debug.Log($"  [Match] Résultat: {(allMatched ? "✓ MATCH" : "✗ Items restants")}");
        return allMatched; // tous les ingr�dients trouv�s
    }
}
