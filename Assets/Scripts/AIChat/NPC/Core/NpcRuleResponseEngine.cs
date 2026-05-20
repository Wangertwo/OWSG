using System.Collections.Generic;
using UnityEngine;

public class NpcRuleResponseEngine
{
    private readonly NpcIntentRouter intentRouter;

    public NpcRuleResponseEngine() : this(new NpcIntentRouter())
    {
    }

    public NpcRuleResponseEngine(NpcIntentRouter router)
    {
        intentRouter = router ?? new NpcIntentRouter();
    }

    public bool TryBuildResponse(
        NpcDialogueDefinition definition,
        NpcConversationState conversationState,
        string userText,
        out ChatResponse response)
    {
        response = null;

        if (definition == null || conversationState == null || string.IsNullOrWhiteSpace(userText))
        {
            return false;
        }

        NpcIntentType intentType = intentRouter.DetectIntent(userText);
        if (intentType == NpcIntentType.Unknown)
        {
            return false;
        }

        if (!definition.IsCapabilityEnabled(intentType))
        {
            return false;
        }

        string reply = BuildReply(definition, conversationState, intentType, userText);
        if (string.IsNullOrWhiteSpace(reply))
        {
            return false;
        }

        string emotion = definition.defaultEmotion;
        string animation = definition.defaultAnimation;
        if (intentType == NpcIntentType.Greeting)
        {
            emotion = string.IsNullOrWhiteSpace(definition.greetingEmotion)
                ? definition.defaultEmotion
                : definition.greetingEmotion;
            animation = string.IsNullOrWhiteSpace(definition.greetingAnimation)
                ? definition.defaultAnimation
                : definition.greetingAnimation;
        }

        conversationState.MarkIntent(intentType);

        response = new ChatResponse
        {
            success = true,
            reply = reply,
            emotion = emotion,
            animation = animation,
            error = string.Empty
        };

        return true;
    }

    private string BuildReply(
        NpcDialogueDefinition definition,
        NpcConversationState conversationState,
        NpcIntentType intentType,
        string userText)
    {
        switch (intentType)
        {
            case NpcIntentType.Greeting:
                if (!conversationState.HasMetPlayer && !string.IsNullOrWhiteSpace(definition.firstMeetingGreeting))
                {
                    return definition.firstMeetingGreeting.Trim();
                }

                return PickRandomReply(definition.greetingReplies,
                    definition.displayName + "向你点头示意。",
                    definition.fallbackReplies);

            case NpcIntentType.AskIdentity:
                return PickRandomReply(definition.identityReplies,
                    "我是" + definition.displayName + "，负责" + definition.roleTitle + "相关事务。",
                    definition.fallbackReplies);

            case NpcIntentType.AskHistory:
                return PickRandomReply(definition.historyReplies,
                    "这里发生过很多事，公告板上也许有你想要的线索。",
                    definition.fallbackReplies);

            case NpcIntentType.AskDirection:
                return BuildDirectionReply(definition, userText);

            case NpcIntentType.AskSurvivalAdvice:
                return PickRandomReply(definition.survivalAdviceReplies,
                    "先保证食物和照明，再决定走多远。",
                    definition.fallbackReplies);

            case NpcIntentType.AskFishingLakeGuide:
                return BuildFishingReply(definition, FishingDialogueTopic.LakeGuide, userText,
                    "沿着码头的木栈道向南走，先找浅滩练手。");

            case NpcIntentType.AskFishingTips:
                return BuildFishingReply(definition, FishingDialogueTopic.FishingTips, userText,
                    "先稳住节奏，收杆别太急，这样上鱼更稳定。");

            case NpcIntentType.AskFishPrice:
                return BuildFishingReply(definition, FishingDialogueTopic.FishPrice, userText,
                    "今天鱼价波动不大，先钓够再统一出手。");

            case NpcIntentType.AskFishingQuest:
                return BuildFishingReply(definition, FishingDialogueTopic.QuestOffer, userText,
                    "先去湖边钓几条回来，我再给你具体委托。",
                    "happy");

            case NpcIntentType.SubmitFishingQuest:
                return BuildFishingReply(definition, FishingDialogueTopic.QuestSubmit, userText,
                    "你先把目标鱼带过来，我这边才能登记提交。",
                    "happy");

            case NpcIntentType.AskFishingStory:
                return BuildFishingReply(definition, FishingDialogueTopic.EventStory, userText,
                    "今天还没有新的渔闻，你可以稍后再来问。",
                    "happy");

            case NpcIntentType.AcceptFishingRumorVerification:
                return BuildFishingReply(definition, FishingDialogueTopic.StartRumorVerification, userText,
                    "先听到一条渔闻，再去验证。",
                    "happy");

            case NpcIntentType.ReportFishingRumorVerification:
                return BuildFishingReply(definition, FishingDialogueTopic.ReportRumorVerification, userText,
                    "你还没有可汇报的渔闻结果。",
                    "neutral");

            default:
                return string.Empty;
        }
    }

    private string BuildFishingReply(
        NpcDialogueDefinition definition,
        FishingDialogueTopic topic,
        string userText,
        string fallbackReply,
        string emotion = "neutral")
    {
        FishingDialogueService service = FishingDialogueService.Instance;
        if (service == null)
        {
            return fallbackReply;
        }

        ChatResponse fishingResponse;
        bool handled = service.TryBuildRuleResponse(
            definition == null ? string.Empty : definition.characterId,
            topic,
            userText,
            out fishingResponse);

        if (!handled || fishingResponse == null || string.IsNullOrWhiteSpace(fishingResponse.reply))
        {
            return fallbackReply;
        }

        return fishingResponse.reply.Trim();
    }

    private string BuildDirectionReply(NpcDialogueDefinition definition, string userText)
    {
        if (definition.landmarkDirections != null)
        {
            string normalizedText = userText.ToLowerInvariant();
            for (int i = 0; i < definition.landmarkDirections.Count; i++)
            {
                NpcLandmarkDirection landmark = definition.landmarkDirections[i];
                if (landmark == null)
                {
                    continue;
                }

                if (landmark.MatchesUserText(normalizedText) && !string.IsNullOrWhiteSpace(landmark.directionReply))
                {
                    return landmark.directionReply.Trim();
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(definition.unknownLandmarkReply))
        {
            return definition.unknownLandmarkReply.Trim();
        }

        if (!string.IsNullOrWhiteSpace(definition.genericDirectionReply))
        {
            return definition.genericDirectionReply.Trim();
        }

        return PickRandomReply(definition.fallbackReplies,
            "我不确定你要找的地方，但你可以先去本区地标问问看。",
            null);
    }

    private string PickRandomReply(List<string> primary, string fallback, List<string> secondary)
    {
        string primaryReply = PickRandomFromList(primary);
        if (!string.IsNullOrWhiteSpace(primaryReply))
        {
            return primaryReply;
        }

        string secondaryReply = PickRandomFromList(secondary);
        if (!string.IsNullOrWhiteSpace(secondaryReply))
        {
            return secondaryReply;
        }

        return fallback;
    }

    private string PickRandomFromList(List<string> source)
    {
        if (source == null || source.Count == 0)
        {
            return string.Empty;
        }

        List<string> validReplies = new List<string>();
        for (int i = 0; i < source.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(source[i]))
            {
                validReplies.Add(source[i].Trim());
            }
        }

        if (validReplies.Count == 0)
        {
            return string.Empty;
        }

        int randomIndex = Random.Range(0, validReplies.Count);
        return validReplies[randomIndex];
    }
}
