using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ���� �κ��丮 �ý����� �ʱ�ȭ�� ����ϴ� �Ŵ��� Ŭ����
/// </summary>
public class PhysicsInventoryInitializer : MonoBehaviour
{
    private static PhysicsInventoryInitializer instance;
    public static PhysicsInventoryInitializer Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("PhysicsInventoryInitializer");
                instance = go.AddComponent<PhysicsInventoryInitializer>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    [Header("Physics Settings")]
    [SerializeField] private float defaultGravityScale = 980f;
    [SerializeField] private float defaultDragDamping = 0.92f;
    [SerializeField] private float defaultBounceMultiplier = 0.4f;
    [SerializeField] private float defaultGroundFriction = 0.8f;
    [SerializeField] private float defaultMinimumVelocity = 10f;

    [Header("Pool Settings")]
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private int maxPoolSize = 50;
    [SerializeField] private int poolGrowSize = 5;

    // ���� �ý��� �ʱ�ȭ ����
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    // �� �ε� �̺�Ʈ ������
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // CombatScene�� �ε�Ǹ� ���� �ý��� �ʱ�ȭ
        if (GameManager.Instance != null &&
            GameManager.Instance.currentGameState == GameState.Playing &&
            !isInitialized)
        {
            StartCoroutine(InitializePhysicsSystemDelayed());
        }
    }

    /// <summary>
    /// ���� �κ��丮 �ý��� �ʱ�ȭ �ڷ�ƾ
    /// </summary>
    public IEnumerator InitializePhysicsSystemDelayed()
    {
        // ���� ������ �ε�� ������ ���
        yield return new WaitForSeconds(0.5f);

        Debug.Log("Initializing Physics Inventory System in scene...");

        // �κ��丮 ��Ʈ�ѷ� ã��
        InventoryController inventoryController = FindAnyObjectByType<InventoryController>();
        if (inventoryController == null)
        {
            Debug.LogWarning("InventoryController not found in scene. Physics Inventory System initialization skipped.");
            yield break;
        }

        // ���� �κ��丮 �Ŵ��� �߰� �Ǵ� ��������
        PhysicsInventoryManager physicsManager = inventoryController.GetComponent<PhysicsInventoryManager>();
        if (physicsManager == null)
        {
            physicsManager = inventoryController.gameObject.AddComponent<PhysicsInventoryManager>();
            Debug.Log("PhysicsInventoryManager added to InventoryController");
        }

        // ������Ʈ Ǯ �ʱ�ȭ
        InitializePhysicsItemPool(physicsManager);

        // �⺻ ���� ����
        yield return ApplyDefaultSettings(physicsManager);

        // �ʱ�ȭ �Ϸ�
        isInitialized = true;
        Debug.Log("Physics Inventory System initialized in scene");
    }

    /// <summary>
    /// ���� �κ��丮 �������� ���� ������Ʈ Ǯ �ʱ�ȭ
    /// </summary>
    private void InitializePhysicsItemPool(PhysicsInventoryManager physicsManager)
    {
        if (ObjectPool.Instance == null)
        {
            Debug.LogWarning("ObjectPool instance not found. Physics item pool initialization skipped.");
            return;
        }

        // ���� ������ ��������
        GameObject weaponPrefab = null;

        // ���� PhysicsInventoryManager���� ���� �������� �õ�
        if (physicsManager != null)
        {
            // ���÷������� weaponPrefab �ʵ� ���� (�ʿ��� ���)
            var weaponPrefabField = typeof(PhysicsInventoryManager).GetField("weaponPrefab",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);

            if (weaponPrefabField != null)
            {
                weaponPrefab = weaponPrefabField.GetValue(physicsManager) as GameObject;
            }
        }

        // ���ҽ����� �ε� �õ�
        if (weaponPrefab == null)
        {
            weaponPrefab = Resources.Load<GameObject>("Prefabs/UI/WeaponItem");
        }

        // ��� ��ȹ: �κ��丮 ��Ʈ�ѷ����� ã��
        if (weaponPrefab == null)
        {
            var inventoryController = FindAnyObjectByType<InventoryController>();
            if (inventoryController != null)
            {
                var weaponPrefabField = typeof(InventoryController).GetField("weaponPrefab",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public);

                if (weaponPrefabField != null)
                {
                    weaponPrefab = weaponPrefabField.GetValue(inventoryController) as GameObject;
                }
            }
        }

        // Ǯ ����
        if (weaponPrefab != null)
        {
            string poolTag = "PhysicsInventoryItem";

            if (!ObjectPool.Instance.DoesPoolExist(poolTag))
            {
                // �� Ǯ ����
                ObjectPool.Pool physicsItemPool = new ObjectPool.Pool
                {
                    tag = poolTag,
                    prefab = weaponPrefab,
                    initialSize = initialPoolSize,
                    maxSize = maxPoolSize,
                    growSize = poolGrowSize
                };

                // Ǯ ���� �޼���� ���� API�� CreatePool ���
                ObjectPool.Instance.CreatePool(poolTag, weaponPrefab, initialPoolSize);
                Debug.Log($"Physics inventory item pool created with {initialPoolSize} items");
            }
            else
            {
                // ���� Ǯ�� ũ�� Ȯ��
                int currentCount = ObjectPool.Instance.GetAvailableCount(poolTag);
                if (currentCount < initialPoolSize)
                {
                    ObjectPool.Instance.ExpandPool(poolTag, initialPoolSize - currentCount);
                    Debug.Log($"Physics inventory item pool expanded to {initialPoolSize} items");
                }
            }
        }
        else
        {
            Debug.LogWarning("Failed to find weapon prefab for physics item pool initialization");
        }
    }

    /// <summary>
    /// �⺻ ���� ���� ����
    /// </summary>
    private IEnumerator ApplyDefaultSettings(PhysicsInventoryManager physicsManager)
    {
        // ���÷����� ���� �⺻ ���� ���� (���� �ʵ� ������ �Ұ����� ���)
        // �����δ� SerializeField�� ����� ���� ������ ����ϴ� ���� �� �����ϴ�.
        try
        {
            // ��: ���÷����� ����� ���� ����
            var gravityField = typeof(PhysicsInventoryManager).GetField("gravityScale",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (gravityField != null)
            {
                gravityField.SetValue(physicsManager, defaultGravityScale);
            }

            // ��Ÿ �����鵵 ����ϰ� ����
            // ...
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error applying default physics settings: {e.Message}");
        }

        yield return null;
    }

    /// <summary>
    /// GameManager���� ȣ��� �� �ִ� ���� �ʱ�ȭ �޼���
    /// </summary>
    public static IEnumerator InitializeInLoadingScreen()
    {
        // �ν��Ͻ� ���� ����
        var initializer = Instance;

        Debug.Log("Pre-initializing Physics Inventory System during loading...");

        // Ǯ �ʱ�ȭ ���� �̸� �غ�
        var poolInstance = ObjectPool.Instance;
        if (poolInstance != null)
        {
            string poolTag = "PhysicsInventoryItem";
            GameObject weaponPrefab = Resources.Load<GameObject>("Prefabs/UI/WeaponItem");

            if (weaponPrefab != null && !poolInstance.DoesPoolExist(poolTag))
            {
                // Ǯ ����
                poolInstance.CreatePool(poolTag, weaponPrefab, initializer.initialPoolSize);
                Debug.Log($"Physics item pool pre-initialized with {initializer.initialPoolSize} items");
            }
        }

        // �ּ� �̸� �ε�
        yield return initializer.PreloadPhysicsAssets();

        // ���� ó�� ���� ���ҽ� �̸� �غ�
        // ...

        Debug.Log("Physics Inventory System pre-initialization completed");
    }

    /// <summary>
    /// ���� �κ��丮 �Ŵ����� ���ҽ��κ��� �ε��ϰ� ����
    /// </summary>
    public IEnumerator PreloadPhysicsAssets()
    {
        // �ʿ��� ������ �̸� �ε�
        GameObject physicsItemPrefab = Resources.Load<GameObject>("Prefabs/Weapons/Item");
        if (physicsItemPrefab != null)
        {
            // ������ �ε� ����
            Debug.Log("Physics item prefab preloaded");
        }
        else
        {
            // ��ü ������ �ε� �õ�
            physicsItemPrefab = Resources.Load<GameObject>("Prefabs/Weapons/Item");
            if (physicsItemPrefab != null)
            {
                Debug.Log("Using default weapon item as physics item prefab");
            }
            else
            {
                Debug.LogWarning("No suitable physics item prefab found");
            }
        }

        yield return null;

        // ��Ÿ �ʿ��� �ּ� �ε�
        // ...

        yield return null;
    }

}