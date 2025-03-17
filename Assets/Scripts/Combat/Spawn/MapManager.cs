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
    Debug.Log("Ÿ�ϸ� ��迡 ��Ȯ�� ���߾� ī�޶� ��� ������Ʈ �õ�...");

    // ���� Ÿ�ϸ��� �����ɴϴ�
    var wallTilemap = map.WallTilemap;
    if (wallTilemap == null)
    {
        Debug.LogError("Wall Ÿ�ϸ��� ã�� �� �����ϴ�!");
        return;
    }

        // ��� CameraBound ��ü ã�� �� ����
        GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
    {
        if (obj.name.Contains("CameraBound"))
        {
            Debug.Log($"���� '{obj.name}' ������Ʈ�� �����մϴ�.");
            Object.DestroyImmediate(obj);
        }
    }

    // �� CameraBound ��ü ����
    GameObject cameraBoundObj = new GameObject("CameraBound");
    Debug.Log("�� CameraBound ������Ʈ�� �����߽��ϴ�");

    // Ÿ�ϸ��� ��ȸ�Ͽ� ���� ���� Ÿ���� ��� ���
    BoundsInt cellBounds = wallTilemap.cellBounds;
    Vector3 cellSize = wallTilemap.layoutGrid.cellSize;
    
    // �ܰ� Ÿ���� ��ǥ�� ������ ����
    float minX = float.MaxValue;
    float minY = float.MaxValue;
    float maxX = float.MinValue;
    float maxY = float.MinValue;
    
    bool foundTiles = false;
    
    // ��� Ÿ���� ��ȸ�ϸ� ���� ��ǥ ���
    for (int x = cellBounds.xMin; x < cellBounds.xMax; x++)
    {
        for (int y = cellBounds.yMin; y < cellBounds.yMax; y++)
        {
            Vector3Int cellPos = new Vector3Int(x, y, 0);
            
            if (wallTilemap.HasTile(cellPos))
            {
                foundTiles = true;
                
                // Ÿ���� ���� ��ǥ ��� - �� �߽� �̵� ���
                Vector3 worldPos = wallTilemap.CellToWorld(cellPos);
                
                // Ÿ�ϸ��� ���� �ڽ��̶�� ���� ��ġ�� ���
                worldPos = wallTilemap.transform.TransformPoint(wallTilemap.CellToLocal(cellPos));
                
                // ��ǥ ������Ʈ
                minX = Mathf.Min(minX, worldPos.x);
                minY = Mathf.Min(minY, worldPos.y);
                maxX = Mathf.Max(maxX, worldPos.x + cellSize.x);
                maxY = Mathf.Max(maxY, worldPos.y + cellSize.y);
            }
        }
    }
    
    if (!foundTiles)
    {
        Debug.LogError("Ÿ�ϸʿ��� Ÿ���� ã�� �� �����ϴ�!");
        Object.DestroyImmediate(cameraBoundObj);
        return;
    }
    
    // ���� �߰� (�ʿ信 ���� ����)
    float paddingX = 0.1f;
    float paddingY = 0.1f;
    minX -= paddingX;
    minY -= paddingY;
    maxX += paddingX;
    maxY += paddingY;
    
    Debug.Log($"���� Ÿ�� ���: min=({minX}, {minY}), max=({maxX}, {maxY})");
    
    // ���� ��踦 �簢�� �ݶ��̴��� ��ȯ
    PolygonCollider2D collider = cameraBoundObj.AddComponent<PolygonCollider2D>();
    
    Vector2[] points = new Vector2[4];
    points[0] = new Vector2(minX, minY); // ���ϴ�
    points[1] = new Vector2(maxX, minY); // ���ϴ�
    points[2] = new Vector2(maxX, maxY); // ����
    points[3] = new Vector2(minX, maxY); // �»��
    
    collider.points = points;
    
    Debug.Log($"��� ����Ʈ ����: " +
              $"���ϴ�({points[0].x}, {points[0].y}), " +
              $"���ϴ�({points[1].x}, {points[1].y}), " +
              $"����({points[2].x}, {points[2].y}), " +
              $"�»��({points[3].x}, {points[3].y})");
    
    // Cinemachine Confiner2D ���� ������Ʈ
    var cinemachineCamera = GameObject.FindFirstObjectByType<CinemachineCamera>();
    if (cinemachineCamera != null)
    {
        // ���� �����̳� ����
        var existingConfiner = cinemachineCamera.GetComponent<CinemachineConfiner2D>();
        if (existingConfiner != null)
        {
            Object.DestroyImmediate(existingConfiner);
        }
        
        // �� �����̳� �߰�
        var confiner = cinemachineCamera.gameObject.AddComponent<CinemachineConfiner2D>();
        confiner.BoundingShape2D = collider;
        confiner.Damping = 0.5f;
        confiner.SlowingDistance = 1.0f;
        confiner.InvalidateBoundingShapeCache();
    }
    else
    {
        Debug.LogError("CinemachineCamera�� ã�� �� �����ϴ�!");
    }
    
    Debug.Log("ī�޶� ��� ������Ʈ �Ϸ�");
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
