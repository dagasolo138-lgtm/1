using UnityEngine;

namespace ShanMen.FiveElements
{
    public sealed class FiveElementSource : MonoBehaviour
    {
        public FiveElement element = FiveElement.Neutral;
        [Min(0f)] public float strength = 15f;
        [Min(0.5f)] public float radius = 4f;
        FiveElementField _field;
        void OnEnable() { _field = FindFirstObjectByType<FiveElementField>(); if (_field != null) _field.Register(this); }
        void OnDisable() { if (_field != null) _field.Unregister(this); }
        public void Configure(FiveElement newElement, float newStrength, float newRadius)
        {
            if (_field != null) _field.Unregister(this);
            element = newElement; strength = Mathf.Max(0f, newStrength); radius = Mathf.Max(0.5f, newRadius);
            if (_field == null) _field = FindFirstObjectByType<FiveElementField>();
            if (_field != null) _field.Register(this);
        }
    }
}
