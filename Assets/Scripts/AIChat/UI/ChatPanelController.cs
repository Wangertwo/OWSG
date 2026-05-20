using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public struct ChatQuickOption
{
    public string label;
    public string payload;

    public ChatQuickOption(string labelText, string payloadText)
    {
        label = labelText;
        payload = payloadText;
    }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(label)
               && !string.IsNullOrWhiteSpace(payload);
    }
}

public class ChatPanelController : MonoBehaviour
{
    private enum DialogueInputMode
    {
        Choice,
        FreeInput
    }

    private static readonly ChatQuickOption[] DefaultQuickOptions =
    {
        new ChatQuickOption("介绍下你自己", "介绍下你自己"),
        new ChatQuickOption("介绍下这个小镇", "介绍下这个小镇"),
        new ChatQuickOption("这里发生了什么", "这里发生了什么"),
        new ChatQuickOption("给我一点生存建议", "给我一点生存建议")
    };

    private static readonly string[] CjkFontCandidates =
    {
        "Microsoft YaHei",
        "Microsoft YaHei UI",
        "Microsoft JhengHei",
        "微软雅黑",
        "微軟正黑體",
        "DengXian",
        "等线",
        "SimHei",
        "SimSun",
        "黑体",
        "宋体",
        "Noto Sans CJK SC",
        "PingFang SC",
        "Source Han Sans SC",
        "Arial Unicode MS"
    };

    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Free Input")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;

    [Header("Common Buttons")]
    [SerializeField] private Button closeButton;

    [Header("Views")]
    [SerializeField] private ChatMessageView messageView;
    [SerializeField] private ChatStatusView statusView;

    [Header("Right Interaction Panel")]
    [SerializeField] private Button choiceModeTabButton;
    [SerializeField] private Button freeInputModeTabButton;
    [SerializeField] private GameObject choiceModePanel;
    [SerializeField] private GameObject freeInputModePanel;

    [Header("Quick Options")]
    [SerializeField] private RectTransform quickOptionsRoot;
    [SerializeField] private List<Button> quickOptionButtons = new List<Button>();
    [SerializeField] private bool autoCollectQuickOptionButtonsFromRoot = true;

    [Header("NPC Info / Dialogue Display")]
    [SerializeField] private Image npcPortraitImage;
    [SerializeField] private Sprite defaultNpcPortrait;
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TextMeshProUGUI npcTitleText;
    [SerializeField] private TextMeshProUGUI relationshipTagText;
    [SerializeField] private TextMeshProUGUI emotionTagText;
    [SerializeField] private TextMeshProUGUI currentLineText;

    [Header("Behavior")]
    [SerializeField] private string playerSpeakerName = "You";
    [SerializeField] private string npcSpeakerName = "NPC";
    [Range(1, 20)]
    [SerializeField] private int maxDisplayedDialogueLines = 8;
    [SerializeField] private bool defaultToChoiceMode = true;
    [SerializeField] private bool submitOnEnter = true;
    [SerializeField] private bool clearInputOnSend = true;
    [SerializeField] private TMP_FontAsset cjkFontOverride;

    private TMP_FontAsset runtimeCjkFontAsset;
    private readonly List<Button> runtimeQuickOptionButtons = new List<Button>();
    private readonly List<string> runtimeQuickOptionPayloads = new List<string>();
    private readonly List<UnityAction> runtimeQuickOptionActions = new List<UnityAction>();
    private readonly Queue<string> currentDialogueLines = new Queue<string>();
    private int lastKeyboardSubmitFrame = -1;

    private DialogueInputMode currentMode = DialogueInputMode.Choice;

    public event Action<string> SendRequested;
    public event Action<string> QuickOptionRequested;
    public event Action CloseRequested;

    public ChatStatusView StatusView => statusView;
    public bool IsOpen
    {
        get
        {
            GameObject root = GetPanelRoot();
            return root != null && root.activeSelf;
        }
    }

    private void Awake()
    {
        ResolveQuickOptionButtons();
        BindStaticButtonEvents();
        BindQuickOptionEvents();
        TryEnableCjkInputSupport();
        SyncMessageViewSpeakerNames();

        SetQuickOptions(null);
        SetInputMode(defaultToChoiceMode ? DialogueInputMode.Choice : DialogueInputMode.FreeInput, false);
    }

    private void OnDestroy()
    {
        UnbindStaticButtonEvents();
        UnbindQuickOptionEvents();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            return;
        }

        TrySendFromKeyboard(requireFocusedInput: true);
    }

    public void ShowPanel()
    {
        GameObject root = GetPanelRoot();
        if (root == null)
        {
            return;
        }

        root.SetActive(true);
        EnsureQuickOptionsVisibleOnShow();
        SetInputMode(defaultToChoiceMode ? DialogueInputMode.Choice : DialogueInputMode.FreeInput, false);
        StartCoroutine(FocusInputNextFrame());
    }

    public void HidePanel()
    {
        GameObject root = GetPanelRoot();
        if (root == null)
        {
            return;
        }

        root.SetActive(false);
    }

    public void FocusInput()
    {
        if (inputField == null)
        {
            return;
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(inputField.gameObject);
        }

        inputField.ActivateInputField();
        inputField.Select();
    }

    public void SetInputLocked(bool isLocked)
    {
        if (inputField != null)
        {
            inputField.interactable = !isLocked;
        }

        if (sendButton != null)
        {
            sendButton.interactable = !isLocked;
        }

        SetQuickOptionsInteractable(!isLocked);
    }

    public void SetSendInteractable(bool isInteractable)
    {
        if (sendButton != null)
        {
            sendButton.interactable = isInteractable;
        }

        if (inputField != null)
        {
            inputField.interactable = isInteractable;
        }

        SetQuickOptionsInteractable(isInteractable);
    }

    public void SetQuickOptions(IReadOnlyList<ChatQuickOption> options)
    {
        ResolveQuickOptionButtons();

        if (runtimeQuickOptionButtons.Count == 0)
        {
            return;
        }

        if (options == null || options.Count == 0)
        {
            for (int i = 0; i < runtimeQuickOptionButtons.Count; i++)
            {
                if (runtimeQuickOptionButtons[i] != null)
                {
                    runtimeQuickOptionButtons[i].gameObject.SetActive(false);
                }

                if (i < runtimeQuickOptionPayloads.Count)
                {
                    runtimeQuickOptionPayloads[i] = string.Empty;
                }
            }

            if (quickOptionsRoot != null)
            {
                quickOptionsRoot.gameObject.SetActive(false);
            }

            return;
        }

        int visibleCount = Mathf.Min(options.Count, runtimeQuickOptionButtons.Count);

        for (int i = 0; i < visibleCount; i++)
        {
            ChatQuickOption option = options[i];
            Button optionButton = runtimeQuickOptionButtons[i];

            if (optionButton == null || !option.IsValid())
            {
                if (optionButton != null)
                {
                    optionButton.gameObject.SetActive(false);
                }

                runtimeQuickOptionPayloads[i] = string.Empty;
                continue;
            }

            runtimeQuickOptionPayloads[i] = option.payload.Trim();
            optionButton.gameObject.SetActive(true);
            optionButton.interactable = true;
            SetButtonLabel(optionButton, option.label.Trim());
        }

        for (int i = visibleCount; i < runtimeQuickOptionButtons.Count; i++)
        {
            if (runtimeQuickOptionButtons[i] != null)
            {
                runtimeQuickOptionButtons[i].gameObject.SetActive(false);
            }

            runtimeQuickOptionPayloads[i] = string.Empty;
        }

        if (quickOptionsRoot != null)
        {
            quickOptionsRoot.gameObject.SetActive(true);
        }
    }

    public void AppendPlayerMessage(string message)
    {
        if (messageView != null)
        {
            messageView.AppendPlayerMessage(message);
        }

        AppendDialogueLine(playerSpeakerName, message);
    }

    public void AppendAssistantMessage(string message)
    {
        if (messageView != null)
        {
            messageView.AppendAssistantMessage(message);
        }

        AppendDialogueLine(npcSpeakerName, message);
    }

    public void AppendSystemMessage(string message)
    {
        if (messageView != null)
        {
            messageView.AppendSystemMessage(message);
        }
    }

    public void ClearMessages()
    {
        if (messageView != null)
        {
            messageView.Clear();
        }

        if (currentLineText != null)
        {
            currentLineText.text = string.Empty;
        }

        currentDialogueLines.Clear();
    }

    public void SetNpcHeader(string displayName, string roleTitle)
    {
        if (npcNameText != null)
        {
            npcNameText.text = string.IsNullOrWhiteSpace(displayName) ? "未知NPC" : displayName.Trim();
        }

        if (npcTitleText != null)
        {
            npcTitleText.text = string.IsNullOrWhiteSpace(roleTitle) ? "幸存者" : roleTitle.Trim();
        }

        string resolvedSpeaker = string.IsNullOrWhiteSpace(displayName) ? roleTitle : displayName;
        if (!string.IsNullOrWhiteSpace(resolvedSpeaker))
        {
            npcSpeakerName = resolvedSpeaker.Trim();
            SyncMessageViewSpeakerNames();
        }
    }

    public void SetNpcPortrait(Sprite portrait)
    {
        if (npcPortraitImage == null)
        {
            return;
        }

        Sprite resolvedPortrait = portrait != null ? portrait : defaultNpcPortrait;
        npcPortraitImage.sprite = resolvedPortrait;
        npcPortraitImage.enabled = resolvedPortrait != null;
    }

    public void SetRelationshipTag(string relationshipText)
    {
        if (relationshipTagText != null)
        {
            relationshipTagText.text = string.IsNullOrWhiteSpace(relationshipText) ? "关系: 未知" : relationshipText.Trim();
        }
    }

    public void SetEmotionTag(string emotionText)
    {
        if (emotionTagText != null)
        {
            emotionTagText.text = string.IsNullOrWhiteSpace(emotionText) ? "状态: 中立" : "状态: " + emotionText.Trim();
        }
    }

    public void SwitchToChoiceMode()
    {
        SetInputMode(DialogueInputMode.Choice, false);
    }

    public void SwitchToFreeInputMode()
    {
        SetInputMode(DialogueInputMode.FreeInput, true);
    }

    public void RefreshBindings()
    {
        UnbindQuickOptionEvents();
        ResolveQuickOptionButtons();
        BindQuickOptionEvents();
    }

    private void HandleSendClick()
    {
        string text = inputField == null ? string.Empty : inputField.text;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        SendRequested?.Invoke(text.Trim());

        if (!clearInputOnSend || inputField == null)
        {
            return;
        }

        inputField.text = string.Empty;
        FocusInput();
    }

    private void HandleCloseClick()
    {
        CloseRequested?.Invoke();
    }

    private void HandleQuickOptionClick(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= runtimeQuickOptionPayloads.Count)
        {
            return;
        }

        string payload = runtimeQuickOptionPayloads[optionIndex];
        if (string.IsNullOrWhiteSpace(payload))
        {
            return;
        }

        QuickOptionRequested?.Invoke(payload);
        SetInputMode(DialogueInputMode.Choice, false);
    }

    private GameObject GetPanelRoot()
    {
        return panelRoot != null ? panelRoot : gameObject;
    }

    private IEnumerator FocusInputNextFrame()
    {
        yield return null;
        FocusInput();
    }

    private void BindStaticButtonEvents()
    {
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(HandleSendClick);
            sendButton.onClick.AddListener(HandleSendClick);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseClick);
            closeButton.onClick.AddListener(HandleCloseClick);
        }

        if (choiceModeTabButton != null)
        {
            choiceModeTabButton.onClick.RemoveListener(SwitchToChoiceMode);
            choiceModeTabButton.onClick.AddListener(SwitchToChoiceMode);
        }

        if (freeInputModeTabButton != null)
        {
            freeInputModeTabButton.onClick.RemoveListener(SwitchToFreeInputMode);
            freeInputModeTabButton.onClick.AddListener(SwitchToFreeInputMode);
        }

        if (inputField != null)
        {
            inputField.onSubmit.RemoveListener(HandleInputSubmit);
            inputField.onSubmit.AddListener(HandleInputSubmit);
        }
    }

    private void UnbindStaticButtonEvents()
    {
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(HandleSendClick);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseClick);
        }

        if (choiceModeTabButton != null)
        {
            choiceModeTabButton.onClick.RemoveListener(SwitchToChoiceMode);
        }

        if (freeInputModeTabButton != null)
        {
            freeInputModeTabButton.onClick.RemoveListener(SwitchToFreeInputMode);
        }

        if (inputField != null)
        {
            inputField.onSubmit.RemoveListener(HandleInputSubmit);
        }
    }

    private void ResolveQuickOptionButtons()
    {
        runtimeQuickOptionButtons.Clear();
        runtimeQuickOptionPayloads.Clear();

        if (quickOptionButtons != null)
        {
            for (int i = 0; i < quickOptionButtons.Count; i++)
            {
                AddQuickOptionButtonIfValid(quickOptionButtons[i]);
            }
        }

        if (runtimeQuickOptionButtons.Count == 0 && autoCollectQuickOptionButtonsFromRoot && quickOptionsRoot != null)
        {
            for (int i = 0; i < quickOptionsRoot.childCount; i++)
            {
                Transform child = quickOptionsRoot.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                Button button = child.GetComponent<Button>();
                if (button == null)
                {
                    button = child.GetComponentInChildren<Button>(true);
                }

                AddQuickOptionButtonIfValid(button);
            }
        }

        if (runtimeQuickOptionButtons.Count == 0 && quickOptionsRoot != null)
        {
            Button[] nestedButtons = quickOptionsRoot.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < nestedButtons.Length; i++)
            {
                AddQuickOptionButtonIfValid(nestedButtons[i]);
            }
        }

        for (int i = 0; i < runtimeQuickOptionButtons.Count; i++)
        {
            runtimeQuickOptionPayloads.Add(string.Empty);
        }
    }

    private void AddQuickOptionButtonIfValid(Button button)
    {
        if (button == null)
        {
            return;
        }

        if (runtimeQuickOptionButtons.Contains(button))
        {
            return;
        }

        runtimeQuickOptionButtons.Add(button);
    }

    private void BindQuickOptionEvents()
    {
        runtimeQuickOptionActions.Clear();

        for (int i = 0; i < runtimeQuickOptionButtons.Count; i++)
        {
            Button button = runtimeQuickOptionButtons[i];
            if (button == null)
            {
                runtimeQuickOptionActions.Add(null);
                continue;
            }

            int capturedIndex = i;
            UnityAction action = () => HandleQuickOptionClick(capturedIndex);
            button.onClick.AddListener(action);
            runtimeQuickOptionActions.Add(action);
        }
    }

    private void UnbindQuickOptionEvents()
    {
        int count = Mathf.Min(runtimeQuickOptionButtons.Count, runtimeQuickOptionActions.Count);
        for (int i = 0; i < count; i++)
        {
            if (runtimeQuickOptionButtons[i] != null && runtimeQuickOptionActions[i] != null)
            {
                runtimeQuickOptionButtons[i].onClick.RemoveListener(runtimeQuickOptionActions[i]);
            }
        }

        runtimeQuickOptionActions.Clear();
    }

    private void SetQuickOptionsInteractable(bool isInteractable)
    {
        for (int i = 0; i < runtimeQuickOptionButtons.Count; i++)
        {
            if (runtimeQuickOptionButtons[i] != null)
            {
                runtimeQuickOptionButtons[i].interactable = isInteractable;
            }
        }
    }

    private void SetButtonLabel(Button button, string text)
    {
        if (button == null)
        {
            return;
        }

        TextMeshProUGUI labelText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (labelText != null)
        {
            labelText.text = text;
        }
    }

    private void EnsureQuickOptionsVisibleOnShow()
    {
        ResolveQuickOptionButtons();

        bool hasAnyPayload = false;
        for (int i = 0; i < runtimeQuickOptionPayloads.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(runtimeQuickOptionPayloads[i]))
            {
                hasAnyPayload = true;
                break;
            }
        }

        if (!hasAnyPayload)
        {
            SetQuickOptions(DefaultQuickOptions);
        }
    }

    private void SetInputMode(DialogueInputMode mode, bool focusInput)
    {
        currentMode = mode;

        if (choiceModePanel != null)
        {
            choiceModePanel.SetActive(mode == DialogueInputMode.Choice);
        }

        if (freeInputModePanel != null)
        {
            freeInputModePanel.SetActive(mode == DialogueInputMode.FreeInput);
        }

        if (choiceModeTabButton != null)
        {
            choiceModeTabButton.interactable = mode != DialogueInputMode.Choice;
        }

        if (freeInputModeTabButton != null)
        {
            freeInputModeTabButton.interactable = mode != DialogueInputMode.FreeInput;
        }

        if (sendButton != null)
        {
            sendButton.gameObject.SetActive(mode == DialogueInputMode.FreeInput || freeInputModePanel == null);
        }

        if (inputField != null)
        {
            inputField.gameObject.SetActive(mode == DialogueInputMode.FreeInput || freeInputModePanel == null);
        }

        if (focusInput)
        {
            FocusInput();
        }
    }

    private void TryEnableCjkInputSupport()
    {
        if (inputField == null || inputField.textComponent == null)
        {
            return;
        }

        const string cjkSample = "你好，！？；：（）【】《》“”‘’";

        if (cjkFontOverride != null)
        {
            ApplyFontToChatPanelTexts(cjkFontOverride);
            return;
        }

        TMP_FontAsset currentFont = inputField.textComponent.font;
        if (currentFont != null && currentFont.HasCharacters(cjkSample))
        {
            return;
        }

        runtimeCjkFontAsset = CreateRuntimeCjkFontAsset();
        if (runtimeCjkFontAsset == null)
        {
            Debug.LogWarning("ChatPanelController: no CJK OS font found. Chinese input may not render correctly.");
            return;
        }

        ApplyFontToChatPanelTexts(runtimeCjkFontAsset);
    }

    private TMP_FontAsset CreateRuntimeCjkFontAsset()
    {
        const string cjkSample = "你好，！？；：（）【】《》“”‘’";

        TMP_FontAsset candidateAsset = TryCreateCjkFontAssetFromNames(CjkFontCandidates, cjkSample);
        if (candidateAsset != null)
        {
            return candidateAsset;
        }

        string[] installedFontNames = Font.GetOSInstalledFontNames();
        if (installedFontNames == null || installedFontNames.Length == 0)
        {
            return null;
        }

        return TryCreateCjkFontAssetFromNames(installedFontNames, cjkSample);
    }

    private TMP_FontAsset TryCreateCjkFontAssetFromNames(string[] fontNames, string cjkSample)
    {
        if (fontNames == null)
        {
            return null;
        }

        foreach (string fontName in fontNames)
        {
            if (string.IsNullOrWhiteSpace(fontName))
            {
                continue;
            }

            Font osFont = Font.CreateDynamicFontFromOSFont(fontName, 32);
            if (osFont == null)
            {
                continue;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(osFont);
            if (fontAsset != null)
            {
                fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                if (IsCjkSampleSupported(fontAsset, cjkSample))
                {
                    return fontAsset;
                }
            }
        }

        return null;
    }

    private bool IsCjkSampleSupported(TMP_FontAsset fontAsset, string cjkSample)
    {
        if (fontAsset == null)
        {
            return false;
        }

        if (fontAsset.HasCharacters(cjkSample))
        {
            return true;
        }

        string missingCharacters;
        bool couldAdd = fontAsset.TryAddCharacters(cjkSample, out missingCharacters);
        return couldAdd && string.IsNullOrEmpty(missingCharacters);
    }

    private void ApplyFontToChatPanelTexts(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null)
        {
            return;
        }

        GameObject root = GetPanelRoot();
        if (root == null)
        {
            return;
        }

        TextMeshProUGUI[] textComponents = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI textComponent in textComponents)
        {
            if (textComponent != null)
            {
                textComponent.font = fontAsset;
            }
        }
    }

    private void AppendDialogueLine(string speakerName, string message)
    {
        if (currentLineText == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        string resolvedSpeaker = string.IsNullOrWhiteSpace(speakerName) ? "NPC" : speakerName.Trim();
        string line = resolvedSpeaker + ": " + message.Trim();

        currentDialogueLines.Enqueue(line);

        int maxLines = Mathf.Max(1, maxDisplayedDialogueLines);
        while (currentDialogueLines.Count > maxLines)
        {
            currentDialogueLines.Dequeue();
        }

        currentLineText.text = string.Join("\n", currentDialogueLines.ToArray());
    }

    private void SyncMessageViewSpeakerNames()
    {
        if (messageView == null)
        {
            return;
        }

        string resolvedPlayer = string.IsNullOrWhiteSpace(playerSpeakerName) ? "You" : playerSpeakerName.Trim();
        string resolvedNpc = string.IsNullOrWhiteSpace(npcSpeakerName) ? "NPC" : npcSpeakerName.Trim();
        messageView.SetSpeakerNames(resolvedPlayer, resolvedNpc, "System");
    }

    private void HandleInputSubmit(string _)
    {
        TrySendFromKeyboard(requireFocusedInput: false);
    }

    private void TrySendFromKeyboard(bool requireFocusedInput)
    {
        if (!CanSubmitFromKeyboard(requireFocusedInput))
        {
            return;
        }

        if (lastKeyboardSubmitFrame == Time.frameCount)
        {
            return;
        }

        lastKeyboardSubmitFrame = Time.frameCount;
        HandleSendClick();
    }

    private bool CanSubmitFromKeyboard(bool requireFocusedInput)
    {
        if (!submitOnEnter || !IsOpen)
        {
            return false;
        }

        if (currentMode != DialogueInputMode.FreeInput)
        {
            return false;
        }

        if (inputField == null || !inputField.gameObject.activeInHierarchy || !inputField.interactable)
        {
            return false;
        }

        if (requireFocusedInput && !inputField.isFocused)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(Input.compositionString))
        {
            return false;
        }

        return true;
    }
}
