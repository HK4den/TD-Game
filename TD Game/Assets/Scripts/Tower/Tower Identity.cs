using UnityEngine;

public class TowerIdentity : MonoBehaviour
{
    [Header("UI Identity")]
    [SerializeField] private string displayName = "Tower";
    [SerializeField] private Sprite icon;

    public string DisplayName => displayName;
    public Sprite Icon => icon;
}
