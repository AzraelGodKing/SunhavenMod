using System;
using System.Collections.Generic;

namespace SenpaisChest.Data
{
    public enum RuleType
    {
        ByItemId,
        ByCategory,
        ByItemType,
        ByProperty,
        ByGroup
    }

    /// <summary>
    /// Player-defined group of item IDs (e.g. "Flowers", "Vegetables").
    /// Stored per-character, reusable across chests.
    /// </summary>
    [Serializable]
    public class ItemGroup
    {
        public string Name = "";
        public List<int> ItemIds = new List<int>();
    }

    [Serializable]
    public class SmartChestRule
    {
        public RuleType Type;
        public int ItemId;
        public string CategoryName = "";
        public string ItemTypeName = "";
        public string PropertyName = "";
        public string GroupName = "";

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
                case RuleType.ByGroup:
                    return $"Group: {GroupName}";
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
        public bool IsEnabled = false;
        public List<SmartChestRule> Rules = new List<SmartChestRule>();

        public SmartChestData() { }

        public SmartChestData(string chestId, string chestName)
        {
            ChestId = chestId;
            ChestName = chestName;
            IsEnabled = false;
            Rules = new List<SmartChestRule>();
        }
    }

    [Serializable]
    public class SmartChestSaveData
    {
        public string CharacterName = "";
        public List<SmartChestData> Chests = new List<SmartChestData>();
        public List<ItemGroup> Groups = new List<ItemGroup>();

        public SmartChestSaveData() { }

        public SmartChestSaveData(string characterName)
        {
            CharacterName = characterName;
            Chests = new List<SmartChestData>();
            Groups = new List<ItemGroup>();
        }
    }

}