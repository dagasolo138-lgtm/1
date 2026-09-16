using System.Collections.Generic;
using ShanMen.Grid;
using UnityEngine;

namespace ShanMen.Cultivation
{
    public sealed class QiField : MonoBehaviour
    {
        public GridMap grid;
        readonly List<QiSource> _sources = new();
        readonly Dictionary<GridPosition, float> _cache = new();
        bool _dirty = true;

        public void Register(QiSource source)
        {
            if (source == null || _sources.Contains(source)) return;
            _sources.Add(source);
            _dirty = true;
        }

        public void Unregister(QiSource source)
        {
            if (_sources.Remove(source)) _dirty = true;
        }

        public void MarkDirty() => _dirty = true;

        public float Sample(Vector3 world)
        {
            if (grid == null) return 0f;
            if (_dirty) Rebuild();
            GridPosition p = grid.WorldToGrid(world);
            return _cache.TryGetValue(p, out float qi) ? qi : 0f;
        }

        void Rebuild()
        {
            _cache.Clear();
            for (int x = 0; x < grid.width; x++)
            for (int y = 0; y < grid.height; y++)
            {
                GridPosition p = new(x, y);
                Vector3 world = grid.GridToWorld(p);
                float total = 0f;
                foreach (var source in _sources)
                {
                    if (source == null) continue;
                    float distance = Vector3.Distance(world, source.transform.position);
                    if (distance > source.radius) continue;
                    float t = 1f - distance / source.radius;
                    total += source.strength * t * t;
                }
                _cache[p] = total;
            }
            _dirty = false;
        }
    }
}
