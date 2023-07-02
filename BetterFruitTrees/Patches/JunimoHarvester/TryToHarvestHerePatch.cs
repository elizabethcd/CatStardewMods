namespace BetterFruitTrees.Patches.JunimoHarvester
{
    /// <summary>If a harvestable fruit tree is nearby, start the harvest timer.</summary>
    internal class TryToHarvestHerePatch
    {
        public static void Postfix(StardewValley.Characters.JunimoHarvester __instance)
        {
            // If junimo harvesting disabled, do nothing
            if (Utils.Config.Disable_Fruit_Tree_Junimo_Harvesting)
                return;

            if (Utils.IsAdjacentReadyToHarvestFruitTree(__instance.getTileLocation(), __instance.currentLocation))
                Utils.GetJunimoHarvesterHarvestTimer(__instance).SetValue(2000);
        }
    }
}
