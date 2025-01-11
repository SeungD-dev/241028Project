using UnityEngine;

public class InventoryHighlight : MonoBehaviour, IPooledObject
{
    [SerializeField] private RectTransform highlighter;
    private ItemGrid currentGrid; // ���� Ȱ�� �׸��� ����

    private InventoryItem associatedItem;
    private void Awake()
    {
        if (highlighter == null)
        {
            highlighter = GetComponent<RectTransform>();
        }     
        InitializeHighlighter();
    }

    private void InitializeHighlighter()
    {     
        // �������� 1�� �ʱ�ȭ
        if (highlighter != null)
        {
            highlighter.localScale = Vector3.one;
        }

        // �ʱ⿡�� ��Ȱ��ȭ
        Show(false);
    }

    public void OnObjectSpawn()
    {
        
        ResetHighlight();
    }

    private void ResetHighlight()
    {
        if (highlighter != null)
        {
            highlighter.localScale = Vector3.one;
            highlighter.localPosition = Vector3.zero;
        }
        associatedItem = null;
    }

    public void Show(bool visible)
    {
        if (highlighter != null)
        {
            highlighter.gameObject.SetActive(visible);
        }
    }

    public void SetSize(InventoryItem targetItem)
    {
        if (targetItem == null || highlighter == null) return;

        Vector2 size = new Vector2(
            targetItem.Width * ItemGrid.TILE_SIZE,
            targetItem.Height * ItemGrid.TILE_SIZE
        );

        highlighter.sizeDelta = size;

        // �������� 1�� �ƴ� ��� ������ 1�� ����
        if (highlighter.localScale != Vector3.one)
        {
            highlighter.localScale = Vector3.one;
            Debug.LogWarning("Highlighter scale was not 1. Resetting to default.");
        }
    }
    public void SetParent(ItemGrid targetGrid)
    {
        if (targetGrid == null || highlighter == null) return;

        RectTransform gridRectTransform = targetGrid.GetComponent<RectTransform>();
        if (gridRectTransform != null)
        {
            highlighter.SetParent(gridRectTransform, false);
            // �θ� ���� �� ���� ������ �ʱ�ȭ ����
            highlighter.localPosition = Vector2.zero;
        }
    }

    public void SetPosition(ItemGrid targetGrid, InventoryItem targetItem, int posX = -1, int posY = -1)
    {
        if (targetGrid == null || targetItem == null || highlighter == null) return;

        int x = posX >= 0 ? posX : targetItem.onGridPositionX;
        int y = posY >= 0 ? posY : targetItem.onGridPositionY;

        Vector2 pos = targetGrid.CalculatePositionOnGrid(targetItem, x, y);
        highlighter.localPosition = pos;
    }

    public InventoryItem GetAssociatedItem() => associatedItem;
}