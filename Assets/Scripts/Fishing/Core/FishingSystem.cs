using System;
using System.Collections.Generic;
using UnityEngine;

public class FishingSystem : MonoBehaviour
{
    public static FishingSystem Instance { get; private set; }

    [Header("References")]
    [SerializeField] private FishingRod fishingRod;
    [SerializeField] private FishDatabase fishDatabase;
    [SerializeField] private LakeDatabase lakeDatabase;
    [SerializeField] private FishingInventoryBridge inventoryBridge;
    [SerializeField] private FishingEconomyService economyService;
    [SerializeField] private FishingQuestService questService;
    [SerializeField] private FishingUIController uiController;
    [SerializeField] private FishingRumorService rumorService;

    [Header("Catch Rules")]
    [Range(0f, 1f)]
    [SerializeField] private float baseCatchChance = 0.5f;

    [Range(0f, 0.4f)]
    [SerializeField] private float fishermanTipBonus = 0f;

    [SerializeField] private bool debugLogs;
    [SerializeField] private bool autoBindRuntimeReferences = true;

    private string lastCastLakeId;
    private float nextRebindTime;
    private FishingRod subscribedRod;
    private int lastResolveFrame = -1;
    private FishingCatchResult lastResolvedResult;

    public event Action<FishingCatchResult> CatchResolved;
    public event Action<string> FishSold;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        TryAutoBindReferences();
    }

    private void Update()
    {
        if (autoBindRuntimeReferences && Time.unscaledTime >= nextRebindTime)
        {
            nextRebindTime = Time.unscaledTime + 0.5f;
            TryAutoBindReferences();
        }

        if (fishingRod == null || !fishingRod.isActiveAndEnabled)
        {
            return;
        }

        if (fishingRod.isCasted && fishingRod.ActiveFishingArea != null)
        {
            lastCastLakeId = fishingRod.ActiveFishingArea.LakeId;

            if (uiController != null)
            {
                uiController.ShowLake(fishingRod.ActiveFishingArea.DisplayName);
            }
        }

    }

    public FishingCatchResult ResolvePullCatch()
    {
        if (lastResolveFrame == Time.frameCount && lastResolvedResult != null)
        {
            return lastResolvedResult;
        }

        lastResolveFrame = Time.frameCount;

        string lakeId = lastCastLakeId;
        if (string.IsNullOrWhiteSpace(lakeId) && fishingRod != null && fishingRod.ActiveFishingArea != null)
        {
            lakeId = fishingRod.ActiveFishingArea.LakeId;
        }

        if (string.IsNullOrWhiteSpace(lakeId) || lakeDatabase == null || lakeDatabase.GetLakeOrNull(lakeId) == null)
        {
            FishingCatchResult invalidLake = FishingCatchResult.Create(FishingResultState.FailedInvalidLake, lakeId, string.Empty, "没有命中有效湖区，空杆了。");
            EmitCatch(invalidLake);
            lastResolvedResult = invalidLake;
            return invalidLake;
        }

        float chance = Mathf.Clamp01(baseCatchChance + GetSkillBonus());
        bool success = UnityEngine.Random.value <= chance;
        if (!success)
        {
            FishingCatchResult failed = FishingCatchResult.Create(FishingResultState.FailedNoBite, lakeId, string.Empty, "鱼跑了！");
            EmitCatch(failed);
            lastResolvedResult = failed;
            return failed;
        }

        string fishId = PickFishId(lakeId);
        if (string.IsNullOrWhiteSpace(fishId))
        {
            FishingCatchResult failedPool = FishingCatchResult.Create(FishingResultState.FailedNoBite, lakeId, string.Empty, "鱼跑了！");
            EmitCatch(failedPool);
            lastResolvedResult = failedPool;
            return failedPool;
        }

        if (inventoryBridge == null)
        {
            inventoryBridge = FindObjectOfType<FishingInventoryBridge>();
        }

        if (inventoryBridge == null || !inventoryBridge.TryAddFish(fishId, 1))
        {
            string errorMessage = "背包空间不足，鱼获掉回水里了。";
            if (inventoryBridge != null)
            {
                switch (inventoryBridge.LastAddFishError)
                {
                    case FishingInventoryBridge.AddFishError.ResourceNotFound:
                        errorMessage = "鱼道具未找到：请在 Resources 下创建 Fish_" + fishId + " 预制体。";
                        break;
                    case FishingInventoryBridge.AddFishError.PrefabInvalid:
                        errorMessage = "鱼道具配置无效：Fish_" + fishId + " 必须是背包UI预制体（含 RectTransform + InventoryItem）。";
                        break;
                    case FishingInventoryBridge.AddFishError.InventoryMissing:
                        errorMessage = "库存系统未初始化。";
                        break;
                }
            }

            FishingCatchResult inventoryFail = FishingCatchResult.Create(FishingResultState.FailedInventoryFull, lakeId, fishId, errorMessage);
            EmitCatch(inventoryFail);
            lastResolvedResult = inventoryFail;
            return inventoryFail;
        }

        if (questService != null)
        {
            questService.OnFishCaught(fishId, 1);
        }

        FishDefinition fish = fishDatabase == null ? null : fishDatabase.GetFishOrNull(fishId);
        string fishName = fish == null || string.IsNullOrWhiteSpace(fish.displayName) ? fishId : fish.displayName;
        FishingCatchResult result = FishingCatchResult.Create(FishingResultState.Success, lakeId, fishId, "上鱼成功: " + fishName + "。");
        EmitCatch(result);
        lastResolvedResult = result;
        return result;
    }

    public bool TrySellFish(string fishId, int count)
    {
        if (string.IsNullOrWhiteSpace(fishId) || count <= 0)
        {
            return false;
        }

        if (inventoryBridge == null)
        {
            inventoryBridge = FindObjectOfType<FishingInventoryBridge>();
        }

        if (economyService == null)
        {
            economyService = FindObjectOfType<FishingEconomyService>();
        }

        if (inventoryBridge == null || economyService == null)
        {
            return false;
        }

        if (!inventoryBridge.TryRemoveFish(fishId, count))
        {
            return false;
        }

        int revenue = economyService.ComputeSellRevenue(fishId, count);
        if (PlayerWallet.Instance != null)
        {
            PlayerWallet.Instance.AddCoins(revenue);
        }

        FishSold?.Invoke("出售 " + fishId + " x" + count + "，获得 " + revenue + "。");

        if (uiController != null)
        {
            uiController.ShowPriceHint("出售 " + fishId + " x" + count + "，获得 " + revenue + "。");
        }

        return true;
    }

    private string PickFishId(string lakeId)
    {
        if (lakeDatabase == null)
        {
            return string.Empty;
        }

        LakeDefinition lake = lakeDatabase.GetLakeOrNull(lakeId);
        if (lake == null || lake.fishPool == null || lake.fishPool.Count == 0)
        {
            return string.Empty;
        }

        float totalWeight = 0f;
        for (int i = 0; i < lake.fishPool.Count; i++)
        {
            FishPoolEntry entry = lake.fishPool[i];
            if (entry != null && !string.IsNullOrWhiteSpace(entry.fishId))
            {
                totalWeight += ResolveFishPoolWeight(lakeId, entry);
            }
        }

        if (totalWeight <= 0.001f)
        {
            return string.Empty;
        }

        float random = UnityEngine.Random.Range(0f, totalWeight);
        float acc = 0f;
        for (int i = 0; i < lake.fishPool.Count; i++)
        {
            FishPoolEntry entry = lake.fishPool[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.fishId))
            {
                continue;
            }

            acc += ResolveFishPoolWeight(lakeId, entry);
            if (random <= acc)
            {
                return entry.fishId.Trim();
            }
        }

        return lake.fishPool[lake.fishPool.Count - 1].fishId;
    }

    private float ResolveFishPoolWeight(string lakeId, FishPoolEntry entry)
    {
        if (entry == null || string.IsNullOrWhiteSpace(entry.fishId))
        {
            return 0f;
        }

        float weight = Mathf.Max(0f, entry.weight);
        if (rumorService == null)
        {
            rumorService = FishingRumorService.Instance;
        }

        if (rumorService != null)
        {
            weight *= rumorService.GetVerificationCatchWeightMultiplier(lakeId, entry.fishId.Trim());
        }

        return weight;
    }

    private float GetSkillBonus()
    {
        // Placeholder for future skill systems / buffs.
        return fishermanTipBonus;
    }

    private void EmitCatch(FishingCatchResult result)
    {
        if (debugLogs && result != null)
        {
            Debug.Log("[FishingSystem] " + result.message);
        }

        if (rumorService != null)
        {
            rumorService.NotifyFishingResult(result);
        }

        if (uiController != null && result != null)
        {
            uiController.SetVisible(true);
            uiController.ShowResult(result.message);

            string priceHint = "-";
            if (result.state == FishingResultState.Success &&
                economyService != null &&
                !string.IsNullOrWhiteSpace(result.fishId))
            {
                int currentPrice = economyService.GetCurrentPrice(result.fishId);
                FishDefinition fish = fishDatabase == null ? null : fishDatabase.GetFishOrNull(result.fishId);
                string fishName = fish == null || string.IsNullOrWhiteSpace(fish.displayName) ? result.fishId : fish.displayName;
                priceHint = currentPrice > 0 ? (fishName + " 收购价 " + currentPrice) : "-";
            }

            string questHint = questService == null
                ? "当前无钓鱼委托"
                : questService.GetFirstActiveQuestProgressText();

            uiController.ShowPriceHint(priceHint);
            uiController.ShowQuestHint(questHint);
        }

        CatchResolved?.Invoke(result);
    }

    private void TryAutoBindReferences()
    {
        if (fishingRod == null || !fishingRod.isActiveAndEnabled)
        {
            FishingRod[] rods = FindObjectsOfType<FishingRod>(true);
            for (int i = 0; i < rods.Length; i++)
            {
                FishingRod candidate = rods[i];
                if (candidate == null || !candidate.isActiveAndEnabled)
                {
                    continue;
                }

                if (candidate.gameObject.scene.IsValid())
                {
                    fishingRod = candidate;
                    BindRodCallbacks();
                    break;
                }
            }
        }
        else
        {
            BindRodCallbacks();
        }

        if (inventoryBridge == null)
        {
            inventoryBridge = FindObjectOfType<FishingInventoryBridge>(true);
        }

        if (economyService == null)
        {
            economyService = FindObjectOfType<FishingEconomyService>(true);
        }

        if (questService == null)
        {
            questService = FindObjectOfType<FishingQuestService>(true);
        }

        if (uiController == null)
        {
            uiController = FindObjectOfType<FishingUIController>(true);
        }

        if (rumorService == null)
        {
            rumorService = FindObjectOfType<FishingRumorService>(true);
        }
    }

    private void BindRodCallbacks()
    {
        if (subscribedRod == fishingRod)
        {
            return;
        }

        if (subscribedRod != null)
        {
            subscribedRod.PullTriggered -= HandlePullTriggered;
            subscribedRod.CastStarted -= HandleCastStarted;
        }

        subscribedRod = fishingRod;
        if (subscribedRod != null)
        {
            subscribedRod.PullTriggered += HandlePullTriggered;
            subscribedRod.CastStarted += HandleCastStarted;
        }
    }

    private void HandleCastStarted(FishingArea area)
    {
        if (area != null)
        {
            lastCastLakeId = area.LakeId;
        }
    }

    private void HandlePullTriggered()
    {
        ResolvePullCatch();
    }

    private void OnDisable()
    {
        if (subscribedRod != null)
        {
            subscribedRod.PullTriggered -= HandlePullTriggered;
            subscribedRod.CastStarted -= HandleCastStarted;
            subscribedRod = null;
        }
    }
}
