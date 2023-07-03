using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.TerrainFeatures;
using System.Linq;
using System.Reflection;
using WinterGrass.LegacySaving;

namespace WinterGrass
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        /*********
        ** Fields
        *********/
        private const int grassStarterItemId = 297;

        /// <summary>The mod instance.</summary>
        internal static ModEntry Instance { get; private set; }

        /// <summary>The mod configuration.</summary>
        internal ModConfig Config;

        /// <summary>The legacy save handler.</summary>
        private LegacySaveConverter legacySaveConverter;

        /*********
        ** Public methods
        *********/

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            ModEntry.Instance = this;
            this.Config = helper.ReadConfig<ModConfig>();

            this.legacySaveConverter = new LegacySaveConverter(this.Helper.DirectoryPath);

            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            helper.Events.GameLoop.SaveLoaded += this.GameLoop_SaveLoaded;
            helper.Events.GameLoop.Saved += this.GameLoop_Saved;
            helper.Events.GameLoop.DayStarted += this.GameLoop_DayStarted;
            helper.Events.Player.InventoryChanged += this.Player_InventoryChanged;

            // Set up GMCM config when game is launched
            helper.Events.GameLoop.GameLaunched += SetUpConfig;
        }

        /*********
        ** Private methods
        *********/

        /// <summary>Raised after the game finishes writing data to the save file (except the initial save creation).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void GameLoop_Saved(object sender, SavedEventArgs e)
        {
            this.legacySaveConverter.DeleteSaveFile();
        }

        /// <summary>Raised after the game begins a new day (including when the player loads a save).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
        {
            if (Game1.IsWinter)
            {
                this.FixGrassColor();
            }
        }

        /// <summary>Raised after the player loads a save slot.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (!Context.IsMainPlayer)
            {
                return;
            }

            this.legacySaveConverter.SetSaveFilePath();

            if (Game1.IsWinter)
            {
                this.legacySaveConverter.AddGrassFromLegacySaveFile();
                this.FixGrassColor();
            }
        }

        /// <summary>Raised after items are added or removed to a player's inventory.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void Player_InventoryChanged(object sender, InventoryChangedEventArgs e)
        {
            // After the user places down a grass starter, fix the color of the newly placed grass
            if (e.IsLocalPlayer && Game1.GetSeasonForLocation(Game1.currentLocation) == "winter" && (e.Removed.Any(item => item.ParentSheetIndex == grassStarterItemId) || e.QuantityChanged.Any(change => change.Item.ParentSheetIndex == grassStarterItemId)))
            {
                this.FixGrassColor();
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
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
                );

            foreach (System.Reflection.PropertyInfo property in typeof(ModConfig).GetProperties())
            {
                if (property.PropertyType.Equals(typeof(bool)))
                {
                    configMenu.AddBoolOption(
                        mod: ModManifest,
                        getValue: () => (bool)property.GetValue(this.Config),
                        setValue: value => property.SetValue(this.Config, value),
                        name: () => Helper.Translation.Get($"{property.Name}.title")
                       );
                }
                if (property.PropertyType.Equals(typeof(int)))
                {
                    configMenu.AddNumberOption(
                        mod: ModManifest,
                        getValue: () => (int)property.GetValue(this.Config),
                        setValue: value => property.SetValue(this.Config, value),
                        name: () => Helper.Translation.Get($"{property.Name}.title")
                       );
                }
                if (property.PropertyType.Equals(typeof(double)))
                {
                    configMenu.AddNumberOption(
                        mod: ModManifest,
                        getValue: () => (float)property.GetValue(this.Config),
                        setValue: value => property.SetValue(this.Config, (double)value),
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
                        getValue: () => (float)property.GetValue(this.Config),
                        setValue: value => property.SetValue(this.Config, value),
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
                        getValue: () => (KeybindList)property.GetValue(this.Config),
                        setValue: value => property.SetValue(this.Config, value),
                        name: () => Helper.Translation.Get($"{property.Name}.title")
                        );
                }
                if (property.PropertyType.Equals(typeof(SButton)))
                {
                    configMenu.AddKeybind(
                        mod: ModManifest,
                        getValue: () => (SButton)property.GetValue(this.Config),
                        setValue: value => property.SetValue(this.Config, value),
                        name: () => Helper.Translation.Get($"{property.Name}.title")
                        );
                }
            }
        }

        /// <summary>Changes the color of every piece of grass to be snowy</summary>
        private void FixGrassColor()
        {
            if (!Context.IsMainPlayer)
            {
                return;
            }

            foreach (Grass grass in Game1.locations.Where(loc => loc != null).SelectMany(loc => loc.terrainFeatures.Pairs).Select(item => item.Value).OfType<Grass>())
            {
                grass.grassSourceOffset.Value = 80;
            }
        }
    }
}
