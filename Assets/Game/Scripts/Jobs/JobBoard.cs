using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShanMen.Jobs
{
    public enum JobType { Farm, Craft, Cultivate }

    public sealed class Job
    {
        public readonly int id;
        public readonly JobType type;
        public readonly Vector3 worldPosition;
        public readonly float workRequired;
        public readonly Action onCompleted;
        public bool Reserved { get; set; }

        public Job(int id, JobType type, Vector3 worldPosition, float workRequired, Action onCompleted)
        {
            this.id = id;
            this.type = type;
            this.worldPosition = worldPosition;
            this.workRequired = Mathf.Max(0.1f, workRequired);
            this.onCompleted = onCompleted;
        }
    }

    public sealed class JobBoard : MonoBehaviour
    {
        readonly List<Job> _jobs = new();
        int _nextId = 1;

        public int PendingCount => _jobs.Count;

        public Job Add(JobType type, Vector3 position, float workRequired, Action onCompleted)
        {
            var job = new Job(_nextId++, type, position, workRequired, onCompleted);
            _jobs.Add(job);
            return job;
        }

        public Job ReserveBest(Vector3 workerPosition, Func<JobType, float> preference)
        {
            Job best = null;
            float bestScore = float.NegativeInfinity;
            foreach (var job in _jobs)
            {
                if (job.Reserved) continue;
                float distance = Vector3.Distance(workerPosition, job.worldPosition);
                float score = preference(job.type) * 10f - distance;
                if (score <= bestScore) continue;
                bestScore = score;
                best = job;
            }
            if (best != null) best.Reserved = true;
            return best;
        }

        public void Complete(Job job)
        {
            if (job == null) return;
            if (_jobs.Remove(job)) job.onCompleted?.Invoke();
        }

        public void CancelReservation(Job job)
        {
            if (job != null) job.Reserved = false;
        }
    }
}
