using System;
using System.Collections.Generic;

[Serializable]
public class NpcLandmarkDirection
{
    public string landmarkName;
    public List<string> matchKeywords = new List<string>();
    public string directionReply;

    public bool MatchesUserText(string normalizedUserText)
    {
        if (string.IsNullOrWhiteSpace(normalizedUserText))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(landmarkName) &&
            normalizedUserText.Contains(landmarkName.ToLowerInvariant()))
        {
            return true;
        }

        if (matchKeywords == null)
        {
            return false;
        }

        for (int i = 0; i < matchKeywords.Count; i++)
        {
            string keyword = matchKeywords[i];
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            if (normalizedUserText.Contains(keyword.ToLowerInvariant()))
            {
                return true;
            }
        }

        return false;
    }

    public NpcLandmarkDirection Clone()
    {
        NpcLandmarkDirection clone = new NpcLandmarkDirection();
        clone.landmarkName = landmarkName;
        clone.directionReply = directionReply;
        clone.matchKeywords = matchKeywords == null
            ? new List<string>()
            : new List<string>(matchKeywords);
        return clone;
    }
}
