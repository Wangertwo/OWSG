using System;
using System.Collections.Generic;
using UnityEngine;

public class FishingStoryService : MonoBehaviour
{
    public static FishingStoryService Instance { get; private set; }

    [SerializeField] private FishingStoryPromptConfig promptConfig;
    [SerializeField] private LakeDatabase lakeDatabase;
    [SerializeField] private int cacheMinutes = 15;

    private readonly Dictionary<string, CachedStory> storyCache = new Dictionary<string, CachedStory>();

    private static readonly string[] StoryKeywords =
    {
        "钓鱼故事", "渔闻", "鱼闻", "赛事", "比赛", "湖边发生", "大鱼", "legendary fish", "fishing story"
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool IsStoryQuery(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText))
        {
            return false;
        }

        string normalized = userText.Trim().ToLowerInvariant();
        for (int i = 0; i < StoryKeywords.Length; i++)
        {
            string keyword = StoryKeywords[i];
            if (!string.IsNullOrWhiteSpace(keyword) && normalized.Contains(keyword.ToLowerInvariant()))
            {
                return true;
            }
        }

        return false;
    }

    public string BuildStoryPrompt(string npcDisplayName, string npcRoleTitle, string userText)
    {
        string instruction = promptConfig == null || string.IsNullOrWhiteSpace(promptConfig.baseInstruction)
            ? "你是末日小镇的渔业播报员，请用中文给出1-2句湖边钓鱼见闻。"
            : promptConfig.baseInstruction.Trim();

        string lakeContext = BuildLakeContext();
        string question = string.IsNullOrWhiteSpace(userText)
            ? (promptConfig == null ? "请讲一个湖边钓鱼见闻。" : promptConfig.fallbackContext)
            : userText.Trim();

        return instruction + "\n" +
               "NPC身份: " + SafeText(npcDisplayName, "未知") + " / " + SafeText(npcRoleTitle, "居民") + "\n" +
               "湖区信息: " + lakeContext + "\n" +
               "渔闻目标鱼: FishV1(小蓝鱼)、FishV2(小灰鱼)、FishV3(剑鱼)、FishV4(胖头鱼)、Shark(淡水鲨)\n" +
               "用户提问: " + question + "\n" +
               "输出格式要求: 先输出一行JSON，字段必须包含 storyText,lakeId,targetFishId,timeWindow,confidence；targetFishId 只能从上述5种目标鱼中选择；再输出1-2句中文渔闻正文，80字以内。";
    }

    public bool TryGetCachedStory(string cacheKey, out string story)
    {
        story = null;
        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            return false;
        }

        CachedStory entry;
        if (!storyCache.TryGetValue(cacheKey, out entry))
        {
            return false;
        }

        if ((DateTime.UtcNow - entry.timestampUtc).TotalMinutes > cacheMinutes)
        {
            storyCache.Remove(cacheKey);
            return false;
        }

        story = entry.story;
        return true;
    }

    public void CacheStory(string cacheKey, string story)
    {
        if (string.IsNullOrWhiteSpace(cacheKey) || string.IsNullOrWhiteSpace(story))
        {
            return;
        }

        storyCache[cacheKey] = new CachedStory
        {
            story = story.Trim(),
            timestampUtc = DateTime.UtcNow
        };
    }

    public string BuildCacheKey(string npcId, string userText)
    {
        string npc = SafeText(npcId, "npc_default");
        string query = SafeText(userText, "story");
        return npc + "::" + query.ToLowerInvariant();
    }

    private string BuildLakeContext()
    {
        if (lakeDatabase == null || lakeDatabase.Lakes == null || lakeDatabase.Lakes.Count == 0)
        {
            return "当前已知湖区较少。";
        }

        List<string> names = new List<string>();
        IReadOnlyList<LakeDefinition> lakes = lakeDatabase.Lakes;
        for (int i = 0; i < lakes.Count; i++)
        {
            LakeDefinition lake = lakes[i];
            if (lake == null || string.IsNullOrWhiteSpace(lake.displayName))
            {
                continue;
            }

            names.Add(lake.displayName.Trim());
            if (names.Count >= 4)
            {
                break;
            }
        }

        if (names.Count == 0)
        {
            return "暂无湖区命名信息。";
        }

        return string.Join("、", names.ToArray());
    }

    private static string SafeText(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private struct CachedStory
    {
        public string story;
        public DateTime timestampUtc;
    }
}
