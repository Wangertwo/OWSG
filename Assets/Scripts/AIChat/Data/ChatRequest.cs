using System;
using System.Collections.Generic;

[Serializable]
public class ChatKnownLake
{
    public string lakeId;
    public string name;

    public ChatKnownLake(string lakeIdValue, string nameValue)
    {
        lakeId = lakeIdValue;
        name = nameValue;
    }
}

[Serializable]
public class ChatKnownFish
{
    public string fishId;
    public string name;

    public ChatKnownFish(string fishIdValue, string nameValue)
    {
        fishId = fishIdValue;
        name = nameValue;
    }
}

[Serializable]
public class ChatConversationTurn
{
    public string speaker;
    public string text;

    public ChatConversationTurn(string speakerValue, string textValue)
    {
        speaker = speakerValue;
        text = textValue;
    }
}

[Serializable]
public class ChatNpcContext
{
    public string npc_id;
    public string display_name;
    public string role;
    public string region;
    public string persona_summary;
    public string world_knowledge;
    public string speaking_style;
    public string response_rules;
    public List<string> core_facts;
    public List<string> do_not_claim;
    public List<ChatConversationTurn> recent_turns;
}

[Serializable]
public class ChatRequest
{
    public string session_id;
    public string character_id;
    public string mode;
    public string chat_mode;
    public string user_text;

    public string npc_name;
    public string npc_role;
    public string user_query;
    public string user_intent_hint;
    public string language;
    public List<ChatKnownLake> known_lakes;
    public List<ChatKnownFish> known_fishes;
    public ChatNpcContext npc_context;

    public ChatRequest(string sessionId, string characterId, string userText)
        : this(sessionId, characterId, "chat", "default_chat", userText)
    {
    }

    public ChatRequest(string sessionId, string characterId, string modeValue, string chatModeValue, string userText)
    {
        session_id = sessionId;
        character_id = characterId;
        mode = string.IsNullOrWhiteSpace(modeValue) ? "chat" : modeValue.Trim();
        chat_mode = string.IsNullOrWhiteSpace(chatModeValue) ? "default_chat" : chatModeValue.Trim();
        user_text = userText;
        npc_name = string.Empty;
        npc_role = string.Empty;
        user_query = string.Empty;
        user_intent_hint = string.Empty;
        language = string.Empty;
        known_lakes = null;
        known_fishes = null;
        npc_context = null;
    }
}
