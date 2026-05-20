using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AiGatewayClient : MonoBehaviour
{
    [SerializeField] private ChatConfig config;

    public void SetConfig(ChatConfig chatConfig)
    {
        config = chatConfig;
    }

    public IEnumerator CheckHealth(Action<HealthResponse, string> onComplete)
    {
        if (config == null)
        {
            onComplete?.Invoke(null, "CHAT_CONFIG_MISSING");
            yield break;
        }

        string url = config.GetHealthUrl();
        if (string.IsNullOrEmpty(url))
        {
            onComplete?.Invoke(null, "HEALTH_URL_INVALID");
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = config.requestTimeoutSeconds;

            UnityWebRequestAsyncOperation operation;
            try
            {
                operation = request.SendWebRequest();
            }
            catch (Exception ex)
            {
                onComplete?.Invoke(null, "REQUEST_EXCEPTION: " + ex.GetType().Name);
                yield break;
            }

            yield return operation;

            if (IsRequestFailed(request))
            {
                onComplete?.Invoke(null, BuildRequestError(request));
                yield break;
            }

            HealthResponse response = ParseJson<HealthResponse>(request.downloadHandler.text);
            if (response == null)
            {
                onComplete?.Invoke(null, "HEALTH_JSON_INVALID");
                yield break;
            }

            onComplete?.Invoke(response, null);
        }
    }

    public IEnumerator SendChat(ChatRequest chatRequest, Action<ChatResponse, string> onComplete)
    {
        if (config == null)
        {
            onComplete?.Invoke(null, "CHAT_CONFIG_MISSING");
            yield break;
        }

        if (chatRequest == null)
        {
            onComplete?.Invoke(null, "CHAT_REQUEST_EMPTY");
            yield break;
        }

        string url = config.GetChatUrl();
        if (string.IsNullOrEmpty(url))
        {
            onComplete?.Invoke(null, "CHAT_URL_INVALID");
            yield break;
        }

        string payload = JsonUtility.ToJson(chatRequest);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

        using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(payloadBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = config.requestTimeoutSeconds;

            UnityWebRequestAsyncOperation operation;
            try
            {
                operation = request.SendWebRequest();
            }
            catch (Exception ex)
            {
                onComplete?.Invoke(null, "REQUEST_EXCEPTION: " + ex.GetType().Name);
                yield break;
            }

            yield return operation;

            if (IsRequestFailed(request))
            {
                onComplete?.Invoke(null, BuildRequestError(request));
                yield break;
            }

            ChatResponse response = ParseJson<ChatResponse>(request.downloadHandler.text);
            if (response == null)
            {
                onComplete?.Invoke(null, "CHAT_JSON_INVALID");
                yield break;
            }

            onComplete?.Invoke(response, null);
        }
    }

    private bool IsRequestFailed(UnityWebRequest request)
    {
        return request.result == UnityWebRequest.Result.ConnectionError
            || request.result == UnityWebRequest.Result.ProtocolError
            || request.result == UnityWebRequest.Result.DataProcessingError;
    }

    private string BuildRequestError(UnityWebRequest request)
    {
        if (request == null)
        {
            return "REQUEST_NULL";
        }

        if (request.result == UnityWebRequest.Result.ConnectionError)
        {
            if (!string.IsNullOrEmpty(request.error))
            {
                string normalizedError = request.error.ToLowerInvariant();
                if (normalizedError.Contains("timed out") ||
                    normalizedError.Contains("timeout"))
                {
                    return "NETWORK_TIMEOUT";
                }

                return "NETWORK_ERROR: " + request.error;
            }

            return "NETWORK_ERROR";
        }

        if (request.responseCode > 0)
        {
            return "HTTP_" + request.responseCode;
        }

        if (!string.IsNullOrEmpty(request.error))
        {
            return request.error;
        }

        return "REQUEST_FAILED";
    }

    private T ParseJson<T>(string json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<T>(json);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
