namespace RuleForge.Runtime.Stats
{
    public interface IRuntimeStatProvider
    {
        bool TryGetStat(RuntimeStatId statId, out RuntimeStat runtimeStat);

        void ClearAllModifiers();
    }
}
