using System;
using UnityEngine;

public enum ShopTradeType
{
    BuyFromMerchant = 0,
    SellToMerchant = 1
}

public enum ShopItemDomainType
{
    Fish = 0,
    GeneralItem = 1
}

public enum ShopStockMode
{
    Infinite = 0,
    Finite = 1
}

public enum ShopErrorCode
{
    None = 0,
    InvalidRequest = 1,
    MissingConfig = 2,
    ItemNotFound = 3,
    InsufficientFunds = 4,
    InventoryFull = 5,
    OutOfStock = 6,
    ItemNotOwned = 7,
    ItemNotTradable = 8,
    PriceUnavailable = 9,
    ItemResourceMissing = 10,
    ItemResourceInvalid = 11,
    TransactionException = 12
}

[Serializable]
public class ShopRuntimeItem
{
    public string itemId;
    public string inventoryItemName;
    public string displayName;
    public string description;
    public Sprite icon;
    public ShopItemDomainType domainType;
    public int unitPrice;
    public int ownedCount;
    public int availableStock;
    public bool canTrade;

    public bool IsFiniteStock()
    {
        return availableStock >= 0;
    }
}

[Serializable]
public class ShopTransactionResult
{
    public bool success;
    public ShopErrorCode errorCode;
    public string message;
    public int moneyDelta;
    public int itemDelta;

    public static ShopTransactionResult Ok(string messageValue, int moneyDeltaValue, int itemDeltaValue)
    {
        return new ShopTransactionResult
        {
            success = true,
            errorCode = ShopErrorCode.None,
            message = messageValue,
            moneyDelta = moneyDeltaValue,
            itemDelta = itemDeltaValue
        };
    }

    public static ShopTransactionResult Fail(ShopErrorCode errorCodeValue, string messageValue)
    {
        return new ShopTransactionResult
        {
            success = false,
            errorCode = errorCodeValue,
            message = messageValue,
            moneyDelta = 0,
            itemDelta = 0
        };
    }
}
