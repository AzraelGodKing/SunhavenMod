using System;
using System.Collections.Generic;

namespace SenpaisChest.Data
{
    public enum RuleType
    {
        ByItemId,
        ByCategory,
        ByItemType,
        ByProperty
    }

    [Serializable]
    public class SmartChestRule
    {
        public RuleType Type;
        public int ItemId;
        public string CategoryName = "";
        public string ItemTypeName = "";
        public string PropertyName = "";

        public string GetDisplayText()
        {
            switch (Type)
            {
                case RuleType.ByItemId:
                    return $"Item ID: {ItemId}";
                case RuleType.ByCategory:
                    return $"Category: {CategoryName}";
                case RuleType.ByItemType:
                    return $"Type: {ItemTypeName}";
                case RuleType.ByProperty:
                    return $"Property: {PropertyName}";
                default:
                    return "Unknown Rule";
            }
        }
    }

    [Serializable]
    public class SmartChestData
    {
        public string ChestId = "";
        public string ChestName = "";
        public bool IsEnabled = true;
        public List<SmartChestRule> Rules = new List<SmartChestRule>();

        public SmartChestData() { }

        public SmartChestData(string chestId, string chestName)
        {
            ChestId = chestId;
            ChestName = chestName;
            IsEnabled = true;
            Rules = new List<SmartChestRule>();
        }
    }

    [Serializable]
    public class SmartChestSaveData
    {
        public string CharacterName = "";
        public List<SmartChestData> Chests = new List<SmartChestData>();

        public SmartChestSaveData() { }

        public SmartChestSaveData(string characterName)
        {
            CharacterName = characterName;
            Chests = new List<SmartChestData>();
        }
    }

    /// <summary>
    /// Wrapper for JsonUtility serialization (it doesn't handle top-level lists directly).
    /// </summary>
    [Serializable]
    public class SmartChestSaveDataWrapper
    {
        public string CharacterName = "";
        public List<SmartChestDataEntry> Chests = new List<SmartChestDataEntry>();

        public static SmartChestSaveDataWrapper FromData(SmartChestSaveData data)
        {
            var wrapper = new SmartChestSaveDataWrapper
            {
                CharacterName = data.CharacterName,
                Chests = new List<SmartChestDataEntry>()
            };

            foreach (var chest in data.Chests)
            {
                var entry = new SmartChestDataEntry
                {
                    ChestId = chest.ChestId,
                    ChestName = chest.ChestName,
                    IsEnabled = chest.IsEnabled,
                    Rules = new List<SmartChestRuleEntry>()
                };

                foreach (var rule in chest.Rules)
                {
                    entry.Rules.Add(new SmartChestRuleEntry
                    {
                        Type = (int)rule.Type,
                        ItemId = rule.ItemId,
                        CategoryName = rule.CategoryName,
                        ItemTypeName = rule.ItemTypeName,
                        PropertyName = rule.PropertyName
                    });
                }

                wrapper.Chests.Add(entry);
            }

            return wrapper;
        }

        public SmartChestSaveData ToData()
        {
            var data = new SmartChestSaveData
            {
                CharacterName = CharacterName,
                Chests = new List<SmartChestData>()
            };

            foreach (var entry in Chests)
            {
                var chest = new SmartChestData
                {
                    ChestId = entry.ChestId,
                    ChestName = entry.ChestName,
                    IsEnabled = entry.IsEnabled,
                    Rules = new List<SmartChestRule>()
                };

                foreach (var ruleEntry in entry.Rules)
                {
                    chest.Rules.Add(new SmartChestRule
                    {
                        Type = (RuleType)ruleEntry.Type,
                        ItemId = ruleEntry.ItemId,
                        CategoryName = ruleEntry.CategoryName ?? "",
                        ItemTypeName = ruleEntry.ItemTypeName ?? "",
                        PropertyName = ruleEntry.PropertyName ?? ""
                    });
                }

                data.Chests.Add(chest);
            }

            return data;
        }
    }

    [Serializable]
    public class SmartChestDataEntry
    {
        public string ChestId = "";
        public string ChestName = "";
        public bool IsEnabled = true;
        public List<SmartChestRuleEntry> Rules = new List<SmartChestRuleEntry>();
    }

    [Serializable]
    public class SmartChestRuleEntry
    {
        public int Type;
        public int ItemId;
        public string CategoryName = "";
        public string ItemTypeName = "";
        public string PropertyName = "";
    }
}
