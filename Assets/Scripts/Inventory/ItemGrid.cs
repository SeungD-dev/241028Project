using System.Collections.Generic;
using UnityEngine;

public class ItemGrid : MonoBehaviour
{
    public static float tileSizeWidth = 32f;
    public static float tileSizeHeight = 32f;

    InventoryItem[,] inventoryItemSlot;

    RectTransform rectTransform;

    [SerializeField] int gridSizeWidth;
    [SerializeField] int gridSizeHeight;
    public int Width => gridSizeWidth;
    public int Height => gridSizeHeight;

    public System.Action<InventoryItem> OnItemAdded;
    public System.Action<InventoryItem> OnItemRemoved;
    public System.Action OnGridChanged;

    public void NotifyGridChanged()
    {
        OnGridChanged?.Invoke();
    }
    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        Init(gridSizeWidth, gridSizeHeight);
    }

    private void OnEnable()
    {
        // UI�� Ȱ��ȭ�� ������ RectTransform Ȯ��
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            Init(gridSizeWidth, gridSizeHeight);
        }
    }

    private void Init(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            Debug.LogError($"Invalid grid size: {width}x{height}");
            return;
        }

        inventoryItemSlot = new InventoryItem[width, height];
        Vector2 size = new Vector2(width * tileSizeWidth, height * tileSizeHeight);
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = size;
        }
        else
        {
            Debug.LogError("RectTransform is null!");
        }
    }
    public Vector2Int GetTileGridPosition(Vector2 touchPosition)
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                Debug.LogError($"RectTransform still null on {gameObject.name}");
                return Vector2Int.zero;
            }
        }

        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        Vector2 gridTopLeft = corners[1];
        float scale = rectTransform.localScale.x;

        Vector2 positionFromTopLeft = touchPosition - gridTopLeft;
        positionFromTopLeft += new Vector2(tileSizeWidth * scale * 0.5f, tileSizeHeight * scale * 0.5f);
        Vector2 positionOnTheGrid = positionFromTopLeft / scale;

        Vector2Int rawGridPosition = new Vector2Int(
            Mathf.FloorToInt(positionOnTheGrid.x / tileSizeWidth),
            Mathf.FloorToInt(-positionOnTheGrid.y / tileSizeHeight)
        );

        // ��ġ�� ��ġ���� �������� ã��
        InventoryItem touchedItem = GetItemAtPosition(rawGridPosition);
        if (touchedItem != null)
        {
            // �������� ���� ��ġ�� ��ȯ
            return new Vector2Int(touchedItem.onGridPositionX, touchedItem.onGridPositionY);
        }

        return rawGridPosition;
    }

    private InventoryItem GetItemAtPosition(Vector2Int position)
    {
        // ��� üũ
        if (position.x < 0 || position.y < 0 || position.x >= gridSizeWidth || position.y >= gridSizeHeight)
        {
            return null;
        }

        return inventoryItemSlot[position.x, position.y];
    }

    public InventoryItem PickUpItem(int x, int y)
    {
        InventoryItem toReturn = inventoryItemSlot[x, y];

        if (toReturn == null) return null;

        // ������ ���� ����
        CleanupItemReferences(toReturn);

        // �Ⱦ� �� �׸��� ���� ����
        ValidateGridState();

        OnItemRemoved?.Invoke(toReturn);
        NotifyGridChanged();

        return toReturn;
    }

    public bool PlaceItem(InventoryItem inventoryItem, int posX, int posY, ref InventoryItem overlapItem)
    {
        if (!BoundryCheck(posX, posY, inventoryItem.WIDTH, inventoryItem.HEIGHT))
        {
            return false;
        }

        // ���� ���� ����
        CleanupItemReferences(inventoryItem);

        if (!OverlapCheck(posX, posY, inventoryItem.WIDTH, inventoryItem.HEIGHT, ref overlapItem))
        {
            return false;
        }

        if (overlapItem != null && overlapItem != inventoryItem)
        {
            CleanupItemReferences(overlapItem);
        }

        // ���� ��ġ�� private �޼��忡 ����
        PlaceItem(inventoryItem, posX, posY);

        // ��ġ �� �׸��� ���� ����
        ValidateGridState();

        return true;
    }

    private void PlaceItem(InventoryItem inventoryItem, int posX, int posY)
    {
        RectTransform rectTransform = inventoryItem.GetComponent<RectTransform>();
        rectTransform.SetParent(this.rectTransform);

        for (int x = 0; x < inventoryItem.WIDTH; x++)
        {
            for (int y = 0; y < inventoryItem.HEIGHT; y++)
            {
                inventoryItemSlot[posX + x, posY + y] = inventoryItem;
            }
        }

        inventoryItem.onGridPositionX = posX;
        inventoryItem.onGridPositionY = posY;

        Vector2 position = CalculatePositionOnGrid(inventoryItem, posX, posY);
        rectTransform.localPosition = position;

        OnItemAdded?.Invoke(inventoryItem);
        NotifyGridChanged();
    }
    public Vector2 CalculatePositionOnGrid(InventoryItem inventoryItem, int posX, int posY)
    {
        Vector2 position = new Vector2();


        position.x = posX * tileSizeWidth + (tileSizeWidth * inventoryItem.WIDTH / 2);
        position.y = -(posY * tileSizeHeight + (tileSizeHeight * inventoryItem.HEIGHT / 2));

        return position;
    }
    private bool OverlapCheck(int posX, int posY, int width, int height, ref InventoryItem overlapItem)
    {
        // ��� üũ
        if (!BoundryCheck(posX, posY, width, height))
        {
            return false;
        }

        overlapItem = null;
        bool hasOverlap = false;

        // �������� �������� ���� ��ü�� �˻�
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                InventoryItem currentSlot = inventoryItemSlot[posX + x, posY + y];

                if (currentSlot != null)
                {
                    // ù ��° ��ħ�� �߰��� ���
                    if (!hasOverlap)
                    {
                        overlapItem = currentSlot;
                        hasOverlap = true;
                    }
                    // �ٸ� �����۰� ��ġ�� ���
                    else if (currentSlot != overlapItem)
                    {
                        overlapItem = null;
                        return false;
                    }
                }
            }
        }

        // hasOverlap�� false�� ��� = ������ �� ����
        // hasOverlap�� true�� ��� = ���� �����۰��� ��ħ
        return true;
    }
    private bool CheckAvailableSpace(int posX, int posY, int width, int height)
    {
        // ��� üũ
        if (!BoundryCheck(posX, posY, width, height))
        {
            return false;
        }

        // �ش� ������ ��� ĭ�� ����ִ��� Ȯ��
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // ���� �˻��ϴ� ��ġ
                int checkX = posX + x;
                int checkY = posY + y;

                // �߰� ��� üũ
                if (checkX >= gridSizeWidth || checkY >= gridSizeHeight)
                {
                    return false;
                }

                // �ٸ� �������� �ִ��� Ȯ��
                if (inventoryItemSlot[checkX, checkY] != null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public void ValidateGridState()
    {
        // ��� �������� ������ �ӽ÷� ����
        HashSet<InventoryItem> processedItems = new HashSet<InventoryItem>();
        List<InventoryItem> invalidItems = new List<InventoryItem>();

        // ��ü �׸��� ��ȸ�ϸ鼭 ���� üũ
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                InventoryItem item = inventoryItemSlot[x, y];
                if (item != null)
                {
                    // �������� ��ϵ� ��ġ�� ���� ��ġ�� ��ġ�ϴ��� Ȯ��
                    if (item.onGridPositionX != x || item.onGridPositionY != y)
                    {
                        // �̹� �ٸ� ��ġ���� �߰ߵ� �������� ���
                        if (!processedItems.Contains(item))
                        {
                            invalidItems.Add(item);
                        }
                    }
                    processedItems.Add(item);
                }
            }
        }

        // �߸��� ������ ���� �����۵� ó��
        foreach (var item in invalidItems)
        {
            CleanupItemReferences(item);
        }
    }

    private void CleanupItemReferences(InventoryItem item)
    {
        if (item == null) return;

        // ��ü �׸��忡�� �ش� �������� ���� ����
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (inventoryItemSlot[x, y] == item)
                {
                    inventoryItemSlot[x, y] = null;
                }
            }
        }
    }
    public bool BoundryCheck(int posX, int posY, int width, int height)
    {
        if (posX < 0 || posY < 0)
        {
            //Debug.Log($"BoundryCheck failed: position ({posX}, {posY}) is negative");
            return false;
        }

        if (posX + width > gridSizeWidth || posY + height > gridSizeHeight)
        {
            //Debug.Log($"BoundryCheck failed: item size {width}x{height} at ({posX}, {posY}) exceeds grid bounds {gridSizeWidth}x{gridSizeHeight}");
            return false;
        }

        return true;
    }

    public InventoryItem GetItem(int x, int y)
    {
        if (inventoryItemSlot == null)
        {
            //Debug.LogError($"inventoryItemSlot is null! Grid might not be initialized properly.");
            return null;
        }

        // �⺻ ��� üũ
        if (x < 0 || y < 0 || x >= gridSizeWidth || y >= gridSizeHeight)
        {
            //Debug.Log($"GetItem: Position ({x}, {y}) is outside grid bounds {gridSizeWidth}x{gridSizeHeight}");
            return null;
        }

        InventoryItem item = inventoryItemSlot[x, y];
        if (item == null)
        {
            return null;
        }

        // �������� ���� ��ġ�� ��ȿ���� Ȯ��
        if (item.onGridPositionX < 0 || item.onGridPositionY < 0 ||
            item.onGridPositionX >= gridSizeWidth || item.onGridPositionY >= gridSizeHeight)
        {
            //Debug.LogWarning($"Item at ({x}, {y}) has invalid origin position: ({item.onGridPositionX}, {item.onGridPositionY})");
            return null;
        }

        // �������� ũ�Ⱑ �׸��带 ������� Ȯ��
        if (!BoundryCheck(item.onGridPositionX, item.onGridPositionY, item.WIDTH, item.HEIGHT))
        {
           // Debug.LogWarning($"Item at ({x}, {y}) extends beyond grid bounds");
            return null;
        }

        // ���� ��ġ�� �������� ��ȯ
        return inventoryItemSlot[item.onGridPositionX, item.onGridPositionY];
    }
    public Vector2Int? FindSpaceForObject(InventoryItem itemToInsert)
    {
        // itemToInsert�� ũ�Ⱑ ��ȿ���� ���� Ȯ��
        if (itemToInsert.WIDTH <= 0 || itemToInsert.HEIGHT <= 0)
        {
           // Debug.LogError($"Invalid item size: {itemToInsert.WIDTH}x{itemToInsert.HEIGHT}");
            return null;
        }

        // �׸��� ������ �������� �� �� �ִ� �ִ� ���� ���
        int maxY = gridSizeHeight - itemToInsert.HEIGHT + 1;
        int maxX = gridSizeWidth - itemToInsert.WIDTH + 1;

        if (maxX <= 0 || maxY <= 0)
        {
            //Debug.LogWarning($"Item size {itemToInsert.WIDTH}x{itemToInsert.HEIGHT} is too large for grid {gridSizeWidth}x{gridSizeHeight}");
            return null;
        }

        // ���� ��ܺ��� ���������� �˻�
        for (int y = 0; y < maxY; y++)
        {
            for (int x = 0; x < maxX; x++)
            {
                // �� ��ġ���� �������� ��ü ������ ����ִ��� Ȯ��
                if (CheckAvailableSpace(x, y, itemToInsert.WIDTH, itemToInsert.HEIGHT))
                {
                    return new Vector2Int(x, y);
                }
            }
        }

        return null;
    }
} 
