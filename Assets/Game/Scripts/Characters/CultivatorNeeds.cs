using ShanMen.Core;
using UnityEngine;

namespace ShanMen.Characters
{
    public sealed class CultivatorNeeds : MonoBehaviour
    {
        [Range(0f, 100f)] public float satiety = 100f;
        [Range(0f, 100f)] public float energy = 100f;
        [Min(0f)] public float hungerPerTick = 0.7f;
        [Min(0f)] public float fatiguePerTick = 0.45f;
        [Min(1f)] public float foodRestore = 55f;
        [Min(1f)] public float restRestorePerSecond = 24f;

        GameClock _clock;
        bool _subscribed;

        public bool WantsFood => satiety <= 35f;
        public bool WantsRest => energy <= 25f;
        public bool FoodSatisfied => satiety >= 75f;
        public bool RestSatisfied => energy >= 92f;

        public float WorkEfficiency
        {
            get
            {
                float need = Mathf.Min(satiety, energy) / 100f;
                return Mathf.Lerp(0.45f, 1f, need);
            }
        }

        public float MoveEfficiency
        {
            get
            {
                float need = Mathf.Min(satiety, energy) / 100f;
                return Mathf.Lerp(0.65f, 1f, need);
            }
        }

        void Start()
        {
            _clock = FindFirstObjectByType<GameClock>();
            Subscribe();
        }

        void OnDisable()
        {
            if (_subscribed && _clock != null) _clock.Tick -= OnTick;
            _subscribed = false;
        }

        void Subscribe()
        {
            if (_subscribed || _clock == null) return;
            _clock.Tick += OnTick;
            _subscribed = true;
        }

        void OnTick(long tick)
        {
            satiety = Mathf.Max(0f, satiety - hungerPerTick);
            energy = Mathf.Max(0f, energy - fatiguePerTick);
        }

        public void EatOneMeal()
        {
            satiety = Mathf.Min(100f, satiety + foodRestore);
        }

        public void Rest(float deltaTime)
        {
            energy = Mathf.Min(100f, energy + restRestorePerSecond * Mathf.Max(0f, deltaTime));
        }
    }
}
