using UnityEngine;

public class MenuCameraAngleDrift : MonoBehaviour
{
    [Header("How far the camera can tilt from its starting angle")]
    public float pitchAmount = 1.0f; // up/down
    public float yawAmount = 1.5f;   // left/right
    public float rollAmount = 0.3f;  // optional, usually keep very low

    [Header("How fast the drift moves")]
    public float pitchSpeed = 0.25f;
    public float yawSpeed = 0.2f;
    public float rollSpeed = 0.15f;

    private Vector3 startEuler;

    void Start()
    {
        startEuler = transform.localEulerAngles;
    }

    void Update()
    {
        float pitchOffset = Mathf.Sin(Time.time * pitchSpeed) * pitchAmount;
        float yawOffset = Mathf.Sin(Time.time * yawSpeed + 1.3f) * yawAmount;
        float rollOffset = Mathf.Sin(Time.time * rollSpeed + 2.1f) * rollAmount;

        Quaternion targetRotation = Quaternion.Euler(
            startEuler.x + pitchOffset,
            startEuler.y + yawOffset,
            startEuler.z + rollOffset
        );

        transform.localRotation = targetRotation;
    }
}