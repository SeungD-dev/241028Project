using UnityEngine;
using System;

/// <summary>
/// �׸��� ��� �κ��丮 �ý����� �����ϴ� Ŭ����
/// �������� ��ġ, �̵�, ������ ó��
/// </summary>
public class ItemGrid : MonoBehaviour
{
    #region Constants
    public const float TILE_SIZE = 32f;
    #endregion

    #region Serialized Fields
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 4;
    [SerializeField] private int gridHeight = 4;
    #endregion

    #region Events
    public event Action<InventoryItem> OnItemAdded;
    public event Action<InventoryItem> OnItemRemoved;
    public event Action OnGridChanged;
    #endregion

    #region Properties
    public int Width => gridWidth;
    public int Height => gridHeight;
    public bool IsInitialized => isInitialized;
    #endregion

    #region Private Fields
    private InventoryItem[,] gridItems;
    private RectTransform rectTransform;
    private bool isInitialized = false;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        InitializeComponents();
        InitializeGrid();
    }

    private void OnEnable()
    {
        if (!isInitialized)
        {
            InitializeGrid();
        }
        ValidateGridState();
    }
    #endregion

    #region Initialization
    private void InitializeComponents()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("RectTransform component missing!");
            return;
        }
    }

    private void InitializeGrid()
    {
        try
        {
            // gridItems�� null�̰ų� ũ�Ⱑ �ٸ��� ���� �ʱ�ȭ
            if (gridItems == null || gridItems.GetLength(0) != gridWidth || gridItems.GetLength(1) != gridHeight)
            {
                gridItems = new InventoryItem[gridWidth, gridHeight];
                Debug.Log($"Grid initialized with size: {gridWidth}x{gridHeight}");
            }
            UpdateGridSize();
            isInitialized = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error initializing grid: {e.Message}");
            isInitialized = false;
        }
    }
    private void UpdateGridSize()
    {
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(
                gridWidth * TILE_SIZE,
                gridHeight * TILE_SIZE
            );
        }
    }
    #endregion

    #region Grid Operations
    /// <summary>
    /// ��ũ�� ��ǥ�� �׸��� ��ǥ�� ��ȯ
    /// </summary>
    public Vector2Int GetGridPosition(Vector2 screenPosition)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        Vector2 gridTopLeft = corners[1];

        Vector2 positionInGrid = screenPosition - gridTopLeft;
        positionInGrid /= rectTransform.lossyScale.x;

        return new Vector2Int(
            Mathf.FloorToInt(positionInGrid.x / TILE_SIZE),
            Mathf.FloorToInt(-positionInGrid.y / TILE_SIZE)
        );
    }

    /// <summary>
    /// �������� ������ ��ġ�� ��ġ
    /// </summary>
    public bool PlaceItem(InventoryItem item, Vector2Int position)
    {
        if (!CanPlaceItem(item, position)) return false;

        // ���� ������ ����
        CleanupItemReferences(item);

        // �� ��ġ�� ������ ��ġ
        for (int x = 0; x < item.Width; x++)
        {
            for (int y = 0; y < item.Height; y++)
            {
                Vector2Int gridPos = position + new Vector2Int(x, y);
                gridItems[gridPos.x, gridPos.y] = item;
            }
        }

        item.SetGridPosition(position);
        UpdateItemVisualPosition(item, position);

        OnItemAdded?.Invoke(item);
        OnGridChanged?.Invoke();

        return true;
    }

    // �����ε�� �޼����
    public bool PlaceItem(InventoryItem item, int x, int y)
    {
        return PlaceItem(item, new Vector2Int(x, y));
    }

    public bool PlaceItem(InventoryItem item, Vector2Int position, ref InventoryItem overlapItem)
    {
        overlapItem = GetItem(position);
        return PlaceItem(item, position);
    }

    public bool PlaceItem(InventoryItem item, int x, int y, ref InventoryItem overlapItem)
    {
        return PlaceItem(item, new Vector2Int(x, y), ref overlapItem);
    }
    /// <summary>
    /// ������ ��ġ���� ������ ����
    /// </summary>
    public InventoryItem RemoveItem(Vector2Int position)
    {
        InventoryItem item = GetItem(position);
        if (item == null) return null;

        // �������� �����ϴ� ��� ĭ���� ���� ����
        for (int x = 0; x < item.Width; x++)
        {
            for (int y = 0; y < item.Height; y++)
            {
                Vector2Int gridPos = position + new Vector2Int(x, y);
                if (IsValidPosition(gridPos))
                {
                    gridItems[gridPos.x, gridPos.y] = null;
                }
            }
        }

        OnItemRemoved?.Invoke(item);
        OnGridChanged?.Invoke();

        return item;
    }

    public InventoryItem PickUpItem(Vector2Int position)
    {
        InventoryItem item = GetItem(position.x, position.y);
        if (item == null) return null;

        RemoveItem(position);
        return item;
    }

    /// <summary>
    /// �������� ��ġ�� �� �ִ��� �˻�
    /// </summary>
    public bool CanPlaceItem(InventoryItem item, Vector2Int position)
    {
        if (!isInitialized)
        {
            Debug.LogError("Grid is not initialized!");
            return false;
        }

        if (item == null)
        {
            Debug.LogError("Attempted to check placement for null item!");
            return false;
        }

        try
        {
            // �׸��� ���� üũ
            if (position.x < 0 || position.y < 0 ||
                position.x + item.Width > gridWidth ||
                position.y + item.Height > gridHeight)
            {
                return false;
            }

            // gridItems null üũ
            if (gridItems == null)
            {
                Debug.LogError("gridItems array is null!");
                return false;
            }

            // ��ħ üũ
            for (int x = 0; x < item.Width; x++)
            {
                for (int y = 0; y < item.Height; y++)
                {
                    Vector2Int checkPos = position + new Vector2Int(x, y);
                    if (gridItems[checkPos.x, checkPos.y] != null)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in CanPlaceItem: {e.Message}\nStackTrace: {e.StackTrace}");
            return false;
        }
    }
    public bool CanPlaceItem(InventoryItem item, int x, int y)
    {
        return CanPlaceItem(item, new Vector2Int(x, y));
    }
    /// <summary>
    /// �׸��� �� �� ���� ã��
    /// </summary>
    public Vector2Int? FindSpaceForObject(InventoryItem item)
    {
        if (item == null)
        {
            Debug.LogError("Attempted to find space for null item!");
            return null;
        }

        try
        {
            for (int y = 0; y < gridHeight - item.Height + 1; y++)
            {
                for (int x = 0; x < gridWidth - item.Width + 1; x++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    if (CanPlaceItem(item, position))
                    {
                        return position;
                    }
                }
            }

            Debug.Log("No free space found in grid");
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in FindSpaceForObject: {e.Message}");
            return null;
        }
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// ������ ��ġ�� ������ ��ȯ
    /// </summary>
    /// 
    public void ForceInitialize()
    {
        InitializeComponents();
        InitializeGrid();
    }
    public InventoryItem GetItem(Vector2Int position)
    {
        return GetItem(position.x, position.y);
    }

    public InventoryItem GetItem(int x, int y)
    {
        if (!IsValidPosition(new Vector2Int(x, y)))
        {
            return null;
        }
        return gridItems[x, y];
    }

    /// <summary>
    /// ��ġ�� �׸��� ���� ������ Ȯ��
    /// </summary>
    public bool IsValidPosition(Vector2Int position)
    {
        return position.x >= 0 && position.x < gridWidth &&
               position.y >= 0 && position.y < gridHeight;
    }

    /// <summary>
    /// �������� �׸���� ��ġ ���
    /// </summary>
    public Vector2 CalculatePositionOnGrid(InventoryItem item, int posX, int posY)
    {
        if (item == null) return Vector2.zero;

        return new Vector2(
            posX * TILE_SIZE + (item.Width * TILE_SIZE / 2),
            -(posY * TILE_SIZE + (item.Height * TILE_SIZE / 2))
        );
    }

    /// <summary>
    /// �׸��� ���� ����
    /// </summary>
    public void ValidateGridState()
    {
        if (!IsInitialized)
        {
            ForceInitialize();
        }

        try
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    InventoryItem item = gridItems[x, y];
                    if (item != null)
                    {
                        Vector2Int itemPos = item.GridPosition;
                        // �������� ��ġ�� ��ȿ���� ������ ���ġ �õ�
                        if (!IsValidPosition(itemPos))
                        {
                            Vector2Int? newPos = FindSpaceForObject(item);
                            if (newPos.HasValue)
                            {
                                PlaceItem(item, newPos.Value);
                            }
                            else
                            {
                                gridItems[x, y] = null;
                            }
                        }
                    }
                }
            }
            OnGridChanged?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in ValidateGridState: {e.Message}");
        }
    }

    private void CleanupItemReferences(InventoryItem item)
    {
        if (item == null) return;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (gridItems[x, y] == item)
                {
                    gridItems[x, y] = null;
                }
            }
        }
    }

    private void UpdateItemVisualPosition(InventoryItem item, Vector2Int position)
    {
        if (item == null) return;

        RectTransform itemRect = item.GetComponent<RectTransform>();
        if (itemRect != null)
        {
            Vector2 gridPosition = CalculatePositionOnGrid(item, position.x, position.y);
            itemRect.localPosition = gridPosition;
        }
    }
    #endregion



    #region Debug Methods
    /// <summary>
    /// �׸��� ���� ����� ���
    /// </summary>
    public void DebugPrintGrid()
    {
        string debug = "Grid State:\n";
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                debug += gridItems[x, y] != null ? "X " : "- ";
            }
            debug += "\n";
        }
        Debug.Log(debug);
    }
    #endregion
}