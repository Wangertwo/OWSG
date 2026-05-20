using System;
using UnityEngine;

public class NpcIntentRouter
{
    private static readonly string[] GreetingKeywords =
    {
        "hello", "hi", "hey", "greetings", "你好", "您好", "嗨", "哈喽"
    };

    private static readonly string[] IdentityKeywords =
    {
        "who are you", "your name", "introduce yourself", "identity",
        "你是谁", "你叫什么", "自我介绍", "介绍你自己", "你的身份", "你是干什么的",
        "村长", "渔夫", "猎户"
    };

    private static readonly string[] HistoryKeywords =
    {
        "history", "story", "past", "happened", "background", "town",
        "历史", "往事", "以前", "背景", "发生过什么", "发生了什么",
        "小镇", "村子", "镇子", "这里"
    };

    private static readonly string[] DirectionKeywords =
    {
        "where", "how to get", "route", "way", "find", "locate",
        "在哪", "哪里", "怎么走", "路线", "路", "怎么去", "往哪走", "去"
    };

    private static readonly string[] SurvivalKeywords =
    {
        "advice", "tip", "tips", "help", "survive", "beginner", "start",
        "建议", "提示", "帮助", "生存", "新手", "该做什么", "怎么活", "开局"
    };

    private static readonly string[] FishingLakeGuideKeywords =
    {
        "lake", "where to fish", "fishing spot", "钓鱼点", "湖在哪", "哪里钓鱼", "去哪个湖", "鱼点"
    };

    private static readonly string[] FishingTipsKeywords =
    {
        "fishing tips", "how to fish", "fishing skill", "钓鱼技巧", "怎么钓", "钓鱼建议", "抛竿技巧"
    };

    private static readonly string[] FishPriceKeywords =
    {
        "fish price", "sell fish", "market", "价格", "鱼价", "卖鱼", "收购价", "行情"
    };

    private static readonly string[] FishingStoryKeywords =
    {
        "fishing story", "rumor", "competition", "赛事", "比赛", "渔闻", "钓鱼故事", "湖边发生"
    };

    private static readonly string[] AcceptRumorKeywords =
    {
        "验证这条消息", "验证渔闻", "我要去验证", "采信这条消息", "去验证"
    };

    private static readonly string[] ReportRumorKeywords =
    {
        "汇报渔闻结果", "验证结果", "我回来汇报", "结果怎么样", "回报结果"
    };

    private static readonly string[] FishingQuestKeywords =
    {
        "quest", "mission", "委托", "任务", "接任务", "有活吗"
    };

    private static readonly string[] SubmitQuestKeywords =
    {
        "submit", "turn in", "交任务", "提交", "交付"
    };

    private static readonly string[] SelfReferenceKeywords =
    {
        "you", "your", "你"
    };

    private static readonly string[] SettlementKeywords =
    {
        "town", "village", "这里", "小镇", "村子", "镇子", "这个村", "这个镇"
    };

    public NpcIntentType DetectIntent(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText))
        {
            return NpcIntentType.Unknown;
        }

        string normalizedText = userText.Trim().ToLowerInvariant();

        int greetingScore = CountKeywordMatches(normalizedText, GreetingKeywords);
        int identityScore = CountKeywordMatches(normalizedText, IdentityKeywords);
        int historyScore = CountKeywordMatches(normalizedText, HistoryKeywords);
        int directionScore = CountKeywordMatches(normalizedText, DirectionKeywords);
        int survivalScore = CountKeywordMatches(normalizedText, SurvivalKeywords);
        int fishingLakeGuideScore = CountKeywordMatches(normalizedText, FishingLakeGuideKeywords);
        int fishingTipsScore = CountKeywordMatches(normalizedText, FishingTipsKeywords);
        int fishPriceScore = CountKeywordMatches(normalizedText, FishPriceKeywords);
        int fishingStoryScore = CountKeywordMatches(normalizedText, FishingStoryKeywords);
        int acceptRumorScore = CountKeywordMatches(normalizedText, AcceptRumorKeywords);
        int reportRumorScore = CountKeywordMatches(normalizedText, ReportRumorKeywords);
        int fishingQuestScore = CountKeywordMatches(normalizedText, FishingQuestKeywords);
        int submitQuestScore = CountKeywordMatches(normalizedText, SubmitQuestKeywords);

        bool hasIntroduce = normalizedText.Contains("介绍") || normalizedText.Contains("introduce");
        bool hasSettlementRef = ContainsAnyKeyword(normalizedText, SettlementKeywords);
        bool hasSelfRef = ContainsAnyKeyword(normalizedText, SelfReferenceKeywords);

        if (hasIntroduce && hasSettlementRef)
        {
            historyScore += 3;
        }

        if (hasIntroduce && hasSelfRef)
        {
            identityScore += 2;
        }

        if (historyScore > 0 && hasSettlementRef)
        {
            historyScore += 1;
        }

        int maxScore = greetingScore;
        maxScore = Mathf.Max(maxScore, identityScore);
        maxScore = Mathf.Max(maxScore, historyScore);
        maxScore = Mathf.Max(maxScore, directionScore);
        maxScore = Mathf.Max(maxScore, survivalScore);
        maxScore = Mathf.Max(maxScore, fishingLakeGuideScore);
        maxScore = Mathf.Max(maxScore, fishingTipsScore);
        maxScore = Mathf.Max(maxScore, fishPriceScore);
        maxScore = Mathf.Max(maxScore, fishingStoryScore);
        maxScore = Mathf.Max(maxScore, acceptRumorScore);
        maxScore = Mathf.Max(maxScore, reportRumorScore);
        maxScore = Mathf.Max(maxScore, fishingQuestScore);
        maxScore = Mathf.Max(maxScore, submitQuestScore);
        if (maxScore <= 0)
        {
            return NpcIntentType.Unknown;
        }

        if (reportRumorScore == maxScore)
        {
            return NpcIntentType.ReportFishingRumorVerification;
        }

        if (acceptRumorScore == maxScore)
        {
            return NpcIntentType.AcceptFishingRumorVerification;
        }

        if (submitQuestScore == maxScore)
        {
            return NpcIntentType.SubmitFishingQuest;
        }

        if (fishingQuestScore == maxScore)
        {
            return NpcIntentType.AskFishingQuest;
        }

        if (fishingStoryScore == maxScore)
        {
            return NpcIntentType.AskFishingStory;
        }

        if (fishPriceScore == maxScore)
        {
            return NpcIntentType.AskFishPrice;
        }

        if (fishingTipsScore == maxScore)
        {
            return NpcIntentType.AskFishingTips;
        }

        if (fishingLakeGuideScore == maxScore)
        {
            return NpcIntentType.AskFishingLakeGuide;
        }

        if (directionScore == maxScore)
        {
            return NpcIntentType.AskDirection;
        }

        if (survivalScore == maxScore)
        {
            return NpcIntentType.AskSurvivalAdvice;
        }

        if (historyScore == maxScore)
        {
            return NpcIntentType.AskHistory;
        }

        if (identityScore == maxScore)
        {
            return NpcIntentType.AskIdentity;
        }

        if (greetingScore == maxScore)
        {
            return NpcIntentType.Greeting;
        }

        return NpcIntentType.Unknown;
    }

    private int CountKeywordMatches(string normalizedText, string[] keywords)
    {
        if (string.IsNullOrWhiteSpace(normalizedText) || keywords == null)
        {
            return 0;
        }

        int score = 0;
        for (int i = 0; i < keywords.Length; i++)
        {
            string keyword = keywords[i];
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            if (normalizedText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 1;
            }
        }

        return score;
    }

    private bool ContainsAnyKeyword(string normalizedText, string[] keywords)
    {
        if (keywords == null)
        {
            return false;
        }

        for (int i = 0; i < keywords.Length; i++)
        {
            string keyword = keywords[i];
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            if (normalizedText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
