using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Data;

public class CharacterScript : MonoBehaviour
{
    public enum UnitType { Player, Enemy }

    public const int SP_CAP = 9;

    [Header("Unit Details")]
    [EnumToggleButtons]
    [OnValueChanged(nameof(OnUnitTypeChanged))]
    public UnitType unitType = UnitType.Player;

    public string characterName;
    public GameObject characterModel;
    public Sprite portrait;

    [TitleGroup("Character Stats")]
    [ShowIf("@unitType == CharacterScript.UnitType.Player")]
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    [HideLabel]
    public CharacterBaseStats characterStats;

    [TitleGroup("Enemy Stats")]
    [ShowIf("@unitType == CharacterScript.UnitType.Enemy")]
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    [HideLabel]
    public EnemyBaseStats enemyStats;

    [Header("Runtime Resources")]
    public int maxHP;
    public int currentHP;
    public int maxSP;
    public int currentSP;

    [Serializable]
    public struct ActiveStatusEffect
    {
        public StatusEffectDefinition effect;
        public int remainingTurns;
        public CharacterScript inflictor;

        public bool skipDecrementThisTurn;
    }
    public List<ActiveStatusEffect> activeStatusEffects = new();

    public event Action<CharacterScript> OnHPChanged;
    public event Action<CharacterScript> OnSPChanged;
    public event Action<CharacterScript> OnDied;

    public bool IsEnemy => unitType == UnitType.Enemy;

    public Animator animator;

    public string idleState = "Idle";
    public string dieState = "Die";

    [Header("Animation Triggers (Dropdowns)")]
    public SkillDefinition.AnimTrigger attackTrigger = SkillDefinition.AnimTrigger.Attack;
    public SkillDefinition.AnimTrigger hurtTrigger = SkillDefinition.AnimTrigger.Hurt;
    public SkillDefinition.AnimTrigger deathTrigger = SkillDefinition.AnimTrigger.Die;

    [Header("Basic Attack Movement (Default)")]
    public SkillDefinition.MoveStyle basicAttackMove = SkillDefinition.MoveStyle.Melee;

    [Header("Animation Timings")]
    [Range(0f, 1f)] public float attackWindup = 0.25f;
    [Range(0f, 1f)] public float attackRecover = 0.25f;

    private static UI.DamagePopup _cachedPopup;
    private static UI.DamagePopup GetPopup()
    {
        if (_cachedPopup != null) return _cachedPopup;
        _cachedPopup = UnityEngine.Object.FindFirstObjectByType<UI.DamagePopup>();
        return _cachedPopup;
    }

    public void PlayIdle()
    {
        if (animator && !string.IsNullOrEmpty(idleState))
            animator.CrossFade(idleState, 0.05f, 0, 0f);
    }

    public void PlayAttack()
    {
        if (!animator) return;
        TryResetTrigger(attackTrigger == SkillDefinition.AnimTrigger.Attack ? hurtTrigger : attackTrigger);
        FireAnim(attackTrigger);
    }

    public void PlayHurt()
    {
        if (!animator) return;
        TryResetTrigger(hurtTrigger == SkillDefinition.AnimTrigger.Hurt ? attackTrigger : hurtTrigger);
        FireAnim(hurtTrigger);
    }

    public void PlayDie()
    {
        if (!animator) return;
        TryResetTrigger(attackTrigger);
        TryResetTrigger(hurtTrigger);
        FireAnim(deathTrigger);
        if (!string.IsNullOrEmpty(dieState))
            animator.CrossFade(dieState, 0.05f, 0, 0f);
    }

    private void Awake() => InitializeFromData();

    public Sprite GetPortrait() => portrait;

    private void OnUnitTypeChanged()
    {
#if UNITY_EDITOR
        Undo.RecordObject(this, "Change Unit Type");
#endif
        if (unitType == UnitType.Player) enemyStats = null;
        else characterStats = null;
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    public void InitializeFromData()
    {
        int baseMaxHP = 100, baseMaxSP = 50;
        if (unitType == UnitType.Player && characterStats)
        {
            baseMaxHP = characterStats.maxHP;
            baseMaxSP = characterStats.maxSP;
            if (string.IsNullOrEmpty(characterName)) characterName = characterStats.characterName;
        }
        else if (unitType == UnitType.Enemy && enemyStats)
        {
            baseMaxHP = enemyStats.maxHP;
            baseMaxSP = enemyStats.maxSP;
            if (string.IsNullOrEmpty(characterName)) characterName = enemyStats.enemyName;
        }

        maxHP = baseMaxHP;
        maxSP = Mathf.Clamp(baseMaxSP, 0, SP_CAP);

        currentHP = maxHP;
        currentSP = 0;
        activeStatusEffects.Clear();
    }

    public int GetBaseATK()
    {
        if (unitType == UnitType.Player && characterStats) return characterStats.atk;
        if (unitType == UnitType.Enemy && enemyStats) return enemyStats.atk;
        return 0;
    }
    public int GetBaseDEF()
    {
        if (unitType == UnitType.Player && characterStats) return characterStats.def;
        if (unitType == UnitType.Enemy && enemyStats) return enemyStats.def;
        return 0;
    }
    public int GetAGI()
    {
        if (unitType == UnitType.Player && characterStats) return characterStats.agi;
        if (unitType == UnitType.Enemy && enemyStats) return enemyStats.agi;
        return 0;
    }

    public void SetHP(int value)
    {
        int v = Mathf.Clamp(value, 0, maxHP);
        if (v == currentHP) return;
        currentHP = v;
        OnHPChanged?.Invoke(this);

        if (currentHP == 0)
        {
            PlayDie();
            OnDied?.Invoke(this);
        }
    }

    public void SetSP(int value)
    {
        int v = Mathf.Clamp(value, 0, maxSP);
        if (v == currentSP) return;
        currentSP = v;
        OnSPChanged?.Invoke(this);
    }

    public void GainSP(int amount)
    {
        if (amount <= 0) return;
        int v = Mathf.Clamp(currentSP + amount, 0, maxSP);
        if (v == currentSP) return;
        currentSP = v;
        OnSPChanged?.Invoke(this);
    }

    public bool SpendSP(int amount)
    {
        if (amount <= 0) return true;
        if (currentSP < amount) return false;
        currentSP -= amount;
        OnSPChanged?.Invoke(this);
        return true;
    }

    public void AddStatusEffect(StatusEffectDefinition so, CharacterScript inflictor = null)
    {
        if (!so) return;
        bool isSelfAppliedThisTurn = (inflictor != null && inflictor == this);
        activeStatusEffects.Add(new ActiveStatusEffect
        {
            effect = so,
            remainingTurns = Mathf.Max(1, so.durationTurns),
            inflictor = inflictor,
            skipDecrementThisTurn = isSelfAppliedThisTurn
        });
    }

    public void TickStatusesAtTurnEnd()
    {
        for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
        {
            var s = activeStatusEffects[i];

            if (s.effect && s.effect.dotActive && currentHP > 0)
            {
                var owner = (s.effect.dotSource == StatusEffectDefinition.DOTSourceOwner.Inflictor)
                            ? (s.inflictor != null ? s.inflictor : this)
                            : this;

                int baseStat = GetStatByType(owner, s.effect.dotBaseStat);
                int amount = 0;

                if (s.effect.dotPotencyMode == PotencyMode.Percent)
                {
                    amount = Mathf.RoundToInt(baseStat * (s.effect.dotPower / 100f));
                }
                else
                {
                    amount = Mathf.Max(0, s.effect.dotPower);
                }

                if (amount > 0)
                {
                    SetHP(currentHP - amount);
                    var popup = GetPopup();
                    if (popup) popup.Spawn(transform.position, amount, false, false);
                    if (currentHP > 0) PlayHurt();
                }
            }

            if (s.skipDecrementThisTurn)
            {
                s.skipDecrementThisTurn = false;
                activeStatusEffects[i] = s;
                continue;
            }

            s.remainingTurns--;
            if (s.remainingTurns <= 0) activeStatusEffects.RemoveAt(i);
            else activeStatusEffects[i] = s;
        }
    }

    public void FireAnim(SkillDefinition.AnimTrigger trig)
    {
        if (!animator || trig == SkillDefinition.AnimTrigger.Default) return;
        string name = GetTriggerName(trig);
        if (!string.IsNullOrEmpty(name))
            animator.SetTrigger(Animator.StringToHash(name));
    }

    private void TryResetTrigger(SkillDefinition.AnimTrigger trig)
    {
        if (!animator || trig == SkillDefinition.AnimTrigger.Default) return;
        string name = GetTriggerName(trig);
        if (!string.IsNullOrEmpty(name))
            animator.ResetTrigger(Animator.StringToHash(name));
    }

    private static string GetTriggerName(SkillDefinition.AnimTrigger trig)
    {
        switch (trig)
        {
            case SkillDefinition.AnimTrigger.Attack: return "Attack";
            case SkillDefinition.AnimTrigger.Hurt: return "Hurt";
            case SkillDefinition.AnimTrigger.Die: return "Die";
            case SkillDefinition.AnimTrigger.Shoot: return "Shoot";
            case SkillDefinition.AnimTrigger.Revive: return "Revive";
            case SkillDefinition.AnimTrigger.Spellcast_Attack: return "Spellcast - Attack";
            case SkillDefinition.AnimTrigger.Spellcast_Healing: return "Spellcast - Healing";
            case SkillDefinition.AnimTrigger.Items: return "Items";
            case SkillDefinition.AnimTrigger.Block: return "Block";
            default: return null;
        }
    }

    private int GetStatByType(CharacterScript who, StatType type)
    {
        if (who == null) who = this;
        switch (type)
        {
            case StatType.ATK: return who.GetBaseATK();
            case StatType.DEF: return who.GetBaseDEF();
            case StatType.MaxHP: return who.maxHP;
            case StatType.MaxSP: return who.maxSP;
            default: return 0;
        }
    }
}
