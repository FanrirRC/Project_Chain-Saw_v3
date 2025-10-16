#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AnimatorTriggerRef))]
public class AnimatorTriggerRefDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var nameProp = property.FindPropertyRelative("triggerName");
        string current = nameProp.stringValue;

        Animator animator = null;
        if (property.serializedObject.targetObject is Component comp)
        {
            animator = comp.GetComponent<Animator>();
            if (!animator)
            {
                var so = new SerializedObject(comp);
                var animProp = so.FindProperty("animator");
                if (animProp != null && animProp.objectReferenceValue)
                    animator = animProp.objectReferenceValue as Animator;
            }
        }

        string[] options = System.Array.Empty<string>();
        if (animator)
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (var p in animator.parameters)
                if (p.type == AnimatorControllerParameterType.Trigger)
                    list.Add(p.name);
            options = list.ToArray();
        }

        EditorGUI.BeginProperty(position, label, property);

        Rect fieldRect = EditorGUI.PrefixLabel(position, label);

        if (options.Length == 0)
        {
            EditorGUI.BeginChangeCheck();
            string typed = EditorGUI.TextField(fieldRect, current);
            if (EditorGUI.EndChangeCheck())
                nameProp.stringValue = typed;
        }
        else
        {
            int index = Mathf.Max(0, System.Array.IndexOf(options, current));
            EditorGUI.BeginChangeCheck();
            index = EditorGUI.Popup(fieldRect, index, options);
            if (EditorGUI.EndChangeCheck())
                nameProp.stringValue = options[index];
        }

        EditorGUI.EndProperty();
    }
}
#endif
