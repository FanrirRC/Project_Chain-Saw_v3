using Sirenix.OdinInspector;
using UnityEngine;

namespace Data
{
    public enum StatusCategory { Ailment, Buff, Debuff }

    [CreateAssetMenu(menuName = "RPG/Status Effect")]
    public class StatusEffectDefinition : ScriptableObject
    {
        [Header("Info")]
        public string displayName;
        public Sprite icon;

        [EnumToggleButtons]
        public StatusCategory category = StatusCategory.Ailment;
        public int durationTurns = 3;

        [Header("Stat modifiers")]
        public int atkFlat;
        [Range(-100, 500)] public int atkPercent;
        public int defFlat;
        [Range(-100, 500)] public int defPercent;

        [Header("Damage multipliers (%)")]
        [Range(-100, 500)] public int damageDealtPercent;
        [Range(-100, 500)] public int damageTakenPercent;

        [Header("Damage over time")]
        public bool isDOT = false;
        [Range(0, 100)] public int dotPercentOfMaxHP = 0;

        [Header("Animation Triggers (optional)")]
        [Tooltip("Trigger fired on the TARGET when this status is applied.")]
        public AnimatorTriggerRef onApplyTrigger;

        [Tooltip("Trigger fired on the TARGET when this status expires.")]
        public AnimatorTriggerRef onExpireTrigger;

        [Tooltip("Trigger fired on the TARGET on each tick (DOT/HoT).")]
        public AnimatorTriggerRef onTickTrigger;
    }
}
