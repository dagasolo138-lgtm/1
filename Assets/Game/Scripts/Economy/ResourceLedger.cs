using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShanMen.Economy
{
    public enum ResourceType { Wood, Stone, Food, Herb, SpiritStone }

    public sealed class ResourceLedger : MonoBehaviour
    {
        [Serializable]
        public struct StartingResource { public ResourceType type; public int amount; }

        public StartingResource[] starting =
        {
            new() { type = ResourceType.Wood, amount = 120 },
            new() { type = ResourceType.Stone, amount = 80 },
            new() { type = ResourceType.Food, amount = 50 },
            new() { type = ResourceType.Herb, amount = 0 },
            new() { type = ResourceType.SpiritStone, amount = 10 },
        };

        readonly Dictionary<ResourceType, int> _amounts = new();
        public event Action Changed;

        void Awake()
        {
            foreach (ResourceType t in Enum.GetValues(typeof(ResourceType))) _amounts[t] = 0;
            foreach (var item in starting) _amounts[item.type] = Mathf.Max(0, item.amount);
        }

        public int Get(ResourceType type) => _amounts.TryGetValue(type, out int value) ? value : 0;

        public bool CanAfford(IReadOnlyList<ResourceCost> costs)
        {
            for (int i = 0; i < costs.Count; i++)
                if (Get(costs[i].type) < costs[i].amount) return false;
            return true;
        }

        public bool Spend(IReadOnlyList<ResourceCost> costs)
        {
            if (!CanAfford(costs)) return false;
            for (int i = 0; i < costs.Count; i++) Add(costs[i].type, -costs[i].amount, false);
            Changed?.Invoke();
            return true;
        }

        public void Add(ResourceType type, int amount, bool notify = true)
        {
            _amounts[type] = Mathf.Max(0, Get(type) + amount);
            if (notify) Changed?.Invoke();
        }
    }

    [Serializable]
    public struct ResourceCost
    {
        public ResourceType type;
        [Min(0)] public int amount;
        public ResourceCost(ResourceType type, int amount) { this.type = type; this.amount = amount; }
    }
}
