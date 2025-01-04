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

    private void NotifyGridChanged()
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

    Vector2 positionOnTheGrid = new Vector2();
    Vector2Int tileGridPosition = new Vector2Int();

    public Vector2Int GetTileGridPosition(Vector2 touchPosition, Vector2 itemSize = default)
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
        float scale = rectTransform.localScale.x;  // 6

        Vector2 positionFromTopLeft = touchPosition - gridTopLeft;

        // scale�� ���ؼ� ����
        positionFromTopLeft += new Vector2(tileSizeWidth * scale * 0.5f, tileSizeHeight * scale * 0.5f);

        Vector2 positionOnTheGrid = positionFromTopLeft / scale;

        return new Vector2Int(
            Mathf.FloorToInt(positionOnTheGrid.x / tileSizeWidth),
            Mathf.FloorToInt(-positionOnTheGrid.y / tileSizeHeight)
        );
    }
    public InventoryItem PickUpItem(int x, int y)
    {
        InventoryItem toReturn = inventoryItemSlot[x, y];

        if (toReturn == null) { return null; }

        CleanGridReference(toReturn);

        return toReturn;
    }

    private void CleanGridReference(InventoryItem item)
    {
        if (!BoundryCheck(item.onGridPositionX, item.onGridPositionY, item.WIDTH, item.HEIGHT))
        {
            Debug.LogWarning($"Attempted to clean grid reference outside bounds: pos({item.onGridPositionX}, {item.onGridPositionY}), size({item.WIDTH}, {item.HEIGHT})");
            return;
        }

        for (int ix = 0; ix < item.WIDTH; ix++)
        {
            for (int iy = 0; iy < item.HEIGHT; iy++)
            {
                if (item.onGridPositionX + ix < gridSizeWidth &&
                    item.onGridPositionY + iy < gridSizeHeight)
                {
                    inventoryItemSlot[item.onGridPositionX + ix, item.onGridPositionY + iy] = null;
                }
            }
        }

        OnItemRemoved?.Invoke(item);
        NotifyGridChanged();
    }

    public bool PlaceItem(InventoryItem inventoryItem, int posX, int posY, ref InventoryItem overlapItem)
    {
        // ��� üũ
        if (!BoundryCheck(posX, posY, inventoryItem.WIDTH, inventoryItem.HEIGHT))
        {
            Debug.LogWarning($"Cannot place item: position ({posX}, {posY}) is out of bounds");
            return false;
        }

        if (!OverlapCheck(posX, posY, inventoryItem.WIDTH, inventoryItem.HEIGHT, ref overlapItem))
        {
            overlapItem = null;
            return false;
        }

        if (overlapItem != null)
        {
            // ��ģ ������ ���� ���� ���� üũ
            if (BoundryCheck(overlapItem.onGridPositionX, overlapItem.onGridPositionY,
                overlapItem.WIDTH, overlapItem.HEIGHT))
            {
                CleanGridReference(overlapItem);
            }
        }

        PlaceItem(inventoryItem, posX, posY);
        return true;
    }



    private void PlaceItem(InventoryItem inventoryItem, int posX, int posY)
    {
        RectTransform rectTransform = inventoryItem.GetComponent<RectTransform>();
        rectTransform.SetParent(this.rectTransform);

        if (BoundryCheck(posX, posY, inventoryItem.WIDTH, inventoryItem.HEIGHT))
        {
            // ���� ������ ����
            for (int x = 0; x < inventoryItem.WIDTH; x++)
            {
                for (int y = 0; y < inventoryItem.HEIGHT; y++)
                {
                    if (inventoryItemSlot[posX + x, posY + y] != null)
                    {
                        CleanGridReference(inventoryItemSlot[posX + x, posY + y]);
                    }
                    inventoryItemSlot[posX + x, posY + y] = inventoryItem;
                }
            }

            inventoryItem.onGridPositionX = posX;
            inventoryItem.onGridPositionY = posY;

            Vector2 position = CalculatePositionOnGrid(inventoryItem, posX, posY);
            rectTransform.localPosition = position;

            // Grid ���� ���� �˸�
            OnItemAdded?.Invoke(inventoryItem);
            NotifyGridChanged();
        }
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
        for(int x =0; x < width; x++)
        {
            for(int y= 0; y< height; y++)
            {
                if (inventoryItemSlot[posX + x, posY + y] != null)
                {
                    if(overlapItem == null)
                    {

                    overlapItem = inventoryItemSlot[posX + x, posY + y];
                    }
                    else
                    {
                        if(overlapItem != inventoryItemSlot[posX + x, posY + y])
                        {                
                            return false;
                        }
                    }
                }
            }
        }

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

    bool PositionCheck(int posX, int posY)
    {
        if(posX < 0 || posY < 0) { return false; }

        if(posX >= gridSizeWidth || posY >= gridSizeHeight) {  return false; }

        return true;
    }

    public bool BoundryCheck(int posX, int posY, int width, int height)
    {
        if(PositionCheck(posX, posY) == false) { return false;}

        posX += width -1;
        posY += height -1;
        if(PositionCheck(posX,posY) == false) { return false;}  

        return true;
    }

    public InventoryItem GetItem(int x, int y)
    {
        Debug.Log($"GetItem called for position ({x}, {y})");

        if (inventoryItemSlot == null || x < 0 || y < 0 || x >= gridSizeWidth || y >= gridSizeHeight)
        {
            Debug.Log($"Invalid grid access: slot array null or position out of bounds");
            return null;
        }

        InventoryItem item = inventoryItemSlot[x, y];
        Debug.Log($"Found item at position: {item != null}");

        if (item != null)
        {
            Debug.Log($"Item origin position: ({item.onGridPositionX}, {item.onGridPositionY})");
            return inventoryItemSlot[item.onGridPositionX, item.onGridPositionY];
        }

        return null;
    }
    public Vector2Int? FindSpaceForObject(InventoryItem itemToInsert)
    {
        // itemToInsert�� ũ�Ⱑ ��ȿ���� ���� Ȯ��
        if (itemToInsert.WIDTH <= 0 || itemToInsert.HEIGHT <= 0)
        {
            Debug.LogError($"Invalid item size: {itemToInsert.WIDTH}x{itemToInsert.HEIGHT}");
            return null;
        }

        // �׸��� ������ �������� �� �� �ִ� �ִ� ���� ���
        int maxY = gridSizeHeight - itemToInsert.HEIGHT + 1;
        int maxX = gridSizeWidth - itemToInsert.WIDTH + 1;

        if (maxX <= 0 || maxY <= 0)
        {
            Debug.LogWarning($"Item size {itemToInsert.WIDTH}x{itemToInsert.HEIGHT} is too large for grid {gridSizeWidth}x{gridSizeHeight}");
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
