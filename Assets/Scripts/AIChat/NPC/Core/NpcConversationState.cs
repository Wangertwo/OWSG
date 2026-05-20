using System.Collections.Generic;

public class NpcConversationState
{
    private readonly Dictionary<NpcIntentType, int> intentCount = new Dictionary<NpcIntentType, int>();

    public bool HasMetPlayer { get; private set; }

    public void MarkIntent(NpcIntentType intentType)
    {
        if (intentType != NpcIntentType.Unknown)
        {
            HasMetPlayer = true;
        }

        if (!intentCount.ContainsKey(intentType))
        {
            intentCount[intentType] = 0;
        }

        intentCount[intentType] += 1;
    }

    public int GetIntentCount(NpcIntentType intentType)
    {
        if (!intentCount.ContainsKey(intentType))
        {
            return 0;
        }

        return intentCount[intentType];
    }
}
