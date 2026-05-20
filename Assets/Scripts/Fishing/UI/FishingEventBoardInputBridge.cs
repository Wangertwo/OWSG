using UnityEngine;

public class FishingEventBoardInputBridge : MonoBehaviour
{
    private static FishingEventBoardInputBridge instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureBridge()
    {
        if (instance != null)
        {
            return;
        }

        GameObject bridgeObject = new GameObject("FishingEventBoardInputBridge");
        instance = bridgeObject.AddComponent<FishingEventBoardInputBridge>();
        DontDestroyOnLoad(bridgeObject);
    }

    private void Update()
    {
        FishingEventBoard.ProcessGlobalToggleInput();
    }
}
