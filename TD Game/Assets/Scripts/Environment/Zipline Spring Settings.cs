using UnityEngine;

public class ZiplineSpringSettings : ScriptableObject
{
    [Min(0f)] public float hookStretchMultiplier = 1f;
    [Min(0f)] public float stretchStrength = 0.8f;
    [Min(0f)] public float maximumStretch = 0.5f;
    [Range(1f, 500f)] public float springStiffness = 100f;
    [Range(0f, 100f)] public float springDamping = 12f;

    private static ZiplineSpringSettings shared;

    public static ZiplineSpringSettings Shared
    {
        get
        {
            if (shared == null)
            {
                shared = Resources.Load<ZiplineSpringSettings>("Zipline Spring Settings");
                if (shared == null)
                {
                    shared = CreateInstance<ZiplineSpringSettings>();
                    shared.hideFlags = HideFlags.HideAndDontSave;
                    Debug.LogWarning("Missing Resources/Zipline Spring Settings asset; using default spring settings for all ziplines.");
                }
            }
            return shared;
        }
    }
}
