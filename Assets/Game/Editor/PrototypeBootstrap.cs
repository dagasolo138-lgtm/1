#if UNITY_EDITOR
using System.IO;
using ShanMen.Buildings;
using ShanMen.Characters;
using ShanMen.Core;
using ShanMen.Cultivation;
using ShanMen.Economy;
using ShanMen.Grid;
using ShanMen.Jobs;
using ShanMen.Debugging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShanMen.EditorTools
{
    public static class PrototypeBootstrap
    {
        [MenuItem("Tools/ShanMen/Create Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            Directory.CreateDirectory("Assets/Game/Scenes");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject("SimulationRoot");
            var clock = root.AddComponent<GameClock>();
            var grid = root.AddComponent<GridMap>();
            var ledger = root.AddComponent<ResourceLedger>();
            var jobs = root.AddComponent<JobBoard>();
            var qiField = root.AddComponent<QiField>();
            qiField.grid = grid;

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
            build.clock = clock;
            build.jobs = jobs;
            build.qiField = qiField;

            var naturalQiGo = new GameObject("NaturalSpiritVein");
            naturalQiGo.transform.position = new Vector3(17f, 0.2f, 15f);
            var naturalQi = naturalQiGo.AddComponent<QiSource>();
            naturalQi.strength = 45f;
            naturalQi.radius = 8f;
            qiField.Register(naturalQi);

            var agents = new CultivatorAgent[3];
            for (int i = 0; i < agents.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = $"Disciple_{i + 1}";
                go.transform.position = new Vector3(4f + i * 1.5f, 1f, 4f);
                go.AddComponent<CultivationProgress>();
                var agent = go.AddComponent<CultivatorAgent>();
                agent.displayName = i switch { 0 => "青禾", 1 => "陆离", _ => "阿石" };
                agent.farmingPriority = i == 0 ? 5 : 2;
                agent.craftingPriority = i == 1 ? 5 : 2;
                agent.cultivationPriority = i == 2 ? 5 : 2;
                agent.Bind(jobs);
                agents[i] = agent;
            }

            var controls = root.AddComponent<PrototypeControls>();
            controls.clock = clock;
            controls.ledger = ledger;
            controls.jobs = jobs;
            controls.qiField = qiField;
            controls.agents = agents;

            EditorSceneManager.SaveScene(scene, "Assets/Game/Scenes/Prototype.unity");
            Selection.activeGameObject = root;
            Debug.Log("[ShanMen] Prototype scene created: Assets/Game/Scenes/Prototype.unity");
        }
    }
}
#endif
