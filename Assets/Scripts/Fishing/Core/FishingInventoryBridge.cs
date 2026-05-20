using System.Collections.Generic;
using UnityEngine;

public class FishingInventoryBridge : MonoBehaviour
{
    public enum AddFishError
    {
        None,
        InvalidInput,
        InventoryMissing,
        InventoryFull,
        ResourceNotFound,
        PrefabInvalid,
        Exception
    }

    [SerializeField] private InventorySystem inventorySystem;
    [SerializeField] private string fishItemPrefix = "Fish_";

    public AddFishError LastAddFishError { get; private set; }

    public bool TryAddFish(string fishId, int count)
    {
        LastAddFishError = AddFishError.None;

        if (string.IsNullOrWhiteSpace(fishId) || count <= 0)
        {
            LastAddFishError = AddFishError.InvalidInput;
            return false;
        }

        InventorySystem targetInventory = inventorySystem != null ? inventorySystem : InventorySystem.Instance;
        if (targetInventory == null)
        {
            LastAddFishError = AddFishError.InventoryMissing;
            return false;
        }

        if (!targetInventory.HasFreeSlots(count))
        {
            LastAddFishError = AddFishError.InventoryFull;
            return false;
        }

        string itemName = BuildItemName(fishId);
        GameObject itemPrefab = Resources.Load<GameObject>(itemName);
        if (itemPrefab == null)
        {
            LastAddFishError = AddFishError.ResourceNotFound;
            Debug.LogWarning("FishingInventoryBridge: missing Resources item prefab: " + itemName, this);
            return false;
        }

        if (!IsValidInventoryPrefab(itemPrefab))
        {
            LastAddFishError = AddFishError.PrefabInvalid;
            Debug.LogWarning("FishingInventoryBridge: invalid inventory prefab: " + itemName + ". It must include RectTransform + InventoryItem.", this);
            return false;
        }

        try
        {
            targetInventory.AddToInventory(itemName, count);
            return true;
        }
        catch (System.Exception ex)
        {
            LastAddFishError = AddFishError.Exception;
            Debug.LogError("FishingInventoryBridge: failed to add fish item " + itemName + ". " + ex.Message, this);
            return false;
        }
    }

    public bool TryRemoveFish(string fishId, int count)
    {
        if (string.IsNullOrWhiteSpace(fishId) || count <= 0)
        {
            return false;
        }

        InventorySystem targetInventory = inventorySystem != null ? inventorySystem : InventorySystem.Instance;
        if (targetInventory == null)
        {
            return false;
        }

        string itemName = BuildItemName(fishId);
        if (!HasEnoughFish(targetInventory, itemName, count))
        {
            return false;
        }

        targetInventory.RemoveItem(itemName, count);
        targetInventory.ReCalculateList();
        return true;
    }

    public int CountFish(string fishId)
    {
        if (string.IsNullOrWhiteSpace(fishId))
        {
            return 0;
        }

        InventorySystem targetInventory = inventorySystem != null ? inventorySystem : InventorySystem.Instance;
        if (targetInventory == null)
        {
            return 0;
        }

        string itemName = BuildItemName(fishId);
        int count = 0;
        List<string> items = targetInventory.itemList;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == itemName)
            {
                count++;
            }
        }

        return count;
    }

    public string BuildItemName(string fishId)
    {
        return fishItemPrefix + fishId;
    }

    private bool IsValidInventoryPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            return false;
        }

        return prefab.GetComponent<RectTransform>() != null
            && prefab.GetComponent<InventoryItem>() != null;
    }

    private bool HasEnoughFish(InventorySystem targetInventory, string itemName, int count)
    {
        int existingCount = 0;
        List<string> items = targetInventory.itemList;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == itemName)
            {
                existingCount++;
                if (existingCount >= count)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
