using HarmonyLib;
using Terraria;

namespace BetterDPSMeter
{
    [HarmonyPatch(typeof(Player), nameof(Player.addDPS))]
    internal static class PlayerAddDpsPatch
    {
        // ReSharper disable once InconsistentNaming
        public static void Postfix(Player __instance, int dmg)
        {
            bool shouldSkip = __instance == null || dmg <= 0;
            if (shouldSkip)
                return;

            if (__instance.whoAmI != Main.myPlayer)
                return;

            BetterDPSMeterMod mod = BetterDPSMeterMod.Instance;
            if (mod == null)
                return;

            double currentTimeSeconds = DpsTrackerRepository.NowSeconds();
            DpsTrackerRepository.AddDamage(
                __instance,
                currentTimeSeconds,
                dmg,
                mod.WindowSeconds,
                mod.CombatTimeoutSeconds);
        }
    }
}