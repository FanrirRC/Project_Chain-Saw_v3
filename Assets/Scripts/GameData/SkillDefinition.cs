using UnityEngine;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;

namespace Data
{
    [CreateAssetMenu(menuName = "RPG/Skill")]
    public partial class SkillDefinition : ScriptableObject
    {
        public enum EffectType { Damage, Heal, None }
        public enum PotencyMode { FlatNumber, Percent }

        public enum StatusOp { Inflict, Remove }

        [Serializable]
        public class StatusEntry
        {
            [TableColumnWidth(125)]
            [LabelText("Status")]
            public StatusEffectDefinition status;

            [LabelText("Utilization")]
            public StatusOp op = StatusOp.Inflict;
        }

        public enum TargetSelection { SelfOnly, Single, Multi }

        [Flags]
        public enum TargetFaction
        {
            None = 0,
            Allies = 1 << 0,
            Enemies = 1 << 1
        }

        public enum MoveStyle { Melee, Ranged }

        public enum AnimTrigger
        {
            Default,
            Attack,
            Hurt,
            Die,
            Shoot,
            Revive,
            Spellcast_Attack,
            Spellcast_Healing,
            Items,
            Block
        }

        [BoxGroup("Skill_Details", centerLabel: true)]
        [HorizontalGroup("Skill_Details/Split", Width = 80), HideLabel, PreviewField(80)]
        public Sprite icon;

        [VerticalGroup("Skill_Details/Split/Properties")]
        [LabelWidth(90)]
        [HorizontalGroup("Skill_Details/Split/Properties/Split")]
        public string displayName;

        [VerticalGroup("Skill_Details/Split/Properties")]
        [HorizontalGroup("Skill_Details/Split/Properties/Split", Width = 0.4f)]
        [ProgressBar(0, 9, 0.25f, 0.6f, 1, Segmented = true)]
        public int spCost = 5;

        [VerticalGroup("Skill_Details/Split/Properties")]
        [TextArea] public string description;

        [BoxGroup("Targeting", centerLabel: true)]
        [EnumToggleButtons, LabelText("Selection")]
        public TargetSelection targetSelection = TargetSelection.Single;

        [BoxGroup("Targeting")]
        [EnumToggleButtons, LabelText("Faction")]
        public TargetFaction targetFaction = TargetFaction.Enemies;

        [BoxGroup("Skill_Effect", centerLabel: true)]
        [EnumToggleButtons, LabelText("Effect Type")]
        public EffectType effectType = EffectType.Damage;

        [HorizontalGroup("Skill_Effect/Split")]
        [Header("Effect Potency")]
        [VerticalGroup("Skill_Effect/Split/Left")]
        [LabelText("Based On")]
        public StatType potencyStat = StatType.ATK;

        [HorizontalGroup("Skill_Effect/Split")]
        [VerticalGroup("Skill_Effect/Split/Left")]
        public int power = 10;

        [HorizontalGroup("Skill_Effect/Split")]
        [VerticalGroup("Skill_Effect/Split/Left")]
        [EnumToggleButtons, LabelText("Potency Mode")]
        public PotencyMode potencyMode = PotencyMode.FlatNumber;

        [HorizontalGroup("Skill_Effect/Split/Right")]
        [Header("Status Effects")]
        [TableList(AlwaysExpanded = true, NumberOfItemsPerPage = 5)]
        public List<StatusEntry> statuses = new List<StatusEntry>();

        [BoxGroup("Animation", centerLabel: true)]
        [HorizontalGroup("Animation/Split", Width = 0.5f)]
        [VerticalGroup("Animation/Split/Left")]
        [LabelText("Movement")]
        public MoveStyle moveStyle = MoveStyle.Melee;

        [VerticalGroup("Animation/Split/Right")]
        [LabelText("Trigger")]
        public AnimTrigger animTrigger = AnimTrigger.Default;

        public bool UsesPercent => potencyMode == PotencyMode.Percent;
        public bool IsDamage => effectType == EffectType.Damage;
        public bool IsHeal => effectType == EffectType.Heal;
        public bool HasEffect => effectType != EffectType.None || (statuses != null && statuses.Count > 0);
    }
}
