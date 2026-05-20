using System.Collections.Generic;
using UnityEngine;

public class FishingQuestService : MonoBehaviour
{
    [SerializeField] private List<FishingQuestDefinition> questDefinitions = new List<FishingQuestDefinition>();
    [SerializeField] private FishingInventoryBridge inventoryBridge;
    [SerializeField] private PlayerWallet wallet;

    private readonly Dictionary<string, FishingQuestDefinition> definitionById = new Dictionary<string, FishingQuestDefinition>();
    private readonly Dictionary<string, FishingQuestProgress> activeProgress = new Dictionary<string, FishingQuestProgress>();
    private readonly HashSet<string> completedOnce = new HashSet<string>();

    private void Awake()
    {
        RebuildLookup();
    }

    private void OnValidate()
    {
        RebuildLookup();
    }

    public bool TryAcceptQuest(string questId)
    {
        FishingQuestDefinition definition;
        if (!TryGetDefinition(questId, out definition))
        {
            return false;
        }

        if (activeProgress.ContainsKey(definition.questId))
        {
            return false;
        }

        if (!definition.repeatable && completedOnce.Contains(definition.questId))
        {
            return false;
        }

        activeProgress[definition.questId] = new FishingQuestProgress(definition.questId);
        return true;
    }

    public void OnFishCaught(string fishId, int count)
    {
        if (string.IsNullOrWhiteSpace(fishId) || count <= 0)
        {
            return;
        }

        foreach (KeyValuePair<string, FishingQuestProgress> pair in activeProgress)
        {
            FishingQuestProgress progress = pair.Value;
            if (progress == null || progress.completed)
            {
                continue;
            }

            FishingQuestDefinition definition;
            if (!TryGetDefinition(progress.questId, out definition))
            {
                continue;
            }

            if (definition.targetFishId != fishId)
            {
                continue;
            }

            progress.currentCount += count;
            if (progress.currentCount >= definition.requiredCount)
            {
                progress.completed = true;
            }
        }
    }

    public bool TrySubmitQuest(string questId)
    {
        FishingQuestDefinition definition;
        if (!TryGetDefinition(questId, out definition))
        {
            return false;
        }

        FishingQuestProgress progress;
        if (!activeProgress.TryGetValue(definition.questId, out progress))
        {
            return false;
        }

        if (!progress.completed)
        {
            return false;
        }

        FishingInventoryBridge bridge = inventoryBridge;
        if (bridge == null)
        {
            bridge = FindObjectOfType<FishingInventoryBridge>();
        }

        if (bridge == null)
        {
            return false;
        }

        if (!bridge.TryRemoveFish(definition.targetFishId, definition.requiredCount))
        {
            return false;
        }

        PlayerWallet targetWallet = wallet != null ? wallet : PlayerWallet.Instance;
        if (targetWallet != null)
        {
            targetWallet.AddCoins(definition.rewardCoins);
        }

        activeProgress.Remove(definition.questId);
        completedOnce.Add(definition.questId);
        return true;
    }

    public string GetQuestProgressText(string questId)
    {
        FishingQuestDefinition definition;
        if (!TryGetDefinition(questId, out definition))
        {
            return "未找到该委托。";
        }

        FishingQuestProgress progress;
        if (!activeProgress.TryGetValue(definition.questId, out progress))
        {
            return "委托未接取。";
        }

        int current = Mathf.Clamp(progress.currentCount, 0, definition.requiredCount);
        return definition.title + " 进度: " + current + "/" + definition.requiredCount;
    }

    public IReadOnlyCollection<FishingQuestProgress> GetActiveProgress()
    {
        return activeProgress.Values;
    }

    public string GetFirstActiveQuestProgressText()
    {
        foreach (KeyValuePair<string, FishingQuestProgress> pair in activeProgress)
        {
            FishingQuestProgress progress = pair.Value;
            if (progress == null)
            {
                continue;
            }

            return GetQuestProgressText(progress.questId);
        }

        return "当前无钓鱼委托";
    }

    public FishingQuestDefinition FindQuestByPublisher(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId))
        {
            return null;
        }

        string key = npcId.Trim();
        for (int i = 0; i < questDefinitions.Count; i++)
        {
            FishingQuestDefinition quest = questDefinitions[i];
            if (quest != null && quest.publisherNpcId == key)
            {
                return quest;
            }
        }

        return null;
    }

    private bool TryGetDefinition(string questId, out FishingQuestDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(questId))
        {
            definition = null;
            return false;
        }

        if (definitionById.Count == 0)
        {
            RebuildLookup();
        }

        return definitionById.TryGetValue(questId.Trim(), out definition);
    }

    private void RebuildLookup()
    {
        definitionById.Clear();
        for (int i = 0; i < questDefinitions.Count; i++)
        {
            FishingQuestDefinition quest = questDefinitions[i];
            if (quest == null || string.IsNullOrWhiteSpace(quest.questId))
            {
                continue;
            }

            string key = quest.questId.Trim();
            if (!definitionById.ContainsKey(key))
            {
                definitionById[key] = quest;
            }
        }
    }
}
