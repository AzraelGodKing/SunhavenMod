using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SunhavenMods.Shared;

namespace SunhavenTodo.Tests
{
    [TestFixture]
    public class MinimalJsonParserTests
    {
        [Test]
        public void Parse_RejectsTruncatedObject()
        {
            Assert.Throws<JsonParseException>(() => MinimalJsonParser.Parse("{\"a\":1"));
        }

        [Test]
        public void Parse_RejectsTrailingContent()
        {
            Assert.Throws<JsonParseException>(() => MinimalJsonParser.Parse("{\"a\":1} trailing"));
        }

        [Test]
        public void WriteJsonString_EscapesControlCharacters()
        {
            var sb = new StringBuilder();
            MinimalJsonParser.WriteJsonString(sb, "a\u0001b");
            Assert.That(sb.ToString(), Is.EqualTo("\"a\\u0001b\""));
        }

        [Test]
        public void RoundTrip_ControlCharacters()
        {
            string original = "x\u0007y\u001fz";
            var sb = new StringBuilder();
            MinimalJsonParser.WriteJsonString(sb, original);
            string json = "{\"v\":" + sb + "}";
            int pos = 0;
            var dict = MinimalJsonParser.ParseObject(json, ref pos);
            Assert.That(dict["v"], Is.EqualTo(original));
        }

        [Test]
        public void TruncationAtEachDecile_IsRejected()
        {
            const string full = "{\"ok\":true,\"items\":[1,2,3],\"name\":\"hero\"}";
            for (int pct = 10; pct <= 90; pct += 10)
            {
                int len = Math.Max(1, full.Length * pct / 100);
                string cut = full.Substring(0, len);
                Assert.Throws<JsonParseException>(() => MinimalJsonParser.Parse(cut), $"pct={pct}");
            }
        }
    }
}
