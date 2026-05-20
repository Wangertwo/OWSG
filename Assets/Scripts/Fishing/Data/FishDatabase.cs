using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FishDatabase", menuName = "Fishing/Fish Database")]
public class FishDatabase : ScriptableObject
{
    [SerializeField] private List<FishDefinition> fishes = new List<FishDefinition>();

    private Dictionary<string, FishDefinition> fishById;

    public IReadOnlyList<FishDefinition> Fishes => fishes;

    public bool TryGetFish(string fishId, out FishDefinition fish)
    {
        EnsureLookup();
        if (string.IsNullOrWhiteSpace(fishId))
        {
            fish = null;
            return false;
        }

        return fishById.TryGetValue(fishId.Trim(), out fish);
    }

    public FishDefinition GetFishOrNull(string fishId)
    {
        FishDefinition fish;
        return TryGetFish(fishId, out fish) ? fish : null;
    }

    private void OnValidate()
    {
        fishById = null;
    }

    private void EnsureLookup()
    {
        if (fishById != null)
        {
            return;
        }

        fishById = new Dictionary<string, FishDefinition>();
        for (int i = 0; i < fishes.Count; i++)
        {
            FishDefinition fish = fishes[i];
            if (fish == null || string.IsNullOrWhiteSpace(fish.fishId))
            {
                continue;
            }

            string key = fish.fishId.Trim();
            if (!fishById.ContainsKey(key))
            {
                fishById.Add(key, fish);
            }
        }
    }
}
