using UnityEngine;
using UnityEngine.SceneManagement;
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

    // ���� ���Ǵ� WaitForSeconds ĳ��
    private static readonly WaitForSeconds InitializationDelay = new WaitForSeconds(0.1f);
    #endregion

    private void Awake()
    {
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
        gameScene = new Dictionary<GameState, int>(4) // �ʱ� �뷮 �������� ���Ҵ� ����
        {
            { GameState.MainMenu, 0 },    // StartScene
            { GameState.Playing, 1 },     // CombatScene
            { GameState.Paused, 1 },      // ���� CombatScene���� Pause
            { GameState.GameOver, 1 }     // ���� CombatScene���� GameOver
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
    /// ������ �����ϰ� CombatScene���� ��ȯ�մϴ�.
    /// </summary>
    public void StartGame()
    {
        var soundManager = SoundManager.Instance;
        if (soundManager != null)
        {
            if (!soundManager.IsBGMPlaying("BGM_Battle"))
            {
                soundManager.LoadSoundBank("CombatSoundBank");
                soundManager.PlaySound("BGM_Battle", 1f, true);
            }
        }

        int sceneIndex;
        if (gameScene.TryGetValue(GameState.Playing, out sceneIndex))
        {
            SetGameState(GameState.Playing);
            SceneManager.LoadScene(sceneIndex, LoadSceneMode.Single);
        }
    }

    /// <summary>
    /// ������ ���¸� �����ϰ� ���� �ý����� ������Ʈ�մϴ�.
    /// </summary>
    public void SetGameState(GameState newState)
    {
        if (currentGameState == newState) return;

        currentGameState = newState;
        OnGameStateChanged?.Invoke(newState);

        switch (newState)
        {
            case GameState.MainMenu:
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
    /// ���� ���� ���¿����� ó���� ����մϴ�.
    /// </summary>
    private void HandleGameOver()
    {
        SavePlayerProgress();

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

    /// <summary>
    /// �÷��̾��� ���� ��Ȳ�� �����մϴ�.
    /// </summary>
    public void SavePlayerProgress()
    {
        // ����� SoundManager�� ��ü������ ���� ������ ����
    }
}