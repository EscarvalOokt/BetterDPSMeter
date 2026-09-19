using TerrariaModder.Core.Config;

namespace BetterDPSMeter
{
    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once InconsistentNaming
    public sealed class BetterDPSMeterConfig : ModConfig
    {
        public override int Version => 1;

        [Client]
        [Label("Show Extra Info")]
        [Description("Show extra combat stats such as hits, total damage and average hit damage.")]
        public bool ShowExtraInfo { get; set; } = true;

        [Client]
        [Label("Require DPS Meter")]
        [Description("If enabled, the overlay is only shown while the DPS Meter item effect is active.")]
        public bool RequireDpsMeter { get; set; } = true;

        [Client]
        [Label("DPS Window (seconds)")]
        [Description("How many seconds of damage history are used for the average DPS calculation.")]
        [Range(2, DpsTrackingConstants.MaxWindowSeconds)]
        public int WindowSeconds { get; set; } = 10;

        [Client]
        [Label("Combat Timeout (seconds)")]
        [Description("How long after the last hit the tracker is still considered in combat.")]
        [Range(0.5f, 30.0f)]
        public float CombatTimeoutSeconds { get; set; } = 3.0f;
    }
}