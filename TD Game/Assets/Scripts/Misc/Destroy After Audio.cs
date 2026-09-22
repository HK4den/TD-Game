using UnityEngine;

public class DestroyAfterAudio : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Lifetime")]
    [SerializeField] private float fallbackLifetime = 3f;

    [Header("Pitch Randomization")]
    [SerializeField] private bool randomizePitch = true;
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private static readonly System.Collections.Generic.Dictionary<(int, int), System.Collections.Generic.Stack<DestroyAfterAudio>> pools =
        new System.Collections.Generic.Dictionary<(int, int), System.Collections.Generic.Stack<DestroyAfterAudio>>();
    private (int, int) poolKey;
    private bool pooled;
    private float remainingTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPools()
    {
        pools.Clear();
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= ClearScenePools;
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded += ClearScenePools;
    }

    private static void ClearScenePools(UnityEngine.SceneManagement.Scene scene)
    {
        var keys = new System.Collections.Generic.List<(int, int)>();
        foreach (var key in pools.Keys)
            if (key.Item1 == scene.handle) keys.Add(key);
        foreach (var key in keys) pools.Remove(key);
    }

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, UnityEngine.SceneManagement.Scene scene)
    {
        if (prefab.GetComponent<DestroyAfterAudio>() == null)
            return Instantiate(prefab, position, rotation);
        var key = (scene.handle, prefab.GetInstanceID());
        if (!pools.TryGetValue(key, out System.Collections.Generic.Stack<DestroyAfterAudio> pool))
        {
            pool = new System.Collections.Generic.Stack<DestroyAfterAudio>();
            pools.Add(key, pool);
        }
        DestroyAfterAudio instance = null;
        while (pool.Count > 0 && instance == null) instance = pool.Pop();
        if (instance == null)
        {
            GameObject created = Instantiate(prefab, position, rotation);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(created, scene);
            instance = created.GetComponent<DestroyAfterAudio>();
        }
        instance.poolKey = key;
        instance.pooled = true;
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.gameObject.SetActive(true);
        return instance.gameObject;
    }

    private void OnEnable()
    {
        remainingTime = Mathf.Max(0.01f, fallbackLifetime);
        if (audioSource == null) return;
        if (randomizePitch)
            audioSource.pitch = Random.Range(minPitch, maxPitch);
        if (audioSource.clip != null)
            remainingTime = audioSource.clip.length / Mathf.Max(0.01f, Mathf.Abs(audioSource.pitch));
        if (audioSource.playOnAwake) audioSource.Play();
    }

    private void OnDisable()
    {
        if (audioSource != null) audioSource.Stop();
    }

    private void Update()
    {
        remainingTime -= Time.deltaTime;
        if (remainingTime > 0f) return;
        if (pooled && pools.TryGetValue(poolKey, out System.Collections.Generic.Stack<DestroyAfterAudio> pool) && pool.Count < 32)
        {
            gameObject.SetActive(false);
            pool.Push(this);
        }
        else
            Destroy(gameObject);
    }
}
