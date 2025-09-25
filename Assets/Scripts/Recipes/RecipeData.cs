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
        // Les deux listes doivent avoir le même nombre d'éléments
        if (itemsOnCounter.Count != ingredients.Length)
            return false;

        // Copies temporaires pour comparer sans modifier les originales
        List<ItemData> tempIngredients = new List<ItemData>(ingredients);
        List<ItemData> tempItems = new List<ItemData>(itemsOnCounter);

        // Vérifie que chaque ingrédient est présent
        foreach (var ingredient in tempIngredients)
        {
            if (tempItems.Contains(ingredient))
                tempItems.Remove(ingredient); // supprime pour gérer les doublons
            else
                return false; // ingrédient manquant
        }

        return tempItems.Count == 0; // tous les ingrédients trouvés
    }
}
