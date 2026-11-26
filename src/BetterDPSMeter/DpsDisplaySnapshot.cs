namespace BetterDPSMeter
{
    internal sealed class DpsDisplaySnapshot
    {
        public long AverageDps { get; set; }
        public long HitsInWindow { get; set; }
        public long DamageInWindow { get; set; }
        public double WindowSecondsCovered { get; set; }
        public double TimeoutRemaining { get; set; }
        public bool IsLastCombatResult { get; set; }

        public void Clear()
        {
            AverageDps = 0;
            HitsInWindow = 0;
            DamageInWindow = 0;
            WindowSecondsCovered = 0.0;
            TimeoutRemaining = 0.0;
            IsLastCombatResult = false;
        }
    }
}