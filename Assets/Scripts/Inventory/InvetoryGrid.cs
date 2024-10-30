using UnityEngine;
using UnityEngine.UI;

public class InventoryGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridSize = 4;
    [SerializeField] private RectTransform backgroundImage;
    [SerializeField] private GameObject cellContainer; // ������ ���� �� �����̳�

    private GridCell[,] cells;
    private float cellSize;

    private void Awake()
    {
        // �� �����̳� ����
        cellContainer = new GameObject("CellContainer");
        var containerRect = cellContainer.AddComponent<RectTransform>();
        cellContainer.transform.SetParent(transform, false);

        // �����̳��� RectTransform ����
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        containerRect.localScale = Vector3.one;
    }

    private void Start()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<RectTransform>();

        InitializeGrid();
    }

    private void InitializeGrid()
    {
        cells = new GridCell[gridSize, gridSize];

        // ��� �̹��� ũ�⸦ �������� �� ���� ũ�� ���
        cellSize = (backgroundImage.rect.width / gridSize);

        // �׸��� �� ����
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                CreateCell(x, y);
            }
        }
    }

    private void CreateCell(int x, int y)
    {
        GameObject cellObj = new GameObject($"Cell [{x},{y}]", typeof(RectTransform));
        RectTransform rectTransform = cellObj.GetComponent<RectTransform>();

        // ���� �����̳��� �ڽ����� ����
        rectTransform.SetParent(cellContainer.transform, false);

        // ���� ��Ŀ ����
        float startX = (float)x / gridSize;
        float endX = (float)(x + 1) / gridSize;
        float startY = 1 - (float)(y + 1) / gridSize;
        float endY = 1 - (float)y / gridSize;

        rectTransform.anchorMin = new Vector2(startX, startY);
        rectTransform.anchorMax = new Vector2(endX, endY);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // ������ ������ 1�� ����
        rectTransform.localScale = Vector3.one;

        // GridCell ������Ʈ �߰�
        var cell = cellObj.AddComponent<GridCell>();
        cell.Initialize(new Vector2Int(x, y), this);
        cells[x, y] = cell;
    }

    // Ư�� ��ġ�� �� ��������
    public GridCell GetCell(Vector2Int position)
    {
        if (position.x >= 0 && position.x < gridSize &&
            position.y >= 0 && position.y < gridSize)
        {
            return cells[position.x, position.y];
        }
        return null;
    }

    // ���콺/��ġ ��ġ�� �׸��� ��ǥ�� ��ȯ
    public Vector2Int GetGridPosition(Vector2 screenPosition)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            backgroundImage, screenPosition, null, out localPoint);

        // ���� ��ǥ�� 0~1 ������ ����ȭ
        Vector2 normalizedPos = new Vector2(
            (localPoint.x + backgroundImage.rect.width * 0.5f) / backgroundImage.rect.width,
            (localPoint.y + backgroundImage.rect.height * 0.5f) / backgroundImage.rect.height
        );

        // �׸��� ��ǥ ���
        int x = Mathf.Clamp(Mathf.FloorToInt(normalizedPos.x * gridSize), 0, gridSize - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt((1 - normalizedPos.y) * gridSize), 0, gridSize - 1);

        return new Vector2Int(x, y);
    }
}
