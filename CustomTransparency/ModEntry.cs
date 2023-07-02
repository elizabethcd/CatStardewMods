using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using System.Reflection;

namespace CustomTransparency
{
    public class ModEntry : Mod
    {
        internal static ModConfig Config;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            var instance = new Harmony(this.Helper.ModRegistry.ModID);

            if (ValidateConfig(helper.ReadConfig<ModConfig>(), out Config))
            {
                helper.WriteConfig(Config);
            }

            instance.PatchAll(Assembly.GetExecutingAssembly());

            // Set up GMCM config when game is launched
            helper.Events.GameLoop.GameLaunched += SetUpConfig;
        }

        /// <summary>Validate the given config, resulting in a new config with values changed to the default if invalid.</summary>
        private static bool ValidateConfig(ModConfig config, out ModConfig validatedConfig)
        {
            bool changed = false;

            validatedConfig = new ModConfig();

            if (config.MinimumBuildingTransparency >= 0 && config.MinimumBuildingTransparency <= 1)
                validatedConfig.MinimumBuildingTransparency = config.MinimumBuildingTransparency;
            else changed = true;

            if (config.MinimumTreeTransparency >= 0 && config.MinimumTreeTransparency <= 1)
                validatedConfig.MinimumTreeTransparency = config.MinimumTreeTransparency;
            else changed = true;

            return changed;
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
