using System;
using System.Collections.Generic;
using RuleForge.DSL;
using UnityEngine;

namespace RuleForge.AI
{
    public enum GameplayProposalConfidence
    {
        Low,
        Medium,
        High
    }

    [Serializable]
    public sealed class GameplayProposal
    {
        [SerializeField] private string summary;
        [SerializeField] private string detectedIntent;
        [SerializeField] private string confidence;
        [SerializeField] private bool goalExplicit;
        [SerializeField] private bool triggerExplicit;
        [SerializeField] private bool rewardExplicit;
        [SerializeField] private bool riskExplicit;
        [SerializeField] private bool scalingExplicit;
        [SerializeField] private bool limitExplicit;
        [SerializeField] private bool hasConflict;
        [SerializeField] private bool withinVocabulary;
        [SerializeField] private string suggestedGoal;
        [SerializeField] private string[] suggestedRules = Array.Empty<string>();
        [SerializeField] private string requiredGoalType;
        [SerializeField] private float requiredGoalTarget;
        [SerializeField] private float requiredTimeLimitSeconds;
        [SerializeField] private float requiredTimeDamageScale;
        [SerializeField] private GameplayMechanicRequirement[] requiredMechanics =
            Array.Empty<GameplayMechanicRequirement>();
        [SerializeField] private string designReasoningSummary;
        [SerializeField] private string[] warnings = Array.Empty<string>();
        [SerializeField] private string clarificationQuestion;
        [SerializeField] private bool canGenerate;
        [SerializeField] private float penaltyRewardRatio = 1f;
        [SerializeField] private string[] actionSuggestions = Array.Empty<string>();

        public string Summary => summary ?? string.Empty;
        public string DetectedIntent => detectedIntent ?? string.Empty;
        public string Confidence => confidence ?? string.Empty;
        public bool GoalExplicit => goalExplicit;
        public bool TriggerExplicit => triggerExplicit;
        public bool RewardExplicit => rewardExplicit;
        public bool RiskExplicit => riskExplicit;
        public bool ScalingExplicit => scalingExplicit;
        public bool LimitExplicit => limitExplicit;
        public bool HasConflict => hasConflict;
        public bool WithinVocabulary => withinVocabulary;
        public string SuggestedGoal => suggestedGoal ?? string.Empty;
        public string[] SuggestedRules => suggestedRules ?? Array.Empty<string>();
        public string RequiredGoalType => requiredGoalType ?? string.Empty;
        public float RequiredGoalTarget => requiredGoalTarget;
        public float RequiredTimeLimitSeconds => requiredTimeLimitSeconds;
        public float RequiredTimeDamageScale => requiredTimeDamageScale;
        public GameplayMechanicRequirement[] RequiredMechanics =>
            requiredMechanics ?? Array.Empty<GameplayMechanicRequirement>();
        public string DesignReasoningSummary => designReasoningSummary ?? string.Empty;
        public string[] Warnings => warnings ?? Array.Empty<string>();
        public string ClarificationQuestion => clarificationQuestion ?? string.Empty;
        public bool CanGenerate => canGenerate;
        public float PenaltyRewardRatio => Mathf.Clamp(
            penaltyRewardRatio,
            0f,
            3f);
        public string[] ActionSuggestions => actionSuggestions ?? Array.Empty<string>();

        public GameplayProposalConfidence ConfidenceLevel
        {
            get
            {
                return Enum.TryParse(
                    Confidence,
                    true,
                    out GameplayProposalConfidence value)
                    ? value
                    : GameplayProposalConfidence.Low;
            }
        }
    }

    [Serializable]
    public sealed class GameplayMechanicRequirement
    {
        [SerializeField] private string trigger;
        [SerializeField] private string effectId;
        [SerializeField] private float value;
        [SerializeField] private bool exactValue;
        [SerializeField] private int maxStacks;
        [SerializeField] private bool exactMaxStacks;

        public string Trigger => trigger ?? string.Empty;
        public string EffectId => effectId ?? string.Empty;
        public float Value => value;
        public bool ExactValue => exactValue;
        public int MaxStacks => maxStacks;
        public bool ExactMaxStacks => exactMaxStacks;
    }

    public static class ChallengeIntentContract
    {
        public static bool TryValidate(
            GameplayProposal proposal,
            ChallengeSpec challenge,
            out string error)
        {
            error = string.Empty;
            if (proposal == null || challenge == null)
            {
                error = "A confirmed proposal and generated challenge are required.";
                return false;
            }

            if (proposal.SuggestedRules.Length > 0 &&
                proposal.RequiredMechanics.Length == 0)
            {
                error = "AI did not specify the core mechanics of its proposal.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(proposal.RequiredGoalType))
            {
                error = "AI proposal did not specify its core goal type.";
                return false;
            }

            if (!string.Equals(proposal.RequiredGoalType, challenge.Goal,
                    StringComparison.OrdinalIgnoreCase))
            {
                error = "Generated goal differs from the confirmed proposal.";
                return false;
            }

            bool endless = string.Equals(proposal.RequiredGoalType,
                ChallengeGoalType.TimeBankEndless.ToString(),
                StringComparison.OrdinalIgnoreCase);
            if ((endless
                    ? proposal.RequiredGoalTarget != 0f
                    : proposal.RequiredGoalTarget <= 0f) ||
                Mathf.Abs(proposal.RequiredGoalTarget -
                          challenge.GoalTarget) > 0.01f)
            {
                error = "Generated victory target differs from the confirmed proposal.";
                return false;
            }

            if (Mathf.Abs(proposal.RequiredTimeLimitSeconds -
                          challenge.TimeLimit) > 0.01f)
            {
                error = "Generated countdown differs from the confirmed proposal.";
                return false;
            }

            if (Mathf.Abs(proposal.RequiredTimeDamageScale -
                          challenge.TimeDamageScale) > 0.001f)
            {
                error = "Generated hit-to-time conversion differs from the confirmed proposal.";
                return false;
            }

            GameplayMechanicRequirement[] requirements =
                proposal.RequiredMechanics;
            GameplayRule[] rules = challenge.Rules;
            HashSet<RuleEffect> matchedEffects =
                new HashSet<RuleEffect>();
            for (int requirementIndex = 0;
                 requirementIndex < requirements.Length;
                 requirementIndex++)
            {
                GameplayMechanicRequirement required =
                    requirements[requirementIndex];
                if (required == null ||
                    string.IsNullOrWhiteSpace(required.Trigger) ||
                    string.IsNullOrWhiteSpace(required.EffectId))
                {
                    error = "AI proposal has an incomplete core mechanic.";
                    return false;
                }

                bool found = false;
                for (int ruleIndex = 0; ruleIndex < rules.Length && !found;
                     ruleIndex++)
                {
                    GameplayRule rule = rules[ruleIndex];
                    if (rule == null || rule.Trigger == null ||
                        !string.Equals(rule.Trigger.Type, required.Trigger,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    RuleEffect[] effects = rule.Effects;
                    for (int effectIndex = 0;
                         effectIndex < effects.Length;
                         effectIndex++)
                    {
                        RuleEffect effect = effects[effectIndex];
                        if (effect == null || matchedEffects.Contains(effect) ||
                            !string.Equals(effect.EffectId,
                                required.EffectId,
                                StringComparison.OrdinalIgnoreCase) ||
                            (required.ExactValue &&
                             Mathf.Abs(effect.Value - required.Value) > 0.001f) ||
                            (required.ExactMaxStacks &&
                             (effect.MaxStacks != required.MaxStacks ||
                              (required.MaxStacks > 1 &&
                               !string.Equals(effect.StackMode, "Stack",
                                   StringComparison.OrdinalIgnoreCase)))))
                        {
                            continue;
                        }

                        matchedEffects.Add(effect);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    error = "Generated challenge omitted or changed " +
                            required.Trigger + " → " + required.EffectId +
                            (required.ExactValue
                                ? " (" + required.Value + ")"
                                : string.Empty) +
                            (required.ExactMaxStacks
                                ? " (max " + required.MaxStacks + " stacks)"
                                : string.Empty) + ".";
                    return false;
                }
            }

            for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
            {
                GameplayRule rule = rules[ruleIndex];
                if (rule == null || rule.Trigger == null)
                {
                    continue;
                }

                RuleEffect[] effects = rule.Effects;
                for (int effectIndex = 0;
                     effectIndex < effects.Length;
                     effectIndex++)
                {
                    RuleEffect effect = effects[effectIndex];
                    if (effect == null)
                    {
                        continue;
                    }

                    if (!matchedEffects.Contains(effect))
                    {
                        error = "Generated challenge added an unconfirmed " +
                                "effect: " + effect.EffectId + ".";
                        return false;
                    }
                }
            }

            return true;
        }
    }

    [Serializable]
    public sealed class GameplayModificationProposal
    {
        [SerializeField] private string summary;
        [SerializeField] private string detectedIntent;
        [SerializeField] private string confidence;
        [SerializeField] private string[] parameterFocus = Array.Empty<string>();
        [SerializeField] private string[] proposedChanges = Array.Empty<string>();
        [SerializeField] private string designReasoningSummary;
        [SerializeField] private string[] warnings = Array.Empty<string>();
        [SerializeField] private string clarificationQuestion;
        [SerializeField] private bool canModify;
        [SerializeField] private string patchInstruction;

        public string Summary => summary ?? string.Empty;
        public string DetectedIntent => detectedIntent ?? string.Empty;
        public string Confidence => confidence ?? string.Empty;
        public string[] ParameterFocus => parameterFocus ?? Array.Empty<string>();
        public string[] ProposedChanges => proposedChanges ?? Array.Empty<string>();
        public string DesignReasoningSummary => designReasoningSummary ?? string.Empty;
        public string[] Warnings => warnings ?? Array.Empty<string>();
        public string ClarificationQuestion => clarificationQuestion ?? string.Empty;
        public bool CanModify => canModify;
        public string PatchInstruction => patchInstruction ?? string.Empty;
    }

    [Serializable]
    public sealed class GameplayRepairResult
    {
        [SerializeField] private string summary;
        [SerializeField] private string[] changes = Array.Empty<string>();
        [SerializeField] private string designReasoningSummary;
        [SerializeField] private ChallengeSpec repairedChallenge;

        public string Summary => summary ?? string.Empty;
        public string[] Changes => changes ?? Array.Empty<string>();
        public string DesignReasoningSummary => designReasoningSummary ?? string.Empty;
        public ChallengeSpec RepairedChallenge => repairedChallenge;
    }

    [Serializable]
    public sealed class GameplayImprovementSuggestion
    {
        [SerializeField] private string title;
        [SerializeField] private string intent;
        [SerializeField] private string reasoning;

        public string Title => title ?? string.Empty;
        public string Intent => intent ?? string.Empty;
        public string Reasoning => reasoning ?? string.Empty;
    }

    [Serializable]
    public sealed class GameplayImprovementSet
    {
        [SerializeField] private string summary;
        [SerializeField] private GameplayImprovementSuggestion[] suggestions =
            Array.Empty<GameplayImprovementSuggestion>();

        public string Summary => summary ?? string.Empty;
        public GameplayImprovementSuggestion[] Suggestions =>
            suggestions ?? Array.Empty<GameplayImprovementSuggestion>();
    }
}
