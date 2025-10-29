using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Data;

namespace Actions
{
    public enum IntentType { None, BasicAttack, Skill, Item, Guard }

    public class ActionExecutor : MonoBehaviour
    {
        [SerializeField] private UI.DamagePopup damagePopup;

        // === Approach/return movement (also used for skills with MoveStyle.Approach) ===
        [Header("Approach Movement")]
        [SerializeField] private float approachDistance = 0.9f; // stop this far from target
        [SerializeField] private float moveSpeed = 6f;          // units/sec

        private IEnumerator MoveTo(Transform who, Vector3 targetPos, float speed)
        {
            if (!who) yield break;
            while ((who.position - targetPos).sqrMagnitude > 0.0004f)
            {
                who.position = Vector3.MoveTowards(who.position, targetPos, speed * Time.deltaTime);
                yield return null;
            }
        }

        public IEnumerator Execute(CharacterScript actor, UI.CommandDecision decision, List<CharacterScript> targets)
        {
            switch (decision.Type)
            {
                case UI.CommandDecision.DecisionType.Attack:
                    yield return DoBasicAttack(actor, targets);
                    break;
                case UI.CommandDecision.DecisionType.Skill:
                    yield return DoSkill(actor, decision.Skill, targets);
                    break;
                case UI.CommandDecision.DecisionType.Item:
                    yield return DoItem(actor, decision.Item, targets);
                    break;
                case UI.CommandDecision.DecisionType.Guard:
                    // (guard behavior unchanged)
                    yield break;
            }
        }

        public IEnumerator ExecuteIntent(CharacterScript actor, EnemyAI.Intent intent)
        {
            switch (intent.Type)
            {
                case IntentType.BasicAttack:
                    yield return DoBasicAttack(actor, intent.Targets);
                    break;
                case IntentType.Skill:
                    yield return DoSkill(actor, intent.Skill, intent.Targets);
                    break;
                case IntentType.Item:
                    yield return DoItem(actor, intent.Item, intent.Targets);
                    break;
            }
        }

        private IEnumerator DoBasicAttack(CharacterScript actor, List<CharacterScript> targets)
        {
            if (targets == null || targets.Count == 0) yield break;
            var t = targets[0];
            if (!t) yield break;

            // Cache transform to restore after the action
            Vector3 startPos = actor.transform.position;
            Quaternion startRot = actor.transform.rotation;

            // Face target (Y-only)
            Vector3 look = t.transform.position - actor.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f)
                actor.transform.rotation = Quaternion.LookRotation(look);

            // Optional pre-anim + approach
            var anim = actor.GetComponent<AnimDriver>();
            if (anim) anim.Fire(AnimDriver.AnimEvent.Approach);

            Vector3 toTarget = (t.transform.position - actor.transform.position);
            toTarget.y = 0f;
            Vector3 approachPos = t.transform.position - toTarget.normalized * Mathf.Max(0.05f, approachDistance);
            approachPos.y = startPos.y;
            yield return MoveTo(actor.transform, approachPos, moveSpeed);

            // Play attack + windup
            actor.PlayAttack();
            if (anim) anim.Fire(AnimDriver.AnimEvent.Attack);
            yield return new WaitForSeconds(actor.attackWindup);

            // Damage application
            int dmg = DamageCalculator.Physical(null, actor, t);
            t.SetHP(t.currentHP - dmg);

            if (damagePopup) damagePopup.Spawn(t.transform.position, dmg, false, false);
            if (t.currentHP > 0) t.PlayHurt();

            // Recover
            yield return new WaitForSeconds(actor.attackRecover);
            actor.PlayIdle();

            // Return & restore
            yield return MoveTo(actor.transform, startPos, moveSpeed);
            actor.transform.rotation = startRot;

            // SP on basic
            actor.GainSP(1);
        }

        private IEnumerator DoSkill(CharacterScript actor, SkillDefinition skill, List<CharacterScript> targets)
        {
            if (!skill) yield break;

            // Enforce targets if needed (legacy behavior: if not self-only and none supplied, bail)
            if (skill.targetSelection != SkillDefinition.TargetSelection.SelfOnly && (targets == null || targets.Count == 0))
                yield break;

            // SP gate
            if (actor.currentSP < skill.spCost) yield break;
            actor.SetSP(actor.currentSP - skill.spCost);

            // Cache for restore
            Vector3 startPos = actor.transform.position;
            Quaternion startRot = actor.transform.rotation;

            // Choose focal (first live) target when not SelfOnly (for facing/approach)
            CharacterScript focal = null;
            if (skill.targetSelection != SkillDefinition.TargetSelection.SelfOnly && targets != null)
            {
                foreach (var z in targets) { if (z) { focal = z; break; } }
            }

            // Face + approach depending on MoveStyle
            if (focal)
            {
                Vector3 to = focal.transform.position - actor.transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.0001f)
                    actor.transform.rotation = Quaternion.LookRotation(to);

                if (skill.moveStyle == SkillDefinition.MoveStyle.Approach)
                {
                    var animDrv = actor.GetComponent<AnimDriver>();
                    if (animDrv) animDrv.Fire(AnimDriver.AnimEvent.Approach);

                    Vector3 approachPos = focal.transform.position - to.normalized * Mathf.Max(0.05f, approachDistance);
                    approachPos.y = startPos.y;
                    yield return MoveTo(actor.transform, approachPos, moveSpeed);
                }
            }

            // Animation trigger: use enum mapping if not Default; otherwise fall back to legacy PlayAttack + generic skill event
            {
                var animDrv = actor.GetComponent<AnimDriver>();
                bool usedOverride = false;

                if (skill.animTrigger != SkillDefinition.AnimTrigger.Default && animDrv && animDrv.animator)
                {
                    string trigName = GetAnimTriggerName(skill.animTrigger);
                    if (!string.IsNullOrEmpty(trigName))
                    {
                        animDrv.animator.SetTrigger(Animator.StringToHash(trigName));
                        usedOverride = true;
                    }
                }

                if (!usedOverride)
                {
                    // Legacy default behavior (kept): use attack anim for skills
                    actor.PlayAttack();
                    if (animDrv) animDrv.Fire(AnimDriver.AnimEvent.SkillCast);
                }
            }

            // Windup
            yield return new WaitForSeconds(actor.attackWindup);

            // ===== EFFECT RESOLUTION =====
            // 1) Damage / Heal based on EffectType + PotencyMode
            if (skill.effectType == SkillDefinition.EffectType.Damage)
            {
                foreach (var t in targets)
                {
                    if (!t) continue;
                    // Interpreting power: FlatNumber (pass override=true) vs Percent (override=false, use calculator as percent if it supports)
                    bool useFlat = (skill.potencyMode == SkillDefinition.PotencyMode.FlatNumber);
                    int dmg = DamageCalculator.Physical(skill, actor, t, skill.power, useFlat);
                    t.SetHP(t.currentHP - dmg);
                    if (damagePopup) damagePopup.Spawn(t.transform.position, dmg, false, false);
                    if (t.currentHP > 0) t.PlayHurt();
                }
            }
            else if (skill.effectType == SkillDefinition.EffectType.Heal)
            {
                // If SelfOnly and no targets passed, heal the caster
                var list = targets;
                if ((list == null || list.Count == 0) && skill.targetSelection == SkillDefinition.TargetSelection.SelfOnly)
                    list = new List<CharacterScript> { actor };

                foreach (var t in list)
                {
                    if (!t) continue;
                    bool isPercent = (skill.potencyMode == SkillDefinition.PotencyMode.Percent);
                    int heal = DamageCalculator.HealAmount(t.maxHP, skill.power, isPercent);
                    t.SetHP(t.currentHP + heal);
                    if (damagePopup) damagePopup.Spawn(t.transform.position, heal, false, true);
                }
            }
            // EffectType.None: intentionally no direct HP change

            // 2) Apply / Remove statuses
            if (skill.statuses != null && skill.statuses.Count > 0)
            {
                var list = targets;
                if ((list == null || list.Count == 0) && skill.targetSelection == SkillDefinition.TargetSelection.SelfOnly)
                    list = new List<CharacterScript> { actor };

                foreach (var entry in skill.statuses)
                {
                    if (entry == null || entry.status == null) continue;

                    foreach (var t in list)
                    {
                        if (!t) continue;

                        if (entry.op == SkillDefinition.StatusOp.Inflict)
                        {
                            t.AddStatusEffect(entry.status);

                            // Optional: fire status-apply anim if present
                            var animT = t.GetComponent<AnimDriver>();
                            if (animT) animT.Fire(AnimDriver.AnimEvent.StatusApply);
                            if (entry.status.onApplyTrigger.Name != null && animT && animT.animator && !string.IsNullOrEmpty(entry.status.onApplyTrigger.Name))
                            {
                                entry.status.onApplyTrigger.ValidateOn(animT.animator);
                                animT.animator.SetTrigger(entry.status.onApplyTrigger.Hash);
                            }
                        }
                        else // Remove
                        {
                            TryRemoveStatus(t, entry.status);
                            var animT = t.GetComponent<AnimDriver>();
                            if (animT) animT.Fire(AnimDriver.AnimEvent.StatusExpire);
                            if (entry.status.onExpireTrigger.Name != null && animT && animT.animator && !string.IsNullOrEmpty(entry.status.onExpireTrigger.Name))
                            {
                                entry.status.onExpireTrigger.ValidateOn(animT.animator);
                                animT.animator.SetTrigger(entry.status.onExpireTrigger.Hash);
                            }
                        }
                    }
                }
            }

            // Recover
            yield return new WaitForSeconds(actor.attackRecover);
            actor.PlayIdle();

            // Return & restore (only moved if we approached)
            if (skill.moveStyle == SkillDefinition.MoveStyle.Approach)
            {
                yield return MoveTo(actor.transform, startPos, moveSpeed);
            }
            actor.transform.rotation = startRot;
        }

        private IEnumerator DoItem(CharacterScript actor, Data.ItemDefinition item, List<CharacterScript> targets)
        {
            if (!item) yield break;

            // consume 1 if you have an ItemsInventory
            var inv = actor.GetComponent<ItemsInventory>();
            if (inv && !inv.TryConsume(item, 1)) yield break;

            if (item.effectType == Data.ItemDefinition.EffectType.Heal)
            {
                foreach (var t in targets)
                {
                    if (!t) continue;
                    int heal = DamageCalculator.HealAmount(t.maxHP, item.power, item.isPercent);
                    t.SetHP(t.currentHP + heal);
                    damagePopup?.Spawn(t.transform.position, heal, false, true);
                }
            }
            else if (item.effectType == Data.ItemDefinition.EffectType.ApplyStatus && item.statusToApply)
            {
                foreach (var t in targets) if (t) t.AddStatusEffect(item.statusToApply);
            }

            yield return null;
        }

        // ===== Helpers =====

        private static string GetAnimTriggerName(SkillDefinition.AnimTrigger trig)
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
                default: return null; // Default → let legacy calls run
            }
        }

        // Tries common remove APIs without forcing you to implement a new method.
        private static System.Reflection.MethodInfo _cachedRemove;
        private static bool TryRemoveStatus(CharacterScript target, StatusEffectDefinition def)
        {
            if (!target || !def) return false;

            // Cache a remove method if available:
            if (_cachedRemove == null)
            {
                // Try common names
                var ty = target.GetType();
                _cachedRemove = ty.GetMethod("RemoveStatusEffect", new[] { typeof(StatusEffectDefinition) })
                                ?? ty.GetMethod("RemoveStatus", new[] { typeof(StatusEffectDefinition) })
                                ?? ty.GetMethod("ClearStatusEffect", new[] { typeof(StatusEffectDefinition) });
            }

            if (_cachedRemove != null)
            {
                _cachedRemove.Invoke(target, new object[] { def });
                return true;
            }

            // No remove method found—silently skip (non-breaking)
            return false;
        }
    }
}
