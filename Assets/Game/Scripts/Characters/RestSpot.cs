using UnityEngine;

namespace ShanMen.Characters
{
    public sealed class RestSpot : MonoBehaviour
    {
        CultivatorAgent _reservedBy;

        public bool IsAvailable => _reservedBy == null;

        public bool TryReserve(CultivatorAgent agent)
        {
            if (agent == null) return false;
            if (_reservedBy != null && _reservedBy != agent) return false;
            _reservedBy = agent;
            return true;
        }

        public void Release(CultivatorAgent agent)
        {
            if (_reservedBy == agent) _reservedBy = null;
        }
    }
}
