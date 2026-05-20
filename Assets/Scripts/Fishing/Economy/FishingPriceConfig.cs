using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FishingPriceConfig", menuName = "Fishing/Price Config")]
public class FishingPriceConfig : ScriptableObject
{
    [Range(0.5f, 1f)]
    public float minMultiplier = 0.8f;
    [Range(1f, 2f)]
    public float maxMultiplier = 1.2f;
    public int baseSeed = 20260428;
    public List<FishPriceOverride> overrides = new List<FishPriceOverride>();
}
