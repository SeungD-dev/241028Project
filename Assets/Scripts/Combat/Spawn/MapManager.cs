using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour
{
    private static MapManager instance;
    public static MapManager Instance => instance;

    [Header("Map Settings")]
    [SerializeField] private GameObject mapPrefabReference;
    [SerializeField] private string mapResourcePath = "Prefabs/Map/Map";

    [Header("Camera Settings")]
    [SerializeField] private CinemachineCamera cinemachineCamera; // Inspector���� �Ҵ�

    private GameMap currentMap;
    private GameObject cameraBoundObj;
    public GameMap CurrentMap => currentMap;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // �ʿ��� ���� �ʱ�ȭ
            if (cinemachineCamera == null)
                cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
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
        var wallTilemap = map.WallTilemap;
        if (wallTilemap == null)
        {
            Debug.LogError("Wall Ÿ�ϸ��� ã�� �� �����ϴ�!");
            return;
        }

        // ���� CameraBound ����
        if (cameraBoundObj != null)
        {
            Destroy(cameraBoundObj);
        }

        // �� ī�޶� �ٿ�� ����
        cameraBoundObj = new GameObject("CameraBound");

        // Ÿ�ϸ� �ٿ�� ��� - �� ȿ������ �������
        CalculateAndApplyBounds(wallTilemap, cameraBoundObj);
    }

    private void CalculateAndApplyBounds(Tilemap tilemap, GameObject boundObj)
    {
        BoundsInt cellBounds = tilemap.cellBounds;
        Vector3 cellSize = tilemap.layoutGrid.cellSize;

        // �����ڸ��� üũ�ؼ� ȿ���� ���
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        bool foundTiles = false;

        // Ÿ�ϸ� ũ��
        int xMin = cellBounds.xMin, xMax = cellBounds.xMax;
        int yMin = cellBounds.yMin, yMax = cellBounds.yMax;

        // ��� �����ڸ��� üũ
        for (int x = xMin; x < xMax; x++)
        {
            Vector3Int cellPos = new Vector3Int(x, yMax - 1, 0);
            if (tilemap.HasTile(cellPos))
            {
                foundTiles = true;
                Vector3 worldPos = tilemap.transform.TransformPoint(tilemap.CellToLocal(cellPos));
                minX = Mathf.Min(minX, worldPos.x);
                maxX = Mathf.Max(maxX, worldPos.x + cellSize.x);
                maxY = Mathf.Max(maxY, worldPos.y + cellSize.y);
            }
        }

        // �ϴ� �����ڸ��� üũ
        for (int x = xMin; x < xMax; x++)
        {
            Vector3Int cellPos = new Vector3Int(x, yMin, 0);
            if (tilemap.HasTile(cellPos))
            {
                foundTiles = true;
                Vector3 worldPos = tilemap.transform.TransformPoint(tilemap.CellToLocal(cellPos));
                minX = Mathf.Min(minX, worldPos.x);
                maxX = Mathf.Max(maxX, worldPos.x + cellSize.x);
                minY = Mathf.Min(minY, worldPos.y);
            }
        }

        // ���� �����ڸ��� üũ
        for (int y = yMin; y < yMax; y++)
        {
            Vector3Int cellPos = new Vector3Int(xMin, y, 0);
            if (tilemap.HasTile(cellPos))
            {
                foundTiles = true;
                Vector3 worldPos = tilemap.transform.TransformPoint(tilemap.CellToLocal(cellPos));
                minX = Mathf.Min(minX, worldPos.x);
                minY = Mathf.Min(minY, worldPos.y);
                maxY = Mathf.Max(maxY, worldPos.y + cellSize.y);
            }
        }

        // ���� �����ڸ��� üũ
        for (int y = yMin; y < yMax; y++)
        {
            Vector3Int cellPos = new Vector3Int(xMax - 1, y, 0);
            if (tilemap.HasTile(cellPos))
            {
                foundTiles = true;
                Vector3 worldPos = tilemap.transform.TransformPoint(tilemap.CellToLocal(cellPos));
                maxX = Mathf.Max(maxX, worldPos.x + cellSize.x);
                minY = Mathf.Min(minY, worldPos.y);
                maxY = Mathf.Max(maxY, worldPos.y + cellSize.y);
            }
        }

        // Ÿ���� ã�� ���� ��� �� ũ��� ��ü
        if (!foundTiles)
        {
            // �� ũ�� ���
            float halfWidth = currentMap.MapSize.x / 2f;
            float halfHeight = currentMap.MapSize.y / 2f;

            minX = -halfWidth;
            minY = -halfHeight;
            maxX = halfWidth;
            maxY = halfHeight;
        }

        // ���� �߰�
        float padding = 0.1f;
        minX -= padding;
        minY -= padding;
        maxX += padding;
        maxY += padding;

        // �ݶ��̴� ����
        PolygonCollider2D collider = boundObj.AddComponent<PolygonCollider2D>();
        Vector2[] points = new Vector2[4];
        points[0] = new Vector2(minX, minY);
        points[1] = new Vector2(maxX, minY);
        points[2] = new Vector2(maxX, maxY);
        points[3] = new Vector2(minX, maxY);

        collider.points = points;

        // Cinemachine Confiner ������Ʈ
        UpdateCameraConfiner(collider);
    }

    // ī�޶� �����̳� ������Ʈ �и�
    private void UpdateCameraConfiner(Collider2D boundingCollider)
    {
        if (cinemachineCamera == null)
        {
            cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
            if (cinemachineCamera == null)
            {
                Debug.LogError("CinemachineCamera�� ã�� �� �����ϴ�!");
                return;
            }
        }

        var confiner = cinemachineCamera.GetComponent<CinemachineConfiner2D>();
        if (confiner == null)
        {
            confiner = cinemachineCamera.gameObject.AddComponent<CinemachineConfiner2D>();
        }

        confiner.BoundingShape2D = boundingCollider;
        confiner.Damping = 0.5f;
        confiner.SlowingDistance = 1.0f;
        confiner.InvalidateBoundingShapeCache();
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