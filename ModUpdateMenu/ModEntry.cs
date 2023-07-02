using ModUpdateMenu.Menus;
using ModUpdateMenu.Updates;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ModUpdateMenu
{
    public class ModEntry : Mod
    {
        /*********
        ** Fields
        *********/
        /// <summary>The update button.</summary>
        private UpdateButton button;

        /// <summary>The update menu.</summary>
        private UpdateMenu menu;

        /// <summary>The mod configuration.</summary>
        private ModConfig config;

        /// <summary>The current mod statuses.</summary>
        private IList<ModStatus> currentStatuses;


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            this.config = this.Helper.ReadConfig<ModConfig>();
            this.button = new UpdateButton(helper);

            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.Display.Rendered += this.OnRendered;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;

            // Set up GMCM config when game is launched
            helper.Events.GameLoop.GameLaunched += SetUpConfig;
        }

        /// <summary>Notifies about the SMAPI update version.</summary>
        /// <param name="version">The SMAPI update version.</param>
        internal void NotifySMAPI(ISemanticVersion version)
        {
            //Debug Info
            this.Monitor.Log($"SMAPI: {version}");
            this.menu.NotifySMAPI(version);
            this.button.NotifySMAPI(version);
        }

        /// <summary>Notifies about mod statuses.</summary>
        /// <param name="statuses">The mod statuses.</param>
        internal void Notify(IList<ModStatus> statuses)
        {
            //Debug Info
            /*if(statuses != null)
                foreach(ModStatus status in statuses.OrderByDescending(item => item.UpdateStatus))
                    this.Monitor.Log($"{status.UpdateStatus} {status.CurrentVersion} {status.NewVersion} {status.UpdateURL} {status.ErrorReason} {status.ModName}");
            else
                this.Monitor.Log("Statuses are null");*/

            if (this.config.HideSkippedMods)
            {
                statuses = statuses?.Where(status => status.UpdateStatus != UpdateStatus.Skipped).ToList();
            }

            this.menu.Notify(statuses);
            this.button.Notify(statuses);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Raised after the game is launched, right before the first update tick. </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            this.menu = new UpdateMenu();
            new Thread(() =>
            {
                UpdateStatusRetriever statusRetriever = new UpdateStatusRetriever(this.Helper);
                int attempts = 50;
                while (true)
                {
                    Thread.Sleep(1000);
                    try
                    {
                        if (statusRetriever.GetUpdateStatuses(out IList<ModStatus> statuses))
                        {
                            if (this.currentStatuses != null && this.currentStatuses.Count == statuses.Count)
                            {
                                this.Notify(statuses);

                                try
                                {
                                    this.NotifySMAPI(statusRetriever.GetSMAPIUpdateVersion());
                                }
                                catch
                                {
                                    this.NotifySMAPI(null);
                                }

                                break;
                            }
                            else
                            {
                                this.currentStatuses = statuses;
                            }
                        }

                        attempts--;
                        if (attempts == 0)
                        {
                            throw new Exception("All update attempts failed.");
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Monitor.Log("Failed retrieving update info from SMAPI: ");
                        this.Monitor.Log(ex.ToString());
                        this.Notify(null);
                        this.NotifySMAPI(null);
                        break;
                    }
                }
            }).Start();
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
                reset: () => this.config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.config),
                titleScreenOnly: true);

            foreach (System.Reflection.PropertyInfo property in typeof(ModConfig).GetProperties())
            {
                if (property.PropertyType.Equals(typeof(bool)))
                {
                    configMenu.AddBoolOption(
                        mod: ModManifest,
                        getValue: () => (bool)property.GetValue(this.config),
                        setValue: value => property.SetValue(this.config, value),
                        name: () => Helper.Translation.Get($"{property.Name}.title")
                       );
                }
                if (property.PropertyType.Equals(typeof(int)))
                {
                    configMenu.AddNumberOption(
                        mod: ModManifest,
                        getValue: () => (int)property.GetValue(this.config),
                        setValue: value => property.SetValue(this.config, value),
                        name: () => Helper.Translation.Get($"{property.Name}.title")
                       );
                }
                if (property.PropertyType.Equals(typeof(double)))
                {
                    configMenu.AddNumberOption(
                        mod: ModManifest,
                        getValue: () => (float)property.GetValue(this.config),
                        setValue: value => property.SetValue(this.config, (double)value),
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
                        getValue: () => (float)property.GetValue(this.config),
                        setValue: value => property.SetValue(this.config, value),
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
                        getValue: () => (KeybindList)property.GetValue(this.config),
                        setValue: value => property.SetValue(this.config, value),
                        name: () => Helper.Translation.Get($"{property.Name}.title")
                        );
                }
                if (property.PropertyType.Equals(typeof(SButton)))
                {
                    configMenu.AddKeybind(
                        mod: ModManifest,
                        getValue: () => (SButton)property.GetValue(this.config),
                        setValue: value => property.SetValue(this.config, value),
                        name: () => Helper.Translation.Get($"{property.Name}.title")
                        );
                }
            }
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (e.Button != SButton.MouseLeft)
            {
                return;
            }

            if (this.button.PointContainsButton(e.Cursor.ScreenPixels) && Game1.activeClickableMenu is TitleMenu && TitleMenu.subMenu == null)
            {
                TitleMenu.subMenu = this.menu;
                this.menu.Activated();
                Game1.playSound("newArtifact");
            }
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            this.button.ShowUpdateButton = this.ShouldDrawUpdateButton();
        }

        /// <summary>Raised after the game draws to the sprite patch in a draw tick, just before the final sprite batch is rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRendered(object sender, RenderedEventArgs e)
        {
            if (Game1.activeClickableMenu is TitleMenu && this.ShouldDrawUpdateButton())
            {
                this.button.Draw(e.SpriteBatch);
            }
        }

        /// <summary>Whether the update button should be drawn.</summary>
        /// <returns>Whether the update button should be drawn.</returns>
        private bool ShouldDrawUpdateButton()
        {
            if (!(Game1.activeClickableMenu is TitleMenu titleMenu))
            {
                return false;
            }

            return TitleMenu.subMenu == null && !this.GetPrivateBool(titleMenu, "isTransitioningButtons") &&
                   (this.GetPrivateBool(titleMenu, "titleInPosition") && !this.GetPrivateBool(titleMenu, "transitioningCharacterCreationMenu"));
        }

        /// <summary>Gets a private boolean using reflection.</summary>
        /// <param name="obj">The object to get the boolean from.</param>
        /// <param name="name">The name of the boolean.</param>
        /// <returns>The boolean value.</returns>
        private bool GetPrivateBool(object obj, string name)
        {
            return this.Helper.Reflection.GetField<bool>(obj, name).GetValue();
        }
    }
}
