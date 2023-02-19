using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GiantCropRing
{
    public class ModEntry : Mod
    {
        private ModConfig config;
        private IModHelper modHelper;

        /// <summary>The Json Assets mod API.</summary>
        private IJsonAssets JA_API;
        /// <summary>The More Giant Crops mod API.</summary>
        private IMoreGiantCrops MoreGiantCropsAPI;
        /// <summary>The Giant Crop Tweaks mod API.</summary>
        private IGiantCropTweaks GiantCropTweaksAPI;

        /// <summary>The item ID for the Giant Crop Ring.</summary>
        public int GiantCropRingID => this.JA_API.GetObjectId("Giant Crop Ring");

        // Helpful records of giant crops available
        private int[] JsonAssetsGiantCrops;
        private int[] MoreGiantCropsGiantCrops;
        private IReadOnlyDictionary<string, IGiantCropData> GiantCropTweaksGiantCrops;

        private int numberOfTimeTicksWearingOneRing;
        private int numberOfTimeTicksWearingTwoRings;

        private int totalNumberOfSeenTimeTicks;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.DayEnding += this.OnDayEnding;
            helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
            helper.Events.Content.AssetRequested += this.OnAssetRequested;
            this.config = helper.ReadConfig<ModConfig>();
            this.modHelper = helper;
        }

        /// <summary>Raised after the game is launched.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // Grab JA API in order to create ring
            JA_API = this.modHelper.ModRegistry.GetApi<IJsonAssets>("spacechase0.JsonAssets");
            if (JA_API == null)
            {
                this.Monitor.Log("Could not get Json Assets API, mod will not work!", LogLevel.Error);
            }
            else
            {
                this.JA_API.LoadAssets(Path.Combine(this.Helper.DirectoryPath, "assets", "json-assets"), this.Helper.Translation);
            }

            // Grab More Giant Crops API
            MoreGiantCropsAPI = this.modHelper.ModRegistry.GetApi<IMoreGiantCrops>("spacechase0.MoreGiantCrops");

            // Grab Giant Crop Tweaks API
            GiantCropTweaksAPI = this.modHelper.ModRegistry.GetApi<IGiantCropTweaks>("leclair.giantcroptweaks");
        }

        /// <summary>Raised after the in-game clock time changes.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {
            int numberOfCropRingsWorn = Game1.player.GetEffectsOfRingMultiplier(GiantCropRingID);

            if (numberOfCropRingsWorn >= 2)
            {
                this.numberOfTimeTicksWearingOneRing++;
                this.numberOfTimeTicksWearingTwoRings++;
            }
            else if (numberOfCropRingsWorn == 1)
                this.numberOfTimeTicksWearingOneRing++;

            this.totalNumberOfSeenTimeTicks++;

            this.Monitor.Log($"Player is wearing {numberOfCropRingsWorn} Giant Crop Rings");
        }

        /// <summary>Raised after a game menu is opened, closed, or replaced.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (Game1.activeClickableMenu is ShopMenu)
            {
                ShopMenu shop = (ShopMenu)Game1.activeClickableMenu;
                if (shop.portraitPerson != null && shop.portraitPerson.Name == "Pierre") // && Game1.dayOfMonth % 7 == )
                {
                    var ring = new StardewValley.Objects.Ring(GiantCropRingID);

                    shop.itemPriceAndStock.Add(ring, new[] { this.config.cropRingPrice, int.MaxValue });
                    shop.forSale.Add(ring);
                }
            }
        }

        /// <summary>Raised when an asset is requested.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            // Can't do nothing if we ain't got API
            if (JA_API == null)
            {
                return;
            }

            // Edit the giant crop ring sell price to match the config/2
            if (e.NameWithoutLocale.IsEquivalentTo("Data/ObjectInformation"))
            {
                e.Edit(asset =>
                {
                    IAssetDataForDictionary<int, string> editor = asset.AsDictionary<int, string>();
                    if (editor.Data.TryGetValue(GiantCropRingID, out string val))
                    {
                        string[] valSplit = val.Split('/');
                        valSplit[2] = (this.config.cropRingPrice).ToString();
                        editor.Data[GiantCropRingID] = String.Join('/', valSplit);
                    }
                });
            }
        }

        /// <summary>Raised before the game ends the current day. This happens before it starts setting up the next day and before <see cref="IGameLoopEvents.Saving"/>.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            double chance = 0.0;

            this.totalNumberOfSeenTimeTicks = Math.Max(1, this.totalNumberOfSeenTimeTicks);
            this.numberOfTimeTicksWearingOneRing = Math.Max(1, this.numberOfTimeTicksWearingOneRing);
            this.numberOfTimeTicksWearingTwoRings = Math.Max(1, this.numberOfTimeTicksWearingTwoRings);

            if (this.numberOfTimeTicksWearingOneRing / (this.totalNumberOfSeenTimeTicks * 1.0) >= this.config.percentOfDayNeededToWearRingToTriggerEffect)
                chance = this.config.cropChancePercentWithRing;

            if (this.config.shouldWearingBothRingsDoublePercentage && this.numberOfTimeTicksWearingTwoRings / (this.totalNumberOfSeenTimeTicks * 1.0) >= this.config.percentOfDayNeededToWearRingToTriggerEffect)
                chance = 2 * this.config.cropChancePercentWithRing;

            if (chance > 0) {
                Monitor.Log("Rings worn enough, rolling dice to see if we should try growing a giant crop!");
                this.MaybeChangeCrops(chance, Game1.getFarm());
            }

            this.numberOfTimeTicksWearingOneRing = 0;
            this.numberOfTimeTicksWearingTwoRings = 0;
            this.totalNumberOfSeenTimeTicks = 0;
        }

        private void MaybeChangeCrops(double chance, GameLocation environment)
        {
            getGiantCropLists();

            foreach ((Vector2 loc, Crop crop) in this.GetValidCrops())
            {
                int xTile = (int)loc.X;
                int yTile = (int)loc.Y;

                double rand = new Random((int)Game1.uniqueIDForThisGame + (int)Game1.stats.DaysPlayed + xTile * 2000 +
                                      yTile).NextDouble();

                bool okCrop = true;
                if (crop.currentPhase.Value == crop.phaseDays.Count - 1 &&
                    canBeGiant(crop) && rand < chance)
                {
                    Monitor.Log("Trying to create giant crop!");
                    for (int index1 = xTile - 1; index1 <= xTile + 1; ++index1)
                    {
                        for (int index2 = yTile - 1; index2 <= yTile + 1; ++index2)
                        {
                            Vector2 key = new Vector2(index1, index2);
                            if (!environment.terrainFeatures.ContainsKey(key) ||
                                !(environment.terrainFeatures[key] is HoeDirt) ||
                                (environment.terrainFeatures[key] as HoeDirt).crop == null ||
                                (environment.terrainFeatures[key] as HoeDirt).crop.indexOfHarvest !=
                                crop.indexOfHarvest)
                            {
                                okCrop = false;

                                break;
                            }
                        }

                        if (!okCrop)
                            break;
                    }

                    if (!okCrop)
                        continue;

                    Monitor.Log("Creating giant crop!");
                    for (int index1 = xTile - 1; index1 <= xTile + 1; ++index1)
                        for (int index2 = yTile - 1; index2 <= yTile + 1; ++index2)
                        {
                            var index3 = new Vector2(index1, index2);
                            (environment.terrainFeatures[index3] as HoeDirt).crop = null;
                        }

                    (environment as Farm).resourceClumps.Add(new GiantCrop(crop.indexOfHarvest.Value,
                        new Vector2(xTile - 1, yTile - 1)));
                }
            }
        }

        private IEnumerable<(Vector2 location, Crop crop)> GetValidCrops()
        {
            return (
                from location in Game1.locations.OfType<Farm>()
                from feature in location.terrainFeatures.Pairs

                let tile = feature.Key
                let dirt = feature.Value as HoeDirt
                let crop = dirt?.crop
                where
                    crop != null
                    && dirt.state.Value == HoeDirt.watered
                    && !crop.dead.Value
                    && (
                        location.SeedsIgnoreSeasonsHere()
                        || crop.seasonsToGrowIn.Contains(location.GetSeasonForLocation())
                    )

                select (tile, crop)
            );
        }

        private void getGiantCropLists()
        {
            // JA crops with giant crops
            if (JA_API != null)
            {
                JsonAssetsGiantCrops = JA_API.GetGiantCropIndexes();
            }

            // More Giant Crops crops
            if (MoreGiantCropsAPI != null)
            {
                MoreGiantCropsGiantCrops = MoreGiantCropsAPI.RegisteredCrops();
            }

            // Giant Crop Tweaks crops
            if (GiantCropTweaksAPI != null)
            {
                GiantCropTweaksGiantCrops = GiantCropTweaksAPI.GiantCrops;
            }
        }

        private bool canBeGiant(Crop crop)
        {
            // Vanilla crops
            if (crop.indexOfHarvest.Value == 276 || crop.indexOfHarvest.Value == 190 || crop.indexOfHarvest.Value == 254)
            {
                return true;
            }

            // JA crops with giant crops
            if (JA_API != null)
            {
                if (JsonAssetsGiantCrops.Contains(crop.indexOfHarvest.Value))
                {
                    return true;
                }
            }

            // More Giant Crops crops
            if (MoreGiantCropsAPI != null)
            {
                if (MoreGiantCropsGiantCrops.Contains(crop.indexOfHarvest.Value))
                {
                    return true;
                }
            }

            // Giant Crop Tweaks crops
            if (GiantCropTweaksAPI != null)
            {
                if (GiantCropTweaksGiantCrops.ContainsKey(crop.indexOfHarvest.ToString()))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}
