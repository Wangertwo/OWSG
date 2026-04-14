public class ChatSessionContext
{
    public string SessionId { get; private set; }
    public string CharacterId { get; private set; }
    public ChatState State { get; private set; }
    public string LastReply { get; private set; }
    public string LastError { get; private set; }

    public ChatSessionContext(string sessionId, string characterId)
    {
        SessionId = sessionId;
        CharacterId = characterId;
        State = ChatState.Disconnected;
        LastReply = string.Empty;
        LastError = string.Empty;
    }

    public void UpdateIdentity(string sessionId, string characterId)
    {
        SessionId = sessionId;
        CharacterId = characterId;
    }

    public ChatRequest BuildRequest(string userText)
    {
        return new ChatRequest(SessionId, CharacterId, userText);
    }

    public void SetState(ChatState nextState)
    {
        State = nextState;
    }

    public void SetLastReply(string reply)
    {
        LastReply = reply;
    }

    public void SetError(string errorCode)
    {
        LastError = errorCode;
    }

    public void ClearError()
    {
        LastError = string.Empty;
    }
}
