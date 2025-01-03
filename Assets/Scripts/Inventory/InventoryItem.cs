using UnityEngine;
using UnityEngine.UI;


public class InventoryItem : MonoBehaviour
{
    public WeaponData weaponData;
    private Vector2 originalSize;
    private RectTransform rectTransform;
    private Image itemImage;

    public int HEIGHT => rotated ? weaponData.width : weaponData.height;
    public int WIDTH => rotated ? weaponData.height : weaponData.width;

    public int onGridPositionX;
    public int onGridPositionY;
    public bool rotated = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        itemImage = GetComponent<Image>();
    }

    internal void Set(WeaponData weaponData)
    {
        this.weaponData = weaponData;
        itemImage.color = weaponData.GetTierColor();
        itemImage.sprite = weaponData.inventoryWeaponIcon;
        rotated = false;
        UpdateSize();
        originalSize = rectTransform.sizeDelta;
        rectTransform.rotation = Quaternion.identity;
    }

    internal void Rotate()
    {
        rotated = !rotated;

        // ���� �׸��� ũ�� ���
        float gridWidth = WIDTH * ItemGrid.tileSizeWidth;
        float gridHeight = HEIGHT * ItemGrid.tileSizeHeight;

        // �̹��� ȸ��
        rectTransform.rotation = Quaternion.Euler(0, 0, rotated ? 90f : 0f);

        // ũ�� ������Ʈ
        if (rotated)
        {
            rectTransform.sizeDelta = new Vector2(gridHeight, gridWidth);
        }
        else
        {
            rectTransform.sizeDelta = new Vector2(gridWidth, gridHeight);
        }

        // ȸ�� �� ��ġ ������ �ʿ��� ���
        Vector2 currentPos = rectTransform.anchoredPosition;
        if (rotated)
        {
            float offsetX = (gridHeight - gridWidth) * 0.5f;
            float offsetY = (gridWidth - gridHeight) * 0.5f;
            rectTransform.anchoredPosition = new Vector2(currentPos.x + offsetX, currentPos.y + offsetY);
        }
        else
        {
            float offsetX = (gridHeight - gridWidth) * 0.5f;
            float offsetY = (gridWidth - gridHeight) * 0.5f;
            rectTransform.anchoredPosition = new Vector2(currentPos.x - offsetX, currentPos.y - offsetY);
        }
    }

    private void UpdateSize()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        float width = weaponData.width * ItemGrid.tileSizeWidth;
        float height = weaponData.height * ItemGrid.tileSizeHeight;

        rectTransform.sizeDelta = new Vector2(width, height);
    }

    // �������� ���� ���� ũ�⸦ ��ȯ�ϴ� �޼��� �߰�
    public Vector2 GetWorldSize()
    {
        return new Vector2(WIDTH * ItemGrid.tileSizeWidth, HEIGHT * ItemGrid.tileSizeHeight);
    }
}