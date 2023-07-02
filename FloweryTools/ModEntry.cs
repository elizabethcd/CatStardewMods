using FloweryTools.Framework.Creators;
using FloweryTools.Framework.Flowerers;
using FloweryTools.ParticleCreator;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using System.Collections.Generic;
using System.Reflection;

namespace FloweryTools
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private int lastToolPower;
        private bool lastUsingSlingshot;
        private bool lastcastedButBobberStillInAir;

        private IList<IToolFlowerer> flowerers;

        private IParticleCreator explosionCreator;
        private IParticleCreator slingshotCreator;

        private ModConfig Config;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            this.Config = this.Helper.ReadConfig<ModConfig>();

            Multiplayer multiplayer = typeof(Game1).GetField("multiplayer", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) as Multiplayer;

            FlowerHelper flowerHelper = new FlowerHelper(multiplayer, this.Config);

            flowerers = new List<IToolFlowerer> {
                new Swipe(flowerHelper),
                new Swing(flowerHelper),
                new Stab(flowerHelper),
                new Defense(flowerHelper),
                new Watering(flowerHelper),
                new TimingCast(flowerHelper)
            };

            explosionCreator = new Explosion(flowerHelper);
            slingshotCreator = new Slingshot(flowerHelper);

            helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;

            // Set up GMCM config when game is launched
            helper.Events.GameLoop.GameLaunched += SetUpConfig;
        }

        /*********
        ** Private methods
        *********/
        private void GameLoop_UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            // Ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            // Handle all normal animation based tools
            foreach (IToolFlowerer flowerer in this.flowerers) {
                if (flowerer.Matches(Game1.player.FarmerSprite.timer, Game1.player.FarmerSprite.CurrentAnimation))
                {
                    flowerer.CreateParticles(Game1.player.currentLocation, Game1.player.FarmerSprite.currentAnimationIndex);
                }
            }

            // I like having flowers burst out when charge level changes, but this doesn't happen through animations like above.
            if (lastToolPower != Game1.player.toolPower && !(lastToolPower == 0 && Game1.player.toolPower == 3))
            {
                this.explosionCreator.CreateParticles(Game1.player.currentLocation, 0);
            }
            this.lastToolPower = Game1.player.toolPower;

            // Same for slingshots.
            if (!Game1.player.usingSlingshot && lastUsingSlingshot)
            {
                this.slingshotCreator.CreateParticles(Game1.player.currentLocation, 0);
            }
            this.lastUsingSlingshot = Game1.player.usingSlingshot;

            // Same for the bobber landing.
            if (Game1.player.CurrentTool is StardewValley.Tools.FishingRod rod)
            {
                if (!rod.castedButBobberStillInAir && lastcastedButBobberStillInAir)
                    this.explosionCreator.CreateParticles(Game1.player.currentLocation, 0);

                this.lastcastedButBobberStillInAir = rod.castedButBobberStillInAir;
            } else
            {
                this.lastcastedButBobberStillInAir = false;
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
                        getValue: () => (float)property.GetValue(Config),
                        setValue: value => property.SetValue(Config, (double)value),
                        name: () => Helper.Translation.Get($"{property.Name}.title"),
                        tooltip: null,
                        min: 0f,
                        max: 1f,
                        interval: 0.01f
                       );
                }
                if (property.PropertyType.Equals(typeof(float)))
                {
                    configMenu.AddNumberOption(
                        mod: ModManifest,
                        getValue: () => (float)property.GetValue(Config),
                        setValue: value => property.SetValue(Config, value),
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
    }
}