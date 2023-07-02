using StardewValley;

namespace BetterGardenPots.Patches.IndoorPot
{
    internal class PerformObjectDropInActionPatchSeasons
    {
        private static bool wasOutdoors;
        private static BetterGardenPotsModConfig Config;

        public static void Init(BetterGardenPotsModConfig config)
        {
            Config = config;
        }

        public static void Prefix()
        {
            // Do nothing if config says to do nothing
            if (!Config.AllowCropsToGrowInAnySeasonOutsideWhenInGardenPot)
                return;

            wasOutdoors = Game1.currentLocation.IsOutdoors;
            Game1.currentLocation.IsOutdoors = false;
        }

        public static void Postfix()
        {
            // Do nothing if config says to do nothing
            if (!Config.AllowCropsToGrowInAnySeasonOutsideWhenInGardenPot)
                return;

            Game1.currentLocation.IsOutdoors = wasOutdoors;
        }
    }
}
