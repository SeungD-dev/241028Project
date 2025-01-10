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
    #endregion

    #region Private Fields
    private InventoryItem[,] gridItems;
    private RectTransform rectTransform;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        InitializeComponents();
        InitializeGrid();
    }

    private void OnEnable()
    {
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
        gridItems = new InventoryItem[gridWidth, gridHeight];
        UpdateGridSize();
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
        if (!IsValidPosition(position) || item == null) return false;

        // �������� �׸��� ������ ������� �˻�
        if (position.x + item.Width > gridWidth ||
            position.y + item.Height > gridHeight)
        {
            return false;
        }

        // �ٸ� �����۰� ��ġ���� �˻�
        for (int x = 0; x < item.Width; x++)
        {
            for (int y = 0; y < item.Height; y++)
            {
                Vector2Int checkPos = position + new Vector2Int(x, y);
                InventoryItem existingItem = gridItems[checkPos.x, checkPos.y];

                if (existingItem != null && existingItem != item)
                {
                    return false;
                }
            }
        }

        return true;
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
        if (item == null) return null;

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

        return null;
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// ������ ��ġ�� ������ ��ȯ
    /// </summary>
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
    private void ValidateGridState()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                InventoryItem item = gridItems[x, y];
                if (item != null)
                {
                    Vector2Int itemPos = new Vector2Int(item.GridPosition.x, item.GridPosition.y);
                    if (!IsValidPosition(itemPos))
                    {
                        gridItems[x, y] = null;
                        OnGridChanged?.Invoke();
                    }
                }
            }
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