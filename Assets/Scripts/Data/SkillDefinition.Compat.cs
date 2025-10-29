using System;

namespace Data
{
    public partial class SkillDefinition
    {
        public bool TargetsSelfOnly
            => targetSelection == TargetSelection.SelfOnly;

        public bool TargetsAllies
            => (targetFaction & TargetFaction.Allies) != 0;

        public bool targetsAll
            => targetSelection == TargetSelection.Multi;
    }
}
