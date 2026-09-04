using System;
using System.Collections.Generic;
using Terraria;
using TerrariaModder.Core.UI;

namespace BetterDPSMeter
{
    internal sealed class DpsDisplayOverlay(BetterDPSMeterMod mod)
    {
        private readonly int _positionX = 500;
        private readonly int _positionY = 20;
        private readonly int _textLineHeight = 16;

        public void Draw()
        {
            if (Main.gameMenu)
                return;

            if (Main.gamePaused)
                return;

            Player player = Main.player[Main.myPlayer];
            bool isPlayerUnavailable = player == null || !player.active;
            if (isPlayerUnavailable)
                return;

            if (Main.playerInventory)
                return;

            bool shouldRequireDpsMeter = mod.RequireDpsMeter && !player.accDreamCatcher;
            if (shouldRequireDpsMeter)
                return;

            double currentTimeSeconds = DpsTrackerRepository.NowSeconds();
            DpsTrackerRepository.Update(player, currentTimeSeconds, mod.WindowSeconds, mod.CombatTimeoutSeconds);

            DpsCombatTracker tracker = DpsTrackerRepository.Get(player);
            DpsDisplaySnapshot snapshot = tracker.DisplaySnapshot;

            List<(string Text, Color4 Color)> displayLines = BuildMainLines(snapshot);
            DrawMixedColumn(displayLines, _positionX, _positionY, _textLineHeight);
        }

        private List<(string Text, Color4 Color)> BuildMainLines(DpsDisplaySnapshot snapshot)
        {
            List<(string Text, Color4 Color)> displayLines = [BuildDpsLine(snapshot)];

            if (mod.ShowExtraInfo)
            {
                foreach ((string Text, Color4 Color) line in BuildExtraLines(snapshot))
                    displayLines.Add(line);
            }

            bool shouldShowStatus = snapshot.IsLastCombatResult || mod.ShowExtraInfo;
            if (shouldShowStatus)
                displayLines.Add(BuildStatusLine(snapshot));

            return displayLines;
        }

        private (string Text, Color4 Color) BuildDpsLine(DpsDisplaySnapshot snapshot)
        {
            if (mod.ShowExtraInfo)
            {
                return ("DPS: " + snapshot.AverageDps + " / " + snapshot.WindowSecondsCovered.ToString("F1") + "s",
                    UIColors.Text);
            }

            return ("DPS: " + snapshot.AverageDps, UIColors.Text);
        }

        private List<(string Text, Color4 Color)> BuildExtraLines(DpsDisplaySnapshot snapshot)
        {
            int averageDamagePerHit = snapshot.HitsInWindow > 0
                ? (int)Math.Round((double)snapshot.DamageInWindow / snapshot.HitsInWindow)
                : 0;

            return
            [
                ("Hits: " + snapshot.HitsInWindow, UIColors.TextDim),
                ("Damage: " + snapshot.DamageInWindow, UIColors.TextDim),
                ("Avg Damage/Hit: " + averageDamagePerHit, UIColors.TextDim)
            ];
        }

        private (string Text, Color4 Color) BuildStatusLine(DpsDisplaySnapshot snapshot)
        {
            if (snapshot.IsLastCombatResult)
                return ("Combat Ended", UIColors.TextDim);

            return ("Exit Combat in: " + snapshot.TimeoutRemaining.ToString("F2") + "s", UIColors.TextDim);
        }

        private static void DrawMixedColumn(List<(string Text, Color4 Color)> lines, int x, int y, int lineHeight)
        {
            for (var index = 0; index < lines.Count; index++)
            {
                (string text, Color4 color) = lines[index];
                UIRenderer.DrawText(text, x, y + index * lineHeight, color);
            }
        }
    }
}