using UnityEngine;

[CreateAssetMenu(fileName = "ItemDCommeata", menuName = "Scriptable Objects/Item")]
public class ItemData : ScriptableObject
{
    [Header("Base Info")]
    public string name;
    public Sprite sprite;

    [Header("Extra")]
    public ItemCategory category;

}

public enum ItemCategory
{
    Ingredient,
    Food
}
