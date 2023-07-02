using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BetterArtisanGoodIcons.Extensions;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley.Objects;

namespace BetterArtisanGoodIcons
{
    /// <summary>Draws different icons for different Artisan Good types.</summary>
    /// <remarks>Honey does not save the original item in <see cref="StardewValley.Object.preservedParentSheetIndex"/> so we have to use its name to determine its type, resulting in
    /// honey and non-honey versions of things.</remarks>
    public class BetterArtisanGoodIconsMod : Mod
    {
        // Store config
        private BetterArtisanGoodIconsConfig Config;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            this.Config = helper.ReadConfig<BetterArtisanGoodIconsConfig>();

            ArtisanGoodsManager.Init(this.Helper, this.Monitor, this.Config);

            var harmony = new Harmony("cat.betterartisangoodicons");

            //Don't need to override draw for Object because artisan goods can't be placed down.
            Type objectType = typeof(StardewValley.Object);
            IList<Tuple<string, Type, Type>> replacements = new List<Tuple<string, Type, Type>>
            {
                {"drawWhenHeld", objectType, typeof(Patches.SObjectPatches.DrawWhenHeldPatch)},
                {"drawInMenu", objectType, typeof(Patches.SObjectPatches.DrawInMenuPatch)},
                {"draw", objectType, typeof(Patches.SObjectPatches.DrawPatch)},
                {"draw", typeof(Furniture), typeof(Patches.FurniturePatches.DrawPatch)}
            };

            foreach (Tuple<string, Type, Type> replacement in replacements)
            {
                MethodInfo original = replacement.Item2.GetMethods(BindingFlags.Instance | BindingFlags.Public).ToList().Find(m => m.Name == replacement.Item1);

                MethodInfo prefix = replacement.Item3.GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(item => item.Name == "Prefix");
                MethodInfo postfix = replacement.Item3.GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(item => item.Name == "Postfix");

                harmony.Patch(original, prefix == null ? null : new HarmonyMethod(prefix), postfix == null ? null : new HarmonyMethod(postfix));
            }

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
                reset: () => this.Config = new BetterArtisanGoodIconsConfig(),
                save: () => this.Helper.WriteConfig(this.Config));

            foreach (System.Reflection.PropertyInfo property in typeof(BetterArtisanGoodIconsConfig).GetProperties())
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
    }
}