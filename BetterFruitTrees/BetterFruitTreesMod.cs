using BetterFruitTrees.Patches;
using BetterFruitTrees.Patches.JunimoHarvester;
using BetterFruitTrees.Patches.JunimoHut;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static BetterFruitTrees.Extensions.ListExtensions;
using SObject = StardewValley.Object;

namespace BetterFruitTrees
{
    public class BetterFruitTreesMod : Mod
    {
        internal static BetterFruitTreesMod Instance;

        internal BetterFruitTreesConfig Config;

        public override void Entry(IModHelper helper)
        {
            Utils.Reflection = helper.Reflection;
            if (helper.ModRegistry.IsLoaded("cat.fruittreesanywhere"))
            {
                this.Monitor.Log("You have both this mod, and the old version ('Fruit Trees Anywhere') installed!", LogLevel.Error);
                this.Monitor.Log("In order for this mod to work properly, you need to delete the FruitTreesAnywhere folder!", LogLevel.Error);
                this.Monitor.Log("This mod does everything the old version does and fruit tree junimo harvesting, so please delete FruitTreesAnywhere!", LogLevel.Error);
                helper.Events.GameLoop.SaveLoaded += this.ShowErrorMessage;
                return;
            }

            Instance = this;

            this.Config = helper.ReadConfig<BetterFruitTreesConfig>();
            Utils.Config = this.Config;

            new GrowHelper(helper.Events);

            var harmony = new Harmony("cat.betterfruittrees");

            IList<Tuple<string, Type, Type>> replacements = new List<Tuple<string, Type, Type>>
            {
                { nameof(SObject.placementAction), typeof(SObject), typeof(PlacementPatch)},
                { nameof(Multiplayer.broadcastGlobalMessage), typeof(Multiplayer), typeof (PlacementWarningPatch) }
            };

            Type junimoHarvesterType = typeof(JunimoHarvester);
            IList<Tuple<string, Type, Type>> junimoReplacements = new List<Tuple<string, Type, Type>>
            {
                { nameof(JunimoHarvester.tryToHarvestHere), junimoHarvesterType, typeof(TryToHarvestHerePatch) },
                { nameof(JunimoHarvester.update), junimoHarvesterType, typeof(UpdatePatch) },
                { "areThereMatureCropsWithinRadius", typeof(JunimoHut), typeof(AreThereMatureCropsWithinRadiusPatch) }
            };

            foreach (Tuple<string, Type, Type> item in junimoReplacements)
                replacements.Add(item);

            foreach (Tuple<string, Type, Type> replacement in replacements)
            {
                MethodInfo original = replacement.Item2
                    .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public).ToList()
                    .Find(m => m.Name == replacement.Item1);

                MethodInfo prefix = replacement.Item3.GetMethods(BindingFlags.Static | BindingFlags.Public)
                    .FirstOrDefault(item => item.Name == "Prefix");
                MethodInfo postfix = replacement.Item3.GetMethods(BindingFlags.Static | BindingFlags.Public)
                    .FirstOrDefault(item => item.Name == "Postfix");

                harmony.Patch(original, prefix == null ? null : new HarmonyMethod(prefix),
                    postfix == null ? null : new HarmonyMethod(postfix));
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
                reset: () => this.Config = new BetterFruitTreesConfig(),
                save: () => this.Helper.WriteConfig(this.Config));

            foreach (System.Reflection.PropertyInfo property in typeof(BetterFruitTreesConfig).GetProperties())
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

        private void ShowErrorMessage(object sender, EventArgs e)
        {
            Game1.showRedMessage("Better Fruit Trees failed to load - please see the console for how to fix this.");
        }
    }
}
