using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// �κ��丮 �����ۿ� ������ Ư���� �߰��ϴ� ������Ʈ
/// �׸���� ������ ȯ�� ������ ��ȯ�� �����մϴ�.
/// ������Ʈ Ǯ���� �����մϴ�.
/// </summary>
public class PhysicsInventoryItem : MonoBehaviour, IPooledObject
{
    #region Serialized Fields
    [Header("Physics Settings")]
    [SerializeField] private float gravityScale = 980f;
    [SerializeField] private float dragDamping = 0.92f;
    [SerializeField] private float bounceMultiplier = 0.4f;
    [SerializeField] private float groundFriction = 0.8f;
    [SerializeField] private float minimumVelocity = 10f;
    [SerializeField] private Vector2 initialImpulse = new Vector2(0f, 150f);

    [Header("References")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image itemImage;

    [Header("Pool Settings")]
    [SerializeField] private bool useObjectPool = true;
    [SerializeField] private string poolTag = "PhysicsInventoryItem";
    #endregion

    #region Private Fields
    private InventoryItem inventoryItem;
    private Canvas parentCanvas;
    private RectTransform canvasRectTransform;
    private Vector2 velocity;
    private bool isPhysicsActive = false;
    private bool isBeingDragged = false;
    private Vector2 screenBounds;
    private Vector2 lastTouchPosition;
    private float itemLiftOffset = 350f; // InventoryController�� ������ ��
    private Vector2Int originalGridPosition;
    private bool isInitialized = false;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private Vector3 spawnPosition;
    #endregion

    #region Properties
    public bool IsPhysicsActive => isPhysicsActive;
    public bool IsBeingDragged => isBeingDragged;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        Initialize();
    }

    private void Update()
    {
        if (!isInitialized)
        {
            Initialize();
            return;
        }

        if (isPhysicsActive && !isBeingDragged)
        {
            UpdatePhysics();
        }
    }

    private void OnDisable()
    {
        // Ǯ�� ��ȯ�� �� ���� �ʱ�ȭ
        isPhysicsActive = false;
        isBeingDragged = false;
        velocity = Vector2.zero;
    }

    /// <summary>
    /// IPooledObject �������̽��� OnObjectSpawn ����
    /// ������Ʈ Ǯ���� ������ �� ȣ���
    /// </summary>
    public void OnObjectSpawn()
    {
        // ���� �ʱ�ȭ
        isPhysicsActive = false;
        isBeingDragged = false;
        velocity = Vector2.zero;

        // �ʱ�ȭ Ȯ��
        if (!isInitialized)
        {
            Initialize();
        }

        // ���� ���� ����
        if (rectTransform != null)
        {
            originalScale = rectTransform.localScale;
            originalRotation = rectTransform.localRotation;
        }

        // �κ��丮 ������ ������Ʈ ��������
        if (inventoryItem != null)
        {
            originalGridPosition = inventoryItem.GridPosition;
        }
    }
    #endregion

    #region Initialization
    private void Initialize()
    {
        // ������Ʈ ���� ����
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (itemImage == null) itemImage = GetComponent<Image>();
        inventoryItem = GetComponent<InventoryItem>();

        // ĵ���� ���� ����
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            canvasRectTransform = parentCanvas.GetComponent<RectTransform>();
            UpdateScreenBounds();
        }

        // ���� ���� ���
        originalScale = rectTransform.localScale;
        originalRotation = rectTransform.localRotation;
        if (inventoryItem != null)
        {
            originalGridPosition = inventoryItem.GridPosition;
        }

        isInitialized = true;
    }
    #endregion

    #region Physics Methods
    /// <summary>
    /// ���� �ùķ��̼��� Ȱ��ȭ�ϰ� �ʱ� �ӵ��� �����մϴ�.
    /// </summary>
    /// <param name="initialVelocity">�ʱ� �ӵ� (������ �⺻�� ���)</param>
    public void ActivatePhysics(Vector2? initialVelocity = null)
    {
        if (!isInitialized) Initialize();

        isPhysicsActive = true;
        velocity = initialVelocity ?? initialImpulse;
        UpdateScreenBounds();

        // ������ Ȱ��ȭ�Ǹ� �θ� ĵ������ �����Ͽ� �׸��忡�� �и�
        if (canvasRectTransform != null)
        {
            rectTransform.SetParent(canvasRectTransform, true);
        }

        // �������� �׸��忡 �־��ٸ� �׸��� ��ġ �ʱ�ȭ
        if (inventoryItem != null && inventoryItem.OnGrid)
        {
            inventoryItem.SetGridPosition(new Vector2Int(-1, -1));
        }
    }

    /// <summary>
    /// ���� �ùķ��̼��� ��Ȱ��ȭ�մϴ�.
    /// </summary>
    public void DeactivatePhysics()
    {
        isPhysicsActive = false;
        velocity = Vector2.zero;
    }

    /// <summary>
    /// ���� ���¸� �� ������ ������Ʈ�մϴ�.
    /// </summary>
    private void UpdatePhysics()
    {
        // �߷� ����
        velocity += Vector2.down * gravityScale * Time.deltaTime;

        // ��ġ ������Ʈ
        rectTransform.position += (Vector3)velocity * Time.deltaTime;

        // ��� �浹 �˻�
        CheckBoundaryCollisions();

        // �ӵ��� Ư�� �� ���ϸ� ���� (���� ����ȭ)
        if (velocity.sqrMagnitude < minimumVelocity * minimumVelocity)
        {
            velocity = Vector2.zero;
        }
    }

    /// <summary>
    /// ȭ�� ������ �浹�� üũ�ϰ� �����մϴ�.
    /// </summary>
    private void CheckBoundaryCollisions()
    {
        if (canvasRectTransform == null) return;

        // �̹��� ũ�� ���
        Vector2 itemHalfSize = rectTransform.sizeDelta * rectTransform.localScale / 2;
        Vector2 itemPosition = rectTransform.position;

        // ȭ�� ���� ��� �浹
        if (itemPosition.x + itemHalfSize.x > screenBounds.x)
        {
            rectTransform.position = new Vector3(screenBounds.x - itemHalfSize.x, rectTransform.position.y, rectTransform.position.z);
            velocity.x = -velocity.x * bounceMultiplier;
        }
        // ȭ�� ���� ��� �浹
        else if (itemPosition.x - itemHalfSize.x < -screenBounds.x)
        {
            rectTransform.position = new Vector3(-screenBounds.x + itemHalfSize.x, rectTransform.position.y, rectTransform.position.z);
            velocity.x = -velocity.x * bounceMultiplier;
        }

        // ȭ�� �ϴ� ��� �浹
        if (itemPosition.y - itemHalfSize.y < -screenBounds.y)
        {
            rectTransform.position = new Vector3(rectTransform.position.x, -screenBounds.y + itemHalfSize.y, rectTransform.position.z);

            // �ٴڿ� ������ Y �ӵ� ���� �� ����, X �ӵ��� ������ ����
            velocity.y = -velocity.y * bounceMultiplier;
            velocity.x *= groundFriction;

            // Y �ӵ��� ���� �� �߰� ���� (�ٴڿ��� ���� ����)
            if (Mathf.Abs(velocity.y) < 100f)
            {
                velocity.y *= 0.5f;
            }
        }

        // ȭ�� ��� ��� �浹 (�ʿ��� ���)
        else if (itemPosition.y + itemHalfSize.y > screenBounds.y)
        {
            rectTransform.position = new Vector3(rectTransform.position.x, screenBounds.y - itemHalfSize.y, rectTransform.position.z);
            velocity.y = -velocity.y * bounceMultiplier;
        }
    }

    /// <summary>
    /// ȭ�� ��� ������ ������Ʈ�մϴ�.
    /// </summary>
    private void UpdateScreenBounds()
    {
        if (canvasRectTransform != null)
        {
            // ĵ������ ��� ���
            Vector3[] corners = new Vector3[4];
            canvasRectTransform.GetWorldCorners(corners);

            // ���ϴܰ� ���� �ڳ� �������� ��� ���
            Vector3 bottomLeft = corners[0];
            Vector3 topRight = corners[2];

            // �߾� ���� ȭ�� ũ���� ���� ��
            screenBounds = new Vector2(
                (topRight.x - bottomLeft.x) * 0.5f,
                (topRight.y - bottomLeft.y) * 0.5f
            );
        }
    }
    #endregion

    #region Interaction Methods
    /// <summary>
    /// ������ �巡�� ����
    /// </summary>
    public void StartDrag(Vector2 touchPosition)
    {
        if (!isInitialized) Initialize();

        isBeingDragged = true;
        lastTouchPosition = touchPosition;

        // ���� ũ��� ȸ������ ����
        rectTransform.localScale = originalScale;
        rectTransform.localRotation = originalRotation;

        // ���� ȿ�� ��Ȱ��ȭ
        DeactivatePhysics();
    }

    /// <summary>
    /// ������ �巡�� �� ��ġ ������Ʈ
    /// </summary>
    public void UpdateDragPosition(Vector2 touchPosition)
    {
        if (!isBeingDragged) return;

        lastTouchPosition = touchPosition;
        Vector2 liftedPosition = touchPosition + Vector2.up * itemLiftOffset;
        rectTransform.position = liftedPosition;
    }

    /// <summary>
    /// ������ �巡�� ����
    /// </summary>
    public void EndDrag(ItemGrid targetGrid, Vector2 finalPosition)
    {
        isBeingDragged = false;

        if (targetGrid != null)
        {
            // �׸��� ��ġ Ȯ��
            Vector2Int gridPosition = targetGrid.GetGridPosition(finalPosition);

            // ��ȿ�� �׸��� ��ġ�̰� ��ġ �����ϸ� �׸��忡 ��ġ
            if (targetGrid.IsValidPosition(gridPosition) &&
                targetGrid.CanPlaceItem(inventoryItem, gridPosition))
            {
                // �׸��忡 ��ġ
                rectTransform.SetParent(targetGrid.transform, false);
                targetGrid.PlaceItem(inventoryItem, gridPosition);
                DeactivatePhysics();
                return;
            }
        }

        // ��ȿ���� ���� ��ġ�� ���� Ȱ��ȭ
        Vector2 dragVelocity = (finalPosition - lastTouchPosition) * 5f; // �巡�� �������� �ణ�� �ʱ� �ӵ�
        ActivatePhysics(dragVelocity);
    }

    /// <summary>
    /// ������ ȸ��
    /// </summary>
    public void Rotate()
    {
        if (inventoryItem != null)
        {
            inventoryItem.Rotate();
        }
    }

    /// <summary>
    /// ���� ��ġ ����
    /// </summary>
    public void SetSpawnPosition(Vector3 position)
    {
        spawnPosition = position;
        rectTransform.position = position;
    }

    /// <summary>
    /// ������Ʈ Ǯ�� ������ ��ȯ
    /// </summary>
    public void ReturnToPool()
    {
        if (!useObjectPool || ObjectPool.Instance == null) return;

        // ���� ��Ȱ��ȭ
        DeactivatePhysics();

        // Ǯ�� ��ȯ
        ObjectPool.Instance.ReturnToPool(poolTag, gameObject);
    }
    #endregion
}