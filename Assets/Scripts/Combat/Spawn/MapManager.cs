using Unity.Cinemachine;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    private static MapManager instance;
    public static MapManager Instance => instance;

    [Header("Map Settings")]
    [SerializeField] private GameObject mapPrefabReference;
    [SerializeField] private string mapResourcePath = "Prefabs/Map/Map";

    private GameMap currentMap;
    public GameMap CurrentMap => currentMap;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ������ �� ���ҽ� �ε�
    public GameMap LoadMap(string mapPath = null)
    {
        // ���� �� ����
        if (currentMap != null)
        {
            Destroy(currentMap.gameObject);
            currentMap = null;
        }

        GameObject mapInstance;

        // �̸� ������ �� ������ ��� (�� ȿ����)
        if (mapPrefabReference != null)
        {
            mapInstance = Instantiate(mapPrefabReference, Vector3.zero, Quaternion.identity);
        }
        else
        {
            // ����: Resources���� �� ������ �ε�
            string path = string.IsNullOrEmpty(mapPath) ? mapResourcePath : mapPath;
            GameObject mapPrefab = Resources.Load<GameObject>(path);

            if (mapPrefab == null)
            {
                Debug.LogError($"Failed to load map from path: {path}");
                return null;
            }

            mapInstance = Instantiate(mapPrefab, Vector3.zero, Quaternion.identity);
        }

        // GameMap ������Ʈ ��������
        currentMap = mapInstance.GetComponent<GameMap>();
        if (currentMap == null)
        {
            Debug.LogError("Loaded map prefab does not have a GameMap component!");
            Destroy(mapInstance);
            return null;
        }

        Debug.Log($"Map loaded: {currentMap.MapName}");

        // ���� ������ �߽� ��ġ
        CenterMapToOrigin(currentMap);

        // ī�޶� �ٿ�� ������Ʈ
        UpdateCameraBounds(currentMap);

        return currentMap;
    }
    private void CenterMapToOrigin(GameMap map)
    {
        // Ÿ�ϸ� �ٿ�� �� �߽� ���
        Vector2 mapCenter = CalculateMapCenter(map);

        // �� ��ġ ����
        Vector3 offset = new Vector3(-mapCenter.x, -mapCenter.y, 0);
        map.transform.position = offset;

        Debug.Log($"Map centered at origin. Applied offset: {offset}");
    }

    private Vector2 CalculateMapCenter(GameMap map)
    {
        // Ÿ�ϸ� �ٿ�� �߽� ���
        var floorTilemap = map.FloorTilemap;
        var wallTilemap = map.WallTilemap;

        // ��� ������ Ÿ�ϸ� ��������
        var tilemap = floorTilemap != null ? floorTilemap : wallTilemap;
        if (tilemap == null) return Vector2.zero;

        // �ٿ�� ���
        var bounds = tilemap.cellBounds;

        // ���� ��ǥ�� ��ȯ
        Vector3 worldMin = tilemap.CellToWorld(bounds.min);
        Vector3 worldMax = tilemap.CellToWorld(bounds.max);

        // �� ũ�⸦ ����� ����
        Vector3 cellSize = tilemap.layoutGrid.cellSize;
        worldMax += new Vector3(cellSize.x, cellSize.y, 0);

        // �� �߽� ���
        return new Vector2(
            (worldMin.x + worldMax.x) * 0.5f,
            (worldMin.y + worldMax.y) * 0.5f
        );
    }

    private void UpdateCameraBounds(GameMap map)
    {
        // Cinemachine Confiner2D ã��
        var confiner = FindAnyObjectByType<CinemachineConfiner2D>();
        if (confiner == null) return;

        // PolygonCollider2D ���� ã��
        var boundingShape = confiner.GetComponent<PolygonCollider2D>();
        if (boundingShape == null)
        {
            Debug.LogWarning("Cinemachine Confiner doesn't have a PolygonCollider2D component");
            return;
        }

        // �� ũ�� ���
        float width = map.MapSize.x;
        float height = map.MapSize.y;

        // �ٿ�� ������ ����
        Vector2[] points = new Vector2[4];
        points[0] = new Vector2(-width / 2, -height / 2);
        points[1] = new Vector2(width / 2, -height / 2);
        points[2] = new Vector2(width / 2, height / 2);
        points[3] = new Vector2(-width / 2, height / 2);

        boundingShape.points = points;

        // ĳ�� ���� 
        confiner.InvalidateBoundingShapeCache();

        Debug.Log($"Updated camera bounds to match map size: {map.MapSize}");
    }
    public Vector2 GetPlayerStartPosition()
    {
        if (currentMap == null)
        {
            Debug.LogWarning("No map loaded. Using default position.");
            return Vector2.zero;
        }

        // �� �߾ӿ� �÷��̾� ��ġ
        return Vector2.zero;
    }
}
