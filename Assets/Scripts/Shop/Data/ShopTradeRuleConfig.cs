using UnityEngine;

[CreateAssetMenu(fileName = "ShopTradeRuleConfig", menuName = "Shop/Trade Rule Config")]
public class ShopTradeRuleConfig : ScriptableObject
{
    [Header("Sell Switch")]
    public bool enableSelling = true;

    [Header("Domain Allow List")]
    public bool allowFishSell = true;
    public bool allowGeneralItemSell = false;

    [Header("Sell Price")]
    [Range(0.1f, 2f)]
    public float fishSellMultiplier = 1f;
    [Range(0.1f, 2f)]
    public float generalItemSellMultiplier = 0.6f;
    [Min(1)]
    public int minSellPrice = 1;

    public bool IsSellAllowed(ShopItemDomainType domainType)
    {
        if (!enableSelling)
        {
            return false;
        }

        switch (domainType)
        {
            case ShopItemDomainType.Fish:
                return allowFishSell;
            case ShopItemDomainType.GeneralItem:
                return allowGeneralItemSell;
            default:
                return false;
        }
    }

    public float ResolveSellMultiplier(ShopItemDomainType domainType)
    {
        switch (domainType)
        {
            case ShopItemDomainType.Fish:
                return Mathf.Max(0.1f, fishSellMultiplier);
            case ShopItemDomainType.GeneralItem:
                return Mathf.Max(0.1f, generalItemSellMultiplier);
            default:
                return 1f;
        }
    }
}
