using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShopCatalogItem
{
    [Header("Identity")]
    public string itemId;
    public string inventoryItemName;
    public string displayName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;
    public ShopItemDomainType domainType = ShopItemDomainType.GeneralItem;

    [Header("Buy Settings")]
    [Min(1)]
    public int buyPrice = 10;
    public ShopStockMode stockMode = ShopStockMode.Infinite;
    [Min(0)]
    public int startingStock = 0;

    public string ResolveInventoryItemName()
    {
        if (!string.IsNullOrWhiteSpace(inventoryItemName))
        {
            return inventoryItemName.Trim();
        }

        return string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim();
    }

    public string ResolveDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(itemId))
        {
            return itemId.Trim();
        }

        return "Unknown Item";
    }

    public string ResolveDescription()
    {
        return string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim();
    }
}

[CreateAssetMenu(fileName = "ShopCatalog", menuName = "Shop/Shop Catalog")]
public class ShopCatalog : ScriptableObject
{
    [Header("Merchant")]
    public string merchantId = "merchant_default";
    public string merchantDisplayName = "Merchant";

    [Header("Buyable Items")]
    public List<ShopCatalogItem> buyableItems = new List<ShopCatalogItem>();

    public bool TryGetItem(string itemId, out ShopCatalogItem item)
    {
        item = null;
        if (string.IsNullOrWhiteSpace(itemId) || buyableItems == null)
        {
            return false;
        }

        string key = itemId.Trim();
        for (int i = 0; i < buyableItems.Count; i++)
        {
            ShopCatalogItem candidate = buyableItems[i];
            if (candidate == null || string.IsNullOrWhiteSpace(candidate.itemId))
            {
                continue;
            }

            if (string.Equals(candidate.itemId.Trim(), key, StringComparison.OrdinalIgnoreCase))
            {
                item = candidate;
                return true;
            }
        }

        return false;
    }

    public string ResolveMerchantId()
    {
        if (!string.IsNullOrWhiteSpace(merchantId))
        {
            return merchantId.Trim();
        }

        return string.IsNullOrWhiteSpace(name) ? "merchant_default" : name.Trim();
    }

    public string ResolveMerchantDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(merchantDisplayName))
        {
            return merchantDisplayName.Trim();
        }

        return ResolveMerchantId();
    }
}
