using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ChatFlowController : MonoBehaviour
{
    [SerializeField] private ChatConfig config;
    [SerializeField] private AiGatewayClient gatewayClient;
    [SerializeField] private ChatPanelController chatPanel;
    [SerializeField] private ChatStatusView statusView;
    [SerializeField] private CharacterExpressionController expressionController;
    [SerializeField] private CharacterAnimationController animationController;
    [SerializeField] private bool connectOnStart = true;

    private ChatSessionContext sessionContext;
    private ChatPanelController subscribedPanel;
    private Coroutine connectRoutine;
    private CharacterExpressionController defaultExpressionController;
    private CharacterAnimationController defaultAnimationController;
    private NpcDialogueAgent activeNpc;
    private readonly List<ChatConversationTurn> recentTurns = new List<ChatConversationTurn>();
    private readonly Dictionary<string, List<ChatConversationTurn>> conversationHistoryByNpcId = new Dictionary<string, List<ChatConversationTurn>>();
    private const int MaxRecentTurns = 10;

    private static readonly ChatQuickOption[] DefaultNpcQuickOptions =
    {
        new ChatQuickOption("介绍下你自己", "介绍下你自己"),
        new ChatQuickOption("介绍下这个小镇", "介绍下这个小镇"),
        new ChatQuickOption("这里发生了什么", "这里发生了什么"),
        new ChatQuickOption("给我一点生存建议", "给我一点生存建议")
    };

    public event Action<ChatState> StateChanged;

    public ChatState CurrentState => sessionContext == null ? ChatState.Disconnected : sessionContext.State;
    public bool IsChatUiOpen => chatPanel != null && chatPanel.IsOpen;

    private void Awake()
    {
        defaultExpressionController = expressionController;
        defaultAnimationController = animationController;
        EnsureContext();
    }

    private void OnEnable()
    {
        TrySubscribePanelEvents();
    }

    private void OnDisable()
    {
        UnsubscribePanelEvents();
    }

    public void SetConnectOnStart(bool shouldConnect)
    {
        connectOnStart = shouldConnect;
    }

    public void SetActiveNpc(NpcDialogueAgent npcAgent)
    {
        bool npcChanged = activeNpc != npcAgent;
        activeNpc = npcAgent;

        if (npcChanged)
        {
            recentTurns.Clear();
            LoadConversationHistoryForActiveNpc();

            if (chatPanel != null)
            {
                chatPanel.ClearMessages();
                RestoreConversationHistoryToPanel();
            }
        }

        EnsureContext();
        ApplyNpcBindings();
        ApplyNpcHudInfo();
        RefreshConversationEntryPoints();
    }

    public void ClearActiveNpc()
    {
        activeNpc = null;
        recentTurns.Clear();
        EnsureContext();
        ApplyNpcBindings();
        ApplyNpcHudInfo();
        RefreshConversationEntryPoints();
    }

    public void Configure(
        ChatConfig chatConfig,
        AiGatewayClient aiGatewayClient,
        ChatPanelController panelController,
        ChatStatusView chatStatus,
        CharacterExpressionController characterExpression,
        CharacterAnimationController characterAnimation)
    {
        config = chatConfig;
        gatewayClient = aiGatewayClient;
        chatPanel = panelController;
        statusView = chatStatus;
        expressionController = characterExpression;
        animationController = characterAnimation;
        defaultExpressionController = characterExpression;
        defaultAnimationController = characterAnimation;

        EnsureContext();
        ApplyNpcBindings();
        ApplyNpcHudInfo();
        RefreshConversationEntryPoints();
        TrySubscribePanelEvents();
    }

    public void Bootstrap()
    {
        EnsureContext();
        ApplyNpcBindings();
        ApplyNpcHudInfo();
        RefreshConversationEntryPoints();

        if (statusView == null && chatPanel != null)
        {
            statusView = chatPanel.StatusView;
        }

        if (chatPanel != null)
        {
            chatPanel.SetSendInteractable(false);
        }

        if (!connectOnStart)
        {
            UpdateState(ChatState.Idle);

            if (statusView != null)
            {
                statusView.SetDisconnected();
            }

            if (chatPanel != null)
            {
                chatPanel.SetInputLocked(false);
                chatPanel.SetSendInteractable(activeNpc != null);
            }

            return;
        }

        Connect();
    }

    public void Connect()
    {
        if (connectRoutine != null)
        {
            StopCoroutine(connectRoutine);
        }

        connectRoutine = StartCoroutine(ConnectRoutine());
    }

    public void OpenChatUI()
    {
        if (chatPanel != null)
        {
            chatPanel.ShowPanel();
        }

        RefreshConversationEntryPoints();

        if (CurrentState == ChatState.Disconnected || CurrentState == ChatState.Error)
        {
            Connect();
        }
    }

    public void CloseChatUI()
    {
        if (chatPanel != null)
        {
            chatPanel.HidePanel();
        }
    }

    private void HandleSendRequested(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText))
        {
            return;
        }

        if (CurrentState == ChatState.Sending)
        {
            return;
        }

        ApplyNpcBindings();

        if (activeNpc == null)
        {
            if (chatPanel != null)
            {
                chatPanel.AppendSystemMessage("请先面对 NPC 按 E 开始对话。");
            }

            return;
        }

        if (CurrentState == ChatState.Connecting)
        {
            if (chatPanel != null)
            {
                chatPanel.AppendSystemMessage("Gateway is still connecting...");
            }

            return;
        }

        if (CurrentState == ChatState.Disconnected || CurrentState == ChatState.Error)
        {
            Connect();

            if (chatPanel != null)
            {
                chatPanel.AppendSystemMessage("Connection is not ready. Reconnecting...");
            }

            return;
        }

        if (CurrentState != ChatState.Idle)
        {
            return;
        }

        StartCoroutine(SendRoutine(userText));
    }

    private IEnumerator ConnectRoutine()
    {
        EnsureContext();

        if (gatewayClient == null || config == null)
        {
            SetErrorState("CHAT_SETUP_MISSING");
            connectRoutine = null;
            yield break;
        }

        gatewayClient.SetConfig(config);

        UpdateState(ChatState.Connecting);

        if (statusView != null)
        {
            statusView.SetConnecting();
        }

        if (chatPanel != null)
        {
            chatPanel.SetInputLocked(false);
            chatPanel.SetSendInteractable(activeNpc != null);
        }

        HealthResponse healthResponse = null;
        string requestError = null;

        yield return StartCoroutine(gatewayClient.CheckHealth((result, errorCode) =>
        {
            healthResponse = result;
            requestError = errorCode;
        }));

        if (!string.IsNullOrEmpty(requestError))
        {
            sessionContext.SetError(requestError);
            UpdateState(ChatState.Idle);

            if (statusView != null)
            {
                statusView.SetError(requestError);
            }

            if (chatPanel != null)
            {
                chatPanel.SetInputLocked(false);
                chatPanel.SetSendInteractable(activeNpc != null);
                chatPanel.AppendSystemMessage("Gateway unavailable: " + requestError);
            }

            connectRoutine = null;
            yield break;
        }

        if (healthResponse == null || !healthResponse.ok)
        {
            sessionContext.SetError("HEALTH_CHECK_FAILED");
            UpdateState(ChatState.Idle);

            if (statusView != null)
            {
                statusView.SetError("HEALTH_CHECK_FAILED");
            }

            if (chatPanel != null)
            {
                chatPanel.SetInputLocked(false);
                chatPanel.SetSendInteractable(activeNpc != null);
                chatPanel.AppendSystemMessage("Gateway unavailable: HEALTH_CHECK_FAILED");
            }

            connectRoutine = null;
            yield break;
        }

        sessionContext.ClearError();
        UpdateState(ChatState.Idle);

        if (statusView != null)
        {
            statusView.SetConnected(healthResponse.service, healthResponse.version);
        }

        if (chatPanel != null)
        {
            chatPanel.SetInputLocked(false);
            chatPanel.SetSendInteractable(activeNpc != null);
        }

        connectRoutine = null;
    }

    private IEnumerator SendRoutine(string userText)
    {
        UpdateState(ChatState.Sending);

        if (statusView != null)
        {
            statusView.SetSending();
        }

        if (chatPanel != null)
        {
            chatPanel.SetInputLocked(true);
            chatPanel.AppendPlayerMessage(userText);
        }

        PushConversationTurn("player", userText);

        EnsureContext();
        ApplyNpcBindings();

        if (ShouldHandleFishingRumorVerificationLocally(userText) && activeNpc != null)
        {
            ChatResponse ruleResponse;
            if (activeNpc.TryBuildRuleResponse(userText, out ruleResponse))
            {
                ApplySuccessfulResponse(ruleResponse);
                yield break;
            }
        }

        string requestUserText = userText;
        string configuredChatSubMode = ResolveActiveNpcChatMode();
        FishingStoryService storyService = FishingStoryService.Instance;
        FishingRumorService rumorService = FishingRumorService.Instance;
        bool forceFishingRumorMode = false;
        string failedVerificationReplyPrefix = string.Empty;

        if (ShouldHandleFailedFishingRumorFeedbackLocally(userText) && activeNpc != null)
        {
            string npcId = ResolveCharacterIdForSession(config == null ? "npc_001" : config.characterId);
            if (rumorService == null)
            {
                ApplySuccessfulResponse(BuildLocalChatResponse("渔闻验证系统还没启动，我暂时没法核对失败记录。", "neutral", "talk"));
                yield break;
            }

            string compensationReply;
            bool shouldGiveNewRumor = rumorService.TryClaimFailedVerificationCompensation(npcId, out compensationReply);
            if (!shouldGiveNewRumor)
            {
                ApplySuccessfulResponse(BuildLocalChatResponse(compensationReply, "neutral", "talk"));
                yield break;
            }

            failedVerificationReplyPrefix = compensationReply;
            requestUserText = "上一条渔闻验证失败了，请带着歉意给出另一条新的渔闻。";
            forceFishingRumorMode = true;
        }

        if (ShouldHandleTrustedFishTradeLocally(userText) && activeNpc != null)
        {
            ApplySuccessfulResponse(BuildTrustedFishTradeResponse(rumorService));
            yield break;
        }

        bool isFishingStoryQuery = false;
        string storyNpcId = string.Empty;

        if (storyService != null && activeNpc != null && (forceFishingRumorMode || storyService.IsStoryQuery(userText)))
        {
            isFishingStoryQuery = true;
            string defaultCharacterId = config == null ? "npc_001" : config.characterId;
            storyNpcId = activeNpc.GetResolvedCharacterId(defaultCharacterId);
        }

        bool isFishingRumorMode = forceFishingRumorMode || isFishingStoryQuery || ShouldUseFishingRumorMode(userText, configuredChatSubMode);
        string requestChatSubMode = isFishingRumorMode ? "fishing_rumor" : "default_chat";

        ChatRequest request = sessionContext.BuildRequest(requestUserText, requestChatSubMode);
        PopulateNpcRoutingFields(request, requestUserText, isFishingRumorMode, requestChatSubMode);
        if (isFishingRumorMode)
        {
            AttachFishingRumorContext(request, requestUserText);
        }
        else
        {
            request.user_text = BuildCompactNpcPrompt(request, requestUserText);
            request.user_query = request.user_text;
        }

        ChatResponse chatResponse = null;
        string requestError = null;

        yield return StartCoroutine(gatewayClient.SendChat(request, (result, errorCode) =>
        {
            chatResponse = result;
            requestError = errorCode;
        }));

        if (!string.IsNullOrEmpty(requestError))
        {
            sessionContext.SetError(requestError);
            UpdateState(ChatState.Idle);

            if (statusView != null)
            {
                statusView.SetError(requestError);
            }

            if (chatPanel != null)
            {
                chatPanel.AppendSystemMessage("Request failed: " + requestError);
                chatPanel.SetInputLocked(false);
                chatPanel.SetSendInteractable(activeNpc != null);
            }

            yield break;
        }

        if (chatResponse == null)
        {
            sessionContext.SetError("INVALID_CHAT_RESPONSE");
            UpdateState(ChatState.Idle);

            if (statusView != null)
            {
                statusView.SetError("INVALID_CHAT_RESPONSE");
            }

            if (chatPanel != null)
            {
                chatPanel.AppendSystemMessage("Invalid response from server.");
                chatPanel.SetInputLocked(false);
                chatPanel.SetSendInteractable(activeNpc != null);
            }

            yield break;
        }

        if (!chatResponse.success)
        {
            string backendError = string.IsNullOrEmpty(chatResponse.error) ? "CHAT_REQUEST_FAILED" : chatResponse.error;
            sessionContext.SetError(backendError);

            if (chatPanel != null)
            {
                chatPanel.AppendSystemMessage("Server error: " + backendError);
                chatPanel.SetInputLocked(false);
                chatPanel.SetSendInteractable(activeNpc != null);
            }

            UpdateState(ChatState.Idle);

            if (statusView != null)
            {
                statusView.SetReady();
            }

            yield break;
        }

        if (!string.IsNullOrWhiteSpace(chatResponse.reply) && ShouldRegisterFishingRumor(isFishingStoryQuery, isFishingRumorMode, requestUserText, chatResponse.reply))
        {
            RegisterFishingRumorFromReply(rumorService, storyNpcId, chatResponse);
        }

        if (!string.IsNullOrWhiteSpace(failedVerificationReplyPrefix) && !string.IsNullOrWhiteSpace(chatResponse.reply))
        {
            chatResponse.reply = failedVerificationReplyPrefix + "\n" + chatResponse.reply;
        }

        ApplySuccessfulResponse(chatResponse);
    }

    private bool ShouldRegisterFishingRumor(bool isFishingStoryQuery, bool isFishingRumorMode, string userText, string modelReply)
    {
        if (isFishingStoryQuery)
        {
            return true;
        }

        string normalizedUserText = string.IsNullOrWhiteSpace(userText) ? string.Empty : userText.Trim().ToLowerInvariant();
        if (isFishingRumorMode && ContainsAny(normalizedUserText, "渔闻", "鱼闻", "见闻", "大鱼", "湖边发生"))
        {
            return true;
        }

        string normalizedReply = string.IsNullOrWhiteSpace(modelReply) ? string.Empty : modelReply.Trim();
        return normalizedReply.Contains("storyText") && normalizedReply.Contains("lakeId") && normalizedReply.Contains("targetFishId");
    }

    private void RegisterFishingRumorFromReply(FishingRumorService rumorService, string npcId, ChatResponse chatResponse)
    {
        if (rumorService == null || chatResponse == null || string.IsNullOrWhiteSpace(chatResponse.reply))
        {
            return;
        }

        string resolvedNpcId = string.IsNullOrWhiteSpace(npcId) ? ResolveCharacterIdForSession(config == null ? "npc_001" : config.characterId) : npcId;
        FishingRumorRecord rumor;
        if (rumorService.TryRegisterRumorFromModel(resolvedNpcId, chatResponse.reply, out rumor) && rumor != null)
        {
            chatResponse.reply = rumor.storyText + "（如要采信，告诉我：我要去验证这条消息）";
            return;
        }

        string fallbackBoardText = BuildFallbackRumorBoardText(chatResponse.reply);
        if (!string.IsNullOrWhiteSpace(fallbackBoardText))
        {
            FishingEventBoard.PostRumor("【渔闻】" + fallbackBoardText);
        }
    }

    private string BuildFallbackRumorBoardText(string modelReply)
    {
        if (string.IsNullOrWhiteSpace(modelReply))
        {
            return string.Empty;
        }

        string text = modelReply.Trim();
        int jsonEnd = text.LastIndexOf('}');
        if (jsonEnd >= 0 && jsonEnd + 1 < text.Length)
        {
            string tail = text.Substring(jsonEnd + 1).Trim();
            if (!string.IsNullOrWhiteSpace(tail))
            {
                return tail;
            }
        }

        return text;
    }

    private void HandleQuickOptionRequested(string optionPayload)
    {
        if (string.IsNullOrWhiteSpace(optionPayload))
        {
            return;
        }

        if (CurrentState == ChatState.Sending)
        {
            return;
        }

        if (activeNpc == null)
        {
            if (chatPanel != null)
            {
                chatPanel.AppendSystemMessage("请先面对 NPC 按 E 开始对话。");
            }

            return;
        }

        FishingStoryService localStoryService = FishingStoryService.Instance;
        if (localStoryService != null && localStoryService.IsStoryQuery(optionPayload))
        {
            StartCoroutine(SendRoutine(optionPayload));
            return;
        }

        ApplyNpcBindings();

        ChatResponse ruleResponse;
        bool handledByRule = activeNpc.TryBuildRuleResponse(optionPayload, out ruleResponse);
        if (!handledByRule)
        {
            StartCoroutine(SendRoutine(optionPayload));
            return;
        }

        StartCoroutine(HandleRuleResponseRoutine(optionPayload, ruleResponse));
    }

    private IEnumerator HandleRuleResponseRoutine(string userText, ChatResponse ruleResponse)
    {
        UpdateState(ChatState.Sending);

        if (statusView != null)
        {
            statusView.SetSending();
        }

        if (chatPanel != null)
        {
            chatPanel.SetInputLocked(true);
            chatPanel.AppendPlayerMessage(userText);
        }

        PushConversationTurn("player", userText);

        yield return null;

        if (ruleResponse == null || !ruleResponse.success)
        {
            if (sessionContext != null)
            {
                sessionContext.SetError("RULE_RESPONSE_INVALID");
            }
            UpdateState(ChatState.Idle);

            if (chatPanel != null)
            {
                chatPanel.AppendSystemMessage("Rule response failed.");
                chatPanel.SetInputLocked(false);
                chatPanel.SetSendInteractable(activeNpc != null);
            }

            if (statusView != null)
            {
                statusView.SetError("RULE_RESPONSE_INVALID");
            }

            yield break;
        }

        ApplySuccessfulResponse(ruleResponse);
    }

    private void ApplySuccessfulResponse(ChatResponse chatResponse)
    {
        if (chatResponse == null)
        {
            return;
        }

        string replyText = string.IsNullOrEmpty(chatResponse.reply) ? "(empty reply)" : chatResponse.reply;
        if (sessionContext != null)
        {
            sessionContext.SetLastReply(replyText);
            sessionContext.ClearError();
        }

        PushConversationTurn("assistant", replyText);

        if (chatPanel != null)
        {
            chatPanel.AppendAssistantMessage(replyText);
            chatPanel.SetInputLocked(false);
            chatPanel.SetSendInteractable(activeNpc != null);
            chatPanel.SetEmotionTag(chatResponse.emotion);
        }

        if (expressionController != null)
        {
            expressionController.ApplyEmotion(chatResponse.emotion);
        }

        if (animationController != null)
        {
            animationController.ApplyAnimation(chatResponse.animation);
        }

        UpdateState(ChatState.Idle);

        if (statusView != null)
        {
            statusView.SetReady();
        }
    }

    private void EnsureContext()
    {
        string baseSessionId = config == null ? "user_001" : config.sessionId;
        string defaultCharacterId = config == null ? "npc_001" : config.characterId;
        string characterId = ResolveCharacterIdForSession(defaultCharacterId);
        string sessionId = ResolveSessionIdForNpc(baseSessionId, characterId);

        if (sessionContext == null)
        {
            sessionContext = new ChatSessionContext(sessionId, characterId);
            return;
        }

        sessionContext.UpdateIdentity(sessionId, characterId);
    }

    private string ResolveCharacterIdForSession(string defaultCharacterId)
    {
        if (activeNpc == null)
        {
            return defaultCharacterId;
        }

        return activeNpc.GetResolvedCharacterId(defaultCharacterId);
    }

    private void ApplyNpcBindings()
    {
        CharacterExpressionController resolvedExpression = defaultExpressionController;
        CharacterAnimationController resolvedAnimation = defaultAnimationController;
        string defaultCharacterId = config == null ? "npc_001" : config.characterId;

        if (activeNpc != null)
        {
            if (activeNpc.ExpressionController != null)
            {
                resolvedExpression = activeNpc.ExpressionController;
            }

            if (activeNpc.AnimationController != null)
            {
                resolvedAnimation = activeNpc.AnimationController;
            }

            defaultCharacterId = activeNpc.GetResolvedCharacterId(defaultCharacterId);
        }

        expressionController = resolvedExpression;
        animationController = resolvedAnimation;

        if (sessionContext != null)
        {
            string baseSessionId = config == null ? "user_001" : config.sessionId;
            string resolvedSessionId = ResolveSessionIdForNpc(baseSessionId, defaultCharacterId);
            sessionContext.UpdateIdentity(resolvedSessionId, defaultCharacterId);
        }
    }

    private string ResolveSessionIdForNpc(string baseSessionId, string characterId)
    {
        string resolvedBase = string.IsNullOrWhiteSpace(baseSessionId) ? "user_001" : baseSessionId.Trim();
        string resolvedCharacter = string.IsNullOrWhiteSpace(characterId) ? "npc_001" : characterId.Trim();
        return resolvedBase + "::" + resolvedCharacter;
    }

    private void TrySubscribePanelEvents()
    {
        if (chatPanel == null)
        {
            return;
        }

        if (subscribedPanel == chatPanel)
        {
            return;
        }

        if (subscribedPanel != null)
        {
            subscribedPanel.SendRequested -= HandleSendRequested;
            subscribedPanel.QuickOptionRequested -= HandleQuickOptionRequested;
        }

        chatPanel.SendRequested += HandleSendRequested;
        chatPanel.QuickOptionRequested += HandleQuickOptionRequested;
        subscribedPanel = chatPanel;
    }

    private void UnsubscribePanelEvents()
    {
        if (subscribedPanel == null)
        {
            return;
        }

        subscribedPanel.SendRequested -= HandleSendRequested;
        subscribedPanel.QuickOptionRequested -= HandleQuickOptionRequested;
        subscribedPanel = null;
    }

    private void UpdateState(ChatState nextState)
    {
        if (sessionContext == null)
        {
            EnsureContext();
        }

        if (sessionContext.State == nextState)
        {
            return;
        }

        sessionContext.SetState(nextState);
        StateChanged?.Invoke(nextState);
    }

    private void SetErrorState(string errorCode)
    {
        if (sessionContext == null)
        {
            EnsureContext();
        }

        sessionContext.SetError(errorCode);
        UpdateState(ChatState.Error);

        if (statusView != null)
        {
            statusView.SetError(errorCode);
        }

        if (chatPanel != null)
        {
            chatPanel.SetInputLocked(false);
            chatPanel.SetSendInteractable(activeNpc != null);
        }
    }

    private string ResolveActiveNpcChatMode()
    {
        if (activeNpc == null)
        {
            return "default_chat";
        }

        return activeNpc.GetChatSubMode("default_chat");
    }

    private void PopulateNpcRoutingFields(ChatRequest request, string userText, bool includeUserQuery, string chatSubMode)
    {
        if (request == null || activeNpc == null)
        {
            return;
        }

        request.npc_name = activeNpc.GetDisplayName();
        request.npc_role = activeNpc.GetRoleTitle();
        request.language = "zh-CN";
        request.user_intent_hint = DetectUserIntentHint(userText, chatSubMode);

        ChatNpcContext npcContext = activeNpc.BuildPromptContext();
        if (npcContext != null)
        {
            npcContext.recent_turns = BuildRecentTurnsSnapshot();
        }
        request.npc_context = npcContext;

        if (includeUserQuery)
        {
            request.user_query = userText;
        }
    }

    private void AttachFishingRumorContext(ChatRequest request, string userText)
    {
        if (request == null)
        {
            return;
        }

        request.user_query = userText;

        FishingDialogueService dialogueService = FishingDialogueService.Instance;
        if (dialogueService == null)
        {
            request.known_lakes = new List<ChatKnownLake>();
            request.known_fishes = new List<ChatKnownFish>();
            return;
        }

        request.known_lakes = dialogueService.BuildKnownLakes();
        request.known_fishes = dialogueService.BuildKnownFishes();
    }

    private string BuildConstrainedUserPrompt(ChatRequest request, string userText)
    {
        if (request == null || string.IsNullOrWhiteSpace(userText))
        {
            return userText;
        }

        if (request.npc_context == null)
        {
            return userText.Trim();
        }

        StringBuilder builder = new StringBuilder(320);
        builder.AppendLine("你正在扮演游戏中的NPC进行中文对话。");
        builder.AppendLine("回答约束:");
        builder.AppendLine("1) 2-4句，总字数尽量不超过80字");
        builder.AppendLine("2) 先直接回答问题，再补一句可执行建议");
        builder.AppendLine("3) 不知道就明确说不知道，不要编造");
        builder.AppendLine("4) 保持NPC身份和地区背景，不要跳戏");

        ChatNpcContext npcContext = request.npc_context;
        builder.Append("NPC名称: ").AppendLine(npcContext.display_name);
        builder.Append("NPC身份: ").AppendLine(npcContext.role);
        builder.Append("NPC区域: ").AppendLine(npcContext.region);

        if (!string.IsNullOrWhiteSpace(npcContext.persona_summary))
        {
            builder.Append("人设摘要: ").AppendLine(npcContext.persona_summary);
        }

        if (!string.IsNullOrWhiteSpace(request.user_intent_hint))
        {
            builder.Append("用户意图: ").AppendLine(request.user_intent_hint);
        }

        builder.Append("用户问题: ").Append(userText.Trim());
        return builder.ToString();
    }

    private string BuildCompactNpcPrompt(ChatRequest request, string userText)
    {
        if (request == null || request.npc_context == null)
        {
            return string.IsNullOrWhiteSpace(userText) ? string.Empty : userText.Trim();
        }

        ChatNpcContext npcContext = request.npc_context;
        return $"你是{npcContext.display_name}，身份是{npcContext.role}，地点是{npcContext.region}。用中文1-2句先回答问题，再给1条实用建议。用户问题：{userText.Trim()}";
    }

    private void PushConversationTurn(string speaker, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        string normalizedSpeaker = string.IsNullOrWhiteSpace(speaker) ? "unknown" : speaker.Trim();
        string normalizedText = text.Trim();
        ChatConversationTurn turn = new ChatConversationTurn(normalizedSpeaker, normalizedText);
        recentTurns.Add(turn);
        TrimConversationTurns(recentTurns);

        string historyKey = ResolveActiveNpcHistoryKey();
        if (!string.IsNullOrWhiteSpace(historyKey))
        {
            List<ChatConversationTurn> history;
            if (!conversationHistoryByNpcId.TryGetValue(historyKey, out history))
            {
                history = new List<ChatConversationTurn>();
                conversationHistoryByNpcId.Add(historyKey, history);
            }

            history.Add(new ChatConversationTurn(turn.speaker, turn.text));
            TrimConversationTurns(history);
        }
    }

    private void LoadConversationHistoryForActiveNpc()
    {
        string historyKey = ResolveActiveNpcHistoryKey();
        if (string.IsNullOrWhiteSpace(historyKey))
        {
            return;
        }

        List<ChatConversationTurn> history;
        if (!conversationHistoryByNpcId.TryGetValue(historyKey, out history))
        {
            return;
        }

        for (int i = 0; i < history.Count; i++)
        {
            ChatConversationTurn turn = history[i];
            if (turn != null)
            {
                recentTurns.Add(new ChatConversationTurn(turn.speaker, turn.text));
            }
        }

        TrimConversationTurns(recentTurns);
    }

    private void RestoreConversationHistoryToPanel()
    {
        if (chatPanel == null)
        {
            return;
        }

        for (int i = 0; i < recentTurns.Count; i++)
        {
            ChatConversationTurn turn = recentTurns[i];
            if (turn == null || string.IsNullOrWhiteSpace(turn.text))
            {
                continue;
            }

            if (string.Equals(turn.speaker, "player", StringComparison.OrdinalIgnoreCase))
            {
                chatPanel.AppendPlayerMessage(turn.text);
            }
            else if (string.Equals(turn.speaker, "assistant", StringComparison.OrdinalIgnoreCase))
            {
                chatPanel.AppendAssistantMessage(turn.text);
            }
            else
            {
                chatPanel.AppendSystemMessage(turn.text);
            }
        }
    }

    private void TrimConversationTurns(List<ChatConversationTurn> turns)
    {
        if (turns == null)
        {
            return;
        }

        while (turns.Count > MaxRecentTurns)
        {
            turns.RemoveAt(0);
        }
    }

    private string ResolveActiveNpcHistoryKey()
    {
        if (activeNpc == null)
        {
            return string.Empty;
        }

        string defaultCharacterId = config == null ? "npc_001" : config.characterId;
        return activeNpc.GetResolvedCharacterId(defaultCharacterId);
    }

    private List<ChatConversationTurn> BuildRecentTurnsSnapshot()
    {
        List<ChatConversationTurn> snapshot = new List<ChatConversationTurn>(recentTurns.Count);
        for (int i = 0; i < recentTurns.Count; i++)
        {
            ChatConversationTurn turn = recentTurns[i];
            if (turn == null)
            {
                continue;
            }

            snapshot.Add(new ChatConversationTurn(turn.speaker, turn.text));
        }

        return snapshot;
    }

    private bool ShouldHandleFishingRumorVerificationLocally(string userText)
    {
        string normalized = string.IsNullOrWhiteSpace(userText) ? string.Empty : userText.Trim().ToLowerInvariant();
        return ContainsAny(normalized, "验证这条消息", "验证渔闻", "我要去验证", "采信这条消息", "采信", "去验证");
    }

    private bool ShouldHandleFailedFishingRumorFeedbackLocally(string userText)
    {
        string normalized = string.IsNullOrWhiteSpace(userText) ? string.Empty : userText.Trim().ToLowerInvariant();
        return ContainsAny(normalized,
            "验证失败",
            "验真失败",
            "验证没成功",
            "渔闻不准",
            "这条渔闻不准",
            "渔闻不对",
            "这条渔闻不对",
            "消息不准",
            "这条消息不准",
            "消息不对",
            "这条消息不对",
            "线索不准",
            "线索不对",
            "没有钓到",
            "没钓到",
            "没验证出来");
    }

    private bool ShouldHandleTrustedFishTradeLocally(string userText)
    {
        string normalized = string.IsNullOrWhiteSpace(userText) ? string.Empty : userText.Trim().ToLowerInvariant();
        return ContainsAny(normalized, "卖鱼", "出售鱼", "交易", "收鱼", "好价钱");
    }

    private ChatResponse BuildTrustedFishTradeResponse(FishingRumorService rumorService)
    {
        string defaultCharacterId = config == null ? "npc_001" : config.characterId;
        string npcId = ResolveCharacterIdForSession(defaultCharacterId);

        if (rumorService == null)
        {
            return BuildLocalChatResponse("渔闻验证系统还没启动，暂时不能开信任交易。", "neutral", "talk");
        }

        if (!rumorService.HasTrustedNpc(npcId))
        {
            return BuildLocalChatResponse(rumorService.BuildTrustedTradeReply(npcId), "neutral", "talk");
        }

        if (!MerchantShopInteractable.TryOpenShopByMerchantId("merchant_dock_01"))
        {
            return BuildLocalChatResponse("信任交易已经解锁，但交易摊还没准备好。", "neutral", "talk");
        }

        return BuildLocalChatResponse(rumorService.BuildTrustedTradeReply(npcId), "happy", "talk");
    }

    private ChatResponse BuildLocalChatResponse(string reply, string emotion, string animation)
    {
        return new ChatResponse
        {
            success = true,
            reply = reply,
            emotion = emotion,
            animation = animation,
            error = string.Empty
        };
    }

    private bool ShouldUseFishingRumorMode(string userText, string chatSubMode)
    {
        if (!string.Equals(chatSubMode, "fishing_rumor", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string normalized = string.IsNullOrWhiteSpace(userText) ? string.Empty : userText.Trim().ToLowerInvariant();
        return ContainsAny(normalized, "渔闻", "鱼闻", "传闻", "听说", "消息", "大鱼", "湖边发生", "验证这条消息");
    }

    private string DetectUserIntentHint(string userText, string chatSubMode)
    {
        string normalized = string.IsNullOrWhiteSpace(userText) ? string.Empty : userText.Trim().ToLowerInvariant();
        if (normalized.Length == 0)
        {
            return "generic_dialogue";
        }

        if (ContainsAny(normalized, "介绍", "你是谁", "你是干什么", "你自己"))
        {
            return "ask_identity";
        }

        if (ContainsAny(normalized, "小镇", "历史", "发生了什么", "最近怎么了"))
        {
            return "ask_local_news";
        }

        if (ContainsAny(normalized, "建议", "怎么办", "怎么做", "生存"))
        {
            return "ask_advice";
        }

        if (ContainsAny(normalized, "哪里", "怎么走", "方位", "在哪"))
        {
            return "ask_direction";
        }

        if (ContainsAny(normalized, "钓鱼", "鱼价", "渔闻", "鱼闻", "上鱼", "鱼"))
        {
            return "ask_fishing";
        }

        if (string.Equals(chatSubMode, "hunting_report", StringComparison.OrdinalIgnoreCase) ||
            ContainsAny(normalized, "狩猎", "猎物", "林子", "兽群"))
        {
            return "ask_hunting";
        }

        return "generic_dialogue";
    }

    private static bool ContainsAny(string source, params string[] keywords)
    {
        if (string.IsNullOrWhiteSpace(source) || keywords == null)
        {
            return false;
        }

        for (int i = 0; i < keywords.Length; i++)
        {
            string keyword = keywords[i];
            if (!string.IsNullOrWhiteSpace(keyword) && source.Contains(keyword))
            {
                return true;
            }
        }

        return false;
    }

    private void RefreshConversationEntryPoints()
    {
        if (chatPanel == null)
        {
            return;
        }

        List<ChatQuickOption> options = null;
        if (activeNpc != null)
        {
            options = activeNpc.BuildQuickOptions();
        }

        if (options == null || options.Count == 0)
        {
            options = new List<ChatQuickOption>(DefaultNpcQuickOptions.Length);
            for (int i = 0; i < DefaultNpcQuickOptions.Length; i++)
            {
                options.Add(DefaultNpcQuickOptions[i]);
            }
        }

        chatPanel.SetQuickOptions(options);
        chatPanel.SetSendInteractable(activeNpc != null);
    }

    private void ApplyNpcHudInfo()
    {
        if (chatPanel == null)
        {
            return;
        }

        if (activeNpc == null)
        {
            chatPanel.SetNpcHeader("未选择NPC", "幸存者");
            chatPanel.SetRelationshipTag("关系: 未建立");
            chatPanel.SetEmotionTag("中立");
            chatPanel.SetNpcPortrait(null);
            return;
        }

        chatPanel.SetNpcHeader(activeNpc.GetDisplayName(), activeNpc.GetRoleTitle());
        chatPanel.SetRelationshipTag("关系: 观察中");
        chatPanel.SetEmotionTag("谨慎");
        chatPanel.SetNpcPortrait(activeNpc.GetPortraitSprite());
    }
}
