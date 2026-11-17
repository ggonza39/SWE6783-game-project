using UnityEngine;

// Serialized level configuration replaces the original facility's fixed dimensions.
public sealed class UXMissionDefinition : MonoBehaviour
{
    public bool crimson;
    public int requiredEvidence = 3;
    public float halfWidth = 14.5f, halfHeight = 8.5f;
    public string LevelId => crimson ? UXCampaign.CrimsonLevelId : UXPlayerProgress.FacilityLevelId;
}
