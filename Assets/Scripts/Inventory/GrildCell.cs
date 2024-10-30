using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GridCell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector2Int gridPosition;
    private InventoryGrid grid;
    private bool isOccupied;

    public Vector2Int Position => gridPosition;
    public bool IsOccupied => isOccupied;

    public void Initialize(Vector2Int position, InventoryGrid parentGrid)
    {
        gridPosition = position;
        grid = parentGrid;
        isOccupied = false;

        // �ʿ��� ��� ����׿� �ð��� ǥ��
#if UNITY_EDITOR
        var image = gameObject.AddComponent<Image>();
        image.color = new Color(1, 1, 1, 0.1f);
#endif
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // �巡�� ���� �������� ���� �� ���̶���Ʈ ȿ��
        if (eventData.dragging)
        {
            // ���̶���Ʈ ȿ�� ����
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // ���̶���Ʈ ȿ�� ����
    }

    public void SetOccupied(bool occupied)
    {
        isOccupied = occupied;
    }
}
