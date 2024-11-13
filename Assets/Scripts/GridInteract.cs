using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ItemGrid))]
public class GridInteract : MonoBehaviour, IPointerEnterHandler
{
    InventoryController inventoryController;
    ItemGrid itemGrid;

    private void Awake()
    {
        inventoryController = FindFirstObjectByType(typeof(InventoryController)) as InventoryController;
        itemGrid = GetComponent<ItemGrid>();

        // ������ �� �ٷ� Grid ����
        if (inventoryController != null && itemGrid != null)
        {
            inventoryController.SelectedItemGrid = itemGrid;
        }
    }

    private void OnEnable()
    {
        // UI�� Ȱ��ȭ�� ������ Grid ����
        if (inventoryController != null && itemGrid != null)
        {
            inventoryController.SelectedItemGrid = itemGrid;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // �߰� Grid�� ���� ��츦 ���� ���ܵ�
        inventoryController.SelectedItemGrid = itemGrid;
    }

    // OnPointerExit ����
}
