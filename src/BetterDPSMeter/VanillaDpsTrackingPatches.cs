using HarmonyLib;
using Terraria;
using Terraria.GameContent;

// ReSharper disable InconsistentNaming

namespace BetterDPSMeter
{
    internal readonly struct VanillaDpsTrackingOverrideState(Player player, bool shouldRestore)
    {
        public Player Player { get; } = player;
        public bool ShouldRestore { get; } = shouldRestore;
    }

    internal static class VanillaDpsTrackingOverride
    {
        public static VanillaDpsTrackingOverrideState Enable(Player player)
        {
            BetterDPSMeterMod mod = BetterDPSMeterMod.Instance;
            if (mod == null || mod.RequireDpsMeter)
                return default;

            bool shouldSkip = player == null ||
                              !player.active ||
                              player.whoAmI != Main.myPlayer ||
                              player.accDreamCatcher;

            if (shouldSkip)
                return default;

            player.accDreamCatcher = true;
            return new VanillaDpsTrackingOverrideState(player, true);
        }

        public static void Restore(VanillaDpsTrackingOverrideState state)
        {
            if (!state.ShouldRestore || state.Player == null)
                return;

            state.Player.accDreamCatcher = false;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.ItemCheck))]
    internal static class PlayerItemCheckDpsTrackingPatch
    {
        public static void Prefix(Player __instance, out VanillaDpsTrackingOverrideState __state)
        {
            __state = VanillaDpsTrackingOverride.Enable(__instance);
        }

        public static void Finalizer(VanillaDpsTrackingOverrideState __state)
        {
            VanillaDpsTrackingOverride.Restore(__state);
        }
    }

    [HarmonyPatch(
        typeof(Player),
        nameof(Player.ApplyDamageToNPC),
        typeof(NPC),
        typeof(int),
        typeof(float),
        typeof(int),
        typeof(bool),
        typeof(PlayerNPCHitSource))]
    internal static class PlayerApplyDamageToNpcDpsTrackingPatch
    {
        public static void Prefix(Player __instance, out VanillaDpsTrackingOverrideState __state)
        {
            __state = VanillaDpsTrackingOverride.Enable(__instance);
        }

        public static void Finalizer(VanillaDpsTrackingOverrideState __state)
        {
            VanillaDpsTrackingOverride.Restore(__state);
        }
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.Damage))]
    internal static class ProjectileDamageDpsTrackingPatch
    {
        public static void Prefix(Projectile __instance, out VanillaDpsTrackingOverrideState __state)
        {
            __state = default;

            if (__instance == null)
                return;

            int owner = __instance.owner;
            bool isLocalPlayerOwner = owner >= 0 && owner < Main.player.Length && owner == Main.myPlayer;

            if (!isLocalPlayerOwner)
                return;

            __state = VanillaDpsTrackingOverride.Enable(Main.player[owner]);
        }

        public static void Finalizer(VanillaDpsTrackingOverrideState __state)
        {
            VanillaDpsTrackingOverride.Restore(__state);
        }
    }
}