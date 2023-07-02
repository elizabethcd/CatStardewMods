using BetterGardenPots.Extensions;
using BetterGardenPots.Patches.IndoorPot;
using BetterGardenPots.Patches.Utility;
using BetterGardenPots.Subscribers;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BetterGardenPots
{
    public class BetterGardenPotsMod : Mod
    {
        private readonly IList<IEventSubscriber> subscribers = new List<IEventSubscriber>();
        private BetterGardenPotsModConfig Config;
        private Harmony StoredHarmony;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            var harmony = new Harmony("cat.bettergardenpots");
            StoredHarmony = harmony;

            Config = helper.ReadConfig<BetterGardenPotsModConfig>();

            helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;

            // Set up GMCM config when game is launched
            helper.Events.GameLoop.GameLaunched += SetUpConfig;
        }

        /// <summary>Raised after the player loads a save slot.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaveLoaded(object sender, EventArgs e)
        {
            if (Config.MakeSprinklersWaterGardenPots) this.subscribers.Add(new GardenPotSprinklerHandler(this.Helper));

            ApplyPatches(StoredHarmony, Config);

            if (Context.IsMainPlayer)
            {
                foreach (IEventSubscriber subscriber in this.subscribers)
                    subscriber.Subscribe();
            }
        }

        /// <summary>Raised after the game returns to the title screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            foreach (IEventSubscriber subscriber in this.subscribers)
                subscriber.Unsubscribe();
        }

        private void ApplyPatches(Harmony harmony, BetterGardenPotsModConfig config)
        {
            Type indoorPotType = typeof(IndoorPot);

            IList<Tuple<string, Type, Type>> replacements = new List<Tuple<string, Type, Type>>();

            if (config.HarvestMatureCropsWhenGardenPotBreaks)
                replacements.Add(nameof(IndoorPot.performToolAction), indoorPotType, typeof(PerformToolActionPatch));

            if (config.AllowPlantingAncientSeedsInGardenPots)
                replacements.Add(nameof(IndoorPot.performObjectDropInAction), indoorPotType, typeof(PerformObjectDropInActionPatchFruit));

            if (config.AllowCropsToGrowInAnySeasonOutsideWhenInGardenPot)
            {
                replacements.Add(nameof(IndoorPot.DayUpdate), indoorPotType, typeof(DayUpdatePatch));
                replacements.Add(nameof(IndoorPot.performObjectDropInAction), indoorPotType, typeof(PerformObjectDropInActionPatchSeasons));
            }

            foreach (Tuple<string, Type, Type> replacement in replacements)
            {
                MethodInfo original = replacement.Item2.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public).ToList().Find(m => m.Name == replacement.Item1);

                MethodInfo prefix = replacement.Item3.GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(item => item.Name == "Prefix");
                MethodInfo postfix = replacement.Item3.GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(item => item.Name == "Postfix");

                harmony.Patch(original, prefix == null ? null : new HarmonyMethod(prefix), postfix == null ? null : new HarmonyMethod(postfix));
            }

            // Patch the beehouse function seperately to get the right overload out of it
            if (config.MakeBeeHousesNoticeFlowersInGardenPots)
            {
                harmony.Patch(
                    original: AccessTools.Method(typeof(Utility), nameof(Utility.findCloseFlower), new Type[] { typeof(GameLocation), typeof(Vector2), typeof(int), typeof(Func<Crop, bool>) }),
                    prefix: new HarmonyMethod(typeof(FindCloseFlowerPatch), nameof(FindCloseFlowerPatch.Prefix))
                );
            }
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
                reset: () => this.Config = new BetterGardenPotsModConfig(),
                save: () => this.Helper.WriteConfig(this.Config),
                titleScreenOnly: true);

            foreach (System.Reflection.PropertyInfo property in typeof(BetterGardenPotsModConfig).GetProperties())
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
