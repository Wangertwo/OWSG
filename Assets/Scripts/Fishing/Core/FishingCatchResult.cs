public class FishingCatchResult
{
    public FishingResultState state;
    public string lakeId;
    public string fishId;
    public string message;

    public static FishingCatchResult Create(FishingResultState stateValue, string lake, string fish, string text)
    {
        FishingCatchResult result = new FishingCatchResult();
        result.state = stateValue;
        result.lakeId = lake;
        result.fishId = fish;
        result.message = text;
        return result;
    }
}
