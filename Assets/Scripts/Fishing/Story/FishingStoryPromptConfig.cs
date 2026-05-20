using UnityEngine;

[CreateAssetMenu(fileName = "FishingStoryPromptConfig", menuName = "Fishing/Story Prompt Config")]
public class FishingStoryPromptConfig : ScriptableObject
{
    [TextArea(3, 6)]
    public string baseInstruction = "你是末日小镇的渔业播报员。请用中文生成1-2句简短、可信、具体的湖边钓鱼事件，包含时间线索和地点线索，不要使用夸张语气。";

    [TextArea(2, 4)]
    public string fallbackContext = "请讲一则今天湖边发生的钓鱼见闻。";
}
