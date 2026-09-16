using System;
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
        readonly Dictionary<GridPosition, float> _roads = new();

        public bool InBounds(GridPosition p) => p.x >= 0 && p.y >= 0 && p.x < width && p.y < height;
        public bool IsOccupied(GridPosition p) => _occupied.Contains(p);
        public bool HasRoad(GridPosition p) => _roads.ContainsKey(p);

        public bool CanOccupyRect(GridPosition anchor, Vector2Int size, bool rejectRoads = true)
        {
            size = NormalizeSize(size);
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            {
                GridPosition p = new(anchor.x + x, anchor.y + y);
                if (!InBounds(p) || IsOccupied(p)) return false;
                if (rejectRoads && HasRoad(p)) return false;
            }
            return true;
        }

        public bool TryOccupy(GridPosition p) => TryOccupyRect(p, Vector2Int.one);

        public bool TryOccupyRect(GridPosition anchor, Vector2Int size, bool rejectRoads = true)
        {
            if (!CanOccupyRect(anchor, size, rejectRoads)) return false;
            size = NormalizeSize(size);
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                _occupied.Add(new GridPosition(anchor.x + x, anchor.y + y));
            return true;
        }

        public void Release(GridPosition p) => ReleaseRect(p, Vector2Int.one);

        public void ReleaseRect(GridPosition anchor, Vector2Int size)
        {
            size = NormalizeSize(size);
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                _occupied.Remove(new GridPosition(anchor.x + x, anchor.y + y));
        }

        public void SetRoad(GridPosition p, float speedMultiplier)
        {
            if (!InBounds(p)) return;
            _roads[p] = Mathf.Clamp(speedMultiplier, 1f, 3f);
        }

        public void RemoveRoad(GridPosition p) => _roads.Remove(p);

        public float GetMoveSpeedMultiplier(Vector3 world)
        {
            GridPosition p = WorldToGrid(world);
            return _roads.TryGetValue(p, out float value) ? value : 1f;
        }

        public Vector3 GridToWorld(GridPosition p) =>
            transform.position + new Vector3((p.x + 0.5f) * cellSize, 0f, (p.y + 0.5f) * cellSize);

        public Vector3 RectCenterToWorld(GridPosition anchor, Vector2Int size)
        {
            size = NormalizeSize(size);
            return transform.position + new Vector3(
                (anchor.x + size.x * 0.5f) * cellSize,
                0f,
                (anchor.y + size.y * 0.5f) * cellSize);
        }

        public GridPosition WorldToGrid(Vector3 world)
        {
            Vector3 local = world - transform.position;
            return new GridPosition(Mathf.FloorToInt(local.x / cellSize), Mathf.FloorToInt(local.z / cellSize));
        }

        public bool TryFindPath(Vector3 startWorld, Vector3 targetWorld, List<Vector3> result)
        {
            if (result == null) return false;
            result.Clear();

            GridPosition start = WorldToGrid(startWorld);
            GridPosition goal = WorldToGrid(targetWorld);
            if (!InBounds(start) || !InBounds(goal)) return false;

            if (start == goal)
            {
                result.Add(new Vector3(targetWorld.x, startWorld.y, targetWorld.z));
                return true;
            }

            var open = new List<GridPosition> { start };
            var closed = new HashSet<GridPosition>();
            var cameFrom = new Dictionary<GridPosition, GridPosition>();
            var gScore = new Dictionary<GridPosition, float> { [start] = 0f };
            var fScore = new Dictionary<GridPosition, float> { [start] = Heuristic(start, goal) };

            int guard = width * height * 8;
            while (open.Count > 0 && guard-- > 0)
            {
                int bestIndex = 0;
                float bestF = Score(fScore, open[0]);
                for (int i = 1; i < open.Count; i++)
                {
                    float f = Score(fScore, open[i]);
                    if (f >= bestF) continue;
                    bestF = f;
                    bestIndex = i;
                }

                GridPosition current = open[bestIndex];
                open.RemoveAt(bestIndex);
                if (current == goal)
                {
                    ReconstructPath(cameFrom, current, start, targetWorld, startWorld.y, result);
                    return true;
                }

                closed.Add(current);
                for (int i = 0; i < 4; i++)
                {
                    GridPosition neighbor = Neighbor(current, i);
                    if (!InBounds(neighbor) || closed.Contains(neighbor)) continue;
                    if (IsOccupied(neighbor) && neighbor != goal && neighbor != start) continue;

                    float tentative = Score(gScore, current) + MoveCost(neighbor);
                    if (tentative >= Score(gScore, neighbor)) continue;

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentative;
                    fScore[neighbor] = tentative + Heuristic(neighbor, goal);
                    if (!open.Contains(neighbor)) open.Add(neighbor);
                }
            }

            return false;
        }

        float MoveCost(GridPosition p)
        {
            if (_roads.TryGetValue(p, out float speed)) return 1f / Mathf.Max(1f, speed);
            return 1f;
        }

        static float Heuristic(GridPosition a, GridPosition b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        static float Score(Dictionary<GridPosition, float> scores, GridPosition p) =>
            scores.TryGetValue(p, out float value) ? value : float.PositiveInfinity;

        static GridPosition Neighbor(GridPosition p, int index)
        {
            switch (index)
            {
                case 0: return new GridPosition(p.x + 1, p.y);
                case 1: return new GridPosition(p.x - 1, p.y);
                case 2: return new GridPosition(p.x, p.y + 1);
                default: return new GridPosition(p.x, p.y - 1);
            }
        }

        void ReconstructPath(Dictionary<GridPosition, GridPosition> cameFrom, GridPosition current, GridPosition start, Vector3 targetWorld, float y, List<Vector3> result)
        {
            var cells = new List<GridPosition> { current };
            while (current != start && cameFrom.TryGetValue(current, out GridPosition previous))
            {
                current = previous;
                cells.Add(current);
            }
            cells.Reverse();

            for (int i = 1; i < cells.Count; i++)
            {
                Vector3 world = GridToWorld(cells[i]);
                result.Add(new Vector3(world.x, y, world.z));
            }

            Vector3 exact = new(targetWorld.x, y, targetWorld.z);
            if (result.Count == 0 || Vector3.Distance(result[result.Count - 1], exact) > 0.15f)
                result.Add(exact);
        }

        static Vector2Int NormalizeSize(Vector2Int size) => new(Mathf.Max(1, size.x), Mathf.Max(1, size.y));

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
