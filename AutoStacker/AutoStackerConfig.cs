using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace AutoStacker
{
    internal class AutoStackerConfig
    {
        public KeybindList ActivationKey { get; set; } = KeybindList.Parse("K");
    }
}
