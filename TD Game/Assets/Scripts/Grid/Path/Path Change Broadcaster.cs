using UnityEngine;

public class PathChangeBroadcaster : MonoBehaviour
{
    public static int Version { get; private set; } = 0;

    public static void Bump()
    {
        Version++;
    }
}
