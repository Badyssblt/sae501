using UnityEngine;

/// <summary>
/// Affiche l'item tenu par le joueur au-dessus de sa tête via un SpriteRenderer.
/// Assigner le SpriteRenderer enfant dans l'Inspector pour configurer librement le rendu.
/// </summary>
public class ItemHolder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer itemRenderer;

    public void ShowItem(ItemData item)
    {
        if (item != null && itemRenderer != null)
        {
            itemRenderer.sprite = item.sprite;
            itemRenderer.enabled = true;
        }
        else
        {
            Clear();
        }
    }

    public void Clear()
    {
        if (itemRenderer != null)
        {
            itemRenderer.sprite = null;
            itemRenderer.enabled = false;
        }
    }
}
