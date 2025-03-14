using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class GameMap : MonoBehaviour
{
    [Header("Map Components")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;

    [Header("Map Properties")]
    [SerializeField] private string mapName = "Map";

    [Header("Spawn Settings")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private float spawnEdgeOffset = 1f;

    private BoundsInt mapBounds;
    private Vector2 mapSize;

    public string MapName => mapName;
    public Vector2 MapSize => mapSize;
    public Tilemap FloorTilemap => floorTilemap;
    public Tilemap WallTilemap => wallTilemap;
    private Vector2 mapCenter;
    public Vector2 MapCenter => mapCenter;

    private Dictionary<Vector2Int, bool> collisionCache = new Dictionary<Vector2Int, bool>();
    private bool useCachedCollisions = true;

    private void Awake()
    {
        InitializeMapBounds();
        //CenterMapToOrigin(); // ���� ������ �߽� ��ġ
        // ���� Ȯ���ϴ� �浹 ��ġ �̸� ĳ��
        if (wallTilemap != null && useCachedCollisions)
        {
            PrecomputeCollisions();
        }
    }
    private void PrecomputeCollisions()
    {
        BoundsInt bounds = wallTilemap.cellBounds;
        collisionCache = new Dictionary<Vector2Int, bool>(bounds.size.x * bounds.size.y);

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                Vector2Int key = new Vector2Int(x, y);
                collisionCache[key] = wallTilemap.HasTile(cellPos);
            }
        }
    }


    private void InitializeMapBounds()
    {
        // �ٴ� Ÿ�ϸʿ��� �� ��� ���
        if (floorTilemap != null)
        {
            mapBounds = floorTilemap.cellBounds;
            CalculateMapSize();
        }
        else if (wallTilemap != null)
        {
            mapBounds = wallTilemap.cellBounds;
            CalculateMapSize();
        }
        else
        {
            Debug.LogError("No tilemaps found in GameMap!");
        }
    }

    private void CalculateMapSize()
    {
        // Ÿ�ϸ� ũ�⸦ ���� ������ ��ȯ
        Vector3Int size = new Vector3Int(
            mapBounds.size.x,
            mapBounds.size.y,
            mapBounds.size.z);

        // Ÿ�� ũ�⸦ ����Ͽ� �� ũ�� ���
        Tilemap tilemap = floorTilemap != null ? floorTilemap : wallTilemap;
        mapSize = new Vector2(
            size.x * tilemap.layoutGrid.cellSize.x,
            size.y * tilemap.layoutGrid.cellSize.y);

        // �� �߽��� ���
        // Ÿ�ϸ��� ��� �߾��� ���� �߽��� �Ǿ�� ��
        Vector3Int min = mapBounds.min;
        Vector3Int max = mapBounds.max;
        Vector3 worldMin = tilemap.CellToWorld(min);
        Vector3 worldMax = tilemap.CellToWorld(max);

        // �� ũ�⸦ ����Ͽ� ���� ���� ������ �� �߽� ���
        Vector3 cellSize = tilemap.layoutGrid.cellSize;
        worldMax += new Vector3(cellSize.x, cellSize.y, 0); // ������ ���� ũ�� ���

        mapCenter = new Vector2(
            (worldMin.x + worldMax.x) * 0.5f,
            (worldMin.y + worldMax.y) * 0.5f
        );

        Debug.Log($"Map size: {mapSize}, Map center: {mapCenter}");
    }
    private void CenterMapToOrigin()
    {
        // ���� �߽��� �������� �̵���Ŵ
        Vector3 offset = new Vector3(-mapCenter.x, -mapCenter.y, 0);
        transform.position = offset;

        Debug.Log($"Centering map to origin. Applied offset: {offset}");
    }


    private void OnEnable()
    {
        // �ڽ� ������Ʈ���� ���� ����Ʈ ����
        CollectSpawnPoints();
    }

    private void CollectSpawnPoints()
    {
        // �̹� ���� ����Ʈ�� �����Ǿ� �ִٸ� ���
        if (spawnPoints != null && spawnPoints.Count > 0)
            return;

        // �ڽ� �߿��� SpawnPoint �±׸� ���� ������Ʈ ã��
        spawnPoints = new List<Transform>();

        foreach (Transform child in transform)
        {
            if (child.CompareTag("SpawnPoint"))
            {
                spawnPoints.Add(child);
            }
        }

        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points found in map. Will use generated positions.");
        }
    }

    // ���� ����Ʈ ��������
    public Vector2 GetSpawnPosition(int index = -1)
    {
        // ������ �ε����� ���� ����Ʈ ��ȯ
        if (index >= 0 && index < spawnPoints.Count)
        {
            return spawnPoints[index].position;
        }

        // ���� ���� ����Ʈ ����
        if (spawnPoints.Count > 0)
        {
            int randomIndex = Random.Range(0, spawnPoints.Count);
            return spawnPoints[randomIndex].position;
        }

        // ���� ����Ʈ�� ������ �� �����ڸ����� ���� ��ġ ����
        return GetRandomEdgePosition();
    }

    // �� �����ڸ����� ���� ��ġ ��ȯ
    public Vector2 GetRandomEdgePosition()
    {
        // �� �߽��� ���� (0,0)�� �����Ƿ�, �����ڸ� ��굵 �׿� �°� ����
        float halfWidth = mapSize.x / 2f;
        float halfHeight = mapSize.y / 2f;

        int side = Random.Range(0, 4);
        Vector2 position;

        switch (side)
        {
            case 0: // ���
                position = new Vector2(
                    Random.Range(-halfWidth + spawnEdgeOffset, halfWidth - spawnEdgeOffset),
                    halfHeight - spawnEdgeOffset);
                break;
            case 1: // ����
                position = new Vector2(
                    halfWidth - spawnEdgeOffset,
                    Random.Range(-halfHeight + spawnEdgeOffset, halfHeight - spawnEdgeOffset));
                break;
            case 2: // �ϴ�
                position = new Vector2(
                    Random.Range(-halfWidth + spawnEdgeOffset, halfWidth - spawnEdgeOffset),
                    -halfHeight + spawnEdgeOffset);
                break;
            case 3: // ����
                position = new Vector2(
                    -halfWidth + spawnEdgeOffset,
                    Random.Range(-halfHeight + spawnEdgeOffset, halfHeight - spawnEdgeOffset));
                break;
            default:
                position = Vector2.zero;
                break;
        }

        return position;
    }

    // �� ���ο� ���� ��ġ ����
    public Vector2 GetRandomPositionInMap()
    {
        float halfWidth = mapSize.x / 2f;
        float halfHeight = mapSize.y / 2f;

        // �� ���ο��� ���� ��ġ ����
        Vector2 randomPosition;
        int maxAttempts = 10;

        do
        {
            randomPosition = new Vector2(
                Random.Range(-halfWidth + spawnEdgeOffset, halfWidth - spawnEdgeOffset),
                Random.Range(-halfHeight + spawnEdgeOffset, halfHeight - spawnEdgeOffset)
            );

            maxAttempts--;
        }
        while (IsPositionColliding(randomPosition) && maxAttempts > 0);

        return randomPosition;
    }

    // �ش� ��ġ�� ���� �浹�ϴ��� Ȯ��
    public bool IsPositionColliding(Vector2 worldPosition)
    {
        if (wallTilemap == null) return false;

        Vector3Int cellPosition = wallTilemap.WorldToCell(worldPosition);
        Vector2Int key = new Vector2Int(cellPosition.x, cellPosition.y);

        if (useCachedCollisions && collisionCache.TryGetValue(key, out bool hasCollision))
        {
            return hasCollision;
        }

        return wallTilemap.HasTile(cellPosition);
    }
    // ��ġ�� �� ���ο� �ִ��� Ȯ��
    public bool IsPositionInMap(Vector2 position)
    {
        float halfWidth = mapSize.x / 2f;
        float halfHeight = mapSize.y / 2f;

        return position.x >= -halfWidth && position.x <= halfWidth &&
               position.y >= -halfHeight && position.y <= halfHeight;
    }

   #if UNITY_EDITOR
private void OnDrawGizmos()
{
    // �� ��� �׸���
    if (Application.isPlaying && mapSize != Vector2.zero)
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(mapSize.x, mapSize.y, 0f));
    }
    
    // ���� ����Ʈ �׸���
    Gizmos.color = Color.red;
    if (spawnPoints != null)
    {
        foreach (var point in spawnPoints)
        {
            if (point != null)
            {
                Gizmos.DrawSphere(point.position, 0.5f);
            }
        }
    }
}
#endif
}