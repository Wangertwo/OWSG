using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FishingNpcProfile", menuName = "Fishing/NPC Profile")]
public class FishingNpcProfile : ScriptableObject
{
    public string npcId;
    public FishingNpcRole role = FishingNpcRole.General;
    [TextArea(1, 3)]
    public string personaHint;

    [Header("Knowledge")]
    public List<string> supportedLakeIds = new List<string>();
    [TextArea(2, 4)]
    public string tipTemplate;
    public bool canPublishEvents = true;
    public bool canIssueQuests = false;
}
