using System;
using TerrariaModder.Core;
using TerrariaModder.Core.Events;
using TerrariaModder.Core.Logging;

namespace BetterDPSMeter
{
    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once InconsistentNaming
    internal class BetterDPSMeterMod : IMod, IModLifecycle
    {
        private BetterDPSMeterConfig _config;
        private ILogger _logger;
        private DpsDisplayOverlay _overlay;

        public static BetterDPSMeterMod Instance { get; private set; }

        public bool ShowExtraInfo { get; private set; } = true;
        public bool RequireDpsMeter { get; private set; } = true;
        public int WindowSeconds { get; private set; } = 10;
        public double CombatTimeoutSeconds { get; private set; } = 3.0;

        public string Id => "better-dps-meter";
        public string Name => "Better DPS Meter";
        public string Version => "1.1.0";

        public void Initialize(ModContext context)
        {
            Instance = this;

            _logger = context.Logger;
            _config = context.GetConfig<BetterDPSMeterConfig>() ??
                      throw new InvalidOperationException("BetterDPSMeterConfig was not loaded by TerrariaModder.");

            LoadConfig();

            _overlay = new DpsDisplayOverlay(this);
            FrameEvents.OnUIOverlay += HandleUiOverlay;

            _logger.Info(Name + " v" + Version + " initialized!");
        }

        public void Unload()
        {
            FrameEvents.OnUIOverlay -= HandleUiOverlay;

            DpsTrackerRepository.Clear();

            _overlay = null;
            _config = null;
            Instance = null;

            _logger?.Info(Name + " unloaded!");
            _logger = null;
        }

        public void OnContentReady(ModContext _)
        {
            // No content-ready initialization is required.
        }

        public void OnWorldLoad()
        {
            DpsTrackerRepository.Clear();
        }

        public void OnWorldUnload()
        {
            DpsTrackerRepository.Clear();
        }

        public void OnConfigChanged()
        {
            LoadConfig();
            _logger?.Info("Better DPS Meter config reloaded.");
        }

        private void LoadConfig()
        {
            if (_config == null)
                return;

            ShowExtraInfo = _config.ShowExtraInfo;
            RequireDpsMeter = _config.RequireDpsMeter;
            WindowSeconds = _config.WindowSeconds;
            CombatTimeoutSeconds = _config.CombatTimeoutSeconds;
        }

        private void HandleUiOverlay()
        {
            _overlay?.Draw();
        }
    }
}