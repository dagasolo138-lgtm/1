using ShanMen.Buildings;
using ShanMen.Economy;
using ShanMen.FiveElements;
using ShanMen.Grid;
using ShanMen.Jobs;
using UnityEngine;

namespace ShanMen.World
{
    public sealed class NaturalResourceNode:MonoBehaviour
    {
        [SerializeField] GridPosition gridPosition;public ResourceType resourceType=ResourceType.Wood;[Min(1)]public int yieldAmount=20;[Min(0.1f)]public float gatherWork=4f;public FiveElement element=FiveElement.Neutral;[Min(0f)]public float elementStrength=15f;[Min(0.5f)]public float elementRadius=4f;GridMap _grid;JobBoard _jobs;BuildController _build;Job _gatherJob;bool _finished;Color _baseColor;Renderer _renderer;public bool Designated=>_gatherJob!=null;
        public void Configure(GridPosition cell,ResourceType type,int amount,float work,FiveElement newElement,float strength,float radius){gridPosition=cell;resourceType=type;yieldAmount=Mathf.Max(1,amount);gatherWork=Mathf.Max(0.1f,work);element=newElement;elementStrength=Mathf.Max(0f,strength);elementRadius=Mathf.Max(0.5f,radius);}
        void Start(){_grid=FindFirstObjectByType<GridMap>();_jobs=FindFirstObjectByType<JobBoard>();_build=FindFirstObjectByType<BuildController>();_renderer=GetComponent<Renderer>();if(_renderer!=null)_baseColor=_renderer.material.color;if(_grid!=null)_grid.TryOccupy(gridPosition);if(element!=FiveElement.Neutral&&elementStrength>0f){FiveElementSource source=GetComponent<FiveElementSource>();if(source==null)source=gameObject.AddComponent<FiveElementSource>();source.Configure(element,elementStrength,elementRadius);}}
        void OnDestroy(){if(_finished)return;if(_gatherJob!=null&&_jobs!=null)_jobs.Cancel(_gatherJob);if(_grid!=null)_grid.Release(gridPosition);}
        public void DesignateGather(){if(_finished||_gatherJob!=null||_jobs==null)return;_gatherJob=_jobs.Add(JobType.Gather,transform.position,gatherWork,CompleteGather);if(_renderer!=null)_renderer.material.color=Color.Lerp(_baseColor,Color.white,0.35f);}
        void CompleteGather(){if(_finished)return;_gatherJob=null;_finished=true;if(_grid!=null)_grid.Release(gridPosition);if(_build!=null)_build.CreateResourceDrop(transform.position,new[]{new ResourceAmount(resourceType,yieldAmount)},$"采集物_{resourceType}");Destroy(gameObject);}
    }
}
