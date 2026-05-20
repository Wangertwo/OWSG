using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class ChatMessageView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI transcriptText;
    [SerializeField] private int maxMessageCount = 40;
    [SerializeField] private string playerSpeakerName = "You";
    [SerializeField] private string assistantSpeakerName = "NPC";
    [SerializeField] private string systemSpeakerName = "System";

    private readonly Queue<string> messageQueue = new Queue<string>();
    private readonly StringBuilder textBuilder = new StringBuilder();

    public void Configure(TextMeshProUGUI transcript, int maxCount = 40)
    {
        transcriptText = transcript;
        maxMessageCount = Mathf.Max(1, maxCount);
    }

    public void SetSpeakerNames(string playerName, string assistantName, string systemName = "System")
    {
        playerSpeakerName = string.IsNullOrWhiteSpace(playerName) ? "You" : playerName.Trim();
        assistantSpeakerName = string.IsNullOrWhiteSpace(assistantName) ? "NPC" : assistantName.Trim();
        systemSpeakerName = string.IsNullOrWhiteSpace(systemName) ? "System" : systemName.Trim();
    }

    public void AppendPlayerMessage(string message)
    {
        AppendMessage(playerSpeakerName, message);
    }

    public void AppendAssistantMessage(string message)
    {
        AppendMessage(assistantSpeakerName, message);
    }

    public void AppendSystemMessage(string message)
    {
        AppendMessage(systemSpeakerName, message);
    }

    public void Clear()
    {
        messageQueue.Clear();

        if (transcriptText != null)
        {
            transcriptText.text = string.Empty;
        }
    }

    private void AppendMessage(string sender, string message)
    {
        if (transcriptText == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        messageQueue.Enqueue(sender + ": " + message.Trim());

        while (messageQueue.Count > maxMessageCount)
        {
            messageQueue.Dequeue();
        }

        RebuildText();
    }

    private void RebuildText()
    {
        textBuilder.Length = 0;

        foreach (string line in messageQueue)
        {
            textBuilder.AppendLine(line);
        }

        transcriptText.text = textBuilder.ToString().TrimEnd();
    }
}
