using UnityEngine;

namespace ShanMen.Cultivation
{
    public sealed class QiSource : MonoBehaviour
    {
        [Min(0f)] public float strength = 20f;
        [Min(0.5f)] public float radius = 5f;

        QiField _field;

        void OnEnable()
        {
            _field = FindFirstObjectByType<QiField>();
            if (_field != null) _field.Register(this);
        }

        void OnDisable()
        {
            if (_field != null) _field.Unregister(this);
        }
    }
}
