using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    public GameState currentGameState { get; private set; }
    public Dictionary<GameState, int> gameScene { get; private set; }

    // PlayerStats ���� �߰�
    private PlayerStats playerStats;
    public PlayerStats PlayerStats => playerStats;

    public event System.Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        gameScene = new Dictionary<GameState, int>()
        {
            { GameState.MainMenu, 0 },
            { GameState.Playing, 1 },
            { GameState.Paused, 2 },
            {GameState.GameOver, 3 }
        };
    }

    public void SetGameState(GameState newState)
    {
        if (currentGameState != newState)
        {
            currentGameState = newState;
            OnGameStateChanged?.Invoke(newState);

            switch (newState)
            {
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;
                case GameState.GameOver:
                    Time.timeScale = 0f;
                    break;
            }
        }
    }

   public bool IsPlaying() { return currentGameState == GameState.Playing; }


    public void StartGame()
    {
        currentGameState = GameState.Playing;
        SceneManager.LoadScene("CombatScene", LoadSceneMode.Single); 
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (currentGameState == GameState.Playing && scene.name == "CombatScene")
        {
            StartCoroutine(InitializeAfterSceneLoad());
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private IEnumerator InitializeAfterSceneLoad()
    {
        // ���� ������ �ε�� ������ ���
        yield return new WaitForEndOfFrame();

        // Player ã�� �� �ʱ�ȭ
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerStats = player.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.InitializeStats();
            }
        }
    }

    public void FindPlayerStats()
    {
        if (playerStats == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerStats = player.GetComponent<PlayerStats>();
            }
        }
    }

    private void InitializeCombatScene()
    {
        ClearCombatData();
        // ���� ���� �� �ڵ����� ���� 1�� ����
        if (playerStats != null)
        {
            playerStats.InitializeStats();
        }
    }


    private void ClearCombatData()
    {
        playerStats = null;
    }

    // PlayerStats ���� �޼����
    public void SetPlayerStats(PlayerStats stats)
    {
        if (currentGameState == GameState.Playing)
        {
            playerStats = stats;
            Debug.Log("PlayerStats registered with GameManager");
        }
    }

    public void ClearPlayerStats()
    {
        playerStats = null;
    }

    // �� ��ȯ �� PlayerStats ������ ������ ���� �޼����
    public void SavePlayerProgress()
    {
        if (playerStats != null)
        {
            // TODO: �ʿ��� ��� �÷��̾� ���� ��Ȳ ����
        }
    }

    public void LoadPlayerProgress()
    {
        if (playerStats != null)
        {
            // TODO: ����� �÷��̾� ���� ��Ȳ �ε�
        }
    }

    // ���� ���� �� ����
    private void OnApplicationQuit()
    {
        SavePlayerProgress();
    }
}
