using System;
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
            tracker.AddDamage(now, damage, timeoutSeconds);
        }

        public static void Update(Player player, double now, double windowSeconds, double timeoutSeconds)
        {
            DpsCombatTracker tracker = Get(player);
            tracker.WindowSeconds = windowSeconds;
            tracker.UpdateState(now, timeoutSeconds);
        }

        public static void Clear()
        {
            _trackersByPlayerId.Clear();
        }

        public static double NowSeconds()
        {
            return DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond;
        }
    }
}