using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    public static WaveSpawner Active { get; private set; }

    [System.Serializable]
    public class SpawnGroup
    {
        [Header("Enemy Type")]
        public EnemyAgent enemyPrefab;

        [Header("Counts & Timing")]
        public int count = 10;
        public float spawnInterval = 0.6f;
        public float delayAfterGroup = 0.0f;
    }

    [System.Serializable]
    public class Wave
    {
        public string name = "Wave";
        public int completionReward = 50;
        public SpawnGroup[] groups;
    }

    [Header("Refs")]
    [SerializeField] private GridManager grid;
    [SerializeField] private GridPathfinder pathfinder;
    [SerializeField] private EconomyManager economy;
    [SerializeField] private BaseHealth baseHealth;

    [Header("Spawn/Goal")]
    [SerializeField] private Vector2Int spawnCoord = new Vector2Int(0, 0);
    [SerializeField] private Vector2Int goalCoord = new Vector2Int(19, 19);
    [SerializeField] private float spawnYOffset = 0f;

    [Header("Waves")]
    [SerializeField] private Wave[] waves;

    [Header("Wave SFX")]
    [SerializeField] private AudioSource waveStartAudioSource;
    [SerializeField] private AudioSource waveEndAudioSource;

    [Header("Wave Music")]
    [SerializeField] private GameObject betweenWavesMusicPrefab;
    [SerializeField] private GameObject activeWaveMusicPrefab;
    [SerializeField] private Vector3 musicSpawnPosition = Vector3.zero;
    [SerializeField] private bool playBetweenWavesMusicOnStart = true;

    [Header("Music Crossfade")]
    [SerializeField] private float musicFadeDuration = 0.4f;

    public event Action<int> OnWaveStarted;
    public event Action<int, int> OnWaveCompleted;

    public int TotalWaves => waves != null ? waves.Length : 0;
    public int NextWaveNumber => Mathf.Clamp(waveIndex + 1, 1, Mathf.Max(1, TotalWaves));
    public bool IsSpawning => running != null;
    public int AliveEnemiesThisWave => aliveThisWave;
    public bool IsWaveInProgress => waveActiveContext && (IsSpawning || aliveThisWave > 0);

    private int waveIndex = 0;
    private Coroutine running;

    private int aliveThisWave = 0;
    private bool spawningFinished = false;
    private bool waveActiveContext = false;

    private readonly HashSet<EnemyAgent> activeWaveMembers = new HashSet<EnemyAgent>();

    private GameObject currentBetweenWavesMusicInstance;
    private GameObject currentActiveWaveMusicInstance;

    private AudioSource betweenWavesMusicSource;
    private AudioSource activeWaveMusicSource;

    private float betweenWavesTargetVolume = 1f;
    private float activeWaveTargetVolume = 1f;

    private Coroutine musicFadeRoutine;

    private Coroutine endWaveSfxFadeRoutine;
    private float waveEndOriginalVolume = 1f;

    private const float EndWaveSfxInterruptFadeDuration = 0.1f;

    private void Awake()
    {
        Active = this;

        if (economy == null) economy = FindFirstObjectByType<EconomyManager>();
        if (baseHealth == null) baseHealth = FindFirstObjectByType<BaseHealth>();
        if (grid == null) grid = FindFirstObjectByType<GridManager>();
        if (pathfinder == null) pathfinder = FindFirstObjectByType<GridPathfinder>();

        if (waveEndAudioSource != null)
            waveEndOriginalVolume = waveEndAudioSource.volume;

        SetupMusicInstances();

        if (playBetweenWavesMusicOnStart)
            ApplyImmediateMusicState(false);
        else
            ApplyImmediateMusicState(IsWaveInProgress);
    }

    private void OnDestroy()
    {
        if (Active == this)
            Active = null;

        if (musicFadeRoutine != null)
            StopCoroutine(musicFadeRoutine);

        if (endWaveSfxFadeRoutine != null)
            StopCoroutine(endWaveSfxFadeRoutine);

        DestroyMusicInstance(ref currentBetweenWavesMusicInstance, ref betweenWavesMusicSource);
        DestroyMusicInstance(ref currentActiveWaveMusicInstance, ref activeWaveMusicSource);
    }

    [ContextMenu("Start Next Wave")]
    public void StartNextWave()
    {
        if (grid != null)
            grid.RebuildLookupFromChildren();

        if (running != null)
            return;

        if (IsWaveInProgress)
            return;

        if (waves == null || waves.Length == 0)
            return;

        if (waveIndex >= waves.Length)
            return;

        FadeOutWaveEndSfxIfPlaying();

        aliveThisWave = 0;
        spawningFinished = false;
        waveActiveContext = true;
        activeWaveMembers.Clear();

        int startedWaveNumber = waveIndex + 1;
        OnWaveStarted?.Invoke(startedWaveNumber);

        PlayWaveStartSound();
        RefreshWaveMusicState();

        running = StartCoroutine(SpawnWave(waves[waveIndex], startedWaveNumber));
        waveIndex++;
    }

    public EnemyAgent SpawnEnemyFromPrefab(EnemyAgent prefab, Vector3 worldPosition, bool registerToCurrentWave = true)
    {
        if (prefab == null || grid == null || pathfinder == null)
            return null;

        EnemyAgent enemy = Instantiate(prefab, worldPosition, Quaternion.identity);
        ConfigureSpawnedEnemy(enemy, registerToCurrentWave);
        return enemy;
    }

    public EnemyAgent SpawnChildEnemy(EnemyAgent prefab, Vector3 worldPosition)
    {
        bool registerToCurrentWave = waveActiveContext;
        return SpawnEnemyFromPrefab(prefab, worldPosition, registerToCurrentWave);
    }

    public void RegisterEnemyAsCurrentWaveMember(EnemyAgent enemy)
    {
        if (enemy == null || !waveActiveContext)
            return;

        if (!activeWaveMembers.Add(enemy))
            return;

        aliveThisWave++;

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
            health.OnDeathFinalized += HandleEnemyDeathFinalized;

        enemy.OnReachedGoal += HandleEnemyReachedGoal;
    }

    private IEnumerator SpawnWave(Wave wave, int waveNumber)
    {
        if (wave == null || wave.groups == null || wave.groups.Length == 0)
        {
            spawningFinished = true;
            running = null;
            TryCompleteWaveIfDone(waveNumber);
            yield break;
        }

        for (int g = 0; g < wave.groups.Length; g++)
        {
            SpawnGroup group = wave.groups[g];
            if (group == null || group.enemyPrefab == null || group.count <= 0)
                continue;

            for (int i = 0; i < group.count; i++)
            {
                SpawnOne(group.enemyPrefab);

                if (group.spawnInterval > 0f)
                    yield return new WaitForSeconds(group.spawnInterval);
                else
                    yield return null;
            }

            if (group.delayAfterGroup > 0f)
                yield return new WaitForSeconds(group.delayAfterGroup);
        }

        spawningFinished = true;
        running = null;

        TryCompleteWaveIfDone(waveNumber);
    }

    private void SpawnOne(EnemyAgent prefab)
    {
        if (prefab == null || grid == null || pathfinder == null)
            return;

        GridTile spawnTile = grid.GetTile(spawnCoord.x, spawnCoord.y);
        if (spawnTile == null)
            return;

        Vector3 pos = spawnTile.transform.position;
        pos.y += spawnYOffset;

        EnemyAgent enemy = Instantiate(prefab, pos, Quaternion.identity);
        ConfigureSpawnedEnemy(enemy, true);
    }

    private void ConfigureSpawnedEnemy(EnemyAgent enemy, bool registerToCurrentWave)
    {
        if (enemy == null)
            return;

        enemy.SetBaseHealth(baseHealth);

        float moveSpeed = 2.5f;
        EnemyStats stats = enemy.GetComponent<EnemyStats>();
        if (stats != null)
            moveSpeed = stats.MoveSpeed;

        enemy.Init(grid, pathfinder, goalCoord, moveSpeed);

        if (registerToCurrentWave)
            RegisterEnemyAsCurrentWaveMember(enemy);
    }

    private void HandleEnemyDeathFinalized(EnemyHealth health)
    {
        if (health == null)
            return;

        EnemyAgent enemy = health.GetComponent<EnemyAgent>();
        if (enemy == null)
            enemy = health.GetComponentInParent<EnemyAgent>();

        MarkEnemyResolved(enemy);
    }

    private void HandleEnemyReachedGoal(EnemyAgent enemy)
    {
        MarkEnemyResolved(enemy);
    }

    private void MarkEnemyResolved(EnemyAgent enemy)
    {
        if (enemy == null)
            return;

        if (!activeWaveMembers.Remove(enemy))
            return;

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
            health.OnDeathFinalized -= HandleEnemyDeathFinalized;

        enemy.OnReachedGoal -= HandleEnemyReachedGoal;

        if (aliveThisWave > 0)
            aliveThisWave--;

        if (aliveThisWave < 0)
            aliveThisWave = 0;

        int currentWaveNumber = waveIndex;
        TryCompleteWaveIfDone(currentWaveNumber);
    }

    private void TryCompleteWaveIfDone(int waveNumber)
    {
        if (!waveActiveContext)
            return;

        if (!spawningFinished)
            return;

        if (aliveThisWave != 0)
            return;

        int idx = waveNumber - 1;
        int reward = 50;

        if (waves != null && idx >= 0 && idx < waves.Length)
            reward = waves[idx].completionReward;

        if (economy != null && reward > 0)
            economy.AddMoney(reward);

        waveActiveContext = false;
        spawningFinished = false;

        PlayWaveEndSound();
        RefreshWaveMusicState();

        OnWaveCompleted?.Invoke(waveNumber, reward);
    }

    private void PlayWaveStartSound()
    {
        if (waveStartAudioSource != null)
            waveStartAudioSource.Play();
    }

    private void PlayWaveEndSound()
    {
        if (waveEndAudioSource == null)
            return;

        if (endWaveSfxFadeRoutine != null)
        {
            StopCoroutine(endWaveSfxFadeRoutine);
            endWaveSfxFadeRoutine = null;
        }

        waveEndAudioSource.volume = waveEndOriginalVolume;
        waveEndAudioSource.Play();
    }

    private void FadeOutWaveEndSfxIfPlaying()
    {
        if (waveEndAudioSource == null || !waveEndAudioSource.isPlaying)
            return;

        if (endWaveSfxFadeRoutine != null)
            StopCoroutine(endWaveSfxFadeRoutine);

        endWaveSfxFadeRoutine = StartCoroutine(FadeOutAndStopWaveEndSfx());
    }

    private IEnumerator FadeOutAndStopWaveEndSfx()
    {
        if (waveEndAudioSource == null)
            yield break;

        float startVolume = waveEndAudioSource.volume;
        float duration = Mathf.Max(0.01f, EndWaveSfxInterruptFadeDuration);
        float time = 0f;

        while (time < duration && waveEndAudioSource != null && waveEndAudioSource.isPlaying)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            waveEndAudioSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        if (waveEndAudioSource != null)
        {
            waveEndAudioSource.volume = 0f;
            waveEndAudioSource.Stop();
            waveEndAudioSource.volume = waveEndOriginalVolume;
        }

        endWaveSfxFadeRoutine = null;
    }

    private void SetupMusicInstances()
    {
        CreateMusicInstanceIfNeeded(
            betweenWavesMusicPrefab,
            ref currentBetweenWavesMusicInstance,
            ref betweenWavesMusicSource,
            ref betweenWavesTargetVolume);

        CreateMusicInstanceIfNeeded(
            activeWaveMusicPrefab,
            ref currentActiveWaveMusicInstance,
            ref activeWaveMusicSource,
            ref activeWaveTargetVolume);
    }

    private void CreateMusicInstanceIfNeeded(
        GameObject prefab,
        ref GameObject instance,
        ref AudioSource source,
        ref float storedTargetVolume)
    {
        if (prefab == null || instance != null)
            return;

        instance = Instantiate(prefab, musicSpawnPosition, Quaternion.identity);
        source = GetMusicAudioSource(instance);

        if (source == null)
        {
            Debug.LogWarning($"Music prefab '{prefab.name}' does not have an AudioSource on it or its children.");
            return;
        }

        storedTargetVolume = source.volume;
        source.loop = true;
        source.playOnAwake = false;

        if (!source.isPlaying)
            source.Play();
    }

    private AudioSource GetMusicAudioSource(GameObject instance)
    {
        if (instance == null)
            return null;

        AudioSource source = instance.GetComponent<AudioSource>();
        if (source != null)
            return source;

        return instance.GetComponentInChildren<AudioSource>();
    }

    private bool lastMusicWaveState = false;

    private void RefreshWaveMusicState()
    {
        bool waveActive = waveActiveContext;

        if (lastMusicWaveState == waveActive && musicFadeRoutine == null)
            return;

        lastMusicWaveState = waveActive;

        if (musicFadeRoutine != null)
            StopCoroutine(musicFadeRoutine);

        musicFadeRoutine = StartCoroutine(CrossfadeMusic(waveActive));
    }

    private IEnumerator CrossfadeMusic(bool waveActive)
    {
        SetupMusicInstances();

        AudioSource fadeInSource = waveActive ? activeWaveMusicSource : betweenWavesMusicSource;
        AudioSource fadeOutSource = waveActive ? betweenWavesMusicSource : activeWaveMusicSource;

        float fadeInTarget = waveActive ? activeWaveTargetVolume : betweenWavesTargetVolume;
        float fadeOutStart = fadeOutSource != null ? fadeOutSource.volume : 0f;
        float fadeInStart = fadeInSource != null ? fadeInSource.volume : 0f;

        if (fadeInSource != null && !fadeInSource.isPlaying)
            fadeInSource.Play();

        float duration = Mathf.Max(0.01f, musicFadeDuration);
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);

            if (fadeOutSource != null)
                fadeOutSource.volume = Mathf.Lerp(fadeOutStart, 0f, t);

            if (fadeInSource != null)
                fadeInSource.volume = Mathf.Lerp(fadeInStart, fadeInTarget, t);

            yield return null;
        }

        if (fadeOutSource != null)
            fadeOutSource.volume = 0f;

        if (fadeInSource != null)
            fadeInSource.volume = fadeInTarget;

        musicFadeRoutine = null;
    }

    private void ApplyImmediateMusicState(bool waveActive)
    {
        SetupMusicInstances();

        if (betweenWavesMusicSource != null)
            betweenWavesMusicSource.volume = waveActive ? 0f : betweenWavesTargetVolume;

        if (activeWaveMusicSource != null)
            activeWaveMusicSource.volume = waveActive ? activeWaveTargetVolume : 0f;
    }

    private void DestroyMusicInstance(ref GameObject instance, ref AudioSource source)
    {
        source = null;

        if (instance == null)
            return;

        Destroy(instance);
        instance = null;
    }
}