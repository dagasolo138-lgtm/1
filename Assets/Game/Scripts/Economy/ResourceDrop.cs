using UnityEngine;

namespace ShanMen.Economy
{
    public sealed class ResourceDrop : MonoBehaviour
    {
        ResourceContainer _container;
        float _emptySince = -1f;

        void Start()
        {
            _container = GetComponent<ResourceContainer>();
        }

        void Update()
        {
            if (_container == null) return;
            if (_container.TotalAmount > 0)
            {
                _emptySince = -1f;
                return;
            }

            if (_emptySince < 0f) _emptySince = Time.unscaledTime;
            if (Time.unscaledTime - _emptySince >= 1f) Destroy(gameObject);
        }
    }
}
