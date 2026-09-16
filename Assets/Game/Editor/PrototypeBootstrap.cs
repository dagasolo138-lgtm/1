#if UNITY_EDITOR
using System;
using System.IO;
using ShanMen.Buildings;
using ShanMen.Characters;
using ShanMen.Core;
using ShanMen.Cultivation;
using ShanMen.Debugging;
using ShanMen.Economy;
using ShanMen.Grid;
using ShanMen.Jobs;
using ShanMen.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShanMen.EditorTools
{
    public static class PrototypeBootstrap
    {
        const string DataFolder = "Assets/Game/Data/Buildings";

        [MenuItem("Tools/ShanMen/Create Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            EnsureFolders();
            BuildingDefinition[] definitions = CreateBuildingDefinitions();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject("SimulationRoot");
            var clock = root.AddComponent<GameClock>();
            var grid = root.AddComponent<GridMap>();
            var ledger = root.AddComponent<ResourceLedger>();
            var jobs = root.AddComponent<JobBoard>();
            var qiField = root.AddComponent<QiField>();
            qiField.grid = grid;

            var logistics = root.AddComponent<LogisticsManager>();
            logistics.clock = clock;
            logistics.ledger = ledger;
            logistics.jobs = jobs;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(grid.width * 0.5f, 0f, grid.height * 0.5f);
            ground.transform.localScale = new Vector3(grid.width / 10f, 1f, grid.height / 10f);

            var camGo = new GameObject("Main Camera");
            var camera = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(12f, 20f, -11f);
            camGo.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
            camera.fieldOfView = 55f;

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            var build = root.AddComponent<BuildController>();
            build.worldCamera = camera;
            build.grid = grid;
            build.ledger = ledger;
            build.definitions = definitions;

            BuildingDefinition stockpile = FindDefinition(definitions, "stockpile");
            build.CreateCompletedBuilding(stockpile, new GridPosition(3, 3), new[]
            {
                new ResourceAmount(ResourceType.Wood, 120),
                new ResourceAmount(ResourceType.Stone, 80),
                new ResourceAmount(ResourceType.Food, 50),
                new ResourceAmount(ResourceType.Herb, 6),
                new ResourceAmount(ResourceType.SpiritStone, 10),
            });

            var naturalQiGo = new GameObject("NaturalSpiritVein");
            naturalQiGo.transform.position = new Vector3(17f, 0.2f, 15f);
            var naturalQi = naturalQiGo.AddComponent<QiSource>();
            naturalQi.strength = 45f;
            naturalQi.radius = 8f;

            var agents = new CultivatorAgent[3];
            for (int i = 0; i < agents.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = $"Disciple_{i + 1}";
                go.transform.position = new Vector3(5f + i * 1.5f, 1f, 5f);
                go.AddComponent<CultivationProgress>();
                var agent = go.AddComponent<CultivatorAgent>();
                agent.displayName = i switch { 0 => "青禾", 1 => "陆离", _ => "阿石" };
                agent.haulingPriority = i == 2 ? 5 : 3;
                agent.buildingPriority = 4;
                agent.farmingPriority = i == 0 ? 5 : 2;
                agent.craftingPriority = i == 1 ? 5 : 2;
                agent.cultivationPriority = i == 2 ? 4 : 1;
                agent.Bind(jobs);
                agents[i] = agent;
            }

            var controls = root.AddComponent<PrototypeControls>();
            controls.clock = clock;
            controls.ledger = ledger;
            controls.jobs = jobs;
            controls.qiField = qiField;
            controls.agents = agents;

            var priorityPanel = root.AddComponent<WorkPriorityPanel>();
            priorityPanel.agents = agents;
            priorityPanel.ledger = ledger;

            EditorSceneManager.SaveScene(scene, "Assets/Game/Scenes/Prototype.unity");
            Selection.activeGameObject = root;
            Debug.Log("[ShanMen] Prototype scene created. Play it, use 1-5 to select buildings, left click to place, right click to cancel.");
        }

        static BuildingDefinition[] CreateBuildingDefinitions()
        {
            var field = Upsert("SpiritField", d =>
            {
                d.id = "spirit_field"; d.displayName = "灵田"; d.description = "产出食物与灵草，产物需要弟子搬回仓库。";
                d.behavior = BuildingBehavior.Producer; d.visualPrimitive = PrimitiveType.Cube;
                d.visualScale = new Vector3(0.9f, 0.25f, 0.9f); d.verticalOffset = 0.125f; d.visualColor = new Color(0.35f, 0.62f, 0.28f);
                d.buildCosts = new[] { new ResourceCost(ResourceType.Wood, 8) }; d.buildWork = 2.5f;
                d.workType = JobType.Farm; d.jobIntervalTicks = 4; d.jobWork = 2.5f;
                d.inputs = Array.Empty<ResourceCost>(); d.outputs = new[] { new ResourceCost(ResourceType.Food, 4), new ResourceCost(ResourceType.Herb, 1) }; d.outputCapacity = 30;
            });

            var workshop = Upsert("Workshop", d =>
            {
                d.id = "workshop"; d.displayName = "炼制工坊"; d.description = "需要先把灵草搬入工坊，再加工成灵石。";
                d.behavior = BuildingBehavior.Processor; d.visualPrimitive = PrimitiveType.Cube;
                d.visualScale = new Vector3(0.9f, 1f, 0.9f); d.verticalOffset = 0.5f; d.visualColor = new Color(0.62f, 0.42f, 0.24f);
                d.buildCosts = new[] { new ResourceCost(ResourceType.Wood, 15), new ResourceCost(ResourceType.Stone, 12) }; d.buildWork = 4f;
                d.workType = JobType.Craft; d.jobIntervalTicks = 6; d.jobWork = 3.5f;
                d.inputs = new[] { new ResourceCost(ResourceType.Herb, 2) }; d.outputs = new[] { new ResourceCost(ResourceType.SpiritStone, 1) };
                d.inputBufferCycles = 3; d.inputCapacity = 20; d.outputCapacity = 12;
            });

            var mat = Upsert("MeditationMat", d =>
            {
                d.id = "meditation_mat"; d.displayName = "蒲团"; d.description = "弟子可在此修炼。";
                d.behavior = BuildingBehavior.Cultivation; d.visualPrimitive = PrimitiveType.Cylinder;
                d.visualScale = new Vector3(0.75f, 0.08f, 0.75f); d.verticalOffset = 0.08f; d.visualColor = new Color(0.42f, 0.48f, 0.72f);
                d.buildCosts = new[] { new ResourceCost(ResourceType.Wood, 10), new ResourceCost(ResourceType.Stone, 3) }; d.buildWork = 2f;
                d.workType = JobType.Cultivate; d.jobIntervalTicks = 3; d.jobWork = 4f;
                d.inputs = Array.Empty<ResourceCost>(); d.outputs = Array.Empty<ResourceCost>();
            });

            var gatherer = Upsert("SpiritGatherer", d =>
            {
                d.id = "spirit_gatherer"; d.displayName = "聚灵台"; d.description = "提高周围灵气浓度。";
                d.behavior = BuildingBehavior.QiSource; d.visualPrimitive = PrimitiveType.Cylinder;
                d.visualScale = new Vector3(0.8f, 0.7f, 0.8f); d.verticalOffset = 0.35f; d.visualColor = new Color(0.36f, 0.72f, 0.8f);
                d.buildCosts = new[] { new ResourceCost(ResourceType.Stone, 18), new ResourceCost(ResourceType.SpiritStone, 4) }; d.buildWork = 5f;
                d.inputs = Array.Empty<ResourceCost>(); d.outputs = Array.Empty<ResourceCost>(); d.qiStrength = 35f; d.qiRadius = 6f;
            });

            var stockpile = Upsert("Stockpile", d =>
            {
                d.id = "stockpile"; d.displayName = "仓库"; d.description = "储存资源。生产建筑的产物会被搬运到这里。";
                d.behavior = BuildingBehavior.Storage; d.visualPrimitive = PrimitiveType.Cube;
                d.visualScale = new Vector3(0.9f, 0.45f, 0.9f); d.verticalOffset = 0.225f; d.visualColor = new Color(0.52f, 0.4f, 0.26f);
                d.buildCosts = new[] { new ResourceCost(ResourceType.Wood, 12), new ResourceCost(ResourceType.Stone, 5) }; d.buildWork = 3f;
                d.storageCapacity = 300; d.inputs = Array.Empty<ResourceCost>(); d.outputs = Array.Empty<ResourceCost>();
            });

            AssetDatabase.SaveAssets();
            return new[] { field, workshop, mat, gatherer, stockpile };
        }

        static BuildingDefinition Upsert(string fileName, Action<BuildingDefinition> configure)
        {
            string path = $"{DataFolder}/{fileName}.asset";
            BuildingDefinition definition = AssetDatabase.LoadAssetAtPath<BuildingDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<BuildingDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }
            configure(definition);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        static BuildingDefinition FindDefinition(BuildingDefinition[] definitions, string id)
        {
            for (int i = 0; i < definitions.Length; i++)
                if (definitions[i] != null && definitions[i].id == id) return definitions[i];
            return null;
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/Game/Scenes");
            Directory.CreateDirectory(DataFolder);
            AssetDatabase.Refresh();
        }
    }
}
#endif
