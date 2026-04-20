using System;
using System.Linq;
using NUnit.Framework;
using SunhavenTodo.Data;

namespace SunhavenTodo.Tests
{
    [TestFixture]
    public class TodoJsonTests
    {
        [Test]
        public void Roundtrip_EmptyList_PreservesCharacterName()
        {
            var data = new TodoListData("Tester");

            var json = TodoJson.Serialize(data);
            var result = TodoJson.Deserialize(json);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.CharacterName, Is.EqualTo("Tester"));
            Assert.That(result.Items, Is.Empty);
        }

        [Test]
        public void Roundtrip_SingleItem_PreservesAllFields()
        {
            var created = new DateTime(2025, 4, 1, 12, 0, 0, DateTimeKind.Utc);
            var data = new TodoListData("Hero");
            data.Items.Add(new TodoItem
            {
                Id = "abc-123",
                Title = "Water crops",
                Description = "Use the watering can",
                Priority = TodoPriority.High,
                Category = TodoCategory.Farming,
                IsCompleted = false,
                CreatedAt = created,
                IconItemId = 42,
                MuseumDestination = "Aquarium",
            });

            var json = TodoJson.Serialize(data);
            var result = TodoJson.Deserialize(json);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Items.Count, Is.EqualTo(1));
            var item = result.Items[0];
            Assert.That(item.Id, Is.EqualTo("abc-123"));
            Assert.That(item.Title, Is.EqualTo("Water crops"));
            Assert.That(item.Description, Is.EqualTo("Use the watering can"));
            Assert.That(item.Priority, Is.EqualTo(TodoPriority.High));
            Assert.That(item.Category, Is.EqualTo(TodoCategory.Farming));
            Assert.That(item.IsCompleted, Is.False);
            Assert.That(item.IconItemId, Is.EqualTo(42));
            Assert.That(item.MuseumDestination, Is.EqualTo("Aquarium"));
            Assert.That(item.CreatedAt, Is.EqualTo(created).Within(TimeSpan.FromSeconds(1)));
        }

        [Test]
        public void Roundtrip_CompletedItem_PreservesCompletedAt()
        {
            var completedAt = new DateTime(2025, 4, 2, 9, 30, 0, DateTimeKind.Utc);
            var data = new TodoListData("Hero");
            data.Items.Add(new TodoItem
            {
                Title = "Mine iron",
                IsCompleted = true,
                CompletedAt = completedAt,
            });

            var json = TodoJson.Serialize(data);
            var result = TodoJson.Deserialize(json);

            var item = result.Items[0];
            Assert.That(item.IsCompleted, Is.True);
            Assert.That(item.CompletedAt, Is.Not.Null);
            Assert.That(item.CompletedAt.Value, Is.EqualTo(completedAt).Within(TimeSpan.FromSeconds(1)));
        }

        [Test]
        public void Roundtrip_RecurringItem_PreservesInterval()
        {
            var data = new TodoListData("Hero");
            data.Items.Add(new TodoItem
            {
                Title = "Check mail",
                IsRecurring = true,
                RecurInterval = RecurInterval.Daily,
            });

            var json = TodoJson.Serialize(data);
            var result = TodoJson.Deserialize(json);

            var item = result.Items[0];
            Assert.That(item.IsRecurring, Is.True);
            Assert.That(item.RecurInterval, Is.EqualTo(RecurInterval.Daily));
        }

        [Test]
        public void Roundtrip_WeeklyRecurring_PreservesInterval()
        {
            var data = new TodoListData("Hero");
            data.Items.Add(new TodoItem
            {
                Title = "Community event",
                IsRecurring = true,
                RecurInterval = RecurInterval.Weekly,
            });

            var json = TodoJson.Serialize(data);
            var result = TodoJson.Deserialize(json);

            Assert.That(result.Items[0].RecurInterval, Is.EqualTo(RecurInterval.Weekly));
        }

        [Test]
        public void Roundtrip_SpecialCharactersInTitle_AreEscapedAndRestored()
        {
            var data = new TodoListData("Hero");
            data.Items.Add(new TodoItem { Title = "Say \"hello\" & go\nnewline" });

            var json = TodoJson.Serialize(data);
            var result = TodoJson.Deserialize(json);

            Assert.That(result.Items[0].Title, Is.EqualTo("Say \"hello\" & go\nnewline"));
        }

        [Test]
        public void Roundtrip_MultipleItems_PreservesOrder()
        {
            var data = new TodoListData("Hero");
            for (int i = 0; i < 5; i++)
                data.Items.Add(new TodoItem { Title = $"Task {i}", Priority = (TodoPriority)(i % 4) });

            var json = TodoJson.Serialize(data);
            var result = TodoJson.Deserialize(json);

            Assert.That(result.Items.Count, Is.EqualTo(5));
            for (int i = 0; i < 5; i++)
                Assert.That(result.Items[i].Title, Is.EqualTo($"Task {i}"));
        }

        [Test]
        public void Deserialize_InvalidJson_ReturnsNull()
        {
            var result = TodoJson.Deserialize("not valid json at all");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Deserialize_EmptyObject_ReturnsEmptyData()
        {
            var result = TodoJson.Deserialize("{}");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Items, Is.Empty);
        }
    }
}
