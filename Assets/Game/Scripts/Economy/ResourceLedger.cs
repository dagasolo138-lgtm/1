using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShanMen.Economy
{
    public enum ResourceType { Wood, Stone, Food, Herb, SpiritStone }

    [Serializable]
    public struct ResourceAmount
    {
        public ResourceType type;
        [Min(0)] public int amount;
        public ResourceAmount(ResourceType type, int amount) { this.type = type; this.amount = amount; }
    }

    [Serializable]
    public struct ResourceCost
    {
        public ResourceType type;
        [Min(0)] public int amount;
        public ResourceCost(ResourceType type, int amount) { this.type = type; this.amount = amount; }
    }

    public sealed class ResourceLedger : MonoBehaviour
    {
        readonly List<ResourceContainer> _containers = new();
        public IReadOnlyList<ResourceContainer> Containers => _containers;
        public event Action Changed;

        public void Register(ResourceContainer container)
        {
            if (container == null || _containers.Contains(container)) return;
            _containers.Add(container);
            Changed?.Invoke();
        }

        public void Unregister(ResourceContainer container)
        {
            if (container != null && _containers.Remove(container)) Changed?.Invoke();
        }

        public int Get(ResourceType type)
        {
            int total = 0;
            for (int i = 0; i < _containers.Count; i++)
                if (_containers[i] != null) total += _containers[i].Get(type);
            return total;
        }

        public int GetSupplyAvailable(ResourceType type)
        {
            int total = 0;
            for (int i = 0; i < _containers.Count; i++)
            {
                ResourceContainer container = _containers[i];
                if (container == null) continue;
                if (container.role != ResourceContainerRole.Stockpile && container.role != ResourceContainerRole.Output) continue;
                total += container.AvailableForPickup(type);
            }
            return total;
        }

        public bool CanSupply(IReadOnlyList<ResourceCost> costs)
        {
            if (costs == null) return true;
            for (int i = 0; i < costs.Count; i++)
                if (GetSupplyAvailable(costs[i].type) < costs[i].amount) return false;
            return true;
        }

        internal void NotifyChanged() => Changed?.Invoke();
    }
}
