using UnityEngine;
using Data;

namespace Actions
{
    public static class DamageCalculator
    {
        public static int Physical(Data.SkillDefinition maybeSkill, CharacterScript attacker, CharacterScript defender, int basePower = 0, bool overrideWithBasePower = false)
        {
            int atkStat = overrideWithBasePower
                ? ApplyAtkMods(attacker, basePower)
                : GetEffectiveATK(attacker);

            int defStat = GetEffectiveDEF(defender);
            int raw = Mathf.Max(0, atkStat - defStat);

            int final = Mathf.RoundToInt(raw);
            return Mathf.Max(0, final);
        }

        public static int HealAmount(int targetMax, int amount, bool isPercent)
            => isPercent ? Mathf.RoundToInt(targetMax * (amount / 100f)) : amount;

        public static int SkillDamage(Data.SkillDefinition skill, CharacterScript attacker, CharacterScript defender)
        {
            if (skill == null || attacker == null || defender == null) return 0;
            int baseStat = GetPotencyBaseStat(skill.potencyStat, attacker, defender, isHeal: false);
            int amount = ApplyPotency(skill.power, skill.potencyMode == Data.SkillDefinition.PotencyMode.Percent, baseStat);
            int defStat = GetEffectiveDEF(defender);
            return Mathf.Max(0, amount - defStat);
        }
        public static int SkillHeal(Data.SkillDefinition skill, CharacterScript source, CharacterScript target)
        {
            if (skill == null || source == null || target == null) return 0;
            int baseStat = GetPotencyBaseStat(skill.potencyStat, source, target, isHeal: true);
            int amount = ApplyPotency(skill.power, skill.potencyMode == Data.SkillDefinition.PotencyMode.Percent, baseStat);
            return Mathf.Max(0, amount);
        }
        public static int ItemHeal(Data.ItemDefinition item, CharacterScript user, CharacterScript target)
        {
            if (item == null || user == null || target == null) return 0;
            int baseStat = GetPotencyBaseStat(item.potencyStat, user, target, isHeal: true);
            bool isPercent = item.potencyMode == Data.ItemDefinition.PotencyMode.Percent;
            int amount = ApplyPotency(item.power, isPercent, baseStat);
            return Mathf.Max(0, amount);
        }
        private static int ApplyPotency(int power, bool isPercent, int baseStat)
        {
            if (!isPercent) return Mathf.Max(0, power);
            return Mathf.RoundToInt(baseStat * (power / 100f));
        }
        private static int GetPotencyBaseStat(StatType stat, CharacterScript source, CharacterScript target, bool isHeal)
        {
            switch (stat)
            {
                case StatType.MaxHP: return target != null ? target.maxHP : 0;
                case StatType.MaxSP: return target != null ? target.maxSP : 0;
                case StatType.DEF: return source != null ? GetEffectiveDEF(source) : 0;
                case StatType.ATK: return source != null ? GetEffectiveATK(source) : 0;
                default: return 0;
            }
        }

        public static int GetEffectiveATK(CharacterScript c)
        {
            int baseAtk = c.GetBaseATK();
            return ApplyAtkMods(c, baseAtk);
        }

        public static int GetEffectiveDEF(CharacterScript c)
        {
            int baseDef = c.GetBaseDEF();
            return ApplyDefMods(c, baseDef);
        }

        private static int ApplyAtkMods(CharacterScript c, int baseValue)
        {
            int flat = 0;
            int pct = 0;

            foreach (var s in c.activeStatusEffects)
            {
                if (s.effect == null) continue;
                if (s.effect.stat != StatType.ATK) continue;

                if (s.effect.potencyMode == PotencyMode.Percent) pct += s.effect.power;
                else flat += s.effect.power;
            }

            float mul = 1f + (pct / 100f);
            return Mathf.Max(0, Mathf.RoundToInt((baseValue + flat) * mul));
        }

        private static int ApplyDefMods(CharacterScript c, int baseValue)
        {
            int flat = 0;
            int pct = 0;

            foreach (var s in c.activeStatusEffects)
            {
                if (s.effect == null) continue;
                if (s.effect.stat != StatType.DEF) continue;

                if (s.effect.potencyMode == PotencyMode.Percent) pct += s.effect.power;
                else flat += s.effect.power;
            }

            float mul = 1f + (pct / 100f);
            return Mathf.Max(0, Mathf.RoundToInt((baseValue + flat) * mul));
        }
    }
}
