using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShanMen.Economy
{
    public enum ResourceContainerRole { Stockpile, Input, Output, Construction }

    [Serializable]
    public struct ResourceTarget
    {
        public ResourceType type;
        [Min(0)] public int desired;
        public ResourceTarget(ResourceType type, int desired) { this.type = type; this.desired = desired; }
    }

    public sealed class ResourceContainer : MonoBehaviour
    {
        public ResourceContainerRole role = ResourceContainerRole.Stockpile;
        [Min(1)] public int capacity = 100;
        public ResourceType[] accepted = Array.Empty<ResourceType>();
        public ResourceTarget[] desired = Array.Empty<ResourceTarget>();
        public ResourceAmount[] starting = Array.Empty<ResourceAmount>();

        readonly Dictionary<ResourceType, int> _amounts = new();
        readonly Dictionary<ResourceType, int> _reservedPickup = new();
        readonly Dictionary<ResourceType, int> _reservedDropoff = new();
        ResourceLedger _ledger;

        public int TotalAmount
        {
            get
            {
                int total = 0;
                foreach (int amount in _amounts.Values) total += amount;
                return total;
            }
        }

        int ReservedIncomingTotal
        {
            get
            {
                int total = 0;
                foreach (int amount in _reservedDropoff.Values) total += amount;
                return total;
            }
        }

        void Awake()
        {
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                _amounts[type] = 0;
                _reservedPickup[type] = 0;
                _reservedDropoff[type] = 0;
            }

            if (starting == null) return;
            for (int i = 0; i < starting.Length; i++)
                _amounts[starting[i].type] = Mathf.Max(0, starting[i].amount);
        }

        void Start()
        {
            if (_ledger == null) Bind(FindFirstObjectByType<ResourceLedger>());
        }

        void OnDestroy()
        {
            if (_ledger != null) _ledger.Unregister(this);
        }

        public void Configure(ResourceContainerRole newRole, int newCapacity, ResourceType[] acceptedTypes, ResourceTarget[] targets)
        {
            role = newRole;
            capacity = Mathf.Max(1, newCapacity);
            accepted = acceptedTypes ?? Array.Empty<ResourceType>();
            desired = targets ?? Array.Empty<ResourceTarget>();
        }

        public void SetStarting(ResourceAmount[] values) => starting = values ?? Array.Empty<ResourceAmount>();

        public void Bind(ResourceLedger ledger)
        {
            if (_ledger == ledger) return;
            if (_ledger != null) _ledger.Unregister(this);
            _ledger = ledger;
            if (_ledger != null) _ledger.Register(this);
        }

        public int Get(ResourceType type) => _amounts.TryGetValue(type, out int value) ? value : 0;
        public int ReservedIncoming(ResourceType type) => _reservedDropoff.TryGetValue(type, out int value) ? value : 0;
        public int AvailableForPickup(ResourceType type) => Mathf.Max(0, Get(type) - ReservedPickup(type));
        public int Desired(ResourceType type)
        {
            if (desired == null) return 0;
            for (int i = 0; i < desired.Length; i++)
                if (desired[i].type == type) return Mathf.Max(0, desired[i].desired);
            return 0;
        }

        public bool Accepts(ResourceType type)
        {
            if (accepted == null || accepted.Length == 0) return true;
            for (int i = 0; i < accepted.Length; i++)
                if (accepted[i] == type) return true;
            return false;
        }

        public int AvailableCapacityFor(ResourceType type)
        {
            if (!Accepts(type)) return 0;
            return Mathf.Max(0, capacity - TotalAmount - ReservedIncomingTotal);
        }

        public bool Has(IReadOnlyList<ResourceCost> costs)
        {
            if (costs == null) return true;
            for (int i = 0; i < costs.Count; i++)
                if (Get(costs[i].type) < costs[i].amount) return false;
            return true;
        }

        public bool CanFit(IReadOnlyList<ResourceCost> amounts)
        {
            if (amounts == null) return true;
            int required = 0;
            for (int i = 0; i < amounts.Count; i++)
            {
                if (!Accepts(amounts[i].type)) return false;
                required += Mathf.Max(0, amounts[i].amount);
            }
            return capacity - TotalAmount - ReservedIncomingTotal >= required;
        }

        public bool TryConsume(IReadOnlyList<ResourceCost> costs)
        {
            if (!Has(costs)) return false;
            for (int i = 0; i < costs.Count; i++)
                _amounts[costs[i].type] = Mathf.Max(0, Get(costs[i].type) - costs[i].amount);
            NotifyChanged();
            return true;
        }

        public bool TryAdd(ResourceType type, int amount)
        {
            if (amount <= 0) return true;
            if (AvailableCapacityFor(type) < amount) return false;
            _amounts[type] = Get(type) + amount;
            NotifyChanged();
            return true;
        }

        public bool ReservePickup(ResourceType type, int amount)
        {
            if (amount <= 0 || AvailableForPickup(type) < amount) return false;
            _reservedPickup[type] = ReservedPickup(type) + amount;
            return true;
        }

        public bool ReserveDropoff(ResourceType type, int amount)
        {
            if (amount <= 0 || AvailableCapacityFor(type) < amount) return false;
            _reservedDropoff[type] = ReservedIncoming(type) + amount;
            return true;
        }

        public bool TryTakeReserved(ResourceType type, int amount)
        {
            if (amount <= 0 || ReservedPickup(type) < amount || Get(type) < amount) return false;
            _reservedPickup[type] = Mathf.Max(0, ReservedPickup(type) - amount);
            _amounts[type] = Mathf.Max(0, Get(type) - amount);
            NotifyChanged();
            return true;
        }

        public bool TryStoreReserved(ResourceType type, int amount)
        {
            if (amount <= 0 || ReservedIncoming(type) < amount) return false;
            _reservedDropoff[type] = Mathf.Max(0, ReservedIncoming(type) - amount);
            _amounts[type] = Get(type) + amount;
            NotifyChanged();
            return true;
        }

        public void ReleasePickup(ResourceType type, int amount)
        {
            _reservedPickup[type] = Mathf.Max(0, ReservedPickup(type) - Mathf.Max(0, amount));
        }

        public void ReleaseDropoff(ResourceType type, int amount)
        {
            _reservedDropoff[type] = Mathf.Max(0, ReservedIncoming(type) - Mathf.Max(0, amount));
        }

        int ReservedPickup(ResourceType type) => _reservedPickup.TryGetValue(type, out int value) ? value : 0;
        void NotifyChanged() => _ledger?.NotifyChanged();
    }
}
