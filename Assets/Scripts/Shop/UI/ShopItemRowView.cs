using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemRowView : MonoBehaviour
{
    [SerializeField] private Button selectButton;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI detailText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Image selectionHighlight;
    [SerializeField] private Color selectedColor = new Color(0.30f, 0.55f, 0.35f, 0.45f);
    [SerializeField] private Color normalColor = new Color(0.08f, 0.08f, 0.08f, 0.30f);

    private ShopRuntimeItem boundItem;
    private Action<ShopRuntimeItem> onSelected;

    private void Awake()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(HandleClick);
            selectButton.onClick.AddListener(HandleClick);
        }
    }

    public void Bind(ShopRuntimeItem item, ShopTradeType tradeType, bool selected, Action<ShopRuntimeItem> onSelectedCallback)
    {
        boundItem = item;
        onSelected = onSelectedCallback;

        if (nameText != null)
        {
            nameText.text = item == null ? "-" : item.displayName;
        }

        if (priceText != null)
        {
            priceText.text = item == null ? "-" : item.unitPrice + " 金币";
        }

        if (detailText != null)
        {
            detailText.text = BuildDetailText(item, tradeType);
        }

        if (iconImage != null)
        {
            bool hasIcon = item != null && item.icon != null;
            iconImage.enabled = hasIcon;
            if (hasIcon)
            {
                iconImage.sprite = item.icon;
            }
        }

        bool interactable = item != null && item.canTrade;
        if (selectButton != null)
        {
            selectButton.interactable = interactable;
        }

        SetSelectedVisual(selected);
    }

    public void SetSelectedVisual(bool selected)
    {
        if (selectionHighlight == null)
        {
            return;
        }

        selectionHighlight.color = selected ? selectedColor : normalColor;
    }

    private string BuildDetailText(ShopRuntimeItem item, ShopTradeType tradeType)
    {
        if (item == null)
        {
            return "-";
        }

        if (tradeType == ShopTradeType.BuyFromMerchant)
        {
            if (item.availableStock >= 0)
            {
                return "库存: " + item.availableStock + " / 拥有: " + item.ownedCount;
            }

            return "库存: 无限 / 拥有: " + item.ownedCount;
        }

        return "可售数量: " + item.ownedCount;
    }

    private void HandleClick()
    {
        if (boundItem == null || onSelected == null)
        {
            return;
        }

        onSelected(boundItem);
    }
}
