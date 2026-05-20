using System;
using System.Collections.Generic;
using UnityEngine;

public class FishingEconomyService : MonoBehaviour
{
    [SerializeField] private FishDatabase fishDatabase;
    [SerializeField] private FishingPriceConfig priceConfig;

    private readonly Dictionary<string, int> cachedDailyPrice = new Dictionary<string, int>();
    private int currentDayKey = int.MinValue;

    public event Action DailyPriceChanged;

    public int GetCurrentPrice(string fishId)
    {
        EnsureDailyPrice();

        if (string.IsNullOrWhiteSpace(fishId))
        {
            return 0;
        }

        string key = fishId.Trim();
        int value;
        return cachedDailyPrice.TryGetValue(key, out value) ? value : 0;
    }

    public int ComputeSellRevenue(string fishId, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        int unitPrice = GetCurrentPrice(fishId);
        return Mathf.Max(0, unitPrice * count);
    }

    public string BuildPriceHintForNpc(string fishId)
    {
        int price = GetCurrentPrice(fishId);
        if (price <= 0)
        {
            return "今天这个鱼种行情不明，先去浅滩试试手气。";
        }

        return "今天 " + fishId + " 的收购价大约是 " + price + "。";
    }

    public void ForceRefreshTodayPrice()
    {
        currentDayKey = int.MinValue;
        EnsureDailyPrice();
    }

    private void EnsureDailyPrice()
    {
        int dayKey = DateTime.UtcNow.Date.GetHashCode();
        if (dayKey == currentDayKey)
        {
            return;
        }

        RebuildPriceTable(dayKey);
        DailyPriceChanged?.Invoke();
    }

    private void RebuildPriceTable(int dayKey)
    {
        cachedDailyPrice.Clear();
        currentDayKey = dayKey;

        if (fishDatabase == null)
        {
            return;
        }

        Dictionary<string, int> overridePrice = BuildOverrideLookup();

        float minMultiplier = priceConfig == null ? 0.8f : Mathf.Clamp(priceConfig.minMultiplier, 0.5f, 1f);
        float maxMultiplier = priceConfig == null ? 1.2f : Mathf.Clamp(priceConfig.maxMultiplier, 1f, 2f);
        int seedBase = priceConfig == null ? 20260428 : priceConfig.baseSeed;

        IReadOnlyList<FishDefinition> fishes = fishDatabase.Fishes;
        for (int i = 0; i < fishes.Count; i++)
        {
            FishDefinition fish = fishes[i];
            if (fish == null || string.IsNullOrWhiteSpace(fish.fishId))
            {
                continue;
            }

            string key = fish.fishId.Trim();
            int fixedPrice;
            if (overridePrice.TryGetValue(key, out fixedPrice))
            {
                cachedDailyPrice[key] = Mathf.Max(1, fixedPrice);
                continue;
            }

            int seed = seedBase ^ dayKey ^ key.GetHashCode();
            System.Random random = new System.Random(seed);
            float t = (float)random.NextDouble();
            float multiplier = Mathf.Lerp(minMultiplier, maxMultiplier, t);
            int dailyPrice = Mathf.Max(1, Mathf.RoundToInt(fish.basePrice * multiplier));
            cachedDailyPrice[key] = dailyPrice;
        }
    }

    private Dictionary<string, int> BuildOverrideLookup()
    {
        Dictionary<string, int> lookup = new Dictionary<string, int>();
        if (priceConfig == null || priceConfig.overrides == null)
        {
            return lookup;
        }

        for (int i = 0; i < priceConfig.overrides.Count; i++)
        {
            FishPriceOverride entry = priceConfig.overrides[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.fishId))
            {
                continue;
            }

            string key = entry.fishId.Trim();
            if (!lookup.ContainsKey(key))
            {
                lookup[key] = entry.fixedPrice;
            }
        }

        return lookup;
    }
}
