using System;
using ShanMen.Core;
using ShanMen.Cultivation;
using ShanMen.Economy;
using ShanMen.Grid;
using ShanMen.Jobs;
using UnityEngine;

namespace ShanMen.Buildings
{
    public enum BuildingType { SpiritField, Workshop, MeditationMat, SpiritGatherer }

    public sealed class BuildingRuntime : MonoBehaviour
    {
        public BuildingType type;
        public GridPosition gridPosition;

        GameClock _clock;
        JobBoard _jobs;
        ResourceLedger _ledger;
        QiField _qiField;
        bool _jobOutstanding;
        int _tickCounter;

        public void Bind(GameClock clock, JobBoard jobs, ResourceLedger ledger, QiField qiField)
        {
            _clock = clock; _jobs = jobs; _ledger = ledger; _qiField = qiField;
            _clock.Tick += OnTick;
        }

        void OnDestroy()
        {
            if (_clock != null) _clock.Tick -= OnTick;
            var source = GetComponent<QiSource>();
            if (source != null && _qiField != null) _qiField.Unregister(source);
        }

        public void InitializeSpecials()
        {
            if (type != BuildingType.SpiritGatherer) return;
            var source = gameObject.AddComponent<QiSource>();
            source.strength = 35f;
            source.radius = 6f;
            _qiField.Register(source);
        }

        void OnTick(long tick)
        {
            _tickCounter++;
            if (_jobOutstanding) return;

            switch (type)
            {
                case BuildingType.SpiritField:
                    if (_tickCounter % 4 == 0) Queue(JobType.Farm, 2.5f, () => { _ledger.Add(ResourceType.Food, 4); _ledger.Add(ResourceType.Herb, 1); });
                    break;
                case BuildingType.Workshop:
                    if (_tickCounter % 6 == 0 && _ledger.Get(ResourceType.Herb) >= 2)
                        Queue(JobType.Craft, 3.5f, () => { _ledger.Add(ResourceType.Herb, -2); _ledger.Add(ResourceType.SpiritStone, 1); });
                    break;
                case BuildingType.MeditationMat:
                    if (_tickCounter % 3 == 0) Queue(JobType.Cultivate, 4f, null);
                    break;
            }
        }

        void Queue(JobType type, float work, Action completed)
        {
            _jobOutstanding = true;
            _jobs.Add(type, transform.position, work, () =>
            {
                _jobOutstanding = false;
                completed?.Invoke();
            });
        }
    }
}
