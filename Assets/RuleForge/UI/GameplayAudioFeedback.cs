using RuleForge.Rules;
using RuleForge.Runtime.Goals;
using UnityEngine;

namespace RuleForge.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class GameplayAudioFeedback : MonoBehaviour
    {
        private const int SampleRate = 22050;

        [Header("Optional authored clips")]
        [SerializeField] private AudioClip shotClip;
        [SerializeField] private AudioClip reloadClip;
        [SerializeField] private AudioClip hitClip;
        [SerializeField] private AudioClip headshotClip;
        [SerializeField] private AudioClip enemyKilledClip;
        [SerializeField] private AudioClip playerDamageClip;
        [SerializeField] private AudioClip ruleTriggeredClip;
        [SerializeField] private AudioClip victoryClip;
        [SerializeField] private AudioClip defeatClip;
        [SerializeField, Range(0f, 1f)] private float masterVolume = 0.65f;

        private AudioSource audioSource;
        private RuleEngine ruleEngine;
        private ChallengeGoalController goalController;

        public bool UsesProceduralFallback { get; private set; }

        public void Configure(
            RuleEngine engine,
            ChallengeGoalController goals)
        {
            UnsubscribeControllers();
            ruleEngine = engine;
            goalController = goals;
            if (isActiveAndEnabled)
            {
                SubscribeControllers();
            }
        }

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.dopplerLevel = 0f;
            EnsureFallbackClips();
        }

        private void OnEnable()
        {
            ruleEngine = ruleEngine != null
                ? ruleEngine
                : FindObjectOfType<RuleEngine>();
            goalController = goalController != null
                ? goalController
                : FindObjectOfType<ChallengeGoalController>();
            GameplayEventBus.EventPublished -= HandleGameplayEvent;
            GameplayEventBus.EventPublished += HandleGameplayEvent;
            SubscribeControllers();
        }

        private void OnDisable()
        {
            GameplayEventBus.EventPublished -= HandleGameplayEvent;
            UnsubscribeControllers();
        }

        private void HandleGameplayEvent(GameplayEvent gameplayEvent)
        {
            switch (gameplayEvent.Type)
            {
                case GameplayEventType.WeaponFired:
                    Play(shotClip, 0.7f);
                    break;
                case GameplayEventType.PlayerReload:
                    Play(reloadClip, 0.55f);
                    break;
                case GameplayEventType.EnemyHit:
                    Play(hitClip, 0.35f);
                    break;
                case GameplayEventType.Headshot:
                    Play(headshotClip, 0.65f);
                    break;
                case GameplayEventType.EnemyKilled:
                    Play(enemyKilledClip, 0.6f);
                    break;
                case GameplayEventType.PlayerHit:
                    Play(playerDamageClip, 0.8f);
                    break;
            }
        }

        private void HandleRuleTriggered(RuleTriggerFeedback feedback)
        {
            Play(ruleTriggeredClip, 0.5f);
        }

        private void HandleGoalStateChanged(ChallengeGoalState state)
        {
            if (state == ChallengeGoalState.Victory)
            {
                Play(victoryClip, 0.9f);
            }
            else if (state == ChallengeGoalState.Defeat)
            {
                Play(defeatClip, 0.9f);
            }
        }

        private void SubscribeControllers()
        {
            if (ruleEngine != null)
            {
                ruleEngine.RuleTriggered -= HandleRuleTriggered;
                ruleEngine.RuleTriggered += HandleRuleTriggered;
            }

            if (goalController != null)
            {
                goalController.StateChanged -= HandleGoalStateChanged;
                goalController.StateChanged += HandleGoalStateChanged;
            }
        }

        private void UnsubscribeControllers()
        {
            if (ruleEngine != null)
            {
                ruleEngine.RuleTriggered -= HandleRuleTriggered;
            }

            if (goalController != null)
            {
                goalController.StateChanged -= HandleGoalStateChanged;
            }
        }

        private void Play(AudioClip clip, float relativeVolume)
        {
            if (audioSource != null && clip != null && masterVolume > 0f)
            {
                audioSource.PlayOneShot(
                    clip,
                    Mathf.Clamp01(masterVolume * relativeVolume));
            }
        }

        private void EnsureFallbackClips()
        {
            UsesProceduralFallback = shotClip == null || reloadClip == null ||
                                     hitClip == null || headshotClip == null ||
                                     enemyKilledClip == null ||
                                     playerDamageClip == null ||
                                     ruleTriggeredClip == null ||
                                     victoryClip == null || defeatClip == null;
            shotClip = shotClip != null
                ? shotClip
                : CreateTone("RF_Shot", 0.09f, 150f, 72f, 0.72f, 0.32f);
            reloadClip = reloadClip != null
                ? reloadClip
                : CreateTone("RF_Reload", 0.16f, 310f, 520f, 0.38f, 0.04f);
            hitClip = hitClip != null
                ? hitClip
                : CreateTone("RF_Hit", 0.06f, 820f, 430f, 0.5f, 0.12f);
            headshotClip = headshotClip != null
                ? headshotClip
                : CreateTone("RF_Headshot", 0.12f, 1050f, 1680f, 0.55f, 0.03f);
            enemyKilledClip = enemyKilledClip != null
                ? enemyKilledClip
                : CreateTone("RF_Kill", 0.16f, 640f, 1180f, 0.5f, 0.05f);
            playerDamageClip = playerDamageClip != null
                ? playerDamageClip
                : CreateTone("RF_Damage", 0.2f, 115f, 58f, 0.65f, 0.2f);
            ruleTriggeredClip = ruleTriggeredClip != null
                ? ruleTriggeredClip
                : CreateTone("RF_Rule", 0.18f, 460f, 760f, 0.42f, 0.02f);
            victoryClip = victoryClip != null
                ? victoryClip
                : CreateTone("RF_Victory", 0.45f, 420f, 920f, 0.55f, 0.01f);
            defeatClip = defeatClip != null
                ? defeatClip
                : CreateTone("RF_Defeat", 0.48f, 260f, 72f, 0.58f, 0.04f);
        }

        private static AudioClip CreateTone(
            string clipName,
            float duration,
            float startFrequency,
            float endFrequency,
            float amplitude,
            float noiseAmount)
        {
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            float[] samples = new float[sampleCount];
            float phase = 0f;
            uint noiseState = 0x9E3779B9u;
            for (int index = 0; index < sampleCount; index++)
            {
                float progress = sampleCount > 1
                    ? (float)index / (sampleCount - 1)
                    : 0f;
                float frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
                phase += 2f * Mathf.PI * frequency / SampleRate;
                float attack = Mathf.Clamp01(progress / 0.045f);
                float envelope = attack * Mathf.Pow(1f - progress, 1.8f);
                noiseState = noiseState * 1664525u + 1013904223u;
                float noise = ((noiseState >> 8) / 16777215f) * 2f - 1f;
                float tone = Mathf.Sin(phase) + 0.24f * Mathf.Sin(phase * 2.01f);
                samples[index] = Mathf.Clamp(
                    (tone * (1f - noiseAmount) + noise * noiseAmount) *
                    amplitude * envelope,
                    -1f,
                    1f);
            }

            AudioClip clip = AudioClip.Create(
                clipName,
                sampleCount,
                1,
                SampleRate,
                false);
            clip.hideFlags = HideFlags.DontSave;
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            DestroyGeneratedClip(shotClip);
            DestroyGeneratedClip(reloadClip);
            DestroyGeneratedClip(hitClip);
            DestroyGeneratedClip(headshotClip);
            DestroyGeneratedClip(enemyKilledClip);
            DestroyGeneratedClip(playerDamageClip);
            DestroyGeneratedClip(ruleTriggeredClip);
            DestroyGeneratedClip(victoryClip);
            DestroyGeneratedClip(defeatClip);
        }

        private static void DestroyGeneratedClip(AudioClip clip)
        {
            if (clip != null && (clip.hideFlags & HideFlags.DontSave) != 0)
            {
                Destroy(clip);
            }
        }
    }
}
