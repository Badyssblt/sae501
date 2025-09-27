using UnityEngine;

[CreateAssetMenu(fileName = "ItemDCommeata", menuName = "Scriptable Objects/Item")]
public class ItemData : ScriptableObject
{
    [Header("Base Info")]
    public string name;
    public Sprite sprite;


    [Header("Crafting Info")]
    public CounterType counterType;
    public int secondsToTransform;
    public ItemData itemCrafted;
    public AudioClip soundToTransform;

    [Header("Extra")]
    public ItemCategory category;
    public int scoreCount = 1;

}

public enum ItemCategory
{
    Ingredient,
    Food
}
