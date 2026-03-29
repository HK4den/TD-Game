using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCam;

    private void LateUpdate()
    {
        if (mainCam == null)
        {
            mainCam = Camera.main;
            if (mainCam == null) return;
        }

        transform.forward = -mainCam.transform.forward;
    }
}