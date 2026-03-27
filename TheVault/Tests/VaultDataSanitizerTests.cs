using System.Collections.Generic;
using NUnit.Framework;
using TheVault.Vault;

namespace TheVault.Tests
{
    [TestFixture]
    public class VaultDataSanitizerTests
    {
        [Test]
        public void Sanitize_ClampsNegativeStringDictValues()
        {
            var data = new VaultData
            {
                Keys = new Dictionary<string, int> { { "copper", -3 }, { "iron", 2 } }
            };

            int fixes = VaultDataSanitizer.Sanitize(data);

            Assert.That(fixes, Is.EqualTo(1));
            Assert.That(data.Keys["copper"], Is.EqualTo(0));
            Assert.That(data.Keys["iron"], Is.EqualTo(2));
        }

        [Test]
        public void Sanitize_ClampsNegativeSeasonalValues()
        {
            var data = new VaultData();
            data.SeasonalTokens[SeasonalTokenType.Spring] = -1;

            int fixes = VaultDataSanitizer.Sanitize(data);

            Assert.That(fixes, Is.GreaterThanOrEqualTo(1));
            Assert.That(data.SeasonalTokens[SeasonalTokenType.Spring], Is.EqualTo(0));
        }

        [Test]
        public void Sanitize_RecoversNullStringKeyedDictionaries()
        {
            var data = new VaultData();
            data.Keys = null;
            data.CustomCurrencies = null;
            data.Tickets = null;

            VaultDataSanitizer.Sanitize(data);

            Assert.That(data.Keys, Is.Not.Null);
            Assert.That(data.CustomCurrencies, Is.Not.Null);
            Assert.That(data.Tickets, Is.Not.Null);
        }

        [Test]
        public void Sanitize_NormalizesTicketKeysToLowerCase_ForHudLookup()
        {
            var data = new VaultData();
            data.Tickets["ManaShard"] = 4;

            VaultDataSanitizer.Sanitize(data);

            Assert.That(data.Tickets.TryGetValue("manashard", out int v));
            Assert.That(v, Is.EqualTo(4));
            Assert.That(data.Tickets.ContainsKey("ManaShard"), Is.False);
        }
    }
}
