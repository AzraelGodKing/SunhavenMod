using NUnit.Framework;
using TheVault.Vault;

namespace TheVault.Tests
{
    [TestFixture]
    public class VaultDataMigrationTests
    {
        [Test]
        public void Migrate_UpgradesVersionZeroToCurrent()
        {
            var data = new VaultData { Version = 0 };

            var migrated = VaultDataMigration.Migrate(data);

            Assert.That(migrated.Version, Is.EqualTo(2));
        }

        [Test]
        public void Migrate_LeavesCurrentVersionUntouched()
        {
            var data = new VaultData { Version = 2 };

            var migrated = VaultDataMigration.Migrate(data);

            Assert.That(migrated.Version, Is.EqualTo(2));
        }

        [Test]
        public void Migrate_CreatesDataWhenInputIsNull()
        {
            var migrated = VaultDataMigration.Migrate(null);

            Assert.That(migrated, Is.Not.Null);
            Assert.That(migrated.Version, Is.EqualTo(2));
        }
    }
}
