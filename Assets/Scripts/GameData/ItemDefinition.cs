using Sirenix.OdinInspector;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(menuName = "RPG/Item")]
    public class ItemDefinition : ScriptableObject
    {
        public enum EffectType { Heal, ApplyStatus }

        [BoxGroup("Item_Details", centerLabel: true)]
        [HorizontalGroup("Item_Details/Split", Width = 80), HideLabel, PreviewField(80)]
        public Sprite icon;
        [HorizontalGroup("Item_Details/Split")]
        [VerticalGroup("Item_Details/Split/Right")]
        [LabelWidth(85)]
        public string displayName;
        [VerticalGroup("Item_Details/Split/Right")]
        [TextArea] public string description;

        [BoxGroup("Item_Effect", centerLabel: true)]
        [EnumToggleButtons]
        public EffectType effectType = EffectType.Heal;
        [HorizontalGroup("Item_Effect/Split")]
        [VerticalGroup("Item_Effect/Split/Left")]
        [Header("Effect Potency")]
        public int power = 30;
        [HorizontalGroup("Item_Effect/Split")]
        [VerticalGroup("Item_Effect/Split/Left")]
        public bool isPercent = false;
        [HorizontalGroup("Item_Effect/Split/Right")]
        [Header("Statuses")]
        public StatusEffectDefinition statusToApply;
    }
}
