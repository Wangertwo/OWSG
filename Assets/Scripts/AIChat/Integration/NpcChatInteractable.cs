using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class NpcChatInteractable : MonoBehaviour
{
    [SerializeField] private ChatFlowController chatFlowController;
    [SerializeField] private ChatPanelController chatPanel;
    [SerializeField] private NpcDialogueAgent npcDialogueAgent;
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

    private static NpcChatInteractable activeInteractable;
    private static readonly List<NpcChatInteractable> registeredInteractables = new List<NpcChatInteractable>();
    private bool playerInTriggerRange;
    private Coroutine ensureIdleRoutine;

    private void Reset()
    {
        chatFlowController = FindObjectOfType<ChatFlowController>();
        chatPanel = FindObjectOfType<ChatPanelController>();
        npcDialogueAgent = GetComponent<NpcDialogueAgent>();
        if (npcDialogueAgent == null)
        {
            npcDialogueAgent = GetComponentInParent<NpcDialogueAgent>();
        }
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

        if (npcDialogueAgent == null)
        {
            npcDialogueAgent = GetComponent<NpcDialogueAgent>();
            if (npcDialogueAgent == null)
            {
                npcDialogueAgent = GetComponentInParent<NpcDialogueAgent>();
            }
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
        if (!registeredInteractables.Contains(this))
        {
            registeredInteractables.Add(this);
        }

        if (chatPanel != null)
        {
            chatPanel.CloseRequested += HandleCloseRequested;
        }
    }

    private void OnDisable()
    {
        registeredInteractables.Remove(this);

        if (chatPanel != null)
        {
            chatPanel.CloseRequested -= HandleCloseRequested;
        }

        if (activeInteractable == this)
        {
            activeInteractable = null;
        }
    }

    private void Update()
    {
        bool playerInRange = IsPlayerInRange();

        if (playerInRange && Input.GetKeyDown(interactKey) && CanInteract() && IsPrimaryInteractionCandidate(playerInRange))
        {
            if (chatFlowController == null || !chatFlowController.IsChatUiOpen)
            {
                OpenChat();
            }
        }

        bool isCurrentActiveNpc = activeInteractable == this;

        if (isCurrentActiveNpc && chatFlowController != null && chatFlowController.IsChatUiOpen && Input.GetKeyDown(closeKey))
        {
            CloseChat();
        }

        if (isCurrentActiveNpc && autoCloseWhenOutOfRange && !playerInRange &&
            chatFlowController != null && chatFlowController.IsChatUiOpen)
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

        ResolveOrCreateDialogueAgent();

        if (ensureIdleRoutine != null)
        {
            StopCoroutine(ensureIdleRoutine);
            ensureIdleRoutine = null;
        }

        if (activeInteractable != null && activeInteractable != this)
        {
            activeInteractable.ForceReturnToIdle();
        }

        activeInteractable = this;
        chatFlowController.SetActiveNpc(npcDialogueAgent);

        CharacterAnimationController activeAnimationController = ResolveAnimationController();
        if (activeAnimationController != null)
        {
            activeAnimationController.ApplyAnimation("talk");
        }
        else
        {
            Debug.LogWarning("NpcChatInteractable: no CharacterAnimationController found on " + gameObject.name + ". NPC talking animation cannot be applied.");
        }

        UnlockCursorForChat();
        chatFlowController.OpenChatUI();
        chatFlowController.SetActiveNpc(npcDialogueAgent);

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
            chatFlowController.ClearActiveNpc();
        }

        CharacterAnimationController activeAnimationController = ResolveAnimationController();
        if (activeAnimationController != null)
        {
            activeAnimationController.ApplyAnimation("idle");
        }

        if (ensureIdleRoutine != null)
        {
            StopCoroutine(ensureIdleRoutine);
        }
        ensureIdleRoutine = StartCoroutine(EnsureIdleAfterClose());

        if (activeInteractable == this)
        {
            activeInteractable = null;
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
        if (activeInteractable != this)
        {
            return;
        }

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

    private CharacterAnimationController ResolveAnimationController()
    {
        if (npcDialogueAgent != null && npcDialogueAgent.AnimationController != null)
        {
            return npcDialogueAgent.AnimationController;
        }

        CharacterAnimationController localController = GetComponent<CharacterAnimationController>();
        if (localController != null)
        {
            return localController;
        }

        CharacterAnimationController childController = GetComponentInChildren<CharacterAnimationController>(true);
        if (childController != null)
        {
            return childController;
        }

        return GetComponentInParent<CharacterAnimationController>();
    }

    private void ResolveOrCreateDialogueAgent()
    {
        if (npcDialogueAgent != null)
        {
            return;
        }

        npcDialogueAgent = GetComponent<NpcDialogueAgent>();
        if (npcDialogueAgent == null)
        {
            npcDialogueAgent = GetComponentInParent<NpcDialogueAgent>();
        }

        if (npcDialogueAgent == null)
        {
            npcDialogueAgent = gameObject.AddComponent<NpcDialogueAgent>();
            Debug.LogWarning("NpcChatInteractable: NpcDialogueAgent was missing. Added a default agent at runtime on " + gameObject.name + ".");
        }
    }

    private bool IsPrimaryInteractionCandidate(bool thisInRange)
    {
        if (!thisInRange)
        {
            return false;
        }

        Transform thisPlayer = ResolvePlayerTransformReference();
        float thisDistance = ResolveDistanceToPlayer(thisPlayer);
        int thisId = GetInstanceID();

        for (int i = 0; i < registeredInteractables.Count; i++)
        {
            NpcChatInteractable other = registeredInteractables[i];
            if (other == null || other == this || !other.isActiveAndEnabled)
            {
                continue;
            }

            if (!other.IsPlayerInRange() || !other.CanInteract())
            {
                continue;
            }

            Transform otherPlayer = other.ResolvePlayerTransformReference();
            float otherDistance = other.ResolveDistanceToPlayer(otherPlayer);

            if (otherDistance + 0.01f < thisDistance)
            {
                return false;
            }

            if (Mathf.Abs(otherDistance - thisDistance) <= 0.01f && other.GetInstanceID() < thisId)
            {
                return false;
            }
        }

        return true;
    }

    private Transform ResolvePlayerTransformReference()
    {
        if (playerTransform != null)
        {
            return playerTransform;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        return playerTransform;
    }

    private float ResolveDistanceToPlayer(Transform targetPlayer)
    {
        if (targetPlayer == null)
        {
            return float.MaxValue;
        }

        return Vector3.Distance(targetPlayer.position, transform.position);
    }

    private void ForceReturnToIdle()
    {
        CharacterAnimationController activeAnimationController = ResolveAnimationController();
        if (activeAnimationController != null)
        {
            activeAnimationController.ApplyAnimation("idle");
        }
    }

    private System.Collections.IEnumerator EnsureIdleAfterClose()
    {
        yield return null;
        ForceReturnToIdle();
        ensureIdleRoutine = null;
    }
}
