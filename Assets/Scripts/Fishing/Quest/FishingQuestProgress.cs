public class FishingQuestProgress
{
    public string questId;
    public int currentCount;
    public bool completed;

    public FishingQuestProgress(string id)
    {
        questId = id;
        currentCount = 0;
        completed = false;
    }
}
