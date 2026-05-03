using NUnit.Framework;
using SenpaisChest.Data;

namespace SenpaisChest.Tests
{
    [TestFixture]
    public class ItemNamePatternMatchTests
    {
        [Test]
        public void Star_MatchesPrefixAndSuffix()
        {
            Assert.That(ItemNamePatternMatch.Matches("Tomato Seed", "Tomato*"), Is.True);
            Assert.That(ItemNamePatternMatch.Matches("Red Tomato", "*Tomato"), Is.True);
            Assert.That(ItemNamePatternMatch.Matches("Ancient Ore", "*Ore"), Is.True);
        }

        [Test]
        public void Question_MatchesSingleChar()
        {
            Assert.That(ItemNamePatternMatch.Matches("Ore A", "Ore ?"), Is.True);
            Assert.That(ItemNamePatternMatch.Matches("Ore AB", "Ore ?"), Is.False);
        }

        [Test]
        public void Case_IsIgnored()
        {
            Assert.That(ItemNamePatternMatch.Matches("Gold Bar", "gold*"), Is.True);
        }

        [Test]
        public void EmptyPattern_NeverMatches()
        {
            Assert.That(ItemNamePatternMatch.Matches("Anything", ""), Is.False);
            Assert.That(ItemNamePatternMatch.Matches("Anything", "   "), Is.False);
        }

        [Test]
        public void LiteralWithSpecialRegexChars_Escapes()
        {
            Assert.That(ItemNamePatternMatch.Matches("Fish (large)", "Fish (large)"), Is.True);
        }
    }
}
