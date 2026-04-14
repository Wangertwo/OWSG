using UnityEngine;

public class ChatBootstrap : MonoBehaviour
{
    [SerializeField] private ChatConfig config;
    [SerializeField] private AiGatewayClient gatewayClient;
    [SerializeField] private ChatFlowController flowController;
    [SerializeField] private ChatPanelController chatPanel;
    [SerializeField] private ChatStatusView statusView;
    [SerializeField] private CharacterExpressionController expressionController;
    [SerializeField] private CharacterAnimationController animationController;
    [SerializeField] private bool connectOnStart = true;
    [SerializeField] private bool hidePanelOnStart = true;

    private void Reset()
    {
        gatewayClient = FindObjectOfType<AiGatewayClient>();
        flowController = FindObjectOfType<ChatFlowController>();
        chatPanel = FindObjectOfType<ChatPanelController>();
        statusView = FindObjectOfType<ChatStatusView>();
        expressionController = FindObjectOfType<CharacterExpressionController>();
        animationController = FindObjectOfType<CharacterAnimationController>();
    }

    private void Awake()
    {
        if (gatewayClient == null)
        {
            gatewayClient = FindObjectOfType<AiGatewayClient>();
        }

        if (flowController == null)
        {
            flowController = FindObjectOfType<ChatFlowController>();
        }

        if (chatPanel == null)
        {
            chatPanel = FindObjectOfType<ChatPanelController>();
        }

        if (statusView == null)
        {
            statusView = FindObjectOfType<ChatStatusView>();
        }

        if (expressionController == null)
        {
            expressionController = FindObjectOfType<CharacterExpressionController>();
        }

        if (animationController == null)
        {
            animationController = FindObjectOfType<CharacterAnimationController>();
        }

        if (gatewayClient == null || flowController == null)
        {
            Debug.LogError("ChatBootstrap is missing required references.");
            return;
        }

        if (config == null)
        {
            Debug.LogWarning("ChatBootstrap has no ChatConfig assigned.");
        }

        gatewayClient.SetConfig(config);
        flowController.SetConnectOnStart(connectOnStart);
        flowController.Configure(
            config,
            gatewayClient,
            chatPanel,
            statusView,
            expressionController,
            animationController);

        if (hidePanelOnStart && chatPanel != null)
        {
            chatPanel.HidePanel();
        }
    }

    private void Start()
    {
        if (flowController != null)
        {
            flowController.Bootstrap();
        }
    }
}
