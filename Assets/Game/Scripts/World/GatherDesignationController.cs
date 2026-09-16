using ShanMen.Buildings;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ShanMen.World
{
    public sealed class GatherDesignationController:MonoBehaviour
    {
        public Camera worldCamera;public BuildController buildController;
        void Start(){if(worldCamera==null)worldCamera=Camera.main;if(buildController==null)buildController=FindFirstObjectByType<BuildController>();}
        void Update(){if(!Input.GetMouseButtonDown(0))return;if(buildController!=null&&buildController.Selected!=null)return;if(EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject())return;if(worldCamera==null)return;Ray ray=worldCamera.ScreenPointToRay(Input.mousePosition);if(!Physics.Raycast(ray,out RaycastHit hit,500f))return;NaturalResourceNode node=hit.collider.GetComponentInParent<NaturalResourceNode>();if(node!=null)node.DesignateGather();}
    }
}
