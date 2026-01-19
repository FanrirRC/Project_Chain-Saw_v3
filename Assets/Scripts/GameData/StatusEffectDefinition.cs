using Sirenix.OdinInspector;
using UnityEngine;

namespace Data
{
    public enum StatusCategory { Ailment, Buff, Debuff }
    public enum StatType { None, ATK, DEF, MaxHP, MaxSP }
    public enum PotencyMode { FlatNumber, Percent }

    [CreateAssetMenu(menuName = "RPG/Status Effect")]
    public class StatusEffectDefinition : ScriptableObject
    {
        [BoxGroup("Effect_Details", centerLabel: true)]
        [HorizontalGroup("Effect_Details/Split", Width = 80), HideLabel, PreviewField(80)]
        public Sprite icon;

        [VerticalGroup("Effect_Details/Split/Details")]
        [LabelWidth(90)]
        [HorizontalGroup("Effect_Details/Split/Details/Split")]
        public string displayName;

        [VerticalGroup("Effect_Details/Split/Details")]
        [HorizontalGroup("Effect_Details/Split/Details/Split", Width = 0.4f)]
        public int durationTurns = 3;

        [VerticalGroup("Effect_Details/Split/Details")]
        [TextArea] public string description;

        [BoxGroup("Type", centerLabel: true)]
        [LabelText("Effect Type")]
        [EnumToggleButtons] public StatusCategory category = StatusCategory.Ailment;

        [HorizontalGroup("ModsAndDot")]

        [VerticalGroup("ModsAndDot/Left")]
        [BoxGroup("ModsAndDot/Left/Stat Modifier", centerLabel: true)]
        [HorizontalGroup("ModsAndDot/Left/Stat Modifier/Split")]
        [VerticalGroup("ModsAndDot/Left/Stat Modifier/Split/Col")]
        [LabelText("Stat")] public StatType stat = StatType.None;

        [VerticalGroup("ModsAndDot/Left/Stat Modifier/Split/Col")]
        [LabelText("Power")] public int power = 0;

        [VerticalGroup("ModsAndDot/Left/Stat Modifier/Split/Col")]
        [EnumToggleButtons, LabelText("Potency Type")]
        public PotencyMode potencyMode = PotencyMode.FlatNumber;

        [VerticalGroup("ModsAndDot/Right")]
        [BoxGroup("ModsAndDot/Right/DOT", centerLabel: true)]
        [LabelText("DOT Active?")] public bool dotActive = false;

        public enum DOTSourceOwner { Inflictor, Target }

        [ShowIf("dotActive")]
        [HorizontalGroup("ModsAndDot/Right/DOT/Row1")]
        [VerticalGroup("ModsAndDot/Right/DOT/Row1/Left")]
        [LabelText("Source Owner")]
        public DOTSourceOwner dotSource = DOTSourceOwner.Inflictor;

        [ShowIf("dotActive")]
        [VerticalGroup("ModsAndDot/Right/DOT/Row1/Left")]
        [LabelText("Base Stat")]
        public StatType dotBaseStat = StatType.ATK;

        [ShowIf("dotActive")]
        [HorizontalGroup("ModsAndDot/Right/DOT/Row2")]
        [VerticalGroup("ModsAndDot/Right/DOT/Row2/Right")]
        [LabelText("DOT Power")] public int dotPower = 5;

        [ShowIf("dotActive")]
        [VerticalGroup("ModsAndDot/Right/DOT/Row2/Right")]
        [EnumToggleButtons, LabelText("Potency Type")]
        public PotencyMode dotPotencyMode = PotencyMode.Percent;
    }
}
