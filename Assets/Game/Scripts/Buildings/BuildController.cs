using System;
using ShanMen.Economy;
using ShanMen.Grid;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ShanMen.Buildings
{
    public sealed class BuildController : MonoBehaviour
    {
        public Camera worldCamera;
        public GridMap grid;
        public ResourceLedger ledger;
        public BuildingDefinition[] definitions = Array.Empty<BuildingDefinition>();

        public int SelectedIndex { get; private set; } = -1;
        public BuildingDefinition Selected => SelectedIndex >= 0 && SelectedIndex < definitions.Length ? definitions[SelectedIndex] : null;

        public void SelectIndex(int index)
        {
            SelectedIndex = index >= 0 && index < definitions.Length ? index : -1;
        }

        public void Cancel() => SelectedIndex = -1;

        void Start()
        {
            if (worldCamera == null) worldCamera = Camera.main;
            if (grid == null) grid = FindFirstObjectByType<GridMap>();
            if (ledger == null) ledger = FindFirstObjectByType<ResourceLedger>();
        }

        void Update()
        {
            for (int i = 0; i < definitions.Length && i < 9; i++)
            {
                KeyCode key = (KeyCode)((int)KeyCode.Alpha1 + i);
                if (Input.GetKeyDown(key)) SelectIndex(i);
            }

            if (Input.GetMouseButtonDown(1)) Cancel();
            if (Selected != null && Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
                TryPlaceAtMouse();
            }
        }

        void TryPlaceAtMouse()
        {
            if (worldCamera == null || grid == null || Selected == null) return;
            Ray ray = worldCamera.ScreenPointToRay(Input.mousePosition);
            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (!ground.Raycast(ray, out float enter)) return;
            GridPosition cell = grid.WorldToGrid(ray.GetPoint(enter));
            TryPlace(Selected, cell);
        }

        public bool TryPlace(BuildingDefinition definition, GridPosition cell)
        {
            if (definition == null || grid == null || !grid.InBounds(cell) || grid.IsOccupied(cell)) return false;
            if (ledger != null && !ledger.CanSupply(definition.buildCosts))
            {
                Debug.Log($"[ShanMen] 建造 {definition.displayName} 失败：可搬运资源不足。");
                return false;
            }

            if (!grid.TryOccupy(cell)) return false;
            GameObject siteGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            siteGo.name = $"工地_{definition.displayName}";
            siteGo.transform.position = grid.GridToWorld(cell) + Vector3.up * 0.18f;
            siteGo.transform.localScale = new Vector3(0.82f, 0.3f, 0.82f);
            SetColor(siteGo, new Color(0.78f, 0.68f, 0.35f));

            ConstructionSite site = siteGo.AddComponent<ConstructionSite>();
            site.Configure(definition, cell);
            return true;
        }

        public void CompleteConstruction(ConstructionSite site)
        {
            if (site == null || site.Definition == null) return;
            BuildingDefinition definition = site.Definition;
            GridPosition cell = site.GridPosition;
            CreateCompletedBuilding(definition, cell, null);
            Destroy(site.gameObject);
        }

        public GameObject CreateCompletedBuilding(BuildingDefinition definition, GridPosition cell, ResourceAmount[] startingResources)
        {
            if (definition == null || grid == null) return null;

            GameObject go = GameObject.CreatePrimitive(definition.visualPrimitive);
            go.name = definition.displayName;
            go.transform.position = grid.GridToWorld(cell) + Vector3.up * definition.verticalOffset;
            go.transform.localScale = definition.visualScale;
            SetColor(go, definition.visualColor);

            BuildingRuntime runtime = go.AddComponent<BuildingRuntime>();
            runtime.Configure(definition, cell);

            if (definition.behavior == BuildingBehavior.Storage)
            {
                var storageGo = new GameObject("Storage");
                storageGo.transform.SetParent(go.transform, false);
                ResourceContainer container = storageGo.AddComponent<ResourceContainer>();
                container.Configure(ResourceContainerRole.Stockpile, definition.storageCapacity, Array.Empty<ResourceType>(), Array.Empty<ResourceTarget>());
                container.SetStarting(startingResources ?? Array.Empty<ResourceAmount>());
            }

            return go;
        }

        static void SetColor(GameObject go, Color color)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = color;
        }
    }
}
