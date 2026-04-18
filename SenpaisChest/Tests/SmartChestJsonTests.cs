using System.Collections.Generic;
using NUnit.Framework;
using SenpaisChest.Data;

namespace SenpaisChest.Tests
{
    [TestFixture]
    public class SmartChestJsonTests
    {
        [Test]
        public void Roundtrip_EmptyData_PreservesCharacterName()
        {
            var data = new SmartChestSaveData("Tester");

            var json = SmartChestJson.Serialize(data);
            var result = SmartChestJson.Deserialize(json);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.CharacterName, Is.EqualTo("Tester"));
            Assert.That(result.Chests, Is.Empty);
            Assert.That(result.Groups, Is.Empty);
        }

        [Test]
        public void Roundtrip_ChestWithByItemIdRule_PreservesAllFields()
        {
            var data = new SmartChestSaveData("Hero");
            var chest = new SmartChestData("10_20_0", "Herb Chest") { IsEnabled = true };
            chest.Rules.Add(new SmartChestRule { Type = RuleType.ByItemId, ItemId = 999 });
            data.Chests.Add(chest);

            var json = SmartChestJson.Serialize(data);
            var result = SmartChestJson.Deserialize(json);

            Assert.That(result.Chests.Count, Is.EqualTo(1));
            var c = result.Chests[0];
            Assert.That(c.ChestId, Is.EqualTo("10_20_0"));
            Assert.That(c.ChestName, Is.EqualTo("Herb Chest"));
            Assert.That(c.IsEnabled, Is.True);
            Assert.That(c.Rules.Count, Is.EqualTo(1));
            Assert.That(c.Rules[0].Type, Is.EqualTo(RuleType.ByItemId));
            Assert.That(c.Rules[0].ItemId, Is.EqualTo(999));
        }

        [Test]
        public void Roundtrip_ChestWithCategoryRule_PreservesCategoryName()
        {
            var data = new SmartChestSaveData("Hero");
            var chest = new SmartChestData("5_5_0", "Gear");
            chest.Rules.Add(new SmartChestRule { Type = RuleType.ByCategory, CategoryName = "Equip" });
            data.Chests.Add(chest);

            var json = SmartChestJson.Serialize(data);
            var result = SmartChestJson.Deserialize(json);

            var rule = result.Chests[0].Rules[0];
            Assert.That(rule.Type, Is.EqualTo(RuleType.ByCategory));
            Assert.That(rule.CategoryName, Is.EqualTo("Equip"));
        }

        [Test]
        public void Roundtrip_ChestWithPropertyRule_PreservesPropertyName()
        {
            var data = new SmartChestSaveData("Hero");
            var chest = new SmartChestData("1_1_0", "Gem Chest");
            chest.Rules.Add(new SmartChestRule { Type = RuleType.ByProperty, PropertyName = "isGem" });
            data.Chests.Add(chest);

            var json = SmartChestJson.Serialize(data);
            var result = SmartChestJson.Deserialize(json);

            Assert.That(result.Chests[0].Rules[0].PropertyName, Is.EqualTo("isGem"));
        }

        [Test]
        public void Roundtrip_MultipleRules_PreservesOrder()
        {
            var data = new SmartChestSaveData("Hero");
            var chest = new SmartChestData("0_0_0", "Mixed");
            chest.Rules.Add(new SmartChestRule { Type = RuleType.ByItemType, ItemTypeName = "Crop" });
            chest.Rules.Add(new SmartChestRule { Type = RuleType.ByItemType, ItemTypeName = "Fish" });
            chest.Rules.Add(new SmartChestRule { Type = RuleType.ByProperty, PropertyName = "isMeal" });
            data.Chests.Add(chest);

            var json = SmartChestJson.Serialize(data);
            var result = SmartChestJson.Deserialize(json);

            var rules = result.Chests[0].Rules;
            Assert.That(rules.Count, Is.EqualTo(3));
            Assert.That(rules[0].ItemTypeName, Is.EqualTo("Crop"));
            Assert.That(rules[1].ItemTypeName, Is.EqualTo("Fish"));
            Assert.That(rules[2].PropertyName, Is.EqualTo("isMeal"));
        }

        [Test]
        public void Roundtrip_GroupWithItems_PreservesAllIds()
        {
            var data = new SmartChestSaveData("Hero");
            data.Groups.Add(new ItemGroup
            {
                Name = "Flowers",
                ItemIds = new List<int> { 101, 202, 303 }
            });

            var json = SmartChestJson.Serialize(data);
            var result = SmartChestJson.Deserialize(json);

            Assert.That(result.Groups.Count, Is.EqualTo(1));
            var g = result.Groups[0];
            Assert.That(g.Name, Is.EqualTo("Flowers"));
            Assert.That(g.ItemIds, Is.EqualTo(new[] { 101, 202, 303 }));
        }

        [Test]
        public void Roundtrip_DisabledChest_PreservesEnabledFalse()
        {
            var data = new SmartChestSaveData("Hero");
            data.Chests.Add(new SmartChestData("3_3_0", "Empty") { IsEnabled = false });

            var json = SmartChestJson.Serialize(data);
            var result = SmartChestJson.Deserialize(json);

            Assert.That(result.Chests[0].IsEnabled, Is.False);
        }

        [Test]
        public void Roundtrip_SpecialCharsInChestName_AreEscapedAndRestored()
        {
            var data = new SmartChestSaveData("Hero");
            data.Chests.Add(new SmartChestData("0_0_0", "Items: \"rare\" & special"));

            var json = SmartChestJson.Serialize(data);
            var result = SmartChestJson.Deserialize(json);

            Assert.That(result.Chests[0].ChestName, Is.EqualTo("Items: \"rare\" & special"));
        }

        [Test]
        public void Deserialize_InvalidJson_ReturnsNull()
        {
            var result = SmartChestJson.Deserialize("this is not json");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Deserialize_EmptyObject_ReturnsEmptyData()
        {
            var result = SmartChestJson.Deserialize("{}");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Chests, Is.Empty);
        }
    }
}
