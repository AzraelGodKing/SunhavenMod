using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using I2.Loc;
using PSS;
using QFSW.QC;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Wish
{
	// Token: 0x02000C72 RID: 3186
	public class Player : Entity, IAggroGenerator, IDamageReceiver, IStatUser, IInitializeOnSceneLoad, IBuffReceiver
	{
		// Token: 0x1700C268 RID: 49768
		// (get) Token: 0x060108DC RID: 67804 RVA: 0x0027DC47 File Offset: 0x0027BE47
		public float MoveSpeed
		{
			get
			{
				return this.moveSpeed;
			}
		}

		// Token: 0x1700C269 RID: 49769
		// (get) Token: 0x060108DD RID: 67805 RVA: 0x0027DC4F File Offset: 0x0027BE4F
		public Transform UseItemsContainer
		{
			get
			{
				return this._useItemsContainer;
			}
		}

		// Token: 0x1700C26A RID: 49770
		// (get) Token: 0x060108DE RID: 67806 RVA: 0x0027DC57 File Offset: 0x0027BE57
		public Transform UseItemTransform
		{
			get
			{
				return this._useItemTransform;
			}
		}

		// Token: 0x1700C26B RID: 49771
		// (get) Token: 0x060108DF RID: 67807 RVA: 0x0027DC60 File Offset: 0x0027BE60
		public UseItem UseItem
		{
			get
			{
				if (this.Spell1 && this.Spell1.Casting)
				{
					return this.Spell1;
				}
				if (this.Spell2 && this.Spell2.Casting)
				{
					return this.Spell2;
				}
				if (this.Spell3 && this.Spell3.Casting)
				{
					return this.Spell3;
				}
				if (this.Spell4 && this.Spell4.Casting)
				{
					return this.Spell4;
				}
				return this._useItem;
			}
		}

		// Token: 0x1700C26C RID: 49772
		// (get) Token: 0x060108E0 RID: 67808 RVA: 0x0027DCF7 File Offset: 0x0027BEF7
		// (set) Token: 0x060108E1 RID: 67809 RVA: 0x0027DCFF File Offset: 0x0027BEFF
		public SpellUseItem Spell1 { get; private set; }

		// Token: 0x1700C26D RID: 49773
		// (get) Token: 0x060108E2 RID: 67810 RVA: 0x0027DD08 File Offset: 0x0027BF08
		// (set) Token: 0x060108E3 RID: 67811 RVA: 0x0027DD10 File Offset: 0x0027BF10
		public SpellUseItem Spell2 { get; private set; }

		// Token: 0x1700C26E RID: 49774
		// (get) Token: 0x060108E4 RID: 67812 RVA: 0x0027DD19 File Offset: 0x0027BF19
		// (set) Token: 0x060108E5 RID: 67813 RVA: 0x0027DD21 File Offset: 0x0027BF21
		public SpellUseItem Spell3 { get; private set; }

		// Token: 0x1700C26F RID: 49775
		// (get) Token: 0x060108E6 RID: 67814 RVA: 0x0027DD2A File Offset: 0x0027BF2A
		// (set) Token: 0x060108E7 RID: 67815 RVA: 0x0027DD32 File Offset: 0x0027BF32
		public SpellUseItem Spell4 { get; private set; }

		// Token: 0x1700C270 RID: 49776
		// (get) Token: 0x060108E8 RID: 67816 RVA: 0x0027DD3B File Offset: 0x0027BF3B
		public Inventory Inventory
		{
			get
			{
				if (!ItemIcon.PrimaryInventory)
				{
					return this._inventory;
				}
				return ItemIcon.PrimaryInventory;
			}
		}

		// Token: 0x1700C271 RID: 49777
		// (get) Token: 0x060108E9 RID: 67817 RVA: 0x0027DD55 File Offset: 0x0027BF55
		public PlayerInventory PlayerInventory
		{
			get
			{
				return this._inventory;
			}
		}

		// Token: 0x1700C272 RID: 49778
		// (get) Token: 0x060108EA RID: 67818 RVA: 0x0027DD5D File Offset: 0x0027BF5D
		public CameraController CameraController
		{
			get
			{
				return this._cameraController;
			}
		}

		// Token: 0x1700C273 RID: 49779
		// (get) Token: 0x060108EB RID: 67819 RVA: 0x0027DD65 File Offset: 0x0027BF65
		public Camera Camera
		{
			get
			{
				return this._playerCamera;
			}
		}

		// Token: 0x1700C274 RID: 49780
		// (get) Token: 0x060108EC RID: 67820 RVA: 0x0027DD6D File Offset: 0x0027BF6D
		public QuestList QuestList
		{
			get
			{
				return this._questList;
			}
		}

		// Token: 0x1700C275 RID: 49781
		// (get) Token: 0x060108ED RID: 67821 RVA: 0x0027DD75 File Offset: 0x0027BF75
		// (set) Token: 0x060108EE RID: 67822 RVA: 0x0027DD7D File Offset: 0x0027BF7D
		public float Health { get; set; }

		// Token: 0x1700C276 RID: 49782
		// (get) Token: 0x060108EF RID: 67823 RVA: 0x0027DD86 File Offset: 0x0027BF86
		public float MaxHealth
		{
			get
			{
				return this.GetStat(StatType.Health);
			}
		}

		// Token: 0x060108F0 RID: 67824 RVA: 0x0027DD90 File Offset: 0x0027BF90
		private void UpdateMaxHealth()
		{
			float num = this.MaxHealth + this.overcappedHealth;
			if (this.PreviousMaxHealth < num)
			{
				this.Health += num - this.PreviousMaxHealth;
			}
			this.PreviousMaxHealth = num;
		}

		// Token: 0x1700C277 RID: 49783
		// (get) Token: 0x060108F1 RID: 67825 RVA: 0x0027DDD0 File Offset: 0x0027BFD0
		public float HealthPercentage
		{
			get
			{
				return this.Health / Mathf.Max(1f, this.MaxHealth);
			}
		}

		// Token: 0x1700C278 RID: 49784
		// (get) Token: 0x060108F2 RID: 67826 RVA: 0x0027DDE9 File Offset: 0x0027BFE9
		// (set) Token: 0x060108F3 RID: 67827 RVA: 0x0027DDF1 File Offset: 0x0027BFF1
		public float Mana { get; set; }

		// Token: 0x1700C279 RID: 49785
		// (get) Token: 0x060108F4 RID: 67828 RVA: 0x0027DDFA File Offset: 0x0027BFFA
		public float MaxMana
		{
			get
			{
				return this.GetStat(StatType.Mana);
			}
		}

		// Token: 0x060108F5 RID: 67829 RVA: 0x0027DE04 File Offset: 0x0027C004
		private void UpdateMaxMana()
		{
			float num = this.MaxMana + this.overcappedMana;
			if (this.PreviousMaxMana < num)
			{
				this.Mana += num - this.PreviousMaxMana;
			}
			this.PreviousMaxMana = num;
		}

		// Token: 0x1700C27A RID: 49786
		// (get) Token: 0x060108F6 RID: 67830 RVA: 0x0027DE44 File Offset: 0x0027C044
		public float ManaPercentage
		{
			get
			{
				return this.Mana / Mathf.Max(1f, this.MaxMana);
			}
		}

		// Token: 0x1700C27B RID: 49787
		// (get) Token: 0x060108F7 RID: 67831 RVA: 0x0027DE5D File Offset: 0x0027C05D
		[HideInInspector]
		public bool pause
		{
			get
			{
				return this._pauseObjects.Count > 0;
			}
		}

		// Token: 0x1700C27C RID: 49788
		// (get) Token: 0x060108F8 RID: 67832 RVA: 0x0027DE6D File Offset: 0x0027C06D
		public PlayerInteractions Interactions
		{
			get
			{
				return this._interactions;
			}
		}

		// Token: 0x1700C27D RID: 49789
		// (get) Token: 0x060108F9 RID: 67833 RVA: 0x0027DE75 File Offset: 0x0027C075
		// (set) Token: 0x060108FA RID: 67834 RVA: 0x0027DE7D File Offset: 0x0027C07D
		public bool IsOwner { get; private set; }

		// Token: 0x1700C27E RID: 49790
		// (get) Token: 0x060108FB RID: 67835 RVA: 0x0027DE86 File Offset: 0x0027C086
		// (set) Token: 0x060108FC RID: 67836 RVA: 0x0027DE8E File Offset: 0x0027C08E
		public bool IsMainMenuPlayer { get; private set; }

		// Token: 0x1700C27F RID: 49791
		// (get) Token: 0x060108FD RID: 67837 RVA: 0x0027DE97 File Offset: 0x0027C097
		// (set) Token: 0x060108FE RID: 67838 RVA: 0x0027DE9F File Offset: 0x0027C09F
		public int ID { get; set; }

		// Token: 0x1700C280 RID: 49792
		// (get) Token: 0x060108FF RID: 67839 RVA: 0x0027DEA8 File Offset: 0x0027C0A8
		public Vector2Int Position
		{
			get
			{
				return new Vector2Int((int)base.transform.position.x, (int)(base.transform.position.y / 1.4142135f + 0.375f));
			}
		}

		// Token: 0x1700C281 RID: 49793
		// (get) Token: 0x06010900 RID: 67840 RVA: 0x0027DEE0 File Offset: 0x0027C0E0
		public Vector2 ExactPosition
		{
			get
			{
				return new Vector2(base.transform.position.x, base.transform.position.y / 1.4142135f + 0.375f - this.graphics.position.z / 1.4142135f);
			}
		}

		// Token: 0x1700C282 RID: 49794
		// (get) Token: 0x06010901 RID: 67841 RVA: 0x0027DF38 File Offset: 0x0027C138
		public Vector2 ExactGraphicsPosition
		{
			get
			{
				return new Vector2(this.graphics.position.x, this.graphics.position.y / 1.4142135f + 0.375f - this.graphics.position.z / 1.4142135f);
			}
		}

		// Token: 0x1700C283 RID: 49795
		// (get) Token: 0x06010902 RID: 67842 RVA: 0x0027DF8D File Offset: 0x0027C18D
		public Vector2 GraphicsPosition
		{
			get
			{
				return this.graphics.transform.position;
			}
		}

		// Token: 0x1700C284 RID: 49796
		// (get) Token: 0x06010903 RID: 67843 RVA: 0x0027DFA4 File Offset: 0x0027C1A4
		public Vector3 OffsetPosition
		{
			get
			{
				return new Vector2(base.transform.position.x, base.transform.position.y + 0.5f);
			}
		}

		// Token: 0x1700C285 RID: 49797
		// (get) Token: 0x06010904 RID: 67844 RVA: 0x0027DFD6 File Offset: 0x0027C1D6
		public PlayerAnimationLayers AnimationLayers
		{
			get
			{
				return this._playerAnimationLayers;
			}
		}

		// Token: 0x1700C286 RID: 49798
		// (get) Token: 0x06010905 RID: 67845 RVA: 0x0027DFDE File Offset: 0x0027C1DE
		// (set) Token: 0x06010906 RID: 67846 RVA: 0x0027DFE6 File Offset: 0x0027C1E6
		public bool Grounded { get; set; } = true;

		// Token: 0x1700C287 RID: 49799
		// (get) Token: 0x06010907 RID: 67847 RVA: 0x0027DFEF File Offset: 0x0027C1EF
		public bool GroundedRecently
		{
			get
			{
				return this.Grounded || Time.time - this.LastGroundedTime < 0.1f;
			}
		}

		// Token: 0x1700C288 RID: 49800
		// (get) Token: 0x06010908 RID: 67848 RVA: 0x0027E00E File Offset: 0x0027C20E
		// (set) Token: 0x06010909 RID: 67849 RVA: 0x0027E016 File Offset: 0x0027C216
		public float LastGroundedTime { get; set; } = -100f;

		// Token: 0x1700C289 RID: 49801
		// (get) Token: 0x0601090A RID: 67850 RVA: 0x0027E01F File Offset: 0x0027C21F
		// (set) Token: 0x0601090B RID: 67851 RVA: 0x0027E027 File Offset: 0x0027C227
		public int AirSkipsUsed { get; set; }

		// Token: 0x1700C28A RID: 49802
		// (get) Token: 0x0601090C RID: 67852 RVA: 0x0027E030 File Offset: 0x0027C230
		// (set) Token: 0x0601090D RID: 67853 RVA: 0x0027E038 File Offset: 0x0027C238
		public int MaxAirSkips { get; set; } = 1;

		// Token: 0x1700C28B RID: 49803
		// (get) Token: 0x0601090E RID: 67854 RVA: 0x0027E041 File Offset: 0x0027C241
		public bool Hurt
		{
			get
			{
				return this._hurtTimer.Value;
			}
		}

		// Token: 0x1700C28C RID: 49804
		// (get) Token: 0x0601090F RID: 67855 RVA: 0x0027E04E File Offset: 0x0027C24E
		// (set) Token: 0x06010910 RID: 67856 RVA: 0x0027E056 File Offset: 0x0027C256
		public bool Invincible { get; set; }

		// Token: 0x1700C28D RID: 49805
		// (get) Token: 0x06010911 RID: 67857 RVA: 0x0000B3E0 File Offset: 0x000095E0
		public bool Chewing
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700C28E RID: 49806
		// (get) Token: 0x06010912 RID: 67858 RVA: 0x0027E05F File Offset: 0x0027C25F
		public Vector2 Velocity
		{
			get
			{
				return this.rigidbody.velocity;
			}
		}

		// Token: 0x1700C28F RID: 49807
		// (get) Token: 0x06010913 RID: 67859 RVA: 0x0027E06C File Offset: 0x0027C26C
		public bool Male
		{
			get
			{
				return SingletonBehaviour<GameSave>.Instance.CurrentSave == null || SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.male;
			}
		}

		// Token: 0x1700C290 RID: 49808
		// (get) Token: 0x06010914 RID: 67860 RVA: 0x0027E090 File Offset: 0x0027C290
		public bool HasHelmet
		{
			get
			{
				return this.PlayerInventory.HasHelmet;
			}
		}

		// Token: 0x1700C291 RID: 49809
		// (get) Token: 0x06010915 RID: 67861 RVA: 0x0027E09D File Offset: 0x0027C29D
		public bool HasHat
		{
			get
			{
				return this.PlayerInventory.HasHat;
			}
		}

		// Token: 0x1700C292 RID: 49810
		// (get) Token: 0x06010916 RID: 67862 RVA: 0x0027E0AA File Offset: 0x0027C2AA
		// (set) Token: 0x06010917 RID: 67863 RVA: 0x0027E0B2 File Offset: 0x0027C2B2
		public int Item { get; private set; }

		// Token: 0x1700C293 RID: 49811
		// (get) Token: 0x06010918 RID: 67864 RVA: 0x0027E0BB File Offset: 0x0027C2BB
		// (set) Token: 0x06010919 RID: 67865 RVA: 0x0027E0C3 File Offset: 0x0027C2C3
		public ItemData ItemData { get; private set; }

		// Token: 0x1700C294 RID: 49812
		// (get) Token: 0x0601091A RID: 67866 RVA: 0x0027E0CC File Offset: 0x0027C2CC
		// (set) Token: 0x0601091B RID: 67867 RVA: 0x0027E0D4 File Offset: 0x0027C2D4
		public int Spell1Item { get; private set; }

		// Token: 0x1700C295 RID: 49813
		// (get) Token: 0x0601091C RID: 67868 RVA: 0x0027E0DD File Offset: 0x0027C2DD
		// (set) Token: 0x0601091D RID: 67869 RVA: 0x0027E0E5 File Offset: 0x0027C2E5
		public int Spell2Item { get; private set; }

		// Token: 0x1700C296 RID: 49814
		// (get) Token: 0x0601091E RID: 67870 RVA: 0x0027E0EE File Offset: 0x0027C2EE
		// (set) Token: 0x0601091F RID: 67871 RVA: 0x0027E0F6 File Offset: 0x0027C2F6
		public int Spell3Item { get; private set; }

		// Token: 0x1700C297 RID: 49815
		// (get) Token: 0x06010920 RID: 67872 RVA: 0x0027E0FF File Offset: 0x0027C2FF
		// (set) Token: 0x06010921 RID: 67873 RVA: 0x0027E107 File Offset: 0x0027C307
		public int Spell4Item { get; private set; }

		// Token: 0x1700C298 RID: 49816
		// (get) Token: 0x06010922 RID: 67874 RVA: 0x0027E110 File Offset: 0x0027C310
		// (set) Token: 0x06010923 RID: 67875 RVA: 0x0027E118 File Offset: 0x0027C318
		public int ItemIndex { get; private set; }

		// Token: 0x1700C299 RID: 49817
		// (get) Token: 0x06010924 RID: 67876 RVA: 0x0027E124 File Offset: 0x0027C324
		public Item CurrentItem
		{
			get
			{
				int itemIndex = this.ItemIndex;
				if (itemIndex - -4 <= 3)
				{
					return Wish.Item.Empty;
				}
				return this.Inventory.Items[this.ItemIndex].item;
			}
		}

		// Token: 0x1700C29A RID: 49818
		// (get) Token: 0x06010925 RID: 67877 RVA: 0x0027E160 File Offset: 0x0027C360
		public bool Mounted
		{
			get
			{
				return this.mount && this.mount.OnMount;
			}
		}

		// Token: 0x1700C29B RID: 49819
		// (get) Token: 0x06010926 RID: 67878 RVA: 0x0027E17C File Offset: 0x0027C37C
		// (set) Token: 0x06010927 RID: 67879 RVA: 0x0027E184 File Offset: 0x0027C384
		public Stats Stats { get; private set; }

		// Token: 0x1700C29C RID: 49820
		// (get) Token: 0x06010928 RID: 67880 RVA: 0x0027E18D File Offset: 0x0027C38D
		// (set) Token: 0x06010929 RID: 67881 RVA: 0x0027E195 File Offset: 0x0027C395
		public Stats FoodStats { get; private set; }

		// Token: 0x1700C29D RID: 49821
		// (get) Token: 0x0601092A RID: 67882 RVA: 0x0027E19E File Offset: 0x0027C39E
		// (set) Token: 0x0601092B RID: 67883 RVA: 0x0027E1A6 File Offset: 0x0027C3A6
		public Stats RelationshipStats { get; private set; }

		// Token: 0x1700C29E RID: 49822
		// (get) Token: 0x0601092C RID: 67884 RVA: 0x0027E1AF File Offset: 0x0027C3AF
		// (set) Token: 0x0601092D RID: 67885 RVA: 0x0027E1B7 File Offset: 0x0027C3B7
		public Stats MiningStats { get; private set; }

		// Token: 0x1700C29F RID: 49823
		// (get) Token: 0x0601092E RID: 67886 RVA: 0x0027E1C0 File Offset: 0x0027C3C0
		// (set) Token: 0x0601092F RID: 67887 RVA: 0x0027E1C8 File Offset: 0x0027C3C8
		public Stats SavedStats { get; private set; }

		// Token: 0x1700C2A0 RID: 49824
		// (get) Token: 0x06010930 RID: 67888 RVA: 0x0027E1D1 File Offset: 0x0027C3D1
		public bool InMineCart
		{
			get
			{
				return this.MineCart != null;
			}
		}

		// Token: 0x1700C2A1 RID: 49825
		// (get) Token: 0x06010931 RID: 67889 RVA: 0x0027E1DF File Offset: 0x0027C3DF
		// (set) Token: 0x06010932 RID: 67890 RVA: 0x0027E1E7 File Offset: 0x0027C3E7
		public Cart MineCart { get; set; }

		// Token: 0x1700C2A2 RID: 49826
		// (get) Token: 0x06010933 RID: 67891 RVA: 0x0027E1F0 File Offset: 0x0027C3F0
		// (set) Token: 0x06010934 RID: 67892 RVA: 0x0027E1F8 File Offset: 0x0027C3F8
		public bool InJumpZone { get; set; }

		// Token: 0x1700C2A3 RID: 49827
		// (get) Token: 0x06010935 RID: 67893 RVA: 0x0027E201 File Offset: 0x0027C401
		// (set) Token: 0x06010936 RID: 67894 RVA: 0x0027E209 File Offset: 0x0027C409
		public bool ControlCameraXY { get; set; } = true;

		// Token: 0x1700C2A4 RID: 49828
		// (get) Token: 0x06010937 RID: 67895 RVA: 0x0027E212 File Offset: 0x0027C412
		// (set) Token: 0x06010938 RID: 67896 RVA: 0x0027E21A File Offset: 0x0027C41A
		public bool IsFishing { get; set; }

		// Token: 0x1700C2A5 RID: 49829
		// (get) Token: 0x06010939 RID: 67897 RVA: 0x0027E223 File Offset: 0x0027C423
		// (set) Token: 0x0601093A RID: 67898 RVA: 0x0027E22B File Offset: 0x0027C42B
		public bool Sleeping { get; set; }

		// Token: 0x1700C2A6 RID: 49830
		// (get) Token: 0x0601093B RID: 67899 RVA: 0x0027E234 File Offset: 0x0027C434
		// (set) Token: 0x0601093C RID: 67900 RVA: 0x0027E23C File Offset: 0x0027C43C
		public bool HasGoldenEggRing { get; set; }

		// Token: 0x1700C2A7 RID: 49831
		// (get) Token: 0x0601093D RID: 67901 RVA: 0x0027E245 File Offset: 0x0027C445
		// (set) Token: 0x0601093E RID: 67902 RVA: 0x0027E24D File Offset: 0x0027C44D
		public bool HasHardwoodRing { get; set; }

		// Token: 0x1700C2A8 RID: 49832
		// (get) Token: 0x0601093F RID: 67903 RVA: 0x0027E256 File Offset: 0x0027C456
		// (set) Token: 0x06010940 RID: 67904 RVA: 0x0027E25E File Offset: 0x0027C45E
		public bool PassedOut { get; set; }

		// Token: 0x1700C2A9 RID: 49833
		// (get) Token: 0x06010941 RID: 67905 RVA: 0x0027E267 File Offset: 0x0027C467
		// (set) Token: 0x06010942 RID: 67906 RVA: 0x0027E26F File Offset: 0x0027C46F
		public bool StandingStill { get; private set; }

		// Token: 0x1700C2AA RID: 49834
		// (get) Token: 0x06010943 RID: 67907 RVA: 0x0027E278 File Offset: 0x0027C478
		// (set) Token: 0x06010944 RID: 67908 RVA: 0x0027E280 File Offset: 0x0027C480
		public bool CompleteOvernight { get; set; }

		// Token: 0x1700C2AB RID: 49835
		// (get) Token: 0x06010945 RID: 67909 RVA: 0x0027E289 File Offset: 0x0027C489
		// (set) Token: 0x06010946 RID: 67910 RVA: 0x0027E291 File Offset: 0x0027C491
		public bool Dying { get; private set; }

		// Token: 0x1700C2AC RID: 49836
		// (get) Token: 0x06010947 RID: 67911 RVA: 0x0027E29A File Offset: 0x0027C49A
		// (set) Token: 0x06010948 RID: 67912 RVA: 0x0027E2A2 File Offset: 0x0027C4A2
		public bool LeftBed { get; set; } = true;

		// Token: 0x1700C2AD RID: 49837
		// (get) Token: 0x06010949 RID: 67913 RVA: 0x0027E2AB File Offset: 0x0027C4AB
		// (set) Token: 0x0601094A RID: 67914 RVA: 0x0027E2B3 File Offset: 0x0027C4B3
		public bool Pathing { get; set; }

		// Token: 0x1700C2AE RID: 49838
		// (get) Token: 0x0601094B RID: 67915 RVA: 0x0027E2BC File Offset: 0x0027C4BC
		public bool ReadyToSleep
		{
			get
			{
				return !Cutscene.Active && !Cutscene.WithinMultipartCutscene && this.AbleToSleep && !this.InMineCart && !GameManager.SceneTransitioning && (!SingletonBehaviour<QuestRewards>.Instance || !SingletonBehaviour<QuestRewards>.Instance.AcceptingRewards);
			}
		}

		// Token: 0x1700C2AF RID: 49839
		// (get) Token: 0x0601094C RID: 67916 RVA: 0x0027E30B File Offset: 0x0027C50B
		public bool InCutscene
		{
			get
			{
				return !Cutscene.Active && !Cutscene.WithinMultipartCutscene;
			}
		}

		// Token: 0x1700C2B0 RID: 49840
		// (get) Token: 0x0601094D RID: 67917 RVA: 0x0027E31E File Offset: 0x0027C51E
		// (set) Token: 0x0601094E RID: 67918 RVA: 0x0027E326 File Offset: 0x0027C526
		public bool AbleToSleep { get; set; } = true;

		// Token: 0x1700C2B1 RID: 49841
		// (get) Token: 0x0601094F RID: 67919 RVA: 0x0027E32F File Offset: 0x0027C52F
		// (set) Token: 0x06010950 RID: 67920 RVA: 0x0027E337 File Offset: 0x0027C537
		public float LastHitTime { get; set; } = -1000f;

		// Token: 0x1700C2B2 RID: 49842
		// (get) Token: 0x06010951 RID: 67921 RVA: 0x0027E340 File Offset: 0x0027C540
		public float TimeSinceLastHit
		{
			get
			{
				return Time.time - this.LastHitTime;
			}
		}

		// Token: 0x1700C2B3 RID: 49843
		// (get) Token: 0x06010952 RID: 67922 RVA: 0x0027E34E File Offset: 0x0027C54E
		// (set) Token: 0x06010953 RID: 67923 RVA: 0x0027E356 File Offset: 0x0027C556
		public float FirstHitWhileOutOfCombat { get; set; }

		// Token: 0x1700C2B4 RID: 49844
		// (get) Token: 0x06010954 RID: 67924 RVA: 0x0027E35F File Offset: 0x0027C55F
		// (set) Token: 0x06010955 RID: 67925 RVA: 0x0027E367 File Offset: 0x0027C567
		public bool InCombat { get; set; }

		// Token: 0x1700C2B5 RID: 49845
		// (get) Token: 0x06010956 RID: 67926 RVA: 0x0027E370 File Offset: 0x0027C570
		public bool Petting
		{
			get
			{
				return this._petting;
			}
		}

		// Token: 0x1700C2B6 RID: 49846
		// (get) Token: 0x06010957 RID: 67927 RVA: 0x0027E378 File Offset: 0x0027C578
		// (set) Token: 0x06010958 RID: 67928 RVA: 0x0027E380 File Offset: 0x0027C580
		public Pet PlayerPet { get; set; }

		// Token: 0x1700C2B7 RID: 49847
		// (get) Token: 0x06010959 RID: 67929 RVA: 0x0027E389 File Offset: 0x0027C589
		public float ActualCameraZoomLevel
		{
			get
			{
				if (!this.OverrideCameraZoomLevel)
				{
					return this.SettingsCameraZoomLevel;
				}
				return this.CameraZoomLevel;
			}
		}

		// Token: 0x1700C2B8 RID: 49848
		// (get) Token: 0x0601095A RID: 67930 RVA: 0x0027E3A0 File Offset: 0x0027C5A0
		// (set) Token: 0x0601095B RID: 67931 RVA: 0x0027E3A8 File Offset: 0x0027C5A8
		public float SettingsCameraZoomLevel { get; set; } = 7.5f;

		// Token: 0x1700C2B9 RID: 49849
		// (get) Token: 0x0601095C RID: 67932 RVA: 0x0027E3B1 File Offset: 0x0027C5B1
		// (set) Token: 0x0601095D RID: 67933 RVA: 0x0027E3B9 File Offset: 0x0027C5B9
		public float CameraZoomLevel
		{
			get
			{
				return this._cameraZoomLevel;
			}
			set
			{
				this._cameraZoomLevel = value;
				Tween tween = this.zoomTween;
				if (tween != null)
				{
					tween.Kill(false);
				}
				this.zoomTween = this._playerCamera.DOOrthoSize(this.ActualCameraZoomLevel, 0.8f).SetEase(Ease.OutQuad);
			}
		}

		// Token: 0x1700C2BA RID: 49850
		// (get) Token: 0x0601095E RID: 67934 RVA: 0x0027E3F6 File Offset: 0x0027C5F6
		// (set) Token: 0x0601095F RID: 67935 RVA: 0x0027E3FE File Offset: 0x0027C5FE
		public bool OverrideCameraZoomLevel { get; set; }

		// Token: 0x1700C2BB RID: 49851
		// (get) Token: 0x06010960 RID: 67936 RVA: 0x0027E407 File Offset: 0x0027C607
		// (set) Token: 0x06010961 RID: 67937 RVA: 0x0027E40F File Offset: 0x0027C60F
		public bool CameraLowerBound { get; set; }

		// Token: 0x1700C2BC RID: 49852
		// (get) Token: 0x06010962 RID: 67938 RVA: 0x0027E418 File Offset: 0x0027C618
		// (set) Token: 0x06010963 RID: 67939 RVA: 0x0027E420 File Offset: 0x0027C620
		public float FinalMovementSpeed { get; private set; } = 1f;

		// Token: 0x1700C2BD RID: 49853
		// (get) Token: 0x06010964 RID: 67940 RVA: 0x0027E429 File Offset: 0x0027C629
		// (set) Token: 0x06010965 RID: 67941 RVA: 0x0027E431 File Offset: 0x0027C631
		private bool OverrideAnimation { get; set; }

		// Token: 0x1700C2BE RID: 49854
		// (get) Token: 0x06010966 RID: 67942 RVA: 0x0027E43A File Offset: 0x0027C63A
		// (set) Token: 0x06010967 RID: 67943 RVA: 0x0027E442 File Offset: 0x0027C642
		private PlayerAnimation Animation { get; set; }

		// Token: 0x1700C2BF RID: 49855
		// (get) Token: 0x06010968 RID: 67944 RVA: 0x0027E44B File Offset: 0x0027C64B
		// (set) Token: 0x06010969 RID: 67945 RVA: 0x0027E453 File Offset: 0x0027C653
		public bool CanFaceNorthSouth { get; set; } = true;

		// Token: 0x1700C2C0 RID: 49856
		// (get) Token: 0x0601096A RID: 67946 RVA: 0x0027E45C File Offset: 0x0027C65C
		// (set) Token: 0x0601096B RID: 67947 RVA: 0x0027E464 File Offset: 0x0027C664
		public bool FreezeWalkAnimations { get; set; }

		// Token: 0x1700C2C1 RID: 49857
		// (get) Token: 0x0601096C RID: 67948 RVA: 0x0027E46D File Offset: 0x0027C66D
		// (set) Token: 0x0601096D RID: 67949 RVA: 0x0027E475 File Offset: 0x0027C675
		public bool FreezeJumpAnimation { get; set; }

		// Token: 0x1700C2C2 RID: 49858
		// (get) Token: 0x0601096E RID: 67950 RVA: 0x0027E480 File Offset: 0x0027C680
		public FishingRod FishingRod
		{
			get
			{
				FishingRod fishingRod = this._useItem as FishingRod;
				if (fishingRod == null)
				{
					return null;
				}
				return fishingRod;
			}
		}

		// Token: 0x1700C2C3 RID: 49859
		// (get) Token: 0x0601096F RID: 67951 RVA: 0x0027E4A0 File Offset: 0x0027C6A0
		public float FishingSkill
		{
			get
			{
				float num = (float)SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Professions[ProfessionType.Fishing].level + this.GetStat(StatType.FishingSkill);
				FishingRod fishingRod = this._useItem as FishingRod;
				return num + (float)((fishingRod != null) ? fishingRod.fishingPower : 0);
			}
		}

		// Token: 0x1700C2C4 RID: 49860
		// (get) Token: 0x06010970 RID: 67952 RVA: 0x0027E4F0 File Offset: 0x0027C6F0
		public float ExplorationSkill
		{
			get
			{
				return (float)SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Professions[ProfessionType.Exploration].level + this.GetStat(StatType.ExplorationSkill);
			}
		}

		// Token: 0x1700C2C5 RID: 49861
		// (get) Token: 0x06010971 RID: 67953 RVA: 0x0027E51C File Offset: 0x0027C71C
		public float MiningSkill
		{
			get
			{
				float num = (float)SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Professions[ProfessionType.Mining].level + this.GetStat(StatType.MiningSkill);
				Pickaxe pickaxe = this._useItem as Pickaxe;
				return num + (float)((pickaxe != null) ? pickaxe.miningSkill : 0);
			}
		}

		// Token: 0x1700C2C6 RID: 49862
		// (get) Token: 0x06010972 RID: 67954 RVA: 0x0027E56C File Offset: 0x0027C76C
		public float WoodcuttingSkill
		{
			get
			{
				float num = (float)SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Professions[ProfessionType.Exploration].level + this.GetStat(StatType.ExplorationSkill);
				Axe axe = this._useItem as Axe;
				return num + (float)((axe != null) ? axe.woodcuttingSkill : 0);
			}
		}

		// Token: 0x1700C2C7 RID: 49863
		// (get) Token: 0x06010973 RID: 67955 RVA: 0x0027E5BC File Offset: 0x0027C7BC
		public float FarmingSkill
		{
			get
			{
				return (float)SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Professions[ProfessionType.Farming].level + this.GetStat(StatType.FarmingSkill);
			}
		}

		// Token: 0x1700C2C8 RID: 49864
		// (get) Token: 0x06010974 RID: 67956 RVA: 0x0027E5E7 File Offset: 0x0027C7E7
		public float SmithingSkill
		{
			get
			{
				return (float)SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Professions[ProfessionType.Mining].level + this.GetStat(StatType.SmithingSkill);
			}
		}

		// Token: 0x1700C2C9 RID: 49865
		// (get) Token: 0x06010975 RID: 67957 RVA: 0x0027E612 File Offset: 0x0027C812
		public float CombatLevel
		{
			get
			{
				return (float)SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Professions[ProfessionType.Combat].level;
			}
		}

		// Token: 0x06010976 RID: 67958 RVA: 0x0027E634 File Offset: 0x0027C834
		public float MaxLevel(ProfessionType professionType)
		{
			int num = 0;
			if (GameManager.Multiplayer)
			{
				using (Dictionary<int, NetworkGamePlayer>.Enumerator enumerator = NetworkLobbyManager.Instance.GamePlayers.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						KeyValuePair<int, NetworkGamePlayer> keyValuePair = enumerator.Current;
						int num2;
						num = Mathf.Max(num, keyValuePair.Value.levels.TryGetValue((int)professionType, out num2) ? num2 : 1);
					}
					goto IL_7D;
				}
			}
			num = SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Professions[professionType].level;
			IL_7D:
			return (float)num;
		}

		// Token: 0x06010977 RID: 67959 RVA: 0x0027E6D0 File Offset: 0x0027C8D0
		protected override void Awake()
		{
			base.Awake();
			this._hurtTimer = base.gameObject.AddComponent<BoolTimer>().Constructor(false);
			this._playerCamera = base.GetComponentInChildren<Camera>();
			this.rigidbody = base.GetComponent<Rigidbody2D>();
			this._movement = base.GetComponent<EntityMovement>();
			this.skillStats = base.GetComponent<SkillStats>();
			this.playerParticles = base.GetComponent<PlayerParticles>();
			this.playerStatistics = new Statistics();
			this._pickup.gameObject.SetActive(false);
			PlayerCostumeHandler playerCostumeHandler = this.costumeHandler;
			if (playerCostumeHandler != null)
			{
				playerCostumeHandler.RemoveCostume();
			}
			this.ResetPlayerMaterial();
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
			Debug.Log("Making player");
		}

		// Token: 0x06010978 RID: 67960 RVA: 0x0027E77E File Offset: 0x0027C97E
		public void SetGraphicsActive(bool active)
		{
			this.graphics.gameObject.SetActive(active);
		}

		// Token: 0x06010979 RID: 67961 RVA: 0x0027E791 File Offset: 0x0027C991
		public void InitializeAsMainMenuPlayer(CharacterData characterData)
		{
			this.IsOwner = true;
			this.IsMainMenuPlayer = true;
			SingletonBehaviour<CharacterClothingStyles>.Instance.SetCharacterStyles(this._playerAnimationLayers, characterData);
			this.SetBaseStats();
		}

		// Token: 0x0601097A RID: 67962 RVA: 0x0027E7B8 File Offset: 0x0027C9B8
		public void InitializeAsOwner(bool host)
		{
			if (this.initialized)
			{
				return;
			}
			Player.Instance = this;
			this.IsOwner = true;
			Debug.Log("Initializing as Owner");
			this.facingDirection = Direction.South;
			if (this.Inventory)
			{
				this.SetSpell((ushort)SingletonBehaviour<GameSave>.Instance.GetProgressIntCharacter("Spell1"), 1, true);
				this.SetSpell((ushort)SingletonBehaviour<GameSave>.Instance.GetProgressIntCharacter("Spell2"), 2, true);
				this.SetSpell((ushort)SingletonBehaviour<GameSave>.Instance.GetProgressIntCharacter("Spell3"), 3, true);
				this.SetSpell((ushort)SingletonBehaviour<GameSave>.Instance.GetProgressIntCharacter("Spell4"), 4, true);
			}
			this.CalculateProfessionLevels();
			CharacterData.onLevelUp = (UnityAction<ProfessionType, int>)Delegate.Combine(CharacterData.onLevelUp, new UnityAction<ProfessionType, int>(this.LevelUp));
			this._objectsToInitializeAsOwner = base.GetComponentsInChildren<IPlayerOwnerInitialized>(true);
			foreach (IPlayerOwnerInitialized playerOwnerInitialized in this._objectsToInitializeAsOwner)
			{
				try
				{
					playerOwnerInitialized.Initialize();
				}
				catch (Exception message)
				{
					Debug.LogError(message);
				}
			}
			SingletonBehaviour<GameSave>.Instance.LoadGame();
			SingletonBehaviour<CharacterClothingStyles>.Instance.SetCharacterStyles(this._playerAnimationLayers, SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData);
			this._playerAnimationLayers.race = SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.race;
			this._playerAnimationLayers.male = SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.male;
			if (SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.pet != null)
			{
				Debug.Log("Loading player pet");
				SingletonBehaviour<PetManager>.Instance.SpawnPet(this, SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.pet, null);
			}
			this.Initialize();
			this.SetBaseStats();
			UnityAction onPlayerInitializedAsOwner = Player.OnPlayerInitializedAsOwner;
			if (onPlayerInitializedAsOwner != null)
			{
				onPlayerInitializedAsOwner();
			}
			Debug.Log("Setup OnPlayerInitializedAsOwner");
			this.GetStatByNumberEaten();
			this.PreviousMaxHealth = this.MaxHealth;
			this.PreviousMaxMana = this.MaxMana;
			SingletonBehaviour<GameSave>.Instance.LoadPlayerStats();
			if (this.Mana <= 0f)
			{
				this.Mana = this.GetStat(StatType.Mana);
			}
			if (this.Health <= 0f)
			{
				this.Health = this.GetStat(StatType.Health);
			}
			this.HandleQuestProgress();
			this.HandlePreviouslyAcquiredAchievements();
			this.HandleMuseumProgress();
			DOTween.KillAll(false);
			DOTween.Init(null, null, null);
			this.initialized = true;
			if (!GameManager.Multiplayer)
			{
				QuantumConsole instance = QuantumConsole.Instance;
				instance.onSentText = (UnityAction<string>)Delegate.Combine(instance.onSentText, new UnityAction<string>(delegate(string x)
				{
					this.DisplayChatBubble(x);
					this.SendChatMessage(SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.characterName, x);
				}));
			}
			Debug.Log("Finished Player Initialization");
		}

		// Token: 0x0601097B RID: 67963 RVA: 0x0027EA68 File Offset: 0x0027CC68
		private void HandleQuestProgress()
		{
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("JourneyToWithergate6Quest") && !this.QuestList.HasQuestOrCompletedQuest("ConfrontingDynus1Quest"))
			{
				this.QuestList.StartQuest("ConfrontingDynus1Quest", false);
			}
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("TheSunDragonsProtection9Quest") && !this.QuestList.HasQuestOrCompletedQuest("PathToNelvari1Quest"))
			{
				this.QuestList.StartQuest("PathToNelvari1Quest", false);
			}
			if (this.QuestList.HasQuest("TheMysteryOfNelvari1Quest"))
			{
				this.QuestList.AbandonQuest("TheMysteryOfNelvari1Quest");
			}
			if (this.QuestList.HasQuest("TheMysteryOfNelvari2Quest"))
			{
				this.QuestList.AbandonQuest("TheMysteryOfNelvari2Quest");
			}
			if (this.QuestList.HasQuest("TheMysteryOfNelvari3Quest"))
			{
				this.QuestList.AbandonQuest("TheMysteryOfNelvari3Quest");
			}
			if (this.QuestList.HasQuest("TheMysteryOfNelvari4Quest"))
			{
				this.QuestList.AbandonQuest("TheMysteryOfNelvari4Quest");
			}
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("NewJourneyToWithergateCutscene1"))
			{
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("NorthPassOpened", true);
			}
			if (this.QuestList.HasQuest("Intro1Quest"))
			{
				this.QuestList.AbandonQuest("Intro1Quest");
				this.QuestList.StartQuest("WelcomeToSunHaven3Quest", false);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("NewTrainIntroCutscene", true);
			}
			if (this.QuestList.HasQuest("Intro2Quest"))
			{
				this.QuestList.AbandonQuest("Intro2Quest");
				this.QuestList.StartQuest("WelcomeToSunHaven4Quest", false);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("NewTrainIntroCutscene", true);
			}
			if (this.QuestList.HasQuest("Intro3Quest"))
			{
				this.QuestList.AbandonQuest("Intro3Quest");
				this.QuestList.StartQuest("DealingWithADragon1Quest", false);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("NewTrainIntroCutscene", true);
			}
			if (this.QuestList.HasQuest("ClearingTheRoad1Quest"))
			{
				this.QuestList.AbandonQuest("ClearingTheRoad1Quest");
				this.QuestList.StartQuest("NewJourneyToWithergate1Quest", false);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("Charon", false);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("NewTrainIntroCutscene", true);
			}
			if (this.QuestList.HasQuest("ClearingTheRoad2Quest"))
			{
				this.QuestList.AbandonQuest("ClearingTheRoad2Quest");
				this.QuestList.StartQuest("NewJourneyToWithergate1Quest", false);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("Charon", false);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("NewTrainIntroCutscene", true);
			}
			if (this.QuestList.HasQuest("JourneyToWithergate1Quest"))
			{
				this.QuestList.AbandonQuest("JourneyToWithergate1Quest");
				this.QuestList.StartQuest("NewJourneyToWithergate1Quest", false);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("Charon", false);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("NewTrainIntroCutscene", true);
			}
			if (this.QuestList.HasQuest("JourneyToWithergate2Quest"))
			{
				this.QuestList.AbandonQuest("JourneyToWithergate2Quest");
				this.QuestList.StartQuest("NewJourneyToWithergate1Quest", false);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("Charon", false);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("NewTrainIntroCutscene", true);
			}
			if (this.QuestList.HasQuest("JourneyToWithergate3Quest"))
			{
				this.QuestList.AbandonQuest("JourneyToWithergate3Quest");
				this.QuestList.StartQuest("NewJourneyToWithergate1Quest", false);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("Charon", false);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("NewTrainIntroCutscene", true);
			}
			if (this.QuestList.HasQuest("JourneyToWithergate4Quest"))
			{
				this.QuestList.AbandonQuest("JourneyToWithergate4Quest");
				this.QuestList.StartQuest("NewJourneyToWithergate1Quest", false);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("Charon", false);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("NewTrainIntroCutscene", true);
			}
			if (this.QuestList.HasQuest("JourneyToWithergate5Quest"))
			{
				this.QuestList.AbandonQuest("JourneyToWithergate5Quest");
				this.QuestList.StartQuest("NewJourneyToWithergate1Quest", false);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("Charon", false);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("NewTrainIntroCutscene", true);
			}
			if (this.QuestList.HasQuest("JourneyToWithergate6Quest"))
			{
				this.QuestList.AbandonQuest("JourneyToWithergate6Quest");
				this.QuestList.StartQuest("NewJourneyToWithergate1Quest", false);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("Charon", false);
			}
			if (this.QuestList.HasQuest("ConfrontingDynus1Quest") || this.QuestList.HasQuest("ConfrontingDynus2Quest") || this.QuestList.HasQuest("ConfrontingDynus3Quest"))
			{
				this.QuestList.AbandonQuest("ConfrontingDynus1Quest");
				this.QuestList.AbandonQuest("ConfrontingDynus2Quest");
				this.QuestList.AbandonQuest("ConfrontingDynus3Quest");
				this.QuestList.StartQuest("TheKingsFavor1Quest", false);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("NewTrainIntroCutscene", true);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("ClearedDarkSigil", true);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("SnakeDefeated", true);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("NewJourneyToWithergateCutscene13", true);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("NewJourneyToWithergateCutscene2", true);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("NewJourneyToWithergateCutscene4", true);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("UnwelcomeWelcomingCutscene2", true);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("UnwelcomeWelcomingCutscene3", true);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("TheKingsFavorCutscene1", true);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("TheKingsFavorCutscene2A", true);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("NewTrainIntroCutscene", true);
			}
			if (this.QuestList.HasQuestOrCompletedQuest("ConfrontingDynus4Quest") || this.QuestList.HasQuestOrCompletedQuest("ConfrontingDynus5Quest") || this.QuestList.HasQuestOrCompletedQuest("ConfrontingDynus6Quest"))
			{
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("ClearedDarkSigil", true);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("TheKingsFavorCutscene12", true);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("SnakeDefeated", true);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("NewJourneyToWithergateCutscene13", true);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("NewJourneyToWithergateCutscene2", true);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("NewJourneyToWithergateCutscene4", true);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("UnwelcomeWelcomingCutscene2", true);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("UnwelcomeWelcomingCutscene3", true);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("TheKingsFavorCutscene1", true);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("TheKingsFavorCutscene2A", true);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("NewTrainIntroCutscene", true);
			}
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("CollectedWithergateCrystals"))
			{
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("NewCollectedWithergateCrystals", true);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("TheKingsFavorCutscene3", true);
			}
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("UnwelcomeWelcoming1Quest"))
			{
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("UnlockedWithergate", true);
			}
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("TheKingsFavor8Quest"))
			{
				Player.Instance.QuestList.AbandonQuest("TheKingsFavor7AQuest");
				Player.Instance.QuestList.AbandonQuest("TheKingsFavor7BQuest");
			}
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("NivarasLessonGrowthCutscene3"))
			{
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("UnlockedWesley", true);
			}
			if (this.MaxLevel() >= 15 && this.QuestList.HasQuest("TheSunDragonsProtection3Quest"))
			{
				DOVirtual.DelayedCall(1.25f, delegate
				{
					MainQuestLineManager mainQuestLineManager = UnityEngine.Object.FindObjectOfType<MainQuestLineManager>();
					if (mainQuestLineManager)
					{
						mainQuestLineManager.StartPart2();
					}
				}, true);
			}
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("TheSunDragonsProtection3Quest"))
			{
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("UnlockedSHTeleport", true);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("UnlockedTeleport", true);
			}
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("ANewHome5Quest"))
			{
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("UnlockedNVTeleport", true);
			}
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("UnwelcomeWelcoming3Quest"))
			{
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("UnlockedWGTeleport", true);
			}
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("DarkWaters7Quest"))
			{
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("UnlockedBSDTeleport", true);
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("UnlockedTeleport", true);
			}
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("TimeOfNeedCutscene2") && !this.QuestList.HasQuestOrCompletedQuest("TracksToTheCityQuest"))
			{
				Player.Instance.QuestList.StartQuest("TracksToTheCityQuest", false);
			}
			if (this.QuestList.HasQuestOrCompletedQuest("SpeakToCristusQuest"))
			{
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("UnlockedGCApartment", true);
				this.QuestList.AbandonQuest("SpeakToCristusQuest");
			}
		}

		// Token: 0x0601097C RID: 67964 RVA: 0x0027F2EC File Offset: 0x0027D4EC
		private void HandlePreviouslyAcquiredAchievements()
		{
			List<int> list = new List<int>();
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("AppeasedDynus"))
			{
				list.Add(22);
			}
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("BeatDynus"))
			{
				list.Add(23);
			}
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("DynusAltarMining"))
			{
				list.Add(24);
			}
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("DynusAltarFishing"))
			{
				list.Add(25);
			}
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("DynusAltarFarming"))
			{
				list.Add(26);
			}
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("DynusAltarForaging"))
			{
				list.Add(27);
			}
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("DynusAltarGold"))
			{
				list.Add(28);
			}
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("DynusAltarWithergate"))
			{
				list.Add(29);
			}
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("DynusAltarFruit"))
			{
				list.Add(30);
			}
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("DynusAltarCooking"))
			{
				list.Add(31);
			}
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("DynusAltarRareItems"))
			{
				list.Add(32);
			}
			bool flag;
			if (SingletonBehaviour<GameSave>.Instance.TryGetProgressBoolCharacter("AcceptGloriteThug", out flag))
			{
				list.Add(flag ? 37 : 38);
			}
			Utilities.UnlockMultipleAcheivements(list);
		}

		// Token: 0x0601097D RID: 67965 RVA: 0x0027F43C File Offset: 0x0027D63C
		private void HandleMuseumProgress()
		{
			if (GameManager.Host && !SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("FixedMuseumProgress"))
			{
				int progressIntWorld = SingletonBehaviour<GameSave>.Instance.GetProgressIntWorld("MuseumProgress");
				SingletonBehaviour<GameSave>.Instance.SetProgressIntCharacter("MuseumProgress", progressIntWorld);
				foreach (ValueTuple<string, int> valueTuple in MuseumCurator.culturalMuseumProgress)
				{
					SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter(valueTuple.Item1, SingletonBehaviour<GameSave>.Instance.GetProgressBoolWorld(valueTuple.Item1));
				}
				foreach (ValueTuple<string, int> valueTuple2 in MuseumCurator.aquaticMuseumProgress)
				{
					SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter(valueTuple2.Item1, SingletonBehaviour<GameSave>.Instance.GetProgressBoolWorld(valueTuple2.Item1));
				}
				foreach (ValueTuple<string, int> valueTuple3 in MuseumCurator.miningMuseumProgress)
				{
					SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter(valueTuple3.Item1, SingletonBehaviour<GameSave>.Instance.GetProgressBoolWorld(valueTuple3.Item1));
				}
				foreach (ValueTuple<string, int> valueTuple4 in AltarRoomRewardCutscene.altarProgress)
				{
					SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter(valueTuple4.Item1, SingletonBehaviour<GameSave>.Instance.GetProgressBoolWorld(valueTuple4.Item1));
				}
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("FixedMuseumProgress", true);
			}
		}

		// Token: 0x0601097E RID: 67966 RVA: 0x0027F60C File Offset: 0x0027D80C
		public void InitializeAsClient()
		{
			Debug.Log("Initializing as Client");
			GameObject[] objectsToDestroyAsClient = this._objectsToDestroyAsClient;
			for (int i = 0; i < objectsToDestroyAsClient.Length; i++)
			{
				DestroyUtilities.DestroyDebug(objectsToDestroyAsClient[i]);
			}
			Component[] componentsToDestroyAsClient = this._componentsToDestroyAsClient;
			for (int i = 0; i < componentsToDestroyAsClient.Length; i++)
			{
				DestroyUtilities.DestroyDebug(componentsToDestroyAsClient[i]);
			}
			this.SetBaseStats();
		}

		// Token: 0x0601097F RID: 67967 RVA: 0x0027F664 File Offset: 0x0027D864
		public void SetBaseStats()
		{
			this.Stats = new Stats();
			this.Stats.Set(new Stat(StatType.HealthRegen, 0.025f));
			this.Stats.Set(new Stat(StatType.ManaRegen, 0.2f));
			this.Stats.Set(new Stat(StatType.AttackSpeed, 1f));
			this.Stats.Set(new Stat(StatType.SpellAttackSpeed, 1f));
			this.Stats.Set(new Stat(StatType.Movespeed, 1.22f));
			this.Stats.Set(new Stat(StatType.Jump, 1f));
			this.Stats.Set(new Stat(StatType.AttackDamage, 0f));
			this.Stats.Set(new Stat(StatType.Health, 30f));
			this.Stats.Set(new Stat(StatType.Mana, 20f));
			this.Stats.Set(new Stat(StatType.Crit, 0.1f));
			this.Stats.Set(new Stat(StatType.MiningCrit, 0.1f));
			this.Stats.Set(new Stat(StatType.WoodcuttingCrit, 0.1f));
			this.FoodStats = new Stats();
			this.RelationshipStats = new Stats();
			this.MiningStats = new Stats();
			this.SavedStats = new Stats();
			this.ResetHealthAndMana();
		}

		// Token: 0x06010980 RID: 67968 RVA: 0x0027F7BA File Offset: 0x0027D9BA
		private void ResetHealthAndMana()
		{
			this.Health = this.GetStat(StatType.Health);
			this.Mana = this.GetStat(StatType.Mana);
		}

		// Token: 0x06010981 RID: 67969 RVA: 0x0027F7D8 File Offset: 0x0027D9D8
		private void AddHealthAndManaOvernight()
		{
			float num = 360f;
			float num2 = 1.05f + 0.15f * (float)GameSave.Combat.GetNodeAmount("Combat8d", 3, true);
			if (GameSave.Combat.GetNode("Combat8d", true))
			{
				num *= num2;
			}
			this.AddMana(this.GetStat(StatType.ManaRegen) * num + this.MaxMana * 0.4f * num2, 1f);
			this.Heal(this.GetStat(StatType.HealthRegen) * num + this.MaxHealth * 0.4f * num2, true, 1f);
		}

		// Token: 0x06010982 RID: 67970 RVA: 0x0027F868 File Offset: 0x0027DA68
		public void GetStatByNumberEaten()
		{
			this.FoodStats = new Stats();
			this.calculationCount++;
			int currentCalculationCount = this.calculationCount;
			Action<FoodData> <>9__0;
			foreach (KeyValuePair<int, int> keyValuePair in SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.foodStats)
			{
				int key = keyValuePair.Key;
				Action<FoodData> onItemLoaded;
				if ((onItemLoaded = <>9__0) == null)
				{
					onItemLoaded = (<>9__0 = delegate(FoodData data)
					{
						if (currentCalculationCount != this.calculationCount)
						{
							return;
						}
						data.AddPlayerStatByNumberEaten();
					});
				}
				Database.GetData<FoodData>(key, onItemLoaded, null);
			}
		}

		// Token: 0x06010983 RID: 67971 RVA: 0x0027F920 File Offset: 0x0027DB20
		public void GetRelationshipStats()
		{
			this.RelationshipStats = new Stats();
			if (GameSave.Exploration.GetNode("Exploration8d", true))
			{
				int num = 0;
				foreach (KeyValuePair<string, float> keyValuePair in SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Relationships)
				{
					NPCAI npcai;
					if (SingletonBehaviour<NPCManager>.Instance._npcs.TryGetValue(keyValuePair.Key, out npcai) && npcai.Romanceable)
					{
						num += (int)(keyValuePair.Value / 5f);
					}
				}
				float num2 = (float)num * (0.25f + 0.25f * (float)GameSave.Exploration.GetNodeAmount("Exploration8d", 3, true));
				num2 = Mathf.Min(50f, num2);
				this.RelationshipStats.Add(new Stat(StatType.Mana, num2));
			}
		}

		// Token: 0x06010984 RID: 67972 RVA: 0x0027FA10 File Offset: 0x0027DC10
		protected virtual void Update()
		{
			if (!this.IsOwner)
			{
				return;
			}
			if (Cutscene.Active || this.Pathing)
			{
				this._interactions.enabled = false;
				this._interactions.ClearInteractables();
				this._hitBox.enabled = false;
			}
			else
			{
				if (this._interactions)
				{
					this._interactions.enabled = true;
				}
				this._hitBox.enabled = true;
				this.HandleInput();
				if (Player.Instance == this && !UIHandler.InventoryOpen && !this.pause && !this.Sleeping && !GameManager.SceneTransitioning)
				{
					this._useItem = this.UseItemTransform.GetComponentInChildren<UseItem>();
					this.Spell1 = this._spell1Transform.GetComponentInChildren<SpellUseItem>();
					this.Spell2 = this._spell2Transform.GetComponentInChildren<SpellUseItem>();
					this.Spell3 = this._spell3Transform.GetComponentInChildren<SpellUseItem>();
					this.Spell4 = this._spell4Transform.GetComponentInChildren<SpellUseItem>();
					if (MouseVisualManager.UsingController || (EventSystem.current && !EventSystem.current.IsPointerOverGameObject() && PlayerInput.AllowChangeActionBarItem))
					{
						this.spell1NotCasting = (this.Spell1 == null || !this.Spell1.Casting);
						this.spell2NotCasting = (this.Spell2 == null || !this.Spell2.Casting);
						this.spell3NotCasting = (this.Spell3 == null || !this.Spell3.Casting);
						this.spell4NotCasting = (this.Spell4 == null || !this.Spell4.Casting);
						this.useItemNotUsing = (this._useItem == null || !this._useItem.Using);
						if (this.useItemNotUsing && this.spell2NotCasting && this.spell3NotCasting && this.spell4NotCasting)
						{
							if (PlayerInput.GetButtonDown(Button.Spell1, false))
							{
								if (this.Spell1 != null)
								{
									this.Spell1.UseDown1();
								}
								else
								{
									UIHandler.Instance.OpenInventoryAndStartSettingSpell(0);
								}
							}
							if (PlayerInput.GetButtonUp(Button.Spell1, false))
							{
								SpellUseItem spell = this.Spell1;
								if (spell != null)
								{
									spell.UseUp1();
								}
							}
							if (PlayerInput.GetButton(Button.Spell1, false))
							{
								SpellUseItem spell2 = this.Spell1;
								if (spell2 != null)
								{
									spell2.Use1();
								}
							}
						}
						if (this.useItemNotUsing && this.spell1NotCasting && this.spell3NotCasting && this.spell4NotCasting)
						{
							if (PlayerInput.GetButtonDown(Button.Spell2, false))
							{
								if (this.Spell2 != null)
								{
									this.Spell2.UseDown1();
								}
								else
								{
									UIHandler.Instance.OpenInventoryAndStartSettingSpell(1);
								}
							}
							if (PlayerInput.GetButtonUp(Button.Spell2, false))
							{
								SpellUseItem spell3 = this.Spell2;
								if (spell3 != null)
								{
									spell3.UseUp1();
								}
							}
							if (PlayerInput.GetButton(Button.Spell2, false))
							{
								SpellUseItem spell4 = this.Spell2;
								if (spell4 != null)
								{
									spell4.Use1();
								}
							}
						}
						if (this.useItemNotUsing && this.spell1NotCasting && this.spell2NotCasting && this.spell4NotCasting)
						{
							if (PlayerInput.GetButtonDown(Button.Spell3, false))
							{
								if (this.Spell3 != null)
								{
									this.Spell3.UseDown1();
								}
								else
								{
									UIHandler.Instance.OpenInventoryAndStartSettingSpell(2);
								}
							}
							if (PlayerInput.GetButtonUp(Button.Spell3, false))
							{
								SpellUseItem spell5 = this.Spell3;
								if (spell5 != null)
								{
									spell5.UseUp1();
								}
							}
							if (PlayerInput.GetButton(Button.Spell3, false))
							{
								SpellUseItem spell6 = this.Spell3;
								if (spell6 != null)
								{
									spell6.Use1();
								}
							}
						}
						if (this.useItemNotUsing && this.spell1NotCasting && this.spell2NotCasting && this.spell3NotCasting)
						{
							if (PlayerInput.GetButtonDown(Button.Spell4, false))
							{
								if (this.Spell4 != null)
								{
									this.Spell4.UseDown1();
								}
								else
								{
									UIHandler.Instance.OpenInventoryAndStartSettingSpell(3);
								}
							}
							if (PlayerInput.GetButtonUp(Button.Spell4, false))
							{
								SpellUseItem spell7 = this.Spell4;
								if (spell7 != null)
								{
									spell7.UseUp1();
								}
							}
							if (PlayerInput.GetButton(Button.Spell4, false))
							{
								SpellUseItem spell8 = this.Spell4;
								if (spell8 != null)
								{
									spell8.Use1();
								}
							}
						}
						this.spell1NotCasting = (this.Spell1 == null || !this.Spell1.Casting);
						this.spell2NotCasting = (this.Spell2 == null || !this.Spell2.Casting);
						this.spell3NotCasting = (this.Spell3 == null || !this.Spell3.Casting);
						this.spell4NotCasting = (this.Spell4 == null || !this.Spell4.Casting);
						this.useItemNotUsing = (this._useItem == null || !this._useItem.Using);
						if (this.CanUseUseItem)
						{
							if (PlayerInput.GetButtonDown(Button.Swing, false) || (PlayerInput.GetButtonDown("RightSwing") && !this.controllerOffset.PlacementMode))
							{
								UseItem useItem = this._useItem;
								if (useItem != null)
								{
									useItem.UseDown1();
								}
							}
							if (PlayerInput.GetButtonUp(Button.Swing, false) || (PlayerInput.GetButtonUp("RightSwing") && !this.controllerOffset.PlacementMode))
							{
								UseItem useItem2 = this._useItem;
								if (useItem2 != null)
								{
									useItem2.UseUp1();
								}
							}
							if (PlayerInput.GetButton(Button.Swing, false) || (PlayerInput.GetButton("RightSwing") && !this.controllerOffset.PlacementMode))
							{
								UseItem useItem3 = this._useItem;
								if (useItem3 != null)
								{
									useItem3.Use1();
								}
							}
							if (!MouseVisualManager.UsingController || !SingletonBehaviour<HelpTooltips>.Instance.HasNotifications)
							{
								if (PlayerInput.GetButton(Button.Use2, false))
								{
									UseItem useItem4 = this._useItem;
									if (useItem4 != null)
									{
										useItem4.Use2();
									}
								}
								if (PlayerInput.GetButtonDown(Button.Use2, false))
								{
									UseItem useItem5 = this._useItem;
									if (useItem5 != null)
									{
										useItem5.UseDown2();
									}
								}
								if (PlayerInput.GetButtonUp(Button.Use2, false))
								{
									UseItem useItem6 = this._useItem;
									if (useItem6 != null)
									{
										useItem6.UseUp2();
									}
								}
							}
						}
					}
					if (PlaySettingsManager.PlaySettings.enableEmotes)
					{
						if (PlayerInput.GetButtonDown("Emote1"))
						{
							this.Emote(1);
						}
						if (PlayerInput.GetButtonDown("Emote2"))
						{
							this.Emote(2);
						}
						if (PlayerInput.GetButtonDown("Emote3"))
						{
							this.Emote(3);
						}
						if (PlayerInput.GetButtonDown("Emote4"))
						{
							this.Emote(5);
						}
						if (PlayerInput.GetButtonDown("Emote5"))
						{
							this.Emote(6);
						}
						if (PlayerInput.GetButtonDown("Emote6"))
						{
							this.Emote(7);
						}
					}
					if (PlayerInput.GetButtonDown(Button.Interact, false))
					{
						IInteractable firstInteractable = this._interactions.FirstInteractable;
						if (firstInteractable != null)
						{
							firstInteractable.Interact(0);
						}
					}
					if (PlayerInput.GetButtonDown("Pickup"))
					{
						IInteractable firstInteractable2 = this._interactions.FirstInteractable;
						if (firstInteractable2 != null)
						{
							firstInteractable2.Interact(1);
						}
					}
					if (PlayerInput.AllowInput && PlayerInput.GetButtonDown(Button.Escape, true))
					{
						this.EndInteraction();
					}
				}
				this.UpdateMyStatsIncremental();
			}
			this.FixedUpdateOLD();
			if (this.PlayerPet != null)
			{
				this.PlayerPet.Graphics.gameObject.SetActive(!Cutscene.Active);
			}
			this.UpdateMaxHealth();
			this.UpdateMaxMana();
			this.Health = Mathf.Clamp(this.Health + this.GetStat(StatType.HealthRegen) * Time.deltaTime, 0f, this.MaxHealth + this.overcappedHealth);
			this.Mana = Mathf.Clamp(this.Mana + this.GetStat(StatType.ManaRegen) * Time.deltaTime, 0f, this.MaxMana + this.overcappedMana);
		}

		// Token: 0x06010985 RID: 67973 RVA: 0x00280158 File Offset: 0x0027E358
		private void UpdateMyStats()
		{
			for (int i = 0; i < Player.statTypes.Length; i++)
			{
				StatType statType = Player.statTypes[i];
				float myStat = this.GetMyStat(statType);
				this.SavedStats.Set(statType, myStat);
			}
		}

		// Token: 0x06010986 RID: 67974 RVA: 0x00280194 File Offset: 0x0027E394
		private void UpdateMyStatsIncremental()
		{
			StatType statType = Player.statTypes[this.statsIncrementalIndex];
			float myStat = this.GetMyStat(statType);
			this.SavedStats.Set(statType, myStat);
			this.statsIncrementalIndex++;
			if (this.statsIncrementalIndex >= Player.statTypes.Length)
			{
				this.statsIncrementalIndex = 0;
			}
		}

		// Token: 0x06010987 RID: 67975 RVA: 0x002801E7 File Offset: 0x0027E3E7
		public void EndInteraction()
		{
			IInteractable firstInteractable = this._interactions.FirstInteractable;
			if (firstInteractable == null)
			{
				return;
			}
			firstInteractable.EndInteract(this._interactions.GetInteractionType());
		}

		// Token: 0x1700C2CA RID: 49866
		// (get) Token: 0x06010988 RID: 67976 RVA: 0x00280209 File Offset: 0x0027E409
		public bool CanUseUseItem
		{
			get
			{
				return this._useItem && this.spell1NotCasting && this.spell2NotCasting && this.spell3NotCasting && this.spell4NotCasting;
			}
		}

		// Token: 0x06010989 RID: 67977 RVA: 0x00280238 File Offset: 0x0027E438
		private void HandleInput()
		{
			this.input = Vector2.zero;
			if (PlayerInput.AllowInput)
			{
				Vector2 vector;
				if (!this.IsMainMenuPlayer || !MouseVisualManager.UsingController)
				{
					float num = PlayerInput.GetAxis("Horizontal", false);
					float num2 = PlayerInput.GetAxis("Vertical", false);
					vector = new Vector2(num, num2);
					if (MouseVisualManager.UsingController)
					{
						num = ((Mathf.Abs(num) > 0.45f) ? num : 0f);
						num2 = ((Mathf.Abs(num2) > 0.45f) ? num2 : 0f);
					}
					this.input = new Vector2(num, num2);
				}
				else
				{
					vector = new Vector2((float)((PlayerInput.GetButtonDown(Button.MenuLeft, false) ? -1 : 0) + (PlayerInput.GetButtonDown(Button.MenuRight, false) ? 1 : 0)), (float)((PlayerInput.GetButtonDown(Button.MenuUp, false) ? 1 : 0) + (PlayerInput.GetButtonDown(Button.MenuDown, false) ? -1 : 0)));
					this.input = vector;
				}
				this.input = Vector2.Lerp(this.input, this.input.normalized, 0.6666667f);
				if (vector.magnitude > 0.1f)
				{
					this.lastWalkingDirection = vector.normalized;
					if (EventSystem.current)
					{
						EventSystem.current.SetSelectedGameObject(null);
					}
				}
				if (this.input.x != 0f)
				{
					HelpTooltips instance = SingletonBehaviour<HelpTooltips>.Instance;
					if (instance != null)
					{
						instance.CompleteNotification(8);
					}
				}
			}
			if (this._paused)
			{
				if (this.input.sqrMagnitude <= 0.01f)
				{
					this._paused = false;
				}
				else
				{
					this.input = Vector2.zero;
				}
			}
			if (!this._paused && this.rigidbody.velocity.sqrMagnitude <= 0.01f && this.input.sqrMagnitude > 0.01f)
			{
				try
				{
					UnityAction onUnpausePlayer = this.OnUnpausePlayer;
					if (onUnpausePlayer != null)
					{
						onUnpausePlayer();
					}
				}
				catch (Exception message)
				{
					Debug.Log(message);
				}
				this.OnUnpausePlayer = null;
			}
			this._pauseObjects.RemoveWhere((string item) => item == null);
			if (this.pause)
			{
				this.input = Vector2.zero;
			}
			this.input.y = this.input.y * 1.3f;
		}

		// Token: 0x0601098A RID: 67978 RVA: 0x00280474 File Offset: 0x0027E674
		private void FixedUpdateOLD()
		{
			if (!this.IsOwner)
			{
				return;
			}
			if (this.Pathing)
			{
				this.Path();
				this.input = this.rigidbody.velocity;
				this.FinalMovementSpeed = 1f;
			}
			else if (Cutscene.Active)
			{
				this.targetVelocity = Vector3.zero;
				this.rigidbody.velocity = this.targetVelocity;
				this.input = this.rigidbody.velocity;
				this.FinalMovementSpeed = 1f;
			}
			else
			{
				this.FinalMovementSpeed = this.moveSpeed * this.GetStat(StatType.Movespeed);
				foreach (FloatRef floatRef in this.moveSpeedMultipliers)
				{
					this.FinalMovementSpeed *= floatRef.value;
				}
				this.targetVelocity = (this.input + this.inputAdd) * this.FinalMovementSpeed;
				float num = (this.Grounded && this.inputAdd.sqrMagnitude < 0.01f) ? 50f : this._airborneSmoothFactor;
				if (this.Hurt)
				{
					num *= 0.3f;
				}
				if (!this.IsMainMenuPlayer && SingletonBehaviour<GameManager>.Instance && this.Grounded)
				{
					this.targetVelocity.y = this.targetVelocity.y * SingletonBehaviour<GameManager>.Instance.Normal(base.transform.position).z;
				}
				this.rigidbody.velocity = Vector2.Lerp(this.rigidbody.velocity, this.targetVelocity, Time.deltaTime * num);
				this.StandingStill = (this.rigidbody.velocity.magnitude <= 0.01f);
				UnityAction<Vector2> onPlayerMove = this.OnPlayerMove;
				if (onPlayerMove != null)
				{
					onPlayerMove(this.targetVelocity);
				}
				this.jumpHeight = 1f;
				foreach (FloatRef floatRef2 in this.jumpMultipliers)
				{
					this.jumpHeight *= floatRef2.value;
				}
				this._movement.SetJumpMultiplier(this.jumpHeight);
			}
			this.UpdateFacingDirection();
			this.HandleBuffUpdates();
		}

		// Token: 0x0601098B RID: 67979 RVA: 0x002806F0 File Offset: 0x0027E8F0
		private void HandleBuffUpdates()
		{
			bool flag = false;
			foreach (KeyValuePair<BuffType, Buff> keyValuePair in this._buffs)
			{
				keyValuePair.Value.UpdateBuff();
				if (keyValuePair.Value.Complete())
				{
					flag = true;
				}
			}
			if (flag)
			{
				foreach (KeyValuePair<BuffType, Buff> keyValuePair2 in this._buffs.ToArray<KeyValuePair<BuffType, Buff>>())
				{
					if (keyValuePair2.Value.Complete())
					{
						keyValuePair2.Value.FinishBuff();
						this._buffs.Remove(keyValuePair2.Key);
					}
				}
			}
		}

		// Token: 0x0601098C RID: 67980 RVA: 0x002807B4 File Offset: 0x0027E9B4
		private void LateUpdate()
		{
			if (!this.IsMainMenuPlayer && this.IsOwner)
			{
				if (this.InMineCart)
				{
					this._currentDepth = this.MineCart.transform.position.z;
				}
				float num;
				if (this.positionCachedForDepthCheck != base.transform.position)
				{
					num = SingletonBehaviour<GameManager>.Instance.Depth(base.transform.position, false);
					this.positionCachedForDepthCheck = base.transform.position;
					this.depthCached = num;
				}
				else
				{
					num = this.depthCached;
				}
				float num2 = (this.InMineCart || !SingletonBehaviour<GameManager>.Instance) ? 0f : num;
				if (this.Grounded && num2 - (this.InJumpZone ? 0f : 0.25f) > this.graphics.transform.position.z)
				{
					this._movement.Jump(0f);
					this.Grounded = false;
				}
				this.graphics.transform.position = new Vector3(base.transform.position.x, base.transform.position.y, this.InMineCart ? this.MineCart.transform.position.z : (this._currentDepth - this._movement.JumpHeight));
				if (num2 < this.graphics.transform.position.z)
				{
					this._movement.StopJump();
					if (!this.Grounded)
					{
						if (this._movement.JumpVelocity < 0.2f)
						{
							AudioManager.Instance.PlayOneShot(SingletonBehaviour<Prefabs>.Instance.playerLand, this.graphics.position, 0.22f, 0.1f, 0.95f, 1.1f, 7.5f, 15f, 8);
						}
						UnityAction onLand = this.OnLand;
						if (onLand != null)
						{
							onLand();
						}
					}
					this.Grounded = true;
					this.LastGroundedTime = Time.time;
					this.graphics.transform.position = new Vector3(base.transform.position.x, base.transform.position.y, this.InMineCart ? this.MineCart.transform.position.z : (num2 - this._movement.JumpHeight));
				}
				if (this.Grounded)
				{
					if (this.InJumpZone && this.graphics.transform.position.z - 0.3f > this.jumpZoneHeight)
					{
						this.ReceiveDamage(new DamageInfo
						{
							damage = this.MaxHealth * 0.1f,
							damageType = DamageType.Enemy,
							hitPoint = base.transform.position,
							knockBack = 0f,
							sender = base.transform,
							trueDamage = true
						});
						this.AddPauseObject("fall");
						DOVirtual.DelayedCall(0.75f, delegate
						{
							this.RemovePauseObject("fall");
						}, true);
						this.lastJumpHeight = this.jumpZoneHeight;
						this._currentDepth = this.jumpZoneHeight;
						base.transform.position = new Vector3(this.lastJumpPos.x, this.lastJumpPos.y, base.transform.position.z);
						this.graphics.transform.position = new Vector3(base.transform.position.x, base.transform.position.y, this.InMineCart ? this.MineCart.transform.position.z : (this.lastJumpHeight - 0.025f));
					}
					else
					{
						this._currentDepth = num2;
						this.lastJumpHeight = this.graphics.position.z;
						this.graphics.transform.position = new Vector3(base.transform.position.x, base.transform.position.y, this.InMineCart ? this.MineCart.transform.position.z : (this._currentDepth - this._movement.JumpHeight));
					}
				}
				float z = this._playerCamera.transform.position.z;
				float num3 = (this.InMineCart ? this.MineCart.transform.position.z : this._currentDepth) - 51f;
				if (!this.InMineCart && Mathf.Abs(num3 - z) > 1.2142135f)
				{
					this._playerCamera.transform.DOMoveZ(num3, 1f, false);
				}
				if (this.ControlCameraXY)
				{
					this._cameraPosition = Vector3.Lerp(this._cameraPosition, new Vector3((float)Mathf.RoundToInt(this.graphics.transform.position.x * 24f) * 0.041666668f, (float)Mathf.RoundToInt((this.graphics.transform.position.y - 50f) * 16.970562f) * 0.058925565f, this._playerCamera.transform.position.z), 7f * Time.deltaTime);
					this._playerCamera.transform.position = new Vector3(this._cameraPosition.x, this._cameraPosition.y, this._playerCamera.transform.position.z) + this.CameraController.transform.localPosition;
				}
				if (Mathf.Abs(this._playerCamera.orthographicSize - this.ActualCameraZoomLevel) > 0.5f && (this.zoomTween == null || !this.zoomTween.IsActive()))
				{
					Tween tween = this.zoomTween;
					if (tween != null)
					{
						tween.Kill(false);
					}
					this.zoomTween = this._playerCamera.DOOrthoSize(this.ActualCameraZoomLevel, 1.25f).SetEase(Ease.Linear);
				}
				if (this.Grounded)
				{
					this.AirSkipsUsed = 0;
				}
				int num4 = this.MaxAirSkips + (GameSave.Exploration.GetNode("Exploration10a", true) ? 1 : 0);
				if (SingletonBehaviour<GameSave>.Instance.CurrentSave != null && GameSave.Exploration.GetNode("Exploration1a", true) && num2 - 0.25f > this.graphics.transform.position.z && !this.Petting && !UIHandler.InventoryOpen && this.AirSkipsUsed < num4 && PlayerInput.GetButtonDown(Button.Jump, false) && !this.UniqueJump)
				{
					int num5 = (this.AirSkipsUsed == 0) ? (5 - GameSave.Exploration.GetNodeAmount("Exploration1a", 3, true)) : (6 - GameSave.Exploration.GetNodeAmount("Exploration10a", 3, true));
					if (this.Mana >= (float)num5)
					{
						this.AirSkip(num5);
					}
				}
				if (!Cutscene.Active && !this.Pathing && !this.pause && this.jumpHeight >= 0.35f && this.Grounded && !this.UniqueJump && !this.Petting && PlayerInput.GetButton(Button.Jump, false) && (!MouseVisualManager.UsingController || !UIHandler.UIWasOpenThisFrame))
				{
					this._movement.Jump(this.GetStat(StatType.Jump));
					SingletonBehaviour<HelpTooltips>.Instance.CompleteNotification(9);
					SingletonBehaviour<GameSave>.Instance.AddProgressIntCharacter("TimesJumped", 1);
					UnityAction onJump = this.OnJump;
					if (onJump != null)
					{
						onJump();
					}
					this.lastJumpHeight = this.graphics.position.z;
					this.Grounded = false;
				}
				if (this.Inventory)
				{
					Item currentItem = this.CurrentItem;
					if (currentItem != null)
					{
						currentItem.UpdateItem();
					}
				}
				this.CheckForInCombat();
				this.UpdateAudio();
			}
			this.UpdateAnimations();
			if (GameManager.SceneTransitioning && !Cutscene.Active)
			{
				this._armsAnimator.SetFloat("Speed", 0f);
				this._bodyAnimator.SetFloat("Speed", 0f);
			}
			if (!this.IsMainMenuPlayer && this.IsOwner && SingletonBehaviour<DayCycle>.Instance && this.Sleeping && !SingletonBehaviour<DayCycle>.Instance.TransitioningDays)
			{
				this.CheckIfAllPlayersSleeping();
			}
		}

		// Token: 0x0601098D RID: 67981 RVA: 0x00281006 File Offset: 0x0027F206
		private void OnDestroy()
		{
			if (Player.Instance == this)
			{
				Player.Instance = null;
			}
		}

		// Token: 0x1700C2CB RID: 49867
		// (get) Token: 0x0601098E RID: 67982 RVA: 0x00026368 File Offset: 0x00024568
		public Transform Transform
		{
			get
			{
				return base.transform;
			}
		}

		// Token: 0x0601098F RID: 67983 RVA: 0x0028101C File Offset: 0x0027F21C
		public DamageHit ReceiveDamage(DamageInfo damageInfo)
		{
			if (Settings.Invincible || this.Invincible || this.Hurt || !this.IsOwner || this.InMineCart || damageInfo.damageType == DamageType.Player || GameManager.SceneTransitioning)
			{
				return new DamageHit
				{
					hit = false,
					damageTaken = 0f
				};
			}
			Boss boss;
			if (damageInfo.sender.TryGetComponent<Boss>(out boss) && boss.partyEventID != Mathf.Abs((int)this.partyState))
			{
				return new DamageHit
				{
					hit = false,
					damageTaken = 0f
				};
			}
			if (Utilities.Chance(this.GetStat(StatType.Dodge)))
			{
				damageInfo.dodge = true;
			}
			if (GameSave.Combat.GetNode("Combat4d", true))
			{
				EnemyAI componentInParent = damageInfo.sender.GetComponentInParent<EnemyAI>();
				if (componentInParent && !componentInParent.Equals(null) && damageInfo.canReflect)
				{
					float num = 0.5f + 0.5f * (float)GameSave.Combat.GetNodeAmount("Combat4d", 3, true);
					componentInParent.ReceiveDamage(new DamageInfo
					{
						damage = damageInfo.damage * num,
						damageType = DamageType.Player,
						crit = false,
						hitPoint = Vector3.one * 100000f,
						hitType = HitType.Normal,
						knockBack = 0f,
						sender = base.transform,
						canReflect = false
					});
				}
			}
			if (GameSave.Combat.GetNode("Combat5d", true))
			{
				this.AddDecayingMoveSpeedBuff(ref this.moveSpeedWhenHurtTween, this.moveSpeedWhenHurtFloatRef, BuffType.Adrenaline, "Adrenaline", 1.1f + 0.2f * (float)GameSave.Combat.GetNodeAmount("Combat5d", 2, true), 3f);
			}
			UnityAction<DamageInfo> unityAction = this.onPreReceiveDamage;
			if (unityAction != null)
			{
				unityAction(damageInfo);
			}
			float num2 = damageInfo.trueDamage ? damageInfo.damage : DamageCalculator.CalculateFinalDamage(damageInfo.damage, this.GetStat(StatType.Defense), this.GetStat(StatType.DamageReduction));
			this.Health -= num2;
			if (this.HealthPercentage <= 0.8f && !SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("Armor"))
			{
				SingletonBehaviour<HelpTooltips>.Instance.SendNotification(ScriptLocalization.Tooltips_Armor_Title, "Taking too much damage? Try crafting some <color=#39CCFF>Copper Armor</color> at an anvil!\n\nSolon's Smithery is also a great place to buy armor.", new List<ValueTuple<Transform, Vector3, Direction>>(), 5, delegate
				{
					SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("Armor", true);
				});
			}
			this.rigidbody.velocity = (base.transform.position - damageInfo.hitPoint).normalized * 5f * damageInfo.knockBack;
			this._movement.KnockBack(2f);
			if (damageInfo.dodge)
			{
				damageInfo.dodge = true;
				FloatingTextManager.Instance.SpawnFloatingDamageText(0, base.transform.position + Vector3.up * 2.3f, DamageType.Enemy, false, true);
				return new DamageHit
				{
					hit = false,
					damageTaken = 0f
				};
			}
			this.UnMount();
			FloatingTextManager.Instance.SpawnFloatingDamageText((int)num2, base.transform.position + Vector3.up * 3f, damageInfo.damageType, false, damageInfo.dodge);
			string animationParameter = (this.rigidbody.velocity.x > 0f) ? "HurtWest" : "HurtEast";
			float num3 = 1f;
			if (GameSave.Combat.GetNode("Combat9d", true))
			{
				num3 += 0.1f + 0.1f * (float)GameSave.Combat.GetNodeAmount("Combat9d", 3, true);
			}
			this._hurtTimer.UpdateValue(0.7f * num3, true, delegate
			{
				this._eyesAnimator.SetBool(animationParameter, true);
			}, delegate
			{
				this._eyesAnimator.SetBool(animationParameter, false);
			});
			Tween tween = this.eyesHurtTween;
			if (tween != null)
			{
				tween.Kill(false);
			}
			this.eyesHurtTween = DOVirtual.DelayedCall(0.4f, delegate
			{
				this._eyesAnimator.SetBool(animationParameter, false);
			}, true);
			this.FlashPlayerRed();
			AudioManager.Instance.PlayOneShot(SingletonBehaviour<Prefabs>.Instance.playerHurt, this.graphics.position, 0.38f, 0.1f, 1f, 1f, 7.5f, 15f, 8);
			if (this.Health <= 0f)
			{
				this.Die();
			}
			return new DamageHit
			{
				hit = true,
				damageTaken = damageInfo.damage
			};
		}

		// Token: 0x06010990 RID: 67984 RVA: 0x002814A0 File Offset: 0x0027F6A0
		private void FlashPlayerRed()
		{
			Renderer[] componentsInChildren = this._playerAnimationLayers.GetComponentsInChildren<Renderer>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Renderer rend = componentsInChildren[i];
				if (rend.transform.parent == this._playerAnimationLayers.transform && rend.transform != this._pickup.transform && rend.sharedMaterial && rend.sharedMaterial.HasProperty("_Color"))
				{
					DOVirtual.Float(0f, 1f, 0.15f, delegate(float x)
					{
						rend.sharedMaterial.SetColor("_Color", new Color(1f, (float)((int)(1f - x * 0.66f)), (float)((int)(1f - x * 0.66f))));
						rend.GetComponent<MeshGenerator>().shader.SetColor("_Color", new Color(1f, 1f - x * 0.66f, 1f - x * 0.66f));
					}).SetLoops(2, LoopType.Yoyo).SetUpdate(UpdateType.Late);
				}
			}
		}

		// Token: 0x06010991 RID: 67985 RVA: 0x00281574 File Offset: 0x0027F774
		private void ResetPlayerMaterial()
		{
			foreach (Renderer renderer in this._playerAnimationLayers.GetComponentsInChildren<Renderer>())
			{
				if (renderer.transform.parent == this._playerAnimationLayers.transform && renderer.transform != this._pickup.transform && renderer.sharedMaterial && renderer.sharedMaterial.HasProperty("_Color"))
				{
					renderer.sharedMaterial.SetColor("_Color", new Color(1f, 1f, 1f));
					renderer.GetComponent<MeshGenerator>().shader.SetColor("_Color", new Color(1f, 1f, 1f));
				}
			}
		}

		// Token: 0x06010992 RID: 67986 RVA: 0x0028164C File Offset: 0x0027F84C
		public void AddDecayingMoveSpeedBuff(ref Tween tween, FloatRef floatRef, BuffType buffType, string buffName, float moveSpeed, float duration)
		{
			Tween tween2 = tween;
			if (tween2 != null)
			{
				tween2.Kill(false);
			}
			this.moveSpeedMultipliers.Add(floatRef);
			BuffIcon buffIcon = SingletonBehaviour<BuffIconManager>.Instance.SetBuffIcon(buffType);
			buffIcon.popup.text = buffName;
			buffIcon.popup.description = "Increase movement speed by " + ((int)((moveSpeed - 1f) * 100f)).ToString() + "% decaying over time";
			tween = DOVirtual.Float(moveSpeed, 1f, duration, delegate(float x)
			{
				floatRef.value = x;
				buffIcon.timer.fillAmount = Mathf.InverseLerp(moveSpeed, 1f, x);
			}).SetEase(Ease.Linear).OnComplete(delegate
			{
				this.moveSpeedMultipliers.Remove(floatRef);
				SingletonBehaviour<BuffIconManager>.Instance.RemoveBuffIcon(buffType);
			});
		}

		// Token: 0x06010993 RID: 67987 RVA: 0x00281738 File Offset: 0x0027F938
		public float GetMyStat(StatType stat)
		{
			if (Player.Instance == this)
			{
				float stat2 = this.Stats.GetStat(stat);
				float stat3 = this.FoodStats.GetStat(stat);
				float stat4 = this.PlayerInventory.GetStat(stat);
				float stat5 = this.RelationshipStats.GetStat(stat);
				float num;
				float num2;
				if (SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.StatBonuses.TryGetValue(stat, out num))
				{
					num2 = num;
				}
				else
				{
					num2 = 0f;
				}
				float num3 = stat2 + stat3 + stat4 + stat5 + num2;
				if (SceneSettingsManager.Instance.GetCurrentSceneSettings != null && SceneSettingsManager.Instance.GetCurrentSceneSettings.mapType == MapType.Mine)
				{
					num3 += this.MiningStats.GetStat(stat);
				}
				return num3 + this.skillStats.GetStat(stat, num3);
			}
			return this.Stats.GetStat(stat);
		}

		// Token: 0x06010994 RID: 67988 RVA: 0x00281810 File Offset: 0x0027FA10
		public float GetStat(StatType stat)
		{
			if (Player.Instance == this)
			{
				return this.SavedStats.GetStat(stat);
			}
			return this.Stats.GetStat(stat);
		}

		// Token: 0x06010995 RID: 67989 RVA: 0x00281838 File Offset: 0x0027FA38
		public float GetStatWithoutSkills(StatType stat)
		{
			if (Player.Instance == this)
			{
				float num;
				return this.Stats.GetStat(stat) + this.PlayerInventory.GetStat(stat) + (SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.StatBonuses.TryGetValue(stat, out num) ? num : 0f);
			}
			return this.Stats.GetStat(stat);
		}

		// Token: 0x06010996 RID: 67990 RVA: 0x002818A0 File Offset: 0x0027FAA0
		public void ChangeMount(PlayerMount playerMount, int id)
		{
			if (this.mount && playerMount && this.mount.id == id)
			{
				return;
			}
			foreach (object obj in this.mountTransform)
			{
				DestroyUtilities.DestroyDebug(((Transform)obj).gameObject);
			}
			this.mount = (playerMount ? UnityEngine.Object.Instantiate<PlayerMount>(playerMount, this.mountTransform) : null);
			this.mount.id = id;
			PlayerMount playerMount2 = this.mount;
			if (playerMount2 == null)
			{
				return;
			}
			playerMount2.SetPlayer(this);
		}

		// Token: 0x06010997 RID: 67991 RVA: 0x0028195C File Offset: 0x0027FB5C
		public void SummonMount()
		{
			if (this.mount)
			{
				this.mount.SummonMount();
				UnityAction<int> unityAction = this.onChangeMount;
				if (unityAction != null)
				{
					unityAction(this.mount.id);
				}
				Utilities.UnlockAcheivement(93);
			}
		}

		// Token: 0x06010998 RID: 67992 RVA: 0x00281999 File Offset: 0x0027FB99
		public void UnMount()
		{
			PlayerMount playerMount = this.mount;
			if (playerMount != null)
			{
				playerMount.UnMount();
			}
			UnityAction<int> unityAction = this.onChangeMount;
			if (unityAction == null)
			{
				return;
			}
			unityAction(0);
		}

		// Token: 0x06010999 RID: 67993 RVA: 0x002819BD File Offset: 0x0027FBBD
		public void Jump(float multiplier = 1f)
		{
			base.StartCoroutine(this.LateJumpRoutine(multiplier));
		}

		// Token: 0x0601099A RID: 67994 RVA: 0x002819CD File Offset: 0x0027FBCD
		private IEnumerator LateJumpRoutine(float multiplier = 1f)
		{
			yield return null;
			this._movement.Jump(this.GetStat(StatType.Jump) * multiplier);
			this.lastJumpHeight = this.graphics.position.z;
			this.Grounded = false;
			yield break;
		}

		// Token: 0x0601099B RID: 67995 RVA: 0x002819E4 File Offset: 0x0027FBE4
		public void Die()
		{
			if (this.Dying)
			{
				return;
			}
			Cart.currentRoom = 0;
			Cart.rewardRoom = "";
			if (PlaySettingsManager.PlaySettings.allowDeath)
			{
				base.StartCoroutine(this.DeathRoutine());
				return;
			}
			this.Health = this.MaxHealth;
			this.partyState = 0;
		}

		// Token: 0x0601099C RID: 67996 RVA: 0x00281A37 File Offset: 0x0027FC37
		protected IEnumerator DeathRoutine()
		{
			this.AddPauseObject("death");
			this.UnMount();
			Utilities.UnlockAcheivement(44);
			this.Dying = true;
			this.Invincible = true;
			SingletonBehaviour<GameSave>.Instance.SetProgressFloatCharacter("DeathCount", SingletonBehaviour<GameSave>.Instance.GetProgressFloatCharacter("DeathCount") + 1f);
			this.overrideFacingDirection = true;
			this.facingDirection = Direction.South;
			int num;
			for (int i = 0; i < 6; i = num + 1)
			{
				float interval = 0.1f * (1f - (float)i / 20f);
				yield return new WaitForSeconds(interval);
				this.facingDirection = Direction.West;
				yield return new WaitForSeconds(interval);
				this.facingDirection = Direction.North;
				yield return new WaitForSeconds(interval);
				this.facingDirection = Direction.East;
				yield return new WaitForSeconds(interval);
				this.facingDirection = Direction.South;
				num = i;
			}
			yield return new WaitForSeconds(0.5f);
			if (Mathf.Abs((int)this.partyState) == 2)
			{
				this.partyState = 0;
				SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(141.2083f, 255.5013f), "Arena", delegate
				{
					SingletonBehaviour<ScenePortalManager>.Instance.UnloadScene();
					this.RemovePauseObject("death");
					this.Health = this.MaxHealth / 2f;
					this.Dying = false;
					this.overrideFacingDirection = false;
					this.facingDirection = Direction.South;
					DOVirtual.DelayedCall(2f, delegate
					{
						this.Invincible = false;
					}, true);
					Player.AddDeath();
					this.partyState = 0;
				}, null, null, SceneFadeType.Fade, 2.5f);
			}
			if (CombatDungeon.CurrentFloor > 0)
			{
				SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(136.48f, 193.92f), "CombatDungeonEntrance", delegate
				{
					this.RemovePauseObject("death");
					this.Health = this.MaxHealth / 2f;
					this.Dying = false;
					this.overrideFacingDirection = false;
					this.facingDirection = Direction.South;
					this.Invincible = false;
					Player.AddDeath();
					this.partyState = 0;
				}, null, null, SceneFadeType.Fade, 2.5f);
			}
			else if (SingletonBehaviour<DayCycle>.Instance.CurrentTownType == TownType.Withergate && SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("Apartment"))
			{
				SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(63.5f, 54.624f), "WithergatePlayerApartment", delegate
				{
					this.RemovePauseObject("death");
					this.Health = this.MaxHealth / 2f;
					this.Dying = false;
					this.overrideFacingDirection = false;
					this.facingDirection = Direction.South;
					this.Invincible = false;
					this.AddDeathAndSubtractGold();
					this.partyState = 0;
				}, null, null, SceneFadeType.Fade, 2.5f);
			}
			else if (SingletonBehaviour<DayCycle>.Instance.CurrentTownType == TownType.Nelvari && SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("NelvariFarm"))
			{
				SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(51.5f, 54.97755f), "NelvariPlayerHouse", delegate
				{
					this.RemovePauseObject("death");
					this.Health = this.MaxHealth / 2f;
					this.Dying = false;
					this.overrideFacingDirection = false;
					this.facingDirection = Direction.South;
					this.Invincible = false;
					this.AddDeathAndSubtractGold();
					this.partyState = 0;
				}, null, null, SceneFadeType.Fade, 2.5f);
			}
			else if (SingletonBehaviour<DayCycle>.Instance.CurrentTownType == TownType.GreatCity && SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("UnlockedGCApartment"))
			{
				SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(398.9583f, 536.0945f), "GCMainDistrictPlayerApartment", delegate
				{
					this.RemovePauseObject("death");
					this.Health = this.MaxHealth / 2f;
					this.Dying = false;
					this.overrideFacingDirection = false;
					this.facingDirection = Direction.South;
					this.Invincible = false;
					this.AddDeathAndSubtractGold();
					this.partyState = 0;
				}, null, null, SceneFadeType.Fade, 2.5f);
			}
			else
			{
				SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(80.83334f, 65.58415f), "Hospital", delegate
				{
					this.RemovePauseObject("death");
					this.Health = this.MaxHealth / 2f;
					this.Dying = false;
					this.overrideFacingDirection = false;
					this.facingDirection = Direction.South;
					this.Invincible = false;
					this.AddDeathAndSubtractGold();
					this.partyState = 0;
				}, null, delegate
				{
					UnityAction unityAction = Player.onDying;
					if (unityAction == null)
					{
						return;
					}
					unityAction();
				}, SceneFadeType.Fade, 2.5f);
			}
			yield break;
		}

		// Token: 0x0601099D RID: 67997 RVA: 0x00281A48 File Offset: 0x0027FC48
		private void AddDeathAndSubtractGold()
		{
			int num = Player.AddDeath();
			if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("FirstTimePassedOut"))
			{
				if (num > 0)
				{
					this.AddMoneyAndRegisterSource(-Mathf.Max(200, (int)(0.03f * (float)GameSave.Coins)), 60101, 1, MoneySource.Exploration, true, true);
					return;
				}
				this.AddMoneyAndRegisterSource(-200, 60101, 1, MoneySource.Exploration, true, true);
			}
		}

		// Token: 0x0601099E RID: 67998 RVA: 0x00281AAC File Offset: 0x0027FCAC
		private static int AddDeath()
		{
			int progressIntCharacter = SingletonBehaviour<GameSave>.Instance.GetProgressIntCharacter("TimesDied");
			SingletonBehaviour<GameSave>.Instance.SetProgressIntCharacter("TimesDied", progressIntCharacter + 1);
			return progressIntCharacter;
		}

		// Token: 0x0601099F RID: 67999 RVA: 0x00281ADC File Offset: 0x0027FCDC
		public void SetUseItem(ushort item, int index, bool fromLocal = true)
		{
			foreach (object obj in this.UseItemTransform)
			{
				DestroyUtilities.DestroyDebug(((Transform)obj).gameObject);
			}
			this._useItem = null;
			if (item != 0)
			{
				Database.GetData<ItemData>((int)item, delegate(ItemData itemData)
				{
					UseItem useItem = itemData.useItem;
					if (useItem != null)
					{
						UseItem useItem2 = UnityEngine.Object.Instantiate<UseItem>(useItem, this.UseItemTransform.position, useItem.transform.rotation, this.UseItemTransform);
						useItem2.SetPlayer(this);
						useItem2.SetItemData(itemData);
						this._useItem = useItem2;
					}
					this.Item = (int)item;
					this.ItemIndex = index;
					this.ItemData = itemData;
					if (fromLocal)
					{
						UnityAction<ushort> unityAction = this.onSetUseItem;
						if (unityAction == null)
						{
							return;
						}
						unityAction(item);
					}
				}, null);
			}
		}

		// Token: 0x060109A0 RID: 68000 RVA: 0x00281B80 File Offset: 0x0027FD80
		private void OnCompleted(AsyncOperationHandle obj, ItemData itemData)
		{
			if (obj.Status == AsyncOperationStatus.Succeeded)
			{
				GameObject gameObject = obj.Result as GameObject;
				UseItem component = UnityEngine.Object.Instantiate<GameObject>(gameObject, this.UseItemTransform.position, gameObject.transform.rotation, this.UseItemTransform).GetComponent<UseItem>();
				component.SetPlayer(this);
				component.SetItemData(itemData);
				this._useItem = component;
			}
			this.loadingItem = false;
			Action action = this.onLoadedItem;
			if (action != null)
			{
				action();
			}
			this.onLoadedItem = null;
		}

		// Token: 0x060109A1 RID: 68001 RVA: 0x00281C00 File Offset: 0x0027FE00
		public void SetSpell(ushort item, int index, bool fromLocal = true)
		{
			Database.GetData<ItemData>((int)item, delegate(ItemData itemData)
			{
				if (itemData == null)
				{
					return;
				}
				switch (index)
				{
				default:
					foreach (object obj in this._spell1Transform)
					{
						DestroyUtilities.DestroyDebug(((Transform)obj).gameObject);
					}
					this.Spell1 = null;
					if (item != 0)
					{
						UseItem useItem = itemData.useItem;
						if (useItem != null && useItem is SpellUseItem)
						{
							UseItem useItem2 = UnityEngine.Object.Instantiate<UseItem>(useItem, this._spell1Transform.position, useItem.transform.rotation, this._spell1Transform);
							useItem2.SetPlayer(this);
							useItem2.SetItemData(itemData);
							this.Spell1 = (SpellUseItem)useItem2;
							this.Spell1.SpellSlot = 1;
						}
					}
					this.Spell1Item = (int)item;
					if (fromLocal)
					{
						SingletonBehaviour<GameSave>.Instance.SetProgressIntCharacter("Spell1", this.Spell1Item);
						UnityAction<ushort> unityAction = this.onSetSpell1;
						if (unityAction != null)
						{
							unityAction(item);
						}
					}
					break;
				case 2:
					foreach (object obj2 in this._spell2Transform)
					{
						DestroyUtilities.DestroyDebug(((Transform)obj2).gameObject);
					}
					this.Spell2 = null;
					if (item != 0)
					{
						UseItem useItem3 = itemData.useItem;
						if (useItem3 != null && useItem3 is SpellUseItem)
						{
							UseItem useItem4 = UnityEngine.Object.Instantiate<UseItem>(useItem3, this._spell2Transform.position, useItem3.transform.rotation, this._spell2Transform);
							useItem4.SetPlayer(this);
							useItem4.SetItemData(itemData);
							this.Spell2 = (SpellUseItem)useItem4;
							this.Spell2.SpellSlot = 2;
						}
					}
					this.Spell2Item = (int)item;
					if (fromLocal)
					{
						SingletonBehaviour<GameSave>.Instance.SetProgressIntCharacter("Spell2", this.Spell2Item);
						UnityAction<ushort> unityAction2 = this.onSetSpell2;
						if (unityAction2 != null)
						{
							unityAction2(item);
						}
					}
					break;
				case 3:
					foreach (object obj3 in this._spell3Transform)
					{
						DestroyUtilities.DestroyDebug(((Transform)obj3).gameObject);
					}
					this.Spell3 = null;
					if (item != 0)
					{
						UseItem useItem5 = itemData.useItem;
						if (useItem5 != null && useItem5 is SpellUseItem)
						{
							UseItem useItem6 = UnityEngine.Object.Instantiate<UseItem>(useItem5, this._spell3Transform.position, useItem5.transform.rotation, this._spell3Transform);
							useItem6.SetPlayer(this);
							useItem6.SetItemData(itemData);
							this.Spell3 = (SpellUseItem)useItem6;
							this.Spell3.SpellSlot = 3;
						}
					}
					this.Spell3Item = (int)item;
					if (fromLocal)
					{
						SingletonBehaviour<GameSave>.Instance.SetProgressIntCharacter("Spell3", this.Spell3Item);
						UnityAction<ushort> unityAction3 = this.onSetSpell3;
						if (unityAction3 != null)
						{
							unityAction3(item);
						}
					}
					break;
				case 4:
					foreach (object obj4 in this._spell4Transform)
					{
						DestroyUtilities.DestroyDebug(((Transform)obj4).gameObject);
					}
					this.Spell4 = null;
					if (item != 0)
					{
						UseItem useItem7 = itemData.useItem;
						if (useItem7 != null && useItem7 is SpellUseItem)
						{
							UseItem useItem8 = UnityEngine.Object.Instantiate<UseItem>(useItem7, this._spell4Transform.position, useItem7.transform.rotation, this._spell4Transform);
							useItem8.SetPlayer(this);
							useItem8.SetItemData(itemData);
							this.Spell4 = (SpellUseItem)useItem8;
							this.Spell4.SpellSlot = 4;
						}
					}
					this.Spell4Item = (int)item;
					if (fromLocal)
					{
						SingletonBehaviour<GameSave>.Instance.SetProgressIntCharacter("Spell4", this.Spell4Item);
						UnityAction<ushort> unityAction4 = this.onSetSpell4;
						if (unityAction4 != null)
						{
							unityAction4(item);
						}
					}
					break;
				}
				if (fromLocal)
				{
					this.PlayerInventory.RefreshSpellImages();
				}
			}, null);
		}

		// Token: 0x060109A2 RID: 68002 RVA: 0x00281C48 File Offset: 0x0027FE48
		public void Heal(float health, bool spawnText = true, float overCapAmount = 1f)
		{
			if (Mathf.Approximately(health, 0f))
			{
				return;
			}
			this.Health += health;
			float maxHealth = this.MaxHealth;
			this.Health = Mathf.Clamp(this.Health, 0f, overCapAmount * maxHealth);
			this.overcappedHealth = Mathf.Max(0f, this.Health - maxHealth);
			if (spawnText)
			{
				FloatingTextManager.Instance.SpawnFloatingDamageText((int)health, base.transform.position + Vector3.up * 3f, DamageType.Heal, false, false);
			}
		}

		// Token: 0x060109A3 RID: 68003 RVA: 0x00281CDC File Offset: 0x0027FEDC
		public void UseMana(float mana)
		{
			if (Mathf.Approximately(mana, 0f))
			{
				return;
			}
			if (GameSave.Combat.GetNode("Combat6a", true))
			{
				mana *= 1f - 0.04f * (float)GameSave.Combat.GetNodeAmount("Combat6a", 3, true);
			}
			this.Mana -= mana;
			this.Mana = Mathf.Clamp(this.Mana, 0f, this.GetStat(StatType.Mana));
			FloatingTextManager.Instance.SpawnFloatingManaUsageText(Mathf.RoundToInt(-mana), this.graphics.transform.position + new Vector3(0.25f, 3f));
			if (this.Mana <= 1.9f && !SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("Mana"))
			{
				SingletonBehaviour<HelpTooltips>.Instance.SendNotification(ScriptLocalization.Mana, ScriptLocalization.Tooltips_Mana_Text, new List<ValueTuple<Transform, Vector3, Direction>>(), 23, delegate
				{
					SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("Mana", true);
				});
			}
		}

		// Token: 0x060109A4 RID: 68004 RVA: 0x00281DE4 File Offset: 0x0027FFE4
		public void AddMana(float mana, float overCapAmount = 1f)
		{
			if (Mathf.Approximately(mana, 0f))
			{
				return;
			}
			this.Mana += mana;
			float maxMana = this.MaxMana;
			this.Mana = Mathf.Clamp(this.Mana, 0f, overCapAmount * maxMana);
			this.overcappedMana = Mathf.Max(0f, this.Mana - maxMana);
			FloatingTextManager.Instance.SpawnFloatingManaUsageText((int)mana, base.transform.position + Vector3.up * 3f);
		}

		// Token: 0x060109A5 RID: 68005 RVA: 0x00281E70 File Offset: 0x00280070
		public void PausePlayerWithDialogue(string pauser, string dialogue, UnityAction onEnd = null, string pauserName = "")
		{
			this.AddPauseObject(pauser);
			DialogueController.Instance.SetDefaultBox(pauserName);
			DialogueController.Instance.PushDialogue(new DialogueNode
			{
				dialogueText = new List<string>
				{
					dialogue
				}
			}, delegate
			{
				this.RemovePauseObject(pauser);
				UnityAction onEnd2 = onEnd;
				if (onEnd2 == null)
				{
					return;
				}
				onEnd2();
			}, true, true);
		}

		// Token: 0x060109A6 RID: 68006 RVA: 0x00281EDF File Offset: 0x002800DF
		public void AddPauseObject(string id)
		{
			this.rigidbody.velocity = Vector2.zero;
			this.targetVelocity = Vector2.zero;
			this._pauseObjects.Add(id);
		}

		// Token: 0x060109A7 RID: 68007 RVA: 0x00281F0E File Offset: 0x0028010E
		public void RemovePauseObject(string id)
		{
			this._pauseObjects.Remove(id);
		}

		// Token: 0x060109A8 RID: 68008 RVA: 0x00281F20 File Offset: 0x00280120
		public void UpdateAnimations()
		{
			this._armsAnimator.SetFloat("Speed", ((GameManager.SceneTransitioning && !Cutscene.Active) || this.FreezeWalkAnimations) ? 0f : (this.input.magnitude * (this.IsOwner ? this.FinalMovementSpeed : 1f)));
			this._bodyAnimator.SetFloat("Speed", ((GameManager.SceneTransitioning && !Cutscene.Active) || this.FreezeWalkAnimations) ? 0f : (this.input.magnitude * (this.IsOwner ? this.FinalMovementSpeed : 1f)));
			this._armsAnimator.SetFloat("WalkSpeed", Mathf.Clamp(this.FinalMovementSpeed / this.moveSpeed, 0.75f, 1.3f));
			this._bodyAnimator.SetFloat("WalkSpeed", Mathf.Clamp(this.FinalMovementSpeed / this.moveSpeed, 0.75f, 1.3f));
			this._armsAnimator.SetBool("Grounded", true);
			this._mouthAnimator.SetInteger("Animation", 0);
			this._armsAnimator.SetFloat("AttackSpeed", 1f);
			this._bodyAnimator.SetFloat("AttackSpeed", 1f);
			this._armsAnimator.SetInteger("Grip", 0);
			if (this.OverrideAnimation)
			{
				if (this.Animation.hideUseItem)
				{
					foreach (Component component in this.UseItemTransform.GetComponentsInChildren<Component>())
					{
						Renderer renderer = component as Renderer;
						if (renderer != null)
						{
							renderer.enabled = false;
						}
						MeshGenerator meshGenerator = component as MeshGenerator;
						if (meshGenerator != null)
						{
							meshGenerator.enabled = false;
						}
					}
				}
				this.facingDirection = this.Animation.facingDirection;
				this._armsAnimator.SetInteger("Animation", (int)this.Animation.swing);
				this._bodyAnimator.SetInteger("Animation", 0);
				this._armsAnimator.SetInteger("Grip", (int)this.Animation.hold);
			}
			else
			{
				WishBottle wishBottle = this.UseItem as WishBottle;
				if (wishBottle != null && !wishBottle.CompletedWish)
				{
					this._armsAnimator.SetInteger("Animation", 5);
				}
				else
				{
					Food food = this.UseItem as Food;
					if (food != null)
					{
						if (food.Eating)
						{
							this.facingDirection = Direction.South;
							this._bodyAnimator.SetFloat("Speed", 0f);
							this._bodyAnimator.SetInteger("Animation", 0);
							this.rigidbody.velocity = Vector2.zero;
							this._armsAnimator.SetFloat("Speed", 0f);
							this._bodyAnimator.SetFloat("Speed", 0f);
							this._armsAnimator.SetInteger("Animation", 4);
						}
						else
						{
							this._armsAnimator.SetInteger("Animation", 5);
						}
					}
					else
					{
						IAnimationItem animationItem = this.UseItem as IAnimationItem;
						if (animationItem != null)
						{
							this._armsAnimator.SetInteger("Animation", (int)animationItem.Hold);
							this._bodyAnimator.SetInteger("Animation", 0);
						}
						else
						{
							KickstarterFood kickstarterFood = this.UseItem as KickstarterFood;
							if (kickstarterFood != null && kickstarterFood != null)
							{
								if (kickstarterFood.Eating)
								{
									this.facingDirection = Direction.South;
									this._bodyAnimator.SetFloat("Speed", 0f);
									this._bodyAnimator.SetInteger("Animation", 0);
									this.rigidbody.velocity = Vector2.zero;
									this._armsAnimator.SetFloat("Speed", 0f);
									this._bodyAnimator.SetFloat("Speed", 0f);
									this._armsAnimator.SetInteger("Animation", 4);
								}
								else
								{
									this._armsAnimator.SetInteger("Animation", 5);
								}
							}
							else
							{
								Weapon weapon = this.UseItem as Weapon;
								if (weapon != null)
								{
									if (weapon.Attacking)
									{
										this._armsAnimator.SetInteger("Animation", (int)weapon.SwingAnimation);
										switch (weapon.SwingAnimation)
										{
										case SwingAnimation.None:
											this._bodyAnimator.SetInteger("Animation", 0);
											break;
										case SwingAnimation.HorizontalSlash:
											this._bodyAnimator.SetInteger("Animation", this.Grounded ? 1 : 0);
											this._bodyAnimator.SetFloat("Speed", 0f);
											break;
										case SwingAnimation.VerticalSlash:
											this._bodyAnimator.SetInteger("Animation", this.Grounded ? 1 : 0);
											this._bodyAnimator.SetFloat("Speed", 0f);
											break;
										case SwingAnimation.SpellCast:
											this._bodyAnimator.SetInteger("Animation", this.Grounded ? 1 : 0);
											this._bodyAnimator.SetFloat("Speed", 0f);
											break;
										case SwingAnimation.SpellCharge:
											this._bodyAnimator.SetInteger("Animation", 0);
											this._bodyAnimator.SetFloat("Speed", 0f);
											break;
										case SwingAnimation.Water:
											this._bodyAnimator.SetInteger("Animation", this.Grounded ? 3 : 0);
											this._bodyAnimator.SetFloat("Speed", 0f);
											break;
										case SwingAnimation.Pull:
											this._bodyAnimator.SetInteger("Animation", 0);
											this._bodyAnimator.SetFloat("Speed", 0f);
											break;
										}
										if (weapon.WeaponHold == WeaponHold.NeutralGrip)
										{
											this._armsAnimator.SetInteger("Animation", 1);
										}
										this._armsAnimator.SetFloat("AttackSpeed", weapon.AttackSpeed());
										this._bodyAnimator.SetFloat("AttackSpeed", weapon.AttackSpeed());
									}
									else
									{
										this._armsAnimator.SetInteger("Grip", 0);
										this._armsAnimator.SetInteger("Animation", 0);
										this._bodyAnimator.SetInteger("Animation", 0);
									}
									this._armsAnimator.SetInteger("Grip", (int)weapon.WeaponHold);
								}
								else
								{
									this._armsAnimator.SetInteger("Grip", 0);
									this._armsAnimator.SetInteger("Animation", 0);
									this._bodyAnimator.SetInteger("Animation", 0);
								}
							}
						}
					}
				}
				if (this._petting)
				{
					this._armsAnimator.SetInteger("Animation", 9);
				}
			}
			Direction chestDirection = this.GetChestDirection();
			this.SetAnimatorBool(this._armsAnimator, chestDirection);
			this.SetAnimatorBool(this._bodyAnimator, chestDirection);
			this.SetAnimatorBool(this._eyesAnimator, this.facingDirection);
			this.SetAnimatorBool(this._mouthAnimator, this.facingDirection);
			this._armsAnimator.SetBool("Grounded", this.FreezeJumpAnimation || this.Grounded);
			this._bodyAnimator.SetBool("Grounded", this.FreezeJumpAnimation || this.Grounded);
			if (this.Sleeping || this.Dying || this.EyesClosed)
			{
				this._eyesAnimator.SetInteger("Animation", 30);
			}
			else
			{
				if (this.emote == 2)
				{
					float magnitude = this.rigidbody.velocity.magnitude;
				}
				this._eyesAnimator.SetInteger("Animation", (int)this.emote);
			}
			this._eyesAnimator.SetBool("Hurt", this.Hurt);
		}

		// Token: 0x060109A9 RID: 68009 RVA: 0x00282698 File Offset: 0x00280898
		public Direction GetChestDirection()
		{
			Direction result = this.facingDirection;
			if (this.rotateChestDirection)
			{
				switch (result)
				{
				case Direction.North:
					result = Direction.West;
					break;
				case Direction.South:
					result = Direction.East;
					break;
				case Direction.East:
					result = Direction.North;
					break;
				case Direction.West:
					result = Direction.South;
					break;
				}
			}
			return result;
		}

		// Token: 0x060109AA RID: 68010 RVA: 0x002826DC File Offset: 0x002808DC
		public void UpdateFacingDirection()
		{
			Vector2 vector = this.input;
			if (!this.Pathing && PlayerInput.AllowArrowKeys && !Cutscene.Active)
			{
				this.input += new Vector2((float)((Input.GetKey(KeyCode.LeftArrow) ? -1 : 0) + (Input.GetKey(KeyCode.RightArrow) ? 1 : 0)), (float)((Input.GetKey(KeyCode.UpArrow) ? 1 : 0) + (Input.GetKey(KeyCode.DownArrow) ? -1 : 0)));
			}
			if (!this.overrideFacingDirection && this.input.sqrMagnitude > 0.01f && this.FinalMovementSpeed > 0.001f)
			{
				if (this.CanFaceNorthSouth)
				{
					if ((double)this.input.y > 0.01)
					{
						this.facingDirection = Direction.North;
						this.northSouthFacingDirection = Direction.North;
					}
					if ((double)this.input.y < -0.01)
					{
						this.facingDirection = Direction.South;
						this.northSouthFacingDirection = Direction.South;
					}
				}
				if (Mathf.Abs(this.input.x) + 0.01f > Mathf.Abs(this.input.y) / 1.4142135f)
				{
					if (this.input.x > 0.01f)
					{
						this.facingDirection = Direction.East;
					}
					if (this.input.x < -0.01f)
					{
						this.facingDirection = Direction.West;
					}
				}
			}
			if (!this.CanFaceNorthSouth && (this.facingDirection == Direction.South || this.facingDirection == Direction.North))
			{
				this.facingDirection = Direction.East;
			}
			this.input = vector;
			if (this._interactCollider)
			{
				this._interactCollider.size = (this.Mounted ? new Vector2(1.25f, 2.25f) : new Vector2(1.25f, 1.65f));
				this._interactCollider.offset = (this.Mounted ? new Vector2(0f, 0.75f) : new Vector2(0f, 0.425f));
			}
		}

		// Token: 0x060109AB RID: 68011 RVA: 0x002828DC File Offset: 0x00280ADC
		public void Emote(byte emote)
		{
			Tween emoteTween = this._emoteTween;
			if (emoteTween != null)
			{
				emoteTween.Kill(false);
			}
			this.emote = emote;
			this._emoteTween = DOVirtual.DelayedCall(2f, delegate
			{
				this.emote = 0;
			}, true);
		}

		// Token: 0x060109AC RID: 68012 RVA: 0x00282914 File Offset: 0x00280B14
		public void EmoteSurprised(float yOffset = 0f)
		{
			DestroyUtilities.DestroyDebug(UnityEngine.Object.Instantiate<GameObject>(SingletonBehaviour<Prefabs>.Instance.aggroParticles, this.graphics.transform.position + new Vector3(0f, 1.6970564f + yOffset, -1.9470564f - yOffset), Quaternion.identity, this.graphics), SingletonBehaviour<Prefabs>.Instance.HeartAnimation.length);
		}

		// Token: 0x060109AD RID: 68013 RVA: 0x0028297C File Offset: 0x00280B7C
		public void CancelEmote()
		{
			Tween emoteTween = this._emoteTween;
			if (emoteTween != null)
			{
				emoteTween.Kill(false);
			}
			this.emote = 0;
		}

		// Token: 0x060109AE RID: 68014 RVA: 0x00282998 File Offset: 0x00280B98
		public void Pet()
		{
			this.moveSpeedMultipliers.Remove(this.petFloatRef);
			this.moveSpeedMultipliers.Add(this.petFloatRef);
			Tween petTween = this._petTween;
			if (petTween != null)
			{
				petTween.Kill(false);
			}
			this._petting = true;
			this._petTween = DOVirtual.DelayedCall(0.9f, delegate
			{
				this._petting = false;
				this.moveSpeedMultipliers.Remove(this.petFloatRef);
			}, true);
		}

		// Token: 0x060109AF RID: 68015 RVA: 0x00282A00 File Offset: 0x00280C00
		public void Pickup(int item, int amount = 1, bool rollForExtra = false)
		{
			this.moveSpeedMultipliers.Remove(this.pickupFloatRef);
			this.moveSpeedMultipliers.Add(this.pickupFloatRef);
			AudioManager.Instance.PlayAudio(SingletonBehaviour<Prefabs>.Instance.pickupSound, 0.4f, 0f, 1f, 1f, false);
			Tween pickupTween = this._pickupTween;
			if (pickupTween != null)
			{
				pickupTween.Kill(false);
			}
			TweenCallback <>9__2;
			Database.GetData<ItemData>(item, delegate(ItemData data)
			{
				this._pickup.gameObject.SetActive(true);
				this._pickup.sprite = data.icon;
				switch (this.facingDirection)
				{
				case Direction.North:
					this._pickup.transform.localPosition = new Vector3(0f, 0.676f, -0.656f);
					break;
				case Direction.South:
					this._pickup.transform.localPosition = new Vector3(0f, 0.323f, -0.343f);
					break;
				case Direction.East:
					this._pickup.transform.localPosition = new Vector3(0.5f, 0.49f, -0.51f);
					break;
				case Direction.West:
					this._pickup.transform.localPosition = new Vector3(-0.5f, 0.49f, -0.51f);
					break;
				case Direction.Any:
					break;
				default:
					this._pickup.transform.localPosition = new Vector3(-0.5f, 0.49f, -0.51f);
					break;
				}
				this.SetPlayerAnimation(new PlayerAnimation
				{
					facingDirection = this.facingDirection,
					hideUseItem = true,
					hold = WeaponHold.NoGrip,
					swing = SwingAnimation.Push
				});
				this._pickupTween = DOVirtual.DelayedCall(0.13f, delegate
				{
					this.SetPlayerAnimation(new PlayerAnimation
					{
						facingDirection = this.facingDirection,
						hideUseItem = true,
						hold = WeaponHold.NoGrip,
						swing = SwingAnimation.SpellCharge
					});
					if (rollForExtra)
					{
						if (GameSave.Exploration.GetNode("Exploration1c", true) && Utilities.Chance((float)GameSave.Exploration.GetNodeAmount("Exploration1c", 3, true) * 0.1f + 0.1f))
						{
							amount++;
						}
						if (Utilities.Chance(this.GetStat(StatType.ExtraForageableChance)))
						{
							amount++;
						}
					}
					this.Inventory.AddItem(data.GenerateItem(), amount, 0, true, true, true);
					this._pickup.transform.localPosition = new Vector3(0f, 1.24f, -1.26f);
					float delay = 0.4f;
					TweenCallback callback;
					if ((callback = <>9__2) == null)
					{
						callback = (<>9__2 = delegate()
						{
							this.CancelPlayerAnimation();
							this.moveSpeedMultipliers.Remove(this.pickupFloatRef);
							this._pickup.gameObject.SetActive(false);
						});
					}
					DOVirtual.DelayedCall(delay, callback, true).SetUpdate(false);
				}, true).SetUpdate(false);
			}, null);
			this.lastPickupTime = Time.time;
		}

		// Token: 0x060109B0 RID: 68016 RVA: 0x00282AA8 File Offset: 0x00280CA8
		public void Infuse()
		{
			this.moveSpeedMultipliers.Remove(this.pickupFloatRef);
			this.moveSpeedMultipliers.Add(this.pickupFloatRef);
			AudioManager.Instance.PlayAudio(SingletonBehaviour<Prefabs>.Instance.pickupSound, 0.25f, 0f, 1f, 1f, false);
			Tween pickupTween = this._pickupTween;
			if (pickupTween != null)
			{
				pickupTween.Kill(false);
			}
			this.SetPlayerAnimation(new PlayerAnimation
			{
				facingDirection = this.facingDirection,
				hideUseItem = true,
				hold = WeaponHold.NoGrip,
				swing = SwingAnimation.SpellCharge
			});
			this._pickupTween = DOVirtual.DelayedCall(0.1f, delegate
			{
				this.SetPlayerAnimation(new PlayerAnimation
				{
					facingDirection = this.facingDirection,
					hideUseItem = true,
					hold = WeaponHold.NoGrip,
					swing = SwingAnimation.SpellCast
				});
				DOVirtual.DelayedCall(0.3f, delegate
				{
					this.CancelPlayerAnimation();
					this.moveSpeedMultipliers.Remove(this.pickupFloatRef);
				}, true);
			}, true);
		}

		// Token: 0x060109B1 RID: 68017 RVA: 0x00282B5D File Offset: 0x00280D5D
		public void Pause()
		{
			this._paused = true;
		}

		// Token: 0x060109B2 RID: 68018 RVA: 0x00282B68 File Offset: 0x00280D68
		public void SetPosition(Vector3 position, bool resetPlayerCamera = true)
		{
			base.transform.position = position;
			float currentDepth = SingletonBehaviour<GameManager>.Instance.Depth(base.transform.position, false);
			this._currentDepth = currentDepth;
			this._movement.StopJump();
			this.OnLand = null;
			this.Grounded = true;
			this.LastGroundedTime = Time.time;
			if (SceneSettingsManager.Instance.GetCurrentSceneSettings != null && SceneSettingsManager.Instance.GetCurrentSceneSettings.interior)
			{
				this.UnMount();
			}
			this.graphics.transform.position = new Vector3(base.transform.position.x, base.transform.position.y, this._currentDepth);
			if (resetPlayerCamera)
			{
				this.InJumpZone = false;
				this.ResetPlayerCamera();
			}
			Pet playerPet = this.PlayerPet;
			if (playerPet == null)
			{
				return;
			}
			playerPet.Initialize();
		}

		// Token: 0x060109B3 RID: 68019 RVA: 0x00282C4C File Offset: 0x00280E4C
		public void RequestSleep(Bed bed, bool isMarriageBed = false, MarriageOvernightCutscene marriageOvernightCutscene = null, bool isCutsceneComplete = false)
		{
			if (SingletonBehaviour<DayCycle>.Instance.Time.Hour >= 18 || SingletonBehaviour<DayCycle>.Instance.Time.Hour < 6)
			{
				if (!isMarriageBed)
				{
					DialogueController.Instance.SetDefaultBox("");
					DialogueController instance = DialogueController.Instance;
					DialogueNode dialogueNode = new DialogueNode();
					dialogueNode.dialogueText = new List<string>
					{
						ScriptLocalization.SleepRequest
					};
					Dictionary<int, Response> dictionary = new Dictionary<int, Response>();
					int key = 0;
					Response response = new Response();
					response.responseText = (() => ScriptLocalization.Yes);
					response.action = delegate()
					{
						this.StartSleep(bed);
					};
					dictionary.Add(key, response);
					int key2 = 1;
					Response response2 = new Response();
					response2.responseText = (() => ScriptLocalization.No);
					response2.action = delegate()
					{
						DialogueController.Instance.CancelDialogue(true, null, true);
						this.Sleeping = false;
					};
					dictionary.Add(key2, response2);
					dialogueNode.responses = dictionary;
					instance.PushDialogue(dialogueNode, null, true, false);
				}
				else if (!isCutsceneComplete)
				{
					DialogueController.Instance.SetDefaultBox("");
					DialogueController instance2 = DialogueController.Instance;
					DialogueNode dialogueNode2 = new DialogueNode();
					dialogueNode2.dialogueText = new List<string>
					{
						ScriptLocalization.SleepRequestSpouse
					};
					Dictionary<int, Response> dictionary2 = new Dictionary<int, Response>();
					int key3 = 0;
					Response response3 = new Response();
					response3.responseText = (() => ScriptLocalization.Yes);
					response3.action = delegate()
					{
						marriageOvernightCutscene.Begin();
					};
					dictionary2.Add(key3, response3);
					int key4 = 1;
					Response response4 = new Response();
					response4.responseText = (() => ScriptLocalization.No);
					response4.action = delegate()
					{
						DialogueController.Instance.CancelDialogue(true, null, true);
						this.Sleeping = false;
					};
					dictionary2.Add(key4, response4);
					dialogueNode2.responses = dictionary2;
					instance2.PushDialogue(dialogueNode2, null, true, false);
				}
				else
				{
					this.StartSleep(bed);
				}
			}
			else
			{
				DialogueController.Instance.SetDefaultBox("");
				DialogueController.Instance.PushDialogue(new DialogueNode
				{
					dialogueText = new List<string>
					{
						ScriptLocalization.TooEarlyToSleep
					}
				}, null, true, false);
			}
			this._paused = true;
			this.OnUnpausePlayer = (UnityAction)Delegate.Combine(this.OnUnpausePlayer, new UnityAction(delegate()
			{
				DialogueController.Instance.CancelDialogue();
				this.Sleeping = false;
			}));
		}

		// Token: 0x060109B4 RID: 68020 RVA: 0x00282EB4 File Offset: 0x002810B4
		public void PassOut()
		{
			if (this.Sleeping)
			{
				return;
			}
			DialogueController.Instance.CancelDialogue(false, null, true);
			UIHandler instance = UIHandler.Instance;
			if (instance != null)
			{
				instance.CloseExternalUI();
			}
			this._pauseObjects.Clear();
			this.AddPauseObject("passout");
			DialogueController.Instance.SetDefaultBox("");
			AudioManager.Instance.PlayAudio(SingletonBehaviour<Prefabs>.Instance.passOutSound, 0.25f, 0f, 1f, 1f, false);
			Cart.currentRoom = 0;
			Cart.rewardRoom = "";
			CombatDungeon.CurrentFloor = 0;
			Utilities.UnlockAcheivement(83);
			this.EndInteraction();
			try
			{
				UnityAction unityAction = Player.onPassOut;
				if (unityAction != null)
				{
					unityAction();
				}
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
			this.partyState = 0;
			DialogueController.Instance.PushDialogue(new DialogueNode
			{
				dialogueText = new List<string>
				{
					ScriptLocalization.YouPassedOut
				}
			}, delegate
			{
				this.PassOutSleep();
			}, true, false);
			this.passoutSleepTween = DOVirtual.DelayedCall(4f, delegate
			{
				this.PassOutSleep();
			}, true);
		}

		// Token: 0x060109B5 RID: 68021 RVA: 0x00282FD8 File Offset: 0x002811D8
		private void PassOutSleep()
		{
			this.PassedOut = true;
			Tween tween = this.passoutSleepTween;
			if (tween != null)
			{
				tween.Kill(false);
			}
			DialogueController.Instance.CancelDialogue();
			int progressIntCharacter = SingletonBehaviour<GameSave>.Instance.GetProgressIntCharacter("TimesPassedOut");
			SingletonBehaviour<GameSave>.Instance.SetProgressIntCharacter("TimesPassedOut", progressIntCharacter + 1);
			if (SceneSettingsManager.Instance.GetCurrentSceneSettings.mapType != MapType.House && SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("FirstTimePassedOut"))
			{
				if (progressIntCharacter > 0)
				{
					this.AddMoneyAndRegisterSource(-Mathf.Max(300, (int)(0.05f * (float)GameSave.Coins)), 60101, 1, MoneySource.Exploration, true, true);
				}
				else
				{
					this.AddMoneyAndRegisterSource(-300, 60101, 1, MoneySource.Exploration, true, true);
				}
			}
			this.StartSleep(null);
		}

		// Token: 0x060109B6 RID: 68022 RVA: 0x00283098 File Offset: 0x00281298
		public void SkipSleep()
		{
			SingletonBehaviour<GameSave>.Instance.SaveGame();
			SingletonBehaviour<GameSave>.Instance.WriteCharacterToFile(false);
			SingletonBehaviour<DayCycle>.Instance.GoToNextDay();
			SingletonBehaviour<WorldController>.Instance.UpdateCharacterOvernight();
			if (!GameManager.Multiplayer || GameManager.Host)
			{
				SingletonBehaviour<WorldController>.Instance.UpdateWorldForOverNight();
			}
			SingletonBehaviour<GameSave>.Instance.SaveGame();
			UnityAction unityAction = Player.onCompleteSleep;
			if (unityAction != null)
			{
				unityAction();
			}
			SingletonBehaviour<DayCycle>.Instance.TransitioningDays = false;
			this.AddMoneyOvernight();
			this.FinishSleeping();
		}

		// Token: 0x060109B7 RID: 68023 RVA: 0x00283118 File Offset: 0x00281318
		private void AddMoneyOvernight()
		{
			this.AddMoneyAndRegisterSource(Mathf.RoundToInt(this.GetStat(StatType.MoneyPerDay)), 60101, 1, MoneySource.Exploration, false, false);
			this.AddTicketsAndRegisterSource(Mathf.RoundToInt(this.GetStat(StatType.TicketsPerDay)), 60100, 1, MoneySource.Exploration, false, false);
			this.AddOrbsAndRegisterSource(Mathf.RoundToInt(this.GetStat(StatType.OrbsPerDay)), 60100, 1, MoneySource.Exploration, false, false);
			if (GameManager.Owner && GameSave.Mining.GetNode("Mining10c", true))
			{
				this.AddMoneyAndRegisterSource((int)(Mathf.Min((float)(GameSave.Mining.GetNodeAmount("Mining10c", 3, true) * 400), (float)GameSave.Coins * 0.02f) * (1f + (float)GameSave.Mining.GetNodeAmount("Mining8c", 3, true) * 0.1f)), 60101, 1, MoneySource.Exploration, false, false);
			}
			if (GameManager.Owner && GameSave.Farming.GetNode("Farming5d", true))
			{
				this.AddMoneyAndRegisterSource(10 + (SingletonBehaviour<NPCManager>.Instance.animals.Count + SingletonBehaviour<NPCManager>.Instance.pets.Count) * GameSave.Farming.GetNodeAmount("Farming5d", 3, true) * 10, 60101, 1, MoneySource.Exploration, false, false);
			}
			if (GameSave.Exploration.GetNode("Exploration5d", true))
			{
				int num = 0;
				foreach (KeyValuePair<string, float> keyValuePair in SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Relationships)
				{
					NPCAI npcai;
					if (SingletonBehaviour<NPCManager>.Instance._npcs.TryGetValue(keyValuePair.Key, out npcai) && npcai.Romanceable)
					{
						num += (int)(keyValuePair.Value / 5f);
					}
				}
				this.AddMoneyAndRegisterSource(Mathf.Min(500, num * (5 + GameSave.Exploration.GetNodeAmount("Exploration5d", 3, true) * 5)), 60101, 1, MoneySource.Exploration, false, false);
			}
			if (GameSave.Mining.GetNode("Mining7c", true))
			{
				HashSet<Decoration> hashSet = new HashSet<Decoration>();
				foreach (KeyValuePair<Vector3Int, Decoration> keyValuePair2 in SingletonBehaviour<GameManager>.Instance.objects)
				{
					hashSet.Add(keyValuePair2.Value);
				}
				this.AddMoneyAndRegisterSource((int)((float)(Mathf.Min(hashSet.Count, 40 * GameSave.Mining.GetNodeAmount("Mining7c", 3, true) - 20) * 4) * (1f + (float)GameSave.Mining.GetNodeAmount("Mining8c", 3, true) * 0.1f)), 60101, 1, MoneySource.Exploration, false, false);
			}
		}

		// Token: 0x060109B8 RID: 68024 RVA: 0x002833D4 File Offset: 0x002815D4
		public void SetJumpZone(float jumpHeight)
		{
			this.lastJumpHeight = jumpHeight;
			this.jumpZoneHeight = jumpHeight;
		}

		// Token: 0x060109B9 RID: 68025 RVA: 0x002833E4 File Offset: 0x002815E4
		public void SetJumpZoneSpot(Transform spot)
		{
			this.lastJumpPos = spot.transform.position;
		}

		// Token: 0x060109BA RID: 68026 RVA: 0x002833FC File Offset: 0x002815FC
		public void AddEXP(ProfessionType profession, float amount)
		{
			if (amount <= 0f)
			{
				return;
			}
			float num = 0f;
			switch (profession)
			{
			case ProfessionType.Farming:
				num = this.GetStat(StatType.BonusFarmingEXP);
				break;
			case ProfessionType.Exploration:
				num = this.GetStat(StatType.BonusWoodcuttingEXP);
				break;
			}
			amount = amount * (1f + this.GetStat(StatType.BonusExperience)) * (1f + num);
			SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.AddExp(profession, amount);
			UIHandler.Instance.endOfDayScreen.RegisterXP(profession, amount);
		}

		// Token: 0x060109BB RID: 68027 RVA: 0x00283489 File Offset: 0x00281689
		public void SetEXP(ProfessionType profession, float amount)
		{
			SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Professions[profession].experience = amount;
			this.RecalculateProfessionLevel(profession);
		}

		// Token: 0x060109BC RID: 68028 RVA: 0x002834B4 File Offset: 0x002816B4
		public int MaxLevel()
		{
			int num = 1;
			foreach (KeyValuePair<ProfessionType, Profession> keyValuePair in SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Professions)
			{
				int level = keyValuePair.Value.level;
				if (level > num)
				{
					num = level;
				}
			}
			return num;
		}

		// Token: 0x060109BD RID: 68029 RVA: 0x00283524 File Offset: 0x00281724
		public void AddMoney(int amount, bool playAudio = true, bool showNotification = false, bool spawnText = true)
		{
			if (amount == 0)
			{
				return;
			}
			if (amount > 0)
			{
				amount = (int)((1f + this.GetStat(StatType.GoldGain)) * (float)amount);
				SingletonBehaviour<GameSave>.Instance.AddProgressIntCharacter("GoldCollected", amount);
			}
			SingletonBehaviour<GameSave>.Instance.AddCoins(this.ID, amount, 1, 1, MoneySource.ShippingPortal, true, playAudio);
			if (spawnText)
			{
				FloatingTextManager.Instance.SpawnFloatingGoldText(amount, this.graphics.transform.position + new Vector3(0f, 3f, 0f));
			}
			if (showNotification)
			{
				SingletonBehaviour<NotificationStack>.Instance.SendNotification(ScriptLocalization.Coins_Name, 60000, amount, false, false);
			}
		}

		// Token: 0x060109BE RID: 68030 RVA: 0x002835C8 File Offset: 0x002817C8
		public void AddMoneyAndRegisterSource(int amount, int itemID, int itemAmount, MoneySource source, bool playAudio = true, bool showNotification = false)
		{
			if (amount == 0)
			{
				return;
			}
			if (amount > 0)
			{
				amount = (int)((1f + this.GetStat(StatType.GoldGain)) * (float)amount);
				SingletonBehaviour<GameSave>.Instance.AddProgressIntCharacter("GoldCollected", amount);
			}
			SingletonBehaviour<GameSave>.Instance.AddCoins(this.ID, amount, itemID, itemAmount, source, true, playAudio);
			FloatingTextManager.Instance.SpawnFloatingGoldText(amount, this.graphics.transform.position + new Vector3(0f, 3f, 0f));
			if (amount > 0)
			{
				UIHandler.Instance.endOfDayScreen.RegisterMoney(GoldType.Gold, amount, itemID, itemAmount, source);
			}
			if (showNotification)
			{
				SingletonBehaviour<NotificationStack>.Instance.SendNotification(ScriptLocalization.Coins_Name, 60000, amount, false, false);
			}
		}

		// Token: 0x060109BF RID: 68031 RVA: 0x00283684 File Offset: 0x00281884
		public void AddOrbs(int amount)
		{
			if (amount == 0)
			{
				return;
			}
			if (amount > 0)
			{
				amount = (int)((1f + this.GetStat(StatType.GoldGain)) * (float)amount);
				SingletonBehaviour<GameSave>.Instance.AddProgressIntCharacter("OrbsCollected", amount);
			}
			SingletonBehaviour<GameSave>.Instance.AddOrbs(this.ID, amount, 1, 1, MoneySource.ShippingPortal, true, true);
			FloatingTextManager.Instance.SpawnFloatingOrbsText(amount, this.graphics.transform.position + new Vector3(0f, 3f, 0f));
		}

		// Token: 0x060109C0 RID: 68032 RVA: 0x00283708 File Offset: 0x00281908
		public void AddOrbsAndRegisterSource(int amount, int itemID, int itemAmount, MoneySource source, bool playAudio = true, bool showNotification = false)
		{
			if (amount == 0)
			{
				return;
			}
			if (amount > 0)
			{
				amount = (int)((1f + this.GetStat(StatType.GoldGain)) * (float)amount);
				SingletonBehaviour<GameSave>.Instance.AddProgressIntCharacter("OrbsCollected", amount);
			}
			SingletonBehaviour<GameSave>.Instance.AddOrbs(this.ID, amount, itemID, itemAmount, source, true, playAudio);
			FloatingTextManager.Instance.SpawnFloatingOrbsText(amount, this.graphics.transform.position + new Vector3(0f, 3f, 0f));
			if (amount > 0)
			{
				UIHandler.Instance.endOfDayScreen.RegisterMoney(GoldType.Orbs, amount, itemID, itemAmount, source);
			}
			if (showNotification)
			{
				SingletonBehaviour<NotificationStack>.Instance.SendNotification(ScriptLocalization.ManaOrbs_Name, 60001, amount, false, false);
			}
			if (!SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("ManaOrbs"))
			{
				SingletonBehaviour<HelpTooltips>.Instance.SendNotification(ScriptLocalization.ManaOrbs_Name, ScriptLocalization.Tooltips_ManaOrbs_Text, new List<ValueTuple<Transform, Vector3, Direction>>(), 15, delegate
				{
					SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("ManaOrbs", true);
				});
			}
		}

		// Token: 0x060109C1 RID: 68033 RVA: 0x0028380C File Offset: 0x00281A0C
		public void AddTickets(int amount)
		{
			if (amount == 0)
			{
				return;
			}
			if (amount > 0)
			{
				amount = (int)((1f + this.GetStat(StatType.GoldGain)) * (float)amount);
				SingletonBehaviour<GameSave>.Instance.AddProgressIntCharacter("TicketsCollected", amount);
			}
			SingletonBehaviour<GameSave>.Instance.AddTickets(this.ID, amount, 1, 1, MoneySource.ShippingPortal, true, true);
			FloatingTextManager.Instance.SpawnFloatingTicketsText(amount, this.graphics.transform.position + new Vector3(0f, 3f, 0f));
		}

		// Token: 0x060109C2 RID: 68034 RVA: 0x00283890 File Offset: 0x00281A90
		public void AddTicketsAndRegisterSource(int amount, int itemID, int itemAmount, MoneySource source, bool playAudio = true, bool showNotification = false)
		{
			if (amount == 0)
			{
				return;
			}
			if (amount > 0)
			{
				amount = (int)((1f + this.GetStat(StatType.GoldGain)) * (float)amount);
				SingletonBehaviour<GameSave>.Instance.AddProgressIntCharacter("TicketsCollected", amount);
			}
			SingletonBehaviour<GameSave>.Instance.AddTickets(this.ID, amount, itemID, itemAmount, source, true, playAudio);
			FloatingTextManager.Instance.SpawnFloatingTicketsText(amount, this.graphics.transform.position + new Vector3(0f, 3f, 0f));
			if (amount > 0)
			{
				UIHandler.Instance.endOfDayScreen.RegisterMoney(GoldType.Tickets, amount, itemID, itemAmount, source);
			}
			if (showNotification)
			{
				SingletonBehaviour<NotificationStack>.Instance.SendNotification(ScriptLocalization.Tickets, 60002, amount, false, false);
			}
			if (!SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("Tickets"))
			{
				SingletonBehaviour<HelpTooltips>.Instance.SendNotification(ScriptLocalization.Tickets, ScriptLocalization.Tooltips_Tickets_Text, new List<ValueTuple<Transform, Vector3, Direction>>(), 16, delegate
				{
					SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("Tickets", true);
				});
			}
		}

		// Token: 0x060109C3 RID: 68035 RVA: 0x00283994 File Offset: 0x00281B94
		public void SetPlayerAnimation(PlayerAnimation animation)
		{
			this.OverrideAnimation = true;
			this.Animation = animation;
		}

		// Token: 0x060109C4 RID: 68036 RVA: 0x002839A4 File Offset: 0x00281BA4
		public void CancelPlayerAnimation()
		{
			this.OverrideAnimation = false;
			foreach (Component component in this.UseItemTransform.GetComponentsInChildren<Component>())
			{
				Renderer renderer = component as Renderer;
				if (renderer != null)
				{
					renderer.enabled = true;
				}
				MeshGenerator meshGenerator = component as MeshGenerator;
				if (meshGenerator != null)
				{
					meshGenerator.enabled = true;
				}
			}
		}

		// Token: 0x060109C5 RID: 68037 RVA: 0x002839F6 File Offset: 0x00281BF6
		public void SetFullPath(List<PathDescription> path, float pathMoveSpeed, UnityAction onComplete = null)
		{
			if (this._fullPathRoutine != null)
			{
				base.StopCoroutine(this._fullPathRoutine);
			}
			this._fullPathRoutine = base.StartCoroutine(this.FullPathRoutine(path, pathMoveSpeed, onComplete));
		}

		// Token: 0x060109C6 RID: 68038 RVA: 0x00283A21 File Offset: 0x00281C21
		public void CancelPath()
		{
			if (this._fullPathRoutine != null)
			{
				base.StopCoroutine(this._fullPathRoutine);
			}
		}

		// Token: 0x060109C7 RID: 68039 RVA: 0x00283A38 File Offset: 0x00281C38
		public void ResetPlayerCamera()
		{
			this._playerCamera.transform.localPosition = new Vector3(0f, -50f, this._currentDepth - 51f);
			this._cameraPosition = this._playerCamera.transform.position;
			this.OverrideCameraZoomLevel = false;
			this.SetZoom((float)Settings.Zoom, true);
		}

		// Token: 0x060109C8 RID: 68040 RVA: 0x00283A9C File Offset: 0x00281C9C
		public void ResetPlayerCamera(Vector3 from, float duration = 2.5f)
		{
			this.ControlCameraXY = false;
			this._playerCamera.transform.position = from;
			Vector3 vector = new Vector3(0f, -50f, this._currentDepth - 51f);
			if (duration <= 0.001f)
			{
				this._playerCamera.transform.localPosition = vector;
				this.ControlCameraXY = true;
				this._cameraPosition = this._playerCamera.transform.position;
				return;
			}
			this._playerCamera.transform.DOLocalMove(vector, duration, false).OnComplete(delegate
			{
				this.ControlCameraXY = true;
				this._cameraPosition = this._playerCamera.transform.position;
			});
		}

		// Token: 0x060109C9 RID: 68041 RVA: 0x00283B3A File Offset: 0x00281D3A
		public void SetYVelocity(float velocity)
		{
			this.rigidbody.velocity = new Vector2(this.rigidbody.velocity.x, velocity);
		}

		// Token: 0x060109CA RID: 68042 RVA: 0x00283B60 File Offset: 0x00281D60
		public void AirSkip(int manaCost)
		{
			if (this.IsOwner)
			{
				this._movement.Jump(this.GetStat(StatType.Jump) * 0.75f);
				if (this.input.magnitude < 0.1f)
				{
					switch (this.facingDirection)
					{
					case Direction.North:
						this.rigidbody.velocity = new Vector2(0f, 0.8f) * 14f * (1f + this.GetStat(StatType.Movespeed) / 3f);
						break;
					case Direction.South:
						this.rigidbody.velocity = new Vector2(0f, -0.8f) * 14f * (1f + this.GetStat(StatType.Movespeed) / 3f);
						break;
					case Direction.East:
						this.rigidbody.velocity = new Vector2(0.8f, 0f) * 14f * (1f + this.GetStat(StatType.Movespeed) / 3f);
						break;
					case Direction.West:
						this.rigidbody.velocity = new Vector2(-0.8f, 0f) * 14f * (1f + this.GetStat(StatType.Movespeed) / 3f);
						break;
					}
				}
				else
				{
					this.rigidbody.velocity = this.input.normalized * 14f * (1f + this.GetStat(StatType.Movespeed) / 3f);
				}
				this.targetVelocity = this.rigidbody.velocity;
				DOVirtual.Float(2f, 10f, 0.65f, delegate(float value)
				{
					this._airborneSmoothFactor = value;
				}).SetEase(Ease.InQuad);
				if (!Utilities.Chance(this.GetStat(StatType.FreeAirSkipChance)))
				{
					this.UseMana((float)manaCost);
				}
				int airSkipsUsed = this.AirSkipsUsed;
				this.AirSkipsUsed = airSkipsUsed + 1;
				SingletonBehaviour<GameSave>.Instance.AddProgressIntCharacter("TimesAirSkipped", 1);
				UnityAction unityAction = Player.onAirSkip;
				if (unityAction != null)
				{
					unityAction();
				}
			}
			if (ParticleManager.Instance && AudioManager.Instance)
			{
				GameObject gameObject = ParticleManager.Instance.InstantiateAnimation(this._flashJumpParticle, this.graphics.position + new Vector3(0f, 0.65f, -0.65f));
				gameObject.transform.rotation = Utilities.RotationFromDirection(this.facingDirection);
				gameObject.transform.localScale = Utilities.ScaleFromDirection(this.facingDirection);
				AudioManager.Instance.PlayOneShot(this._flashJump, this.graphics.position, 0.2f, 0f, 1f, 1f, 7.5f, 15f, 8);
			}
		}

		// Token: 0x060109CB RID: 68043 RVA: 0x00283E38 File Offset: 0x00282038
		private void LevelUp(ProfessionType professionType, int level)
		{
			ParticleManager.Instance.InstantiateAnimation(this._levelUpParticle, this.graphics.position + new Vector3(0f, -0.25f, 0f), this.graphics.transform, true);
			AudioManager.Instance.PlayOneShot(this.levelUpSound, this.graphics.transform.position, 0.4f, 0.1f, 1f, 1f, 7.5f, 15f, 8);
		}

		// Token: 0x060109CC RID: 68044 RVA: 0x00283EC5 File Offset: 0x002820C5
		private void SetAnimatorBool(Animator animator, Direction direction)
		{
			animator.SetBool("North", direction == Direction.North);
			animator.SetBool("South", direction == Direction.South);
			animator.SetBool("East", direction == Direction.East);
			animator.SetBool("West", direction == Direction.West);
		}

		// Token: 0x060109CD RID: 68045 RVA: 0x00283F03 File Offset: 0x00282103
		private IEnumerator PauseCoroutine(float duration)
		{
			this.AddPauseObject("pause");
			yield return new WaitForSeconds(duration);
			this.RemovePauseObject("pause");
			yield break;
		}

		// Token: 0x060109CE RID: 68046 RVA: 0x00283F1C File Offset: 0x0028211C
		private void StartSleep(Bed bed)
		{
			Debug.Log("Sleeping!");
			this.CompleteOvernight = false;
			this.LeftBed = false;
			this.Sleeping = true;
			this.facingDirection = Direction.South;
			if (bed)
			{
				base.transform.position = bed.SleepingSpot;
			}
		}

		// Token: 0x060109CF RID: 68047 RVA: 0x00283F68 File Offset: 0x00282168
		private void CheckIfAllPlayersSleeping()
		{
			if (GameManager.Multiplayer)
			{
				using (Dictionary<int, NetworkGamePlayer>.ValueCollection.Enumerator enumerator = NetworkLobbyManager.Instance.GamePlayers.Values.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (!enumerator.Current.sleeping)
						{
							return;
						}
					}
				}
				this.Sleep();
				return;
			}
			this.Sleep();
		}

		// Token: 0x060109D0 RID: 68048 RVA: 0x00283FDC File Offset: 0x002821DC
		private void Sleep()
		{
			this.Sleeping = true;
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolWorld("Overnight", false, true);
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolWorld("OvernightRoutine", false, true);
			Debug.Log("Sleeping now!");
			SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("Slept", true);
			SingletonBehaviour<DayCycle>.Instance.MiddleOfNight = true;
			CutsceneProgressManager.HandleCutsceneProgress();
			this.Sleeping = true;
			UnityAction onStartSleep = this.OnStartSleep;
			if (onStartSleep != null)
			{
				onStartSleep();
			}
			PlayerInput.DisableInput("sleep");
			this.CompleteOvernight = false;
			SingletonBehaviour<DayCycle>.Instance.TransitioningDays = true;
			UIHandler instance = UIHandler.Instance;
			instance.OnCompleteOvernight = (UnityAction)Delegate.Combine(instance.OnCompleteOvernight, new UnityAction(this.CompleteSleep));
			SingletonBehaviour<ScenePortalManager>.Instance.ScreenFadeOut(Color.black, 100f, delegate
			{
				SingletonBehaviour<GameSave>.Instance.SaveGame();
				SingletonBehaviour<GameSave>.Instance.WriteCharacterToFile(false);
				SingletonBehaviour<GameSave>.Instance.WriteCharacterRollingBackupToFile();
				Shader.SetGlobalFloat("_GlobalLightIntensity", 1f);
				if (this.PassedOut)
				{
					if (SingletonBehaviour<DayCycle>.Instance.CurrentTownType == TownType.Withergate && SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("Apartment"))
					{
						SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(63.5f, 54.624f), "WithergatePlayerApartment", delegate
						{
							this.Overnight();
						}, null, null, SceneFadeType.None, 1f);
						return;
					}
					if (SingletonBehaviour<DayCycle>.Instance.CurrentTownType == TownType.Nelvari && SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("NelvariFarm"))
					{
						SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(51.5f, 54.97755f), "NelvariPlayerHouse", delegate
						{
							this.Overnight();
						}, null, null, SceneFadeType.None, 1f);
						return;
					}
					if (SingletonBehaviour<DayCycle>.Instance.CurrentTownType == TownType.GreatCity && SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("UnlockedGCApartment"))
					{
						SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(398.9583f, 536.0945f), "GCMainDistrictPlayerApartment", delegate
						{
							this.Overnight();
						}, null, null, SceneFadeType.None, 1f);
						return;
					}
					SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(80.83334f, 65.58415f), "Hospital", delegate
					{
						this.Overnight();
					}, null, null, SceneFadeType.None, 1f);
					return;
				}
				else
				{
					if (SceneSettingsManager.Instance.GetCurrentSceneSettings && SceneSettingsManager.Instance.GetCurrentSceneSettings.mapType == MapType.House)
					{
						this.Overnight();
						return;
					}
					if (SingletonBehaviour<DayCycle>.Instance.CurrentTownType == TownType.Withergate)
					{
						SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(63.5f, 54.624f), "WithergatePlayerApartment", delegate
						{
							this.Overnight();
						}, null, null, SceneFadeType.None, 1f);
						return;
					}
					if (SingletonBehaviour<DayCycle>.Instance.CurrentTownType == TownType.Nelvari)
					{
						SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(51.5f, 54.97755f), "NelvariPlayerHouse", delegate
						{
							this.Overnight();
						}, null, null, SceneFadeType.None, 1f);
						return;
					}
					if (SingletonBehaviour<DayCycle>.Instance.CurrentTownType == TownType.GreatCity && SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("UnlockedGCApartment"))
					{
						SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(398.9583f, 536.0945f), "GCMainDistrictPlayerApartment", delegate
						{
							this.Overnight();
						}, null, null, SceneFadeType.None, 1f);
						return;
					}
					SingletonBehaviour<ScenePortalManager>.Instance.ChangeScene(new Vector2(48f, 68f), "Tier1House0", delegate
					{
						this.Overnight();
					}, null, null, SceneFadeType.None, 1f);
					return;
				}
			});
		}

		// Token: 0x060109D1 RID: 68049 RVA: 0x002840B8 File Offset: 0x002822B8
		private void CompleteSleep()
		{
			SingletonBehaviour<ScenePortalManager>.Instance.ScreenFadeIn(Color.black, 100f, delegate
			{
				base.StartCoroutine(this.CompleteOvernightRoutine());
			});
			SingletonBehaviour<DayCycle>.Instance.TransitioningDays = false;
			SingletonBehaviour<DayCycle>.Instance.DayEnding = false;
			SingletonBehaviour<DayCycle>.Instance.MiddleOfNight = false;
		}

		// Token: 0x060109D2 RID: 68050 RVA: 0x00284106 File Offset: 0x00282306
		private IEnumerator CompleteOvernightRoutine()
		{
			yield return null;
			UnityAction unityAction = Player.onCompleteSleep;
			if (unityAction != null)
			{
				unityAction();
			}
			yield return SingletonBehaviour<ScenePortalManager>.Instance.InitializeScene();
			yield break;
		}

		// Token: 0x060109D3 RID: 68051 RVA: 0x0028410E File Offset: 0x0028230E
		private void Overnight()
		{
			base.StartCoroutine(this.OvernightRoutine());
		}

		// Token: 0x060109D4 RID: 68052 RVA: 0x0028411D File Offset: 0x0028231D
		private IEnumerator OvernightRoutine()
		{
			bool host = !GameManager.Multiplayer || GameManager.Host;
			SingletonBehaviour<DayCycle>.Instance.GoToNextDay();
			SingletonBehaviour<WorldController>.Instance.UpdateCharacterOvernight();
			if (host)
			{
				SingletonBehaviour<WorldController>.Instance.UpdateWorldForOverNight();
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolWorld("Overnight", true, true);
			}
			this.AddMoneyOvernight();
			this.AddHealthAndManaOvernight();
			UnityAction onBeginOvernightScreen = this.OnBeginOvernightScreen;
			if (onBeginOvernightScreen != null)
			{
				onBeginOvernightScreen();
			}
			if (GameManager.Multiplayer)
			{
				yield return new WaitForSeconds(3f);
			}
			yield return this.overnightQueue.ProcessCoroutines();
			if (host)
			{
				SingletonBehaviour<GameSave>.Instance.SetProgressBoolWorld("OvernightRoutine", true, true);
			}
			yield return new WaitForSeconds(2.5f);
			AsyncOperation unloadResources = Resources.UnloadUnusedAssets();
			while (unloadResources != null && !unloadResources.isDone)
			{
				yield return null;
			}
			UIHandler.Instance.ShowOvernightUI();
			UnityAction onSleeping = this.OnSleeping;
			if (onSleeping != null)
			{
				onSleeping();
			}
			yield break;
		}

		// Token: 0x060109D5 RID: 68053 RVA: 0x0028412C File Offset: 0x0028232C
		public void FinishSleeping()
		{
			Debug.Log("Finish sleeping");
			this.Sleeping = false;
			this.PassedOut = false;
			SingletonBehaviour<NPCManager>.Instance.GenerateNPCPaths(true);
			SingletonBehaviour<NPCManager>.Instance.GenerateNPCQuestsAndCycles(true);
			AudioManager.Instance.PlayAudio(this.wakeUpSound, 0.8f, 1f, 1f, 1f, false);
			PlayerInput.EnableInput("sleep");
			this.RemovePauseObject("passout");
			if (this.IsOwner && GameSave.Exploration.GetNode("Exploration9d", true))
			{
				float num = 0f;
				foreach (KeyValuePair<string, float> keyValuePair in SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Relationships)
				{
					num += keyValuePair.Value;
					if (num >= 50f)
					{
						break;
					}
				}
				if (num >= 50f)
				{
					this.Inventory.AddItem(18013, GameSave.Exploration.GetNodeAmount("Exploration9d", 3, true), true);
				}
			}
			if (this.IsOwner && !SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter("Saving"))
			{
				SingletonBehaviour<HelpTooltips>.Instance.SendNotification(ScriptLocalization.Tooltips_Saving_Title, ScriptLocalization.Tooltips_Saving_Text, new List<ValueTuple<Transform, Vector3, Direction>>(), 24, delegate
				{
					SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter("Saving", true);
				});
			}
			int num2 = Mathf.RoundToInt(this.GetStat(StatType.CommunityTokenPerDay));
			if (num2 > 0)
			{
				this.Inventory.AddItem(18013, num2, true);
			}
		}

		// Token: 0x060109D6 RID: 68054 RVA: 0x002842CC File Offset: 0x002824CC
		private void CalculateProfessionLevels()
		{
			foreach (ProfessionType profession in (ProfessionType[])Enum.GetValues(typeof(ProfessionType)))
			{
				this.RecalculateProfessionLevel(profession);
			}
		}

		// Token: 0x060109D7 RID: 68055 RVA: 0x00284308 File Offset: 0x00282508
		private void RecalculateProfessionLevel(ProfessionType profession)
		{
			if (SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Professions.ContainsKey(profession))
			{
				int levelFromExp = Profession.GetLevelFromExp(SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Professions[profession].experience);
				SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Professions[profession].level = levelFromExp;
				Skills.SetLevelProgress(profession, levelFromExp);
			}
		}

		// Token: 0x060109D8 RID: 68056 RVA: 0x0028437D File Offset: 0x0028257D
		private IEnumerator FullPathRoutine(List<PathDescription> path, float pathMoveSpeed, UnityAction onComplete)
		{
			if (path.Count <= 0)
			{
				yield break;
			}
			this.Pathing = true;
			bool destinationReached = false;
			this.SetTarget(path[0].location, pathMoveSpeed);
			if (path[0].teleport)
			{
				base.transform.position = path[0].location;
			}
			this.OnDestinationReached = (UnityAction)Delegate.Combine(this.OnDestinationReached, new UnityAction(delegate()
			{
				destinationReached = true;
			}));
			if (path.Count > 1)
			{
				UnityAction <>9__2;
				UnityAction <>9__3;
				int num2;
				for (int i = 0; i < path.Count - 1; i = num2 + 1)
				{
					while (!destinationReached)
					{
						yield return null;
					}
					int num = i;
					this.SetTarget(path[num + 1].location, pathMoveSpeed);
					if (path[num + 1].teleport)
					{
						base.transform.position = path[num + 1].location;
					}
					if (num == path.Count - 2)
					{
						Delegate onDestinationReached = this.OnDestinationReached;
						UnityAction b;
						if ((b = <>9__2) == null)
						{
							b = (<>9__2 = delegate()
							{
								this.Pathing = false;
								UnityAction onComplete2 = onComplete;
								if (onComplete2 == null)
								{
									return;
								}
								onComplete2();
							});
						}
						this.OnDestinationReached = (UnityAction)Delegate.Combine(onDestinationReached, b);
					}
					destinationReached = false;
					Delegate onDestinationReached2 = this.OnDestinationReached;
					UnityAction b2;
					if ((b2 = <>9__3) == null)
					{
						b2 = (<>9__3 = delegate()
						{
							destinationReached = true;
						});
					}
					this.OnDestinationReached = (UnityAction)Delegate.Combine(onDestinationReached2, b2);
					num2 = i;
				}
			}
			else
			{
				this.OnDestinationReached = (UnityAction)Delegate.Combine(this.OnDestinationReached, new UnityAction(delegate()
				{
					this.Pathing = false;
					UnityAction onComplete2 = onComplete;
					if (onComplete2 == null)
					{
						return;
					}
					onComplete2();
				}));
			}
			yield break;
		}

		// Token: 0x060109D9 RID: 68057 RVA: 0x002843A1 File Offset: 0x002825A1
		public void StopPath()
		{
			this.Pathing = false;
			if (this._fullPathRoutine != null)
			{
				base.StopCoroutine(this._fullPathRoutine);
			}
		}

		// Token: 0x060109DA RID: 68058 RVA: 0x002843C0 File Offset: 0x002825C0
		private void Path()
		{
			Vector2 vector = this._pathingLocation - new Vector2(base.transform.position.x, base.transform.position.y);
			if (vector.sqrMagnitude > 0.033f)
			{
				this.targetVelocity = vector.normalized * this._pathMoveSpeed;
				this.rigidbody.velocity = this.targetVelocity;
				this.input = this.rigidbody.velocity;
				return;
			}
			base.transform.position = new Vector3(this._pathingLocation.x, this._pathingLocation.y, base.transform.position.z);
			this.rigidbody.velocity = Vector2.zero;
			this.targetVelocity = Vector2.zero;
			this.input = this.rigidbody.velocity;
			UnityAction onDestinationReached = this.OnDestinationReached;
			if (onDestinationReached != null)
			{
				onDestinationReached();
			}
			this.OnDestinationReached = null;
		}

		// Token: 0x060109DB RID: 68059 RVA: 0x002844D0 File Offset: 0x002826D0
		public void SetTarget(Vector2 targetLocation, float pathMoveSpeed)
		{
			this._pathMoveSpeed = pathMoveSpeed;
			this._pathingLocation = targetLocation;
			Vector2 vector = this._pathingLocation - new Vector2(base.transform.position.x, base.transform.position.y);
			if (vector.sqrMagnitude > 0.001f)
			{
				this.targetVelocity = vector.normalized * this._pathMoveSpeed;
			}
		}

		// Token: 0x060109DC RID: 68060 RVA: 0x00284547 File Offset: 0x00282747
		public void Hit(float time, DamageInfo damageInfo, IDamageReceiver damageReceiver)
		{
			this.LastHitTime = time;
			if (!this.InCombat)
			{
				this.FirstHitWhileOutOfCombat = time;
				this.InCombat = true;
			}
			UnityAction<DamageInfo, IDamageReceiver> unityAction = this.onHit;
			if (unityAction == null)
			{
				return;
			}
			unityAction(damageInfo, damageReceiver);
		}

		// Token: 0x060109DD RID: 68061 RVA: 0x00284578 File Offset: 0x00282778
		public void PreHit(DamageInfo damageInfo, IDamageReceiver damageReceiver)
		{
			UnityAction<DamageInfo, IDamageReceiver> unityAction = this.onPreHit;
			if (unityAction == null)
			{
				return;
			}
			unityAction(damageInfo, damageReceiver);
		}

		// Token: 0x060109DE RID: 68062 RVA: 0x0028458C File Offset: 0x0028278C
		public void Hit(float time)
		{
			this.LastHitTime = time;
			if (!this.InCombat)
			{
				this.FirstHitWhileOutOfCombat = time;
				this.InCombat = true;
			}
		}

		// Token: 0x060109DF RID: 68063 RVA: 0x002845AC File Offset: 0x002827AC
		private void CheckForInCombat()
		{
			if (this.InCombat && this.TimeSinceLastHit > 7f)
			{
				if (this.sentInCombatEvent)
				{
					UnityAction onExitCombat = Player.OnExitCombat;
					if (onExitCombat != null)
					{
						onExitCombat();
					}
				}
				this.sentInCombatEvent = false;
				this.InCombat = false;
			}
			if (!this.sentInCombatEvent && this.InCombat && this.FirstHitWhileOutOfCombat + 4f < Time.time && this.TimeSinceLastHit < 1f)
			{
				this.sentInCombatEvent = true;
				UnityAction onEnterCombat = Player.OnEnterCombat;
				if (onEnterCombat == null)
				{
					return;
				}
				onEnterCombat();
			}
		}

		// Token: 0x060109E0 RID: 68064 RVA: 0x0028463A File Offset: 0x0028283A
		public void ShowGraphics()
		{
			this.graphics.Find("Layers").gameObject.SetActive(true);
			if (this.PlayerPet != null)
			{
				this.PlayerPet.gameObject.SetActive(true);
			}
		}

		// Token: 0x060109E1 RID: 68065 RVA: 0x00284676 File Offset: 0x00282876
		public void HideGraphics()
		{
			this.graphics.Find("Layers").gameObject.SetActive(false);
			if (this.PlayerPet != null)
			{
				this.PlayerPet.gameObject.SetActive(false);
			}
		}

		// Token: 0x060109E2 RID: 68066 RVA: 0x002846B2 File Offset: 0x002828B2
		public void ShowPetGraphics()
		{
			if (this.PlayerPet != null)
			{
				this.PlayerPet.gameObject.SetActive(true);
			}
		}

		// Token: 0x060109E3 RID: 68067 RVA: 0x002846D3 File Offset: 0x002828D3
		public void HidePetGraphics()
		{
			if (this.PlayerPet != null)
			{
				this.PlayerPet.gameObject.SetActive(false);
			}
		}

		// Token: 0x060109E4 RID: 68068 RVA: 0x002846F4 File Offset: 0x002828F4
		public void SetZoom(float zoomLevel, bool immediate = false)
		{
			float num = 22.5f / zoomLevel;
			this.SettingsCameraZoomLevel = num;
			if (immediate && !this.OverrideCameraZoomLevel)
			{
				Tween tween = this.zoomTween;
				if (tween != null)
				{
					tween.Kill(false);
				}
				this._playerCamera.orthographicSize = num;
			}
		}

		// Token: 0x060109E5 RID: 68069 RVA: 0x0028473C File Offset: 0x0028293C
		public void SetPet(Pet pet)
		{
			this.PlayerPet = pet;
			if (this.IsOwner)
			{
				if (pet != null)
				{
					if (GameSave.Farming.GetNode("Farming7d", true))
					{
						Player.petBuff.entity = this;
						float expAmount = 2f * (float)GameSave.Farming.GetNodeAmount("Farming7d", 3, true);
						Player.petBuff.onHourChange = delegate(int hour, int minute)
						{
							if (minute == 0)
							{
								this.AddEXP(ProfessionType.Exploration, expAmount);
							}
						};
						int num = GameSave.Farming.GetNodeAmount("Farming7d", 3, true);
						switch (num)
						{
						case 1:
							num = 1;
							break;
						case 2:
							num = 3;
							break;
						case 3:
							num = 5;
							break;
						}
						Player.petBuff.buffDescription = string.Concat(new string[]
						{
							"Receive <color=#B7FFA3>",
							expAmount.ToString(),
							" ",
							ProfessionType.Exploration.ToString(),
							" experience</color> every hour and grants a <color=#B7FFA3>bonus movement speed of ",
							num.ToString(),
							"</color>."
						});
						this.ReceiveBuff(BuffType.PetParade, Player.petBuff);
					}
				}
				else
				{
					this.FinishBuff(BuffType.PetParade);
				}
				UnityAction<PetItem> unityAction = this.onSetPet;
				if (unityAction != null)
				{
					PetItem arg;
					if (!pet)
					{
						(arg = new PetItem()).id = -1;
					}
					else
					{
						arg = pet.petItem;
					}
					unityAction(arg);
				}
				Debug.Log("On Set Pet " + (pet ? pet.petItem.id : -1).ToString());
			}
		}

		// Token: 0x060109E6 RID: 68070 RVA: 0x002848CA File Offset: 0x00282ACA
		public void DisplayChatBubble(string text)
		{
			this.chatBubbleStack.SendNotification(text);
		}

		// Token: 0x060109E7 RID: 68071 RVA: 0x002848D8 File Offset: 0x00282AD8
		public void SendChatMessage(string characterName, string message)
		{
			string str = "<color=#3969ff>" + characterName + ":</color> ";
			QuantumConsole.Instance.LogPlayerText(str + message);
		}

		// Token: 0x060109E8 RID: 68072 RVA: 0x002752EE File Offset: 0x002734EE
		public void SendChatMessage(string message)
		{
			QuantumConsole.Instance.LogPlayerText(message);
		}

		// Token: 0x060109E9 RID: 68073 RVA: 0x00284908 File Offset: 0x00282B08
		public void SendMail(MailAsset mail, bool forceRepeatSend = false)
		{
			if (mail == null)
			{
				return;
			}
			if (SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Mail.ContainsKey(mail.name))
			{
				return;
			}
			if (!forceRepeatSend && SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter(mail.name))
			{
				return;
			}
			List<ItemAmount> items = null;
			if (mail.items2 != null && mail.items2.Count > 0)
			{
				items = new List<ItemAmount>();
				using (List<SerializedItemDataAmount>.Enumerator enumerator = mail.items2.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						SerializedItemDataAmount mailItem = enumerator.Current;
						if (mailItem != null)
						{
							Database.GetData<ItemData>(mailItem.id, delegate(ItemData data)
							{
								items.Add(new ItemAmount
								{
									amount = mailItem.amount,
									item = data.GenerateItem()
								});
							}, null);
						}
					}
				}
			}
			SingletonBehaviour<GameSave>.Instance.CurrentSave.characterData.Mail[mail.name] = new MailData
			{
				items = items
			};
		}

		// Token: 0x060109EA RID: 68074 RVA: 0x00284A2C File Offset: 0x00282C2C
		public void CancelOutOfEverythingAndPrepareForCutscene()
		{
			DialogueController.Instance.CancelDialogue(false, null, true);
		}

		// Token: 0x060109EB RID: 68075 RVA: 0x00284A3C File Offset: 0x00282C3C
		public void Initialize()
		{
			if (this.IsOwner && Player.Instance == this)
			{
				this.ResetPlayerCamera();
				this.Inventory.RemoveAll(1250);
				this.Inventory.RemoveAll(1259);
				if (!this.Sleeping)
				{
					this.LeftBed = true;
				}
			}
		}

		// Token: 0x1700C2CC RID: 49868
		// (get) Token: 0x060109EC RID: 68076 RVA: 0x00005C25 File Offset: 0x00003E25
		public int Order
		{
			get
			{
				return 1;
			}
		}

		// Token: 0x1700C2CD RID: 49869
		// (get) Token: 0x060109ED RID: 68077 RVA: 0x00203053 File Offset: 0x00201253
		public Vector3 HitPosition
		{
			get
			{
				return base.transform.position;
			}
		}

		// Token: 0x060109EE RID: 68078 RVA: 0x00284A93 File Offset: 0x00282C93
		public void ReceiveBuff(BuffType buffType, Buff buff)
		{
			if (this._buffs.ContainsKey(buffType))
			{
				this._buffs[buffType].FinishBuff();
			}
			this._buffs[buffType] = buff;
			buff.StartBuff();
		}

		// Token: 0x060109EF RID: 68079 RVA: 0x00284AC7 File Offset: 0x00282CC7
		public void FinishBuff(BuffType buffType)
		{
			if (this._buffs.ContainsKey(buffType))
			{
				this._buffs[buffType].FinishBuff();
				this._buffs.Remove(buffType);
			}
		}

		// Token: 0x060109F0 RID: 68080 RVA: 0x00284AF8 File Offset: 0x00282CF8
		public void JumpRopeJump(float jumpMultiplier)
		{
			this._movement.Jump(jumpMultiplier);
			SingletonBehaviour<HelpTooltips>.Instance.CompleteNotification(9);
			UnityAction onJump = this.OnJump;
			if (onJump != null)
			{
				onJump();
			}
			this.lastJumpHeight = this.graphics.position.z;
			this.Grounded = false;
		}

		// Token: 0x060109F1 RID: 68081 RVA: 0x00284B4C File Offset: 0x00282D4C
		public void SaveCurrentPosition()
		{
			this.currentScene = ScenePortalManager.ActiveSceneName;
			this.currentPosition = base.transform.position;
			string str = "Save Player Position: ";
			string str2 = this.currentScene;
			string str3 = " ";
			Vector2 vector = this.currentPosition;
			Debug.Log(str + str2 + str3 + vector.ToString());
		}

		// Token: 0x060109F2 RID: 68082 RVA: 0x00284BA8 File Offset: 0x00282DA8
		public string CheckAmariType()
		{
			if (this._playerAnimationLayers.tail.sprite == null)
			{
				return "Cat";
			}
			if (this._playerAnimationLayers.tail.sprite.name.Contains("Fish") || this._playerAnimationLayers.tail.sprite.name.Contains("fish") || this._playerAnimationLayers.tail.sprite.name.Contains("merfolk"))
			{
				Debug.Log("Player is fish");
				return "Aquatic";
			}
			if (this._playerAnimationLayers.tail.sprite.name.Contains("Cat") || this._playerAnimationLayers.tail.sprite.name.Contains("cat"))
			{
				Debug.Log("Player is cat");
				return "Cat";
			}
			if (this._playerAnimationLayers.tail.sprite.name.Contains("Dog") || this._playerAnimationLayers.tail.sprite.name.Contains("dog"))
			{
				Debug.Log("Player is dog");
				return "Dog";
			}
			if (this._playerAnimationLayers.tail.sprite.name.Contains("Bird") || this._playerAnimationLayers.tail.sprite.name.Contains("bird"))
			{
				Debug.Log("Player is bird");
				return "Bird";
			}
			if (this._playerAnimationLayers.tail.sprite.name.Contains("Lizard") || this._playerAnimationLayers.tail.sprite.name.Contains("lizard"))
			{
				Debug.Log("Player is lizard");
				return "Lizard";
			}
			return null;
		}

		// Token: 0x060109F3 RID: 68083 RVA: 0x00284D90 File Offset: 0x00282F90
		public string CheckElementalType()
		{
			if (this._playerAnimationLayers.head.sprite.name.Contains("Head_Elemental") || this._playerAnimationLayers.head.sprite.name.Contains("head_elemental"))
			{
				Debug.Log("Player is fire");
				return "Fire";
			}
			if (this._playerAnimationLayers.head.sprite.name.Contains("Head18") || this._playerAnimationLayers.head.sprite.name.Contains("head18"))
			{
				Debug.Log("Player is water");
				return "Water";
			}
			return null;
		}

		// Token: 0x060109F4 RID: 68084 RVA: 0x00284E44 File Offset: 0x00283044
		public string CheckSkinTone()
		{
			if (this._playerAnimationLayers.head.sprite.name.Contains("Head01") || this._playerAnimationLayers.head.sprite.name.Contains("head01") || this._playerAnimationLayers.head.sprite.name.Contains("Head02") || this._playerAnimationLayers.head.sprite.name.Contains("head02") || this._playerAnimationLayers.head.sprite.name.Contains("Head3") || this._playerAnimationLayers.head.sprite.name.Contains("head3") || this._playerAnimationLayers.head.sprite.name.Contains("Head4") || this._playerAnimationLayers.head.sprite.name.Contains("head4"))
			{
				Debug.Log("Player skin tone is light");
				return "Light";
			}
			if (this._playerAnimationLayers.head.sprite.name.Contains("Head5") || this._playerAnimationLayers.head.sprite.name.Contains("head5") || this._playerAnimationLayers.head.sprite.name.Contains("Head6") || this._playerAnimationLayers.head.sprite.name.Contains("head6") || this._playerAnimationLayers.head.sprite.name.Contains("Head7") || this._playerAnimationLayers.head.sprite.name.Contains("head7"))
			{
				Debug.Log("Player skin tone is medium light");
				return "Medium Light";
			}
			if (this._playerAnimationLayers.head.sprite.name.Contains("Head8") || this._playerAnimationLayers.head.sprite.name.Contains("head8") || this._playerAnimationLayers.head.sprite.name.Contains("Head9") || this._playerAnimationLayers.head.sprite.name.Contains("head9") || this._playerAnimationLayers.head.sprite.name.Contains("Head10") || this._playerAnimationLayers.head.sprite.name.Contains("head10"))
			{
				Debug.Log("Player skin tone is medium dark");
				return "Medium Dark";
			}
			if (this._playerAnimationLayers.head.sprite.name.Contains("Head11") || this._playerAnimationLayers.head.sprite.name.Contains("head11") || this._playerAnimationLayers.head.sprite.name.Contains("Head12") || this._playerAnimationLayers.head.sprite.name.Contains("head12"))
			{
				Debug.Log("Player skin tone is dark");
				return "Dark";
			}
			return null;
		}

		// Token: 0x060109F5 RID: 68085 RVA: 0x002851C4 File Offset: 0x002833C4
		private void UpdateAudio()
		{
			this._footstepsTimer -= Time.deltaTime;
			if (this._footstepsTimer <= 0f && this.Grounded && !this.Mounted && !this.FreezeWalkAnimations && this.rigidbody.velocity.sqrMagnitude > 0.02f)
			{
				AudioManager.Instance.PlayAudio(this.GetFootstepsAudio(), this._footstepsVol, 0f, 1f, 1f, false);
				this._footstepsTimer = 0.325f / (this.FinalMovementSpeed / this.moveSpeed) * ((GameSave.CurrentCharacter.race == 3) ? 1.8f : 1f);
			}
		}

		// Token: 0x060109F6 RID: 68086 RVA: 0x00285284 File Offset: 0x00283484
		private AudioClip GetFootstepsAudio()
		{
			this._footstepsVol = 0.3f;
			if (GameSave.CurrentCharacter.race == 3)
			{
				return this._nagaFootstep;
			}
			TileInfo tileInfo = SingletonBehaviour<TileManager>.Instance.GetTileInfo(this.Position);
			if (tileInfo == TileInfo.Stone || tileInfo == TileInfo.Workshop || tileInfo == TileInfo.Brick || tileInfo == TileInfo.Patterned || tileInfo == TileInfo.WhiteStone || tileInfo == TileInfo.OceanBrick || tileInfo == TileInfo.Brinestones || tileInfo == TileInfo.BlackSkull || tileInfo == TileInfo.BlueSkull || tileInfo == TileInfo.BrownTiledFloor || tileInfo == TileInfo.CherryAndStrawberry || tileInfo == TileInfo.ChiseledFloralStone || tileInfo == TileInfo.DarkFloralStone || tileInfo == TileInfo.GoldenMoon || tileInfo == TileInfo.GrassyStoney || tileInfo == TileInfo.HeartCheckered || tileInfo == TileInfo.LightFloralStone || tileInfo == TileInfo.MottledBrownStone || tileInfo == TileInfo.MottledPinkStone || tileInfo == TileInfo.OrangeGilded || tileInfo == TileInfo.OvergrownPinkBrick || tileInfo == TileInfo.PinkBrick || tileInfo == TileInfo.PurpleMoon || tileInfo == TileInfo.Rainbow || tileInfo == TileInfo.RoseStone || tileInfo == TileInfo.RoundedStone || tileInfo == TileInfo.SeashellCheckered || tileInfo == TileInfo.SimpleStones || tileInfo == TileInfo.SpookyFloor || tileInfo == TileInfo.SteelPlate || tileInfo == TileInfo.SteppingStone || tileInfo == TileInfo.StitchedSpookyFloor || tileInfo == TileInfo.TeddyBear)
			{
				return this._stoneFootsteps;
			}
			if (tileInfo == TileInfo.Wood || tileInfo == TileInfo.WoodenPlank || tileInfo == TileInfo.OakPlank || tileInfo == TileInfo.DiagonalDarkOak || tileInfo == TileInfo.MismatchedWooden || tileInfo == TileInfo.OvergrownWoodenSlat || tileInfo == TileInfo.SimpleWoven || tileInfo == TileInfo.WoodenSteppingStone)
			{
				this._footstepsVol = 0.4f;
				return this._woodFootsteps;
			}
			if (tileInfo == TileInfo.BeachSand || tileInfo == TileInfo.SandFloor)
			{
				return this._dirtFootsteps;
			}
			if (tileInfo == TileInfo.GrassyFlower)
			{
				return this._grassFootsteps;
			}
			switch (SingletonBehaviour<TileManager>.Instance.GetFootstepType(this.Position))
			{
			case FootstepType.Grass:
			{
				SceneSettings sceneSettings;
				if (SingletonBehaviour<DayCycle>.Instance.Season != Season.Winter || !SceneSettingsManager.Instance.sceneDictionary.TryGetValue((int)ScenePortalManager.ActiveSceneIndex, out sceneSettings))
				{
					return this._grassFootsteps;
				}
				if (sceneSettings.hasSnow)
				{
					return this._snowFootsteps;
				}
				return this._grassFootsteps;
			}
			case FootstepType.Stone:
				return this._stoneFootsteps;
			case FootstepType.Wood:
				this._footstepsVol = 0.4f;
				return this._woodFootsteps;
			}
			return this._dirtFootsteps;
		}

		// Token: 0x04011B5D RID: 72541
		public static Player Instance;

		// Token: 0x04011B5E RID: 72542
		[Header("Player Settings")]
		[SerializeField]
		private float moveSpeed;

		// Token: 0x04011B5F RID: 72543
		public GameObject playerGraphics;

		// Token: 0x04011B60 RID: 72544
		[Header("References")]
		[SerializeField]
		private Transform _useItemsContainer;

		// Token: 0x04011B61 RID: 72545
		[SerializeField]
		private Transform _useItemTransform;

		// Token: 0x04011B62 RID: 72546
		private UseItem _useItem;

		// Token: 0x04011B63 RID: 72547
		[SerializeField]
		private Transform _spell1Transform;

		// Token: 0x04011B65 RID: 72549
		[SerializeField]
		private Transform _spell2Transform;

		// Token: 0x04011B67 RID: 72551
		[SerializeField]
		private Transform _spell3Transform;

		// Token: 0x04011B69 RID: 72553
		[SerializeField]
		private Transform _spell4Transform;

		// Token: 0x04011B6B RID: 72555
		[SerializeField]
		private MeshGenerator _pickup;

		// Token: 0x04011B6C RID: 72556
		[SerializeField]
		private PlayerInventory _inventory;

		// Token: 0x04011B6D RID: 72557
		[SerializeField]
		private CameraController _cameraController;

		// Token: 0x04011B6E RID: 72558
		[SerializeField]
		private QuestList _questList;

		// Token: 0x04011B6F RID: 72559
		[SerializeField]
		private Transform mountTransform;

		// Token: 0x04011B70 RID: 72560
		public ControllerOffset controllerOffset;

		// Token: 0x04011B71 RID: 72561
		[SerializeField]
		private ChatBubbleStack chatBubbleStack;

		// Token: 0x04011B72 RID: 72562
		private float depthCached;

		// Token: 0x04011B73 RID: 72563
		private Vector3 positionCachedForDepthCheck = Vector2.zero;

		// Token: 0x04011B74 RID: 72564
		private Tween chatBubbleTween;

		// Token: 0x04011B75 RID: 72565
		private PlayerMount mount;

		// Token: 0x04011B77 RID: 72567
		private float PreviousMaxHealth;

		// Token: 0x04011B79 RID: 72569
		private float PreviousMaxMana;

		// Token: 0x04011B7A RID: 72570
		[HideInInspector]
		public HashSet<FloatRef> moveSpeedMultipliers = new HashSet<FloatRef>();

		// Token: 0x04011B7B RID: 72571
		[HideInInspector]
		public HashSet<FloatRef> jumpMultipliers = new HashSet<FloatRef>();

		// Token: 0x04011B7C RID: 72572
		[HideInInspector]
		public Direction facingDirection = Direction.South;

		// Token: 0x04011B7D RID: 72573
		[HideInInspector]
		public Direction northSouthFacingDirection;

		// Token: 0x04011B7E RID: 72574
		[HideInInspector]
		public Statistics playerStatistics;

		// Token: 0x04011B7F RID: 72575
		[HideInInspector]
		public bool overrideFacingDirection;

		// Token: 0x04011B80 RID: 72576
		[HideInInspector]
		public bool rotateChestDirection;

		// Token: 0x04011B81 RID: 72577
		[HideInInspector]
		public bool inArena;

		// Token: 0x04011B82 RID: 72578
		[HideInInspector]
		public sbyte partyState;

		// Token: 0x04011B83 RID: 72579
		private HashSet<string> _pauseObjects = new HashSet<string>();

		// Token: 0x04011B84 RID: 72580
		private int _pauseCount;

		// Token: 0x04011B85 RID: 72581
		[Header("Hitbox Interactions")]
		[SerializeField]
		private BoxCollider2D _hitBox;

		// Token: 0x04011B86 RID: 72582
		[SerializeField]
		private BoxCollider2D _interactCollider;

		// Token: 0x04011B87 RID: 72583
		[SerializeField]
		private PlayerInteractions _interactions;

		// Token: 0x04011B88 RID: 72584
		public UnityAction<Vector2> OnPlayerMove;

		// Token: 0x04011B89 RID: 72585
		public UnityAction OnUnpausePlayer;

		// Token: 0x04011B8A RID: 72586
		public UnityAction OnDestinationReached;

		// Token: 0x04011B8B RID: 72587
		public UnityAction OnJump;

		// Token: 0x04011B8C RID: 72588
		public static UnityAction OnEnterCombat;

		// Token: 0x04011B8D RID: 72589
		public static UnityAction OnExitCombat;

		// Token: 0x04011B8E RID: 72590
		public static UnityAction OnObtainedWeapon;

		// Token: 0x04011B8F RID: 72591
		public UnityAction OnLand;

		// Token: 0x04011B90 RID: 72592
		[HideInInspector]
		public Rigidbody2D rigidbody;

		// Token: 0x04011B91 RID: 72593
		private EntityMovement _movement;

		// Token: 0x04011B92 RID: 72594
		private BoolTimer _hurtTimer;

		// Token: 0x04011B93 RID: 72595
		public Vector3 targetVelocity;

		// Token: 0x04011B94 RID: 72596
		private Camera _playerCamera;

		// Token: 0x04011B95 RID: 72597
		private float _pathMoveSpeed;

		// Token: 0x04011B96 RID: 72598
		private SkillStats skillStats;

		// Token: 0x04011B97 RID: 72599
		private PlayerParticles playerParticles;

		// Token: 0x04011B98 RID: 72600
		[HideInInspector]
		public Vector2 input;

		// Token: 0x04011B99 RID: 72601
		[HideInInspector]
		public Vector2 lastWalkingDirection;

		// Token: 0x04011B9A RID: 72602
		public Vector2 inputAdd;

		// Token: 0x04011B9B RID: 72603
		private bool _paused;

		// Token: 0x04011B9C RID: 72604
		private float _currentDepth;

		// Token: 0x04011B9D RID: 72605
		[HideInInspector]
		public byte emote;

		// Token: 0x04011B9E RID: 72606
		public static UnityAction<Vector2, byte[]> useEvent;

		// Token: 0x04011B9F RID: 72607
		public static UnityAction<Vector2, byte[]> spell1Event;

		// Token: 0x04011BA0 RID: 72608
		public static UnityAction<Vector2, byte[]> spell2Event;

		// Token: 0x04011BA1 RID: 72609
		public static UnityAction<Vector2, byte[]> spell3Event;

		// Token: 0x04011BA2 RID: 72610
		public static UnityAction<Vector2, byte[]> spell4Event;

		// Token: 0x04011BA6 RID: 72614
		[Header("Animators")]
		[SerializeField]
		private PlayerAnimationLayers _playerAnimationLayers;

		// Token: 0x04011BA7 RID: 72615
		[SerializeField]
		private Animator _bodyAnimator;

		// Token: 0x04011BA8 RID: 72616
		[SerializeField]
		private Animator _armsAnimator;

		// Token: 0x04011BA9 RID: 72617
		[SerializeField]
		private Animator _eyesAnimator;

		// Token: 0x04011BAA RID: 72618
		[SerializeField]
		private Animator _mouthAnimator;

		// Token: 0x04011BAB RID: 72619
		public PlayerCostumeHandler costumeHandler;

		// Token: 0x04011BAC RID: 72620
		[Header("Particles")]
		[SerializeField]
		private GameObject _flashJumpParticle;

		// Token: 0x04011BAD RID: 72621
		[SerializeField]
		private GameObject _levelUpParticle;

		// Token: 0x04011BAE RID: 72622
		public GameObject WinterBreath;

		// Token: 0x04011BAF RID: 72623
		public bool PlayerGraphicsHidden;

		// Token: 0x04011BB0 RID: 72624
		[Header("NetworkComponents")]
		[SerializeField]
		private GameObject[] _objectsToDestroyAsClient;

		// Token: 0x04011BB1 RID: 72625
		[SerializeField]
		private Component[] _componentsToDestroyAsClient;

		// Token: 0x04011BB2 RID: 72626
		private IPlayerOwnerInitialized[] _objectsToInitializeAsOwner;

		// Token: 0x04011BB3 RID: 72627
		[Header("Audio Sources")]
		[SerializeField]
		private AudioClip _grassFootsteps;

		// Token: 0x04011BB4 RID: 72628
		[SerializeField]
		private AudioClip _snowFootsteps;

		// Token: 0x04011BB5 RID: 72629
		[SerializeField]
		private AudioClip _stoneFootsteps;

		// Token: 0x04011BB6 RID: 72630
		[SerializeField]
		private AudioClip _woodFootsteps;

		// Token: 0x04011BB7 RID: 72631
		[SerializeField]
		private AudioClip _dirtFootsteps;

		// Token: 0x04011BB8 RID: 72632
		[SerializeField]
		private AudioClip _nagaFootstep;

		// Token: 0x04011BB9 RID: 72633
		[SerializeField]
		private AudioClip _flashJump;

		// Token: 0x04011BBA RID: 72634
		[SerializeField]
		private AudioClip levelUpSound;

		// Token: 0x04011BBB RID: 72635
		[SerializeField]
		private AudioClip wakeUpSound;

		// Token: 0x04011BBC RID: 72636
		public static UnityAction OnPlayerInitializedAsOwner;

		// Token: 0x04011BBD RID: 72637
		public static UnityAction onAirSkip;

		// Token: 0x04011BBE RID: 72638
		public static UnityAction onCompleteSleep;

		// Token: 0x04011BBF RID: 72639
		public static UnityAction onPassOut;

		// Token: 0x04011BC0 RID: 72640
		public static UnityAction onDying;

		// Token: 0x04011BC1 RID: 72641
		public UnityAction OnSleeping;

		// Token: 0x04011BC2 RID: 72642
		private float _airborneSmoothFactor = 10f;

		// Token: 0x04011BC7 RID: 72647
		public bool UniqueJump;

		// Token: 0x04011BDE RID: 72670
		public bool EyesClosed;

		// Token: 0x04011BDF RID: 72671
		public bool Teleporting;

		// Token: 0x04011BEA RID: 72682
		private float _cameraZoomLevel = 7.5f;

		// Token: 0x04011BF3 RID: 72691
		protected Coroutine _fullPathRoutine;

		// Token: 0x04011BF4 RID: 72692
		private Vector2 _pathingLocation;

		// Token: 0x04011BF5 RID: 72693
		public UnityAction OnBeginOvernightScreen;

		// Token: 0x04011BF6 RID: 72694
		public UnityAction OnStartSleep;

		// Token: 0x04011BF7 RID: 72695
		public CoroutineQueue overnightQueue = new CoroutineQueue();

		// Token: 0x04011BF8 RID: 72696
		[HideInInspector]
		public string currentScene;

		// Token: 0x04011BF9 RID: 72697
		[HideInInspector]
		public Vector2 currentPosition;

		// Token: 0x04011BFA RID: 72698
		private bool initialized;

		// Token: 0x04011BFB RID: 72699
		private int calculationCount;

		// Token: 0x04011BFC RID: 72700
		private static readonly StatType[] statTypes = (StatType[])Enum.GetValues(typeof(StatType));

		// Token: 0x04011BFD RID: 72701
		private int statsIncrementalIndex;

		// Token: 0x04011BFE RID: 72702
		private bool spell1NotCasting;

		// Token: 0x04011BFF RID: 72703
		private bool spell2NotCasting;

		// Token: 0x04011C00 RID: 72704
		private bool spell3NotCasting;

		// Token: 0x04011C01 RID: 72705
		private bool spell4NotCasting;

		// Token: 0x04011C02 RID: 72706
		private bool useItemNotUsing;

		// Token: 0x04011C03 RID: 72707
		private Vector2 lastJumpPos;

		// Token: 0x04011C04 RID: 72708
		private float lastJumpHeight;

		// Token: 0x04011C05 RID: 72709
		private float jumpZoneHeight;

		// Token: 0x04011C06 RID: 72710
		private int sleepPauseFrame;

		// Token: 0x04011C07 RID: 72711
		private float jumpHeight = 1f;

		// Token: 0x04011C08 RID: 72712
		private Vector3 _cameraPosition;

		// Token: 0x04011C09 RID: 72713
		private Tween zoomTween;

		// Token: 0x04011C0A RID: 72714
		private Tween eyesHurtTween;

		// Token: 0x04011C0B RID: 72715
		private Tween moveSpeedWhenHurtTween;

		// Token: 0x04011C0C RID: 72716
		private FloatRef moveSpeedWhenHurtFloatRef = new FloatRef
		{
			value = 1.3f
		};

		// Token: 0x04011C0D RID: 72717
		public UnityAction<DamageInfo> onPreReceiveDamage;

		// Token: 0x04011C0E RID: 72718
		public UnityAction<int> onChangeMount;

		// Token: 0x04011C0F RID: 72719
		public UnityAction<ushort> onSetUseItem;

		// Token: 0x04011C10 RID: 72720
		public UnityAction<ushort> onSetSpell1;

		// Token: 0x04011C11 RID: 72721
		public UnityAction<ushort> onSetSpell2;

		// Token: 0x04011C12 RID: 72722
		public UnityAction<ushort> onSetSpell3;

		// Token: 0x04011C13 RID: 72723
		public UnityAction<ushort> onSetSpell4;

		// Token: 0x04011C14 RID: 72724
		private AsyncOperationHandle currentLoadOp;

		// Token: 0x04011C15 RID: 72725
		private bool loadingItem;

		// Token: 0x04011C16 RID: 72726
		private Action onLoadedItem;

		// Token: 0x04011C17 RID: 72727
		private float overcappedHealth;

		// Token: 0x04011C18 RID: 72728
		private float overcappedMana;

		// Token: 0x04011C19 RID: 72729
		private Tween _emoteTween;

		// Token: 0x04011C1A RID: 72730
		private Tween _petTween;

		// Token: 0x04011C1B RID: 72731
		private Tween _pickupTween;

		// Token: 0x04011C1C RID: 72732
		private bool _petting;

		// Token: 0x04011C1D RID: 72733
		private FloatRef petFloatRef = new FloatRef
		{
			value = 0f
		};

		// Token: 0x04011C1E RID: 72734
		private FloatRef pickupFloatRef = new FloatRef
		{
			value = 0f
		};

		// Token: 0x04011C1F RID: 72735
		public float lastPickupTime = -100f;

		// Token: 0x04011C20 RID: 72736
		public float lastPickaxeTime = -100f;

		// Token: 0x04011C21 RID: 72737
		private Tween passoutSleepTween;

		// Token: 0x04011C22 RID: 72738
		public UnityAction<DamageInfo, IDamageReceiver> onHit;

		// Token: 0x04011C23 RID: 72739
		public UnityAction<DamageInfo, IDamageReceiver> onPreHit;

		// Token: 0x04011C24 RID: 72740
		private bool sentInCombatEvent;

		// Token: 0x04011C25 RID: 72741
		public UnityAction<PetItem> onSetPet;

		// Token: 0x04011C26 RID: 72742
		private static HourlyBuff petBuff = new HourlyBuff
		{
			entity = Player.Instance,
			duration = 6000000f,
			buffType = BuffType.PetParade,
			buffName = "Promenade",
			buffDescription = "Gives exp over time"
		};

		// Token: 0x04011C27 RID: 72743
		private Dictionary<BuffType, Buff> _buffs = new Dictionary<BuffType, Buff>();

		// Token: 0x04011C28 RID: 72744
		private float _footstepsTimer;

		// Token: 0x04011C29 RID: 72745
		private float _footstepsVol;
	}
}
