using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LakeDefinition", menuName = "Fishing/Lake Definition")]
public class LakeDefinition : ScriptableObject
{
    [Header("Identity")]
    public string lakeId;
    public string displayName;
    [TextArea(2, 4)]
    public string locationHint;
    public bool unlockedByDefault = true;

    [Header("Fish Pool")]
    public List<FishPoolEntry> fishPool = new List<FishPoolEntry>();
}
