using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    [SerializeField] private ItemData item;

    [SerializeField] private Image itemIcon;

    private Coroutine punchCoroutine;

    private void Awake()
    {
        if (itemIcon == null)
        {
            // Cherche automatiquement un enfant nomm� "ItemIcon"
            itemIcon = transform.Find("ItemIcon")?.GetComponent<Image>();
        }
    }

    private void Start()
    {
        // Masquer l'icône au démarrage si aucun item n'est défini
        if (item == null)
        {
            Clear();
        }
    }

    public void SetItem(ItemData newItem)
    {
        item = newItem;

        if (item != null && itemIcon != null)
        {
            itemIcon.sprite = item.sprite;
            itemIcon.enabled = true;
            PunchScale();
        }
        else
        {
            Clear();
        }
    }

    private void PunchScale()
    {
        if (punchCoroutine != null) StopCoroutine(punchCoroutine);
        punchCoroutine = StartCoroutine(PunchScaleCoroutine(itemIcon.transform, 1.3f, 0.15f));
    }

    private IEnumerator PunchScaleCoroutine(Transform target, float intensity, float duration)
    {
        Vector3 original = Vector3.one;
        Vector3 big = original * intensity;
        target.localScale = big;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float n = t / duration;
            float eased = 1f - Mathf.Pow(1f - n, 3f);
            target.localScale = Vector3.Lerp(big, original, eased);
            yield return null;
        }
        target.localScale = original;
        punchCoroutine = null;
    }

    public void Clear()
    {
        item = null;
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }
    }

    public ItemData GetItem()
    {
        return item;
    }
}
