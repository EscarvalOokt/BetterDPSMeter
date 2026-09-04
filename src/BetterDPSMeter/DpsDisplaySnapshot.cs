namespace BetterDPSMeter
{
    internal sealed class DpsDisplaySnapshot
    {
        public int AverageDps { get; set; }
        public int HitsInWindow { get; set; }
        public int DamageInWindow { get; set; }
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