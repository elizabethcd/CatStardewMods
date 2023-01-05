namespace BetterFruitTrees.Patches
{
    internal class PlacementWarningPatch
    {
        public static bool Prefix(string localization_string_key)
        {
            if (localization_string_key.Equals("Strings\\UI:FruitTree_Warning"))
            {
                return false;
            }
            return true;
        }
    }
}