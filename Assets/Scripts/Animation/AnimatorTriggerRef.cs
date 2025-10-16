using System;
using UnityEngine;

[Serializable]
public struct AnimatorTriggerRef
{
    [SerializeField] private string triggerName;
    [NonSerialized] private int _hash;

    public string Name => triggerName;
    public int Hash => _hash != 0 ? _hash : (_hash = Animator.StringToHash(triggerName));

    public void ValidateOn(Animator animator)
    {
#if UNITY_EDITOR
        if (!animator || string.IsNullOrEmpty(triggerName)) return;
        bool found = false;
        foreach (var p in animator.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == triggerName) { found = true; break; }
        if (!found) Debug.LogWarning($"Animator trigger '{triggerName}' not found on Animator '{animator.name}'.", animator);
#endif
    }
}
