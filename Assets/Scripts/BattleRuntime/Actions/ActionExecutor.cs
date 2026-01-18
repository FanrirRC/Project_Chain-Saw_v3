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

        [Header("Melee Movement")]
        [SerializeField] private float meleeDistance = 0.9f;
        [SerializeField] private float moveSpeed = 6f;

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

            Vector3 startPos = actor.transform.position;
            Quaternion startRot = actor.transform.rotation;

            // Face target (Y-only)
            Vector3 look = t.transform.position - actor.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f)
                actor.transform.rotation = Quaternion.LookRotation(look);

            var anim = actor.GetComponent<AnimDriver>();
            bool isMelee = actor.basicAttackMove == SkillDefinition.MoveStyle.Melee;

            if (isMelee)
            {
                if (anim) anim.Fire(AnimDriver.AnimEvent.Melee);
                Vector3 toTarget = (t.transform.position - actor.transform.position);
                toTarget.y = 0f;
                Vector3 meleePos = t.transform.position - toTarget.normalized * Mathf.Max(0.05f, meleeDistance);
                meleePos.y = startPos.y;
                yield return MoveTo(actor.transform, meleePos, moveSpeed);
            }

            actor.PlayAttack();
            if (anim) anim.Fire(AnimDriver.AnimEvent.Attack);
            yield return new WaitForSeconds(actor.attackWindup);

            int dmg = DamageCalculator.Physical(null, actor, t);
            t.SetHP(t.currentHP - dmg);

            if (damagePopup) damagePopup.Spawn(t.transform.position, dmg, false, false);
            if (t.currentHP > 0) t.PlayHurt();

            yield return new WaitForSeconds(actor.attackRecover);
            actor.PlayIdle();

            if (isMelee)
                yield return MoveTo(actor.transform, startPos, moveSpeed);

            actor.transform.rotation = startRot;

            actor.GainSP(1);
        }

        private IEnumerator DoSkill(CharacterScript actor, SkillDefinition skill, List<CharacterScript> targets)
        {
            if (!skill) yield break;

            if (skill.targetSelection != SkillDefinition.TargetSelection.SelfOnly && (targets == null || targets.Count == 0))
                yield break;

            if (actor.currentSP < skill.spCost) yield break;
            actor.SetSP(actor.currentSP - skill.spCost);

            Vector3 startPos = actor.transform.position;
            Quaternion startRot = actor.transform.rotation;

            CharacterScript focal = null;
            if (skill.targetSelection != SkillDefinition.TargetSelection.SelfOnly && targets != null)
            {
                foreach (var z in targets) { if (z) { focal = z; break; } }
            }

            bool shouldRotate = (skill.targetSelection == SkillDefinition.TargetSelection.Single);
            
            Vector3 to = Vector3.zero;

            if (focal)
            {
                to = focal.transform.position - actor.transform.position;
                to.y = 0f;
                if (shouldRotate && to.sqrMagnitude > 0.0001f)
                    actor.transform.rotation = Quaternion.LookRotation(to);

                if (skill.moveStyle == SkillDefinition.MoveStyle.Melee)
                {
                    var animDrv = actor.GetComponent<AnimDriver>();
                    if (animDrv) animDrv.Fire(AnimDriver.AnimEvent.Melee);

                    Vector3 meleePos = focal.transform.position - to.normalized * Mathf.Max(0.05f, meleeDistance);
                    meleePos.y = startPos.y;
                    yield return MoveTo(actor.transform, meleePos, moveSpeed);
                }
            }

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
                    actor.PlayAttack();
                    if (animDrv) animDrv.Fire(AnimDriver.AnimEvent.SkillCast);
                }
            }

            yield return new WaitForSeconds(actor.attackWindup);

            // ===== EFFECT RESOLUTION =====
            if (skill.effectType == SkillDefinition.EffectType.Damage)
            {
                foreach (var t in targets)
                {
                    if (!t) continue;
                    int dmg = DamageCalculator.SkillDamage(skill, actor, t);
                    t.SetHP(t.currentHP - dmg);
                    if (damagePopup) damagePopup.Spawn(t.transform.position, dmg, false, false);
                    if (t.currentHP > 0) t.PlayHurt();
                }
            }
            else if (skill.effectType == SkillDefinition.EffectType.Heal)
            {
                var list = targets;
                if ((list == null || list.Count == 0) && skill.targetSelection == SkillDefinition.TargetSelection.SelfOnly)
                    list = new List<CharacterScript> { actor };

                foreach (var t in list)
                {
                    if (!t) continue;
                    int heal = DamageCalculator.SkillHeal(skill, actor, t);
                    t.SetHP(t.currentHP + heal);
                    if (damagePopup) damagePopup.Spawn(t.transform.position, heal, false, true);
                }
            }

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
                            t.AddStatusEffect(entry.status, actor);
                        else
                            TryRemoveStatus(t, entry.status);
                    }
                }
            }

            yield return new WaitForSeconds(actor.attackRecover);
            actor.PlayIdle();

            if (skill.moveStyle == SkillDefinition.MoveStyle.Melee)
                yield return MoveTo(actor.transform, startPos, moveSpeed);

            actor.transform.rotation = startRot;
        }

        private IEnumerator DoItem(CharacterScript actor, Data.ItemDefinition item, List<CharacterScript> targets)
        {
            if (!item) yield break;

            var inv = actor.GetComponent<ItemsInventory>();
            if (inv && !inv.TryConsume(item, 1)) yield break;

            if (item.effectType == Data.ItemDefinition.EffectType.Heal)
            {
                foreach (var t in targets)
                {
                    if (!t) continue;
                    int heal = DamageCalculator.ItemHeal(item, actor, t);
                    t.SetHP(t.currentHP + heal);
                    damagePopup?.Spawn(t.transform.position, heal, false, true);
                }
            }
            else if (item.effectType == Data.ItemDefinition.EffectType.ApplyStatus && item.statuses != null)
            {
                foreach (var entry in item.statuses)
                {
                    if (entry == null || entry.status == null) continue;

                    foreach (var t in targets)
                    {
                        if (!t) continue;

                        if (entry.op == Data.ItemDefinition.StatusOp.Inflict)
                            t.AddStatusEffect(entry.status, actor);
                        else
                            TryRemoveStatus(t, entry.status);
                    }
                }
            }

            yield return null;
        }

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
                default: return null;
            }
        }

        private static System.Reflection.MethodInfo _cachedRemove;
        private static bool TryRemoveStatus(CharacterScript target, StatusEffectDefinition def)
        {
            if (!target || !def) return false;

            if (_cachedRemove == null)
            {
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

            return false;
        }
    }
}
