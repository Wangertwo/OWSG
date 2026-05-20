using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanelController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Header")]
    [SerializeField] private TextMeshProUGUI merchantNameText;
    [SerializeField] private TextMeshProUGUI coinText;

    [Header("Tabs")]
    [SerializeField] private Button buyTabButton;
    [SerializeField] private Button sellTabButton;

    [Header("Item List")]
    [SerializeField] private RectTransform itemListRoot;
    [SerializeField] private ShopItemRowView itemRowPrefab;
    [SerializeField] private TextMeshProUGUI emptyListText;

    [Header("Detail")]
    [SerializeField] private Image itemIconImage;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI unitPriceText;
    [SerializeField] private TextMeshProUGUI ownedCountText;
    [SerializeField] private TextMeshProUGUI stockText;

    [Header("Quantity")]
    [SerializeField] private Button quantityMinusButton;
    [SerializeField] private Button quantityPlusButton;
    [SerializeField] private Button quantityMaxButton;
    [SerializeField] private TextMeshProUGUI quantityText;
    [Min(1)]
    [SerializeField] private int maxQuantityPerTrade = 99;

    [Header("Action")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private TextMeshProUGUI confirmButtonText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button closeButton;
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    private readonly List<ShopItemRowView> runtimeRows = new List<ShopItemRowView>();
    private readonly List<ShopRuntimeItem> runtimeItems = new List<ShopRuntimeItem>();

    private ShopService shopService;
    private ShopCatalog activeCatalog;
    private ShopRuntimeItem selectedItem;
    private ShopTradeType currentTradeType = ShopTradeType.BuyFromMerchant;
    private int selectedQuantity = 1;

    public event Action CloseRequested;

    public bool IsOpen
    {
        get
        {
            GameObject root = GetRoot();
            return root != null && root.activeSelf;
        }
    }

    private void Awake()
    {
        BindStaticEvents();
        ResetRuntimeState();
    }

    private void OnDestroy()
    {
        UnbindStaticEvents();
    }

    private void Update()
    {
        if (!IsOpen)
        {
            return;
        }

        if (Input.GetKeyDown(closeKey))
        {
            HandleCloseClicked();
        }
    }

    public void OpenShop(ShopService service, ShopCatalog catalog)
    {
        shopService = service;
        activeCatalog = catalog;
        currentTradeType = ShopTradeType.BuyFromMerchant;
        selectedQuantity = 1;
        selectedItem = null;

        GameObject root = GetRoot();
        if (root != null)
        {
            root.SetActive(true);
        }

        if (statusText != null)
        {
            statusText.text = string.Empty;
        }

        RefreshAll();
    }

    public void HidePanel()
    {
        GameObject root = GetRoot();
        if (root != null)
        {
            root.SetActive(false);
        }

        ResetRuntimeState();
    }

    private void ResetRuntimeState()
    {
        ClearRuntimeRows();
        runtimeItems.Clear();
        selectedItem = null;
        selectedQuantity = 1;
    }

    private void BindStaticEvents()
    {
        if (buyTabButton != null)
        {
            buyTabButton.onClick.RemoveListener(HandleBuyTabClicked);
            buyTabButton.onClick.AddListener(HandleBuyTabClicked);
        }

        if (sellTabButton != null)
        {
            sellTabButton.onClick.RemoveListener(HandleSellTabClicked);
            sellTabButton.onClick.AddListener(HandleSellTabClicked);
        }

        if (quantityMinusButton != null)
        {
            quantityMinusButton.onClick.RemoveListener(HandleQuantityMinusClicked);
            quantityMinusButton.onClick.AddListener(HandleQuantityMinusClicked);
        }

        if (quantityPlusButton != null)
        {
            quantityPlusButton.onClick.RemoveListener(HandleQuantityPlusClicked);
            quantityPlusButton.onClick.AddListener(HandleQuantityPlusClicked);
        }

        if (quantityMaxButton != null)
        {
            quantityMaxButton.onClick.RemoveListener(HandleQuantityMaxClicked);
            quantityMaxButton.onClick.AddListener(HandleQuantityMaxClicked);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            confirmButton.onClick.AddListener(HandleConfirmClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseClicked);
            closeButton.onClick.AddListener(HandleCloseClicked);
        }
    }

    private void UnbindStaticEvents()
    {
        if (buyTabButton != null)
        {
            buyTabButton.onClick.RemoveListener(HandleBuyTabClicked);
        }

        if (sellTabButton != null)
        {
            sellTabButton.onClick.RemoveListener(HandleSellTabClicked);
        }

        if (quantityMinusButton != null)
        {
            quantityMinusButton.onClick.RemoveListener(HandleQuantityMinusClicked);
        }

        if (quantityPlusButton != null)
        {
            quantityPlusButton.onClick.RemoveListener(HandleQuantityPlusClicked);
        }

        if (quantityMaxButton != null)
        {
            quantityMaxButton.onClick.RemoveListener(HandleQuantityMaxClicked);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(HandleConfirmClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseClicked);
        }
    }

    private void RefreshAll()
    {
        RefreshHeader();
        RefreshItemList();
        RefreshDetailPanel();
        RefreshActionLabel();
    }

    private void RefreshHeader()
    {
        if (merchantNameText != null)
        {
            merchantNameText.text = activeCatalog == null ? "商人" : activeCatalog.ResolveMerchantDisplayName();
        }

        if (coinText != null)
        {
            int coins = shopService == null ? 0 : shopService.GetPlayerCoins();
            coinText.text = "金币: " + coins;
        }

        if (buyTabButton != null)
        {
            buyTabButton.interactable = currentTradeType != ShopTradeType.BuyFromMerchant;
        }

        if (sellTabButton != null)
        {
            sellTabButton.interactable = currentTradeType != ShopTradeType.SellToMerchant;
        }
    }

    private void RefreshItemList()
    {
        runtimeItems.Clear();
        ClearRuntimeRows();

        if (shopService == null)
        {
            SetEmptyListState("ShopService 未绑定。");
            return;
        }

        if (currentTradeType == ShopTradeType.BuyFromMerchant)
        {
            if (activeCatalog != null)
            {
                runtimeItems.AddRange(shopService.BuildBuyItems(activeCatalog));
            }
        }
        else
        {
            runtimeItems.AddRange(shopService.BuildSellItemsFishOnly(activeCatalog));
        }

        if (runtimeItems.Count == 0)
        {
            SetEmptyListState(currentTradeType == ShopTradeType.BuyFromMerchant
                ? "该商人当前没有可购买商品。"
                : "背包中暂无可出售鱼类。");
            selectedItem = null;
            return;
        }

        if (emptyListText != null)
        {
            emptyListText.gameObject.SetActive(false);
        }

        string selectedId = selectedItem == null ? string.Empty : selectedItem.itemId;
        selectedItem = null;
        for (int i = 0; i < runtimeItems.Count; i++)
        {
            ShopRuntimeItem item = runtimeItems[i];
            if (item == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(selectedId) &&
                string.Equals(item.itemId, selectedId, StringComparison.OrdinalIgnoreCase))
            {
                selectedItem = item;
            }
        }

        if (selectedItem == null)
        {
            selectedItem = runtimeItems[0];
        }

        if (itemListRoot == null || itemRowPrefab == null)
        {
            return;
        }

        for (int i = 0; i < runtimeItems.Count; i++)
        {
            ShopRuntimeItem item = runtimeItems[i];
            ShopItemRowView row = Instantiate(itemRowPrefab, itemListRoot, false);
            NormalizeRowRectTransform(row);
            bool isSelected = selectedItem != null && item != null &&
                              string.Equals(selectedItem.itemId, item.itemId, StringComparison.OrdinalIgnoreCase);
            row.Bind(item, currentTradeType, isSelected, HandleItemSelected);
            runtimeRows.Add(row);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(itemListRoot);
    }

    private void NormalizeRowRectTransform(ShopItemRowView row)
    {
        if (row == null)
        {
            return;
        }

        RectTransform rowRect = row.GetComponent<RectTransform>();
        if (rowRect == null)
        {
            return;
        }

        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.localScale = Vector3.one;
        rowRect.localRotation = Quaternion.identity;
        rowRect.anchoredPosition = Vector2.zero;

        if (Mathf.Abs(rowRect.sizeDelta.y) < 0.01f)
        {
            rowRect.sizeDelta = new Vector2(0f, 82f);
        }
    }

    private void RefreshDetailPanel()
    {
        int maxQuantity = GetMaxQuantityForSelected();
        if (maxQuantity <= 0)
        {
            selectedQuantity = 0;
        }
        else
        {
            selectedQuantity = Mathf.Clamp(selectedQuantity, 1, maxQuantity);
        }

        if (selectedItem == null)
        {
            if (itemNameText != null)
            {
                itemNameText.text = "未选择商品";
            }

            if (itemDescriptionText != null)
            {
                itemDescriptionText.text = string.Empty;
            }

            if (unitPriceText != null)
            {
                unitPriceText.text = "单价: -";
            }

            if (ownedCountText != null)
            {
                ownedCountText.text = "持有: -";
            }

            if (stockText != null)
            {
                stockText.text = "库存: -";
            }

            if (quantityText != null)
            {
                quantityText.text = "0";
            }

            if (itemIconImage != null)
            {
                itemIconImage.enabled = false;
            }

            SetQuantityButtonsInteractable(false, false);
            SetConfirmInteractable(false);
            return;
        }

        if (itemNameText != null)
        {
            itemNameText.text = selectedItem.displayName;
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = string.IsNullOrWhiteSpace(selectedItem.description)
                ? "暂无描述"
                : selectedItem.description;
        }

        if (unitPriceText != null)
        {
            unitPriceText.text = "单价: " + selectedItem.unitPrice;
        }

        if (ownedCountText != null)
        {
            ownedCountText.text = "持有: " + selectedItem.ownedCount;
        }

        if (stockText != null)
        {
            stockText.text = selectedItem.availableStock < 0
                ? "库存: 无限"
                : "库存: " + selectedItem.availableStock;
        }

        if (quantityText != null)
        {
            quantityText.text = selectedQuantity.ToString();
        }

        if (itemIconImage != null)
        {
            bool hasIcon = selectedItem.icon != null;
            itemIconImage.enabled = hasIcon;
            if (hasIcon)
            {
                itemIconImage.sprite = selectedItem.icon;
            }
        }

        bool hasTradeCapacity = maxQuantity > 0;
        SetQuantityButtonsInteractable(hasTradeCapacity, selectedQuantity < maxQuantity);
        SetConfirmInteractable(hasTradeCapacity && selectedItem.canTrade && selectedQuantity > 0);
    }

    private void RefreshActionLabel()
    {
        if (confirmButtonText == null)
        {
            return;
        }

        confirmButtonText.text = currentTradeType == ShopTradeType.BuyFromMerchant ? "购买" : "出售";
    }

    private void SetQuantityButtonsInteractable(bool canDecreaseOrMax, bool canIncrease)
    {
        if (quantityMinusButton != null)
        {
            quantityMinusButton.interactable = canDecreaseOrMax && selectedQuantity > 1;
        }

        if (quantityPlusButton != null)
        {
            quantityPlusButton.interactable = canIncrease;
        }

        if (quantityMaxButton != null)
        {
            quantityMaxButton.interactable = canDecreaseOrMax;
        }
    }

    private void SetConfirmInteractable(bool isInteractable)
    {
        if (confirmButton != null)
        {
            confirmButton.interactable = isInteractable;
        }
    }

    private void HandleBuyTabClicked()
    {
        currentTradeType = ShopTradeType.BuyFromMerchant;
        selectedQuantity = 1;
        RefreshAll();
    }

    private void HandleSellTabClicked()
    {
        currentTradeType = ShopTradeType.SellToMerchant;
        selectedQuantity = 1;
        RefreshAll();
    }

    private void HandleItemSelected(ShopRuntimeItem item)
    {
        selectedItem = item;
        selectedQuantity = 1;
        RefreshHeader();
        RefreshItemRowSelection();
        RefreshDetailPanel();
    }

    private void RefreshItemRowSelection()
    {
        for (int i = 0; i < runtimeRows.Count; i++)
        {
            ShopItemRowView row = runtimeRows[i];
            if (row == null)
            {
                continue;
            }

            ShopRuntimeItem item = i < runtimeItems.Count ? runtimeItems[i] : null;
            bool isSelected = selectedItem != null && item != null &&
                              string.Equals(selectedItem.itemId, item.itemId, StringComparison.OrdinalIgnoreCase);
            row.SetSelectedVisual(isSelected);
        }
    }

    private void HandleQuantityMinusClicked()
    {
        if (selectedQuantity > 1)
        {
            selectedQuantity--;
            RefreshDetailPanel();
        }
    }

    private void HandleQuantityPlusClicked()
    {
        int maxQuantity = GetMaxQuantityForSelected();
        if (maxQuantity <= 0)
        {
            return;
        }

        if (selectedQuantity < maxQuantity)
        {
            selectedQuantity++;
            RefreshDetailPanel();
        }
    }

    private void HandleQuantityMaxClicked()
    {
        int maxQuantity = GetMaxQuantityForSelected();
        selectedQuantity = maxQuantity;
        RefreshDetailPanel();
    }

    private void HandleConfirmClicked()
    {
        if (shopService == null || selectedItem == null)
        {
            ShowStatus("没有可执行的交易。", false);
            return;
        }

        if (selectedQuantity <= 0)
        {
            ShowStatus("请先选择数量。", false);
            return;
        }

        ShopTransactionResult result;
        if (currentTradeType == ShopTradeType.BuyFromMerchant)
        {
            result = shopService.TryBuy(activeCatalog, selectedItem.itemId, selectedQuantity);
        }
        else
        {
            result = shopService.TrySellFish(activeCatalog, selectedItem.itemId, selectedQuantity);
        }

        if (result == null)
        {
            ShowStatus("交易失败：未知错误。", false);
            return;
        }

        ShowStatus(result.message, result.success);
        RefreshAll();
    }

    private void HandleCloseClicked()
    {
        CloseRequested?.Invoke();

        if (IsOpen)
        {
            HidePanel();
        }
    }

    private int GetMaxQuantityForSelected()
    {
        if (selectedItem == null || shopService == null)
        {
            return 0;
        }

        int calculatedMax;
        if (currentTradeType == ShopTradeType.BuyFromMerchant)
        {
            calculatedMax = shopService.GetMaxBuyQuantity(activeCatalog, selectedItem.itemId);
        }
        else
        {
            calculatedMax = shopService.GetMaxSellFishQuantity(selectedItem.itemId);
        }

        if (maxQuantityPerTrade > 0)
        {
            calculatedMax = Mathf.Min(calculatedMax, maxQuantityPerTrade);
        }

        return Mathf.Max(0, calculatedMax);
    }

    private void ShowStatus(string message, bool success)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
        statusText.color = success ? new Color(0.52f, 0.94f, 0.57f) : new Color(1f, 0.56f, 0.56f);
    }

    private void SetEmptyListState(string message)
    {
        if (emptyListText != null)
        {
            emptyListText.gameObject.SetActive(true);
            emptyListText.text = message;
        }
    }

    private void ClearRuntimeRows()
    {
        for (int i = 0; i < runtimeRows.Count; i++)
        {
            if (runtimeRows[i] != null)
            {
                Destroy(runtimeRows[i].gameObject);
            }
        }

        runtimeRows.Clear();
    }

    private GameObject GetRoot()
    {
        return panelRoot != null ? panelRoot : gameObject;
    }
}
