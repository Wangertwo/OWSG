using UnityEngine;

[CreateAssetMenu(fileName = "NpcDialogueConfig", menuName = "AI Chat/NPC Dialogue Config")]
public class NpcDialogueConfig : ScriptableObject
{
    public NpcRolePreset preset = NpcRolePreset.Custom;
    public NpcDialogueDefinition definition = new NpcDialogueDefinition();
}
