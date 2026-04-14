using System;

[Serializable]
public class ChatRequest
{
    public string session_id;
    public string character_id;
    public string user_text;

    public ChatRequest(string sessionId, string characterId, string userText)
    {
        session_id = sessionId;
        character_id = characterId;
        user_text = userText;
    }
}
