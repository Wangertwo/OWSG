using UnityEngine;

[CreateAssetMenu(fileName = "ChatConfig", menuName = "AI Chat/Chat Config")]
public class ChatConfig : ScriptableObject
{
    [Header("Gateway")]
    public string gatewayBaseUrl = "http://127.0.0.1:8080";
    public string healthPath = "/health";
    public string chatPath = "/chat";
    [Min(1)]
    public int requestTimeoutSeconds = 30;

    [Header("Session")]
    public string sessionId = "user_001";
    public string characterId = "npc_001";

    public string GetHealthUrl()
    {
        return BuildUrl(gatewayBaseUrl, healthPath);
    }

    public string GetChatUrl()
    {
        return BuildUrl(gatewayBaseUrl, chatPath);
    }

    private string BuildUrl(string baseUrl, string path)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return string.Empty;
        }

        string normalizedBase = baseUrl.TrimEnd('/');
        string normalizedPath = string.IsNullOrWhiteSpace(path) ? string.Empty : path.Trim();

        if (!normalizedPath.StartsWith("/"))
        {
            normalizedPath = "/" + normalizedPath;
        }

        return normalizedBase + normalizedPath;
    }
}
