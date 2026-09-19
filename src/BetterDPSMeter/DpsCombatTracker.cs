using System;
using System.Collections.Generic;

namespace BetterDPSMeter
{
    internal sealed class DpsCombatTracker
    {
        private readonly Queue<(double time, int damage)> _damageEvents = new Queue<(double time, int damage)>();
        private readonly DpsDisplaySnapshot _displaySnapshot = new DpsDisplaySnapshot();

        private double _combatStartTime = -1.0;
        private bool _isCombatActive;
        private double _lastHitTime = -1.0;
        private int _sessionHitCount;
        private int _sessionTotalDamage;

        public double WindowSeconds { get; set; } = 10.0;
        public double LastHitTime => _lastHitTime;
        public DpsDisplaySnapshot DisplaySnapshot => _displaySnapshot;

        public void AddDamage(double now, int damage, double timeoutSeconds, double minimumDurationSeconds)
        {
            if (damage <= 0)
                return;

            if (_isCombatActive && GetCombatTimeoutRemaining(now, timeoutSeconds) <= 0.0)
                EndCombat(minimumDurationSeconds);

            if (!_isCombatActive)
                StartCombat(now);

            _lastHitTime = now;
            _sessionTotalDamage += damage;
            _sessionHitCount++;

            _damageEvents.Enqueue((now, damage));
            TrimHistory(now);

            RebuildSnapshot(now, timeoutSeconds);
        }

        public void UpdateState(double now, double timeoutSeconds, double minimumDurationSeconds)
        {
            if (!_isCombatActive)
                return;

            if (GetCombatTimeoutRemaining(now, timeoutSeconds) <= 0.0)
            {
                EndCombat(minimumDurationSeconds);
                return;
            }

            RebuildSnapshot(now, timeoutSeconds);
        }

        private void StartCombat(double now)
        {
            _damageEvents.Clear();
            _isCombatActive = true;
            _combatStartTime = now;
            _sessionHitCount = 0;
            _sessionTotalDamage = 0;

            _displaySnapshot.Clear();
            _displaySnapshot.IsLastCombatResult = false;
        }

        private void EndCombat(double minimumDurationSeconds)
        {
            BuildFinalSnapshot(minimumDurationSeconds);

            _damageEvents.Clear();
            _combatStartTime = -1.0;
            _lastHitTime = -1.0;
            _sessionHitCount = 0;
            _sessionTotalDamage = 0;
            _isCombatActive = false;
        }

        private void BuildFinalSnapshot(double minimumDurationSeconds)
        {
            double hitSpan = _combatStartTime >= 0.0 && _lastHitTime >= _combatStartTime
                ? _lastHitTime - _combatStartTime
                : 0.0;

            double finalDuration = hitSpan > 0.0 ? hitSpan : minimumDurationSeconds;

            if (finalDuration < 0.0)
                finalDuration = 0.0;

            int averageDps = finalDuration > 0.0 ? (int)Math.Round(_sessionTotalDamage / finalDuration) : 0;

            _displaySnapshot.AverageDps = averageDps;
            _displaySnapshot.HitsInWindow = _sessionHitCount;
            _displaySnapshot.DamageInWindow = _sessionTotalDamage;
            _displaySnapshot.WindowSecondsCovered = finalDuration;
            _displaySnapshot.TimeoutRemaining = 0.0;
            _displaySnapshot.IsLastCombatResult = true;
        }

        private void RebuildSnapshot(double now, double timeoutSeconds)
        {
            TrimHistory(now);

            var totalDamage = 0;
            var hitCount = 0;

            foreach ((double time, int damage) damageEvent in _damageEvents)
            {
                if (now - damageEvent.time > WindowSeconds)
                    continue;

                totalDamage += damageEvent.damage;
                hitCount++;
            }

            double coveredSeconds = GetCoveredSeconds(now);

            int averageDps = coveredSeconds > 0.0 ? (int)Math.Round(totalDamage / coveredSeconds) : 0;

            _displaySnapshot.AverageDps = averageDps;
            _displaySnapshot.HitsInWindow = hitCount;
            _displaySnapshot.DamageInWindow = totalDamage;
            _displaySnapshot.WindowSecondsCovered = coveredSeconds;
            _displaySnapshot.TimeoutRemaining = GetCombatTimeoutRemaining(now, timeoutSeconds);
            _displaySnapshot.IsLastCombatResult = false;
        }

        private double GetCombatTimeoutRemaining(double now, double timeoutSeconds)
        {
            if (_lastHitTime < 0.0)
                return 0.0;

            double remainingSeconds = timeoutSeconds - (now - _lastHitTime);
            return remainingSeconds > 0.0 ? remainingSeconds : 0.0;
        }

        private double GetCoveredSeconds(double now)
        {
            if (!_isCombatActive || _combatStartTime < 0.0)
                return 0.0;

            double coveredSeconds = now - _combatStartTime;

            if (coveredSeconds > WindowSeconds)
                coveredSeconds = WindowSeconds;

            return coveredSeconds > 0.0 ? coveredSeconds : 0.0;
        }

        private void TrimHistory(double now)
        {
            while (_damageEvents.Count > 0 && now - _damageEvents.Peek().time > DpsTrackingConstants.MaxWindowSeconds)
            {
                _damageEvents.Dequeue();
            }
        }
    }
}