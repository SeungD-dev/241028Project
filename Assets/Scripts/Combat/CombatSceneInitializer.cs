using Unity.Cinemachine;
using UnityEngine;

public class CombatSceneInitializer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private ShopController shopController;
    [SerializeField] private CombatController combatController;
    [SerializeField] private GameOverController gameOverController;
    [SerializeField] private WaveManager waveManager;

    [Header("Camera Settings")]
    [SerializeField] private GameObject cameraBoundObject; // CameraBound ������Ʈ ����

    private void Start()
    {
        // �� �ε� �� �÷��̾� ��ġ ����
        if (MapManager.Instance != null)
        {
            GameMap map = MapManager.Instance.LoadMap();
            if (map != null && playerStats != null)
            {
                // �÷��̾ ������ ��ġ
                Vector2 startPosition = MapManager.Instance.GetPlayerStartPosition();
                playerStats.transform.position = startPosition;

                // ī�޶� �ٿ�� ������Ʈ
                UpdateCameraBounds(map);

                // ����� ���� ���
                Debug.Log($"Map position: {map.transform.position}");
                Debug.Log($"Map bounds: min={map.FloorTilemap.cellBounds.min}, max={map.FloorTilemap.cellBounds.max}");
                Debug.Log($"Map world bounds: min={map.FloorTilemap.CellToWorld(map.FloorTilemap.cellBounds.min)}, " +
                        $"max={map.FloorTilemap.CellToWorld(map.FloorTilemap.cellBounds.max)}");
                Debug.Log($"Player position: {playerStats.transform.position}");
            }
        }

        // ���� �Ŵ��� ���� ����
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCombatSceneReferences(
                playerStats,
                shopController,
                combatController,
                gameOverController
            );

            // WaveManager �ʱ�ȭ
            if (waveManager != null)
            {
                waveManager.EnsureInitialized(MapManager.Instance.CurrentMap);
            }

            // �ʱ� �Ͻ����� ���� ����
            GameManager.Instance.SetGameState(GameState.Paused);

            // ù ���� ����
            OpenInitialShopPhase();
        }
    }

    private void UpdateCameraBounds(GameMap map)
    {
        if (cameraBoundObject == null)
        {
            Debug.LogWarning("Camera bound reference not set in CombatSceneInitializer");
            return;
        }

        // ������ �ݶ��̴� ��������
        var polygonCollider = cameraBoundObject.GetComponent<PolygonCollider2D>();
        if (polygonCollider == null)
        {
            Debug.LogWarning("PolygonCollider2D not found on CameraBound object. Adding new one.");
            polygonCollider = cameraBoundObject.AddComponent<PolygonCollider2D>();
        }

        // �� ũ�� ��������
        Vector2 mapSize = map.MapSize;

        // �� ũ�⿡ �°� ī�޶� �ٿ�� ����
        float halfWidth = mapSize.x / 2f;
        float halfHeight = mapSize.y / 2f;

        // ��¦ ������ �θ� ȭ�� �����ڸ����� ��谡 ������ ����
        // float padding = 0.5f; // �ʿ��ϸ� �е� �߰�

        Vector2[] points = new Vector2[4];
        points[0] = new Vector2(-halfWidth, -halfHeight);
        points[1] = new Vector2(halfWidth, -halfHeight);
        points[2] = new Vector2(halfWidth, halfHeight);
        points[3] = new Vector2(-halfWidth, halfHeight);

        polygonCollider.points = points;

       
        var confiner = cameraBoundObject.GetComponent<CinemachineConfiner2D>();
        if (confiner != null)
        {
            confiner.InvalidateBoundingShapeCache();
        }

        Debug.Log($"Updated camera bounds to match map size: {mapSize}");
    }

    private void OpenInitialShopPhase()
    {
        if (shopController != null)
        {
            // ù ���� ǥ��
            shopController.isFirstShop = true;
            shopController.InitializeShop();
        }
    }
}