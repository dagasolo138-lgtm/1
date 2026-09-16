using System;
using UnityEngine;

namespace ShanMen.Core
{
    public sealed class GameClock : MonoBehaviour
    {
        [Min(0.05f)] public float secondsPerTick = 0.5f;
        [Range(0f, 8f)] public float speed = 1f;
        public bool paused;

        public long TickIndex { get; private set; }
        public event Action<long> Tick;

        float _accumulator;

        void Update()
        {
            if (paused || speed <= 0f) return;
            _accumulator += Time.unscaledDeltaTime * speed;
            while (_accumulator >= secondsPerTick)
            {
                _accumulator -= secondsPerTick;
                TickIndex++;
                Tick?.Invoke(TickIndex);
            }
        }
    }
}
