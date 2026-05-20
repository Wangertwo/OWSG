using System;
using System.Collections.Generic;
using UnityEngine;

public class FishingRumorService : MonoBehaviour
{
    [Serializable]
    private class RumorPayload
    {
        public string storyText = string.Empty;
        public string lakeId = string.Empty;
        public string targetFishId = string.Empty;
        public string timeWindow = string.Empty;
        public int confidence = 0;
    }

    public static FishingRumorService Instance { get; private set; }

    [Header("References")]
    [SerializeField] private FishDatabase fishDatabase;
    [SerializeField] private LakeDatabase lakeDatabase;

    [Header("Verification")]
    [SerializeField] private int maxRumorCountPerNpc = 5;
    [SerializeField] private int verificationWindowMinutes = 20;
    [SerializeField] private int maxVerificationAttempts = 5;
    [SerializeField] private bool logDebug;

    private readonly Dictionary<string, List<FishingRumorRecord>> rumorsByNpcId = new Dictionary<string, List<FishingRumorRecord>>();
    private readonly HashSet<string> trustedNpcIds = new HashSet<string>();
    private readonly HashSet<string> compensatedFailedRumorIds = new HashSet<string>();
    private FishingRumorRecord activeVerificationRumor;
    private string latestVerificationSummary = "暂无渔闻验证记录。";

    public string LatestVerificationSummary => latestVerificationSummary;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool TryRegisterRumorFromModel(string npcId, string modelReply, out FishingRumorRecord rumor)
    {
        rumor = null;
        if (string.IsNullOrWhiteSpace(npcId) || string.IsNullOrWhiteSpace(modelReply))
        {
            return false;
        }

        rumor = BuildRumor(npcId.Trim(), modelReply.Trim());
        if (rumor == null)
        {
            return false;
        }

        FishingRumorRecord latest;
        if (TryGetLatestRumor(npcId, out latest) && latest != null)
        {
            if (latest.storyText == rumor.storyText &&
                latest.lakeId == rumor.lakeId &&
                latest.targetFishId == rumor.targetFishId &&
                latest.expireDayIndex == rumor.expireDayIndex)
            {
                rumor = latest;
                FishingEventBoard.PostRumor(BuildBoardEntry(latest));
                return true;
            }
        }

        AddRumor(rumor);

        FishingEventBoard.PostRumor(BuildBoardEntry(rumor));

        if (logDebug)
        {
            Debug.Log("[FishingRumorService] rumor registered: " + rumor.rumorId, this);
        }

        return true;
    }

    public bool TryGetLatestRumor(string npcId, out FishingRumorRecord rumor)
    {
        rumor = null;
        if (string.IsNullOrWhiteSpace(npcId))
        {
            return false;
        }

        List<FishingRumorRecord> rumors;
        if (!rumorsByNpcId.TryGetValue(npcId.Trim(), out rumors) || rumors.Count == 0)
        {
            return false;
        }

        rumor = rumors[rumors.Count - 1];
        return rumor != null;
    }

    public string BuildRumorBrief(string npcId)
    {
        FishingRumorRecord rumor;
        if (!TryGetLatestRumor(npcId, out rumor))
        {
            return "今天湖面很平静，还没有新的渔闻。你可以再问我一条湖边见闻。";
        }

        return "最新渔闻：" + rumor.storyText +
            "（地点：" + ResolveLakeName(rumor.lakeId) +
            "，目标：" + ResolveFishName(rumor.targetFishId) +
            "，时段：" + FormatWindow(rumor.timeWindow) +
            "，可信度：" + rumor.confidence + "%）";
    }

    public string AcceptLatestRumorVerification(string npcId)
    {
        FishingRumorRecord rumor;
        if (!TryGetLatestRumor(npcId, out rumor))
        {
            return "你还没有可验证的渔闻，先问我今天有什么见闻。";
        }

        rumor.verificationAccepted = true;
        rumor.verificationResolved = false;
        rumor.verificationSuccess = false;
        rumor.attemptCount = 0;
        rumor.successCount = 0;
        rumor.acceptedAtUtc = DateTime.UtcNow;
        activeVerificationRumor = rumor;

        return "好，你去验证这条渔闻：在 " + ResolveLakeName(rumor.lakeId) +
            " 重点找 " + ResolveFishName(rumor.targetFishId) + "。回来后告诉我结果。";
    }

    public string BuildVerificationReport()
    {
        if (activeVerificationRumor == null)
        {
            return latestVerificationSummary;
        }

        if (!activeVerificationRumor.verificationResolved)
        {
            return "你正在验证渔闻，当前尝试次数：" + activeVerificationRumor.attemptCount + "/" + maxVerificationAttempts + "。";
        }

        return latestVerificationSummary;
    }

    public bool HasTrustedNpc(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId))
        {
            return false;
        }

        return trustedNpcIds.Contains(npcId.Trim());
    }

    public string BuildTrustedTradeReply(string npcId)
    {
        if (!HasTrustedNpc(npcId))
        {
            return "先帮我验证一条渔闻，确认你靠谱后我再给你好价。";
        }

        return "看在你帮我验明渔闻的份上，我按信任价收你的鱼。";
    }

    public bool TryClaimFailedVerificationCompensation(string npcId, out string reply)
    {
        reply = string.Empty;
        if (activeVerificationRumor == null || string.IsNullOrWhiteSpace(npcId))
        {
            reply = "我这边还没有收到失败的渔闻验证记录。";
            return false;
        }

        if (!string.Equals(activeVerificationRumor.sourceNpcId, npcId.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            reply = "这条渔闻不是我给你的，我没法核对失败记录。";
            return false;
        }

        if (!activeVerificationRumor.verificationResolved || activeVerificationRumor.verificationSuccess)
        {
            reply = "这条渔闻还没有确认失准，先按当前线索继续验证。";
            return false;
        }

        if (compensatedFailedRumorIds.Contains(activeVerificationRumor.rumorId))
        {
            reply = "这条失准渔闻我已经补偿过你了，我再给你换一条新线索。";
            return true;
        }

        string itemName = UnityEngine.Random.value < 0.5f ? "Stick" : "Stone";
        if (InventorySystem.Instance == null || !InventorySystem.Instance.HasFreeSlots(1))
        {
            reply = "抱歉，这条渔闻不准。你背包满了，我先欠你一份补偿，再给你换一条新线索。";
            return true;
        }

        InventorySystem.Instance.AddToInventory(itemName, 1);
        compensatedFailedRumorIds.Add(activeVerificationRumor.rumorId);

        string displayName = itemName == "Stick" ? "树枝" : "石头";
        reply = "抱歉，这条渔闻不准，害你白跑一趟。这个 " + displayName + " 给你当补偿，我再认真给你换一条新渔闻。";
        return true;
    }

    public void NotifyFishingResult(FishingCatchResult result)
    {
        if (activeVerificationRumor == null || !activeVerificationRumor.verificationAccepted || activeVerificationRumor.verificationResolved)
        {
            return;
        }

        if ((DateTime.UtcNow - activeVerificationRumor.acceptedAtUtc).TotalMinutes > verificationWindowMinutes)
        {
            ResolveVerification(false, "渔闻验证超时了，这条消息已经过期。", true);
            return;
        }

        activeVerificationRumor.attemptCount++;

        bool caughtFish = result != null && result.state == FishingResultState.Success;
        bool lakeMatch = caughtFish && string.Equals(result.lakeId, activeVerificationRumor.lakeId, StringComparison.OrdinalIgnoreCase);
        bool fishMatch = caughtFish && string.Equals(result.fishId, activeVerificationRumor.targetFishId, StringComparison.OrdinalIgnoreCase);
        bool windowMatch = IsWindowMatched(activeVerificationRumor.timeWindow);

        if (caughtFish && lakeMatch && fishMatch && windowMatch)
        {
            activeVerificationRumor.successCount++;
            ResolveVerification(true, "渔闻验证成功！这条消息靠谱。", true);
            return;
        }

        if (activeVerificationRumor.attemptCount >= maxVerificationAttempts)
        {
            ResolveVerification(false, "连续验证未命中，这条渔闻可能不准。", true);
        }
    }

    private void ResolveVerification(bool success, string summary, bool addBoardEvent)
    {
        if (activeVerificationRumor == null)
        {
            return;
        }

        activeVerificationRumor.verificationResolved = true;
        activeVerificationRumor.verificationSuccess = success;
        latestVerificationSummary = summary;

        if (success && !string.IsNullOrWhiteSpace(activeVerificationRumor.sourceNpcId))
        {
            trustedNpcIds.Add(activeVerificationRumor.sourceNpcId.Trim());
        }

        if (addBoardEvent)
        {
            FishingEventBoard.PostEvent((success ? "【验真】" : "【失准】") + summary);
        }
    }

    private string BuildBoardEntry(FishingRumorRecord rumor)
    {
        if (rumor == null)
        {
            return string.Empty;
        }

        return "【渔闻】" + rumor.storyText + "\n" +
            "地点：" + ResolveLakeName(rumor.lakeId) +
            "  目标：" + ResolveFishName(rumor.targetFishId) +
            "  时段：" + FormatWindow(rumor.timeWindow) +
            "  可信度：" + rumor.confidence + "%";
    }

    private FishingRumorRecord BuildRumor(string npcId, string rawReply)
    {
        RumorPayload payload = TryParsePayload(rawReply);

        string lakeId = ResolveLakeId(payload == null ? null : payload.lakeId, rawReply);
        string fishId = ResolveFishId(payload == null ? null : payload.targetFishId, rawReply);
        if (string.IsNullOrWhiteSpace(lakeId) || string.IsNullOrWhiteSpace(fishId))
        {
            return null;
        }

        FishingRumorRecord rumor = new FishingRumorRecord();
        rumor.rumorId = "rumor_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + npcId;
        rumor.sourceNpcId = npcId;
        rumor.lakeId = lakeId;
        rumor.targetFishId = fishId;
        rumor.timeWindow = ParseWindow(payload == null ? null : payload.timeWindow);
        rumor.confidence = RollRumorConfidence();
        rumor.storyText = payload != null && !string.IsNullOrWhiteSpace(payload.storyText)
            ? payload.storyText.Trim()
            : ExtractStoryText(rawReply);
        rumor.createdAtUtc = DateTime.UtcNow;
        rumor.expireDayIndex = DateTime.UtcNow.DayOfYear;

        if (string.IsNullOrWhiteSpace(rumor.storyText))
        {
            rumor.storyText = "昨夜 " + ResolveLakeName(rumor.lakeId) + " 水面有异动，有人说见到 " + ResolveFishName(rumor.targetFishId) + "。";
        }

        return rumor;
    }

    private RumorPayload TryParsePayload(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        int start = text.IndexOf('{');
        int end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }

        string json = text.Substring(start, end - start + 1);
        try
        {
            return JsonUtility.FromJson<RumorPayload>(json);
        }
        catch
        {
            return null;
        }
    }

    private string ExtractStoryText(string rawReply)
    {
        if (string.IsNullOrWhiteSpace(rawReply))
        {
            return string.Empty;
        }

        int end = rawReply.LastIndexOf('}');
        if (end >= 0 && end + 1 < rawReply.Length)
        {
            string tail = rawReply.Substring(end + 1).Trim();
            if (!string.IsNullOrWhiteSpace(tail))
            {
                return tail;
            }
        }

        return rawReply.Trim();
    }

    private string ResolveLakeId(string candidateLakeId, string fallbackText)
    {
        if (lakeDatabase == null || lakeDatabase.Lakes == null || lakeDatabase.Lakes.Count == 0)
        {
            return string.Empty;
        }

        LakeDefinition direct = lakeDatabase.GetLakeOrNull(candidateLakeId);
        if (direct != null)
        {
            return direct.lakeId;
        }

        string normalized = string.IsNullOrWhiteSpace(fallbackText) ? string.Empty : fallbackText.ToLowerInvariant();
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
                return lake.lakeId;
            }
        }

        return lakes[UnityEngine.Random.Range(0, lakes.Count)].lakeId;
    }

    private string ResolveFishId(string candidateFishId, string fallbackText)
    {
        if (fishDatabase == null || fishDatabase.Fishes == null || fishDatabase.Fishes.Count == 0)
        {
            return string.Empty;
        }

        FishDefinition direct = fishDatabase.GetFishOrNull(candidateFishId);
        if (direct != null)
        {
            return direct.fishId;
        }

        string normalized = string.IsNullOrWhiteSpace(fallbackText) ? string.Empty : fallbackText.ToLowerInvariant();
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
                return fish.fishId;
            }
        }

        return fishes[UnityEngine.Random.Range(0, fishes.Count)].fishId;
    }

    public float GetVerificationCatchWeightMultiplier(string lakeId, string fishId)
    {
        if (activeVerificationRumor == null ||
            !activeVerificationRumor.verificationAccepted ||
            activeVerificationRumor.verificationResolved)
        {
            return 1f;
        }

        if (!string.Equals(activeVerificationRumor.lakeId, lakeId, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(activeVerificationRumor.targetFishId, fishId, StringComparison.OrdinalIgnoreCase))
        {
            return 1f;
        }

        if (activeVerificationRumor.confidence >= 75)
        {
            return 4f;
        }

        if (activeVerificationRumor.confidence >= 50)
        {
            return 1.5f;
        }

        return 0.5f;
    }

    private int RollRumorConfidence()
    {
        int tier = UnityEngine.Random.Range(0, 3);
        switch (tier)
        {
            case 0:
                return UnityEngine.Random.Range(25, 46);
            case 1:
                return UnityEngine.Random.Range(50, 71);
            default:
                return UnityEngine.Random.Range(75, 96);
        }
    }

    private string ResolveLakeName(string lakeId)
    {
        LakeDefinition lake = lakeDatabase == null ? null : lakeDatabase.GetLakeOrNull(lakeId);
        if (lake == null)
        {
            return string.IsNullOrWhiteSpace(lakeId) ? "未知湖区" : lakeId;
        }

        return string.IsNullOrWhiteSpace(lake.displayName) ? lake.lakeId : lake.displayName;
    }

    private string ResolveFishName(string fishId)
    {
        FishDefinition fish = fishDatabase == null ? null : fishDatabase.GetFishOrNull(fishId);
        if (fish == null)
        {
            return string.IsNullOrWhiteSpace(fishId) ? "未知鱼种" : fishId;
        }

        return string.IsNullOrWhiteSpace(fish.displayName) ? fish.fishId : fish.displayName;
    }

    private void AddRumor(FishingRumorRecord rumor)
    {
        List<FishingRumorRecord> rumors;
        if (!rumorsByNpcId.TryGetValue(rumor.sourceNpcId, out rumors))
        {
            rumors = new List<FishingRumorRecord>();
            rumorsByNpcId.Add(rumor.sourceNpcId, rumors);
        }

        rumors.Clear();
        rumors.Add(rumor);

        if (activeVerificationRumor != null &&
            string.Equals(activeVerificationRumor.sourceNpcId, rumor.sourceNpcId, StringComparison.OrdinalIgnoreCase))
        {
            activeVerificationRumor = null;
            latestVerificationSummary = "新的渔闻已经覆盖旧渔闻，请重新采信后再验证。";
        }
    }

    private FishingRumorTimeWindow ParseWindow(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return FishingRumorTimeWindow.Any;
        }

        string normalized = value.Trim().ToLowerInvariant();
        if (normalized.Contains("morning") || normalized.Contains("早"))
        {
            return FishingRumorTimeWindow.Morning;
        }
        if (normalized.Contains("noon") || normalized.Contains("midday") || normalized.Contains("午"))
        {
            return FishingRumorTimeWindow.Noon;
        }
        if (normalized.Contains("dusk") || normalized.Contains("evening") || normalized.Contains("黄昏") || normalized.Contains("傍晚"))
        {
            return FishingRumorTimeWindow.Dusk;
        }
        if (normalized.Contains("night") || normalized.Contains("夜"))
        {
            return FishingRumorTimeWindow.Night;
        }

        return FishingRumorTimeWindow.Any;
    }

    private bool IsWindowMatched(FishingRumorTimeWindow window)
    {
        if (window == FishingRumorTimeWindow.Any)
        {
            return true;
        }

        int hour = DateTime.Now.Hour;
        switch (window)
        {
            case FishingRumorTimeWindow.Morning:
                return hour >= 5 && hour < 11;
            case FishingRumorTimeWindow.Noon:
                return hour >= 11 && hour < 16;
            case FishingRumorTimeWindow.Dusk:
                return hour >= 16 && hour < 20;
            case FishingRumorTimeWindow.Night:
                return hour >= 20 || hour < 5;
            default:
                return true;
        }
    }

    private string FormatWindow(FishingRumorTimeWindow window)
    {
        switch (window)
        {
            case FishingRumorTimeWindow.Morning:
                return "清晨";
            case FishingRumorTimeWindow.Noon:
                return "正午";
            case FishingRumorTimeWindow.Dusk:
                return "黄昏";
            case FishingRumorTimeWindow.Night:
                return "夜间";
            default:
                return "全天";
        }
    }
}
