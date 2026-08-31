using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuleForge.Rules
{
    public enum GameplayEventType
    {
        GameStarted,
        EnemyKilled,
        EnemyHit,
        Headshot,
        PlayerHit,
        PlayerReload,
        WeaponFired,
        TimerInterval,
        KillStreakReached,
        HeadshotStreakReached,
        PlayerHPChanged
    }

    public static class GameplayEventCapabilities
    {
        private static readonly GameplayEventType[] SupportedEvents =
        {
            GameplayEventType.GameStarted,
            GameplayEventType.EnemyKilled,
            GameplayEventType.EnemyHit,
            GameplayEventType.Headshot,
            GameplayEventType.PlayerHit,
            GameplayEventType.PlayerReload,
            GameplayEventType.WeaponFired,
            GameplayEventType.PlayerHPChanged
        };

        public static IReadOnlyList<GameplayEventType> RuntimeEvents =>
            SupportedEvents;

        public static bool IsRuntimeSupported(GameplayEventType eventType)
        {
            for (int index = 0; index < SupportedEvents.Length; index++)
            {
                if (SupportedEvents[index] == eventType)
                {
                    return true;
                }
            }

            return false;
        }

        public static string[] GetRuntimeEventNames()
        {
            string[] names = new string[SupportedEvents.Length];
            for (int index = 0; index < SupportedEvents.Length; index++)
            {
                names[index] = SupportedEvents[index].ToString();
            }

            return names;
        }
    }

    public readonly struct GameplayEvent
    {
        public GameplayEvent(
            GameplayEventType type,
            GameObject source = null,
            GameObject instigator = null,
            string subjectType = null,
            float value = 0f)
        {
            Type = type;
            Source = source;
            Instigator = instigator;
            SubjectType = subjectType ?? string.Empty;
            Value = value;
        }

        public GameplayEventType Type { get; }
        public GameObject Source { get; }
        public GameObject Instigator { get; }
        public string SubjectType { get; }
        public float Value { get; }
    }

    public static class GameplayEventBus
    {
        public static event Action<GameplayEvent> EventPublished;

        public static void Publish(GameplayEvent gameplayEvent)
        {
            EventPublished?.Invoke(gameplayEvent);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            EventPublished = null;
        }
    }
}
