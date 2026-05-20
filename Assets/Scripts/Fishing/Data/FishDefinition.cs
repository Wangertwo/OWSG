using UnityEngine;

[CreateAssetMenu(fileName = "FishDefinition", menuName = "Fishing/Fish Definition")]
public class FishDefinition : ScriptableObject
{
    [Header("Identity")]
    public string fishId;
    public string displayName;
    [TextArea(2, 4)]
    public string description;
    public FishRarity rarity = FishRarity.Common;

    [Header("Economy")]
    [Min(1)]
    public int basePrice = 10;

    [Header("Catch")]
    [Min(0.01f)]
    public float defaultCatchWeight = 1f;
}
