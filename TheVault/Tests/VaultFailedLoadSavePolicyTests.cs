using NUnit.Framework;
using TheVault.Vault;

namespace TheVault.Tests
{
    [TestFixture]
    public class VaultFailedLoadSavePolicyTests
    {
        [Test]
        public void AllowSave_False_WhenLoadFailedNoRecoverableData()
        {
            Assert.That(VaultFailedLoadSavePolicy.AllowSave(loadFailedNoRecoverableData: true), Is.False);
        }

        [Test]
        public void AllowSave_True_AfterSuccessfulLoadOrStartFresh()
        {
            Assert.That(VaultFailedLoadSavePolicy.AllowSave(loadFailedNoRecoverableData: false), Is.True);
        }
    }
}
