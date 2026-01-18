#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    // Draws Data.SkillDefinition.MoveStyle as vertical buttons.
    public class MoveStyleVerticalDrawer : OdinValueDrawer<Data.SkillDefinition.MoveStyle>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            // Label on the left like normal fields
            if (label != null)
                EditorGUILayout.PrefixLabel(label);

            // Current value
            var value = this.ValueEntry.SmartValue;

            // Vertical stack of "toggle buttons"
            EditorGUILayout.BeginVertical();
            bool isMelee = value == Data.SkillDefinition.MoveStyle.Melee;
            bool isRanged = value == Data.SkillDefinition.MoveStyle.Ranged;

            if (GUILayout.Toggle(isMelee, "Melee", "Button"))
                value = Data.SkillDefinition.MoveStyle.Melee;
            if (GUILayout.Toggle(isRanged, "Ranged", "Button"))
                value = Data.SkillDefinition.MoveStyle.Ranged;

            EditorGUILayout.EndVertical();

            // Write back if changed
            if (!EqualityComparer<Data.SkillDefinition.MoveStyle>.Default.Equals(value, this.ValueEntry.SmartValue))
                this.ValueEntry.SmartValue = value;
        }
    }
}
#endif
