using System;

[Serializable]
public class FishingRumorRecord
{
    public string rumorId;
    public string sourceNpcId;
    public string lakeId;
    public string targetFishId;
    public FishingRumorTimeWindow timeWindow;
    public int confidence;
    public string storyText;
    public DateTime createdAtUtc;
    public DateTime acceptedAtUtc;
    public int expireDayIndex;

    public bool verificationAccepted;
    public bool verificationResolved;
    public bool verificationSuccess;
    public int attemptCount;
    public int successCount;
}
