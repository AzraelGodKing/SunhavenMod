using System.Collections.Generic;
using System.Linq;
using PSS;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Wish;

[DefaultExecutionOrder(-500)]
public class Inventory : MonoBehaviour
{
	public UnityAction OnInventoryUpdated;

	public UnityAction<int> OnAddedItem;

	public int maxSlots = 50;

	[Header("UI References")]
	[SerializeField]
	protected Transform _inventoryPanel;

	[SerializeField]
	private bool hasMinorTabs = true;

	protected int minorTabIndex;

	protected Slot[] _slots;

	[Header("Starting Items")]
	[SerializeField]
	protected List<ItemInfo> _startingItems = new List<ItemInfo>();

	protected bool _initialized;

	protected ItemIcon currentItemIcon;

	public UnityAction<ItemIcon> onItemIconInitialized;

	private Dictionary<int, int> currentAmounts = new Dictionary<int, int>();

	public bool needToCheckEquipmentSlots;

	private static Dictionary<int, uint> DLCRequirementsDictionary = new Dictionary<int, uint>
	{
		{ 285, 2401500u },
		{ 288, 2401501u },
		{ 287, 2401501u },
		{ 286, 2401500u },
		{ 13158, 2375140u },
		{ 13159, 2375140u },
		{ 13160, 2375140u },
		{ 13157, 2375140u },
		{ 13161, 2375140u },
		{ 13162, 2375130u },
		{ 13163, 2375130u },
		{ 13164, 2375130u },
		{ 13165, 2375130u },
		{ 13166, 2375130u },
		{ 5100, 1667600u },
		{ 13110, 1559320u },
		{ 13109, 1667610u },
		{ 10115, 1667660u },
		{ 5383, 1667620u },
		{ 5701, 1667620u },
		{ 5206, 1667620u },
		{ 5606, 1667620u },
		{ 5006, 1667620u },
		{ 5406, 1667620u }
	};

	public List<SlotItemData> Items { get; protected set; }

	public static ItemIcon CurrentItemIcon { get; set; }

	protected virtual void Start()
	{
		if (!_initialized)
		{
			SetUpInventoryData();
		}
	}

	protected virtual void SetUpInventoryData()
	{
		Items = new List<SlotItemData>();
		_slots = _inventoryPanel.GetComponentsInChildren<Slot>(includeInactive: true);
		maxSlots = Mathf.Min(maxSlots, _slots.Length);
		for (int i = 0; i < _slots.Length; i++)
		{
			if (_slots[i].gameObject.activeSelf)
			{
				_slots[i].slotNumber = i;
				Items.Add(new SlotItemData(new NormalItem(0), 0, i, _slots[i]));
			}
		}
		foreach (ItemInfo startingItem in _startingItems)
		{
			if (startingItem != null)
			{
				AddItem(startingItem.item.id, startingItem.amount, 0, sendNotification: false);
			}
		}
		_initialized = true;
	}

	public virtual void AddItem(Item item, int amount, int slot, bool sendNotification, bool specialItem = true, bool superSecretCheck = true)
	{
		if (amount <= 0)
		{
			return;
		}
		Database.GetData(item.ID(), delegate(ItemData itemData)
		{
			if (!(itemData == null) && (!specialItem || !AddSpecialItem(amount, sendNotification, itemData)) && (!superSecretCheck || SuperSecretMethodIfYouRemoveThisWeWillSue(itemData.id)))
			{
				InformEncylopedia(item);
				if (itemData.stackSize > 1)
				{
					for (int i = slot; i < Mathf.Min(maxSlots, Items.Count); i++)
					{
						if (item.Equals(Items[i].item))
						{
							int num = (Items[i].slot.onlyAcceptSpecificItem ? Items[i].slot.numberOfItemToAccept : itemData.stackSize);
							if (Items[i].amount < num)
							{
								if (Items[i].amount + amount > num)
								{
									int num2 = num - Items[i].amount;
									int amount2 = amount - num2;
									Items[i].amount = num;
									Items[i].id = item.ID();
									if (sendNotification)
									{
										SingletonBehaviour<NotificationStack>.Instance.SendNotification(itemData.UnformattedDisplayName, itemData.id, num2);
									}
									Items[i].slot.GetComponentInChildren<ItemIcon>().UpdateAmount(Items[i].amount);
									currentAmounts.Remove(itemData.id);
									UpdateInventory();
									OnAddedItem?.Invoke(Items[i].id);
									AddItem(item, amount2, 0, sendNotification);
								}
								else
								{
									Items[i].amount += amount;
									Items[i].id = item.ID();
									if (sendNotification)
									{
										SingletonBehaviour<NotificationStack>.Instance.SendNotification(itemData.UnformattedDisplayName, itemData.id, amount);
									}
									Items[i].slot.GetComponentInChildren<ItemIcon>().UpdateAmount(Items[i].amount);
									currentAmounts.Remove(itemData.id);
									UpdateInventory();
									OnAddedItem?.Invoke(Items[i].id);
								}
								return;
							}
						}
					}
				}
				for (int j = slot; j < Mathf.Min(maxSlots, Items.Count); j++)
				{
					if (Items[j].item.ID() == 0 && Items[j].slot.ValidateItem(item.ID()))
					{
						Items[j].item = item.DeepCloneItem();
						Items[j].id = item.ID();
						int num3 = (Items[j].slot.onlyAcceptSpecificItem ? Items[j].slot.numberOfItemToAccept : itemData.stackSize);
						if (amount > num3)
						{
							int num4 = num3 - Items[j].amount;
							int amount3 = amount - num4;
							Items[j].amount = num3;
							if (sendNotification)
							{
								SingletonBehaviour<NotificationStack>.Instance.SendNotification(itemData.UnformattedDisplayName, itemData.id, num4);
							}
							AddItem(item, amount3, 0, sendNotification);
						}
						else
						{
							Items[j].amount = amount;
							if (sendNotification)
							{
								SingletonBehaviour<NotificationStack>.Instance.SendNotification(itemData.UnformattedDisplayName, itemData.id, amount);
							}
						}
						ItemIcon itemIcon = Object.Instantiate(SingletonBehaviour<Prefabs>.Instance.ItemIcon, Items[j].slot.transform);
						Items[j].slot.ModifyItemQuality(Items[j].item);
						itemIcon.Initialize(Items[j]);
						currentAmounts.Remove(itemData.id);
						UpdateInventory();
						OnAddedItem?.Invoke(item.ID());
						return;
					}
				}
				Item item2 = item.DeepCloneItem();
				Pickup.Spawn(Player.Instance.transform.position, item2, amount, homeIn: false, 0.4f, Pickup.BounceAnimation.Normal, 2f, 100f);
			}
		});
	}

	public void NeedToRecalculateAmounts(int id)
	{
		currentAmounts.Remove(id);
	}

	private void InformEncylopedia(Item item)
	{
		if (item != null)
		{
			SingletonBehaviour<GameSave>.Instance.SaveEncylopediaItem(item.ID(), DayCycle.Day);
		}
	}

	private static bool AddSpecialItem(int amount, bool sendNotification, ItemData itemData)
	{
		switch (itemData.id)
		{
		case 60000:
			Player.Instance.AddMoneyAndRegisterSource(amount, 60101, 1, MoneySource.Exploration, playAudio: false);
			if (sendNotification)
			{
				SingletonBehaviour<NotificationStack>.Instance.SendNotification(itemData.UnformattedDisplayName, itemData.id, amount);
			}
			return true;
		case 60001:
			Player.Instance.AddOrbsAndRegisterSource(amount, 60001, 1, MoneySource.Exploration, playAudio: false);
			if (sendNotification)
			{
				SingletonBehaviour<NotificationStack>.Instance.SendNotification(itemData.UnformattedDisplayName, itemData.id, amount);
			}
			return true;
		case 60002:
			Player.Instance.AddTicketsAndRegisterSource(amount, 60002, 1, MoneySource.Exploration, playAudio: false);
			if (sendNotification)
			{
				SingletonBehaviour<NotificationStack>.Instance.SendNotification(itemData.UnformattedDisplayName, itemData.id, amount);
			}
			return true;
		case 60004:
			Player.Instance.AddEXP(ProfessionType.Farming, amount);
			if (sendNotification)
			{
				SingletonBehaviour<NotificationStack>.Instance.SendNotification(itemData.UnformattedDisplayName, itemData.id, amount);
			}
			return true;
		case 60003:
			Player.Instance.AddEXP(ProfessionType.Combat, amount);
			if (sendNotification)
			{
				SingletonBehaviour<NotificationStack>.Instance.SendNotification(itemData.UnformattedDisplayName, itemData.id, amount);
			}
			return true;
		case 60006:
			Player.Instance.AddEXP(ProfessionType.Mining, amount);
			if (sendNotification)
			{
				SingletonBehaviour<NotificationStack>.Instance.SendNotification(itemData.UnformattedDisplayName, itemData.id, amount);
			}
			return true;
		case 60008:
			Player.Instance.AddEXP(ProfessionType.Fishing, amount);
			if (sendNotification)
			{
				SingletonBehaviour<NotificationStack>.Instance.SendNotification(itemData.UnformattedDisplayName, itemData.id, amount);
			}
			return true;
		case 60005:
			Player.Instance.AddEXP(ProfessionType.Exploration, amount);
			if (sendNotification)
			{
				SingletonBehaviour<NotificationStack>.Instance.SendNotification(itemData.UnformattedDisplayName, itemData.id, amount);
			}
			return true;
		case 60007:
			SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.AddStatBonus(StatType.Mana, amount);
			return true;
		case 60009:
			SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.AddStatBonus(StatType.Health, amount);
			return true;
		case 60012:
			SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.AddStatBonus(StatType.Movespeed, amount);
			return true;
		case 60010:
			SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.AddStatBonus(StatType.AttackDamage, amount);
			return true;
		case 60011:
			SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.AddStatBonus(StatType.SpellDamage, amount);
			return true;
		case 1250:
			AudioManager.Instance.PlayAudio(SingletonBehaviour<Prefabs>.Instance.rustyKeySound, 0.55f);
			return false;
		case 6205:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("AnneKeepsake", value: true);
			break;
		case 6204:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("LynnKeepsake", value: true);
			break;
		case 6206:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("DonovanKeepsake", value: true);
			break;
		case 6207:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("VaanKeepsake", value: true);
			break;
		case 6208:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("XylaKeepsake", value: true);
			break;
		case 6209:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("NathanielKeepsake", value: true);
			break;
		case 6210:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("JunKeepsake", value: true);
			break;
		case 6211:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("LiamKeepsake", value: true);
			break;
		case 6212:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("CatherineKeepsake", value: true);
			break;
		case 6213:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("ClaudeKeepsake", value: true);
			break;
		case 6214:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("KittyKeepsake", value: true);
			break;
		case 6215:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("WornhardtKeepsake", value: true);
			break;
		case 6216:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("DariusKeepsake", value: true);
			break;
		case 6217:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("LuciaKeepsake", value: true);
			break;
		case 6218:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("IrisKeepsake", value: true);
			break;
		case 6221:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("WesleyKeepsake", value: true);
			break;
		case 6222:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("KaiKeepsake", value: true);
			break;
		case 6223:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("ViviKeepsake", value: true);
			break;
		case 6225:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("ShangKeepsake", value: true);
			break;
		case 6224:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("MiyeonKeepsake", value: true);
			break;
		case 6226:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("LuciusKeepsake", value: true);
			break;
		case 6228:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("ZariaKeepsake", value: true);
			break;
		case 6227:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("KarishKeepsake", value: true);
			break;
		case 6229:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("ElyssiaKeepsake", value: true);
			break;
		case 6230:
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("ThorianKeepsake", value: true);
			break;
		}
		return false;
	}

	private static bool SuperSecretMethodIfYouRemoveThisWeWillSue(int item)
	{
		return true;
	}

	public virtual void AddItem(int id)
	{
		AddItem(id, 1, sendNotification: true);
	}

	public virtual void AddItem(Item item, int amount, bool sendNotification)
	{
		AddItem(item, amount, 0, sendNotification);
	}

	public virtual void AddItem(int item, int amount, bool sendNotification)
	{
		AddItem(item, amount, 0, sendNotification);
	}

	public virtual void AddItem(int item, int amount, int slot, bool sendNotification, bool specialItem = true)
	{
		Database.GetData(item, delegate(ItemData data)
		{
			AddItem(data.GenerateItem(), amount, slot, sendNotification, specialItem);
		});
	}

	public virtual bool AddItemToFirstOpenSlotIfPossible(Item item, int amount, int minSlot, int maxSlot)
	{
		while (minSlot < Mathf.Min(maxSlot, maxSlots))
		{
			if (Items[minSlot].item.ID() == 0)
			{
				Database.GetData<ItemData>(item.ID(), delegate
				{
					Items[minSlot].item = item.DeepCloneItem();
					Items[minSlot].id = item.ID();
					Items[minSlot].amount = amount;
					ItemIcon itemIcon = Object.Instantiate(SingletonBehaviour<Prefabs>.Instance.ItemIcon, Items[minSlot].slot.transform);
					Items[minSlot].slot.ModifyItemQuality(Items[minSlot].item);
					itemIcon.Initialize(Items[minSlot]);
					UpdateInventory();
				});
				return true;
			}
			minSlot++;
		}
		return false;
	}

	public virtual void AddItemToSpecificSlot(int id, int amount, int slot)
	{
		Database.GetData(id, delegate(ItemData itemData)
		{
			AddItemToSpecificSlot(itemData.GenerateItem(), amount, slot);
		});
	}

	public virtual bool AddItemToSpecificSlot(Item item, int amount, int slot)
	{
		if (Items[slot].item.ID() == 0)
		{
			Database.GetData(item.ID(), delegate(ItemData itemData)
			{
				Items[slot].item = item;
				Items[slot].id = item.ID();
				Items[slot].amount = amount;
				ItemIcon itemIcon = Object.Instantiate(SingletonBehaviour<Prefabs>.Instance.ItemIcon, Items[slot].slot.transform);
				Items[slot].slot.ModifyItemQuality(Items[slot].item);
				itemIcon.Initialize(Items[slot]);
				SingletonBehaviour<NotificationStack>.Instance.SendNotification(itemData.UnformattedDisplayName, itemData.id, amount);
				UpdateInventory();
			});
			return true;
		}
		return false;
	}

	public virtual void RemoveItemAt(int slot)
	{
		ItemIcon componentInChildren = _slots[slot].GetComponentInChildren<ItemIcon>();
		if ((bool)componentInChildren)
		{
			RemoveItemIcon(componentInChildren);
		}
		currentAmounts.Remove(Items[slot].id);
		Items[slot].RemoveItem();
		UpdateInventory();
	}

	public virtual void RemoveItemAt(int slot, int amount)
	{
		if (slot < 0 || slot >= Items.Count)
		{
			return;
		}
		if (Items[slot].amount > amount)
		{
			Items[slot].amount -= amount;
			_slots[slot].GetComponentInChildren<ItemIcon>().UpdateAmount(Items[slot].amount);
			currentAmounts.Remove(Items[slot].id);
		}
		else
		{
			ItemIcon componentInChildren = _slots[slot].GetComponentInChildren<ItemIcon>();
			if ((bool)componentInChildren)
			{
				RemoveItemIcon(componentInChildren);
			}
			currentAmounts.Remove(Items[slot].id);
			Items[slot].RemoveItem();
		}
		UpdateInventory();
	}

	private void RemoveItemIcon(ItemIcon itemObj)
	{
		if (EventSystem.current.currentSelectedGameObject == itemObj.gameObject && (bool)itemObj.slot)
		{
			EventSystem.current.SetSelectedGameObject(null);
			EventSystem.current.SetSelectedGameObject(itemObj.slot.gameObject);
		}
		if ((bool)itemObj.slot)
		{
			itemObj.slot.GetComponent<Selectable>().interactable = true;
			itemObj.slot.inventory.UpdateInventory();
		}
		DestroyUtilities.DestroyDebug(itemObj.gameObject);
	}

	public List<ItemAmount> RemoveItem(Item item, int amount = 1, int slot = 0)
	{
		switch (item.ID())
		{
		case 60200:
			return RemoveFish(ItemRarity.Common, amount);
		case 60201:
			return RemoveFish(ItemRarity.Uncommon, amount);
		case 60202:
			return RemoveFish(ItemRarity.Rare, amount);
		case 60203:
			return RemoveFish(ItemRarity.Epic, amount);
		case 60204:
			return RemoveFish(ItemRarity.Legendary, amount);
		default:
		{
			List<ItemAmount> list = new List<ItemAmount>();
			int num = amount;
			for (int i = slot; i < Mathf.Min(maxSlots, Items.Count); i++)
			{
				if (Items[i].item.ID().Equals(item.ID()))
				{
					Item item2 = Items[i].item;
					int num2 = Mathf.Min(num, Items[i].amount);
					Items[i].amount -= num2;
					num -= num2;
					ItemIcon componentInChildren = Items[i].slot.GetComponentInChildren<ItemIcon>();
					componentInChildren.UpdateAmount(Items[i].amount);
					currentAmounts.Remove(Items[i].id);
					if (Items[i].amount <= 0)
					{
						RemoveItemIcon(componentInChildren);
						Items[i].RemoveItem();
					}
					list.Add(new ItemAmount
					{
						amount = num2,
						item = item2
					});
					UpdateInventory();
					if (num <= 0)
					{
						break;
					}
				}
			}
			return list;
		}
		}
	}

	private List<ItemAmount> RemoveFish(ItemRarity rarity, int amount = 1)
	{
		List<ItemAmount> list = new List<ItemAmount>();
		int num = amount;
		for (int i = 0; i < Mathf.Min(maxSlots, Items.Count); i++)
		{
			if (Items[i].item is FishItem && SingletonBehaviour<ItemInfoDatabase>.Instance.allItemSellInfos[Items[i].id].rarity.Equals(rarity))
			{
				Item item = Items[i].item;
				int num2 = Mathf.Min(num, Items[i].amount);
				Items[i].amount -= num2;
				num -= num2;
				ItemIcon componentInChildren = Items[i].slot.GetComponentInChildren<ItemIcon>();
				componentInChildren.UpdateAmount(Items[i].amount);
				currentAmounts.Remove(Items[i].id);
				if (Items[i].amount <= 0)
				{
					RemoveItemIcon(componentInChildren);
					Items[i].RemoveItem();
				}
				list.Add(new ItemAmount
				{
					amount = num2,
					item = item
				});
				UpdateInventory();
				if (num <= 0)
				{
					break;
				}
			}
		}
		return list;
	}

	public virtual void RemoveAll(Item item)
	{
		for (int i = 0; i < _slots.Length; i++)
		{
			if (Items[i].item.Equals(item))
			{
				Items[i].amount = 0;
				currentAmounts.Remove(Items[i].id);
				ItemIcon componentInChildren = Items[i].slot.GetComponentInChildren<ItemIcon>();
				componentInChildren.UpdateAmount(Items[i].amount);
				RemoveItemIcon(componentInChildren);
				Items[i].RemoveItem();
				UpdateInventory();
				break;
			}
		}
	}

	public virtual List<ItemAmount> RemoveItem(int id, int amount, int slot = 0)
	{
		return RemoveItem(new NormalItem(id), amount, slot);
	}

	public virtual void RemoveAll(int id)
	{
		RemoveAll(new NormalItem(id));
	}

	public virtual void SwapItems(int slot1, int slot2, out ItemIcon newIcon1, out ItemIcon newIcon2)
	{
		Item item = Items[slot1].item;
		int amount = Items[slot1].amount;
		Items[slot1].item = Items[slot2].item;
		Items[slot1].id = Items[slot2].item.ID();
		Items[slot1].amount = Items[slot2].amount;
		Items[slot2].item = item;
		Items[slot2].amount = amount;
		Items[slot2].id = item.ID();
		newIcon1 = SetupItemIcon(slot1);
		newIcon2 = SetupItemIcon(slot2);
		currentAmounts.Remove(Items[slot1].id);
		currentAmounts.Remove(Items[slot2].id);
		UpdateInventory();
		AudioManager.Instance.PlayAudio(SingletonBehaviour<Prefabs>.Instance.pickupItemSound, 0.2f);
	}

	public bool Empty()
	{
		for (int i = 0; i < Mathf.Min(maxSlots, Items.Count); i++)
		{
			if (Items[i].item.ID() != 0)
			{
				return false;
			}
		}
		return true;
	}

	public void Sort(int minSlot, int maxSlot)
	{
		AudioManager.Instance.PlayAudio(SingletonBehaviour<Prefabs>.Instance.sortItems, 0.35f);
		int num = maxSlot - minSlot;
		List<SlotItemData> list = new List<SlotItemData>(num);
		for (int i = minSlot; i < maxSlot; i++)
		{
			SlotItemData slotItemData = Items[i];
			list.Add(new SlotItemData(slotItemData.item, slotItemData.amount, slotItemData.slotNumber, slotItemData.slot));
		}
		list = list.OrderBy((SlotItemData x) => (x.item.ID() == 0) ? 999999999999999L : (x.item.ID() * 10000 + x.amount)).ToList();
		for (int num2 = 0; num2 < num; num2++)
		{
			SlotItemData slotItemData2 = list[num2];
			int num3 = minSlot + num2;
			Items[num3].item = slotItemData2.item;
			Items[num3].id = slotItemData2.item.ID();
			Items[num3].amount = slotItemData2.amount;
			SetupItemIcon(num3);
		}
	}

	public void SortPlayerInventory()
	{
		Sort(10, 50);
	}

	public void Sort()
	{
		Sort(0, Items.Count);
	}

	public int GetFirstEmptySlot()
	{
		for (int i = 10; i < Mathf.Min(maxSlots, Items.Count); i++)
		{
			if (Items[i].item.ID() == 0)
			{
				return i;
			}
		}
		for (int j = 0; j < 10; j++)
		{
			if (Items[j].item.ID() == 0)
			{
				return j;
			}
		}
		return -1;
	}

	public void TransferAllToOtherInventory(Inventory otherInventory)
	{
		for (int num = Mathf.Min(maxSlots - 1, Items.Count - 1); num >= 0; num--)
		{
			SlotItemData slotItemData = Items[num];
			if (slotItemData.item != null && Database.ValidID(slotItemData.item.ID()) && otherInventory.CanAcceptItem(slotItemData.item, slotItemData.amount, out var amountToAccept))
			{
				otherInventory.AddItem(slotItemData.item, amountToAccept, 0, sendNotification: false);
				RemoveItemAt(num, amountToAccept);
			}
		}
	}

	public void TransferPlayerAllToOtherInventory(Inventory otherInventory)
	{
		for (int num = Mathf.Min(maxSlots - 1, Items.Count - 1); num >= 10; num--)
		{
			SlotItemData slotItemData = Items[num];
			if (slotItemData.item != null && Database.ValidID(slotItemData.item.ID()) && otherInventory.CanAcceptItem(slotItemData.item, slotItemData.amount, out var amountToAccept))
			{
				otherInventory.AddItem(slotItemData.item, amountToAccept, 0, sendNotification: false);
				RemoveItemAt(num, amountToAccept);
			}
		}
	}

	public void TransferSimilarToOtherInventorySimple(Inventory otherInventory)
	{
		TransferSimilarToOtherInventory(otherInventory);
	}

	public HashSet<Inventory> TransferSimilarToOtherInventory(Inventory otherInventory)
	{
		HashSet<int> hashSet = new HashSet<int>();
		HashSet<int> hashSet2 = new HashSet<int>();
		HashSet<Inventory> hashSet3 = new HashSet<Inventory>();
		foreach (SlotItemData item in otherInventory.Items)
		{
			for (int num = Mathf.Min(maxSlots - 1, Items.Count - 1); num >= 0; num--)
			{
				SlotItemData slotItemData = Items[num];
				if (slotItemData.item != null && Database.ValidID(slotItemData.item.ID()) && item.item.Equals(slotItemData.item))
				{
					if (otherInventory.CanAcceptItem(slotItemData.item, slotItemData.amount, out var amountToAccept))
					{
						otherInventory.AddItem(slotItemData.item, amountToAccept, 0, sendNotification: false);
						RemoveItemAt(num, amountToAccept);
						hashSet3.Add(otherInventory);
					}
					hashSet.Add(item.item.ID());
				}
			}
		}
		foreach (SlotItemData item2 in Items)
		{
			int id = item2.id;
			if (id <= 0 || !hashSet.Contains(id) || hashSet2.Contains(id))
			{
				continue;
			}
			if (otherInventory.CanAcceptItem(item2.item, item2.amount, out var amountToAccept2))
			{
				if (amountToAccept2 < item2.amount)
				{
					hashSet2.Add(id);
				}
				otherInventory.AddItem(item2.item, amountToAccept2, 0, sendNotification: false);
				RemoveItemAt(item2.slotNumber, amountToAccept2);
				hashSet3.Add(otherInventory);
			}
			else
			{
				hashSet2.Add(id);
			}
		}
		return hashSet3;
	}

	public void TransferPlayerSimilarToOtherInventory(Inventory otherInventory)
	{
		foreach (SlotItemData item in otherInventory.Items)
		{
			for (int num = Mathf.Min(maxSlots - 1, Items.Count - 1); num >= 10; num--)
			{
				SlotItemData slotItemData = Items[num];
				if (slotItemData.item != null && Database.ValidID(slotItemData.item.ID()) && item.item.Equals(slotItemData.item) && otherInventory.CanAcceptItem(slotItemData.item, slotItemData.amount, out var amountToAccept))
				{
					otherInventory.AddItem(slotItemData.item, amountToAccept, 0, sendNotification: false);
					RemoveItemAt(num, amountToAccept);
				}
			}
		}
	}

	public void TransferToNearbyChests()
	{
		HashSet<Inventory> hashSet = new HashSet<Inventory>();
		foreach (Inventory inventory in ChestManager.inventories)
		{
			HashSet<Inventory> other = TransferSimilarToOtherInventory(inventory);
			hashSet.UnionWith(other);
		}
		foreach (Inventory item in hashSet)
		{
			if (ChestManager.associatedChests.TryGetValue(item, out var value))
			{
				value.UpdateChestForMultiplayer();
			}
		}
	}

	public void UpdateInventory()
	{
		needToCheckEquipmentSlots = true;
		OnInventoryUpdated?.Invoke();
	}

	public void LoadInventory(Dictionary<short, InventoryItemData> items)
	{
		if (!_initialized)
		{
			SetUpInventoryData();
		}
		currentAmounts.Clear();
		if (items == null || items.Count <= 0)
		{
			return;
		}
		foreach (KeyValuePair<short, InventoryItemData> item2 in items)
		{
			if (item2.Key >= 0 && item2.Key < Items.Count && item2.Value?.Item != null)
			{
				Item item = item2.Value.Item;
				if (item.ID() == 0 || Database.ValidID(item.ID()))
				{
					ValidateItem(ref item);
					Items[item2.Key].item = item;
					Items[item2.Key].id = item.ID();
					Items[item2.Key].amount = item2.Value.Amount;
					SetupItemIcon(item2.Key);
					InformEncylopedia(item2.Value.Item);
				}
			}
		}
	}

	private void ValidateItem(ref Item item)
	{
	}

	public void ClearInventory()
	{
		if (Items == null)
		{
			return;
		}
		foreach (SlotItemData item in Items)
		{
			RemoveItemAt(item.slotNumber);
		}
	}

	public bool HasEnough(int id, int amount)
	{
		return GetAmount(id) >= amount;
	}

	public bool CanAcceptFullItemStack(Item item, int amount)
	{
		if (item.ID() == 60000 || item.ID() == 60002 || item.ID() == 60001)
		{
			return true;
		}
		for (int num = Mathf.Min(maxSlots, Items.Count) - 1; num >= 0; num--)
		{
			SlotItemData slotItemData = Items[num];
			if (slotItemData.item.ID().Equals(0) && slotItemData.slot.ValidateItem(item.ID()))
			{
				return true;
			}
			if (slotItemData.item.Equals(item) && SingletonBehaviour<ItemInfoDatabase>.Instance.allItemSellInfos[item.ID()].stackSize >= amount + slotItemData.amount)
			{
				return true;
			}
		}
		return false;
	}

	public bool CanAcceptItem(Item item, int amount, out int amountToAccept)
	{
		amountToAccept = 0;
		bool result = false;
		ItemSellInfo itemSellInfo = SingletonBehaviour<ItemInfoDatabase>.Instance.allItemSellInfos[item.ID()];
		for (int num = Mathf.Min(maxSlots - 1, Items.Count - 1); num >= 0; num--)
		{
			SlotItemData slotItemData = Items[num];
			int num2 = (slotItemData.slot.onlyAcceptSpecificItem ? slotItemData.slot.numberOfItemToAccept : itemSellInfo.stackSize);
			if (slotItemData.item.ID().Equals(0) && slotItemData.slot.ValidateItem(item.ID()))
			{
				amountToAccept += Mathf.Min(num2 - slotItemData.amount, amount);
				amount -= amountToAccept;
				result = true;
				if (amount <= 0)
				{
					break;
				}
			}
			if (item.Equals(slotItemData.item))
			{
				amountToAccept += Mathf.Min(num2 - slotItemData.amount, amount);
				amount -= amountToAccept;
				result = true;
				if (amount <= 0)
				{
					break;
				}
			}
		}
		return result;
	}

	public int GetAmount(int id)
	{
		switch (id)
		{
		case 60000:
			return GameSave.Coins;
		case 60001:
			return GameSave.Orbs;
		case 60002:
			return GameSave.Tickets;
		case 60200:
			return GetFishRarityAmount(ItemRarity.Common);
		case 60201:
			return GetFishRarityAmount(ItemRarity.Uncommon);
		case 60202:
			return GetFishRarityAmount(ItemRarity.Rare);
		case 60203:
			return GetFishRarityAmount(ItemRarity.Epic);
		case 60204:
			return GetFishRarityAmount(ItemRarity.Legendary);
		default:
		{
			if (currentAmounts.TryGetValue(id, out var value))
			{
				return value;
			}
			int num = 0;
			for (int i = 0; i < Mathf.Min(maxSlots, Items.Count); i++)
			{
				if (Items[i].id == id)
				{
					num += Items[i].amount;
				}
			}
			currentAmounts[id] = num;
			return num;
		}
		}
	}

	private int GetFishRarityAmount(ItemRarity rarity)
	{
		int num = 0;
		for (int i = 0; i < Mathf.Min(maxSlots, Items.Count); i++)
		{
			if (Items[i].item is FishItem && SingletonBehaviour<ItemInfoDatabase>.Instance.allItemSellInfos[Items[i].item.ID()].rarity.Equals(rarity))
			{
				num += Items[i].amount;
			}
		}
		return num;
	}

	public bool IsSlotFull(int slotIndex)
	{
		SlotItemData slotItemData = Items[slotIndex];
		ItemIcon componentInChildren = slotItemData.slot.GetComponentInChildren<ItemIcon>();
		if ((bool)componentInChildren)
		{
			ItemData itemData = componentInChildren.itemData;
			int num = (slotItemData.slot.onlyAcceptSpecificItem ? slotItemData.slot.numberOfItemToAccept : itemData.stackSize);
			return slotItemData.amount >= num;
		}
		return false;
	}

	private ItemIcon SetupItemIcon(int slot)
	{
		ItemIcon itemIcon;
		if (Items[slot].item.ID() == 0)
		{
			itemIcon = _slots[slot].GetComponentInChildren<ItemIcon>();
			if ((bool)itemIcon)
			{
				RemoveItemIcon(itemIcon);
				itemIcon = null;
			}
		}
		else
		{
			itemIcon = _slots[slot].GetComponentInChildren<ItemIcon>();
			if ((bool)itemIcon)
			{
				itemIcon.Initialize(Items[slot]);
			}
			else
			{
				itemIcon = Object.Instantiate(SingletonBehaviour<Prefabs>.Instance.ItemIcon, Items[slot].slot.transform);
				Items[slot].slot.ModifyItemQuality(Items[slot].item);
				itemIcon.Initialize(Items[slot]);
			}
		}
		return itemIcon;
	}
}
