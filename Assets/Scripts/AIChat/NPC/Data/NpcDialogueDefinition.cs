using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NpcDialogueDefinition
{
    [Header("Identity")]
    public string npcId = "npc_default";
    public string characterId = "npc_default";
    public string displayName = "Unnamed NPC";
    public string roleTitle = "Villager";
    public string regionId = "unknown_region";
    public string chatSubMode = "default_chat";

    [Header("Prompt Persona")]
    [TextArea(2, 4)]
    public string personaSummary;
    [TextArea(2, 5)]
    public string worldKnowledge;
    [TextArea(2, 4)]
    public string speakingStyle;
    [TextArea(2, 4)]
    public string responseRules;
    [TextArea(2, 4)]
    public List<string> coreFacts = new List<string>();
    [TextArea(2, 4)]
    public List<string> doNotClaim = new List<string>();

    [Header("Capabilities")]
    public bool supportsGreeting = true;
    public bool supportsIdentity = true;
    public bool supportsHistory = true;
    public bool supportsDirection = true;
    public bool supportsSurvivalAdvice = true;
    public bool supportsFishingLakeGuide = true;
    public bool supportsFishingTips = true;
    public bool supportsFishPrice = true;
    public bool supportsFishingRumor = true;
    public bool supportsRumorVerification = true;
    public bool supportsFishingQuest = true;
    public bool supportsFishingQuestSubmit = true;

    [Header("Greeting")]
    [TextArea(2, 4)]
    public string firstMeetingGreeting;
    [TextArea(2, 4)]
    public List<string> greetingReplies = new List<string>();

    [Header("Identity Replies")]
    [TextArea(2, 4)]
    public List<string> identityReplies = new List<string>();

    [Header("History Replies")]
    [TextArea(2, 4)]
    public List<string> historyReplies = new List<string>();

    [Header("Direction")]
    [TextArea(2, 4)]
    public string genericDirectionReply;
    [TextArea(2, 4)]
    public string unknownLandmarkReply;
    public List<NpcLandmarkDirection> landmarkDirections = new List<NpcLandmarkDirection>();

    [Header("Survival Advice")]
    [TextArea(2, 4)]
    public List<string> survivalAdviceReplies = new List<string>();

    [Header("Fallback")]
    [TextArea(2, 4)]
    public List<string> fallbackReplies = new List<string>();

    [Header("Emotion And Animation")]
    public string defaultEmotion = "neutral";
    public string defaultAnimation = "talk";
    public string greetingEmotion = "happy";
    public string greetingAnimation = "greet";

    public bool IsCapabilityEnabled(NpcIntentType intentType)
    {
        switch (intentType)
        {
            case NpcIntentType.Greeting:
                return supportsGreeting;
            case NpcIntentType.AskIdentity:
                return supportsIdentity;
            case NpcIntentType.AskHistory:
                return supportsHistory;
            case NpcIntentType.AskDirection:
                return supportsDirection;
            case NpcIntentType.AskSurvivalAdvice:
                return supportsSurvivalAdvice;
            case NpcIntentType.AskFishingLakeGuide:
                return supportsFishingLakeGuide;
            case NpcIntentType.AskFishingTips:
                return supportsFishingTips;
            case NpcIntentType.AskFishPrice:
                return supportsFishPrice;
            case NpcIntentType.AskFishingStory:
                return supportsFishingRumor;
            case NpcIntentType.AcceptFishingRumorVerification:
                return supportsRumorVerification;
            case NpcIntentType.ReportFishingRumorVerification:
                return supportsRumorVerification;
            case NpcIntentType.AskFishingQuest:
                return supportsFishingQuest;
            case NpcIntentType.SubmitFishingQuest:
                return supportsFishingQuestSubmit;
            default:
                return false;
        }
    }

    public string ResolveCharacterId(string fallbackCharacterId)
    {
        if (!string.IsNullOrWhiteSpace(characterId))
        {
            return characterId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(fallbackCharacterId))
        {
            return fallbackCharacterId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(npcId))
        {
            return npcId.Trim();
        }

        return "npc_default";
    }

    public string ResolveChatSubMode(string fallbackMode = "default_chat")
    {
        if (!string.IsNullOrWhiteSpace(chatSubMode))
        {
            return chatSubMode.Trim();
        }

        return string.IsNullOrWhiteSpace(fallbackMode) ? "default_chat" : fallbackMode.Trim();
    }

    public ChatNpcContext BuildNpcContext()
    {
        ChatNpcContext context = new ChatNpcContext();
        context.npc_id = string.IsNullOrWhiteSpace(npcId) ? "npc_default" : npcId.Trim();
        context.display_name = string.IsNullOrWhiteSpace(displayName) ? "Unnamed NPC" : displayName.Trim();
        context.role = string.IsNullOrWhiteSpace(roleTitle) ? "Villager" : roleTitle.Trim();
        context.region = string.IsNullOrWhiteSpace(regionId) ? "unknown_region" : regionId.Trim();
        context.persona_summary = string.IsNullOrWhiteSpace(personaSummary) ? string.Empty : personaSummary.Trim();
        context.world_knowledge = string.IsNullOrWhiteSpace(worldKnowledge) ? string.Empty : worldKnowledge.Trim();
        context.speaking_style = string.IsNullOrWhiteSpace(speakingStyle) ? string.Empty : speakingStyle.Trim();
        context.response_rules = string.IsNullOrWhiteSpace(responseRules) ? string.Empty : responseRules.Trim();
        context.core_facts = CloneList(coreFacts);
        context.do_not_claim = CloneList(doNotClaim);
        context.recent_turns = null;
        return context;
    }

    public bool HasAnyReplyContent()
    {
        return !string.IsNullOrWhiteSpace(firstMeetingGreeting)
            || HasAnyText(greetingReplies)
            || HasAnyText(identityReplies)
            || HasAnyText(historyReplies)
            || HasAnyText(survivalAdviceReplies)
            || HasAnyText(fallbackReplies)
            || !string.IsNullOrWhiteSpace(genericDirectionReply)
            || !string.IsNullOrWhiteSpace(unknownLandmarkReply)
            || (landmarkDirections != null && landmarkDirections.Count > 0);
    }

    public bool HasPromptPersonaContent()
    {
        return !string.IsNullOrWhiteSpace(personaSummary)
            || !string.IsNullOrWhiteSpace(worldKnowledge)
            || !string.IsNullOrWhiteSpace(speakingStyle)
            || !string.IsNullOrWhiteSpace(responseRules)
            || HasAnyText(coreFacts)
            || HasAnyText(doNotClaim);
    }

    public NpcDialogueDefinition Clone()
    {
        NpcDialogueDefinition clone = new NpcDialogueDefinition();

        clone.npcId = npcId;
        clone.characterId = characterId;
        clone.displayName = displayName;
        clone.roleTitle = roleTitle;
        clone.regionId = regionId;
        clone.chatSubMode = chatSubMode;
        clone.personaSummary = personaSummary;
        clone.worldKnowledge = worldKnowledge;
        clone.speakingStyle = speakingStyle;
        clone.responseRules = responseRules;
        clone.coreFacts = CloneList(coreFacts);
        clone.doNotClaim = CloneList(doNotClaim);

        clone.supportsGreeting = supportsGreeting;
        clone.supportsIdentity = supportsIdentity;
        clone.supportsHistory = supportsHistory;
        clone.supportsDirection = supportsDirection;
        clone.supportsSurvivalAdvice = supportsSurvivalAdvice;
        clone.supportsFishingLakeGuide = supportsFishingLakeGuide;
        clone.supportsFishingTips = supportsFishingTips;
        clone.supportsFishPrice = supportsFishPrice;
        clone.supportsFishingRumor = supportsFishingRumor;
        clone.supportsRumorVerification = supportsRumorVerification;
        clone.supportsFishingQuest = supportsFishingQuest;
        clone.supportsFishingQuestSubmit = supportsFishingQuestSubmit;

        clone.firstMeetingGreeting = firstMeetingGreeting;
        clone.greetingReplies = CloneList(greetingReplies);
        clone.identityReplies = CloneList(identityReplies);
        clone.historyReplies = CloneList(historyReplies);
        clone.survivalAdviceReplies = CloneList(survivalAdviceReplies);
        clone.fallbackReplies = CloneList(fallbackReplies);

        clone.genericDirectionReply = genericDirectionReply;
        clone.unknownLandmarkReply = unknownLandmarkReply;
        clone.landmarkDirections = CloneLandmarks(landmarkDirections);

        clone.defaultEmotion = defaultEmotion;
        clone.defaultAnimation = defaultAnimation;
        clone.greetingEmotion = greetingEmotion;
        clone.greetingAnimation = greetingAnimation;

        return clone;
    }

    private static bool HasAnyText(List<string> source)
    {
        if (source == null)
        {
            return false;
        }

        for (int i = 0; i < source.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(source[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static List<string> CloneList(List<string> source)
    {
        return source == null ? new List<string>() : new List<string>(source);
    }

    private static List<NpcLandmarkDirection> CloneLandmarks(List<NpcLandmarkDirection> source)
    {
        List<NpcLandmarkDirection> clone = new List<NpcLandmarkDirection>();
        if (source == null)
        {
            return clone;
        }

        for (int i = 0; i < source.Count; i++)
        {
            NpcLandmarkDirection landmark = source[i];
            if (landmark != null)
            {
                clone.Add(landmark.Clone());
            }
        }

        return clone;
    }
}
