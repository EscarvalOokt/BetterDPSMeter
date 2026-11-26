using System.Linq;
using NUnit.Framework;

namespace BetterDPSMeter.Tests.UI
{
    [TestFixture]
    public sealed class DpsOverlayLayoutTests
    {
        [Test]
        public void CompactLayout_KeepsTheSameAnchorUntilAFourthBuffRowCrowdsItOut()
        {
            var buffTypes = new int[44];
            Assert.That(DpsOverlayLayout.GetPosition(800, 600, buffTypes), Is.EqualTo((32, 248)));

            // The game draws positive buff slots even when earlier slots are empty.
            for (var slot = 11; slot < buffTypes.Length; slot++)
                buffTypes[slot] = slot + 1;

            Assert.That(DpsOverlayLayout.GetPosition(800, 600, buffTypes), Is.EqualTo((32, 248)));

            buffTypes[10] = 1;
            Assert.That(DpsOverlayLayout.GetPosition(800, 600, buffTypes), Is.EqualTo((500, 20)));
        }

        [Test]
        public void CompactLayout_UsesTheGapWhenAllFiveLinesFitAboveFullChat()
        {
            int[] buffTypes = Enumerable.Range(1, 44).ToArray();

            Assert.That(DpsOverlayLayout.GetPosition(800, 637, buffTypes), Is.EqualTo((500, 20)));
            Assert.That(DpsOverlayLayout.GetPosition(800, 638, buffTypes), Is.EqualTo((32, 286)));
        }

        [Test]
        public void ResizingAcrossTheLogicalWidthThreshold_RestoresTheWideAnchor()
        {
            var buffTypes = new int[44];

            Assert.That(DpsOverlayLayout.GetPosition(1024, 768, buffTypes), Is.EqualTo((32, 416)));
            Assert.That(DpsOverlayLayout.GetPosition(1025, 768, buffTypes), Is.EqualTo((500, 20)));
        }
    }
}