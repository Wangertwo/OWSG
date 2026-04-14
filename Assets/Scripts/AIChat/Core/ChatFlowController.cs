using System;
using System.Collections;
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

    public event Action<ChatState> StateChanged;

    public ChatState CurrentState => sessionContext == null ? ChatState.Disconnected : sessionContext.State;
    public bool IsChatUiOpen => chatPanel != null && chatPanel.IsOpen;

    private void Awake()
    {
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

        EnsureContext();
        TrySubscribePanelEvents();
    }

    public void Bootstrap()
    {
        EnsureContext();

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
                chatPanel.SetSendInteractable(true);
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
            chatPanel.SetSendInteractable(false);
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
                chatPanel.SetSendInteractable(true);
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
                chatPanel.SetSendInteractable(true);
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
            chatPanel.SetSendInteractable(true);
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

        ChatRequest request = sessionContext.BuildRequest(userText);

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
                chatPanel.SetSendInteractable(true);
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
                chatPanel.SetSendInteractable(true);
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
            }

            UpdateState(ChatState.Idle);

            if (statusView != null)
            {
                statusView.SetReady();
            }

            yield break;
        }

        string replyText = string.IsNullOrEmpty(chatResponse.reply) ? "(empty reply)" : chatResponse.reply;
        sessionContext.SetLastReply(replyText);
        sessionContext.ClearError();

        if (chatPanel != null)
        {
            chatPanel.AppendAssistantMessage(replyText);
            chatPanel.SetInputLocked(false);
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
        string sessionId = config == null ? "user_001" : config.sessionId;
        string characterId = config == null ? "npc_001" : config.characterId;

        if (sessionContext == null)
        {
            sessionContext = new ChatSessionContext(sessionId, characterId);
            return;
        }

        sessionContext.UpdateIdentity(sessionId, characterId);
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
        }

        chatPanel.SendRequested += HandleSendRequested;
        subscribedPanel = chatPanel;
    }

    private void UnsubscribePanelEvents()
    {
        if (subscribedPanel == null)
        {
            return;
        }

        subscribedPanel.SendRequested -= HandleSendRequested;
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
            chatPanel.SetSendInteractable(true);
        }
    }
}
