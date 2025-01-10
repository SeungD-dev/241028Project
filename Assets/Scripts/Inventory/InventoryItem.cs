using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// �κ��丮���� ���Ǵ� �������� �����ϴ� ������Ʈ
/// �������� ũ��, ȸ��, �׸��� ��ġ ���� ó��
/// </summary>
public class InventoryItem : MonoBehaviour
{
    #region Fields
    [SerializeField] private Image itemImage;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private WeaponData itemData;

    private Vector2Int gridPosition = new Vector2Int(-1, -1);
    private bool isRotated;
    private readonly Vector2Int INVALID_POSITION = new Vector2Int(-1, -1);
    #endregion

    #region Properties
    #region Properties
    /// <summary>
    /// �������� ���� �����Ϳ� ���� ���� ������
    /// </summary>
    public WeaponData WeaponData
    {
        get { return itemData; }
        private set { itemData = value; }
    }

  
    public int Width => isRotated ? itemData.height : itemData.width;

   
    public int Height => isRotated ? itemData.width : itemData.height;

    public Vector2Int GridPosition => gridPosition;

  
    public bool IsRotated => isRotated;

    public bool OnGrid => gridPosition.x >= 0 && gridPosition.y >= 0;


   
    public int onGridPositionX => gridPosition.x;
    public int onGridPositionY => gridPosition.y;
    #endregion


    #region Unity Methods
    private void Awake()
    {
        InitializeComponents();
        gridPosition = INVALID_POSITION;
    }

    private void OnValidate()
    {
        InitializeComponents();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// ������ �ʱ�ȭ
    /// </summary>
    /// <param name="data">���� ������</param>
    /// 
    public WeaponData GetWeaponData()
    {
        return itemData;
    }
    public void Initialize(WeaponData data)
    {
        WeaponData = data;
        isRotated = false;
        gridPosition = INVALID_POSITION;

        UpdateVisuals();
        UpdateSize();
    }
    /// <summary>
    /// ������ ȸ��
    /// </summary>
    public void Rotate()
    {
        isRotated = !isRotated;
        UpdateRotation();
        UpdateSize();
    }

    /// <summary>
    /// �׸��� ��ġ ����
    /// </summary>
    public void SetGridPosition(Vector2Int position)
    {
        gridPosition = position;
    }
    /// <summary>
    /// ������ ũ�� ��ȯ
    /// </summary>
    public Vector2 GetWorldSize()
    {
        return new Vector2(
            Width * ItemGrid.TILE_SIZE,
            Height * ItemGrid.TILE_SIZE
        );
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// �������� �ð��� ��� ������Ʈ
    /// </summary>
    private void InitializeComponents()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (itemImage == null) itemImage = GetComponent<Image>();
    }
    private void UpdateVisuals()
    {
        if (itemImage != null && WeaponData != null)
        {
            itemImage.sprite = WeaponData.GetColoredInventoryWeaponIcon();
            itemImage.color = WeaponData.GetTierColor();
        }
    }

    /// <summary>
    /// RectTransform ũ�� ������Ʈ
    /// </summary>
    private void UpdateSize()
    {
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(
                Width * ItemGrid.TILE_SIZE,
                Height * ItemGrid.TILE_SIZE
            );
        }
    }

    /// <summary>
    /// ȸ�� ����
    /// </summary>
    private void UpdateRotation()
    {
        if (rectTransform == null) return;

        rectTransform.localRotation = Quaternion.Euler(0, 0, isRotated ? 90f : 0f);
        UpdateRotationOffset();
    }
    private void UpdateRotationOffset()
    {
        float gridWidth = Width * ItemGrid.TILE_SIZE;
        float gridHeight = Height * ItemGrid.TILE_SIZE;

        float offsetX = (gridHeight - gridWidth) * 0.5f;
        float offsetY = (gridWidth - gridHeight) * 0.5f;

        Vector2 offset = new Vector2(offsetX, offsetY);
        rectTransform.anchoredPosition += isRotated ? offset : -offset;
    }
    #endregion
    #endregion   
}