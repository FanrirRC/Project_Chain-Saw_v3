using UnityEngine;
using Sirenix.OdinInspector;

namespace Data
{
    [CreateAssetMenu(menuName = "RPG/Skill")]
    public class SkillDefinition : ScriptableObject
    {
        public enum EffectType { Damage, Heal, ApplyStatus }
        public string displayName;
        [TextArea] public string description;
        public int spCost = 5;
        public Sprite icon;

        [EnumToggleButtons]
        public EffectType effectType = EffectType.Damage;

        [Header("Targeting")]
        public bool TargetsAllies = false;
        public bool TargetsSelfOnly = false;
        public bool targetsAll = false;

        [Header("Power / Rules")]
        public int power = 10;
        public bool overrideWithPower = true;
        public bool isPercent = false;

        [Header("Status (if ApplyStatus)")]
        public StatusEffectDefinition statusToApply;

        [Header("Animation")]
        [Tooltip("If enabled, this skill will fire the selected trigger on the ACTOR when used.")]
        public bool overrideActorTrigger = false;

        [Tooltip("Animator trigger to fire on the actor when this skill is used (dropdown).")]
        public AnimatorTriggerRef skillTrigger;
    }
}
