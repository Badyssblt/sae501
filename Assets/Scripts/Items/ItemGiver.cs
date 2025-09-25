using UnityEngine;

public class ItemGiver : MonoBehaviour
{
    [SerializeField] private ItemData itemToGive;
    private bool inRange = false;
    InventorySystem inventory;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            inRange = true;
            inventory = collision.GetComponent<InventorySystem>();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            inRange = false;
        }
    }

    private void Update()
    {
        if(inRange && Input.GetButtonDown("P1_B1"))
        {
            inventory.AddItem(itemToGive);
            InventoryUI.Instance.UpdateInventory();
        }
    }
}
