using ShanMen.Cultivation;
using ShanMen.Jobs;
using UnityEngine;

namespace ShanMen.Characters
{
    public sealed class CultivatorAgent : MonoBehaviour
    {
        public string displayName = "弟子";
        [Min(0.2f)] public float moveSpeed = 3f;
        [Min(0.1f)] public float workSpeed = 1f;
        [Range(0, 5)] public int farmingPriority = 3;
        [Range(0, 5)] public int craftingPriority = 2;
        [Range(0, 5)] public int cultivationPriority = 1;

        JobBoard _board;
        Job _job;
        float _workDone;
        CultivationProgress _cultivation;

        public string CurrentJobName => _job == null ? "空闲" : _job.type.ToString();

        void Awake() => _cultivation = GetComponent<CultivationProgress>();

        void Start()
        {
            if (_board == null) _board = FindFirstObjectByType<JobBoard>();
            if (_cultivation == null) _cultivation = GetComponent<CultivationProgress>();
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

            Vector3 target = _job.worldPosition;
            Vector3 flatTarget = new Vector3(target.x, transform.position.y, target.z);
            float distance = Vector3.Distance(transform.position, flatTarget);
            if (distance > 0.25f)
            {
                transform.position = Vector3.MoveTowards(transform.position, flatTarget, moveSpeed * Time.deltaTime);
                return;
            }

            _workDone += workSpeed * Time.deltaTime;
            if (_workDone < _job.workRequired) return;

            if (_job.type == JobType.Cultivate && _cultivation != null)
                _cultivation.AddCultivation(10f);

            _board.Complete(_job);
            _job = null;
            _workDone = 0f;
        }

        float PriorityFor(JobType type)
        {
            switch (type)
            {
                case JobType.Farm: return farmingPriority;
                case JobType.Craft: return craftingPriority;
                case JobType.Cultivate: return cultivationPriority;
                default: return 0f;
            }
        }
    }
}
