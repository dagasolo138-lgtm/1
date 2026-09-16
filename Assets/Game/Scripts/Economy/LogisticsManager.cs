using System;
using ShanMen.Core;
using ShanMen.Jobs;
using UnityEngine;

namespace ShanMen.Economy
{
    public sealed class LogisticsManager : MonoBehaviour
    {
        public GameClock clock;
        public ResourceLedger ledger;
        public JobBoard jobs;
        [Min(1)] public int maxStackSize = 8;

        bool _subscribed;

        void Start()
        {
            if (clock == null) clock = FindFirstObjectByType<GameClock>();
            if (ledger == null) ledger = FindFirstObjectByType<ResourceLedger>();
            if (jobs == null) jobs = FindFirstObjectByType<JobBoard>();
            Subscribe();
        }

        void OnDisable()
        {
            if (_subscribed && clock != null) clock.Tick -= OnTick;
            _subscribed = false;
        }

        void Subscribe()
        {
            if (_subscribed || clock == null) return;
            clock.Tick += OnTick;
            _subscribed = true;
        }

        void OnTick(long tick)
        {
            if (ledger == null || jobs == null) return;
            PlanDemandHauls();
            PlanOutputHauls();
        }

        void PlanDemandHauls()
        {
            for (int i = 0; i < ledger.Containers.Count; i++)
            {
                ResourceContainer destination = ledger.Containers[i];
                if (destination == null) continue;
                if (destination.role != ResourceContainerRole.Input && destination.role != ResourceContainerRole.Construction) continue;

                foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
                {
                    int desired = destination.Desired(type);
                    if (desired <= 0) continue;
                    int need = desired - destination.Get(type) - destination.ReservedIncoming(type);
                    if (need <= 0) continue;

                    ResourceContainer source = FindBestSource(destination, type);
                    if (source == null) continue;
                    int amount = Mathf.Min(maxStackSize, need, source.AvailableForPickup(type), destination.AvailableCapacityFor(type));
                    if (amount > 0) jobs.AddHaul(source, destination, type, amount);
                }
            }
        }

        void PlanOutputHauls()
        {
            for (int i = 0; i < ledger.Containers.Count; i++)
            {
                ResourceContainer source = ledger.Containers[i];
                if (source == null || source.role != ResourceContainerRole.Output) continue;

                foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
                {
                    int available = source.AvailableForPickup(type);
                    if (available <= 0) continue;
                    ResourceContainer destination = FindBestStockpile(source, type);
                    if (destination == null) continue;
                    int amount = Mathf.Min(maxStackSize, available, destination.AvailableCapacityFor(type));
                    if (amount > 0) jobs.AddHaul(source, destination, type, amount);
                }
            }
        }

        ResourceContainer FindBestSource(ResourceContainer destination, ResourceType type)
        {
            ResourceContainer best = null;
            float bestScore = float.PositiveInfinity;
            for (int i = 0; i < ledger.Containers.Count; i++)
            {
                ResourceContainer candidate = ledger.Containers[i];
                if (candidate == null || candidate == destination) continue;
                if (candidate.role != ResourceContainerRole.Output && candidate.role != ResourceContainerRole.Stockpile) continue;
                if (candidate.AvailableForPickup(type) <= 0) continue;

                float roleBias = candidate.role == ResourceContainerRole.Output ? -4f : 0f;
                float score = Vector3.Distance(candidate.transform.position, destination.transform.position) + roleBias;
                if (score >= bestScore) continue;
                bestScore = score;
                best = candidate;
            }
            return best;
        }

        ResourceContainer FindBestStockpile(ResourceContainer source, ResourceType type)
        {
            ResourceContainer best = null;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < ledger.Containers.Count; i++)
            {
                ResourceContainer candidate = ledger.Containers[i];
                if (candidate == null || candidate == source || candidate.role != ResourceContainerRole.Stockpile) continue;
                if (candidate.AvailableCapacityFor(type) <= 0) continue;
                float distance = Vector3.Distance(source.transform.position, candidate.transform.position);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = candidate;
            }
            return best;
        }
    }
}
