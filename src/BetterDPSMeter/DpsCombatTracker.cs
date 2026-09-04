using System;
using System.Collections.Generic;

namespace BetterDPSMeter
{
    internal sealed class DpsCombatTracker
    {
        private readonly Queue<(double time, int damage)> _damageEvents = new Queue<(double time, int damage)>();
        private readonly DpsDisplaySnapshot _displaySnapshot = new DpsDisplaySnapshot();
        private bool _isCombatActive;

        private double _lastHitTime = -1.0;

        public double WindowSeconds { get; set; } = 10.0;
        public double LastHitTime => _lastHitTime;
        public DpsDisplaySnapshot DisplaySnapshot => _displaySnapshot;

        public void AddDamage(double now, int damage, double timeoutSeconds)
        {
            if (damage <= 0)
                return;

            if (!_isCombatActive)
                StartCombat();

            _lastHitTime = now;
            _damageEvents.Enqueue((now, damage));
            Trim(now);

            RebuildSnapshot(now, timeoutSeconds);
        }

        public void UpdateState(double now, double timeoutSeconds)
        {
            if (!_isCombatActive)
                return;

            double timeoutRemaining = GetCombatTimeoutRemaining(now, timeoutSeconds);
            _displaySnapshot.TimeoutRemaining = timeoutRemaining;

            if (timeoutRemaining <= 0.0)
                EndCombat();
        }

        private void StartCombat()
        {
            _damageEvents.Clear();
            _isCombatActive = true;
            _displaySnapshot.Clear();
            _displaySnapshot.IsLastCombatResult = false;
        }

        private void EndCombat()
        {
            _damageEvents.Clear();
            _lastHitTime = -1.0;
            _isCombatActive = false;

            _displaySnapshot.TimeoutRemaining = 0.0;
            _displaySnapshot.IsLastCombatResult = true;
        }

        private void RebuildSnapshot(double now, double timeoutSeconds)
        {
            Trim(now);

            var totalDamage = 0;
            foreach ((double time, int damage) damageEvent in _damageEvents)
                totalDamage += damageEvent.damage;

            int hitCount = _damageEvents.Count;
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
            if (_damageEvents.Count == 0)
                return 0.0;

            double oldestEventTime = _damageEvents.Peek().time;
            double coveredSeconds = now - oldestEventTime;

            if (coveredSeconds > WindowSeconds)
                coveredSeconds = WindowSeconds;

            return coveredSeconds > 0.0 ? coveredSeconds : 0.0;
        }

        private void Trim(double now)
        {
            while (_damageEvents.Count > 0 && now - _damageEvents.Peek().time > WindowSeconds)
                _damageEvents.Dequeue();
        }
    }
}