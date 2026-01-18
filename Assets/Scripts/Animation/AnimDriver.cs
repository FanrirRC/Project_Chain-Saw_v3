using System;
using System.Collections.Generic;
using UnityEngine;

public class AnimDriver : MonoBehaviour
{
    public enum AnimEvent
    {
        Idle,
        StartTurn,
        Melee,
        Attack,
        SkillCast,
        Hurt,
        Guard,
        Death,
        StatusApply,
        StatusExpire,
        StatusTick
    }

    [Serializable]
    public class AnimEventEntry
    {
        public AnimEvent eventType;
        public AnimatorTriggerRef trigger;
    }

    [Header("Animator")]
    public Animator animator;

    [Tooltip("Map gameplay events to animator triggers (dropdown).")]
    public List<AnimEventEntry> animationEvents = new();

    private readonly Dictionary<AnimEvent, AnimatorTriggerRef> _map = new();

    void Awake()
    {
        _map.Clear();
        foreach (var e in animationEvents)
        {
            if (e == null) continue;
            e.trigger.ValidateOn(animator);
            _map[e.eventType] = e.trigger;
        }
    }

    public void Fire(AnimEvent evt)
    {
        if (!animator) return;
        if (_map.TryGetValue(evt, out var trig) && !string.IsNullOrEmpty(trig.Name))
            animator.SetTrigger(trig.Hash);
    }

    public void PlayIdle() => Fire(AnimEvent.Idle);
    public void PlayAttack() => Fire(AnimEvent.Attack);
    public void PlayHurt() => Fire(AnimEvent.Hurt);
    public void PlayGuard() => Fire(AnimEvent.Guard);
    public void PlayDeath() => Fire(AnimEvent.Death);
}
