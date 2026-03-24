using UnityEngine;
using DamageNumbersPro;

public class TowerTargetingFeedback : MonoBehaviour
{
    [Header("DNP World-Space Popup")]
    [SerializeField] private DamageNumber worldSpacePopupPrefab;

    [Header("Spawn Offset")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2f, 0f);

    public void ShowMode(TowerTargetingMode mode)
    {
        if (worldSpacePopupPrefab == null)
            return;

        if (mode == TowerTargetingMode.None)
            return;

        string text = mode.ToString();

        DamageNumber dn = worldSpacePopupPrefab.Spawn(transform.position + worldOffset);
        dn.leftText = text;
    }
}