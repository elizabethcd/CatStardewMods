using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BetterHay
{
    public class BetterHayMod : Mod
    {
        //Config
        public static ModConfig Config;

        private Random dropGrassStarterRandom;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            this.dropGrassStarterRandom = new Random();

            if (Config.EnableGettingHayFromGrassAnytime)
            {
                helper.Events.World.TerrainFeatureListChanged += this.OnTerrainFeatureListChanged;
            }

            var harmony = new Harmony(helper.ModRegistry.ModID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // Set up GMCM config when game is launched
            helper.Events.GameLoop.GameLaunched += SetUpConfig;
        }

        /// <summary>Raised when the game is launched in order to set up the GMCM config.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void SetUpConfig(object sender, GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
            {
                return;
            }

            // Register with GMCM
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(Config));

            foreach (System.Reflection.PropertyInfo property in typeof(ModConfig).GetProperties())
            {
                if (property.PropertyType.Equals(typeof(bool)))
                {
                    configMenu.AddBoolOption(
                        mod: ModManifest,
                        getValue: () => (bool)property.GetValue(Config),
                        setValue: value => property.SetValue(Config, value),
                        name: () => Helper.Translation.Get($"{property.Name}.title")
                       );
                }
                if (property.PropertyType.Equals(typeof(int)))
                {
                    configMenu.AddNumberOption(
                        mod: ModManifest,
                        getValue: () => (int)property.GetValue(Config),
                        setValue: value => property.SetValue(Config, value),
                        name: () => Helper.Translation.Get($"{property.Name}.title")
                       );
                }
                if (property.PropertyType.Equals(typeof(double)))
                {
                    configMenu.AddNumberOption(
                        mod: ModManifest,
                        getValue: () => (float)(double)property.GetValue(Config),
                        setValue: value => property.SetValue(Config, (double)value),
                        name: () => Helper.Translation.Get($"{property.Name}.title"),
                        tooltip: null,
                        min: 0f,
                        max: 1f,
                        interval: 0.01f
                       );
                }
                if (property.PropertyType.Equals(typeof(KeybindList)))
                {
                    configMenu.AddKeybindList(
                        mod: ModManifest,
                        getValue: () => (KeybindList)property.GetValue(Config),
                        setValue: value => property.SetValue(Config, value),
                        name: () => Helper.Translation.Get($"{property.Name}.title")
                        );
                }
                if (property.PropertyType.Equals(typeof(SButton)))
                {
                    configMenu.AddKeybind(
                        mod: ModManifest,
                        getValue: () => (SButton)property.GetValue(Config),
                        setValue: value => property.SetValue(Config, value),
                        name: () => Helper.Translation.Get($"{property.Name}.title")
                        );
                }
            }
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnTerrainFeatureListChanged(object sender, TerrainFeatureListChangedEventArgs e)
        {
            // check for removed grass and spawn hay if appropriate
            if (Config.EnableGettingHayFromGrassAnytime)
            {
                foreach (KeyValuePair<Vector2, TerrainFeature> item in e.Removed)
                {
                    if (item.Value is Grass grass && grass.numberOfWeeds.Value <= 0 && grass.grassType.Value == 1)
                    {
                        if ((Game1.IsMultiplayer
                                ? Game1.recentMultiplayerRandom
                                : new Random((int)(Game1.uniqueIDForThisGame + item.Key.X * 1000.0 + item.Key.Y * 11.0)))
                            .NextDouble() < 0.5)
                        {
                            if (Game1.player.CurrentTool is MeleeWeapon && (Game1.player.CurrentTool.Name.Contains("Scythe") || Game1.player.CurrentTool.ParentSheetIndex == 47))
                            {
                                if (this.IsWithinRange(Game1.player.getTileLocation(), item.Key, 3))
                                {
                                    if (this.dropGrassStarterRandom.NextDouble() < Config.ChanceToDropGrassStarterInsteadOfHay)
                                        this.AttemptToGiveGrassStarter(item.Key, Game1.getFarm().piecesOfHay.Value == Utility.numSilos() * 240);
                                    else if (Game1.getFarm().tryToAddHay(1) != 0)
                                    {
                                        if (!BetterHayGrass.TryAddItemToInventory(178) && Config.DropHayOnGroundIfNoRoomInInventory)
                                            BetterHayGrass.DropOnGround(item.Key, 178);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        //Correctly give grass starters instead of hay when silos are full and not full
        private void AttemptToGiveGrassStarter(Vector2 location, bool silosAreFull)
        {
            bool added = BetterHayGrass.TryAddItemToInventory(297);
            if (!added && Config.DropHayOnGroundIfNoRoomInInventory)
            {
                BetterHayGrass.DropOnGround(location, 297);
                added = true;
            }

            if (!silosAreFull && added)
                Game1.getFarm().piecesOfHay.Value -= 1;
        }

        //Returns whether the first vector is with range of the second, in euclidian distance
        private bool IsWithinRange(Vector2 first, Vector2 second, double range)
        {
            return Math.Sqrt(Math.Pow(first.X - second.X, 2) + Math.Pow(first.Y - second.Y, 2)) < range;
        }
    }
}
