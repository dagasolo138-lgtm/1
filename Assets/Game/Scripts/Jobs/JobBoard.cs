using System;
using System.Collections.Generic;
using ShanMen.Economy;
using UnityEngine;

namespace ShanMen.Jobs
{
    public enum JobType { Haul, Build, Farm, Craft, Cultivate, Gather }
    public sealed class Job
    {
        public readonly int id; public readonly JobType type; public readonly Vector3 worldPosition; public readonly float workRequired; public readonly Action onCompleted; public bool Reserved { get; set; }
        public readonly ResourceContainer haulSource; public readonly ResourceContainer haulDestination; public readonly ResourceType haulResource; public readonly int haulAmount; public bool PickupCompleted { get; set; }
        public bool IsHaul => type == JobType.Haul;
        public Vector3 ReservationTarget => IsHaul && haulSource != null ? haulSource.transform.position : worldPosition;
        public Job(int id, JobType type, Vector3 worldPosition, float workRequired, Action onCompleted) { this.id=id; this.type=type; this.worldPosition=worldPosition; this.workRequired=Mathf.Max(0.1f,workRequired); this.onCompleted=onCompleted; }
        public Job(int id, ResourceContainer source, ResourceContainer destination, ResourceType resource, int amount) { this.id=id; type=JobType.Haul; haulSource=source; haulDestination=destination; haulResource=resource; haulAmount=Mathf.Max(1,amount); worldPosition=source!=null?source.transform.position:Vector3.zero; workRequired=0.1f; }
    }
    public sealed class JobBoard : MonoBehaviour
    {
        readonly List<Job> _jobs = new(); int _nextId=1; public int PendingCount=>_jobs.Count;
        public Job Add(JobType type, Vector3 position, float workRequired, Action onCompleted) { var job=new Job(_nextId++,type,position,workRequired,onCompleted); _jobs.Add(job); return job; }
        public Job AddHaul(ResourceContainer source, ResourceContainer destination, ResourceType resource, int amount)
        {
            if(source==null||destination==null||source==destination||amount<=0)return null;
            if(!source.ReservePickup(resource,amount))return null;
            if(!destination.ReserveDropoff(resource,amount)){source.ReleasePickup(resource,amount);return null;}
            var job=new Job(_nextId++,source,destination,resource,amount);_jobs.Add(job);return job;
        }
        public Job ReserveBest(Vector3 workerPosition, Func<JobType,float> preference)
        {
            Job best=null;float bestScore=float.NegativeInfinity;
            foreach(Job job in _jobs){if(job.Reserved)continue;float priority=preference(job.type);if(priority<=0f)continue;float distance=Vector3.Distance(workerPosition,job.ReservationTarget);if(job.IsHaul&&job.haulSource!=null&&job.haulDestination!=null)distance+=Vector3.Distance(job.haulSource.transform.position,job.haulDestination.transform.position)*0.2f;float score=priority*100f-distance;if(score<=bestScore)continue;bestScore=score;best=job;}
            if(best!=null)best.Reserved=true;return best;
        }
        public void MarkPickedUp(Job job){if(job!=null&&job.IsHaul)job.PickupCompleted=true;}
        public void Complete(Job job){if(job==null)return;if(!_jobs.Remove(job))return;ReleaseHaulReservations(job);job.onCompleted?.Invoke();}
        public void Cancel(Job job){if(job==null)return;if(_jobs.Remove(job))ReleaseHaulReservations(job);}
        public void CancelHaulsForContainer(ResourceContainer container){if(container==null)return;for(int i=_jobs.Count-1;i>=0;i--){Job job=_jobs[i];if(!job.IsHaul)continue;if(job.haulSource!=container&&job.haulDestination!=container)continue;_jobs.RemoveAt(i);ReleaseHaulReservations(job);}}
        public void CancelReservation(Job job){if(job!=null)job.Reserved=false;}
        void ReleaseHaulReservations(Job job){if(job==null||!job.IsHaul)return;if(!job.PickupCompleted&&job.haulSource!=null)job.haulSource.ReleasePickup(job.haulResource,job.haulAmount);if(job.haulDestination!=null)job.haulDestination.ReleaseDropoff(job.haulResource,job.haulAmount);}
    }
}
