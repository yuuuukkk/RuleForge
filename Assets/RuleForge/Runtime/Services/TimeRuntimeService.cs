using UnityEngine;

namespace RuleForge.Runtime.Services
{
    [DisallowMultipleComponent]
    public sealed class TimeRuntimeService : MonoBehaviour
    {
        private float startedAt;

        public float ElapsedTime => Time.time - startedAt;
        public float CurrentTimeScale => Time.timeScale;

        private void OnEnable()
        {
            RestartClock();
        }

        public void RestartClock()
        {
            startedAt = Time.time;
        }

        public void SetTimeScale(float timeScale)
        {
            Time.timeScale = Mathf.Max(0f, timeScale);
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                Time.timeScale = 1f;
            }
        }
    }
}
