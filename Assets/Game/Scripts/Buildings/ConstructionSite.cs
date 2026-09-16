using System;
using ShanMen.Core;
using ShanMen.Economy;
using ShanMen.Grid;
using ShanMen.Jobs;
using UnityEngine;

namespace ShanMen.Buildings
{
    public sealed class ConstructionSite:MonoBehaviour
    {
        [SerializeField] BuildingDefinition definition;[SerializeField] GridPosition gridPosition;GameClock _clock;GridMap _grid;JobBoard _jobs;ResourceLedger _ledger;BuildController _buildController;ResourceContainer _materials;Job _buildJob;bool _jobOutstanding;bool _subscribed;bool _finished;
        public BuildingDefinition Definition=>definition;public GridPosition GridPosition=>gridPosition;public ResourceContainer Materials=>_materials;
        public void Configure(BuildingDefinition newDefinition,GridPosition cell){definition=newDefinition;gridPosition=cell;}
        void Start(){_clock=FindFirstObjectByType<GameClock>();_grid=FindFirstObjectByType<GridMap>();_jobs=FindFirstObjectByType<JobBoard>();_ledger=FindFirstObjectByType<ResourceLedger>();_buildController=FindFirstObjectByType<BuildController>();if(_grid!=null&&definition!=null)_grid.TryOccupyRect(gridPosition,definition.NormalizedFootprint,definition.behavior!=BuildingBehavior.Road);EnsureMaterialContainer();if(_clock!=null){_clock.Tick+=OnTick;_subscribed=true;}}
        void OnDestroy(){if(_subscribed&&_clock!=null)_clock.Tick-=OnTick;if(_finished)return;if(_buildJob!=null&&_jobs!=null)_jobs.Cancel(_buildJob);if(_materials!=null&&_jobs!=null)_jobs.CancelHaulsForContainer(_materials);ReleaseFootprint();}
        void EnsureMaterialContainer(){_materials=GetComponent<ResourceContainer>();if(_materials==null)_materials=gameObject.AddComponent<ResourceContainer>();int capacity=1;ResourceType[] accepted=definition==null||definition.buildCosts==null?Array.Empty<ResourceType>():new ResourceType[definition.buildCosts.Length];ResourceTarget[] targets=definition==null||definition.buildCosts==null?Array.Empty<ResourceTarget>():new ResourceTarget[definition.buildCosts.Length];if(definition!=null&&definition.buildCosts!=null){for(int i=0;i<definition.buildCosts.Length;i++){accepted[i]=definition.buildCosts[i].type;targets[i]=new ResourceTarget(definition.buildCosts[i].type,definition.buildCosts[i].amount);capacity+=definition.buildCosts[i].amount;}}_materials.Configure(ResourceContainerRole.Construction,capacity,accepted,targets);_materials.Bind(_ledger);}
        void OnTick(long tick){if(_jobOutstanding||definition==null||_materials==null||_jobs==null)return;if(!_materials.Has(definition.buildCosts))return;_jobOutstanding=true;_buildJob=_jobs.Add(JobType.Build,transform.position,definition.buildWork,()=>{_jobOutstanding=false;_buildJob=null;if(!_materials.TryConsume(definition.buildCosts))return;if(_buildController!=null)_buildController.CompleteConstruction(this);});}
        public void MarkCompleted(){_finished=true;}
        public void CancelConstruction(){if(_finished)return;if(_buildJob!=null&&_jobs!=null){_jobs.Cancel(_buildJob);_buildJob=null;}if(_materials!=null&&_jobs!=null)_jobs.CancelHaulsForContainer(_materials);ResourceAmount[] refund=_materials!=null?_materials.SnapshotContents():Array.Empty<ResourceAmount>();if(_buildController!=null&&refund.Length>0)_buildController.CreateRefundDrop(transform.position,refund);ReleaseFootprint();_finished=true;Destroy(gameObject);}
        void ReleaseFootprint(){if(_grid==null||definition==null)return;_grid.ReleaseRect(gridPosition,definition.NormalizedFootprint);}
    }
}
