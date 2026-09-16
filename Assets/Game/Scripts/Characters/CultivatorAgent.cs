using ShanMen.Cultivation;
using ShanMen.Economy;
using ShanMen.Jobs;
using UnityEngine;

namespace ShanMen.Characters
{
    public sealed class CultivatorAgent : MonoBehaviour
    {
        enum PersonalTask { None, Eat, Rest }

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
        CultivatorNeeds _needs;
        ResourceLedger _ledger;
        ResourceType _carriedType;
        int _carriedAmount;

        PersonalTask _personalTask;
        ResourceContainer _foodSource;
        RestSpot _restSpot;

        public string CurrentJobName
        {
            get
            {
                if (_personalTask == PersonalTask.Eat) return "吃饭";
                if (_personalTask == PersonalTask.Rest) return "休息";
                if (_job == null) return "空闲";
                if (_job.IsHaul) return $"搬运 {_job.haulResource} x{_job.haulAmount}";
                return JobLabel(_job.type);
            }
        }

        public string CarryingText => _carriedAmount <= 0 ? string.Empty : $"携带 {_carriedType} x{_carriedAmount}";
        public string NeedText => _needs == null ? string.Empty : $"饱{_needs.satiety:0} 体{_needs.energy:0}";

        void Awake()
        {
            _cultivation = GetComponent<CultivationProgress>();
            _needs = GetComponent<CultivatorNeeds>();
            if (_needs == null) _needs = gameObject.AddComponent<CultivatorNeeds>();
        }

        void Start()
        {
            if (_board == null) _board = FindFirstObjectByType<JobBoard>();
            if (_cultivation == null) _cultivation = GetComponent<CultivationProgress>();
            if (_needs == null) _needs = GetComponent<CultivatorNeeds>();
            _ledger = FindFirstObjectByType<ResourceLedger>();
        }

        void OnDisable()
        {
            AbortCurrentJob();
            AbortPersonalTask();
        }

        public void Bind(JobBoard board) => _board = board;

        void Update()
        {
            if (_board == null) return;

            if (_personalTask != PersonalTask.None)
            {
                UpdatePersonalTask();
                return;
            }

            if (_job == null)
            {
                if (TryStartNeedTask()) return;
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
            float efficiency = _needs != null ? _needs.WorkEfficiency : 1f;
            _workDone += workSpeed * efficiency * Time.deltaTime;
            if (_workDone < _job.workRequired) return;

            if (_job.type == JobType.Cultivate && _cultivation != null)
                _cultivation.AddCultivation(10f);

            _board.Complete(_job);
            ResetJob();
        }

        bool TryStartNeedTask()
        {
            if (_needs == null) return false;

            if (_needs.WantsFood)
            {
                ResourceContainer food = FindBestFoodSource();
                if (food != null && food.ReservePickup(ResourceType.Food, 1))
                {
                    _foodSource = food;
                    _personalTask = PersonalTask.Eat;
                    return true;
                }
            }

            if (_needs.WantsRest)
            {
                RestSpot spot = FindBestRestSpot();
                if (spot != null && spot.TryReserve(this))
                {
                    _restSpot = spot;
                    _personalTask = PersonalTask.Rest;
                    return true;
                }
            }

            return false;
        }

        void UpdatePersonalTask()
        {
            if (_personalTask == PersonalTask.Eat)
            {
                if (_foodSource == null)
                {
                    AbortPersonalTask();
                    return;
                }

                if (!MoveTo(_foodSource.transform.position)) return;
                if (_foodSource.TryTakeReserved(ResourceType.Food, 1)) _needs?.EatOneMeal();
                else _foodSource.ReleasePickup(ResourceType.Food, 1);
                _foodSource = null;
                _personalTask = PersonalTask.None;
                return;
            }

            if (_personalTask == PersonalTask.Rest)
            {
                if (_restSpot == null)
                {
                    AbortPersonalTask();
                    return;
                }

                if (!MoveTo(_restSpot.transform.position)) return;
                _needs?.Rest(Time.deltaTime);
                if (_needs == null || !_needs.RestSatisfied) return;
                _restSpot.Release(this);
                _restSpot = null;
                _personalTask = PersonalTask.None;
            }
        }

        ResourceContainer FindBestFoodSource()
        {
            if (_ledger == null) _ledger = FindFirstObjectByType<ResourceLedger>();
            if (_ledger == null) return null;

            ResourceContainer best = null;
            float bestScore = float.PositiveInfinity;
            for (int i = 0; i < _ledger.Containers.Count; i++)
            {
                ResourceContainer candidate = _ledger.Containers[i];
                if (candidate == null) continue;
                if (candidate.role != ResourceContainerRole.Stockpile && candidate.role != ResourceContainerRole.Output) continue;
                if (candidate.AvailableForPickup(ResourceType.Food) <= 0) continue;

                float roleBias = candidate.role == ResourceContainerRole.Stockpile ? -5f : 0f;
                float score = Vector3.Distance(transform.position, candidate.transform.position) + roleBias;
                if (score >= bestScore) continue;
                bestScore = score;
                best = candidate;
            }
            return best;
        }

        RestSpot FindBestRestSpot()
        {
            RestSpot[] spots = FindObjectsByType<RestSpot>(FindObjectsSortMode.None);
            RestSpot best = null;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < spots.Length; i++)
            {
                RestSpot spot = spots[i];
                if (spot == null || !spot.IsAvailable) continue;
                float distance = Vector3.Distance(transform.position, spot.transform.position);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = spot;
            }
            return best;
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
            float efficiency = _needs != null ? _needs.MoveEfficiency : 1f;
            transform.position = Vector3.MoveTowards(transform.position, flatTarget, moveSpeed * efficiency * Time.deltaTime);
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

        void AbortPersonalTask()
        {
            if (_foodSource != null)
            {
                _foodSource.ReleasePickup(ResourceType.Food, 1);
                _foodSource = null;
            }

            if (_restSpot != null)
            {
                _restSpot.Release(this);
                _restSpot = null;
            }

            _personalTask = PersonalTask.None;
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
