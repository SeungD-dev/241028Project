using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// ������ �������� ���¿� �ý����� �����ϴ� �Ŵ��� Ŭ����
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GameObject("GameManager").AddComponent<GameManager>();
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

    public PlayerStats PlayerStats => playerStats;
    public ShopController ShopController => shopController;
    public CombatController CombatController => combatController;

    private GameOverController gameOverController;
    public GameOverController GameOverController => gameOverController;
    public bool IsInitialized { get; private set; }

    public event System.Action<GameState> OnGameStateChanged;
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
        gameScene = new Dictionary<GameState, int>()
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
        // StartScene(MainMenu)���� �����ϹǷ� IntroSoundBank �ε�
        var soundManager = SoundManager.Instance;
        soundManager.LoadSoundBank("IntroSoundBank");
    }

    /// <summary>
    /// CombatScene�� �ֿ� ������Ʈ���� �����մϴ�.
    /// </summary>
    public void SetCombatSceneReferences(PlayerStats stats, ShopController shop, CombatController combat, GameOverController gameOver)
    {
        playerStats = stats;
        shopController = shop;
        combatController = combat;
        gameOverController = gameOver;

        if (playerStats != null)
        {
            playerStats.InitializeStats();
            IsInitialized = true;
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
        IsInitialized = false;
    }

    /// <summary>
    /// ������ �����ϰ� CombatScene���� ��ȯ�մϴ�.
    /// </summary>
    public void StartGame()
    {
        var soundManager = SoundManager.Instance;

        // CombatScene���� ��ȯ �� �����ũ �ε�
        soundManager.LoadSoundBank("CombatSoundBank");

        // ���� ���� ���� ������ ä�� ������� ���
        if (!soundManager.IsBGMPlaying("BGM_Battle"))
        {
            soundManager.PlaySound("BGM_Battle", 1f, true);
        }

        SetGameState(GameState.Playing);
        SceneManager.LoadScene(gameScene[GameState.Playing], LoadSceneMode.Single);
    }

    /// <summary>
    /// ������ ���¸� �����ϰ� ���� �ý����� ������Ʈ�մϴ�.
    /// </summary>
    public void SetGameState(GameState newState)
    {
        if (currentGameState != newState)
        {
            currentGameState = newState;
            OnGameStateChanged?.Invoke(newState);

            switch (newState)
            {
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    break;
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.GameOver:
                    if (gameOverController != null)
                    {
                        gameOverController.ShowGameOverPanel();
                    }
                    HandleGameOver();
                    Time.timeScale = 0f;
                    break;
            }
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
        // ����� SoundManager�� ��ü������ ���� ������ �����ϹǷ�
        // �߰����� ������ ������ �ʿ��� ��� ���⿡ ����
    }
}