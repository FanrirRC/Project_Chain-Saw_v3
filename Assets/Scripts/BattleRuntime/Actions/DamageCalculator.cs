using UnityEngine;
using Data; // for StatType and PotencyMode

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

            // If you later add global damage dealt/taken %, apply here.
            float dealtMul = 1f;
            float takenMul = 1f;

            int final = Mathf.RoundToInt(raw * dealtMul * takenMul);
            return Mathf.Max(0, final);
        }

        public static int HealAmount(int targetMax, int amount, bool isPercent)
            => isPercent ? Mathf.RoundToInt(targetMax * (amount / 100f)) : amount;

        // --- Effective stats (use new status model) ---

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
