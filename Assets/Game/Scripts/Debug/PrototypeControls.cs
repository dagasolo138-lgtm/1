using ShanMen.Characters;
using ShanMen.Core;
using ShanMen.Cultivation;
using ShanMen.Economy;
using ShanMen.Jobs;
using UnityEngine;

namespace ShanMen.Debugging
{
    public sealed class PrototypeControls : MonoBehaviour
    {
        public GameClock clock;
        public ResourceLedger ledger;
        public JobBoard jobs;
        public QiField qiField;
        public CultivatorAgent[] agents;

        void OnEnable()
        {
            if (clock != null) clock.Tick += OnTick;
        }

        void OnDisable()
        {
            if (clock != null) clock.Tick -= OnTick;
        }

        void Update()
        {
            if (!Input.GetKeyDown(KeyCode.B)) return;
            foreach (var agent in agents)
            {
                if (agent == null) continue;
                var progress = agent.GetComponent<CultivationProgress>();
                if (progress == null || !progress.CanBreakthrough) continue;
                float qi = qiField.Sample(agent.transform.position);
                bool success = progress.TryBreakthrough(qi);
                UnityEngine.Debug.Log($"[ShanMen] {agent.displayName} 突破 {(success ? "成功" : "失败")}，当地灵气 {qi:0.0}");
                break;
            }
        }

        void OnTick(long tick)
        {
            if (tick % 10 != 0) return;
            UnityEngine.Debug.Log(
                $"[ShanMen] Tick {tick} | 木 {ledger.Get(ResourceType.Wood)} 石 {ledger.Get(ResourceType.Stone)} " +
                $"食 {ledger.Get(ResourceType.Food)} 药 {ledger.Get(ResourceType.Herb)} 灵石 {ledger.Get(ResourceType.SpiritStone)} | Jobs {jobs.PendingCount}");
        }
    }
}
