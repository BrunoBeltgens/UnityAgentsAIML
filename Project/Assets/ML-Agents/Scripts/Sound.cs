using UnityEngine;

namespace ML_Agents.Scripts
{
    public struct Sound
    {
        public Vector3 Origin;
        public float Radius;

        public Sound(Vector3 origin, float radius)
        {
            Origin = origin;
            Radius = radius;
        }
    }
}
