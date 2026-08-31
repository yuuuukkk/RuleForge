using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuleForge.Validation
{
    [Serializable]
    public sealed class ValidationResult
    {
        [SerializeField] private List<string> errors = new List<string>();
        [SerializeField] private List<string> warnings = new List<string>();

        public bool IsValid => errors.Count == 0;
        public IReadOnlyList<string> Errors => errors;
        public IReadOnlyList<string> Warnings => warnings;

        public void AddError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                errors.Add(message);
            }
        }

        public void AddWarning(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                warnings.Add(message);
            }
        }

        public string BuildSummary()
        {
            if (IsValid)
            {
                return warnings.Count == 0
                    ? "Challenge validation passed."
                    : $"Challenge validation passed with {warnings.Count} warning(s).";
            }

            return $"Challenge validation rejected with {errors.Count} error(s).";
        }
    }
}
