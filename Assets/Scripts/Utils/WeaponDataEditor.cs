// WeaponDataEditor.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponData))]
public class WeaponDataEditor : Editor
{
    private bool[,] editingShape;
    private Vector2Int gridSize = new Vector2Int(3, 3);

    public override void OnInspectorGUI()
    {
        WeaponData weaponData = (WeaponData)target;

        // �⺻ �ν����� ������Ƽ�� ǥ��
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Weapon Shape Editor", EditorStyles.boldLabel);

        // �׸��� ũ�� ����
        gridSize = EditorGUILayout.Vector2IntField("Grid Size", gridSize);
        if (gridSize.x < 1) gridSize.x = 1;
        if (gridSize.y < 1) gridSize.y = 1;

        // �ʱ� shape �迭�� ���ų� ũ�Ⱑ �ٸ��� ���� ����
        if (editingShape == null ||
            editingShape.GetLength(0) != gridSize.x ||
            editingShape.GetLength(1) != gridSize.y)
        {
            editingShape = new bool[gridSize.x, gridSize.y];

            // ���� �����Ͱ� �ִٸ� ����
            if (!string.IsNullOrEmpty(weaponData.shapeLayout))
            {
                string[] rows = weaponData.shapeLayout.Split('/');
                for (int y = 0; y < Mathf.Min(rows.Length, gridSize.y); y++)
                {
                    string[] cols = rows[y].Split(',');
                    for (int x = 0; x < Mathf.Min(cols.Length, gridSize.x); x++)
                    {
                        editingShape[x, y] = cols[x] == "1";
                    }
                }
            }
        }

        // �׸��� ������ �׸���
        EditorGUILayout.Space();
        var rect = GUILayoutUtility.GetRect(200, 200);
        float cellSize = Mathf.Min(rect.width / gridSize.x, rect.height / gridSize.y);

        for (int y = 0; y < gridSize.y; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < gridSize.x; x++)
            {
                // ��� ��ư���� �� ���� ����
                editingShape[x, y] = EditorGUILayout.Toggle(editingShape[x, y],
                    GUILayout.Width(cellSize), GUILayout.Height(cellSize));
            }
            EditorGUILayout.EndHorizontal();
        }

        // ������� ���� ��ư
        if (GUILayout.Button("Apply Shape"))
        {
            // bool[,] �迭�� ���ڿ��� ��ȯ
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int y = 0; y < gridSize.y; y++)
            {
                if (y > 0) sb.Append('/');
                for (int x = 0; x < gridSize.x; x++)
                {
                    if (x > 0) sb.Append(',');
                    sb.Append(editingShape[x, y] ? "1" : "0");
                }
            }

            // Undo ���
            Undo.RecordObject(weaponData, "Update Weapon Shape");

            // ������ ������Ʈ
            weaponData.shapeLayout = sb.ToString();
            weaponData.size = gridSize;

            // ���� ����
            EditorUtility.SetDirty(weaponData);
        }
    }
}
#endif
