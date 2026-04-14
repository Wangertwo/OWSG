using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NpcChatInteractable : MonoBehaviour
{
    [SerializeField] private ChatFlowController chatFlowController;
    [SerializeField] private ChatPanelController chatPanel;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;
    [SerializeField] private bool useTriggerRange = true;
    [SerializeField] private bool useDistanceRange = true;
    [Min(0.5f)]
    [SerializeField] private float interactionDistance = 2.5f;
    [SerializeField] private bool requireSelectionTarget = false;
    [SerializeField] private GameObject selectionObjectOverride;
    [SerializeField] private bool autoCloseWhenOutOfRange = true;

    private bool playerInTriggerRange;

    private void Reset()
    {
        chatFlowController = FindObjectOfType<ChatFlowController>();
        chatPanel = FindObjectOfType<ChatPanelController>();
        selectionObjectOverride = gameObject;

        Collider targetCollider = GetComponent<Collider>();
        if (targetCollider != null)
        {
            targetCollider.isTrigger = true;
        }
    }

    private void Start()
    {
        if (chatFlowController == null)
        {
            chatFlowController = FindObjectOfType<ChatFlowController>();
        }

        if (selectionObjectOverride == null)
        {
            selectionObjectOverride = gameObject;
        }

        if (chatPanel == null)
        {
            chatPanel = FindObjectOfType<ChatPanelController>();
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        if (chatPanel != null)
        {
            chatPanel.CloseRequested -= HandleCloseRequested;
            chatPanel.CloseRequested += HandleCloseRequested;
        }
    }

    private void OnEnable()
    {
        if (chatPanel != null)
        {
            chatPanel.CloseRequested += HandleCloseRequested;
        }
    }

    private void OnDisable()
    {
        if (chatPanel != null)
        {
            chatPanel.CloseRequested -= HandleCloseRequested;
        }
    }

    private void Update()
    {
        bool playerInRange = IsPlayerInRange();

        if (playerInRange && Input.GetKeyDown(interactKey) && CanInteract())
        {
            if (chatFlowController != null && chatFlowController.IsChatUiOpen)
            {
                CloseChat();
            }
            else
            {
                OpenChat();
            }
        }

        if (chatFlowController != null && chatFlowController.IsChatUiOpen && Input.GetKeyDown(closeKey))
        {
            CloseChat();
        }

        if (autoCloseWhenOutOfRange && !playerInRange && chatFlowController != null && chatFlowController.IsChatUiOpen)
        {
            CloseChat();
        }
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

    private void OpenChat()
    {
        if (chatFlowController == null)
        {
            return;
        }

        UnlockCursorForChat();
        chatFlowController.OpenChatUI();

        if (chatPanel != null)
        {
            chatPanel.FocusInput();
        }
    }

    private void CloseChat()
    {
        if (chatFlowController != null)
        {
            chatFlowController.CloseChatUI();
        }

        RestoreCursorAfterChat();
    }

    private void UnlockCursorForChat()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (SelectionManager.Instance != null)
        {
            SelectionManager.Instance.DisableSelection();
            SelectionManager.Instance.enabled = false;
        }
    }

    private void RestoreCursorAfterChat()
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

    private void HandleCloseRequested()
    {
        CloseChat();
    }

    private bool IsPlayerInRange()
    {
        bool inTriggerRange = false;
        if (useTriggerRange)
        {
            inTriggerRange = playerInTriggerRange;
        }

        bool inDistanceRange = false;
        if (useDistanceRange)
        {
            Transform targetPlayer = playerTransform;
            if (targetPlayer == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    targetPlayer = player.transform;
                    playerTransform = targetPlayer;
                }
            }

            if (targetPlayer != null)
            {
                float distance = Vector3.Distance(targetPlayer.position, transform.position);
                inDistanceRange = distance <= interactionDistance;
            }
        }

        if (useTriggerRange && useDistanceRange)
        {
            return inTriggerRange || inDistanceRange;
        }

        if (useTriggerRange)
        {
            return inTriggerRange;
        }

        if (useDistanceRange)
        {
            return inDistanceRange;
        }

        return false;
    }
}
