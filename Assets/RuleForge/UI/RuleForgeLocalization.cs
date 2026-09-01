using UnityEngine;
using System.Text;
using RuleForge.Validation;

namespace RuleForge.UI
{
    public enum RuleForgeLanguage
    {
        English,
        Chinese
    }

    public static class RuleForgeLocalization
    {
        public static RuleForgeLanguage Current { get; private set; } =
            RuleForgeLanguage.Chinese;

        public static string ToggleLabel => Current == RuleForgeLanguage.Chinese
            ? "English"
            : "中文";

        public static string T(string english, string chinese)
        {
            return Current == RuleForgeLanguage.Chinese ? chinese : english;
        }

        public static void Toggle()
        {
            Current = Current == RuleForgeLanguage.Chinese
                ? RuleForgeLanguage.English
                : RuleForgeLanguage.Chinese;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLanguage()
        {
            Current = RuleForgeLanguage.Chinese;
        }

        public static string DataValue(string value)
        {
            return DataValue(value, Current);
        }

        public static string DataValue(
            string value,
            RuleForgeLanguage language)
        {
            if (language != RuleForgeLanguage.Chinese ||
                string.IsNullOrWhiteSpace(value))
            {
                return value ?? string.Empty;
            }

            switch (value)
            {
                case "Survive": return "生存";
                case "KillCount": return "击杀数量";
                case "Score": return "分数";
                case "GameStarted": return "游戏开始";
                case "EnemyKilled": return "敌人死亡";
                case "EnemyHit": return "命中敌人";
                case "Headshot": return "爆头";
                case "PlayerHit": return "玩家受伤";
                case "PlayerReload": return "玩家换弹";
                case "WeaponFired": return "武器开火";
                case "PlayerAmmoChanged": return "玩家弹药变化";
                case "PlayerHPChanged": return "玩家生命变化";
                case "Always": return "始终";
                case "RandomChance": return "随机概率";
                case "EnemyType": return "敌人类型";
                case "Equals": return "等于";
                case "NotEquals": return "不等于";
                case "LessThan": return "小于";
                case "LessOrEqual": return "小于或等于";
                case "GreaterThan": return "大于";
                case "GreaterOrEqual": return "大于或等于";
                case "EventValue": return "事件数值";
                case "PlayerMissingHPPercent": return "已损失生命比例";
                case "Linear": return "线性变化";
                case "Reward": return "奖励";
                case "Penalty": return "风险";
                case "Neutral": return "中性";
                case "Balanced": return "平衡";
                case "RewardHeavy": return "奖励偏高";
                case "RiskHeavy": return "风险偏高";
                case "NoSignal": return "无有效数据";
                case "Unknown": return "未知";
                case "Relaxed": return "轻松";
                case "Moderate": return "适中";
                case "Hard": return "困难";
                case "Extreme": return "极限";
                case "Slow": return "缓慢";
                case "Medium": return "中等";
                case "Fast": return "快速";
                case "High Risk / High Reward": return "高风险高收益";
                case "Snowball": return "滚雪球";
                case "Glass Cannon": return "玻璃大炮";
                case "Random": return "随机";
                case "Survival": return "生存";
                case "Scaling": return "状态成长";
                case "Burst": return "短期爆发";
                case "Grunt": return "普通敌人";
                case "Runner": return "快速敌人";
                case "Tank": return "重型敌人";
                case "Assault Rifle": return "突击步枪";
                case "Shotgun": return "霰弹枪";
                case "Sniper": return "狙击步枪";
                case "None": return "无";
                default: return value;
            }
        }

        public static string EffectName(string effectId, string fallback)
        {
            return EffectName(effectId, fallback, Current);
        }

        public static string EffectName(
            string effectId,
            string fallback,
            RuleForgeLanguage language)
        {
            if (language != RuleForgeLanguage.Chinese)
            {
                return fallback ?? effectId ?? string.Empty;
            }

            switch (effectId)
            {
                case "PlayerDamage": return "玩家伤害";
                case "EnemyMoveSpeed": return "敌人移动速度";
                case "PlayerDamageFromMissingHealth": return "损失生命增伤";
                case "PlayerMoveSpeedFromMissingHealth": return "损失生命移速";
                case "SpawnRunner": return "生成快速敌人";
                case "SpawnGrunt": return "生成普通敌人";
                case "SpawnTank": return "生成重型敌人";
                case "GiveAmmo": return "补充弹药";
                default: return fallback ?? effectId ?? string.Empty;
            }
        }

        public static string ValidationMessage(string message)
        {
            if (Current != RuleForgeLanguage.Chinese ||
                string.IsNullOrWhiteSpace(message))
            {
                return message ?? string.Empty;
            }

            return message
                .Replace("Validation requires ", "验证需要 ")
                .Replace(" is required.", " 为必填项。")
                .Replace(" is null.", " 为空。")
                .Replace(" is unknown.", " 未知。")
                .Replace(" is duplicated.", " 重复。")
                .Replace(" must contain at least one rule.", " 必须至少包含一条规则。")
                .Replace(" must contain at least one effect.", " 必须至少包含一个效果。")
                .Replace(" must be a finite number.", " 必须是有限数值。")
                .Replace(" requires an enemy type.", " 需要指定敌人类型。")
                .Replace(" is not supported by the current runtime.", " 当前运行时不支持。")
                .Replace(" must be between ", " 必须位于范围 ")
                .Replace(" exceeds ", " 超过限制 ");
        }

        public static string ValidationSummary(ValidationResult validation)
        {
            if (validation == null)
            {
                return T("No validation result.", "没有验证结果。");
            }

            StringBuilder builder = new StringBuilder(
                validation.IsValid
                    ? T("Validation passed.", "验证通过。")
                    : T("Validation rejected.", "验证被拒绝。"));
            for (int index = 0; index < validation.Errors.Count; index++)
            {
                builder.AppendLine();
                builder.Append(ValidationMessage(validation.Errors[index]));
            }

            return builder.ToString();
        }
    }
}
