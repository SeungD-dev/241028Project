using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;

public class InventoryController : MonoBehaviour
{
    private TouchActions touchActions;
    private InputAction touchPosition;
    private InputAction touchPress;
    private InputAction touchDelta;

    [HideInInspector]
    private ItemGrid selectedItemGrid;
    public ItemGrid SelectedItemGrid
    {
        get => selectedItemGrid;
        set
        {
            selectedItemGrid = value;
            inventoryHighlight.SetParent(value);
        }
    }
    WeaponManager weaponManager;
    InventoryItem selectedItem;
    InventoryItem overlapItem;

    RectTransform rectTransform;

    [SerializeField] List<WeaponData> weapons;
    [SerializeField] GameObject weaponPrefab;
    [SerializeField] Transform canvasTransform;
    [SerializeField] private WeaponInfoUI weaponInfoUI;
    [SerializeField] private GameObject inventoryUI;
    [SerializeField] private Transform itemSpawnPoint;
    [SerializeField] private GameObject playerUI;
    [SerializeField] private ItemGrid mainInventoryGrid;
    [SerializeField] private Button progressButton;

    InventoryHighlight inventoryHighlight;
    private bool isDragging = false;
    private Vector2 originalSpawnPosition;

    private const float SLIDE_THRESHOLD = 50f; // ȸ���� ���� �ּ� �����̵� �Ÿ�
    private bool isSliding = false;
    private Vector2 slideStartPosition;

    private void Awake()
    {
        inventoryHighlight = GetComponent<InventoryHighlight>();
        if (inventoryHighlight == null)
        {
            Debug.LogError("InventoryHighlight component not found!");
        }

        weaponManager = GameObject.FindGameObjectWithTag("Player")?.GetComponent<WeaponManager>();

        touchActions = new TouchActions();
        touchPosition = touchActions.Touch.Position;
        touchPress = touchActions.Touch.Press;
        touchDelta = touchActions.Touch.Delta;
        
        

        touchPress.started += OnTouchStarted;
        touchPress.canceled += OnTouchEnded;

        if (itemSpawnPoint != null)
        {
            originalSpawnPosition = itemSpawnPoint.position;
        }

        InitializeGrid();
        ToggleInventoryUI(false);
    }
    private void InitializeGrid()
    {
        if (mainInventoryGrid == null)
        {
            Debug.LogError("mainInventoryGrid is not assigned in inspector!");
            return;
        }

        selectedItemGrid = mainInventoryGrid;
        if (inventoryHighlight != null)
        {
            inventoryHighlight.SetParent(selectedItemGrid);
        }
    }
    private void OnEnable()
    {
        touchActions.Enable();
    }


    private void OnDisable()
    {
        touchActions.Disable();
    }

    private void ResetItemToSpawnPoint(GameObject itemObj)
    {
        if (rectTransform == null || itemSpawnPoint == null) return;

        Vector2 spawnPos = itemSpawnPoint.position;
        Vector2 itemSize = rectTransform.sizeDelta;

        // ������ ũ���� ���ݸ�ŭ ������ ����
        spawnPos.x += itemSize.x * 0.5f;
        spawnPos.y -= itemSize.y * 0.5f;

        rectTransform.position = spawnPos;
        rectTransform.SetAsLastSibling();

        // �������� ���õ� ���·� �����ϰ� �巡�� �����ϵ��� ����
        isDragging = true;

        // ���̶���Ʈ ��ġ�� ������Ʈ
        if (inventoryHighlight != null && selectedItem != null)
        {
            inventoryHighlight.Show(true);
            inventoryHighlight.SetSize(selectedItem);
        }
    }


    public void ToggleInventoryUI(bool isActive)
    {
        if (inventoryUI != null)
        {
            inventoryUI.SetActive(isActive);

            if (isActive && selectedItemGrid == null)
            {
                InitializeGrid();
            }

            // CleanupInvalidItems�� UI�� ������ Ȱ��ȭ�� �Ŀ� ȣ��
            if (isActive)
            {
                StartCoroutine(CleanupInvalidItemsDelayed());
            }
        }

        if (playerUI != null)
        {
            playerUI.SetActive(!isActive);
        }

        if (!isActive)
        {
            if (selectedItem != null)
            {
                Destroy(selectedItem.gameObject);
                selectedItem = null;
            }
            isDragging = false;
            if (inventoryHighlight != null)
            {
                inventoryHighlight.Show(false);
            }
        }
    }
    private IEnumerator CleanupInvalidItemsDelayed()
    {
        yield return new WaitForEndOfFrame();
        CleanupInvalidItems();
    }
    private void HandleItemOutOfBounds()
    {
        if (selectedItem != null && isDragging)
        {
            Vector2 touchPos = touchPosition.ReadValue<Vector2>();

            // ȭ�� ũ�� ��������
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);

            // ȭ�� ������ �������� üũ
            if (touchPos.x < 0 || touchPos.x > screenSize.x ||
                touchPos.y < 0 || touchPos.y > screenSize.y)
            {
                ResetItemToSpawnPoint(selectedItem.gameObject);
                isDragging = false;
                if (inventoryHighlight != null)
                {
                    inventoryHighlight.Show(false);
                }
            }
        }
    }


    public void StartGame()
    {
        // Grid ���� üũ
        if (selectedItemGrid == null)
        {
            Debug.LogError("No ItemGrid selected! Cannot start game.");
            return;
        }

        bool hasEquippedItem = false;
        InventoryItem equippedItem = null;

        // ���� �׸��忡�� ������ ������ ã��
        for (int x = 0; x < selectedItemGrid.Width && !hasEquippedItem; x++)
        {
            for (int y = 0; y < selectedItemGrid.Height && !hasEquippedItem; y++)
            {
                InventoryItem item = selectedItemGrid.GetItem(x, y);
                if (item != null)
                {
                    equippedItem = item;
                    hasEquippedItem = true;
                    break;
                }
            }
        }

        if (hasEquippedItem && equippedItem != null)
        {
            // ���� ���� �� ���� ����
            OnWeaponEquipped(equippedItem);

            // UI ��ȯ
            inventoryHighlight?.Show(false);
            inventoryUI?.SetActive(false);
            playerUI?.SetActive(true);

            // ���� ���� ����
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(GameState.Playing);
            }
        }
        else
        {
            // ��� �޽��� ǥ��
            Debug.LogWarning("No item equipped! Please place a weapon in the grid.");
            // ���⿡ ����ڿ��� �˸��� ǥ���ϴ� UI ������ �߰��� �� �ֽ��ϴ�.
        }
    }

    private void OnWeaponEquipped(InventoryItem item)
    {
        if (item != null && item.weaponData != null && weaponManager != null)
        {
            Debug.Log($"Equipping weapon: {item.weaponData.weaponName}");
            weaponManager.EquipWeapon(item.weaponData);
        }
    }

    private void ProcessSlide()
    {
        if (selectedItem != null && touchPress.IsPressed())
        {
            Vector2 delta = touchDelta.ReadValue<Vector2>();

            // �����̵� ���� �ƴϰ�, �����̵� �Ÿ��� �Ӱ谪�� ������ ȸ�� ó��
            if (!isSliding && delta.magnitude > SLIDE_THRESHOLD)
            {
                // �¿� �����̵��� ���� ȸ�� (���� �̵��� ���� �̵����� Ŭ ��)
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    isSliding = true;
                    RotateItem();
                    isDragging = false; // ȸ�� �߿��� �巡�� ����
                }
            }
        }
    }

    private void OnTouchStarted(InputAction.CallbackContext context)
    {
        if (!inventoryUI.activeSelf || selectedItemGrid == null) return;

        Vector2 touchPos = touchPosition.ReadValue<Vector2>();
        isSliding = false; // ��ġ ���� �� �����̵� ���� �ʱ�ȭ

        if (!isDragging)
        {
            Vector2Int tileGridPosition = GetTileGridPosition(touchPos);
            if (IsPositionWithinGrid(tileGridPosition))
            {
                PickUpItem(tileGridPosition);
                if (selectedItem != null)
                {
                    isDragging = true;
                    if (inventoryHighlight != null)
                    {
                        inventoryHighlight.SetSize(selectedItem);
                    }
                }
            }
        }
    }


    private void OnTouchEnded(InputAction.CallbackContext context)
    {
        if (!inventoryUI.activeSelf || selectedItemGrid == null) return;

        // �����̵� ���� �ʱ�ȭ
        if (isSliding)
        {
            isSliding = false;
            isDragging = true; // �巡�� ���� ����
            return;
        }

        if (selectedItem != null && isDragging)
        {
            Vector2 touchPos = touchPosition.ReadValue<Vector2>();
            Vector2Int tileGridPosition = GetTileGridPosition(touchPos);

            bool isWithinGrid = IsPositionWithinGrid(tileGridPosition) &&
                              selectedItemGrid.BoundryCheck(tileGridPosition.x, tileGridPosition.y,
                                  selectedItem.WIDTH, selectedItem.HEIGHT);

            if (isWithinGrid)
            {
                PutDownItem(tileGridPosition);
            }
            else
            {
                ResetItemToSpawnPoint(selectedItem.gameObject);
                // isDragging�� ResetItemToSpawnPoint ������ true�� ������
            }
        }
    }

    private void Update()
    {
        if (!inventoryUI.activeSelf || selectedItemGrid == null)
        {
            return;
        }

        ProcessSlide(); // �����̵� ó�� �߰�

        // �����̵� ���� �ƴ� ���� �巡�� ó��
        if (isDragging && selectedItem != null && !isSliding)
        {
            Vector2 touchPos = touchPosition.ReadValue<Vector2>();
            rectTransform.position = touchPos;

            Vector2Int positionOnGrid = GetTileGridPosition(touchPos);
            bool isWithinGrid = IsPositionWithinGrid(positionOnGrid) &&
                              selectedItemGrid.BoundryCheck(positionOnGrid.x, positionOnGrid.y,
                                  selectedItem.WIDTH, selectedItem.HEIGHT);

            if (isWithinGrid)
            {
                inventoryHighlight?.Show(true);
                inventoryHighlight?.SetSize(selectedItem);
                inventoryHighlight?.SetPosition(selectedItemGrid, selectedItem, positionOnGrid.x, positionOnGrid.y);
            }
            else
            {
                inventoryHighlight?.Show(false);
            }
        }

        HandleHighlight();
    }



    private void HandleHighlight()
    {
        if (selectedItemGrid == null || !inventoryUI.activeSelf)
        {
            inventoryHighlight?.Show(false);
            return;
        }

        Vector2 touchPos = touchPosition.ReadValue<Vector2>();
        Vector2Int positionOnGrid = GetTileGridPosition(touchPos);

        // Grid �ٱ� ���� üũ
        if (!IsPositionWithinGrid(positionOnGrid))
        {
            inventoryHighlight?.Show(false);
            return;
        }

        // �������� ��� ���� ���� ���
        if (selectedItem == null)
        {
            InventoryItem itemToHighlight = selectedItemGrid.GetItem(positionOnGrid.x, positionOnGrid.y);
            if (itemToHighlight != null)
            {
                inventoryHighlight?.Show(true);
                inventoryHighlight?.SetSize(itemToHighlight);
                inventoryHighlight?.SetPosition(selectedItemGrid, itemToHighlight,
                    itemToHighlight.onGridPositionX, itemToHighlight.onGridPositionY);

                if (weaponInfoUI != null)
                {
                    weaponInfoUI.UpdateWeaponInfo(itemToHighlight.weaponData);
                }
            }
            else
            {
                inventoryHighlight?.Show(false);
            }
        }
        else
        {
            // �������� ��� �ִ� ���
            if (weaponInfoUI != null)
            {
                weaponInfoUI.UpdateWeaponInfo(selectedItem.weaponData);
            }

            // ���� ��ġ�� ��ȿ���� Ȯ��
            bool isValidPosition = selectedItemGrid.BoundryCheck(positionOnGrid.x, positionOnGrid.y,
                selectedItem.WIDTH, selectedItem.HEIGHT);

            if (isValidPosition)
            {
                inventoryHighlight?.Show(true);
                inventoryHighlight?.SetSize(selectedItem);
                inventoryHighlight?.SetPosition(selectedItemGrid, selectedItem, positionOnGrid.x, positionOnGrid.y);
            }
            else
            {
                inventoryHighlight?.Show(false);
            }
        }
    }

    private void RotateItem()
    {
        if (selectedItem == null) return;
        selectedItem.Rotate();

        if(inventoryHighlight != null)
        {
            inventoryHighlight.SetSize(selectedItem);
        }
    }

    private Vector2Int GetTileGridPosition(Vector2 position)
    {
        if (selectedItem != null)
        {
            position.x -= (selectedItem.WIDTH - 1) * ItemGrid.tileSizeWidth / 2;
            position.y += (selectedItem.HEIGHT - 1) * ItemGrid.tileSizeHeight / 2;
        }

        return selectedItemGrid.GetTileGridPosition(position);
    }

    private InventoryItem itemToHighlight;
    Vector2Int oldPosition;

    private void OnDestroy()
    {
        if (progressButton != null)
        {
            progressButton.onClick.RemoveListener(StartGame);
        }

        if (touchPress != null)
        {
            touchPress.started -= OnTouchStarted;
            touchPress.canceled -= OnTouchEnded;
        }

        touchActions?.Dispose();
    }


    public void CreatePurchasedItem(WeaponData weaponData)
    {
        if (itemSpawnPoint == null)
        {
            Debug.LogWarning("Item spawn point is not assigned!");
            return;
        }

        // ������ ����
        GameObject itemObj = Instantiate(weaponPrefab);
        InventoryItem inventoryItem = itemObj.GetComponent<InventoryItem>();
        selectedItem = inventoryItem;
        rectTransform = itemObj.GetComponent<RectTransform>();

        // ������ ������ ����
        inventoryItem.Set(weaponData);

        // Canvas�� �ڽ����� ����
        rectTransform.SetParent(canvasTransform, false);

        // ���� ��ġ ����
        ResetItemToSpawnPoint(itemObj);

        // �ڵ����� �巡�� ��� ����
        isDragging = true;

        // ���̶���Ʈ ���� ������Ʈ
        if (inventoryHighlight != null)
        {
            inventoryHighlight.Show(true);
            inventoryHighlight.SetSize(selectedItem);
        }

        // ���� ���� UI ������Ʈ
        if (weaponInfoUI != null)
        {
            weaponInfoUI.UpdateWeaponInfo(weaponData);
        }
    }

    private void PickUpItem(Vector2Int tileGridPosition)
    {
        selectedItem = selectedItemGrid.PickUpItem(tileGridPosition.x, tileGridPosition.y);
        if (selectedItem != null)
        {
            rectTransform = selectedItem.GetComponent<RectTransform>();

            if (weaponInfoUI != null)
            {
                weaponInfoUI.UpdateWeaponInfo(selectedItem.weaponData);
            }
        }
    }



    private void PutDownItem(Vector2Int tileGridPosition)
    {
        if (selectedItem == null) return;

        bool complete = selectedItemGrid.PlaceItem(selectedItem, tileGridPosition.x, tileGridPosition.y, ref overlapItem);

        if (complete)
        {
            if (overlapItem != null)
            {
                // ��ģ �����۰� ��ġ ��ȯ
                selectedItem = overlapItem;
                rectTransform = selectedItem.GetComponent<RectTransform>();
                rectTransform.SetAsLastSibling();
                isDragging = true;

                // ���̶���Ʈ ������Ʈ
                if (inventoryHighlight != null)
                {
                    inventoryHighlight.Show(true);
                    inventoryHighlight.SetSize(selectedItem);
                }
            }
            else
            {
                // ���������� ��ġ��
                selectedItem = null;
                isDragging = false;
                if (inventoryHighlight != null)
                {
                    inventoryHighlight.Show(false);
                }
            }
        }
        else
        {
            // ��ġ ���� �� ���� ����Ʈ�� �����ϰ� �巡�� ���� ���� ����
            ResetItemToSpawnPoint(selectedItem.gameObject);
            // isDragging�� ResetItemToSpawnPoint ������ true�� ������
        }

        // ���� ���� UI ������Ʈ
        if (weaponInfoUI != null && selectedItem != null)
        {
            weaponInfoUI.UpdateWeaponInfo(selectedItem.weaponData);
        }
    }


    // Grid ���� üũ �� ���� �޼��� �߰�
    private void CleanupInvalidItems()
    {
        if (selectedItemGrid == null || !selectedItemGrid.gameObject.activeInHierarchy)
        {
            return;
        }

        for (int x = 0; x < selectedItemGrid.Width; x++)
        {
            for (int y = 0; y < selectedItemGrid.Height; y++)
            {
                InventoryItem item = selectedItemGrid.GetItem(x, y);
                if (item != null)
                {
                    if (!selectedItemGrid.BoundryCheck(x, y, item.WIDTH, item.HEIGHT))
                    {
                        selectedItemGrid.PickUpItem(x, y);
                        if (item.gameObject != null)
                        {
                            selectedItem = item;
                            rectTransform = item.GetComponent<RectTransform>();
                            ResetItemToSpawnPoint(item.gameObject);
                        }
                    }
                }
            }
        }
    }

    private bool IsPositionWithinGrid(Vector2Int position)
    {
        return position.x >= 0 && position.x < selectedItemGrid.Width &&
               position.y >= 0 && position.y < selectedItemGrid.Height;
    }

    public void OnPurchaseItem(WeaponData weaponData)
    {
        if (selectedItemGrid == null)
        {
            Debug.LogError("No ItemGrid available for item placement!");
            InitializeGrid();
            if (selectedItemGrid == null)
            {
                return;
            }
        }

        // �κ��丮 UI�� ���� Ȱ��ȭ
        inventoryUI.SetActive(true);
        if (playerUI != null)
        {
            playerUI.SetActive(false);
        }

        // �ణ�� ���� �� ������ ����
        StartCoroutine(CreatePurchasedItemDelayed(weaponData));
    }

    private IEnumerator CreatePurchasedItemDelayed(WeaponData weaponData)
    {
        // UI�� ������ Ȱ��ȭ�� ������ ���
        yield return new WaitForEndOfFrame();

        CreatePurchasedItem(weaponData);
    }


}
