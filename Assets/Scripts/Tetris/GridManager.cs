using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public sealed class GridManager : MonoBehaviour
{
    [Serializable]
    public sealed class GridCell
    {
        [SerializeField] private Vector2Int coordinate;
        [SerializeField] private Vector3 worldPosition;

        public Vector2Int Coordinate => coordinate;
        public Vector3 WorldPosition => worldPosition;

        public GridCell(Vector2Int coordinate, Vector3 worldPosition)
        {
            this.coordinate = coordinate;
            this.worldPosition = worldPosition;
        }
    }

    [Header("Grid Settings")]
    [SerializeField] private int columns = 10;
    [SerializeField] private int rows = 20;
    [SerializeField] private float cellWidth = 3f;
    [SerializeField] private float cellHeight = 5f;
    [SerializeField] private Vector2 originOffset = new Vector2(-13.5f, -47.5f);

    [Header("Preview")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color gridColor = new Color(0.2f, 0.8f, 1f, 0.85f);
    [SerializeField] private List<GridCell> previewCells = new List<GridCell>();

    public int Columns => columns;
    public int Rows => rows;
    public float CellWidth => cellWidth;
    public float CellHeight => cellHeight;
    public IReadOnlyList<GridCell> PreviewCells => previewCells;

    public Vector3 GetCellCenter(Vector2Int coordinate)
    {
        Vector3 origin = transform.position + new Vector3(originOffset.x, originOffset.y, 0f);
        return origin + new Vector3((coordinate.x + 0.5f) * cellWidth, (coordinate.y + 0.5f) * cellHeight, 0f);
    }

    public bool Contains(Vector2Int coordinate)
    {
        return coordinate.x >= 0 && coordinate.x < columns && coordinate.y >= 0 && coordinate.y < rows;
    }

    public bool TryGetCell(Vector2Int coordinate, out GridCell cell)
    {
        if (!Contains(coordinate))
        {
            cell = null;
            return false;
        }

        cell = new GridCell(coordinate, GetCellCenter(coordinate));
        return true;
    }

    public Vector2Int GetNearestCoordinate(Vector3 worldPosition)
    {
        Vector3 origin = transform.position + new Vector3(originOffset.x, originOffset.y, 0f);
        Vector3 local = worldPosition - origin;

        int x = Mathf.FloorToInt(local.x / cellWidth);
        int y = Mathf.FloorToInt(local.y / cellHeight);

        return new Vector2Int(
            Mathf.Clamp(x, 0, Mathf.Max(0, columns - 1)),
            Mathf.Clamp(y, 0, Mathf.Max(0, rows - 1)));
    }

    private void Reset()
    {
        RegeneratePreviewCells();
    }

    private void OnValidate()
    {
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
        cellWidth = Mathf.Max(0.01f, cellWidth);
        cellHeight = Mathf.Max(0.01f, cellHeight);

        RegeneratePreviewCells();
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
        {
            return;
        }

#if UNITY_EDITOR
        if (Camera.current != null && Camera.current.cameraType != CameraType.SceneView)
        {
            return;
        }
#endif

        Gizmos.color = gridColor;

        Vector3 origin = transform.position + new Vector3(originOffset.x, originOffset.y, 0f);
        Vector3 size = new Vector3(columns * cellWidth, rows * cellHeight, 0f);
        Vector3 right = Vector3.right * cellWidth;
        Vector3 up = Vector3.up * cellHeight;

        for (int x = 0; x <= columns; x++)
        {
            Vector3 start = origin + (Vector3.right * x * cellWidth);
            Vector3 end = start + Vector3.up * size.y;
            Gizmos.DrawLine(start, end);
        }

        for (int y = 0; y <= rows; y++)
        {
            Vector3 start = origin + (Vector3.up * y * cellHeight);
            Vector3 end = start + Vector3.right * size.x;
            Gizmos.DrawLine(start, end);
        }

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                Vector3 center = origin + (right * (x + 0.5f)) + (up * (y + 0.5f));
                Gizmos.DrawWireCube(center, new Vector3(cellWidth, cellHeight, 0.05f));
            }
        }
    }

    private void RegeneratePreviewCells()
    {
        if (previewCells == null)
        {
            previewCells = new List<GridCell>();
        }

        previewCells.Clear();

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                Vector2Int coordinate = new Vector2Int(x, y);
                previewCells.Add(new GridCell(coordinate, GetCellCenter(coordinate)));
            }
        }
    }
}
