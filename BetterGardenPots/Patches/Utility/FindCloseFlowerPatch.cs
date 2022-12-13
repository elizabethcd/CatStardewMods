using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using SObject = StardewValley.Object;

namespace BetterGardenPots.Patches.Utility
{
    internal class FindCloseFlowerPatch
    {
        public static bool Prefix(GameLocation location, Vector2 startTileLocation, int range, Func<Crop, bool> additional_check, ref Crop __result)
        {
            __result = null;

            Queue<Vector2> vector2Queue = new Queue<Vector2>();
            HashSet<Vector2> vector2Set = new HashSet<Vector2>();

            vector2Queue.Enqueue(startTileLocation);
            for (int attempts = 0; range >= 0 || (range < 0 && attempts <= 150); attempts++)
            {
                Vector2 index2 = vector2Queue.Dequeue();

                Crop current = null;

                // Get any crop at this tile location, either in the ground or in a garden pot
                if (location.terrainFeatures.TryGetValue(index2, out TerrainFeature feature) && feature is HoeDirt dirt)
                    current = dirt.crop;
                else if (location.objects.TryGetValue(index2, out SObject obj) && obj is StardewValley.Objects.IndoorPot pot &&
                         pot.hoeDirt.Value != null)
                    current = pot.hoeDirt.Value.crop;

                // If the crop is not null, a flower, fully grown, and not dead, we've got a winner
                if (current != null && new SObject(current.indexOfHarvest.Value, 1).Category == -80 &&
                    current.currentPhase.Value >= current.phaseDays.Count - 1 && !current.dead.Value)
                {
                    __result = current;
                    break;
                }

                // Add each adjacent tile location as long as it's within range
                foreach (Vector2 adjacentTileLocation in StardewValley.Utility.getAdjacentTileLocations(index2))
                    if (!vector2Set.Contains(adjacentTileLocation) && (range < 0 || Math.Abs(adjacentTileLocation.X - startTileLocation.X) + Math.Abs(adjacentTileLocation.Y - startTileLocation.Y) <= (float)range))
                        vector2Queue.Enqueue(adjacentTileLocation);
                vector2Set.Add(index2);
            }

            return false;
        }
    }
}
