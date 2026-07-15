namespace Universes.Game
{
    public static class PrestigeTreeVisibility
    {
        public static bool IsNodeVisible(PrestigeState prestige, PrestigeUpgradeDefinition definition)
        {
            if (prestige == null || definition == null)
                return false;

            if (prestige.GetLevel(definition) > 0)
                return true;

            return definition.ArePrerequisitesMet(prestige, out _);
        }

        public static bool IsConnectionVisible(PrestigeState prestige,
            PrestigeUpgradeDefinition from, PrestigeUpgradeDefinition to)
        {
            if (to == null)
                return false;

            if (!IsNodeVisible(prestige, to))
                return false;

            return from == null || IsNodeVisible(prestige, from);
        }
    }
}
