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

        [Header("Approach Movement")]
        [SerializeField] private float approachDistance = 0.9f; 
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

            Vector3 look = t.transform.position - actor.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f)
                actor.transform.rotation = Quaternion.LookRotation(look);

            var anim = actor.GetComponent<AnimDriver>();
            if (anim) anim.Fire(AnimDriver.AnimEvent.Approach);

            Vector3 toTarget = (t.transform.position - actor.transform.position);
            toTarget.y = 0f;
            Vector3 approachPos = t.transform.position - toTarget.normalized * Mathf.Max(0.05f, approachDistance);
            approachPos.y = startPos.y; 
            yield return MoveTo(actor.transform, approachPos, moveSpeed);

            actor.PlayAttack();
            if (anim) anim.Fire(AnimDriver.AnimEvent.Attack); 
            yield return new WaitForSeconds(actor.attackWindup);

            int dmg = DamageCalculator.Physical(null, actor, t);
            t.SetHP(t.currentHP - dmg);

            if (damagePopup) damagePopup.Spawn(t.transform.position, dmg, false, false);
            if (t.currentHP > 0) t.PlayHurt();

            yield return new WaitForSeconds(actor.attackRecover);
            actor.PlayIdle();

            yield return MoveTo(actor.transform, startPos, moveSpeed);
            actor.transform.rotation = startRot;

            actor.GainSP(1);
        }

        private IEnumerator DoSkill(CharacterScript actor, Data.SkillDefinition skill, List<CharacterScript> targets)
        {
            if (!skill) yield break;

            if (!skill.TargetsSelfOnly && (targets == null || targets.Count == 0))
                yield break;

            if (actor.currentSP < skill.spCost) yield break;
            actor.SetSP(actor.currentSP - skill.spCost);

            Vector3 startPos = actor.transform.position;
            Quaternion startRot = actor.transform.rotation;

            CharacterScript firstTarget = null;
            if (!skill.TargetsSelfOnly && targets != null)
            {
                foreach (var z in targets) { if (z) { firstTarget = z; break; } }
            }

            if (firstTarget)
            {
                Vector3 to = firstTarget.transform.position - actor.transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.0001f)
                    actor.transform.rotation = Quaternion.LookRotation(to);

                var animDrv = actor.GetComponent<AnimDriver>();
                if (animDrv) animDrv.Fire(AnimDriver.AnimEvent.Approach);

                Vector3 approachPos = firstTarget.transform.position - to.normalized * Mathf.Max(0.05f, approachDistance);
                approachPos.y = startPos.y;
                yield return MoveTo(actor.transform, approachPos, moveSpeed);
            }

            {
                var animDrv = actor.GetComponent<AnimDriver>();
                bool usedOverride = false;
                if (skill.overrideActorTrigger && animDrv && animDrv.animator && !string.IsNullOrEmpty(skill.skillTrigger.Name))
                {
                    skill.skillTrigger.ValidateOn(animDrv.animator);
                    animDrv.animator.SetTrigger(skill.skillTrigger.Hash);
                    usedOverride = true;
                }

                if (!usedOverride)
                {
                    actor.PlayAttack();
                    if (animDrv) animDrv.Fire(AnimDriver.AnimEvent.SkillCast);
                }
            }

            yield return new WaitForSeconds(actor.attackWindup);

            if (skill.effectType == Data.SkillDefinition.EffectType.Damage)
            {
                foreach (var t in targets)
                {
                    if (!t) continue;
                    int dmg = DamageCalculator.Physical(skill, actor, t, skill.power, skill.overrideWithPower);
                    t.SetHP(t.currentHP - dmg);
                    if (damagePopup) damagePopup.Spawn(t.transform.position, dmg, false, false);
                    if (t.currentHP > 0) t.PlayHurt();
                }
            }
            else if (skill.effectType == Data.SkillDefinition.EffectType.Heal)
            {
                if ((targets == null || targets.Count == 0) && skill.TargetsSelfOnly)
                    targets = new System.Collections.Generic.List<CharacterScript> { actor };

                foreach (var t in targets)
                {
                    if (!t) continue;
                    int heal = DamageCalculator.HealAmount(t.maxHP, skill.power, skill.isPercent);
                    t.SetHP(t.currentHP + heal);
                    if (damagePopup) damagePopup.Spawn(t.transform.position, heal, false, true);
                }
            }
            else if (skill.effectType == Data.SkillDefinition.EffectType.ApplyStatus && skill.statusToApply)
            {
                if ((targets == null || targets.Count == 0) && skill.TargetsSelfOnly)
                    targets = new System.Collections.Generic.List<CharacterScript> { actor };

                foreach (var t in targets)
                {
                    if (!t) continue;
                    t.AddStatusEffect(skill.statusToApply);

                    var animT = t.GetComponent<AnimDriver>();
                    if (animT) animT.Fire(AnimDriver.AnimEvent.StatusApply);
                    if (skill.statusToApply && animT && animT.animator && !string.IsNullOrEmpty(skill.statusToApply.onApplyTrigger.Name))
                    {
                        skill.statusToApply.onApplyTrigger.ValidateOn(animT.animator);
                        animT.animator.SetTrigger(skill.statusToApply.onApplyTrigger.Hash);
                    }
                }
            }

            yield return new WaitForSeconds(actor.attackRecover);
            actor.PlayIdle();

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
    }
}
