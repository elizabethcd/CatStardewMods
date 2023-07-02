using StardewValley;

namespace BetterGardenPots.Patches.IndoorPot
{
    internal class PerformObjectDropInActionPatchSeasons
    {
        private static bool wasOutdoors;
        
        public static void Prefix()
        {
            // Do nothing if config says to do nothing
            if (!BetterGardenPotsMod.Config.AllowCropsToGrowInAnySeasonOutsideWhenInGardenPot)
                return;

            wasOutdoors = Game1.currentLocation.IsOutdoors;
            Game1.currentLocation.IsOutdoors = false;
        }

        public static void Postfix()
        {
            // Do nothing if config says to do nothing
            if (!BetterGardenPotsMod.Config.AllowCropsToGrowInAnySeasonOutsideWhenInGardenPot)
                return;

            Game1.currentLocation.IsOutdoors = wasOutdoors;
        }
    }
}
