using UnityEngine;

[DisallowMultipleComponent]
public class FishingArea : MonoBehaviour
{
    [SerializeField] private string lakeId;
    [SerializeField] private string displayName = "Unnamed Lake";

    public string LakeId => string.IsNullOrWhiteSpace(lakeId) ? gameObject.name : lakeId.Trim();
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? LakeId : displayName;
}
