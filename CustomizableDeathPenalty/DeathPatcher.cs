using System;
using StardewValley;
using HarmonyLib;
using StardewModdingAPI;
using System.Linq;
using System.Collections.Generic;
using StardewValley.Tools;
using StardewValley.Objects;
using StardewValley.Menus;

namespace CustomizableDeathPenalty
{
    internal class DeathPatcher
    {
        private static IMonitor Monitor;
        private static ModConfig Config;

        public static void Initialize(IMonitor monitor, ModConfig config)
        {
            Monitor = monitor;
            Config = config;
        }

        public static void Apply(Harmony harmony)
        {
            try
            {
                Monitor.Log("Applying Harmony patch to postfix command_minedeath and command_hospitaldeath in Event.cs", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(Event), nameof(Event.command_minedeath)),
                    postfix: new HarmonyMethod(typeof(DeathPatcher), nameof(DeathPatcher.Event_command_minedeath_Postfix))
                );
                harmony.Patch(
                    original: AccessTools.Method(typeof(Event), nameof(Event.command_hospitaldeath)),
                    postfix: new HarmonyMethod(typeof(DeathPatcher), nameof(DeathPatcher.Event_command_hospitaldeath_Postfix))
                );
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to add postfix to after death event function with exception: {ex}", LogLevel.Error);
            }
        }

        private static void Event_command_minedeath_Postfix(Event __instance)
        {
            int moneyToLose = calculateLossesMines().Item1;
            int numberOfItemsLost = calculateLossesMines().Item2;

            // Do nothing if you wouldn't have lost anything
            if ((moneyToLose == 0 && numberOfItemsLost == 0) || (!Config.KeepItems && !Config.KeepMoney))
            {
                return;
            }

            // Change to match what's going to happen in reality
            if (Config.KeepMoney)
            {
                moneyToLose = 0;
            }
            if (Config.KeepItems)
            {
                numberOfItemsLost = 0;
            }

            Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1057") + " " + ((moneyToLose <= 0) ? "" : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1058", moneyToLose)) + ((numberOfItemsLost <= 0) ? ((moneyToLose <= 0) ? "" : ".") : ((moneyToLose <= 0) ? (Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1060") + ((numberOfItemsLost == 1) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1061") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1062", numberOfItemsLost))) : (Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1063") + ((numberOfItemsLost == 1) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1061") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1062", numberOfItemsLost))))));

            if (numberOfItemsLost > 0)
            {
                Game1.activeClickableMenu = new ItemListMenu(Game1.content.LoadString("Strings\\UI:ItemList_ItemsLost"), Game1.player.itemsLostLastDeath.ToList());
            }
            __instance.CurrentCommand++;
        }

        private static void Event_command_hospitaldeath_Postfix(Event __instance)
        {
            int moneyToLose = calculateLossesHospital().Item1;
            int numberOfItemsLost = calculateLossesHospital().Item2;

            // Do nothing if you wouldn't have lost anything
            if (moneyToLose == 0 && numberOfItemsLost == 0)
            {
                return;
            }

            // Change to match what's going to happen in reality
            if (Config.KeepMoney)
            {
                moneyToLose = 0;
            }
            if (Config.KeepItems)
            {
                numberOfItemsLost = 0;
            }

            Game1.drawObjectDialogue(((moneyToLose > 0) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1068", moneyToLose) : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1070")) + ((numberOfItemsLost > 0) ? (Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1071") + ((numberOfItemsLost == 1) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1061") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1062", numberOfItemsLost))) : ""));

            if (numberOfItemsLost > 0)
            {
                Game1.activeClickableMenu = new ItemListMenu(Game1.content.LoadString("Strings\\UI:ItemList_ItemsLost"), Game1.player.itemsLostLastDeath.ToList());
            }
            __instance.CurrentCommand++;
        }

        private static (int,int) calculateLossesMines()
        {
            Random r = new Random((int)Game1.uniqueIDForThisGame / 2 + (int)Game1.stats.DaysPlayed + Game1.timeOfDay);
            int moneyToLose = r.Next(Game1.player.Money / 20, Game1.player.Money / 4);
            moneyToLose = Math.Min(moneyToLose, 5000);
            moneyToLose -= (int)((double)Game1.player.LuckLevel * 0.01 * (double)moneyToLose);
            moneyToLose -= moneyToLose % 100;
            int numberOfItemsLost = 0;
            double itemLossRate = 0.25 - (double)Game1.player.LuckLevel * 0.05 - Game1.player.DailyLuck;
            for (int i = Game1.player.Items.Count - 1; i >= 0; i--)
            {
                if (Game1.player.Items[i] != null && (!(Game1.player.Items[i] is Tool) || (Game1.player.Items[i] is MeleeWeapon && (Game1.player.Items[i] as MeleeWeapon).InitialParentTileIndex != 47 && (Game1.player.Items[i] as MeleeWeapon).InitialParentTileIndex != 4)) && Game1.player.Items[i].canBeTrashed() && !(Game1.player.Items[i] is Ring) && r.NextDouble() < itemLossRate)
                {
                    numberOfItemsLost++;
                }
            }
            return (moneyToLose, numberOfItemsLost);
        }

        private static (int,int) calculateLossesHospital()
        {
            Random r = new Random((int)Game1.uniqueIDForThisGame / 2 + (int)Game1.stats.DaysPlayed + Game1.timeOfDay);
            int numberOfItemsLost = 0;
            double itemLossRate = 0.25 - (double)Game1.player.LuckLevel * 0.05 - Game1.player.DailyLuck;
            for (int i = Game1.player.Items.Count - 1; i >= 0; i--)
            {
                if (Game1.player.Items[i] != null && (!(Game1.player.Items[i] is Tool) || (Game1.player.Items[i] is MeleeWeapon && (Game1.player.Items[i] as MeleeWeapon).InitialParentTileIndex != 47 && (Game1.player.Items[i] as MeleeWeapon).InitialParentTileIndex != 4)) && Game1.player.Items[i].canBeTrashed() && !(Game1.player.Items[i] is Ring) && r.NextDouble() < itemLossRate)
                {
                    numberOfItemsLost++;
                }
            }
            int moneyToLose = Math.Min(1000, Game1.player.Money);
            return (moneyToLose, numberOfItemsLost);
        }
    }
}
