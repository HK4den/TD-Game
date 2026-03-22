using UnityEngine;

public class TowerRotationController : MonoBehaviour
{
    public enum ManualRotationMode
    {
        VisualOnly = 0,
        FunctionalAndVisual = 1
    }

    [Header("Rotation Roots")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform functionalRoot;

    [Header("Manual Rotation")]
    [SerializeField] private bool allowManualRotation = true;
    [SerializeField] private bool cardinalOnlyManualRotation = true;
    [SerializeField] private float nonCardinalManualStepDegrees = 45f;
    [SerializeField] private ManualRotationMode manualRotationMode = ManualRotationMode.VisualOnly;

    [Header("Attack Windup Rotation")]
    [SerializeField] private bool allowAttackWindupRotation = true;
    [SerializeField] private bool windupUsesYawOnly = true;

    private int currentCardinalIndex;

    public Transform VisualRoot => visualRoot != null ? visualRoot : transform;
    public Transform FunctionalRoot => functionalRoot != null ? functionalRoot : transform;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        if (functionalRoot == null)
            functionalRoot = transform;

        currentCardinalIndex = GetClosestCardinalIndex(FunctionalRoot.eulerAngles.y);
    }

    public bool CanManualRotate => allowManualRotation;

    public void RotateManualForward()
    {
        if (!allowManualRotation)
            return;

        if (cardinalOnlyManualRotation)
        {
            currentCardinalIndex = (currentCardinalIndex + 1) % 4;
            float yaw = currentCardinalIndex * 90f;
            ApplyManualYaw(yaw);
            return;
        }

        float nextYaw = GetCurrentYaw() + nonCardinalManualStepDegrees;
        ApplyManualYaw(nextYaw);
    }

    public void RotateManualBackward()
    {
        if (!allowManualRotation)
            return;

        if (cardinalOnlyManualRotation)
        {
            currentCardinalIndex--;
            if (currentCardinalIndex < 0)
                currentCardinalIndex = 3;

            float yaw = currentCardinalIndex * 90f;
            ApplyManualYaw(yaw);
            return;
        }

        float nextYaw = GetCurrentYaw() - nonCardinalManualStepDegrees;
        ApplyManualYaw(nextYaw);
    }

    public void SnapAimAtWorldPoint(Vector3 worldPoint)
    {
        if (!allowAttackWindupRotation)
            return;

        Transform root = VisualRoot;
        Vector3 origin = root.position;
        Vector3 direction = worldPoint - origin;

        if (windupUsesYawOnly)
            direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        root.rotation = lookRotation;
    }

    public Vector3 GetFunctionalForward()
    {
        return FunctionalRoot.forward;
    }

    public Quaternion GetFunctionalRotation()
    {
        return FunctionalRoot.rotation;
    }

    public float GetCurrentYaw()
    {
        return FunctionalRoot.eulerAngles.y;
    }

    private void ApplyManualYaw(float yaw)
    {
        if (manualRotationMode == ManualRotationMode.FunctionalAndVisual)
        {
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);

            if (functionalRoot != null)
                functionalRoot.rotation = rot;

            if (visualRoot != null && visualRoot != functionalRoot)
                visualRoot.rotation = rot;
        }
        else
        {
            if (visualRoot != null)
            {
                Vector3 euler = visualRoot.eulerAngles;
                euler.y = yaw;
                visualRoot.eulerAngles = euler;
            }
        }
    }

    private int GetClosestCardinalIndex(float yaw)
    {
        yaw = Mathf.Repeat(yaw, 360f);

        if (yaw < 45f) return 0;
        if (yaw < 135f) return 1;
        if (yaw < 225f) return 2;
        if (yaw < 315f) return 3;
        return 0;
    }
}