using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MenuCameraFOVPulse : MonoBehaviour
{
    public float minFOV = 58f;
    public float maxFOV = 62f;
    public float speed = 0.4f;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
        cam.fieldOfView = Mathf.Lerp(minFOV, maxFOV, t);
    }
}