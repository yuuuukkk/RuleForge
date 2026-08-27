using UnityEngine;

namespace RuleForge.Runtime
{
    public readonly struct DamageInfo
    {
        public DamageInfo(
            float amount,
            Vector3 hitPoint = default,
            Vector3 direction = default,
            GameObject source = null)
        {
            Amount = Mathf.Max(0f, amount);
            HitPoint = hitPoint;
            Direction = direction;
            Source = source;
        }

        public float Amount { get; }

        public Vector3 HitPoint { get; }

        public Vector3 Direction { get; }

        public GameObject Source { get; }
    }
}
