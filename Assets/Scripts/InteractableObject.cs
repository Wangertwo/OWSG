using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
public class InteractableObject : MonoBehaviour
{
    public string ItemName;

    public bool PlayerInRange;
 
    public string GetItemName()
    {
        return ItemName;
    }

    void Update()
    {
        if (PlayerInRange && Input.GetKeyDown(KeyCode.Mouse0) 
            && SelectionManager.Instance.onTarget && SelectionManager.Instance.selectedObject == gameObject)
        {
            if (!CompareTag("pickable"))
            {
                return;
            }

            if (!InventorySystem.Instance.CheckIfFull())
            {
                Debug.Log("Interacted with " + ItemName);
                InventorySystem.Instance.AddToInventory(ItemName);
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayPickupItemSound();
                }
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("Inventory is full");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInRange = false;
        }
    }
}
