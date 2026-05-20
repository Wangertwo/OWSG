using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopService : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventorySystem inventorySystem;
    [SerializeField] private FishingInventoryBridge fishingInventoryBridge;
    [SerializeField] private FishDatabase fishDatabase;
    [SerializeField] private FishingEconomyService fishingEconomyService;
    [SerializeField] private FishingRumorService rumorService;
    [SerializeField] private PlayerWallet wallet;
    [SerializeField] private ShopTradeRuleConfig tradeRuleConfig;

    [Header("Trusted Fish Trade")]
    [SerializeField] private string trustedFishingNpcId = "npc_fisherman";
    [SerializeField] private string trustedFishingMerchantId = "merchant_dock_01";
    [SerializeField] private float trustedFishSellMultiplier = 1.25f;

    [Header("Behavior")]
    [SerializeField] private bool autoResolveReferences = true;
    [SerializeField] private bool autoCreateWalletIfMissing = true;
    [Min(0)]
    [SerializeField] private int autoCreatedWalletStartingCoins = 300;
    [SerializeField] private string fishInventoryItemPrefix = "Fish_";

    private readonly Dictionary<string, int> runtimeStockByKey = new Dictionary<string, int>();

    public event Action ShopDataChanged;

    private void Awake()
    {
        ResolveReferences();
    }

    public int GetPlayerCoins()
    {
        ResolveReferences();
        return wallet == null ? 0 : wallet.Coins;
    }

    public int GetFreeSlotCount()
    {
        ResolveReferences();
        return inventorySystem == null ? 0 : inventorySystem.GetFreeSlotCount();
    }

    public List<ShopRuntimeItem> BuildBuyItems(ShopCatalog catalog)
    {
        ResolveReferences();

        List<ShopRuntimeItem> items = new List<ShopRuntimeItem>();
        if (catalog == null || catalog.buyableItems == null)
        {
            return items;
        }

        EnsureRuntimeStockSeeded(catalog);

        for (int i = 0; i < catalog.buyableItems.Count; i++)
        {
            ShopCatalogItem source = catalog.buyableItems[i];
            if (source == null || string.IsNullOrWhiteSpace(source.itemId))
            {
                continue;
            }

            string inventoryItemName = source.ResolveInventoryItemName();
            int ownedCount = CountInventoryItem(inventoryItemName);
            int availableStock = source.stockMode == ShopStockMode.Infinite
                ? -1
                : ResolveRuntimeStock(catalog, source.itemId, source.startingStock);

            bool canTrade = source.stockMode == ShopStockMode.Infinite || availableStock > 0;

            ShopRuntimeItem runtimeItem = new ShopRuntimeItem
            {
                itemId = source.itemId.Trim(),
                inventoryItemName = inventoryItemName,
                displayName = source.ResolveDisplayName(),
                description = source.ResolveDescription(),
                icon = source.icon,
                domainType = source.domainType,
                unitPrice = Mathf.Max(1, source.buyPrice),
                ownedCount = ownedCount,
                availableStock = availableStock,
                canTrade = canTrade
            };

            items.Add(runtimeItem);
        }

        return items;
    }

    public List<ShopRuntimeItem> BuildSellItemsFishOnly()
    {
        return BuildSellItemsFishOnly(null);
    }

    public List<ShopRuntimeItem> BuildSellItemsFishOnly(ShopCatalog catalog)
    {
        ResolveReferences();

        Dictionary<string, int> fishCountById = new Dictionary<string, int>();
        if (inventorySystem == null || inventorySystem.itemList == null)
        {
            return new List<ShopRuntimeItem>();
        }

        List<string> sourceItems = inventorySystem.itemList;
        for (int i = 0; i < sourceItems.Count; i++)
        {
            string inventoryItemName = sourceItems[i];
            string fishId;
            if (!TryParseFishIdFromInventoryItem(inventoryItemName, out fishId))
            {
                continue;
            }

            int count;
            fishCountById.TryGetValue(fishId, out count);
            fishCountById[fishId] = count + 1;
        }

        List<ShopRuntimeItem> result = new List<ShopRuntimeItem>();
        foreach (KeyValuePair<string, int> pair in fishCountById)
        {
            int unitPrice = ResolveFishSellUnitPrice(pair.Key, catalog);
            bool canTrade = pair.Value > 0 && IsSellDomainAllowed(ShopItemDomainType.Fish) && unitPrice > 0;

            FishDefinition fish = fishDatabase == null ? null : fishDatabase.GetFishOrNull(pair.Key);
            Sprite icon = TryResolveInventoryItemIcon(BuildFishInventoryItemName(pair.Key));

            ShopRuntimeItem item = new ShopRuntimeItem
            {
                itemId = pair.Key,
                inventoryItemName = BuildFishInventoryItemName(pair.Key),
                displayName = GetFishDisplayName(pair.Key),
                description = fish == null || string.IsNullOrWhiteSpace(fish.description) ? string.Empty : fish.description.Trim(),
                icon = icon,
                domainType = ShopItemDomainType.Fish,
                unitPrice = unitPrice,
                ownedCount = pair.Value,
                availableStock = pair.Value,
                canTrade = canTrade
            };

            result.Add(item);
        }

        result.Sort((a, b) => string.CompareOrdinal(a.displayName, b.displayName));
        return result;
    }

    public int GetMaxBuyQuantity(ShopCatalog catalog, string itemId)
    {
        ResolveReferences();

        if (catalog == null || string.IsNullOrWhiteSpace(itemId))
        {
            return 0;
        }

        ShopCatalogItem item;
        if (!catalog.TryGetItem(itemId, out item) || item == null)
        {
            return 0;
        }

        int unitPrice = Mathf.Max(1, item.buyPrice);
        int affordableCount = wallet == null ? 0 : wallet.Coins / unitPrice;
        int freeSlots = inventorySystem == null ? 0 : inventorySystem.GetFreeSlotCount();
        int stockLimit = item.stockMode == ShopStockMode.Infinite
            ? int.MaxValue
            : Mathf.Max(0, ResolveRuntimeStock(catalog, item.itemId, item.startingStock));

        int maxQuantity = Mathf.Min(affordableCount, freeSlots);
        maxQuantity = Mathf.Min(maxQuantity, stockLimit);
        return Mathf.Max(0, maxQuantity);
    }

    public int GetMaxSellFishQuantity(string fishId)
    {
        if (string.IsNullOrWhiteSpace(fishId) || !IsSellDomainAllowed(ShopItemDomainType.Fish))
        {
            return 0;
        }

        return CountFish(fishId.Trim());
    }

    public ShopTransactionResult TryBuy(ShopCatalog catalog, string itemId, int quantity)
    {
        ResolveReferences();

        if (catalog == null)
        {
            return ShopTransactionResult.Fail(ShopErrorCode.MissingConfig, "商店配置缺失。请绑定 ShopCatalog。");
        }

        if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
        {
            return ShopTransactionResult.Fail(ShopErrorCode.InvalidRequest, "购买数量无效。");
        }

        if (inventorySystem == null || wallet == null)
        {
            return ShopTransactionResult.Fail(ShopErrorCode.MissingConfig, "背包或钱包未初始化。");
        }

        ShopCatalogItem item;
        if (!catalog.TryGetItem(itemId, out item) || item == null)
        {
            return ShopTransactionResult.Fail(ShopErrorCode.ItemNotFound, "商品不存在。");
        }

        EnsureRuntimeStockSeeded(catalog);

        if (item.stockMode == ShopStockMode.Finite)
        {
            int stock = ResolveRuntimeStock(catalog, item.itemId, item.startingStock);
            if (stock < quantity)
            {
                return ShopTransactionResult.Fail(ShopErrorCode.OutOfStock, "商人库存不足。");
            }
        }

        int unitPrice = Mathf.Max(1, item.buyPrice);
        int totalPrice = unitPrice * quantity;
        if (wallet.Coins < totalPrice)
        {
            return ShopTransactionResult.Fail(ShopErrorCode.InsufficientFunds, "金币不足。");
        }

        if (!inventorySystem.HasFreeSlots(quantity))
        {
            return ShopTransactionResult.Fail(ShopErrorCode.InventoryFull, "背包空间不足。");
        }

        string inventoryItemName = item.ResolveInventoryItemName();
        ShopTransactionResult resourceCheck = ValidateInventoryItemResource(inventoryItemName);
        if (!resourceCheck.success)
        {
            return resourceCheck;
        }

        if (!wallet.TrySpendCoins(totalPrice))
        {
            return ShopTransactionResult.Fail(ShopErrorCode.InsufficientFunds, "金币不足。");
        }

        try
        {
            inventorySystem.AddToInventory(inventoryItemName, quantity);
        }
        catch (Exception ex)
        {
            wallet.AddCoins(totalPrice);
            Debug.LogError("ShopService: buy transaction failed for item " + inventoryItemName + ". " + ex.Message, this);
            return ShopTransactionResult.Fail(ShopErrorCode.TransactionException, "购买失败，请检查物品配置。");
        }

        if (item.stockMode == ShopStockMode.Finite)
        {
            string stockKey = BuildStockKey(catalog.ResolveMerchantId(), item.itemId);
            int stock = ResolveRuntimeStock(catalog, item.itemId, item.startingStock);
            runtimeStockByKey[stockKey] = Mathf.Max(0, stock - quantity);
        }

        ShopDataChanged?.Invoke();

        string message = "购买成功: " + item.ResolveDisplayName() + " x" + quantity + "。";
        return ShopTransactionResult.Ok(message, -totalPrice, quantity);
    }

    public ShopTransactionResult TrySellFish(string fishId, int quantity)
    {
        return TrySellFish(null, fishId, quantity);
    }

    public ShopTransactionResult TrySellFish(ShopCatalog catalog, string fishId, int quantity)
    {
        ResolveReferences();

        if (string.IsNullOrWhiteSpace(fishId) || quantity <= 0)
        {
            return ShopTransactionResult.Fail(ShopErrorCode.InvalidRequest, "出售数量无效。");
        }

        if (!IsSellDomainAllowed(ShopItemDomainType.Fish))
        {
            return ShopTransactionResult.Fail(ShopErrorCode.ItemNotTradable, "当前商店不收鱼类。");
        }

        if (wallet == null)
        {
            return ShopTransactionResult.Fail(ShopErrorCode.MissingConfig, "钱包未初始化。");
        }

        string normalizedFishId = fishId.Trim();
        int ownedCount = CountFish(normalizedFishId);
        if (ownedCount < quantity)
        {
            return ShopTransactionResult.Fail(ShopErrorCode.ItemNotOwned, "背包里鱼数量不足。");
        }

        int unitPrice = ResolveFishSellUnitPrice(normalizedFishId, catalog);
        if (unitPrice <= 0)
        {
            return ShopTransactionResult.Fail(ShopErrorCode.PriceUnavailable, "该鱼当前没有有效收购价。");
        }

        bool removed;
        if (fishingInventoryBridge != null)
        {
            removed = fishingInventoryBridge.TryRemoveFish(normalizedFishId, quantity);
        }
        else
        {
            removed = RemoveInventoryItemByName(BuildFishInventoryItemName(normalizedFishId), quantity);
        }

        if (!removed)
        {
            return ShopTransactionResult.Fail(ShopErrorCode.ItemNotOwned, "出售失败：背包更新失败。");
        }

        int totalRevenue = unitPrice * quantity;
        wallet.AddCoins(totalRevenue);

        ShopDataChanged?.Invoke();

        string fishName = GetFishDisplayName(normalizedFishId);
        string trustSuffix = IsTrustedFishTradeActive(catalog) ? " 信任加价已生效。" : string.Empty;
        string message = "出售成功: " + fishName + " x" + quantity + "，获得 " + totalRevenue + " 金币。" + trustSuffix;
        return ShopTransactionResult.Ok(message, totalRevenue, -quantity);
    }

    public string GetFishDisplayName(string fishId)
    {
        if (string.IsNullOrWhiteSpace(fishId))
        {
            return "未知鱼类";
        }

        if (fishDatabase == null)
        {
            return fishId.Trim();
        }

        FishDefinition fish = fishDatabase.GetFishOrNull(fishId.Trim());
        if (fish == null || string.IsNullOrWhiteSpace(fish.displayName))
        {
            return fishId.Trim();
        }

        return fish.displayName.Trim();
    }

    private void ResolveReferences()
    {
        if (!autoResolveReferences)
        {
            return;
        }

        if (inventorySystem == null)
        {
            inventorySystem = InventorySystem.Instance;
            if (inventorySystem == null)
            {
                inventorySystem = FindObjectOfType<InventorySystem>(true);
            }
        }

        if (wallet == null)
        {
            wallet = PlayerWallet.Instance;
            if (wallet == null)
            {
                wallet = FindObjectOfType<PlayerWallet>(true);
            }

            if (wallet == null && autoCreateWalletIfMissing)
            {
                GameObject walletObject = new GameObject("PlayerWallet_Auto");
                wallet = walletObject.AddComponent<PlayerWallet>();

                if (autoCreatedWalletStartingCoins > 0)
                {
                    wallet.AddCoins(autoCreatedWalletStartingCoins);
                }

                Debug.LogWarning("ShopService: PlayerWallet not found in scene. Auto-created PlayerWallet_Auto.", this);
            }
        }

        if (fishingInventoryBridge == null)
        {
            fishingInventoryBridge = FindObjectOfType<FishingInventoryBridge>(true);
        }

        if (fishingEconomyService == null)
        {
            fishingEconomyService = FindObjectOfType<FishingEconomyService>(true);
        }

        if (rumorService == null)
        {
            rumorService = FishingRumorService.Instance;
            if (rumorService == null)
            {
                rumorService = FindObjectOfType<FishingRumorService>(true);
            }
        }

        if (fishDatabase == null)
        {
            FishingSystem fishingSystem = FishingSystem.Instance;
            if (fishingSystem != null)
            {
                // FishDatabase is currently not exposed by FishingSystem.
            }
        }
    }

    private void EnsureRuntimeStockSeeded(ShopCatalog catalog)
    {
        if (catalog == null || catalog.buyableItems == null)
        {
            return;
        }

        string merchantId = catalog.ResolveMerchantId();
        for (int i = 0; i < catalog.buyableItems.Count; i++)
        {
            ShopCatalogItem item = catalog.buyableItems[i];
            if (item == null || item.stockMode != ShopStockMode.Finite || string.IsNullOrWhiteSpace(item.itemId))
            {
                continue;
            }

            string key = BuildStockKey(merchantId, item.itemId.Trim());
            if (!runtimeStockByKey.ContainsKey(key))
            {
                runtimeStockByKey[key] = Mathf.Max(0, item.startingStock);
            }
        }
    }

    private int ResolveRuntimeStock(ShopCatalog catalog, string itemId, int fallbackStock)
    {
        if (catalog == null || string.IsNullOrWhiteSpace(itemId))
        {
            return Mathf.Max(0, fallbackStock);
        }

        string key = BuildStockKey(catalog.ResolveMerchantId(), itemId.Trim());
        int stock;
        if (runtimeStockByKey.TryGetValue(key, out stock))
        {
            return Mathf.Max(0, stock);
        }

        runtimeStockByKey[key] = Mathf.Max(0, fallbackStock);
        return Mathf.Max(0, fallbackStock);
    }

    private string BuildStockKey(string merchantId, string itemId)
    {
        string merchant = string.IsNullOrWhiteSpace(merchantId) ? "merchant_default" : merchantId.Trim();
        string item = string.IsNullOrWhiteSpace(itemId) ? "item_default" : itemId.Trim();
        return merchant + "::" + item;
    }

    private ShopTransactionResult ValidateInventoryItemResource(string inventoryItemName)
    {
        if (string.IsNullOrWhiteSpace(inventoryItemName))
        {
            return ShopTransactionResult.Fail(ShopErrorCode.ItemResourceMissing, "商品背包资源名为空。");
        }

        GameObject prefab = Resources.Load<GameObject>(inventoryItemName);
        if (prefab == null)
        {
            return ShopTransactionResult.Fail(ShopErrorCode.ItemResourceMissing, "未找到商品资源: " + inventoryItemName);
        }

        if (prefab.GetComponent<RectTransform>() == null || prefab.GetComponent<InventoryItem>() == null)
        {
            return ShopTransactionResult.Fail(ShopErrorCode.ItemResourceInvalid, "商品资源无效: " + inventoryItemName + " 必须包含 RectTransform + InventoryItem。");
        }

        return ShopTransactionResult.Ok(string.Empty, 0, 0);
    }

    private Sprite TryResolveInventoryItemIcon(string inventoryItemName)
    {
        if (string.IsNullOrWhiteSpace(inventoryItemName))
        {
            return null;
        }

        GameObject prefab = Resources.Load<GameObject>(inventoryItemName);
        if (prefab == null)
        {
            return null;
        }

        UnityEngine.UI.Image image = prefab.GetComponent<UnityEngine.UI.Image>();
        return image == null ? null : image.sprite;
    }

    private int CountInventoryItem(string inventoryItemName)
    {
        if (inventorySystem == null || inventorySystem.itemList == null || string.IsNullOrWhiteSpace(inventoryItemName))
        {
            return 0;
        }

        int count = 0;
        List<string> source = inventorySystem.itemList;
        string key = inventoryItemName.Trim();
        for (int i = 0; i < source.Count; i++)
        {
            if (source[i] == key)
            {
                count++;
            }
        }

        return count;
    }

    private int CountFish(string fishId)
    {
        if (string.IsNullOrWhiteSpace(fishId))
        {
            return 0;
        }

        if (fishingInventoryBridge != null)
        {
            return fishingInventoryBridge.CountFish(fishId.Trim());
        }

        return CountInventoryItem(BuildFishInventoryItemName(fishId.Trim()));
    }

    private bool RemoveInventoryItemByName(string inventoryItemName, int quantity)
    {
        if (inventorySystem == null || string.IsNullOrWhiteSpace(inventoryItemName) || quantity <= 0)
        {
            return false;
        }

        if (CountInventoryItem(inventoryItemName) < quantity)
        {
            return false;
        }

        inventorySystem.RemoveItem(inventoryItemName, quantity);
        inventorySystem.ReCalculateList();
        return true;
    }

    private bool TryParseFishIdFromInventoryItem(string inventoryItemName, out string fishId)
    {
        fishId = string.Empty;
        if (string.IsNullOrWhiteSpace(inventoryItemName) || string.IsNullOrWhiteSpace(fishInventoryItemPrefix))
        {
            return false;
        }

        string normalizedItemName = inventoryItemName.Trim();
        string prefix = fishInventoryItemPrefix.Trim();
        if (!normalizedItemName.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        fishId = normalizedItemName.Substring(prefix.Length);
        return !string.IsNullOrWhiteSpace(fishId);
    }

    private string BuildFishInventoryItemName(string fishId)
    {
        return fishInventoryItemPrefix + fishId;
    }

    private bool IsSellDomainAllowed(ShopItemDomainType domainType)
    {
        if (tradeRuleConfig == null)
        {
            return domainType == ShopItemDomainType.Fish;
        }

        return tradeRuleConfig.IsSellAllowed(domainType);
    }

    private int ResolveFishSellUnitPrice(string fishId, ShopCatalog catalog)
    {
        if (string.IsNullOrWhiteSpace(fishId))
        {
            return 0;
        }

        float multiplier = tradeRuleConfig == null
            ? 1f
            : tradeRuleConfig.ResolveSellMultiplier(ShopItemDomainType.Fish);

        if (IsTrustedFishTradeActive(catalog))
        {
            multiplier *= Mathf.Max(1f, trustedFishSellMultiplier);
        }

        int rawPrice = 0;
        if (fishingEconomyService != null)
        {
            rawPrice = fishingEconomyService.GetCurrentPrice(fishId.Trim());
        }

        if (rawPrice <= 0)
        {
            rawPrice = ResolveFishBasePrice(fishId.Trim());
        }

        if (rawPrice <= 0)
        {
            return 0;
        }

        int minPrice = tradeRuleConfig == null ? 1 : Mathf.Max(1, tradeRuleConfig.minSellPrice);
        int unitPrice = Mathf.RoundToInt(rawPrice * multiplier);
        return Mathf.Max(minPrice, unitPrice);
    }

    private bool IsTrustedFishTradeActive(ShopCatalog catalog)
    {
        if (catalog == null || rumorService == null)
        {
            return false;
        }

        if (!string.Equals(catalog.ResolveMerchantId(), trustedFishingMerchantId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return rumorService.HasTrustedNpc(trustedFishingNpcId);
    }

    private int ResolveFishBasePrice(string fishId)
    {
        if (fishDatabase == null || string.IsNullOrWhiteSpace(fishId))
        {
            return 0;
        }

        FishDefinition fish = fishDatabase.GetFishOrNull(fishId.Trim());
        if (fish == null)
        {
            return 0;
        }

        return Mathf.Max(0, fish.basePrice);
    }
}
