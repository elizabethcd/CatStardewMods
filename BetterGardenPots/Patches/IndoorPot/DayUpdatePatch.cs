using StardewValley;

namespace BetterGardenPots.Patches.IndoorPot
{
    internal class DayUpdatePatch
    {
        private static bool wasOutdoors;

        public static void Prefix(GameLocation location)
        {
            // Do nothing if config says to do nothing
            if (!BetterGardenPotsMod.Config.AllowCropsToGrowInAnySeasonOutsideWhenInGardenPot)
                return;

            wasOutdoors = location.IsOutdoors;
            location.IsOutdoors = false;
        }

        public static void Postfix(StardewValley.Objects.IndoorPot __instance, GameLocation location)
        {
            // Do nothing if config says to do nothing
            if (!BetterGardenPotsMod.Config.AllowCropsToGrowInAnySeasonOutsideWhenInGardenPot)
                return;

            location.IsOutdoors = wasOutdoors;

            if (Game1.isRaining && location.IsOutdoors)
                __instance.hoeDirt.Value.state.Value = 1;
            __instance.showNextIndex.Value = __instance.hoeDirt.Value.state.Value == 1;
        }
    }
}
