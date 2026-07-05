using NUnit.Framework;
using SunhavenMods.Shared;

namespace SunhavenTodo.Tests
{
    [TestFixture]
    public class ModDiagnosticsTests
    {
        [Test]
        public void ReportStartup_StoresReportAndFormatsLogLine()
        {
            ModDiagnostics.ReportStartup(new ModHealthReport
            {
                PluginGuid = "com.test.mod",
                DisplayName = "Test Mod",
                PluginVersion = "1.0.0",
                SharedCodeRevision = SharedCodeRevision.Value,
                IntegrationSummary = "Vault=ok",
                Mode = "startup",
                DataLoaded = false,
                LastPersistenceOutcome = "—"
            });

            var report = ModDiagnostics.GetReport("com.test.mod");
            Assert.That(report, Is.Not.Null);
            Assert.That(report.DisplayName, Is.EqualTo("Test Mod"));

            string line = ModDiagnostics.FormatHealthLogLine(report);
            Assert.That(line, Does.StartWith("[Health] Test Mod v1.0.0"));
            Assert.That(line, Does.Contain($"shared {SharedCodeRevision.Value}"));
            Assert.That(line, Does.Contain("Vault=ok"));
        }

        [Test]
        public void AnalyzeSharedCodeSkew_FlagsMultipleRevisions()
        {
            var rows = new[]
            {
                new ModHealthAggregator.MergedModHealthRow
                {
                    PluginGuid = "a",
                    DisplayName = "Mod A",
                    SharedCodeRevision = "2026.07.05"
                },
                new ModHealthAggregator.MergedModHealthRow
                {
                    PluginGuid = "b",
                    DisplayName = "Mod B",
                    SharedCodeRevision = "2026.06.01"
                }
            };

            var skew = ModHealthAggregator.AnalyzeSharedCodeSkew(rows);
            Assert.That(skew.HasSkew, Is.True);
            Assert.That(skew.ModsByRevision.Count, Is.EqualTo(2));
        }
    }
}
