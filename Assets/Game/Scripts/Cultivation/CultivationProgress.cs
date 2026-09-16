using UnityEngine;

namespace ShanMen.Cultivation
{
    public enum Realm { Mortal, QiGathering, Foundation, GoldenCore }

    public sealed class CultivationProgress : MonoBehaviour
    {
        public Realm realm = Realm.Mortal;
        [Min(0f)] public float cultivation;
        [Min(1f)] public float breakthroughRequirement = 100f;

        public float Progress01 => Mathf.Clamp01(cultivation / breakthroughRequirement);
        public bool CanBreakthrough => cultivation >= breakthroughRequirement;

        public void AddCultivation(float amount) => cultivation = Mathf.Max(0f, cultivation + amount);

        public bool TryBreakthrough(float localQi)
        {
            if (!CanBreakthrough || localQi < RequiredQiFor(realm)) return false;
            cultivation -= breakthroughRequirement;
            realm = realm switch
            {
                Realm.Mortal => Realm.QiGathering,
                Realm.QiGathering => Realm.Foundation,
                Realm.Foundation => Realm.GoldenCore,
                _ => Realm.GoldenCore
            };
            breakthroughRequirement *= 2.25f;
            return true;
        }

        static float RequiredQiFor(Realm current) => current switch
        {
            Realm.Mortal => 5f,
            Realm.QiGathering => 15f,
            Realm.Foundation => 30f,
            _ => 999f
        };
    }
}
