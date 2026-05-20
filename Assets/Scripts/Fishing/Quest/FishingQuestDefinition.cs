using UnityEngine;

[CreateAssetMenu(fileName = "FishingQuestDefinition", menuName = "Fishing/Quest Definition")]
public class FishingQuestDefinition : ScriptableObject
{
    public string questId;
    public string publisherNpcId;
    public string title;
    [TextArea(2, 4)]
    public string description;

    public string targetFishId;
    [Min(1)]
    public int requiredCount = 1;
    [Min(0)]
    public int rewardCoins = 10;
    public bool repeatable = true;
}
