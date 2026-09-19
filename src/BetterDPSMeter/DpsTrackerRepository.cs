using System.Collections.Generic;
using Terraria;

namespace BetterDPSMeter
{
    internal static class DpsTrackerRepository
    {
        private static readonly Dictionary<int, DpsCombatTracker> _trackersByPlayerId =
            new Dictionary<int, DpsCombatTracker>();

        public static DpsCombatTracker Get(Player player)
        {
            if (!_trackersByPlayerId.TryGetValue(player.whoAmI, out DpsCombatTracker tracker))
            {
                tracker = new DpsCombatTracker();
                _trackersByPlayerId[player.whoAmI] = tracker;
            }

            return tracker;
        }

        public static void AddDamage(Player player, double now, int damage, double windowSeconds, double timeoutSeconds)
        {
            DpsCombatTracker tracker = Get(player);
            tracker.WindowSeconds = windowSeconds;
            tracker.AddDamage(now, damage, timeoutSeconds, Main.TARGET_FRAME_TIME);
        }

        public static void Update(Player player, double now, double windowSeconds, double timeoutSeconds)
        {
            DpsCombatTracker tracker = Get(player);
            tracker.WindowSeconds = windowSeconds;
            tracker.UpdateState(now, timeoutSeconds, Main.TARGET_FRAME_TIME);
        }

        public static void Clear()
        {
            _trackersByPlayerId.Clear();
        }

        public static double NowSeconds()
        {
            return Main.GameUpdateCount * Main.TARGET_FRAME_TIME;
        }
    }
}