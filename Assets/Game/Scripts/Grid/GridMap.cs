using System.Collections.Generic;
using UnityEngine;

namespace ShanMen.Grid
{
    public sealed class GridMap : MonoBehaviour
    {
        [Min(4)] public int width = 24;
        [Min(4)] public int height = 24;
        [Min(0.25f)] public float cellSize = 1f;

        readonly HashSet<GridPosition> _occupied = new();

        public bool InBounds(GridPosition p) => p.x >= 0 && p.y >= 0 && p.x < width && p.y < height;
        public bool IsOccupied(GridPosition p) => _occupied.Contains(p);

        public bool TryOccupy(GridPosition p)
        {
            if (!InBounds(p) || IsOccupied(p)) return false;
            _occupied.Add(p);
            return true;
        }

        public void Release(GridPosition p) => _occupied.Remove(p);

        public Vector3 GridToWorld(GridPosition p) => transform.position + new Vector3((p.x + 0.5f) * cellSize, 0f, (p.y + 0.5f) * cellSize);

        public GridPosition WorldToGrid(Vector3 world)
        {
            Vector3 local = world - transform.position;
            return new GridPosition(Mathf.FloorToInt(local.x / cellSize), Mathf.FloorToInt(local.z / cellSize));
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
            for (int x = 0; x <= width; x++)
            {
                Vector3 a = transform.position + new Vector3(x * cellSize, 0.01f, 0f);
                Vector3 b = a + new Vector3(0f, 0f, height * cellSize);
                Gizmos.DrawLine(a, b);
            }
            for (int y = 0; y <= height; y++)
            {
                Vector3 a = transform.position + new Vector3(0f, 0.01f, y * cellSize);
                Vector3 b = a + new Vector3(width * cellSize, 0f, 0f);
                Gizmos.DrawLine(a, b);
            }
        }
    }
}
