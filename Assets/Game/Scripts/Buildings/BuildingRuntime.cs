using System;
using ShanMen.Characters;
using ShanMen.Core;
using ShanMen.Cultivation;
using ShanMen.Economy;
using ShanMen.Grid;
using ShanMen.Jobs;
using UnityEngine;

namespace ShanMen.Buildings
{
    public sealed class BuildingRuntime : MonoBehaviour
    {
        [SerializeField] BuildingDefinition definition;
        [SerializeField] GridPosition gridPosition;

        GameClock _clock;
        GridMap _grid;
        JobBoard _jobs;
        ResourceLedger _ledger;
        ResourceContainer _input;
        ResourceContainer _output;
        ResourceContainer _storage;
        bool _jobOutstanding;
        bool _subscribed;
        int _tickCounter;

        public BuildingDefinition Definition => definition;
        public GridPosition GridPosition => gridPosition;

        public void Configure(BuildingDefinition newDefinition, GridPosition cell)
        {
            definition = newDefinition;
            gridPosition = cell;
        }

        void Start()
        {
            _clock = FindFirstObjectByType<GameClock>();
            _grid = FindFirstObjectByType<GridMap>();
            _jobs = FindFirstObjectByType<JobBoard>();
            _ledger = FindFirstObjectByType<ResourceLedger>();

            if (_grid != null) _grid.TryOccupy(gridPosition);
            EnsureRuntimeParts();
            Subscribe();
        }

        void OnDestroy()
        {
            if (_subscribed && _clock != null) _clock.Tick -= OnTick;
            _subscribed = false;
        }

        void Subscribe()
        {
            if (_subscribed || _clock == null) return;
            _clock.Tick += OnTick;
            _subscribed = true;
        }

        void EnsureRuntimeParts()
        {
            if (definition == null) return;

            ResourceContainer[] existing = GetComponentsInChildren<ResourceContainer>(true);
            for (int i = 0; i < existing.Length; i++)
            {
                switch (existing[i].role)
                {
                    case ResourceContainerRole.Input: _input = existing[i]; break;
                    case ResourceContainerRole.Output: _output = existing[i]; break;
                    case ResourceContainerRole.Stockpile: _storage = existing[i]; break;
                }
                existing[i].Bind(_ledger);
            }

            switch (definition.behavior)
            {
                case BuildingBehavior.Producer:
                    if (_output == null) _output = CreateContainer("Output", ResourceContainerRole.Output, definition.outputCapacity, OutputTypes(), Array.Empty<ResourceTarget>());
                    break;
                case BuildingBehavior.Processor:
                    if (_input == null) _input = CreateContainer("Input", ResourceContainerRole.Input, definition.inputCapacity, InputTypes(), InputTargets());
                    if (_output == null) _output = CreateContainer("Output", ResourceContainerRole.Output, definition.outputCapacity, OutputTypes(), Array.Empty<ResourceTarget>());
                    break;
                case BuildingBehavior.Storage:
                    if (_storage == null) _storage = CreateContainer("Storage", ResourceContainerRole.Stockpile, definition.storageCapacity, Array.Empty<ResourceType>(), Array.Empty<ResourceTarget>());
                    break;
                case BuildingBehavior.QiSource:
                    QiSource source = GetComponent<QiSource>();
                    if (source == null) source = gameObject.AddComponent<QiSource>();
                    source.strength = definition.qiStrength;
                    source.radius = definition.qiRadius;
                    break;
                case BuildingBehavior.Rest:
                    if (GetComponent<RestSpot>() == null) gameObject.AddComponent<RestSpot>();
                    break;
            }
        }

        ResourceContainer CreateContainer(string objectName, ResourceContainerRole role, int capacity, ResourceType[] accepted, ResourceTarget[] desired)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            var container = go.AddComponent<ResourceContainer>();
            container.Configure(role, capacity, accepted, desired);
            container.Bind(_ledger);
            return container;
        }

        void OnTick(long tick)
        {
            if (definition == null || _jobs == null || _jobOutstanding) return;
            _tickCounter++;
            if (_tickCounter % Mathf.Max(1, definition.jobIntervalTicks) != 0) return;

            switch (definition.behavior)
            {
                case BuildingBehavior.Producer:
                    if (_output != null && _output.CanFit(definition.outputs))
                        QueueWork(() => Produce(definition.outputs));
                    break;
                case BuildingBehavior.Processor:
                    if (_input != null && _output != null && _input.Has(definition.inputs) && _output.CanFit(definition.outputs))
                        QueueWork(ProcessRecipe);
                    break;
                case BuildingBehavior.Cultivation:
                    QueueWork(null);
                    break;
            }
        }

        void QueueWork(Action completed)
        {
            _jobOutstanding = true;
            _jobs.Add(definition.workType, transform.position, definition.jobWork, () =>
            {
                _jobOutstanding = false;
                completed?.Invoke();
            });
        }

        void ProcessRecipe()
        {
            if (!_input.TryConsume(definition.inputs)) return;
            Produce(definition.outputs);
        }

        void Produce(ResourceCost[] outputs)
        {
            if (_output == null || outputs == null) return;
            for (int i = 0; i < outputs.Length; i++)
                _output.TryAdd(outputs[i].type, outputs[i].amount);
        }

        ResourceType[] InputTypes()
        {
            if (definition.inputs == null) return Array.Empty<ResourceType>();
            var result = new ResourceType[definition.inputs.Length];
            for (int i = 0; i < result.Length; i++) result[i] = definition.inputs[i].type;
            return result;
        }

        ResourceTarget[] InputTargets()
        {
            if (definition.inputs == null) return Array.Empty<ResourceTarget>();
            var result = new ResourceTarget[definition.inputs.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = new ResourceTarget(definition.inputs[i].type, definition.inputs[i].amount * Mathf.Max(1, definition.inputBufferCycles));
            return result;
        }

        ResourceType[] OutputTypes()
        {
            if (definition.outputs == null) return Array.Empty<ResourceType>();
            var result = new ResourceType[definition.outputs.Length];
            for (int i = 0; i < result.Length; i++) result[i] = definition.outputs[i].type;
            return result;
        }
    }
}
