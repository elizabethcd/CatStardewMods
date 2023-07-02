using Microsoft.Xna.Framework;
using StardewValley;
using SObject = StardewValley.Object;

namespace BetterGardenPots.Patches.IndoorPot
{
    internal class PerformToolActionPatch
    {
        private static Crop crop;
        private static BetterGardenPotsModConfig Config;

        public static void Init(BetterGardenPotsModConfig config)
        {
            Config = config;
        }

        public static void Prefix(StardewValley.Objects.IndoorPot __instance)
        {
            // Do nothing if config says not to
            if (!Config.HarvestMatureCropsWhenGardenPotBreaks)
                return;

            crop = __instance.hoeDirt.Value?.crop;
        }

        public static void Postfix(StardewValley.Objects.IndoorPot __instance, GameLocation location)
        {
            // Do nothing if config says not to
            if (!Config.HarvestMatureCropsWhenGardenPotBreaks)
                return;

            if (__instance.hoeDirt.Value?.crop == null && crop != null && crop.currentPhase.Value == crop.phaseDays.Count - 1)
                location.debris.Add(new Debris(new SObject(crop.indexOfHarvest.Value, 1), __instance.TileLocation * 64f + new Vector2(32f, 32f)));
        }
    }
}
