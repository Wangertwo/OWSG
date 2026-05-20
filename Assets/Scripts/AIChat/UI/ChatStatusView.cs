using TMPro;
using UnityEngine;

public class ChatStatusView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject busyIndicator;

    public void Configure(TextMeshProUGUI text, GameObject indicator = null)
    {
        statusText = text;
        busyIndicator = indicator;
    }

    public void SetDisconnected()
    {
        SetStatus("Disconnected", false);
    }

    public void SetConnecting()
    {
        SetStatus("Connecting...", true);
    }

    public void SetConnected(string serviceName, string version)
    {
        string text = "Connected";

        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            text += ": " + serviceName;
        }

        if (!string.IsNullOrWhiteSpace(version))
        {
            text += " v" + version;
        }

        SetStatus(text, false);
    }

    public void SetReady()
    {
        SetStatus("Ready", false);
    }

    public void SetSending()
    {
        SetStatus("Sending...", true);
    }

    public void SetError(string errorCode)
    {
        string text = "Error";

        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            text += ": " + errorCode;
        }

        SetStatus(text, false);
    }

    private void SetStatus(string text, bool isBusy)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }

        if (busyIndicator != null)
        {
            busyIndicator.SetActive(isBusy);
        }
    }
}
