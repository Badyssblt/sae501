using UnityEngine;

[CreateAssetMenu(
    fileName = "CounterTypeScriptable",
    menuName = "Scriptable Objects/CounterType Scriptable",
    order = 1)]
public class CounterTypeScriptable : ScriptableObject
{
    public CounterType type;
    public Sprite counterSprite;
    public AudioClip soundOnUse;
    public bool needPlayerFreeze;
    // Est-ce que l'item doit être afficher (ex: on n'affiche pas des frites dans une friteuse)
    public bool itemNeedHidden;
}
