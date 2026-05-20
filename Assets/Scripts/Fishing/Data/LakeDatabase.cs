using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LakeDatabase", menuName = "Fishing/Lake Database")]
public class LakeDatabase : ScriptableObject
{
    [SerializeField] private List<LakeDefinition> lakes = new List<LakeDefinition>();

    private Dictionary<string, LakeDefinition> lakeById;

    public IReadOnlyList<LakeDefinition> Lakes => lakes;

    public bool TryGetLake(string lakeId, out LakeDefinition lake)
    {
        EnsureLookup();
        if (string.IsNullOrWhiteSpace(lakeId))
        {
            lake = null;
            return false;
        }

        return lakeById.TryGetValue(lakeId.Trim(), out lake);
    }

    public LakeDefinition GetLakeOrNull(string lakeId)
    {
        LakeDefinition lake;
        return TryGetLake(lakeId, out lake) ? lake : null;
    }

    private void OnValidate()
    {
        lakeById = null;
    }

    private void EnsureLookup()
    {
        if (lakeById != null)
        {
            return;
        }

        lakeById = new Dictionary<string, LakeDefinition>();
        for (int i = 0; i < lakes.Count; i++)
        {
            LakeDefinition lake = lakes[i];
            if (lake == null || string.IsNullOrWhiteSpace(lake.lakeId))
            {
                continue;
            }

            string key = lake.lakeId.Trim();
            if (!lakeById.ContainsKey(key))
            {
                lakeById.Add(key, lake);
            }
        }
    }
}
