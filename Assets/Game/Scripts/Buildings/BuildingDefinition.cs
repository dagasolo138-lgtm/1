using System;
using ShanMen.Economy;
using ShanMen.Jobs;
using UnityEngine;

namespace ShanMen.Buildings
{
    public enum BuildingBehavior { Producer, Processor, Cultivation, QiSource, Storage, Rest }

    [CreateAssetMenu(menuName = "ShanMen/Building Definition", fileName = "BuildingDefinition")]
    public sealed class BuildingDefinition : ScriptableObject
    {
        public string id = "building";
        public string displayName = "建筑";
        [TextArea] public string description;
        public BuildingBehavior behavior;

        [Header("Visual")]
        public PrimitiveType visualPrimitive = PrimitiveType.Cube;
        public Vector3 visualScale = new Vector3(0.9f, 1f, 0.9f);
        public float verticalOffset = 0.5f;
        public Color visualColor = Color.white;

        [Header("Construction")]
        public ResourceCost[] buildCosts = Array.Empty<ResourceCost>();
        [Min(0.1f)] public float buildWork = 3f;

        [Header("Work")]
        public JobType workType = JobType.Farm;
        [Min(1)] public int jobIntervalTicks = 4;
        [Min(0.1f)] public float jobWork = 2.5f;

        [Header("Recipe")]
        public ResourceCost[] inputs = Array.Empty<ResourceCost>();
        public ResourceCost[] outputs = Array.Empty<ResourceCost>();
        [Min(1)] public int inputBufferCycles = 3;
        [Min(1)] public int inputCapacity = 40;
        [Min(1)] public int outputCapacity = 40;

        [Header("Storage")]
        [Min(1)] public int storageCapacity = 300;

        [Header("Qi")]
        [Min(0f)] public float qiStrength = 35f;
        [Min(0.5f)] public float qiRadius = 6f;
    }
}
