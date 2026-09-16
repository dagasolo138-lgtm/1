using ShanMen.Cultivation;
using ShanMen.Economy;
using ShanMen.Jobs;
using UnityEngine;

namespace ShanMen.Characters
{
    public sealed class CultivatorAgent : MonoBehaviour
    {
        public string displayName = "弟子";
        [Min(0.2f)] public float moveSpeed = 3f;
        [Min(0.1f)] public float workSpeed = 1f;
        [Range(0, 5)] public int haulingPriority = 4;
        [Range(0, 5)] public int buildingPriority = 4;
        [Range(0, 5)] public int farmingPriority = 3;
        [Range(0, 5)] public int craftingPriority = 2;
        [Range(0, 5)] public int cultivationPriority = 1;

        JobBoard _board;
        Job _job;
        float _workDone;
        CultivationProgress _cultivation;
        ResourceType _carriedType;
        int _carriedAmount;

        public string CurrentJobName
        {
            get
            {
                if (_job == null) return "空闲";
                if (_job.IsHaul) return $"搬运 {_job.haulResource} x{_job.haulAmount}";
                return JobLabel(_job.type);
            }
        }

        public string CarryingText => _carriedAmount <= 0 ? string.Empty : $"携带 {_carriedType} x{_carriedAmount}";

        void Awake() => _cultivation = GetComponent<CultivationProgress>();

        void Start()
        {
            if (_board == null) _board = FindFirstObjectByType<JobBoard>();
            if (_cultivation == null) _cultivation = GetComponent<CultivationProgress>();
        }

        void OnDisable()
        {
            AbortCurrentJob();
        }

        public void Bind(JobBoard board) => _board = board;

        void Update()
        {
            if (_board == null) return;
            if (_job == null)
            {
                _job = _board.ReserveBest(transform.position, PriorityFor);
                _workDone = 0f;
                return;
            }

            if (_job.IsHaul)
            {
                UpdateHaul();
                return;
            }

            if (!MoveTo(_job.worldPosition)) return;
            _workDone += workSpeed * Time.deltaTime;
            if (_workDone < _job.workRequired) return;

            if (_job.type == JobType.Cultivate && _cultivation != null)
                _cultivation.AddCultivation(10f);

            _board.Complete(_job);
            ResetJob();
        }

        void UpdateHaul()
        {
            if (_job.haulSource == null || _job.haulDestination == null)
            {
                AbortCurrentJob();
                return;
            }

            if (!_job.PickupCompleted)
            {
                if (!MoveTo(_job.haulSource.transform.position)) return;
                if (!_job.haulSource.TryTakeReserved(_job.haulResource, _job.haulAmount))
                {
                    AbortCurrentJob();
                    return;
                }

                _carriedType = _job.haulResource;
                _carriedAmount = _job.haulAmount;
                _board.MarkPickedUp(_job);
                return;
            }

            if (!MoveTo(_job.haulDestination.transform.position)) return;
            if (!_job.haulDestination.TryStoreReserved(_job.haulResource, _carriedAmount))
            {
                AbortCurrentJob();
                return;
            }

            _carriedAmount = 0;
            _board.Complete(_job);
            ResetJob();
        }

        bool MoveTo(Vector3 target)
        {
            Vector3 flatTarget = new Vector3(target.x, transform.position.y, target.z);
            if (Vector3.Distance(transform.position, flatTarget) <= 0.25f) return true;
            transform.position = Vector3.MoveTowards(transform.position, flatTarget, moveSpeed * Time.deltaTime);
            return false;
        }

        void AbortCurrentJob()
        {
            if (_job == null || _board == null) return;

            if (_job.IsHaul && _carriedAmount > 0 && _job.haulSource != null)
                _job.haulSource.TryAdd(_carriedType, _carriedAmount);

            _carriedAmount = 0;
            _board.Cancel(_job);
            ResetJob();
        }

        void ResetJob()
        {
            _job = null;
            _workDone = 0f;
        }

        public int GetPriority(JobType type)
        {
            switch (type)
            {
                case JobType.Haul: return haulingPriority;
                case JobType.Build: return buildingPriority;
                case JobType.Farm: return farmingPriority;
                case JobType.Craft: return craftingPriority;
                case JobType.Cultivate: return cultivationPriority;
                default: return 0;
            }
        }

        public void SetPriority(JobType type, int value)
        {
            value = Mathf.Clamp(value, 0, 5);
            switch (type)
            {
                case JobType.Haul: haulingPriority = value; break;
                case JobType.Build: buildingPriority = value; break;
                case JobType.Farm: farmingPriority = value; break;
                case JobType.Craft: craftingPriority = value; break;
                case JobType.Cultivate: cultivationPriority = value; break;
            }
        }

        float PriorityFor(JobType type) => GetPriority(type);

        public static string JobLabel(JobType type)
        {
            switch (type)
            {
                case JobType.Haul: return "搬运";
                case JobType.Build: return "建造";
                case JobType.Farm: return "种植";
                case JobType.Craft: return "制造";
                case JobType.Cultivate: return "修炼";
                default: return type.ToString();
            }
        }
    }
}
