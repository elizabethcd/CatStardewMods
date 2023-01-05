using Microsoft.Xna.Framework;
using StardewValley;

namespace CustomWarpLocations
{
    internal class NewWarpLocations
    {
        static Point getGameFarmWarps()
        {
            int default_x = 48;
            int default_y = 7;
            try
            {
                if (Game1.whichFarm == 5)
                {
                    default_x = 48;
                    default_y = 39;
                }
                else if (Game1.whichFarm == 6)
                {
                    default_x = 82;
                    default_y = 29;
                }
            }
            catch
            {
            }
            
            Point mapPropertyPosition = Game1.getFarm().GetMapPropertyPosition("WarpTotemEntry", default_x, default_y);
            return mapPropertyPosition;
        }

        public WarpLocation FarmWarpLocation_Totem { get; set; } = new WarpLocation("Farm", getGameFarmWarps().X, getGameFarmWarps().Y);

        public WarpLocation MountainWarpLocation_Totem { get; set; } = new WarpLocation("Mountain", 31, 20);

        public WarpLocation BeachWarpLocation_Totem { get; set; } = new WarpLocation("Beach", 20, 4);

        public WarpLocation DesertWarpLocation_Totem { get; set; } = new WarpLocation("Desert", 35, 43);

        public WarpLocation IslandWarpLocation_Totem { get; set; } = new WarpLocation("IslandSouth", 11, 11);

        public WarpLocation FarmWarpLocation_Scepter { get; set; } = new WarpLocation("Farm", 64, 15);

        public WarpLocation MountainWarpLocation_Obelisk { get; set; } = new WarpLocation("Mountain", 31, 20);

        public WarpLocation BeachWarpLocation_Obelisk { get; set; } = new WarpLocation("Beach", 20, 4);

        public WarpLocation DesertWarpLocation_Obelisk { get; set; } = new WarpLocation("Desert", 35, 43);

        public WarpLocation IslandWarpLocation_Obelisk { get; set; } = new WarpLocation("IslandSouth", 11, 11);
    }
}
