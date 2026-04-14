using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class ChatMessageView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI transcriptText;
    [SerializeField] private int maxMessageCount = 40;

    private readonly Queue<string> messageQueue = new Queue<string>();
    private readonly StringBuilder textBuilder = new StringBuilder();

    public void AppendPlayerMessage(string message)
    {
        AppendMessage("You", message);
    }

    public void AppendAssistantMessage(string message)
    {
        AppendMessage("NPC", message);
    }

    public void AppendSystemMessage(string message)
    {
        AppendMessage("System", message);
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

        messageQueue.Enqueue("[" + sender + "] " + message.Trim());

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
