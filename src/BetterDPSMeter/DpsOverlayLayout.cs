namespace BetterDPSMeter
{
    internal static class DpsOverlayLayout
    {
        public const int LineSpacing = 16;
        private const int MaxCompactWidth = 1024;
        private const int Gap = 10;

        // Terraria packs active buffs left to right and draws their timers below the icons.
        private const int BuffLeft = 32;
        private const int BuffTop = 76;
        private const int BuffsPerRow = 11;
        private const int BuffRowHeight = 50;

        // The bottom chat line starts 58 units from the bottom. Nine more fit above it.
        private const int ChatLineHeight = 22;
        private const int FullChatTopFromBottom = 58 + 9 * ChatLineHeight;
        private const int FiveLineHeight = 4 * LineSpacing + ChatLineHeight;

        private static readonly (int X, int Y) _defaultPosition = (500, 20);

        public static (int X, int Y) GetPosition(int uiWidth, int uiHeight, int[] buffTypes)
        {
            if (uiWidth > MaxCompactWidth)
                return _defaultPosition;

            var activeBuffs = 0;
            foreach (int buffType in buffTypes)
            {
                if (buffType > 0)
                    activeBuffs++;
            }

            int buffRows = (activeBuffs + BuffsPerRow - 1) / BuffsPerRow;
            int buffsBottom = BuffTop + buffRows * BuffRowHeight;
            int overlayTop = uiHeight - FullChatTopFromBottom - Gap - FiveLineHeight;
            if (overlayTop < buffsBottom + Gap)
                return _defaultPosition;

            return (BuffLeft, overlayTop);
        }
    }
}