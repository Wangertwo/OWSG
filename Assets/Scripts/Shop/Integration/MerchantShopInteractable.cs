using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MerchantShopInteractable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShopService shopService;
    [SerializeField] private ShopPanelController shopPanel;
    [SerializeField] private ShopCatalog shopCatalog;
    [SerializeField] private Transform playerTransform;

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Range")]
    [SerializeField] private bool useTriggerRange = true;
    [SerializeField] private bool useDistanceRange = true;
    [Min(0.5f)]
    [SerializeField] private float interactionDistance = 2.5f;
    [SerializeField] private bool autoCloseWhenOutOfRange = true;

    [Header("Selection")]
    [SerializeField] private bool requireSelectionTarget = false;
    [SerializeField] private GameObject selectionObjectOverride;

    private static MerchantShopInteractable activeInteractable;
    private static readonly List<MerchantShopInteractable> registeredInteractables = new List<MerchantShopInteractable>();

    private bool playerInTriggerRange;
    private bool ignoreRangeUntilClosed;

    private void Reset()
    {
        shopService = FindObjectOfType<ShopService>();
        shopPanel = FindObjectOfType<ShopPanelController>(true);
        selectionObjectOverride = gameObject;

        Collider targetCollider = GetComponent<Collider>();
        if (targetCollider != null)
        {
            targetCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (selectionObjectOverride == null)
        {
            selectionObjectOverride = gameObject;
        }
    }

    private void Start()
    {
        if (shopService == null)
        {
            shopService = FindObjectOfType<ShopService>();
        }

        if (shopPanel == null)
        {
            shopPanel = FindObjectOfType<ShopPanelController>(true);
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        if (shopPanel != null)
        {
            shopPanel.CloseRequested -= HandlePanelCloseRequested;
            shopPanel.CloseRequested += HandlePanelCloseRequested;
        }
    }

    private void OnEnable()
    {
        if (!registeredInteractables.Contains(this))
        {
            registeredInteractables.Add(this);
        }

        if (shopPanel != null)
        {
            shopPanel.CloseRequested += HandlePanelCloseRequested;
        }
    }

    private void OnDisable()
    {
        registeredInteractables.Remove(this);

        if (shopPanel != null)
        {
            shopPanel.CloseRequested -= HandlePanelCloseRequested;
        }

        if (activeInteractable == this)
        {
            CloseShop();
        }
    }

    private void Update()
    {
        bool playerInRange = IsPlayerInRange();

        if (playerInRange && Input.GetKeyDown(interactKey) && CanInteract() && IsPrimaryInteractionCandidate())
        {
            if (activeInteractable == this && shopPanel != null && shopPanel.IsOpen)
            {
                CloseShop();
            }
            else
            {
                OpenShop(false);
            }
        }

        if (activeInteractable == this && autoCloseWhenOutOfRange && !ignoreRangeUntilClosed && !playerInRange)
        {
            if (shopPanel != null && shopPanel.IsOpen)
            {
                CloseShop();
            }
        }
    }

    public static bool TryOpenShopByMerchantId(string merchantId)
    {
        if (string.IsNullOrWhiteSpace(merchantId))
        {
            return false;
        }

        for (int i = 0; i < registeredInteractables.Count; i++)
        {
            MerchantShopInteractable candidate = registeredInteractables[i];
            if (candidate == null || candidate.shopCatalog == null || candidate.shopPanel == null || candidate.shopService == null)
            {
                continue;
            }

            if (string.Equals(candidate.shopCatalog.ResolveMerchantId(), merchantId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                candidate.OpenShop(true);
                return true;
            }
        }

        return false;
    }

    private void OpenShop(bool ignoreRange)
    {
        if (shopPanel == null || shopService == null || shopCatalog == null)
        {
            Debug.LogWarning("MerchantShopInteractable: missing ShopPanel / ShopService / ShopCatalog on " + gameObject.name, this);
            return;
        }

        if (activeInteractable != null && activeInteractable != this)
        {
            activeInteractable.CloseShop();
        }

        activeInteractable = this;
        ignoreRangeUntilClosed = ignoreRange;

        UnlockCursorForShop();
        shopPanel.OpenShop(shopService, shopCatalog);
    }

    private void CloseShop()
    {
        if (shopPanel != null)
        {
            shopPanel.HidePanel();
        }

        if (activeInteractable == this)
        {
            activeInteractable = null;
        }

        ignoreRangeUntilClosed = false;
        RestoreCursorAfterShop();
    }

    private bool CanInteract()
    {
        if (!requireSelectionTarget)
        {
            return true;
        }

        if (SelectionManager.Instance == null)
        {
            return true;
        }

        if (!SelectionManager.Instance.onTarget)
        {
            return false;
        }

        return SelectionManager.Instance.selectedObject == selectionObjectOverride;
    }

    private bool IsPrimaryInteractionCandidate()
    {
        if (playerTransform == null)
        {
            return true;
        }

        float myDistance = Vector3.Distance(playerTransform.position, transform.position);
        for (int i = 0; i < registeredInteractables.Count; i++)
        {
            MerchantShopInteractable other = registeredInteractables[i];
            if (other == null || other == this)
            {
                continue;
            }

            if (!other.IsPlayerInRange())
            {
                continue;
            }

            if (other.playerTransform == null)
            {
                continue;
            }

            float otherDistance = Vector3.Distance(other.playerTransform.position, other.transform.position);
            if (otherDistance + 0.05f < myDistance)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsPlayerInRange()
    {
        bool inTriggerRange = !useTriggerRange || playerInTriggerRange;
        bool inDistanceRange = true;

        if (useDistanceRange)
        {
            if (playerTransform == null)
            {
                inDistanceRange = false;
            }
            else
            {
                inDistanceRange = Vector3.Distance(playerTransform.position, transform.position) <= interactionDistance;
            }
        }

        return inTriggerRange && inDistanceRange;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInTriggerRange = true;
        if (playerTransform == null)
        {
            playerTransform = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInTriggerRange = false;
    }

    private void HandlePanelCloseRequested()
    {
        if (activeInteractable != this)
        {
            return;
        }

        CloseShop();
    }

    private void UnlockCursorForShop()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (SelectionManager.Instance != null)
        {
            SelectionManager.Instance.DisableSelection();
            SelectionManager.Instance.enabled = false;
        }
    }

    private void RestoreCursorAfterShop()
    {
        bool keepCursorUnlocked = false;

        if (InventorySystem.Instance != null && InventorySystem.Instance.isOpen)
        {
            keepCursorUnlocked = true;
        }

        if (CraftingSystem.Instance != null && CraftingSystem.Instance.isOpen)
        {
            keepCursorUnlocked = true;
        }

        if (MenuManager.Instance != null && MenuManager.Instance.isMenuOpen)
        {
            keepCursorUnlocked = true;
        }

        if (keepCursorUnlocked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (SelectionManager.Instance != null)
        {
            SelectionManager.Instance.EnableSelection();
            SelectionManager.Instance.enabled = true;
        }
    }
}
