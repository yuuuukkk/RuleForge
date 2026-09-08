using System;
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
        [SerializeField] private string designReasoningSummary;
        [SerializeField] private string[] warnings = Array.Empty<string>();
        [SerializeField] private string clarificationQuestion;
        [SerializeField] private bool canGenerate;
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
        public string DesignReasoningSummary => designReasoningSummary ?? string.Empty;
        public string[] Warnings => warnings ?? Array.Empty<string>();
        public string ClarificationQuestion => clarificationQuestion ?? string.Empty;
        public bool CanGenerate => canGenerate;
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
