using System;
using UnityEngine;

public abstract class EnemyAI : MonoBehaviour
{
    public StateMachine stateMachine;
    protected Enemy enemyStats;
    protected Transform playerTransform;
    [HideInInspector] public SpriteRenderer spriteRenderer;


    protected virtual void Awake()
    {
        enemyStats = GetComponent<Enemy>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        stateMachine = new StateMachine();
        InitializeStates();
    }

    protected virtual void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        // ���� ���� ���� �̺�Ʈ ����
        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
    }

    protected virtual void OnDestroy()
    {
        // �̺�Ʈ ���� ����
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        // ���� ���¿� ���� AI Ȱ��ȭ/��Ȱ��ȭ
        enabled = (newState == GameState.Playing);
    }

    protected virtual void InitializeStates()
    {
        var idleState = new IdleState(this);
        var chasingState = new ChasingState(this);
        var dieState = new DieState(this);

        stateMachine.SetState(idleState);
        stateMachine.AddTransition(idleState, chasingState,
            new FuncPredicate(() => IsPlayerAlive() && IsGamePlaying()));
    }

    protected virtual void Update()
    {
        // ������ �÷��� ���� ���� ���� �ӽ� ������Ʈ
        if (IsGamePlaying())
        {
            stateMachine.Update();
        }
    }

    protected virtual bool IsPlayerAlive()
    {
        return GameManager.Instance.PlayerStats != null &&
               GameManager.Instance.PlayerStats.CurrentHealth > 0;
    }

    protected virtual bool IsGamePlaying()
    {
        return GameManager.Instance.currentGameState == GameState.Playing;
    }
}
