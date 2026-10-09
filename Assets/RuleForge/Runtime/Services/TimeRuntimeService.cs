using UnityEngine;

namespace RuleForge.Runtime.Services
{
    [DisallowMultipleComponent]
    public sealed class TimeRuntimeService : MonoBehaviour
    {
        private float startedAt;
        private float countdownDeadline;
        private float initialCountdownSeconds;
        private bool countdownActive;

        public float ElapsedTime => Time.time - startedAt;
        public float CurrentTimeScale => Time.timeScale;
        public float InitialCountdownSeconds => initialCountdownSeconds;
        public float RemainingSeconds => countdownActive
            ? Mathf.Max(0f, countdownDeadline - Time.time)
            : 0f;

        private void OnEnable()
        {
            RestartClock();
        }

        public void RestartClock()
        {
            startedAt = Time.time;
            countdownActive = false;
            countdownDeadline = 0f;
            initialCountdownSeconds = 0f;
        }

        public void BeginCountdown(float seconds)
        {
            countdownActive = seconds > 0f;
            initialCountdownSeconds = Mathf.Max(0f, seconds);
            countdownDeadline = Time.time + Mathf.Max(0f, seconds);
        }

        public bool AddCountdownSeconds(float seconds)
        {
            if (!countdownActive || seconds <= 0f || RemainingSeconds <= 0f)
            {
                return false;
            }

            float remaining = RemainingSeconds;
            float extended = Mathf.Min(300f, remaining + seconds);
            if (extended <= remaining)
            {
                return false;
            }

            countdownDeadline = Time.time + extended;
            return true;
        }

        public float RemoveCountdownSeconds(float seconds)
        {
            if (!countdownActive || seconds <= 0f)
            {
                return 0f;
            }

            float remaining = RemainingSeconds;
            float removed = Mathf.Min(remaining, seconds);
            countdownDeadline = Time.time + remaining - removed;
            return removed;
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
