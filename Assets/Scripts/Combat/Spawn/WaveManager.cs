using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class WaveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveData waveData;
    [SerializeField] private SpawnWarningController warningController;
    [SerializeField] private ShopController shopController;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private GameObject warningPrefab; // ��� ������

    [Header("Wave UI")]
    [SerializeField] private TextMeshProUGUI waveNumberText;
    [SerializeField] private GameObject waveCompleteBanner;
    [SerializeField] private TextMeshProUGUI waveCompleteText;

    [Header("Spawn Settings")]
    [SerializeField] private float minDistanceFromPlayer = 8f; // �÷��̾�κ��� �ּ� ���� �Ÿ�

    // ���̺� ����
    private int currentWaveNumber = 0;
    private bool isWaveActive = false;
    private bool isInSurvivalPhase = false;
    private float waveTimer = 0f;
    private float spawnTimer = 0f;
    private WaveData.Wave currentWave;
    private Coroutine spawnCoroutine;

    // ĳ��
    private PlayerStats playerStats;
    private PlayerUIController playerUIController;
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private Camera mainCamera;
    private GameMap gameMap;

    // ���ڿ� ĳ��
    private readonly System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(32);
    private const string WAVE_TIME_FORMAT = "Wave: {0:00}";
    private const string SURVIVAL_TIME_FORMAT = "Survive: {0:00}";

    private void Awake()
    {
        if (waveData == null)
        {
            Debug.LogError("WaveData is not assigned!");
            enabled = false;
            return;
        }

        mainCamera = Camera.main;
    }

    private void ValidateReferences()
    {
        if (waveData == null)
        {
            Debug.LogError("WaveData is not assigned!");
            enabled = false;
            return;
        }
        playerUIController = FindAnyObjectByType<PlayerUIController>();
    }

    private void Start()
    {
        // �ʼ� �������� ��� �غ�� ������ ��ٸ��� �ڷ�ƾ ����
        StartCoroutine(WaitForDependencies());

        // ���������� �ʱ�ȭ�� �� �ִ� �۾� ���� ����
        InitializeWarningPool();

        // �κ��丮 ��Ʈ�ѷ��� ���� ��ư �̺�Ʈ ����
        if (inventoryController != null)
        {
            inventoryController.OnProgressButtonClicked += StartNextWave;
        }
    }
    private IEnumerator WaitForDependencies()
    {
        float timeOut = 5f;
        float elapsed = 0f;

        // GameManager ������ Ȯ��
        while (GameManager.Instance == null && elapsed < timeOut)
        {
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager not found after timeout!");
            yield break;
        }

        // PlayerStats ������ �ʱ�ȭ
        while (GameManager.Instance.PlayerStats == null && elapsed < timeOut)
        {
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (GameManager.Instance.PlayerStats == null)
        {
            Debug.LogError("PlayerStats not found after timeout!");
            yield break;
        }

        playerStats = GameManager.Instance.PlayerStats;
        playerStats.OnPlayerDeath += HandlePlayerDeath;

        // MapManager ������ Ȯ��
        while (MapManager.Instance == null && elapsed < timeOut)
        {
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (MapManager.Instance == null)
        {
            Debug.LogError("MapManager not found after timeout!");
            yield break;
        }

        // �� �ε� ���
        yield return StartCoroutine(WaitForMapLoad());

        // ������ �̺�Ʈ ����
        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

        playerUIController = FindAnyObjectByType<PlayerUIController>();
    }
    private IEnumerator WaitForMapLoad()
    {
        float timeOut = 2f; // �� ª�� Ÿ�Ӿƿ� (5�ʡ�2��)
        float elapsed = 0f;

        // MapManager�� ���� ���� �ε�� ������ ��ٸ�
        while (MapManager.Instance.CurrentMap == null && elapsed < timeOut)
        {
            elapsed += 0.05f; // �� ª�� �������� üũ (0.1�ʡ�0.05��)
            yield return new WaitForSeconds(0.05f);
        }

        // �� ���� ��������
        gameMap = MapManager.Instance.CurrentMap;

        if (gameMap == null)
        {
            Debug.LogError("GameMap not found after timeout!");
            yield break;
        }

        // ���� �ε�� �Ŀ� ����Ǿ�� �ϴ� �ʱ�ȭ ����
        InitializeEnemyPools();
        SetupFirstWave();

        Debug.Log("WaveManager fully initialized with map reference");
    }

    private void InitializeSystem()
    {
        // ���� �ε�� �Ŀ� ����Ǿ�� �ϴ� �ʱ�ȭ ����
        InitializeEnemyPools();
        SetupFirstWave();
    }
    private void InitializeWarningPool()
    {
        if (warningPrefab != null && ObjectPool.Instance != null)
        {
            if (!ObjectPool.Instance.DoesPoolExist("SpawnWarning"))
            {
                ObjectPool.Instance.CreatePool("SpawnWarning", warningPrefab, 10);
            }
        }
    }
    private void InitializeEnemyPools()
    {
        // ��� ���̺꿡�� ���Ǵ� �� ���� ����
        HashSet<EnemyData> allEnemyTypes = new HashSet<EnemyData>();

        foreach (var wave in waveData.waves)
        {
            foreach (var enemy in wave.enemies)
            {
                if (enemy.enemyData != null)
                {
                    allEnemyTypes.Add(enemy.enemyData);
                }
            }
        }

        // �� �� ������ ���� Ǯ ����
        foreach (var enemyData in allEnemyTypes)
        {
            if (enemyData.enemyPrefab != null)
            {
                // �̹� Ǯ�� �ִ��� Ȯ��
                if (!ObjectPool.Instance.DoesPoolExist(enemyData.enemyName))
                {
                    // �ø� �Ŵ��� ���� ���
                    EnemyCullingManager cullingManager = FindAnyObjectByType<EnemyCullingManager>();

                    // Enemy ������Ʈ �ʱ�ȭ
                    if (cullingManager != null)
                    {
                        GameObject prefabInstance = enemyData.enemyPrefab;
                        Enemy enemyComponent = prefabInstance.GetComponent<Enemy>();
                        if (enemyComponent != null)
                        {
                            enemyComponent.SetCullingManager(cullingManager);
                        }
                    }

                    // Ǯ ����
                    ObjectPool.Instance.CreatePool(
                        enemyData.enemyName,
                        enemyData.enemyPrefab,
                        enemyData.initialPoolSize
                    );
                }
            }
        }
    }

    private void SetupFirstWave()
    {
        currentWaveNumber = 1;
        currentWave = waveData.GetWave(currentWaveNumber);
        if (currentWave != null)
        {
            UpdateWaveUI();
        }
        else
        {
            Debug.LogError("Failed to get first wave data!");
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        // ���� �÷��� ������ ���� ���̺� ����
        if (newState == GameState.Playing)
        {
            // ������ ���۵Ǹ� ù ���̺� ����
            if (!isWaveActive && currentWaveNumber == 1 && waveTimer == 0f)
            {
                StartWave(currentWaveNumber);
            }
            else if (isWaveActive && spawnCoroutine == null)
            {
                // �Ͻ����� �� �簳 �� ���� �ڷ�ƾ �ٽ� ����
                spawnCoroutine = StartCoroutine(SpawnEnemiesCoroutine());
            }
        }
        else if (newState == GameState.Paused || newState == GameState.GameOver)
        {
            // �Ͻ������� ���ӿ��� �� ���� �ڷ�ƾ ����
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
        }
    }

    private void Update()
    {
        if (!isWaveActive || GameManager.Instance.currentGameState != GameState.Playing)
            return;

        // Ÿ�̸� ������Ʈ
        waveTimer += Time.deltaTime;

        // Ÿ�̸� UI ������Ʈ
        UpdateTimerUI();

        // ���̺� �ܰ� ����
        if (!isInSurvivalPhase && waveTimer >= currentWave.waveDuration)
        {
            // ���̺� �ð� ���� - ���� �ܰ� ����
            isInSurvivalPhase = true;
            waveTimer = 0f; // Ÿ�̸� ����

            // ���� �ڷ�ƾ ����
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
        }
        else if (isInSurvivalPhase && waveTimer >= currentWave.survivalDuration)
        {
            // ���� �ܰ� �Ϸ� - ���̺� Ŭ����
            CompleteWave();
        }

        // �ֱ������� �ı��� �� ����
        if (Time.frameCount % 60 == 0) // �� 1�ʸ���
        {
            CleanupDestroyedEnemies();
        }
    }

    public void StartNextWave()
    {
        // First time this is called, it should start wave 1
        // Next times, it will get the next wave number

        int nextWaveNumber;
        if (currentWaveNumber == 0) // First time
        {
            nextWaveNumber = 1;
        }
        else
        {
            nextWaveNumber = waveData.GetNextWaveNumber(currentWaveNumber);
        }

        // Start the appropriate wave
        if (nextWaveNumber > 0)
        {
            // Hide wave complete banner
            if (waveCompleteBanner != null)
            {
                waveCompleteBanner.SetActive(false);
            }

            StartWave(nextWaveNumber);
        }
        else
        {
            // All waves completed - game victory
            Debug.Log("All waves completed!");
            GameManager.Instance.SetGameState(GameState.GameOver);
        }
    }

    public void StartWave(int waveNumber)
    {
        currentWave = waveData.GetWave(waveNumber);
        if (currentWave == null)
        {
            Debug.LogError($"Wave {waveNumber} not found!");
            return;
        }

        // ���̺� ���� ����
        currentWaveNumber = waveNumber;
        waveTimer = 0f;
        isWaveActive = true;
        isInSurvivalPhase = false;

        // UI ������Ʈ
        UpdateWaveUI();

        // ���� �ڷ�ƾ ����
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        spawnCoroutine = StartCoroutine(SpawnEnemiesCoroutine());

        // ���� ���� �÷��̷� ����
        GameManager.Instance.SetGameState(GameState.Playing);
    }

    private IEnumerator SpawnEnemiesCoroutine()
    {
        // ���� Ÿ�̸� �ʱ�ȭ
        spawnTimer = 0f;

        // ���̺� Ȱ��ȭ ���� �� ���� �ܰ谡 �ƴ� ���� ����
        while (isWaveActive && !isInSurvivalPhase)
        {
            // ������ �÷��� ������ ���� ����
            if (GameManager.Instance.currentGameState == GameState.Playing)
            {
                spawnTimer += Time.deltaTime;

                // ���� �ð��� �Ǿ��� ��
                if (spawnTimer >= currentWave.spawnInterval)
                {
                    // �� ����
                    SpawnEnemyBatch(currentWave.spawnAmount);
                    spawnTimer = 0f;
                }

                // ���̺� �ð��� ����Ǿ����� Ȯ��
                if (waveTimer >= currentWave.waveDuration)
                {
                    break; // ���� �ߴ�
                }
            }

            yield return null;
        }

        spawnCoroutine = null;
    }

    private void SpawnEnemyBatch(int count)
    {
        List<Vector2> spawnPositions = new List<Vector2>(count);

        // Generate all positions first
        for (int i = 0; i < count; i++)
        {
            spawnPositions.Add(GetOptimizedSpawnPosition());
        }

        // Show warnings and spawn enemies in a single coroutine
        StartCoroutine(ShowWarningsAndSpawnBatch(spawnPositions));
    }
    private IEnumerator ShowWarningsAndSpawnBatch(List<Vector2> positions)
    {
       
        List<GameObject> warnings = new List<GameObject>();
        foreach (Vector2 pos in positions)
        {
            GameObject warning = ObjectPool.Instance.SpawnFromPool("SpawnWarning", pos, Quaternion.identity);
            warnings.Add(warning);
        }

        
        yield return new WaitForSeconds(1f);

        
        foreach (GameObject warning in warnings)
        {
            ObjectPool.Instance.ReturnToPool("SpawnWarning", warning);
        }

        foreach (Vector2 pos in positions)
        {
            SpawnEnemy(pos);
        }
    }  
    private void SpawnEnemy(Vector2 position)
    {
        if (!isWaveActive || isInSurvivalPhase) return;

        EnemyData enemyData = waveData.GetRandomEnemy(currentWave);
        if (enemyData == null)
        {
            Debug.LogWarning("Failed to get enemy data for spawning");
            return;
        }

        GameObject enemyObject = ObjectPool.Instance.SpawnFromPool(
            enemyData.enemyName,
            position,
            Quaternion.identity
        );

        if (enemyObject != null)
        {
            Enemy enemy = enemyObject.GetComponent<Enemy>();
            EnemyAI enemyAI = enemyObject.GetComponent<EnemyAI>();

            if (enemy != null && enemyAI != null)
            {
                // �� �ʱ�ȭ
                enemy.SetEnemyData(enemyData);
                enemy.Initialize(GameManager.Instance.PlayerTransform);
                enemyAI.Initialize(GameManager.Instance.PlayerTransform);

                // �ø� �Ŵ��� ���� ����
                EnemyCullingManager cullingManager = FindAnyObjectByType<EnemyCullingManager>();
                if (cullingManager != null)
                {
                    enemy.SetCullingManager(cullingManager);
                }

                // Ȱ��ȭ�� �� ��Ͽ� �߰�
                spawnedEnemies.Add(enemyObject);
            }
            else
            {
                Debug.LogError($"Required components not found on prefab: {enemyData.enemyName}");
                ObjectPool.Instance.ReturnToPool(enemyData.enemyName, enemyObject);
            }
        }
    }

    private Vector2 GetOptimizedSpawnPosition()
    {
        // �ʿ��� �����ڸ� ��ġ ��������
        Vector2 spawnPosition = gameMap.GetRandomEdgePosition();

        // �÷��̾� ��ġ
        Vector2 playerPos = playerStats.transform.position;

        // �÷��̾�� �Ÿ� üũ
        int maxAttempts = 5;
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            float distance = Vector2.Distance(playerPos, spawnPosition);

            // �÷��̾�κ��� �ּ� �Ÿ��� �����ϴ��� Ȯ��
            if (distance >= minDistanceFromPlayer)
            {
                // ȭ�鿡 ������ �ʴ��� Ȯ��
                if (!IsPositionVisible(spawnPosition))
                {
                    break;
                }
            }

            // �ٸ� ��ġ �õ�
            spawnPosition = gameMap.GetRandomEdgePosition();
            attempts++;
        }

        return spawnPosition;
    }

    private bool IsPositionVisible(Vector2 position)
    {
        if (mainCamera == null) return false;

        Vector2 viewportPoint = mainCamera.WorldToViewportPoint(position);
        return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
               viewportPoint.y >= 0 && viewportPoint.y <= 1;
    }

    private void HandlePlayerDeath()
    {
        isWaveActive = false;

        // ���� �ڷ�ƾ ����
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private void CompleteWave()
    {
        isWaveActive = false;
        isInSurvivalPhase = false;

        // ���� �ڷ�ƾ ����
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        // ���̺� ���� ����
        if (playerStats != null && currentWave != null)
        {
            playerStats.AddCoins(currentWave.coinReward);
        }

        // ���̺� �Ϸ� ��� ǥ��
        if (waveCompleteBanner != null)
        {
            waveCompleteBanner.SetActive(true);
            if (waveCompleteText != null)
            {
                waveCompleteText.text = $"Wave {currentWaveNumber} Complete!";
            }
        }

        if (AreAllWavesCompleted())
        {
            Debug.Log("Game Clear");
            //GameManager.Instance.HandleGameVictory();
        }
        else
        {
            // Show shop as usual
            shopController.OpenShop();
        }
    }
    public bool AreAllWavesCompleted()
    {
        return waveData.GetNextWaveNumber(currentWaveNumber) < 0;
    }

    private void UpdateWaveUI()
    {
        if (waveNumberText != null)
        {
            waveNumberText.text = $"Wave {currentWaveNumber}";
        }
    }

    private void UpdateTimerUI()
    {
        // ���ڿ� ����
        string timerDisplay;

        if (isInSurvivalPhase)
        {
            // ���� �ܰ� - ���� ���� �ð� ǥ��
            float remainingTime = currentWave.survivalDuration - waveTimer;
            if (remainingTime < 0) remainingTime = 0;

            stringBuilder.Clear();
            stringBuilder.AppendFormat(SURVIVAL_TIME_FORMAT, remainingTime);
            timerDisplay = stringBuilder.ToString();
        }
        else
        {
            // ���̺� �ܰ� - ���� ���̺� �ð� ǥ��
            float remainingTime = currentWave.waveDuration - waveTimer;
            if (remainingTime < 0) remainingTime = 0;

            stringBuilder.Clear();
            stringBuilder.AppendFormat(WAVE_TIME_FORMAT, remainingTime);
            timerDisplay = stringBuilder.ToString();
        }

        // PlayerUIController�� �ִ� �ð� ǥ�ÿ� ����ȭ
        if (playerUIController != null)
        {
            playerUIController.SetExternalTimer(timerDisplay);
        }
    }

    private void CleanupDestroyedEnemies()
    {
        // ��Ȱ��ȭ�� �� ������Ʈ ����
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (spawnedEnemies[i] == null || !spawnedEnemies[i].activeInHierarchy)
            {
                spawnedEnemies.RemoveAt(i);
            }
        }
    }
    public void EnsureInitialized(GameMap map)
    {
        if (map != null && gameMap == null)
        {
            gameMap = map;
            InitializeSystem();
        }
    }
    private void OnDestroy()
    {
        // �̺�Ʈ ���� ����
        if (playerStats != null)
        {
            playerStats.OnPlayerDeath -= HandlePlayerDeath;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }

        if (inventoryController != null)
        {
            inventoryController.OnProgressButtonClicked -= StartNextWave;
        }

        // �ڷ�ƾ ����
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
    }
}