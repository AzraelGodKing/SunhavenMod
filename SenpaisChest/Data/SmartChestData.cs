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

}