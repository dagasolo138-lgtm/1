using System.Collections.Generic;
using ShanMen.Core;
using ShanMen.Cultivation;
using ShanMen.Economy;
using ShanMen.Grid;
using ShanMen.Jobs;
using UnityEngine;

namespace ShanMen.Buildings
{
    public sealed class BuildController : MonoBehaviour
    {
        public Camera worldCamera;
        public GridMap grid;
        public ResourceLedger ledger;
        public GameClock clock;
        public JobBoard jobs;
        public QiField qiField;
        public BuildingType? Selected { get; private set; }

        readonly Dictionary<BuildingType, ResourceCost[]> _costs = new()
        {
            [BuildingType.SpiritField] = new[] { new ResourceCost(ResourceType.Wood, 8) },
            [BuildingType.Workshop] = new[] { new ResourceCost(ResourceType.Wood, 15), new ResourceCost(ResourceType.Stone, 12) },
            [BuildingType.MeditationMat] = new[] { new ResourceCost(ResourceType.Wood, 10), new ResourceCost(ResourceType.Stone, 3) },
            [BuildingType.SpiritGatherer] = new[] { new ResourceCost(ResourceType.Stone, 18), new ResourceCost(ResourceType.SpiritStone, 4) },
        };

        public void Select(BuildingType type) => Selected = type;
        public void Cancel() => Selected = null;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) Select(BuildingType.SpiritField);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Select(BuildingType.Workshop);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Select(BuildingType.MeditationMat);
            if (Input.GetKeyDown(KeyCode.Alpha4)) Select(BuildingType.SpiritGatherer);
            if (Input.GetMouseButtonDown(1)) Cancel();
            if (Selected.HasValue && Input.GetMouseButtonDown(0)) TryPlaceAtMouse();
        }

        void TryPlaceAtMouse()
        {
            if (worldCamera == null || grid == null) return;
            Ray ray = worldCamera.ScreenPointToRay(Input.mousePosition);
            Plane ground = new(Vector3.up, Vector3.zero);
            if (!ground.Raycast(ray, out float enter)) return;
            Vector3 point = ray.GetPoint(enter);
            GridPosition cell = grid.WorldToGrid(point);
            if (!grid.InBounds(cell) || grid.IsOccupied(cell)) return;

            BuildingType type = Selected.Value;
            ResourceCost[] costs = _costs[type];
            if (!ledger.Spend(costs)) return;
            if (!grid.TryOccupy(cell)) return;

            GameObject go = GameObject.CreatePrimitive(type == BuildingType.MeditationMat ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            go.name = type.ToString();
            go.transform.position = grid.GridToWorld(cell) + Vector3.up * (type == BuildingType.MeditationMat ? 0.08f : 0.5f);
            go.transform.localScale = type == BuildingType.MeditationMat ? new Vector3(0.75f, 0.08f, 0.75f) : new Vector3(0.9f, 1f, 0.9f);

            var runtime = go.AddComponent<BuildingRuntime>();
            runtime.type = type;
            runtime.gridPosition = cell;
            runtime.Bind(clock, jobs, ledger, qiField);
            runtime.InitializeSpecials();
        }
    }
}
