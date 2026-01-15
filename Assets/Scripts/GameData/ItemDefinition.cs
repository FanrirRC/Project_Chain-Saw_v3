using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace Data
{
    [CreateAssetMenu(menuName = "RPG/Item")]
    public class ItemDefinition : ScriptableObject
    {
        public enum EffectType { Heal, ApplyStatus }
        public enum PotencyMode { FlatNumber, Percent } // shown as "Potency Type"
        public enum StatusOp { Inflict, Remove }

        [System.Serializable]
        public class StatusEntry
        {
            [HorizontalGroup("Row")]
            [HideLabel] public StatusEffectDefinition status; // hide field label

            [HorizontalGroup("Row")]
            [LabelText("Op")] public StatusOp op = StatusOp.Inflict;
        }

        [BoxGroup("Item_Details", centerLabel: true)]
        [HorizontalGroup("Item_Details/Split", Width = 80), HideLabel, PreviewField(80)]
        public Sprite icon;

        [VerticalGroup("Item_Details/Split/Details")]
        [LabelWidth(90)]
        [HorizontalGroup("Item_Details/Split/Details/Split", Width = 0.6f)]
        public string displayName;

        [VerticalGroup("Item_Details/Split/Details")]
        [TextArea] public string description;

        [BoxGroup("Effect", centerLabel: true)]
        [EnumToggleButtons] public EffectType effectType = EffectType.Heal;

        [HorizontalGroup("Effect/Split")]
        [VerticalGroup("Effect/Split/Left")]
        [LabelText("Power")] public int power = 20;

        [HorizontalGroup("Effect/Split")]
        [VerticalGroup("Effect/Split/Left")]
        [EnumToggleButtons, LabelText("Potency Type")]
        public PotencyMode potencyMode = PotencyMode.FlatNumber;

        [HorizontalGroup("Effect/Split")]
        [VerticalGroup("Effect/Split/Right")]
        [Header("Status Effects")]
        [HideLabel]
        [TableList(AlwaysExpanded = true, NumberOfItemsPerPage = 5)]
        public List<StatusEntry> statuses = new List<StatusEntry>();
    }
}
