using HarmonyLib;
using StardewModdingAPI;
using System.Reflection;

namespace NoCrows
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        /*********
        ** Public methods
        *********/

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            var harmony = new Harmony(this.Helper.ModRegistry.ModID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
