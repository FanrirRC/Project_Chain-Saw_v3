using UnityEngine;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;

namespace Data
{
    [CreateAssetMenu(menuName = "RPG/Skill")]
    public partial class SkillDefinition : ScriptableObject
    {
        // ====== 1) EFFECT / POTENCY ======
        public enum EffectType { Damage, Heal, None }
        public enum PotencyMode { FlatNumber, Percent }

        // ====== 2) STATUS OPS ======
        public enum StatusOp { Inflict, Remove }

        [Serializable]
        public class StatusEntry
        {
            [HorizontalGroup("Row", Width = 220)]
            [LabelWidth(90)]
            public StatusEffectDefinition status;

            [HorizontalGroup("Row")]
            [LabelText("Op")]
            public StatusOp op = StatusOp.Inflict;
        }

        // ====== 3) TARGETING ======
        public enum TargetSelection { SelfOnly, Single, Multi }

        [Flags]
        public enum TargetFaction
        {
            None = 0,
            Allies = 1 << 0,
            Enemies = 1 << 1
        }

        // ====== 4) ANIMATION ======
        public enum MoveStyle { Melee, Ranged }

        public enum AnimTrigger
        {
            Default,            // use character defaults / legacy calls
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

        // =======================
        //   INSPECTOR LAYOUT
        // =======================

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

        // ---- Targeting (new schema) ----
        [BoxGroup("Targeting", centerLabel: true)]
        [EnumToggleButtons, LabelText("Selection")]
        public TargetSelection targetSelection = TargetSelection.Single;

        [BoxGroup("Targeting")]
        [EnumToggleButtons, LabelText("Faction")]
        public TargetFaction targetFaction = TargetFaction.Enemies;

        // ---- Effect / Potency (updated schema) ----
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

        // ---- Status list (apply/remove multiple) ----
        [HorizontalGroup("Skill_Effect/Split/Right")]
        [Header("Status Effects")]
        [TableList(AlwaysExpanded = true, NumberOfItemsPerPage = 5)]
        public List<StatusEntry> statuses = new List<StatusEntry>();

        // ---- Animation override (shared trigger names) ----
        [BoxGroup("Animation", centerLabel: true)]
        // Two side-by-side columns under Animation:
        [HorizontalGroup("Animation/Split", Width = 0.5f)]
        [VerticalGroup("Animation/Split/Left")]
        [LabelText("Movement")]
        public MoveStyle moveStyle = MoveStyle.Melee;

        [VerticalGroup("Animation/Split/Right")]
        [LabelText("Trigger")]
        public AnimTrigger animTrigger = AnimTrigger.Default;

        // =======================
        //   HELPER QUERIES
        // =======================

        public bool UsesPercent => potencyMode == PotencyMode.Percent;
        public bool IsDamage => effectType == EffectType.Damage;
        public bool IsHeal => effectType == EffectType.Heal;
        public bool HasEffect => effectType != EffectType.None || (statuses != null && statuses.Count > 0);
    }
}
