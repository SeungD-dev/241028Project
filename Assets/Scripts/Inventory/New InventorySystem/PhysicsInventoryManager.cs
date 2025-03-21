using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

/// <summary>
/// ���� ��� �������� �����ϴ� �Ŵ��� Ŭ���� - ������Ʈ Ǯ Ȱ�� ����
/// </summary>
public class PhysicsInventoryManager : MonoBehaviour
{
    #region Serialized Fields
    [Header("References")]
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private ItemGrid mainGrid;
    [SerializeField] private Transform itemSpawnPoint;
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private GameObject weaponPrefab;

    [Header("Physics Settings")]
    [SerializeField] private float dragThreshold = 0.3f;
    [SerializeField] private float holdDelay = 0.3f;
    [SerializeField] private float physicsSpawnOffset = 50f;
    [SerializeField] private float physicsRandomVariance = 30f;

    [Header("Pool Settings")]
    [SerializeField] private bool useObjectPool = true;
    [SerializeField] private string poolTag = "PhysicsInventoryItem";
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private int ensurePoolSize = 10; // �ּ��� ������ Ǯ ũ��
    #endregion

    #region Private Fields
    private List<PhysicsInventoryItem> physicsItems = new List<PhysicsInventoryItem>();
    private PhysicsInventoryItem selectedPhysicsItem;
    private TouchActions touchActions;
    private InputAction touchPosition;
    private InputAction touchPress;
    private bool isHolding = false;
    private bool isDragging = false;
    private Vector2 touchStartPosition;
    private Coroutine holdCoroutine;
    private Camera mainCamera;
    private bool isInitialized = false;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        StartCoroutine(DelayedInitialization());
    }

    private void OnEnable()
    {
        if (touchActions != null)
        {
            touchActions.Enable();
        }
    }

    private void OnDisable()
    {
        if (touchActions != null)
        {
            touchActions.Disable();
        }

        // ���� ���� �ڷ�ƾ ����
        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }
    }

    private void Update()
    {
        // ���õ� �������� �ְ� �巡�� ���̸� ��ġ ������Ʈ
        if (selectedPhysicsItem != null && isDragging)
        {
            Vector2 currentTouchPos = touchPosition.ReadValue<Vector2>();
            selectedPhysicsItem.UpdateDragPosition(currentTouchPos);
        }

        // ���� ����ȭ: Ȱ�� ���� ������ ������Ʈ
        UpdateActivePhysicsItems();
    }

    private void OnDestroy()
    {
        if (touchActions != null)
        {
            touchActions.Dispose();
        }

        // Ǯ�� ������ ��ȯ
        ReturnAllItemsToPool();
    }
    #endregion

    #region Initialization
    private IEnumerator DelayedInitialization()
    {
        // ù �������� �ǳʶپ� �ٸ� �ʱ�ȭ�� �Ϸ�ǵ��� ��
        yield return null;

        // ������Ʈ Ǯ ���� �̸� �ʱ�ȭ
        if (useObjectPool && ObjectPool.Instance != null)
        {
            InitializePhysicsItemPool();
        }

        SetupInputSystem();
        isInitialized = true;
    }

    private void InitializeComponents()
    {
        if (inventoryController == null)
        {
            inventoryController = GetComponent<InventoryController>();
        }

        if (mainGrid == null && inventoryController != null)
        {
            mainGrid = inventoryController.GetComponentInChildren<ItemGrid>();
        }

        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }

        if (itemSpawnPoint == null)
        {
            // InventoryController���� spawnPoint ��������
            var field = typeof(InventoryController).GetField("itemSpawnPoint",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                itemSpawnPoint = field.GetValue(inventoryController) as Transform;
            }

            if (itemSpawnPoint == null)
            {
                Debug.LogWarning("ItemSpawnPoint not found! Creating a default one.");
                GameObject spawnObj = new GameObject("DefaultItemSpawnPoint");
                spawnObj.transform.SetParent(transform);
                spawnObj.transform.localPosition = Vector3.zero;
                itemSpawnPoint = spawnObj.transform;
            }
        }

        if (weaponPrefab == null && inventoryController != null)
        {
            // InventoryController�� weaponPrefab �ʵ� ��������
            var field = typeof(InventoryController).GetField("weaponPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                weaponPrefab = field.GetValue(inventoryController) as GameObject;
            }
        }

        mainCamera = Camera.main;
    }

    /// <summary>
    /// ���� �κ��丮 �������� ���� ������Ʈ Ǯ �ʱ�ȭ
    /// </summary>
    private void InitializePhysicsItemPool()
    {
        if (ObjectPool.Instance == null || weaponPrefab == null) return;

        // Ǯ�� �̹� �����ϴ��� Ȯ��
        if (!ObjectPool.Instance.DoesPoolExist(poolTag))
        {
            // �� Ǯ ����
            ObjectPool.Instance.CreatePool(poolTag, weaponPrefab, initialPoolSize);
            Debug.Log($"Physics inventory item pool created with {initialPoolSize} items");
        }
        else
        {
            // Ǯ ũ�Ⱑ ������� Ȯ��
            int availableCount = ObjectPool.Instance.GetAvailableCount(poolTag);
            if (availableCount < ensurePoolSize)
            {
                ObjectPool.Instance.ExpandPool(poolTag, ensurePoolSize - availableCount);
                Debug.Log($"Physics inventory item pool expanded. Added {ensurePoolSize - availableCount} items");
            }
        }
    }

    private void SetupInputSystem()
    {
        touchActions = new TouchActions();
        touchPosition = touchActions.Touch.Position;
        touchPress = touchActions.Touch.Press;

        touchPress.started += OnTouchStarted;
        touchPress.canceled += OnTouchEnded;

        touchActions.Enable();
    }
    #endregion

    #region Item Creation
    /// <summary>
    /// ���� ��� �κ��丮 ������ ���� - ������Ʈ Ǯ Ȱ��
    /// </summary>
    public InventoryItem CreatePhysicsItem(WeaponData weaponData)
    {
        if (weaponData == null || weaponPrefab == null) return null;

        // ������ ���� ��ġ ���
        Vector3 spawnPosition = itemSpawnPoint.position;
        spawnPosition += new Vector3(
            Random.Range(-physicsRandomVariance, physicsRandomVariance),
            physicsSpawnOffset + Random.Range(0, physicsRandomVariance),
            0
        );

        GameObject itemObj;

        // ������Ʈ Ǯ���� ��������
        if (useObjectPool && ObjectPool.Instance != null && ObjectPool.Instance.DoesPoolExist(poolTag))
        {
            // ���� ������Ʈ�� ������� Ȯ��
            int availableCount = ObjectPool.Instance.GetAvailableCount(poolTag);
            if (availableCount < 1)
            {
                // Ǯ Ȯ��
                ObjectPool.Instance.ExpandPool(poolTag, ensurePoolSize);
                Debug.Log($"Physics item pool expanded by {ensurePoolSize} items");
            }

            // Ǯ���� ������Ʈ ��������
            itemObj = ObjectPool.Instance.SpawnFromPool(poolTag, spawnPosition, Quaternion.identity);
        }
        else
        {
            // ������Ʈ Ǯ�� ������� �ʰų� ����� �� ���� ��� ���� ����
            itemObj = Instantiate(weaponPrefab, spawnPosition, Quaternion.identity, parentCanvas.transform);
        }

        if (itemObj == null)
        {
            Debug.LogError("Failed to create physics inventory item");
            return null;
        }

        // �κ��丮 ������ ������Ʈ ��������
        InventoryItem inventoryItem = itemObj.GetComponent<InventoryItem>();
        if (inventoryItem == null)
        {
            Debug.LogError("InventoryItem component not found on prefab");
            if (useObjectPool && ObjectPool.Instance != null)
            {
                ObjectPool.Instance.ReturnToPool(poolTag, itemObj);
            }
            else
            {
                Destroy(itemObj);
            }
            return null;
        }

        // ���� ������ �ʱ�ȭ
        inventoryItem.Initialize(weaponData);
        inventoryItem.SetGridPosition(new Vector2Int(-1, -1)); // �׸��� �ܺ� ǥ��

        // ���� ������Ʈ �߰� �Ǵ� ��������
        PhysicsInventoryItem physicsItem = itemObj.GetComponent<PhysicsInventoryItem>();
        if (physicsItem == null)
        {
            physicsItem = itemObj.AddComponent<PhysicsInventoryItem>();
        }

        // ��ġ ���� �� ���� Ȱ��ȭ
        physicsItem.SetSpawnPosition(spawnPosition);
        physicsItem.ActivatePhysics();

        // ���� ��Ͽ� �߰�
        physicsItems.Add(physicsItem);

        return inventoryItem;
    }

    /// <summary>
    /// �κ��丮 �׸��忡 �� ������ ���� �� ���� ���������� ����
    /// </summary>
    public void HandleFullInventory(WeaponData weaponData)
    {
        CreatePhysicsItem(weaponData);

        // ȿ���� ��� (�ִ� ���)
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound("ItemDrop_sfx", 0.8f, false);
        }
    }
    #endregion

    #region Touch Handling
    private void OnTouchStarted(InputAction.CallbackContext context)
    {
        // ��ġ ��ġ ��������
        Vector2 touchPos = touchPosition.ReadValue<Vector2>();
        touchStartPosition = touchPos;

        // �׸��� �� ������ ���� üũ
        Vector2Int gridPosition = mainGrid?.GetGridPosition(touchPos) ?? new Vector2Int(-1, -1);
        if (mainGrid != null && mainGrid.IsValidPosition(gridPosition))
        {
            // �׸��� �� ������ ��ġ ó���� InventoryController�� ���
            return;
        }

        // ���� ������ ��ġ üũ
        PhysicsInventoryItem touchedItem = GetPhysicsItemAtPosition(touchPos);
        if (touchedItem != null)
        {
            // ���� ������ ����
            selectedPhysicsItem = touchedItem;

            // Ȧ�� üũ ����
            if (holdCoroutine != null)
            {
                StopCoroutine(holdCoroutine);
            }
            holdCoroutine = StartCoroutine(CheckForHold(touchedItem, touchPos));
        }
    }

    private IEnumerator CheckForHold(PhysicsInventoryItem item, Vector2 startPosition)
    {
        float elapsedTime = 0f;

        while (touchPress.IsPressed())
        {
            elapsedTime += Time.deltaTime;
            Vector2 currentPos = touchPosition.ReadValue<Vector2>();
            float distance = Vector2.Distance(startPosition, currentPos);

            // Ȧ�� �ð��� �����ų�, ���� �Ÿ� �̻� �������� ���
            if (elapsedTime >= holdDelay || distance > dragThreshold)
            {
                StartDragging(item, currentPos);
                break;
            }

            yield return null;
        }

        holdCoroutine = null;
    }

    private void StartDragging(PhysicsInventoryItem item, Vector2 position)
    {
        if (item == null) return;

        isHolding = true;
        isDragging = true;
        selectedPhysicsItem = item;

        // ���� ��Ȱ��ȭ�ϰ� �巡�� ����
        item.StartDrag(position);

        // ȿ���� ��� (SoundManager�� �ִ� ���)
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound("ItemLift_sfx", 1f, false);
        }
    }

    private void OnTouchEnded(InputAction.CallbackContext context)
    {
        if (selectedPhysicsItem != null && isDragging)
        {
            Vector2 finalPosition = touchPosition.ReadValue<Vector2>();

            // �巡�� ���� ó��
            selectedPhysicsItem.EndDrag(mainGrid, finalPosition);

            // ���� �ʱ�ȭ
            ResetDragState();
        }

        // Ȧ�� �ڷ�ƾ ����
        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }
    }

    private void ResetDragState()
    {
        isHolding = false;
        isDragging = false;
        selectedPhysicsItem = null;
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Ư�� ��ġ�� �ִ� ���� ������ ã��
    /// </summary>
    private PhysicsInventoryItem GetPhysicsItemAtPosition(Vector2 screenPosition)
    {
        // ����ĳ��Ʈ ����
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // ��� ó��
        foreach (RaycastResult result in results)
        {
            PhysicsInventoryItem physicsItem = result.gameObject.GetComponent<PhysicsInventoryItem>();
            if (physicsItem != null)
            {
                return physicsItem;
            }

            // �θ� ��ü������ Ȯ��
            Transform parentTransform = result.gameObject.transform.parent;
            if (parentTransform != null)
            {
                physicsItem = parentTransform.GetComponent<PhysicsInventoryItem>();
                if (physicsItem != null)
                {
                    return physicsItem;
                }
            }
        }

        // �������� ã�� ����
        return null;
    }

    /// <summary>
    /// �׸��� ������ �巡�׵� �������� ���� ���������� ��ȯ
    /// </summary>
    public void ConvertToPhysicsItem(InventoryItem item, Vector2 position)
    {
        if (item == null) return;

        GameObject itemObj = item.gameObject;

        // �̹� PhysicsInventoryItem ������Ʈ�� �ִ��� Ȯ��
        PhysicsInventoryItem physicsItem = itemObj.GetComponent<PhysicsInventoryItem>();
        if (physicsItem == null)
        {
            // ������ �߰�
            physicsItem = itemObj.AddComponent<PhysicsInventoryItem>();
        }

        // ĵ������ �θ� ����
        itemObj.transform.SetParent(parentCanvas.transform);

        // ���� Ȱ��ȭ
        Vector2 dragVelocity = (position - touchStartPosition) * 2f;
        physicsItem.ActivatePhysics(dragVelocity);

        // ���� ��Ͽ� �߰�
        if (!physicsItems.Contains(physicsItem))
        {
            physicsItems.Add(physicsItem);
        }
    }

    /// <summary>
    /// ��� ����� ������ ���� (�ڼ� ȿ�� � ���)
    /// </summary>
    public void CollectAllItems(Transform target)
    {
        foreach (var item in physicsItems.ToList())
        {
            if (item != null && !item.IsBeingDragged)
            {
                // ��� �������� �̵��ϴ� ���� ȿ��
                Vector2 direction = (target.position - item.transform.position).normalized;
                item.ActivatePhysics(direction * 500f);
            }
        }
    }

    /// <summary>
    /// �κ��丮 �׸��忡 Ư�� �������� ���� ������ �ִ��� Ȯ��
    /// </summary>
    public bool HasSpaceForItem(WeaponData weaponData)
    {
        if (mainGrid == null || weaponData == null || weaponPrefab == null)
        {
            return false;
        }

        // Ǯ���� ������ �ӽ� �뿩 �Ǵ� �ӽ� ����
        GameObject tempObj;
        bool fromPool = false;

        if (useObjectPool && ObjectPool.Instance != null && ObjectPool.Instance.DoesPoolExist(poolTag) &&
            ObjectPool.Instance.GetAvailableCount(poolTag) > 0)
        {
            // Ǯ���� ������ �������� (ȭ�� �� ��ġ)
            tempObj = ObjectPool.Instance.SpawnFromPool(poolTag, new Vector3(-10000, -10000, 0), Quaternion.identity);
            fromPool = true;
        }
        else
        {
            // �ӽ� ������ ����
            tempObj = Instantiate(weaponPrefab);
        }

        if (tempObj == null)
        {
            return false;
        }

        InventoryItem tempItem = tempObj.GetComponent<InventoryItem>();
        if (tempItem == null)
        {
            // ��� �� ó��
            if (fromPool)
            {
                ObjectPool.Instance.ReturnToPool(poolTag, tempObj);
            }
            else
            {
                Destroy(tempObj);
            }
            return false;
        }

        // �ӽ÷� ���� ������ �ʱ�ȭ
        tempItem.Initialize(weaponData);

        // �׸��忡�� ���� ã��
        Vector2Int? freePosition = mainGrid.FindSpaceForObject(tempItem);

        // ��� �� ������ ó��
        if (fromPool)
        {
            ObjectPool.Instance.ReturnToPool(poolTag, tempObj);
        }
        else
        {
            Destroy(tempObj);
        }

        return freePosition.HasValue;
    }

    /// <summary>
    /// ȭ�� �� ������ ���� ��Ȱ��ȭ �� ���� ����ȭ
    /// </summary>
    private void UpdateActivePhysicsItems()
    {
        if (mainCamera == null || physicsItems.Count == 0) return;

        // ȭ�� ��� ���
        Vector2 screenBounds = new Vector2(Screen.width, Screen.height);

        // ȭ�� �� ���� (�������� ������ ȭ�� ������ ��������)
        float margin = 100f;

        foreach (var item in physicsItems)
        {
            if (item == null || item.IsBeingDragged) continue;

            // ������ ��ġ Ȯ��
            Vector2 viewportPosition = mainCamera.WorldToScreenPoint(item.transform.position);

            // ȭ�鿡�� �ʹ� �ָ� ���������� üũ
            bool isFarOutside =
                viewportPosition.x < -margin ||
                viewportPosition.x > screenBounds.x + margin ||
                viewportPosition.y < -margin ||
                viewportPosition.y > screenBounds.y + margin;

            // �ʹ� �ָ� �ִ� �������� Ǯ�� ��ȯ
            if (isFarOutside)
            {
                ReturnItemToPool(item);
            }
        }

        // ���� ���� ����
        physicsItems.RemoveAll(item => item == null);
    }

    /// <summary>
    /// �������� Ǯ�� ��ȯ
    /// </summary>
    private void ReturnItemToPool(PhysicsInventoryItem item)
    {
        if (item == null) return;

        physicsItems.Remove(item);

        // ���� ��Ȱ��ȭ
        item.DeactivatePhysics();

        // Ǯ�� ��ȯ
        if (useObjectPool && ObjectPool.Instance != null)
        {
            ObjectPool.Instance.ReturnToPool(poolTag, item.gameObject);
        }
        else
        {
            Destroy(item.gameObject);
        }
    }

    /// <summary>
    /// ��� �������� Ǯ�� ��ȯ
    /// </summary>
    private void ReturnAllItemsToPool()
    {
        if (!useObjectPool || ObjectPool.Instance == null) return;

        foreach (var item in physicsItems.ToList())
        {
            if (item != null)
            {
                ReturnItemToPool(item);
            }
        }

        physicsItems.Clear();
    }
    #endregion
}