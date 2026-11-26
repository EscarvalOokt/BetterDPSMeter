using System;
using System.Collections.Generic;

namespace BetterDPSMeter
{
    internal sealed class DpsCombatTracker
    {
        private readonly Queue<(double time, int damage)> _damageEvents = new Queue<(double time, int damage)>();
        private readonly DpsDisplaySnapshot _displaySnapshot = new DpsDisplaySnapshot();
        private readonly Queue<(double time, int damage)> _windowEvents = new Queue<(double time, int damage)>();
        private double _appliedWindowSeconds;

        private double _combatStartTime = -1.0;
        private bool _isCombatActive;
        private double _lastHitTime = -1.0;
        private long _sessionHitCount;
        private long _sessionTotalDamage;
        private long _windowDamage;

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

            (double time, int damage) hit = (time: now, damage);
            _damageEvents.Enqueue(hit);
            AddToWindow(hit);

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
            _isCombatActive = true;
            _combatStartTime = now;
            _appliedWindowSeconds = WindowSeconds;

            _displaySnapshot.Clear();
        }

        private void EndCombat(double minimumDurationSeconds)
        {
            BuildFinalSnapshot(minimumDurationSeconds);

            _damageEvents.Clear();
            _windowEvents.Clear();
            _windowDamage = 0;
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

            long averageDps = finalDuration > 0.0 ? (long)Math.Round(_sessionTotalDamage / finalDuration) : 0;

            _displaySnapshot.AverageDps = averageDps;
            _displaySnapshot.HitsInWindow = _sessionHitCount;
            _displaySnapshot.DamageInWindow = _sessionTotalDamage;
            _displaySnapshot.WindowSecondsCovered = finalDuration;
            _displaySnapshot.TimeoutRemaining = 0.0;
            _displaySnapshot.IsLastCombatResult = true;
        }

        private void RebuildSnapshot(double now, double timeoutSeconds)
        {
            UpdateWindow(now);

            double coveredSeconds = GetCoveredSeconds(now);

            long averageDps = coveredSeconds > 0.0 ? (long)Math.Round(_windowDamage / coveredSeconds) : 0;

            _displaySnapshot.AverageDps = averageDps;
            _displaySnapshot.HitsInWindow = _windowEvents.Count;
            _displaySnapshot.DamageInWindow = _windowDamage;
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

        private void AddToWindow((double time, int damage) hit)
        {
            _windowEvents.Enqueue(hit);
            _windowDamage += hit.damage;
        }

        private void UpdateWindow(double now)
        {
            while (_damageEvents.Count > 0 && now - _damageEvents.Peek().time > DpsTrackingConstants.MaxWindowSeconds)
            {
                _damageEvents.Dequeue();
            }

            double windowSeconds = Math.Min(WindowSeconds, DpsTrackingConstants.MaxWindowSeconds);
            if (windowSeconds > _appliedWindowSeconds)
            {
                _windowEvents.Clear();
                _windowDamage = 0;
                foreach ((double time, int damage) hit in _damageEvents)
                {
                    if (now - hit.time <= windowSeconds)
                        AddToWindow(hit);
                }
            }

            _appliedWindowSeconds = windowSeconds;
            while (_windowEvents.Count > 0 && now - _windowEvents.Peek().time > windowSeconds)
            {
                _windowDamage -= _windowEvents.Dequeue().damage;
            }
        }
    }
}