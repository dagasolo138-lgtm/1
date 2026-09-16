using System;
using System.Collections.Generic;
using ShanMen.Grid;
using UnityEngine;

namespace ShanMen.FiveElements
{
    public enum FiveElement { Neutral, Wood, Fire, Earth, Metal, Water }

    [Serializable]
    public struct ElementValues
    {
        public float wood; public float fire; public float earth; public float metal; public float water;
        public float Get(FiveElement element)
        {
            switch (element)
            {
                case FiveElement.Wood: return wood; case FiveElement.Fire: return fire; case FiveElement.Earth: return earth;
                case FiveElement.Metal: return metal; case FiveElement.Water: return water; default: return 0f;
            }
        }
        public void Add(FiveElement element, float value)
        {
            switch (element)
            {
                case FiveElement.Wood: wood += value; break; case FiveElement.Fire: fire += value; break;
                case FiveElement.Earth: earth += value; break; case FiveElement.Metal: metal += value; break;
                case FiveElement.Water: water += value; break;
            }
        }
    }

    public sealed class FiveElementField : MonoBehaviour
    {
        public GridMap grid;
        readonly List<FiveElementSource> _sources = new();
        readonly Dictionary<GridPosition, ElementValues> _cache = new();
        bool _dirty = true;

        public void Register(FiveElementSource source)
        {
            if (source == null || source.element == FiveElement.Neutral || _sources.Contains(source)) return;
            _sources.Add(source); _dirty = true;
        }
        public void Unregister(FiveElementSource source) { if (_sources.Remove(source)) _dirty = true; }
        public void MarkDirty() => _dirty = true;
        public ElementValues Sample(Vector3 world)
        {
            if (grid == null) return default;
            if (_dirty) Rebuild();
            GridPosition p = grid.WorldToGrid(world);
            return _cache.TryGetValue(p, out ElementValues values) ? values : default;
        }
        public float GetWorkMultiplier(Vector3 world, FiveElement target, float sensitivity)
        {
            if (target == FiveElement.Neutral || sensitivity <= 0f) return 1f;
            ElementValues values = Sample(world);
            float support = values.Get(target) * 0.25f + values.Get(GeneratingElement(target)) * 0.6f - values.Get(ControllingElement(target)) * 0.65f;
            float delta = Mathf.Clamp(support / 80f, -0.35f, 0.5f) * Mathf.Clamp01(sensitivity);
            return Mathf.Clamp(1f + delta, 0.65f, 1.5f);
        }
        public static FiveElement GeneratingElement(FiveElement target)
        {
            switch (target)
            {
                case FiveElement.Wood: return FiveElement.Water; case FiveElement.Fire: return FiveElement.Wood;
                case FiveElement.Earth: return FiveElement.Fire; case FiveElement.Metal: return FiveElement.Earth;
                case FiveElement.Water: return FiveElement.Metal; default: return FiveElement.Neutral;
            }
        }
        public static FiveElement ControllingElement(FiveElement target)
        {
            switch (target)
            {
                case FiveElement.Wood: return FiveElement.Metal; case FiveElement.Fire: return FiveElement.Water;
                case FiveElement.Earth: return FiveElement.Wood; case FiveElement.Metal: return FiveElement.Fire;
                case FiveElement.Water: return FiveElement.Earth; default: return FiveElement.Neutral;
            }
        }
        void Rebuild()
        {
            _cache.Clear();
            if (grid == null) { _dirty = false; return; }
            for (int x = 0; x < grid.width; x++) for (int y = 0; y < grid.height; y++)
            {
                GridPosition p = new(x, y); Vector3 world = grid.GridToWorld(p); ElementValues values = default;
                for (int i = 0; i < _sources.Count; i++)
                {
                    FiveElementSource source = _sources[i]; if (source == null || source.element == FiveElement.Neutral) continue;
                    float distance = Vector3.Distance(world, source.transform.position); if (distance > source.radius) continue;
                    float t = 1f - distance / Mathf.Max(0.1f, source.radius); values.Add(source.element, source.strength * t * t);
                }
                _cache[p] = values;
            }
            _dirty = false;
        }
    }
}
