﻿using ChatCommands.ClassReplacements;
using ChatCommands.Commands;
using ChatCommands.Util;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Collections.Specialized.BitVector32;
using System.Reflection;

namespace ChatCommands
{
    public class ChatCommandsMod : Mod, ICommandHandler
    {
        private const int BaseWaitPeriod = 15;
        //TODO: MOUSE TO CLICK/DRAG SCROLL

        private CommandValidator commandValidator;
        private NotifyingTextWriter consoleNotifier;
        private InputState inputState;
        private ChatCommandsConfig modConfig;

        private int repeatWaitPeriod = BaseWaitPeriod;

        private bool wasEscapeDown;

        /// <summary>Get whether this <see cref="ICommandHandler" /> can handle the given input.</summary>
        public bool CanHandle(string input)
        {
            return input.Length > 1 && this.commandValidator.IsValidCommand(input.Substring(1));
        }

        /// <summary>Handle the given input.</summary>
        /// <param name="input">The input to handle.</param>
        public void Handle(string input)
        {
            input = input.Substring(1);
            string[] parts = Utils.ParseArgs(input);

            if (parts[0] == "halp")
                parts[0] = "help";

            this.consoleNotifier.Notify = true;

            // Replace previous console commands trigger code with new, Pathos-written fancy stuff
            ICommandHelper commandHelper = Helper.ConsoleCommands;
            object commandManager = commandHelper.GetType().GetField("CommandManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(commandHelper);
            if (commandManager is null)
                throw new InvalidOperationException("Can't get SMAPI's underlying command manager.");

            MethodInfo triggerCommand = commandManager.GetType().GetMethod("Trigger", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (triggerCommand is null)
                throw new InvalidOperationException("Can't get SMAPI's underlying CommandManager.Trigger method.");

            triggerCommand.Invoke(commandManager, new object[] { parts[0], parts.Skip(1).ToArray() });

            this.consoleNotifier.Notify = false;
        }

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            this.commandValidator = new CommandValidator(helper.ConsoleCommands);
            this.consoleNotifier = new NotifyingTextWriter(Console.Out, this.OnLineWritten);

            this.inputState = helper.Reflection.GetField<InputState>(typeof(Game1), "input").GetValue();

            Console.SetOut(this.consoleNotifier);
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;

            this.modConfig = helper.ReadConfig<ChatCommandsConfig>();
            if (this.modConfig.ColorOverrides == null)
            {
                this.modConfig.ColorOverrides = new Dictionary<string, string>();
            }

            Utils.Validate(this.modConfig.ColorOverrides, this.Monitor);

            IEnumerable<ICommand> commands = new ICommand[]
            {
                new ListenCommand(this.Monitor, this.modConfig, this.consoleNotifier),
                new WhisperCommand(this.Monitor),
                new ReplyCommand(this.Monitor)
            };

            foreach (ICommand command in commands)
                command.Register(helper.ConsoleCommands);
        }

        /// <summary>Raised after the game returns to the title screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            (Game1.chatBox as CommandChatBox)?.ClearHistory();
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            // escape console
            if (this.Helper.Input.IsDown(SButton.Escape) && !this.wasEscapeDown)
            {
                this.wasEscapeDown = !this.wasEscapeDown;
                (Game1.chatBox as CommandChatBox)?.EscapeStatusChanged(this.wasEscapeDown);
            }

            // resend the direction keys if held
            if (e.IsMultipleOf(2) && Game1.chatBox != null && Game1.chatBox.isActive())
            {
                bool isLeftDown = this.Helper.Input.IsDown(SButton.Left);
                bool isRightDown = this.Helper.Input.IsDown(SButton.Right);
                bool isUpDown = this.Helper.Input.IsDown(SButton.Up);
                bool isDownDown = this.Helper.Input.IsDown(SButton.Down);

                if (isLeftDown ^ isRightDown ^ isUpDown ^ isDownDown)
                {
                    SButton heldKey = isLeftDown
                        ? SButton.Left
                        : (isRightDown ? SButton.Right : (isUpDown ? SButton.Up : SButton.Down));

                    if (this.repeatWaitPeriod != 0)
                        this.repeatWaitPeriod--;

                    if (this.repeatWaitPeriod == 0)
                    {
                        Game1.chatBox.receiveKeyPress((Keys)heldKey);
                        if (isUpDown || isDownDown)
                            this.repeatWaitPeriod = BaseWaitPeriod;
                    }
                }
                else
                {
                    this.repeatWaitPeriod = BaseWaitPeriod;
                }
            }
        }

        /// <summary>Raised after the player loads a save slot.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            // replace the game's chatbox
            if (Game1.chatBox != null && Game1.chatBox is CommandChatBox) return;
            if (Game1.chatBox != null)
                Game1.onScreenMenus.Remove(Game1.chatBox);
            Game1.chatBox = new CommandChatBox(this.Helper, this, this.modConfig);
            Game1.onScreenMenus.Add(Game1.chatBox);
            this.Monitor.Log("Replaced Chatbox", LogLevel.Trace);
        }

        /// <summary>When a line is written to the console, add it to the chatbox.</summary>
        private void OnLineWritten(char[] buffer, int index, int count)
        {
            string toWrite = string.Join("", buffer.Skip(index).Take(count)).Trim();
            if (toWrite.Length > 0 && toWrite[0] == 0x200b)
            {
                toWrite = toWrite.Substring(1);
            }

            string noPrefix = Utils.StripSMAPIPrefix(toWrite).Trim();

            if (Utils.ShouldIgnore(noPrefix))
                return;

            if (this.modConfig.RemoveSMAPIMessagePrefix && !this.consoleNotifier.ForceNotify)
                toWrite = noPrefix;

            if (!string.IsNullOrWhiteSpace(toWrite))
                (Game1.chatBox as CommandChatBox)?.AddConsoleMessage(toWrite, Utils.ConvertConsoleColorToColor(Console.ForegroundColor, this.modConfig.ColorOverrides));
        }
    }
}
