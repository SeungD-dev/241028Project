using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ������ �������� ���¿� �ý����� �����ϴ� �Ŵ��� Ŭ����
/// </summary>
public class GameManager : MonoBehaviour
{
    // �̱��� �ν��Ͻ�
    private static GameManager instance;
    private static readonly object _lock = new object();

    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                lock (_lock)
                {
                    if (instance == null)
                    {
                        var go = new GameObject("GameManager");
                        instance = go.AddComponent<GameManager>();
                    }
                }
            }
            return instance;
        }
    }

    #region Properties
    public GameState currentGameState { get; private set; }
    public Dictionary<GameState, int> gameScene { get; private set; }

    private PlayerStats playerStats;
    private ShopController shopController;
    private CombatController combatController;
    private GameOverController gameOverController;

    // ���� ���Ǵ� �Ӽ����� ĳ��
    private bool isInitialized;
    public bool IsInitialized => isInitialized;
    public PlayerStats PlayerStats => playerStats;
    public ShopController ShopController => shopController;
    public CombatController CombatController => combatController;
    public GameOverController GameOverController => gameOverController;

    public event System.Action<GameState> OnGameStateChanged;

    // �ε� ���� �̺�Ʈ
    public event System.Action OnLoadingCompleted;
    public event System.Action OnLoadingCancelled;

    // ���� ���Ǵ� WaitForSeconds ĳ��
    private static readonly WaitForSeconds InitializationDelay = new WaitForSeconds(0.1f);
    private static readonly WaitForSeconds ResourceLoadDelay = new WaitForSeconds(0.02f);
    #endregion

    // �ε� �ý��� ���� �Ӽ�
    [Header("�ε� ����")]
    [SerializeField] private float minimumLoadingTime = 1.5f;
    [SerializeField] private bool aggressiveMemoryOptimization = true;
    public float LoadingProgress { get; private set; }

    // ���ҽ� ĳ�� �� �ε� ���� ����
    private bool isLoadingCancelled = false;
    private AsyncOperation currentSceneLoadOperation;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGameScenes();
            InitializeSound();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// ���� �� ������ �ʱ�ȭ�մϴ�.
    /// </summary>
    private void InitializeGameScenes()
    {
        gameScene = new Dictionary<GameState, int>(5) // �ʱ� �뷮 �������� ���Ҵ� ����
        {
            { GameState.MainMenu, 0 },    // StartScene
            { GameState.Loading, 1 },     // LoadingScene
            { GameState.Playing, 2 },     // CombatScene
            { GameState.Paused, 2 },      // ���� CombatScene���� Pause
            { GameState.GameOver, 2 }     // ���� CombatScene���� GameOver
        };
    }

    /// <summary>
    /// ���� �ý����� �ʱ�ȭ�մϴ�.
    /// </summary>
    private void InitializeSound()
    {
        var soundManager = SoundManager.Instance;
        if (soundManager != null)
        {
            soundManager.LoadSoundBank("IntroSoundBank");
        }
    }

    /// <summary>
    /// ������ ���� �ý����� �ʱ�ȭ�մϴ�.
    /// </summary>
    private void InitializeCombatSound()
    {
        var soundManager = SoundManager.Instance;
        if (soundManager != null)
        {
            soundManager.LoadSoundBank("CombatSoundBank");
        }
    }

    /// <summary>
    /// CombatScene�� �ֿ� ������Ʈ���� �����մϴ�.
    /// </summary>
    public void SetCombatSceneReferences(PlayerStats stats, ShopController shop, CombatController combat, GameOverController gameOver)
    {
        bool shouldInitialize = !isInitialized && stats != null;

        playerStats = stats;
        shopController = shop;
        combatController = combat;
        gameOverController = gameOver;

        if (shouldInitialize)
        {
            playerStats.InitializeStats();
            isInitialized = true;
        }
    }

    /// <summary>
    /// �� ��ȯ �� ������ �ʱ�ȭ�մϴ�.
    /// </summary>
    public void ClearSceneReferences()
    {
        playerStats = null;
        shopController = null;
        combatController = null;
        gameOverController = null;
        isInitialized = false;
    }

    /// <summary>
    /// ������ �����ϰ� �ε� ȭ������ ��ȯ�մϴ�.
    /// </summary>
    public void StartGame()
    {
        // �ε� ���� ���� �ʱ�ȭ
        LoadingProgress = 0f;
        isLoadingCancelled = false;

        // �޸� ����ȭ (������)
        if (aggressiveMemoryOptimization)
        {
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        // �ε� ���·� ��ȯ
        SetGameState(GameState.Loading);

        // �ε� ������ ��ȯ
        int loadingSceneIndex;
        if (gameScene.TryGetValue(GameState.Loading, out loadingSceneIndex))
        {
            SceneManager.LoadScene(loadingSceneIndex);
        }
    }

    /// <summary>
    /// �ε� ���μ����� �����մϴ�. �ε� ������ ȣ��˴ϴ�.
    /// </summary>
    public void StartLoadingProcess()
    {
        StartCoroutine(LoadGameCoroutine());
    }

    private IEnumerator LoadGameCoroutine()
    {
        float startTime = Time.time;

        // 1. �ʱ�ȭ �۾� ����
        yield return StartCoroutine(PerformInitializationSteps());

        if (isLoadingCancelled)
        {
            OnLoadingCancelled?.Invoke();
            yield break;
        }

        // 2. ���� �� �񵿱� �ε�
        int combatSceneIndex;
        if (!gameScene.TryGetValue(GameState.Playing, out combatSceneIndex))
        {
            combatSceneIndex = 2; // �⺻��
        }

        currentSceneLoadOperation = SceneManager.LoadSceneAsync(combatSceneIndex);
        currentSceneLoadOperation.allowSceneActivation = false; // �ε��� �Ϸ�Ǿ �ٷ� Ȱ��ȭ���� ����

        // �� �ε� ����� ������Ʈ (90% -> 100%)
        while (currentSceneLoadOperation.progress < 0.9f)
        {
            LoadingProgress = 0.9f + (currentSceneLoadOperation.progress / 10f);
            yield return null;

            if (isLoadingCancelled)
            {
                OnLoadingCancelled?.Invoke();
                yield break;
            }
        }

        // 3. �ּ� �ε� �ð� ����
        float elapsedTime = Time.time - startTime;
        if (elapsedTime < minimumLoadingTime)
        {
            yield return new WaitForSeconds(minimumLoadingTime - elapsedTime);
        }

        // 4. �ε� �Ϸ�
        LoadingProgress = 1.0f;

        // �ε� �Ϸ� �̺�Ʈ �߻�
        OnLoadingCompleted?.Invoke();

        // 5. �� Ȱ��ȭ
        SetGameState(GameState.Playing);
        currentSceneLoadOperation.allowSceneActivation = true;

        // 6. ���� ���� ���� ���
        var soundManager = SoundManager.Instance;
        if (soundManager != null && !soundManager.IsBGMPlaying("BGM_Battle"))
        {
            soundManager.LoadSoundBank("CombatSoundBank");
            soundManager.PlaySound("BGM_Battle", 1f, true);
        }
    }

    /// <summary>
    /// �ε��� ����մϴ�.
    /// </summary>
    public void CancelLoading()
    {
        isLoadingCancelled = true;

        // �� �ε� �۾� ��� (������ ���)
        if (currentSceneLoadOperation != null && !currentSceneLoadOperation.isDone)
        {
            // Unity�� ���������� AsyncOperation�� ����� ����� �������� ����
            // ��� ���� �޴��� ���ư� �� �޸� ����
            if (aggressiveMemoryOptimization)
            {
                Resources.UnloadUnusedAssets();
                System.GC.Collect();
            }
        }
    }

    // ��� �ʱ�ȭ �ܰ踦 �����ϴ� �ڷ�ƾ
    private IEnumerator PerformInitializationSteps()
    {
        // 1. ���� �ý��� �ʱ�ȭ (0% -> 10%)
        InitializeCombatSound(); // ������ ���� �غ�
        LoadingProgress = 0.1f;
        yield return InitializationDelay;

        if (isLoadingCancelled) yield break;

        // 2. ������Ʈ Ǯ �ʱ�ȭ (10% -> 25%)
        yield return InitializeObjectPools();
        LoadingProgress = 0.25f;

        if (isLoadingCancelled) yield break;

        // 3. ���� ���ҽ� �ε� (25% -> 50%)
        yield return PreloadGameResources();
        LoadingProgress = 0.5f;

        if (isLoadingCancelled) yield break;

        // 4. ���� �ý��� �غ� (50% -> 75%)
        yield return PrepareCombatSystem();
        LoadingProgress = 0.75f;

        if (isLoadingCancelled) yield break;

        // 5. ���� �غ� (75% -> 90%)
        yield return FinalizeInitialization();
        LoadingProgress = 0.9f;
    }

    private IEnumerator InitializeObjectPools()
    {
        // ObjectPool�� �̹� �����ϴ��� Ȯ��
        if (ObjectPool.Instance == null)
        {
            GameObject poolObject = new GameObject("ObjectPool");
            poolObject.AddComponent<ObjectPool>();
            DontDestroyOnLoad(poolObject);
        }

        // �⺻ Ǯ �ʱ�ȭ
        ObjectPool.Instance.CreatePool("BulletDestroyVFX", Resources.Load<GameObject>("Prefabs/VFX/BulletDestroyVFX"), 30);
        LoadingProgress = 0.15f;
        yield return ResourceLoadDelay;

        // �ֿ� ���� Ǯ �ʱ�ȭ
        string[] weaponTypes = { "Buster", "Machinegun", "BeamSaber", "Shotgun", "Cutter", "Sawblade", "Grinder", "ForceField" };
        for (int i = 0; i < weaponTypes.Length; i++)
        {
            string poolName = $"{weaponTypes[i]}_Projectile";
            GameObject prefab = Resources.Load<GameObject>($"Prefabs/Weapons/Projectile{poolName}");
            if (prefab != null)
            {
                int poolSize = GetOptimalPoolSize(weaponTypes[i]);
                ObjectPool.Instance.CreatePool(poolName, prefab, poolSize);
            }

            // ����� ������Ʈ
            LoadingProgress = 0.15f + (0.1f * (i + 1) / weaponTypes.Length);
            yield return ResourceLoadDelay;

            if (isLoadingCancelled) yield break;
        }
    }

    private int GetOptimalPoolSize(string weaponType)
    {
        // ���� Ÿ�Կ� ���� ������ Ǯ ũ�� ��ȯ
        switch (weaponType)
        {
            case "Machinegun": return 50;
            case "Shotgun": return 20;
            case "Buster": return 15;
            case "Cutter": return 20;
            case "Sawblade": return 10;
            case "BeamSaber": return 12;
            case "Grinder": return 12;
            case "ForceField": return 4;
            default: return 15;
        }
    }

    private IEnumerator PreloadGameResources()
    {
        // �� ������ �ε�
        string[] enemyTypes = { "Walker", "Hunter", "Heavy"};
        for (int i = 0; i < enemyTypes.Length; i++)
        {
            GameObject enemyPrefab = Resources.Load<GameObject>($"Prefabs/Characters/{enemyTypes[i]}");
            if (enemyPrefab != null)
            {
                ObjectPool.Instance.CreatePool(enemyTypes[i], enemyPrefab, 15);
            }
            yield return ResourceLoadDelay;
        }
        LoadingProgress = 0.35f;

        if (isLoadingCancelled) yield break;

        // ������ ������ �ε�
        string[] itemTypes = {
            "ExperienceSmall", "ExperienceMedium", "ExperienceLarge",
            "Coin", "Potion", "Magnet"
        };

        for (int i = 0; i < itemTypes.Length; i++)
        {
            GameObject itemPrefab = Resources.Load<GameObject>($"Prefabs/Collectibles/{itemTypes[i]}");
            if (itemPrefab != null)
            {
                int poolSize = (itemTypes[i].Contains("Experience")) ? 30 : 10;
                ObjectPool.Instance.CreatePool(itemTypes[i], itemPrefab, poolSize);
            }
            yield return ResourceLoadDelay;
        }
        LoadingProgress = 0.45f;

        // VFX �ε�
        string[] vfxTypes = { "HitVFX", "ExplosionVFX", "LevelUpVFX", "PickupVFX" };
        for (int i = 0; i < vfxTypes.Length; i++)
        {
            GameObject vfxPrefab = Resources.Load<GameObject>($"Prefabs/VFX/{vfxTypes[i]}");
            if (vfxPrefab != null)
            {
                ObjectPool.Instance.CreatePool(vfxTypes[i], vfxPrefab, 10);
            }
            yield return ResourceLoadDelay;
        }
        LoadingProgress = 0.5f;
    }

    private IEnumerator PrepareCombatSystem()
    {
        // SpawnSettings �ε�
        var spawnSettings = Resources.Load<ScriptableObject>("Data/SpawnSettings");
        yield return ResourceLoadDelay;
        LoadingProgress = 0.6f;

        // �����ͺ��̽� �ε�
        var weaponDatabase = Resources.Load<ScriptableObject>("Data/WeaponDatabase");
        var enemySpawnDatabase = Resources.Load<ScriptableObject>("Data/EnemySpawnDatabase");
        yield return ResourceLoadDelay;
        LoadingProgress = 0.7f;

        // ��Ÿ �ʿ��� ������ �ε�
        yield return ResourceLoadDelay;
        LoadingProgress = 0.75f;
    }

    private IEnumerator FinalizeInitialization()
    {
        // �޸� ����ȭ
        if (aggressiveMemoryOptimization)
        {
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        // �ʱ�ȭ �Ϸ� ���
        yield return InitializationDelay;
    }

    /// <summary>
    /// ������ ���¸� �����ϰ� ���� �ý����� ������Ʈ�մϴ�.
    /// </summary>
    public void SetGameState(GameState newState)
    {
        if (currentGameState == newState) return;

        GameState previousState = currentGameState;
        currentGameState = newState;

        // ���� ���� ���� �ʿ��� �غ� �۾�
        PrepareForStateTransition(previousState, newState);

        // �̺�Ʈ �߻�
        OnGameStateChanged?.Invoke(newState);

        // ���¿� ���� ���� ���� ����
        switch (newState)
        {
            case GameState.MainMenu:
            case GameState.Loading:
            case GameState.Playing:
                Time.timeScale = 1f;
                break;
            case GameState.Paused:
            case GameState.GameOver:
                Time.timeScale = 0f;
                if (newState == GameState.GameOver)
                {
                    HandleGameOver();
                }
                break;
        }
    }

    /// <summary>
    /// ���� ��ȯ �� �ʿ��� �غ� �۾��� �����մϴ�.
    /// </summary>
    private void PrepareForStateTransition(GameState previousState, GameState newState)
    {
        // ��: ���� �޴����� �ε����� ��ȯ ��
        if (previousState == GameState.MainMenu && newState == GameState.Loading)
        {
            // �޸� ���� ���� �۾��� �ʿ��� ���
        }
    }

    /// <summary>
    /// ���� ���� ���¿����� ó���� ����մϴ�.
    /// </summary>
    private void HandleGameOver()
    {
        SavePlayerProgress();

        // ���� ���� ȿ���� ���
        SoundManager.Instance?.PlaySound("GameOver_sfx", 1f, false);

        if (gameOverController != null)
        {
            gameOverController.ShowGameOverPanel();
        }
        else
        {
            Debug.LogError("GameOverController reference is missing!");
        }
    }

    public bool IsPlaying() => currentGameState == GameState.Playing;

    private void OnApplicationQuit()
    {
        SavePlayerProgress();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // ���� ��׶���� �� �� (pauseStatus == true)
        if (pauseStatus && IsPlaying())
        {
            // ���� ��Ȳ �ڵ� ����
            SavePlayerProgress();
        }
    }

    /// <summary>
    /// �÷��̾��� ���� ��Ȳ�� �����մϴ�.
    /// </summary>
    public void SavePlayerProgress()
    {
        // ����� SoundManager�� ��ü������ ���� ������ ����
        // �߰����� ���� ������ �ʿ��ϸ� ���⿡ ����
    }
}
