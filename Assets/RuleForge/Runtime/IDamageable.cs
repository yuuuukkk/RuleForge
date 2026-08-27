namespace RuleForge.Runtime
{
    public interface IDamageable
    {
        float CurrentHealth { get; }

        float MaxHealth { get; }

        bool IsAlive { get; }

        void TakeDamage(DamageInfo damageInfo);
    }
}
