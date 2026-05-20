using System.Collections.Generic;
using UnityEngine;

public class FishingDialogueService : MonoBehaviour
{
    public static FishingDialogueService Instance { get; private set; }

    [SerializeField] private LakeDatabase lakeDatabase;
    [SerializeField] private FishDatabase fishDatabase;
    [SerializeField] private FishingEconomyService economyService;
    [SerializeField] private FishingQuestService questService;
    [SerializeField] private FishingRumorService rumorService;
    [SerializeField] private FishingNpcProfile[] npcProfiles;

    private readonly Dictionary<string, FishingNpcProfile> profileByNpcId = new Dictionary<string, FishingNpcProfile>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RebuildLookup();

        if (rumorService == null)
        {
            rumorService = FindObjectOfType<FishingRumorService>(true);
        }
    }

    private void OnValidate()
    {
        RebuildLookup();
    }

    public bool TryBuildRuleResponse(
        string npcId,
        FishingDialogueTopic topic,
        string userText,
        out ChatResponse response)
    {
        response = null;
        string reply = string.Empty;

        switch (topic)
        {
            case FishingDialogueTopic.LakeGuide:
                reply = BuildLakeGuideReply(npcId, userText);
                break;
            case FishingDialogueTopic.FishingTips:
                reply = BuildTipsReply(npcId);
                break;
            case FishingDialogueTopic.FishPrice:
                reply = BuildPriceReply(userText);
                break;
            case FishingDialogueTopic.EventStory:
                reply = BuildRumorReply(npcId);
                break;
            case FishingDialogueTopic.StartRumorVerification:
                reply = BuildRumorVerificationAcceptReply(npcId);
                break;
            case FishingDialogueTopic.ReportRumorVerification:
                reply = BuildRumorVerificationReportReply();
                break;
            case FishingDialogueTopic.QuestOffer:
                reply = BuildQuestOfferReply(npcId);
                break;
            case FishingDialogueTopic.QuestSubmit:
                reply = BuildQuestSubmitReply(npcId);
                break;
            default:
                return false;
        }

        if (string.IsNullOrWhiteSpace(reply))
        {
            return false;
        }

        response = new ChatResponse
        {
            success = true,
            reply = reply,
            emotion = "neutral",
            animation = "talk",
            error = string.Empty
        };
        return true;
    }

    public List<ChatKnownLake> BuildKnownLakes(int maxCount = 8)
    {
        List<ChatKnownLake> knownLakes = new List<ChatKnownLake>();
        if (lakeDatabase == null || lakeDatabase.Lakes == null)
        {
            return knownLakes;
        }

        int safeMax = Mathf.Max(1, maxCount);
        IReadOnlyList<LakeDefinition> lakes = lakeDatabase.Lakes;
        for (int i = 0; i < lakes.Count && knownLakes.Count < safeMax; i++)
        {
            LakeDefinition lake = lakes[i];
            if (lake == null || string.IsNullOrWhiteSpace(lake.lakeId))
            {
                continue;
            }

            string name = string.IsNullOrWhiteSpace(lake.displayName) ? lake.lakeId.Trim() : lake.displayName.Trim();
            knownLakes.Add(new ChatKnownLake(lake.lakeId.Trim(), name));
        }

        return knownLakes;
    }

    public List<ChatKnownFish> BuildKnownFishes(int maxCount = 12)
    {
        List<ChatKnownFish> knownFishes = new List<ChatKnownFish>();
        if (fishDatabase == null || fishDatabase.Fishes == null)
        {
            return knownFishes;
        }

        int safeMax = Mathf.Max(1, maxCount);
        IReadOnlyList<FishDefinition> fishes = fishDatabase.Fishes;
        for (int i = 0; i < fishes.Count && knownFishes.Count < safeMax; i++)
        {
            FishDefinition fish = fishes[i];
            if (fish == null || string.IsNullOrWhiteSpace(fish.fishId))
            {
                continue;
            }

            string name = string.IsNullOrWhiteSpace(fish.displayName) ? fish.fishId.Trim() : fish.displayName.Trim();
            knownFishes.Add(new ChatKnownFish(fish.fishId.Trim(), name));
        }

        return knownFishes;
    }

    private string BuildLakeGuideReply(string npcId, string userText)
    {
        LakeDefinition matchedLake = FindLakeFromUserText(userText);
        if (matchedLake != null)
        {
            string hint = string.IsNullOrWhiteSpace(matchedLake.locationHint)
                ? "沿着水声走，留意地势低洼处。"
                : matchedLake.locationHint.Trim();
            return "你要找的是 " + matchedLake.displayName + "。" + hint;
        }

        FishingNpcProfile profile = GetProfile(npcId);
        if (profile != null && profile.supportedLakeIds != null && profile.supportedLakeIds.Count > 0)
        {
            string firstLakeId = profile.supportedLakeIds[0];
            LakeDefinition preferredLake = lakeDatabase == null ? null : lakeDatabase.GetLakeOrNull(firstLakeId);
            if (preferredLake != null)
            {
                string hint = string.IsNullOrWhiteSpace(preferredLake.locationHint)
                    ? "先去这片湖试试。"
                    : preferredLake.locationHint.Trim();
                return "先去 " + preferredLake.displayName + "。" + hint;
            }
        }

        return "先去码头附近的浅水区练手，白天风平时更容易看漂。";
    }

    private string BuildTipsReply(string npcId)
    {
        FishingNpcProfile profile = GetProfile(npcId);
        if (profile != null && !string.IsNullOrWhiteSpace(profile.tipTemplate))
        {
            return profile.tipTemplate.Trim();
        }

        return "抛竿后别急着收，等一拍再拉杆。今天先稳住节奏，命中率会更高。";
    }

    private string BuildPriceReply(string userText)
    {
        if (economyService == null || fishDatabase == null || fishDatabase.Fishes == null || fishDatabase.Fishes.Count == 0)
        {
            return "今天鱼价还没整理出来，晚点来看公告板。";
        }

        FishDefinition requestedFish = FindFishFromUserText(userText);
        if (requestedFish != null)
        {
            int price = economyService.GetCurrentPrice(requestedFish.fishId);
            return requestedFish.displayName + " 今天收购价约 " + price + "。";
        }

        FishDefinition topFish = FindTopPricedFish();
        if (topFish == null)
        {
            return "今天普通鱼价比较平稳。";
        }

        int topPrice = economyService.GetCurrentPrice(topFish.fishId);
        return "今天最值钱的是 " + topFish.displayName + "，收购价约 " + topPrice + "。";
    }

    private string BuildQuestOfferReply(string npcId)
    {
        if (questService == null)
        {
            return "今天没有整理新的钓鱼委托。";
        }

        FishingQuestDefinition quest = questService.FindQuestByPublisher(npcId);
        if (quest == null)
        {
            return "我这边暂时没有钓鱼委托，先去练练手。";
        }

        bool accepted = questService.TryAcceptQuest(quest.questId);
        if (!accepted)
        {
            return "你已经接了这个委托，进度自己看看。";
        }

        return "新委托: " + quest.title + "。目标是收集 " + quest.requiredCount + " 条 " + quest.targetFishId + "。";
    }

    private string BuildQuestSubmitReply(string npcId)
    {
        if (questService == null)
        {
            return "委托系统还没准备好。";
        }

        FishingQuestDefinition quest = questService.FindQuestByPublisher(npcId);
        if (quest == null)
        {
            return "你没在我这里接委托。";
        }

        bool submitted = questService.TrySubmitQuest(quest.questId);
        if (!submitted)
        {
            return questService.GetQuestProgressText(quest.questId);
        }

        return "收到了，委托完成，奖励已经发给你。";
    }

    private string BuildRumorReply(string npcId)
    {
        FishingRumorService service = rumorService != null ? rumorService : FishingRumorService.Instance;
        if (service == null)
        {
            return "渔闻板还没准备好，你可以稍后再来问。";
        }

        return service.BuildRumorBrief(npcId);
    }

    private string BuildRumorVerificationAcceptReply(string npcId)
    {
        FishingRumorService service = rumorService != null ? rumorService : FishingRumorService.Instance;
        if (service == null)
        {
            return "渔闻验证系统还没启动。";
        }

        return service.AcceptLatestRumorVerification(npcId);
    }

    private string BuildRumorVerificationReportReply()
    {
        FishingRumorService service = rumorService != null ? rumorService : FishingRumorService.Instance;
        if (service == null)
        {
            return "渔闻验证系统还没启动。";
        }

        return service.BuildVerificationReport();
    }

    private FishingQuestDefinition FindQuestByPublisher(string npcId)
    {
        return questService == null ? null : questService.FindQuestByPublisher(npcId);
    }

    private FishingNpcProfile GetProfile(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId))
        {
            return null;
        }

        if (profileByNpcId.Count == 0)
        {
            RebuildLookup();
        }

        FishingNpcProfile profile;
        profileByNpcId.TryGetValue(npcId.Trim(), out profile);
        return profile;
    }

    private LakeDefinition FindLakeFromUserText(string userText)
    {
        if (lakeDatabase == null || lakeDatabase.Lakes == null || string.IsNullOrWhiteSpace(userText))
        {
            return null;
        }

        string normalized = userText.ToLowerInvariant();
        IReadOnlyList<LakeDefinition> lakes = lakeDatabase.Lakes;
        for (int i = 0; i < lakes.Count; i++)
        {
            LakeDefinition lake = lakes[i];
            if (lake == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(lake.displayName) && normalized.Contains(lake.displayName.ToLowerInvariant()))
            {
                return lake;
            }

            if (!string.IsNullOrWhiteSpace(lake.lakeId) && normalized.Contains(lake.lakeId.ToLowerInvariant()))
            {
                return lake;
            }
        }

        return null;
    }

    private FishDefinition FindFishFromUserText(string userText)
    {
        if (fishDatabase == null || fishDatabase.Fishes == null || string.IsNullOrWhiteSpace(userText))
        {
            return null;
        }

        string normalized = userText.ToLowerInvariant();
        IReadOnlyList<FishDefinition> fishes = fishDatabase.Fishes;
        for (int i = 0; i < fishes.Count; i++)
        {
            FishDefinition fish = fishes[i];
            if (fish == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(fish.displayName) && normalized.Contains(fish.displayName.ToLowerInvariant()))
            {
                return fish;
            }

            if (!string.IsNullOrWhiteSpace(fish.fishId) && normalized.Contains(fish.fishId.ToLowerInvariant()))
            {
                return fish;
            }
        }

        return null;
    }

    private FishDefinition FindTopPricedFish()
    {
        if (fishDatabase == null || fishDatabase.Fishes == null)
        {
            return null;
        }

        FishDefinition best = null;
        int bestPrice = int.MinValue;
        IReadOnlyList<FishDefinition> fishes = fishDatabase.Fishes;
        for (int i = 0; i < fishes.Count; i++)
        {
            FishDefinition fish = fishes[i];
            if (fish == null || string.IsNullOrWhiteSpace(fish.fishId))
            {
                continue;
            }

            int price = economyService.GetCurrentPrice(fish.fishId);
            if (price > bestPrice)
            {
                bestPrice = price;
                best = fish;
            }
        }

        return best;
    }

    private void RebuildLookup()
    {
        profileByNpcId.Clear();
        if (npcProfiles == null)
        {
            return;
        }

        for (int i = 0; i < npcProfiles.Length; i++)
        {
            FishingNpcProfile profile = npcProfiles[i];
            if (profile == null || string.IsNullOrWhiteSpace(profile.npcId))
            {
                continue;
            }

            string key = profile.npcId.Trim();
            if (!profileByNpcId.ContainsKey(key))
            {
                profileByNpcId.Add(key, profile);
            }
        }
    }
}
