using NUnit.Framework.Interfaces;
using UnityEngine.EventSystems;
using UnityEngine;

public class InventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // WeaponData�� public ������Ƽ�� ����
    [SerializeField] private WeaponData weaponData;
    public WeaponData WeaponData => weaponData;

    // CurrentShape�� public ������Ƽ�� ����
    private bool[,] currentShape;
    public bool[,] CurrentShape => currentShape;

    // CurrentPosition�� get, set �����ϰ� ����
    private Vector2Int? currentPosition;
    public Vector2Int? CurrentPosition { get; set; }

    private RectTransform rectTransform;
    private InventoryGrid grid;
    private Vector2 dragOffset;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        grid = GetComponentInParent<InventoryGrid>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // �巡�� ���� ��ġ ����
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out localPoint);
        dragOffset = localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // �巡�� �� ��ġ ������Ʈ
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            grid.GetComponent<RectTransform>(), eventData.position, eventData.pressEventCamera, out localPoint))
        {
            rectTransform.localPosition = localPoint - dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // ��ӵ� ��ġ�� �׸��� �� Ȯ��
        Vector2Int gridPosition = grid.GetGridPosition(eventData.position);
        GridCell targetCell = grid.GetCell(gridPosition);

        if (targetCell != null && !targetCell.IsOccupied)
        {
            // �������� ���� ���� ��ġ
            RectTransform cellRect = targetCell.GetComponent<RectTransform>();
            rectTransform.position = cellRect.position;
            targetCell.SetOccupied(true);
        }
        else
        {
            // ���� ��ġ�� ���ư���
            // ... ����ġ ���� ����
        }
    }
}