﻿using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour, IPooledObject
{
    [Header("Hit Effect")]
    [SerializeField] private float hitFlashDuration = 0.1f;
    [SerializeField] private Color hitColor = Color.red;

    [SerializeField] private EnemyData enemyData;
    private float currentHealth;
    private float calculatedMaxHealth;
    private float lastDamageTime;
    private const float damageDelay = 1f;
    private Transform targetTransform;
    private bool isFlashing = false;

    // 컴포넌트 캐싱
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if(spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }
    public void Initialize(Transform target)
    {
        targetTransform = target;
    }


    public void SetEnemyData(EnemyData data)
    {
        if (data == null)
        {
            Debug.LogError("Attempted to set null EnemyData!");
            return;
        }

        enemyData = data;

        // 스프라이트 설정
        if (spriteRenderer != null && enemyData.enemySprite != null)
        {
            spriteRenderer.sprite = enemyData.enemySprite;
        }

        // 스탯 초기화
        InitializeStats();
    }

    public void OnObjectSpawn()
    {
        if (enemyData != null)
        {
            InitializeStats();
        }
        else
        {
            Debug.LogWarning("Enemy spawned without EnemyData!");
        }

        if(spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        isFlashing = false;
    }

    private void InitializeStats()
    {
        if (enemyData == null) return;

        // 플레이어 레벨에 따른 체력 계산
        int playerLevel = GameManager.Instance.PlayerStats.Level;
        calculatedMaxHealth = Mathf.Min(
            enemyData.baseHealth * playerLevel,
            enemyData.maxPossibleHealth
        );
        currentHealth = calculatedMaxHealth;
        lastDamageTime = 0f;

        Debug.Log($"Enemy {enemyData.enemyName} initialized with health: {currentHealth}");
    }

    public void TakeDamage(int damage)
    {
        if (!gameObject.activeSelf) return;

        currentHealth -= damage;

        PlayHitEffect();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void PlayHitEffect()
    {
        if(spriteRenderer != null & !isFlashing)
        {
            StartCoroutine(HitFlashCoroutine());
        }
    }

    private IEnumerator HitFlashCoroutine()
    {
        isFlashing = true;

        spriteRenderer.color = hitColor;
        
        yield return new WaitForSeconds(hitFlashDuration);

        spriteRenderer.color = originalColor;

        isFlashing= false;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!gameObject.activeSelf) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time >= lastDamageTime + damageDelay)
            {
                PlayerStats playerStats = collision.gameObject.GetComponent<PlayerStats>();
                if (playerStats != null)
                {
                    playerStats.TakeDamage(enemyData.baseDamage);
                    lastDamageTime = Time.time;
                }
            }
        }
    }
    private void Die()
    {
        if (enemyData.dropTable != null && GameManager.Instance?.CombatController != null)
        {
            GameManager.Instance.CombatController.SpawnDrops(transform.position, enemyData.dropTable);
        }

        // 적 처치 카운트 증가
        if (GameManager.Instance?.PlayerStats != null)
        {
            GameManager.Instance.PlayerStats.AddKill();
        }
        ReturnToPool();
    }

    public void ReturnToPool()
    {
        if (enemyData == null)
        {
            Debug.LogError("Trying to return enemy to pool but enemyData is null!");
            return;
        }

        // 컴포넌트 상태 초기화
        currentHealth = 0;
        lastDamageTime = 0;

        // 물리 관련 초기화
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // 타겟 초기화
        targetTransform = null;

        // 적의 이름으로 풀에 반환
        ObjectPool.Instance.ReturnToPool(enemyData.enemyName, gameObject);
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        if(spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        isFlashing = false;
    }


    // 프로퍼티
    public float CurrentHealth => currentHealth;
    public float MaxHealth => calculatedMaxHealth;
    public float Damage => enemyData?.baseDamage ?? 0f;
    public float MoveSpeed => enemyData?.moveSpeed ?? 0f;
    public string EnemyName => enemyData?.enemyName ?? "Unknown Enemy";
}


