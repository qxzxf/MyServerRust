using CompanionServer;
using Facepunch.Extend;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Plugins.Extension;
using Oxide.Plugins.Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using VLB;
using Time = UnityEngine.Time;

namespace Oxide.Plugins
{
	[Info("SSkin", "qxzxf", "1.1.3")]
	internal class SSkin : RustPlugin
	{
		private readonly Dictionary<ulong, BaseEntity> _startUseMenu = new Dictionary<ulong, BaseEntity>();

		#region Configuration
		private class Configuration : Sigleton<Configuration>
		{
			public bool AddApproved;
			public bool AddHazmat;

			public string DefaultSkinsPermission;

			public Grades GradesSettings;
			public Hammer HammerSettings;
			public Command SkinCommand;
			public Command SkinEntityCommand;

			public Dictionary<string, List<ulong>> PermissionSkins;

			public class Grades
			{
				public List<BuildingGrade> Array;
				public string Permission;
			}

			public class BuildingGrade
			{
				public string Name;
				public List<SkinUrl> Skins;
			}

			public class SkinUrl : Skin.Skin
			{
				public string Url;
			}

			public class Hammer
			{
				public string OpenSkinEntityPermission;
				public BUTTON OpenSkinEntityButton;
                public BUTTON OpenSkinBuildingButton;
                public string ChangeSkinPressLeftButtonPermission;
			}

			public class Command
			{
				public List<string> Commands;
				public string Permission;
			}

			public List<ulong> GetBlockPermissionSkins(BasePlayer player)
			{
				List<ulong> items = new List<ulong>();

				foreach (var perm in PermissionSkins)
				{
					if (player.HasPermission(perm.Key))
						continue;

					items.AddRange(perm.Value);
				}

				return items;
			}

			public List<ulong> GetPermissionSkins(BasePlayer player)
			{
				List<ulong> items = new List<ulong>();

				foreach (var perm in PermissionSkins)
				{
					if (player.HasPermission(perm.Key))
						items.AddRange(perm.Value);
				}

				return items;
			}

			public void SetDefaultConfig()
			{
				AddApproved = true;
				AddHazmat = true;

				DefaultSkinsPermission = "defaultskins.use";

				GradesSettings = new Grades()
				{
					Array = new List<BuildingGrade>()
					{
						new BuildingGrade()
						{
							Name = "Stone",
							Skins = new List<SkinUrl>()
							{
								new SkinUrl()
								{
									Name = "Default Stone",
									Id = 0,
									Url = "https://rustlabs.com/img/items180/stones.png"
								},
								new SkinUrl()
								{
									Name = "Upgrade Stone",
									Id = 10220,
									Url = "https://media.discordapp.net/attachments/1103353725772386318/1111205056520388608/IMG_9516.png",
								},
								new SkinUrl()
								{
									Name = "Upgrade Stone",
									Id = 10223,
									Url = "https://i.ibb.co/0JPt5Xq/777.png",
								}
							}
						},
						new BuildingGrade()
						{
							Name = "Metal",
							Skins = new List<SkinUrl>()
							{
								new SkinUrl()
								{
									Name = "Default Metal",
									Id = 0,
									Url = "https://rustlabs.com/img/items180/metal.refined.png"
								},
								new SkinUrl()
								{
									Name = "Upgrade Metal",
									Id = 10221,
									Url = "https://media.discordapp.net/attachments/1090555264106770513/1114467883943215164/512fx512f.png",
								}
							}
						}
					},
					Permission = "gradesetting.use"
				};

				PermissionSkins = new Dictionary<string, List<ulong>>()
				{
					{
						"testperm",
						new List<ulong>()
						{
							1
						}
					}
				};

				HammerSettings = new Hammer()
				{
					ChangeSkinPressLeftButtonPermission = "changeskinpressleftbutton.use",
					OpenSkinEntityPermission = "openskinentity.use",
					OpenSkinEntityButton = BUTTON.FIRE_SECONDARY,
					OpenSkinBuildingButton = BUTTON.USE,
				};

				SkinCommand = new Command()
				{
					Commands = new List<string>() { "skin" },
					Permission = "skincommand.use"
				};

				SkinEntityCommand = new Command()
				{
					Commands = new List<string>() { "skinentity", "se" },
					Permission = "skinentitycommand.use"
				};
			}
		}

		protected override void LoadDefaultConfig()
		{
			if (Configuration.Instance == null)
				new Configuration();

			Configuration.Instance.SetDefaultConfig();
		}
		protected override void SaveConfig()
		{
			Config.WriteObject(Configuration.Instance);
		}
		protected override void LoadConfig()
		{
			base.LoadConfig();

			try
			{
				Configuration.Instance = Config.ReadObject<Configuration>();
			}
			catch
			{
				LoadDefaultConfig();
			}

			NextTick(SaveConfig);
		}
		#endregion

		#region Lang
		protected override void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>()
			{
				["NOPERMUSE"] = "У вас нет прав на использование команды!",
				["LABLEMENUTEXT"] = "МЕНЮ СКИНОВ",
				["INFOCLICKSKIN"] = "Нажмите на предмет,\nна который нужно установить скин",
				["FAVSKINSLABLE"] = "ИЗБРАННЫЕ СКИНЫ",
				["NOTYOUREBUILD"] = "Этот предмет не ваш"
			}, this, "ru");

			lang.RegisterMessages(new Dictionary<string, string>()
			{
				["NOPERMUSE"] = "You don't have the rights to use the command!",
				["LABLEMENUTEXT"] = "Menu Skin",
				["INFOCLICKSKIN"] = "Click on the item you want to install the skin on",
				["FAVSKINSLABLE"] = "SKINS FAVORITAS",
				["NOTYOUREBUILD"] = "This item is not yours"
			}, this, "en");
		}

		#endregion

		#region Hooks
		private void Loaded()
		{
			new Items().Load();
			new Players().Load();
		}

		private void OnServerInitialized()
		{
			new SSkinWeb(this);
			new ImageLibrary(plugins.Find("ImageLibrary"));
			new SSkinLang(this);
			new InterfaceBuilder();
			new Helper();

			foreach (var command in Configuration.Instance.SkinCommand.Commands)
				cmd.AddChatCommand(command, this, SkinCommand);

			foreach (var command in Configuration.Instance.SkinEntityCommand.Commands)
				cmd.AddChatCommand(command, this, SkinEntityCommand);

			NextTick(() =>
			{
				if (Configuration.Instance.AddHazmat)
				{
					Items.Instance.LoadSkinHazmats();
				}

				if (Configuration.Instance.AddApproved)
				{
					Items.Instance.LoadSkinDefines();
					Items.Instance.LoadApprovedSkin();
					Items.Instance.Unload();
				}
			});

			BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected);

			ImageLibrary.Instance.AddImage($"https://i.ibb.co/XX5ntbr/grid.png", "grid2");

			RegisterPermission(Configuration.Instance.DefaultSkinsPermission);
			RegisterPermission(Configuration.Instance.HammerSettings.OpenSkinEntityPermission);
			RegisterPermission(Configuration.Instance.HammerSettings.ChangeSkinPressLeftButtonPermission);
			RegisterPermission(Configuration.Instance.SkinCommand.Permission);
			RegisterPermission(Configuration.Instance.SkinEntityCommand.Permission);
			RegisterPermission(Configuration.Instance.GradesSettings.Permission);

			foreach (var perm in Configuration.Instance.PermissionSkins)
			{
				RegisterPermission(perm.Key);

				foreach (var skin in perm.Value)
				{
					if (skin > 0)
						SSkinWeb.Instance.DownloadSkin(skin);
				}
			}

			if (Configuration.Instance.GradesSettings.Array.Count > 0)
			{
				foreach (var grade in Configuration.Instance.GradesSettings.Array)
				{
					foreach (var skin in grade.Skins)
					{
						ImageLibrary.Instance.AddImage(skin.Url, skin.Name);
					}

					if (Items.Instance.Get(grade.Name) != null) continue;

					Items.Instance.Add(grade.Name).Skins = grade.Skins.Select(s => new Skin.Skin() { Name = s.Name, Id = s.Id }).ToList();
				}
			}
		}

		private void OnPlayerConnected(BasePlayer player)
		{
			player.gameObject.GetOrAddComponent<OpenMenuButton>();

			if (Players.Instance.Get(player.userID) == null)
				Players.Instance.Value.Add(new Player.Player() { SteamId = player.userID });
		}

		private void OnPlayerDisconnected(BasePlayer player, string reason) 
		{
			var openMenuButton = player.gameObject.GetComponent<OpenMenuButton>();
			if (openMenuButton == null) return;
			openMenuButton.Destroy();
		}

		private void Unload()
		{
			Items.Instance.Unload();
			Players.Instance.Unload();

			if (_startUseMenu.Count > 0)
				_startUseMenu.Keys.ToList().ForEach(s =>
				{
					var basePlayer = Player.FindById(s);
					InterfaceBuilder.Instance.DestroyAllUI(basePlayer);
					basePlayer.EndLooting();
					OnPlayerDisconnected(basePlayer, "");
				});
		}

		private void CanWearItem(PlayerInventory inventory, Item item, int targetSlot) => CanEquipItem(inventory, item, targetSlot);

		private void CanEquipItem(PlayerInventory inventory, Item item, int targetPos)
		{
			if (inventory.baseEntity == null || item == null || !inventory.baseEntity.userID.IsSteamId()) return;

			var player = inventory.baseEntity.ToPlayer();

			if (player == null) return;

			var blockedSkins = Configuration.Instance.GetBlockPermissionSkins(player);

			var playerData = Players.Instance.Get(inventory.baseEntity.userID);

            if (!player.HasPermission(Configuration.Instance.DefaultSkinsPermission) || !playerData.DefaultSkins.ContainsKey(item.info.shortname))
			{
				if (blockedSkins.Contains(item.skin))
				{
					item.SetItemSkin(0);
					player.SendNetworkUpdate();
				}
				return;
			}

            playerData.SetDefaultSkin(item);
			player.SendNetworkUpdate();
		}

		private void OnStructureRepair(BaseCombatEntity entity, BasePlayer player)
		{
			if (entity == null || player == null) return;

			if (entity.OwnerID != player.userID && !(player.Team != null && player.Team.members.Contains(entity.OwnerID))) return;

			if (!player.HasPermission(Configuration.Instance.DefaultSkinsPermission)) return;

			if (player.HasPermission(Configuration.Instance.GradesSettings.Permission) && entity is BuildingBlock)
			{
				var block = entity as BuildingBlock;

				if ((!player.CanBuild())) return;

				if (Items.Instance.Get(block.grade.ToString()) == null) return;

				Players.Instance.Get(player.userID).SetDefaultSkin(block);
				player.SendNetworkUpdateImmediate(false);
				player.ClientRPC(null, "RefreshSkin");
				return;
			}

			if (player.HasPermission(Configuration.Instance.HammerSettings.ChangeSkinPressLeftButtonPermission))
				Players.Instance.Get(player.userID).SetDefaultSkin(entity);
		}

		private void OnStructureUpgrade(BuildingBlock block, BasePlayer player, BuildingGrade.Enum grade)
		{
			if (player == null || block == null) return;

			if (!player.HasPermission(Configuration.Instance.DefaultSkinsPermission, Configuration.Instance.GradesSettings.Permission))
				return;

			if (Items.Instance.Get(grade.ToString()) == null) return;

			timer.Once(1, () =>
			{
				NextTick(() =>
				{
					Players.Instance.Get(player.userID).SetDefaultSkin(block, grade);
					player.SendNetworkUpdateImmediate(false);
					player.ClientRPC(null, "RefreshSkin");
				});
			});
		}

		private void OnItemAddedToContainer(ItemContainer container, Item item)
		{
			if (container == null || item == null || item.skin != 0 ||
				Items.Instance.Get(item.info.shortname) == null) return;

			var basePlayer = item.GetOwnerPlayer();

			if (basePlayer == null || basePlayer.IPlayer == null || basePlayer.IsNpc || !basePlayer.userID.IsSteamId()) return;

			var blockedSkins = Configuration.Instance.GetBlockPermissionSkins(basePlayer);

            var playerData = Players.Instance.Get(basePlayer.userID);

            if (!basePlayer.HasPermission(Configuration.Instance.DefaultSkinsPermission) || !playerData.DefaultSkins.ContainsKey(item.info.shortname))
            {
                if (blockedSkins.Contains(item.skin))
				{
					item.SetItemSkin(0);
					basePlayer.SendNetworkUpdate();

				}
				return;
			}

			if (container != basePlayer.inventory.containerMain
				&& container != basePlayer.inventory.containerBelt
				&& container != basePlayer.inventory.containerWear) return;

            playerData.SetDefaultSkin(item);
			basePlayer.SendNetworkUpdate();
		}

		private void OnItemCraftFinished(ItemCraftTask task, Item item, ItemCrafter owner)
		{
			if (task.skinID != 0) return;
			Players.Instance.Get(owner.owner.userID).SetDefaultSkin(item);
		}

		private object OnItemAction(Item item, string action, BasePlayer player)
		{
			if (_startUseMenu.ContainsKey(player.userID)) return false;
			return null;
		}

		private object CanLootPlayer(BasePlayer looted, BasePlayer looter)
		{
			if (looter == null) return null;

			if (_startUseMenu.ContainsKey(looter.userID))
				return true;

			return null;
		}

		private void OnPlayerLootEnd(PlayerLoot inventory)
		{
			if (inventory == null) return;
			var player = inventory.GetComponent<BasePlayer>();
			if (player == null || !_startUseMenu.ContainsKey(player.userID)) return;
			player.SendConsoleCommand("uiskinmenu close");
		}
		#endregion

		#region Commands

		#region ChatCommands

		private void SkinCommand(BasePlayer player, string command, string[] args)
		{
			if (!player.HasPermission(Configuration.Instance.SkinCommand.Permission))
			{
				SendReply(player, SSkinLang.Instance.Get("NOPERMUSE", player.UserIDString));
				return;
			}

			if (_startUseMenu.ContainsKey(player.userID)) return;

			timer.Once(0.5f, () =>
			{
				ItemContainer container = new ItemContainer();
				container.entityOwner = player;
				container.isServer = true;
				container.allowedContents = ItemContainer.ContentsType.Generic;
				container.GiveUID();
				_startUseMenu.Add(player.userID, null);
				container.capacity = 12;
				container.playerOwner = player;
				player.LootContainer(container);
				InterfaceBuilder.Instance.Draw(player);
			});
		}

		private void SkinEntityCommand(BasePlayer player, string command, string[] args)
		{
			if (!player.HasPermission(Configuration.Instance.SkinEntityCommand.Permission))
			{
				SendReply(player, SSkinLang.Instance.Get("NOPERMUSE", player.UserIDString));
				return;
			}

			if (_startUseMenu.ContainsKey(player.userID)) return;

			var entity = player.FindConstructioninDirectionLook();

			if (entity == null) return;

			if (entity.OwnerID != player.userID && !(player.Team != null && player.Team.members.Contains(entity.OwnerID)))
			{
				SendReply(player, SSkinLang.Instance.Get("NOTYOUREBUILD", player.UserIDString));
				return;
			}

			if(entity is BuildingBlock)
            {
				if (args.Length > 0 && args[0] != "buildings")
					return;
            }
			else
			{
				if (args.Length > 0 && args[0] != "entity")
					return;
			}

			string shortName = entity.GetShortName();

			if (string.IsNullOrEmpty(shortName)) return;

			_startUseMenu.Add(player.userID, entity);

			InterfaceBuilder.Instance.Draw(player, false);

			player.SendConsoleCommand($"uiskinmenu", new object[] { "weaponSelect", shortName, 0 });
		}
[ConsoleCommand("uiskinmenu")]
		private void ConsoleCommand(ConsoleSystem.Arg arg)
		{
			var player = arg.Player();
			if (player == null) return;
			string shortName;
			ulong skinId;
			int page;
			ulong itemId;

			var playerData = Players.Instance.Get(player.userID);

			var sound = new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0, new Vector3(), new Vector3());

			switch (arg.Args[0])
			{
				case "page":
					{
						shortName = arg.Args[1];
						itemId = ulong.Parse(arg.Args[2]);
						page = arg.Args[3].ToInt();
						InterfaceBuilder.Instance.DrawSkins(player, shortName, itemId, page);
						InterfaceBuilder.Instance.DrawSelectSkins(player, shortName, itemId, page);
					}
					break;
				case "weaponSelect":
					{
						shortName = arg.Args[1];
						itemId = ulong.Parse(arg.Args[2]);

						InterfaceBuilder.Instance.DrawSkins(player, shortName, itemId, 1);
						InterfaceBuilder.Instance.DrawSelectSkins(player, shortName, itemId, 1);
					}
					break;
				case "setSelected":
					{
						shortName = arg.Args[1];
						itemId = ulong.Parse(arg.Args[2]);
						skinId = ulong.Parse(arg.Args[3]);
						page = arg.Args[4].ToInt();
						string layer = arg.Args[5];

						playerData.AddOrRemoveSelectedSkin(shortName, skinId);

						InterfaceBuilder.Instance.UpdateItem(player, shortName, itemId, skinId, page);
						//InterfaceBuilder.Instance.DrawSkins(player, shortName, itemId, page);
						InterfaceBuilder.Instance.DrawSelectSkins(player, shortName, itemId, page);
					}
					break;
				case "setdefault":
					{
						shortName = arg.Args[1];
						itemId = ulong.Parse(arg.Args[2]);
						skinId = ulong.Parse(arg.Args[3]);
						page = arg.Args[4].ToInt();
						string layer = arg.Args[5];

						if (playerData.DefaultSkins.ContainsKey(shortName))
						{
							var defaultSkinId = playerData.DefaultSkins[shortName];
							playerData.DefaultSkins[shortName] = skinId;

							if(defaultSkinId != skinId)
								InterfaceBuilder.Instance.UpdateItem(player, shortName, itemId, defaultSkinId, page);
						}
						else
							playerData.DefaultSkins.Add(shortName, skinId);

						InterfaceBuilder.Instance.UpdateItem(player, shortName, itemId, skinId, page);

						//InterfaceBuilder.Instance.DrawSkins(player, shortName, itemId, page);
						InterfaceBuilder.Instance.DrawSelectSkins(player, shortName, itemId, page);
					}
					break;
				case "setskin":
					{
						itemId = ulong.Parse(arg.Args[1]);
						skinId = ulong.Parse(arg.Args[2]);

						var entity = _startUseMenu[playerData.SteamId];

						if (entity == null)
						{
							var item = player.inventory.FindItemByUID(new ItemId(itemId));
							item.SetItemSkin(skinId);

							player.SendNetworkUpdate();
							return;
						}

						entity.SetSkin(skinId);
					}
					break;
				case "close":
					{
						InterfaceBuilder.Instance.DestroyAllUI(player);
						_startUseMenu.Remove(player.userID);
						player.EndLooting();
					}
					break;
				case "openEntityUi":
					{
						if (arg.Args.Length < 1)
							return;

						SkinEntityCommand(player, "console", new string[] { arg.Args[1] });
					}
					break;
				case "openDefaultUi":
					{
						SkinCommand(player, "console", new string[0]);
					}
					break;
			}

			EffectNetwork.Send(sound, player.Connection);
		}
		#endregion

		#endregion

		#region UI

		private interface IContainer
		{
			string OffsetMin { get; }
			string OffsetMax { get; }

			PlayerInventory.Type Type { get; }
			Tuple<string, string> GetOffsets(int index);
		}

		private class MainContainer : IContainer
		{
			public string OffsetMin => "-200 85";

			public string OffsetMax => "180 337";

			public PlayerInventory.Type Type => PlayerInventory.Type.Main;

			public Tuple<string, string> GetOffsets(int i)
			{
				var x = i * 64 - Math.Floor((double)i / 6) * 6 * 64;
				var y = Math.Floor((double)i / 6) * 63;

				return new Tuple<string, string>($"{x} {-60 - y}", $"{60 + x} {-y}");
			}
		}

		private class WearContainer : IContainer
		{
			public string OffsetMin => "-588 51";
			public string OffsetMax => "-215 115";

			public PlayerInventory.Type Type => PlayerInventory.Type.Wear;

			public Tuple<string, string> GetOffsets(int i)
			{
				var x = i * 54 - Math.Floor((double)i / 7) * 7 * 54;

				return new Tuple<string, string>($"{x} 0", $"{50 + x} 50");
			}
		}

		private class BeltContainer : IContainer
		{
			public string OffsetMin => "-200 -43";
			public string OffsetMax => "180 19";

			public PlayerInventory.Type Type => PlayerInventory.Type.Belt;

			public Tuple<string, string> GetOffsets(int i)
			{
				var x = i * 64 - Math.Floor((double)i / 6) * 6 * 64;

				return new Tuple<string, string>($"{x} 0", $"{60 + x} 60");
			}
		}

		private class DefaultInterface
		{
			private string _json = "[{\"name\":\"UiSkuliSkins\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0 0 0 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 1\",\"anchormax\":\"0.5 1\",\"offsetmin\":\"-225 -325\",\"offsetmax\":\"615 -1\"},{\"type\":\"NeedsCursor\"}]},{\"name\":\"UiSkuliSkins-Lable\",\"parent\":\"UiSkuliSkins\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.33 0.32 0.32 1.00\",\"imagetype\":\"Tiled\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 1\",\"anchormax\":\"0 1\",\"offsetmin\":\"7 -47\",\"offsetmax\":\"427 5\"}]},{\"parent\":\"UiSkuliSkins-Lable\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"%LABLEMENUTEXT%\",\"fontSize\":24,\"align\":\"MiddleCenter\",\"color\":\"0.75 0.71 0.67 1.00\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.5\",\"anchormax\":\"0 0.5\",\"offsetmin\":\"4 -30\",\"offsetmax\":\"427 25\"}]},{\"name\":\"61533899272e46f486d03ceff11413f2\",\"parent\":\"UiSkuliSkins-Lable\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"uiskinmenu close\",\"sprite\":\"assets/icons/close.png\",\"color\":\"0.75 0.75 0.75 0.65\"},{\"type\":\"RectTransform\",\"anchormin\":\"1 0.5\",\"anchormax\":\"1 0.5\",\"offsetmin\":\"5 -15\",\"offsetmax\":\"35 15\"}]},{\"name\":\"UiSkuliSkins-Search\",\"parent\":\"UiSkuliSkins\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"%INFOCLICKSKIN%\",\"fontSize\":12,\"align\":\"MiddleCenter\",\"color\":\"1 1 1 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.5\",\"anchormax\":\"0.5 0.5\",\"offsetmin\":\"-400 -185\",\"offsetmax\":\"-35 115\"}]},{\"name\":\"UiSkuliSkins-LableIzb\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.33 0.32 0.32 1.00\",\"imagetype\":\"Tiled\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0\",\"anchormax\":\"0.5 0\",\"offsetmin\":\"190 235\",\"offsetmax\":\"575 290\"}]},{\"parent\":\"UiSkuliSkins-LableIzb\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"%FAVSKINSLABLE%\",\"fontSize\":24,\"align\":\"MiddleCenter\",\"color\":\"0.75 0.71 0.67 1.00\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.5\",\"anchormax\":\"0 0.5\",\"offsetmin\":\"20 -25\",\"offsetmax\":\"350 25\"}]},{\"name\":\"UiSkuliSkins-Selected\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0 0 0 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0\",\"anchormax\":\"0.5 0\",\"offsetmin\":\"201 111\",\"offsetmax\":\"571 231\"}]}]";

			public DefaultInterface()
			{
				//SkuliElement skuliElement = new SkuliElement("Overlay", false);

				//var layer = skuliElement.AddChildren(new CuiPanel()
				//{
				//	RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-225 -325", OffsetMax = "615 -1" },
				//	CursorEnabled = true,
				//	Image = { Color = "0 0 0 0" }
				//}, InterfaceBuilder.Layer);

				//var lable = layer.AddChildren(new CuiPanel()
				//{
				//	Image = { Color = "0.33 0.32 0.32 1.00", ImageType = Image.Type.Tiled },
				//	RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "7 -47", OffsetMax = "427 5" }
				//}, InterfaceBuilder.Layer + "-Lable");

				//lable.AddChildren(new CuiElement()
				//{
				//	Components =
				//	{
				//		new CuiTextComponent() { Text =  "%LABLEMENUTEXT%", Align = TextAnchor.MiddleCenter, FontSize = 24, Color = "0.75 0.71 0.67 1.00" },
				//		new CuiRectTransformComponent() { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "4 -30", OffsetMax = "427 25" }
				//	}
				//});

				//layer.AddChildren(new CuiElement()
				//{
				//	Name = InterfaceBuilder.Layer + "-Search",
				//	Components =
				//	{
				//		new CuiTextComponent() { Text = "%INFOCLICKSKIN%", Color = "1 1 1 1", FontSize = 12, Align = TextAnchor.MiddleCenter },
				//		new CuiRectTransformComponent() { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400 -185", OffsetMax = "-35 115" }
				//	}
				//});

				//lable.AddChildren(new CuiButton()
				//{
				//	RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "5 -15", OffsetMax = "35 15" },
				//	Button = { Color = "0.75 0.75 0.75 0.65", Command = "uiskinmenu close", Sprite = "assets/icons/close.png" },
				//	Text = { Text = "", Align = TextAnchor.MiddleCenter }
				//});

				//var lableIzb = skuliElement.AddChildren(new CuiPanel()
				//{
				//	Image = { Color = "0.33 0.32 0.32 1.00", ImageType = Image.Type.Tiled },
				//	RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "190 235", OffsetMax = "575 290" },
				//}, InterfaceBuilder.Layer + "-LableIzb");

				//lableIzb.AddChildren(new CuiElement()
				//{
				//	Components =
				//	{
				//		new CuiTextComponent() { Text = "%FAVSKINSLABLE%", Align = TextAnchor.MiddleCenter, FontSize = 24, Color = "0.75 0.71 0.67 1.00" },
				//		new CuiRectTransformComponent() { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "20 -25", OffsetMax = "350 25" }
				//	}
				//});

				//skuliElement.AddChildren(new CuiPanel()
				//{
				//	Image = { Color = "0 0 0 0" },
				//	RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "201 111", OffsetMax = "571 231" }
				//}, InterfaceBuilder.Layer + "-Selected");

				//_json = skuliElement.ToJson();
			}

			public string GetElement(string lableMenuText, string infoClickSkin, string favSkinable)
			{
				return _json.Replace("%LABLEMENUTEXT%", lableMenuText).Replace("%INFOCLICKSKIN%", infoClickSkin).Replace("%FAVSKINSLABLE%", favSkinable);
			}
		}

		private class ContainerInterface
		{
			private string _json = "[{\"name\":\"%Layer%\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0 0 0 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0\",\"anchormax\":\"0.5 0\",\"offsetmin\":\"%OffsetMin%\",\"offsetmax\":\"%OffsetMax%\"}]}]";
			//	private SkuliElement _skuliElement = new SkuliElement("Overlay", false);

			public ContainerInterface()
			{
				//_skuliElement.AddChildren(new CuiPanel()
				//{
				//	Image = { Color = "0 0 0 0" },
				//	RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "%OffsetMin%", OffsetMax = "%OffsetMax%" }
				//},
				//"%Layer%");

				//Debug.Log(_skuliElement.ToJson());
			}

			public string GetElement(string offsetMin, string offsetMax, string layer)
			{
				return _json.Replace("%OffsetMin%", offsetMin).Replace("%OffsetMax%", offsetMax).Replace("%Layer%", layer);
			}
		}

		private class ContainerItemInterface //Динамический предмет 
		{
			//private SkuliElement _skuliElement = new SkuliElement("%Layer%", false);
			private string _json = "[{\"name\":\"6f1e1c3df3804b128afb75440617ec81\",\"parent\":\"%Layer%\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"uiskinmenu weaponSelect %ShortName% %SkinId%\",\"color\":\"0.65 0.65 0.65 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 1\",\"anchormax\":\"0 1\",\"offsetmin\":\"%OffsetMin%\",\"offsetmax\":\"%OffsetMax%\"}]}]";
			public ContainerItemInterface()
			{
				//_skuliElement.AddChildren(new CuiButton()
				//{
				//	Button = { Color = "0.65 0.65 0.65 0", Command = $"uiskinmenu weaponSelect %ShortName% %SkinId%" },
				//	RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "%OffsetMin%", OffsetMax = "%OffsetMax%" }
				//});

				//Debug.Log(_skuliElement.ToJson());
			}

			public string GetElement(string offsetMin, string offsetMax, string layer, string shortName, ulong skinId)
			{
				return _json
					.Replace("%OffsetMin%", offsetMin)
					.Replace("%OffsetMax%", offsetMax)
					.Replace("%Layer%", layer)
					.Replace("%ShortName%", shortName)
					.Replace("%SkinId%", skinId.ToString());
			}
		}

		private class BlockItemInterface
		{
			private string _json = "[{\"parent\":\"%Layer%\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"color\":\"1 1 1 0.05\",\"png\":\"2001040039\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 1\",\"anchormax\":\"0 1\",\"offsetmin\":\"%OffsetMin%\",\"offsetmax\":\"%OffsetMax%\"}]}]";
			//	private SkuliElement _skuliElement = new SkuliElement("%Layer%", false);

			public BlockItemInterface()
			{
				//_skuliElement.AddChildren(new CuiElement()
				//{
				//	Components =
				//	{
				//		new CuiRawImageComponent()
				//		{
				//			Png = ImageLibrary.Instance.GetImage("grid2"),
				//			Color = "1 1 1 0.05"
				//		},
				//		new CuiRectTransformComponent()
				//		{
				//			AnchorMin = "0 1",
				//			AnchorMax = "0 1",
				//			OffsetMin = "%OffsetMin%",
				//			OffsetMax  = "%OffsetMax%",
				//		}
				//	}
				//});

				//Debug.Log(_skuliElement.ToJson());
			}

			public string GetElement(string offsetMin, string offsetMax, string layer)
			{
				return _json.Replace("%OffsetMin%", offsetMin).Replace("%OffsetMax%", offsetMax).Replace("%Layer%", layer);
			}
		}

		private class ItemSkinInterface
		{
			private string _jsonOwnerElement = "[{\"name\":\"%ParrentLayer%%LayerName%\",\"parent\":\"%ParrentLayer%\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.35 0.35 0.35 0.65\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.5\",\"anchormax\":\"0 0.5\",\"offsetmin\":\"%OffsetMin%\",\"offsetmax\":\"%OffsetMax%\"}]},{\"parent\":\"%ParrentLayer%%LayerName%\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"itemid\":1,\"skinid\":1},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.5\",\"anchormax\":\"0.5 0.5\",\"offsetmin\":\"%ImageOffsetMin%\",\"offsetmax\":\"%ImageOffsetMax%\"}]},{\"parent\":\"%ParrentLayer%%LayerName%\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"%Name%\",\"fontSize\":9,\"font\":\"robotocondensed-regular.ttf\",\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0\",\"anchormax\":\"0.5 0\",\"offsetmin\":\"%NameOffsetMin%\",\"offsetmax\":\"%NameOffsetMax%\"}]},{\"name\":\"5397032dd08d4ef4ac90d44e848ac005\",\"parent\":\"%ParrentLayer%%LayerName%\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"uiskinmenu setskin %SelectItemId% %SkinId%\",\"color\":\"0 0 0 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.5\",\"anchormax\":\"0.5 0.5\",\"offsetmin\":\"%ImageOffsetMin%\",\"offsetmax\":\"%ImageOffsetMax%\"}]}]";
			private string _jsonSelectebleElement = "[{\"name\":\"21c13360b417459f8ac5a24ba73f244e\",\"parent\":\"%ParrentLayer%%LayerName%\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"uiskinmenu setSelected %ShortName% %SelectItemId% %SkinId% %Page% %LayerName%\",\"sprite\":\"assets/icons/favourite_servers.png\",\"color\":\"%ColorSelected%\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 1\",\"anchormax\":\"0 1\",\"offsetmin\":\"%SelectableOffsetMin%\",\"offsetmax\":\"%SelectableOffsetMax%\"}]}]";
			private string _jsonDefaultElement = "[{\"name\":\"fe1b35c48ccd4c0db6843c7b821aae33\",\"parent\":\"%ParrentLayer%%LayerName%\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"uiskinmenu setdefault %ShortName% %SelectItemId% %SkinId% %Page% %LayerName%\",\"sprite\":\"assets/icons/power.png\",\"color\":\"%ColorDefault%\"},{\"type\":\"RectTransform\",\"anchormin\":\"1 1\",\"anchormax\":\"1 1\",\"offsetmin\":\"%DefaultOffsetMin%\",\"offsetmax\":\"%DefaultOffsetMax%\"}]}]";

			//private SkuliElement _skuliElement = new SkuliElement("%ParrentLayer%", false);
			//private SkuliElement _skuliElementSelecteble = new SkuliElement("%ParrentLayer%" + "%LayerName%", false);
			//private SkuliElement _skuliElementDefault = new SkuliElement("%ParrentLayer%" + "%LayerName%", false);

			public ItemSkinInterface()
			{
				//var layerNameElement = _skuliElement.AddChildren(new CuiElement()
				//{
				//	Name = "%ParrentLayer%" + "%LayerName%",
				//	Components =
				//	{
				//		new CuiImageComponent()
				//		{
				//			Color = "0.35 0.35 0.35 0.65",
				//		},
				//		new CuiRectTransformComponent
				//		{
				//			AnchorMin = "0 0.5",
				//			AnchorMax = "0 0.5",
				//			OffsetMin = "%OffsetMin%",
				//			OffsetMax = "%OffsetMax%"
				//		}
				//	}
				//});

				//layerNameElement.AddChildren(new CuiElement()
				//{
				//	Components =
				//	{
				//		new CuiImageComponent()
				//		{
				//			ItemId = 1,
				//			SkinId = 1
				//		},
				//		new CuiRectTransformComponent()
				//		{
				//			AnchorMin = "0.5 0.5",
				//			AnchorMax = "0.5 0.5",
				//			OffsetMin = "%ImageOffsetMin%", //-30 -30
				//			OffsetMax = "%ImageOffsetMax%" //30 30
				//		}
				//	}
				//});

				//layerNameElement.AddChildren(new CuiElement()
				//{
				//	Components =
				//	{
				//		new CuiTextComponent()
				//		{
				//			Text = "%Name%", Align = TextAnchor.MiddleCenter,
				//			Font = InterfaceBuilder.Regular,
				//			FontSize = 9
				//		},
				//		new CuiRectTransformComponent()
				//		{
				//			AnchorMin = "0.5 0",
				//			AnchorMax = "0.5 0",
				//			OffsetMin = "%NameOffsetMin%", //-40 -15
				//			OffsetMax = "%NameOffsetMax%" //40 0
				//		}
				//	}
				//});

				//layerNameElement.AddChildren(new CuiButton()
				//{
				//	RectTransform =
				//	{
				//		AnchorMin = "0.5 0.5",
				//		AnchorMax = "0.5 0.5",
				//		OffsetMin = "%ImageOffsetMin%",
				//		OffsetMax = "%ImageOffsetMax%"
				//	},
				//	Button =
				//	{
				//		Command = $"uiskinmenu setskin %SelectItemId% %SkinId%",
				//		Color = "0 0 0 0"
				//	},
				//	Text = { Text = "" }
				//});

				//_jsonOwnerElement = layerNameElement.ToJson();

				//_skuliElementSelecteble.AddChildren(new CuiButton()
				//{
				//	RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "%SelectableOffsetMin%", OffsetMax = "%SelectableOffsetMax%" }, //1 -18 "18 -1" 
				//	Button = { Command = $"uiskinmenu setSelected %ShortName% %SelectItemId% %SkinId% %Page%", Color = "%ColorSelected%", Sprite = "assets/icons/favourite_servers.png" },
				//	Text = { Text = "" }
				//});

				//_jsonSelectebleElement = _skuliElementSelecteble.ToJson();

				//	Debug.Log(_jsonSelectebleElement);

				//_skuliElementDefault.AddChildren(new CuiButton()
				//{
				//	RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "%DefaultOffsetMin%", OffsetMax = "%DefaultOffsetMax%" },
				//	Button = { Command = $"uiskinmenu setdefault %ShortName% %SelectItemId% %SkinId% %Page%", Color = "%ColorDefault%", Sprite = "assets/icons/power.png", },
				//	Text = { Text = "" }
				//});

				//_jsonDefaultElement = _skuliElementDefault.ToJson();

				//Debug.Log(_jsonDefaultElement);
			}

			public string GetElement(string offsetMin, string offsetMax, string shortName, Skin.Skin skin, ulong selectItemId, string ownLayer, string layerName, int page)
			{
				var json = _jsonOwnerElement
					.Replace("%LayerName%", layerName)
					.Replace("%Name%", skin.Name)
					.Replace("%SelectItemId%", selectItemId.ToString())
					.Replace("%SkinId%", skin.Id.ToString())
					.Replace("%Page%", page.ToString())
					.Replace("%ShortName%", shortName)
					.Replace("%OffsetMin%", offsetMin)
					.Replace("%OffsetMax%", offsetMax)
					.Replace("%ParrentLayer%", ownLayer);

				//Configuration.Instance.Grades.FirstOrDefault()
				if (ImageLibrary.Instance.HasImage(skin.Name))
				{
					json = json.Replace("\"type\":\"UnityEngine.UI.Image\",\"itemid\":1,\"skinid\":1", $"\"type\":\"UnityEngine.UI.RawImage\",\"png\":\"{ImageLibrary.Instance.GetImage(skin.Name)}\"");
				}
				else
				{
					json = json.Replace("\"itemid\":1", "\"itemid\":" + ItemExtension.GetItemId(shortName, skin.Id));

					if (skin.Id == 0)
					{
						json = json.Replace(",\"skinid\":1", "");
					}
					else
					{
						json = json.Replace("\"skinid\":1", "\"skinid\":" + skin.Id);
					}

				}

				return json;
			}

			public string GetElementSelectable(string shortName, ulong selectItemId, ulong skinId, int page, string layerName, string ownLayer, bool isSelected)
			{
				return _jsonSelectebleElement
					.Replace("%SelectItemId%", selectItemId.ToString())
					.Replace("%SkinId%", skinId.ToString())
					.Replace("%Page%", page.ToString())
					.Replace("%ShortName%", shortName)
					.Replace("%LayerName%", layerName)
					.Replace("%ColorSelected%", isSelected ? "0.87 0.68 0.22 1.00" : "1 1 1 1")
					.Replace("%ParrentLayer%", ownLayer);
			}

			public string GetElementDefault(string shortName, ulong selectItemId, ulong skinId, int page, string layerName, string ownLayer, bool isDefault)
			{
				return _jsonDefaultElement
					.Replace("%SelectItemId%", selectItemId.ToString())
					.Replace("%SkinId%", skinId.ToString())
					.Replace("%Page%", page.ToString())
					.Replace("%ShortName%", shortName)
					.Replace("%LayerName%", layerName)
					.Replace("%ColorDefault%", isDefault ? "0.21 0.62 0.28 1.00" : "0.98 0.31 0.23 1.00")
					.Replace("%ParrentLayer%", ownLayer);
			}
		}

		private class SearchSkinsInterface
		{

			string _json = "[{\"name\":\"UiSkuliSkins-Search\",\"parent\":\"UiSkuliSkins\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"assets/icons/greyout.mat\",\"color\":\"0.33 0.32 0.32 0.00\",\"imagetype\":\"Filled\",\"png\":\"assets/standard assets/effects/imageeffects/textures/noise.png\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.5\",\"anchormax\":\"0.5 0.5\",\"offsetmin\":\"-400 -185\",\"offsetmax\":\"%OffsetMax%\"}]},{\"name\":\"UiSkuliSkins-Page\",\"parent\":\"UiSkuliSkins-Search\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"assets/icons/greyout.mat\",\"color\":\"0.33 0.32 0.32 0.17\",\"imagetype\":\"Filled\",\"png\":\"assets/standard assets/effects/imageeffects/textures/noise.png\"},{\"type\":\"RectTransform\",\"anchormin\":\"1 0.5\",\"anchormax\":\"1 0.5\",\"offsetmin\":\"15 0\",\"offsetmax\":\"15 0\"}]},{\"name\":\"0d06821bfe8b478cbb868a378443a9a2\",\"parent\":\"UiSkuliSkins-Page\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"%CommandRightButton%\",\"color\":\"0.36 0.50 0.73 1.00\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.5\",\"anchormax\":\"0.5 0.5\",\"offsetmin\":\"-10 5\",\"offsetmax\":\"10 143\"}]},{\"parent\":\"0d06821bfe8b478cbb868a378443a9a2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"»\",\"fontSize\":20,\"align\":\"MiddleCenter\",\"color\":\"%ColorRightButton%\"},{\"type\":\"RectTransform\"}]},{\"name\":\"6b0b7f9ea38f4de283d5676c29e8a1d2\",\"parent\":\"UiSkuliSkins-Page\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"%CommandLeftButton%\",\"color\":\"0.36 0.50 0.73 1.00\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.5\",\"anchormax\":\"0.5 0.5\",\"offsetmin\":\"-10 -137\",\"offsetmax\":\"10 -5\"}]},{\"parent\":\"6b0b7f9ea38f4de283d5676c29e8a1d2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"«\",\"fontSize\":20,\"align\":\"MiddleCenter\",\"color\":\"%ColorLeftButton%\"},{\"type\":\"RectTransform\"}]}]";

			public SearchSkinsInterface()
			{
				//var search = _skuliElement.AddChildren(new CuiPanel()
				//{
				//	Image = { ImageType = Image.Type.Filled, Png = "assets/standard assets/effects/imageeffects/textures/noise.png", Color = "0.33 0.32 0.32 0.00", Material = "assets/icons/greyout.mat" },
				//	RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400 -185", OffsetMax = "%OffsetMax%" }
				//}, InterfaceBuilder.Layer + "-Search");

				//var pageElement = search.AddChildren(new CuiPanel()
				//{
				//	Image = { ImageType = Image.Type.Filled, Png = "assets/standard assets/effects/imageeffects/textures/noise.png", Color = "0.33 0.32 0.32 0.17", Material = "assets/icons/greyout.mat" },
				//	RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "15 0", OffsetMax = "15 0" }
				//}, InterfaceBuilder.Layer + "-Page");

				//pageElement.AddChildren(PageButtonInterface.MoveRightButton("%CommandRightButton%", "%ColorRightButton%"));
				//pageElement.AddChildren(PageButtonInterface.MoveLeftButton("%CommandLeftButton%", "%ColorLeftButton%"));

			}

			public string GetElement(string offsetMax, string commandRightButton, string colorRightButton, string commandLeftButton, string colorLeftButton)
			{
				return _json
					.Replace("%OffsetMax%", offsetMax)
					.Replace("%CommandRightButton%", commandRightButton)
					.Replace("%ColorRightButton%", colorRightButton)
					.Replace("%CommandLeftButton%", commandLeftButton)
					.Replace("%ColorLeftButton%", colorLeftButton);
			}
		}

		private class InterfaceBuilder : Sigleton<InterfaceBuilder>
		{
			public const string Layer = "UiSkuliSkins";
			public const string Regular = "robotocondensed-regular.ttf";

			private readonly List<IContainer> _wears = new List<IContainer>()
			{
				new MainContainer(),
				new BeltContainer(),
				new WearContainer(),
			};

			private DefaultInterface _defaultInterface = new DefaultInterface();
			private ContainerInterface _containerInterface = new ContainerInterface();
			private ContainerItemInterface _containerItemInterface = new ContainerItemInterface();
			private BlockItemInterface _blockItemInterface = new BlockItemInterface();
			private ItemSkinInterface _itemSkinInterface = new ItemSkinInterface();
			private SearchSkinsInterface _searchSkinsInterface = new SearchSkinsInterface();

			public void Draw(BasePlayer player, bool drawWears = true)
			{
				DestroyAllUI(player);

				CuiHelper.AddUi(player, _defaultInterface.GetElement(SSkinLang.Instance.Get("LABLEMENUTEXT", player.UserIDString),
																	SSkinLang.Instance.Get("INFOCLICKSKIN", player.UserIDString),
																	SSkinLang.Instance.Get("FAVSKINSLABLE", player.UserIDString)
																	));

				if (drawWears)
					foreach (var wear in _wears)
						LoadMainUI(player, wear);
			}

			private void LoadMainUI(BasePlayer player, IContainer iContainer)
			{
				string layer = Layer + "-" + iContainer.Type.ToString();
				CuiHelper.DestroyUi(player, layer);

				string json = _containerInterface.GetElement(iContainer.OffsetMin, iContainer.OffsetMax, layer);

				json = json.Remove(json.Length - 1, 1);

				var itemContainer = player.inventory.GetContainer(iContainer.Type);

				for (int i = 0; i < itemContainer.capacity; i++)
				{
					var offsets = iContainer.GetOffsets(i);

					json += ",";

					var item = itemContainer.GetSlot(i);
					if (item != null)
					{
						var shortname = item.info.shortname.Contains("hazmatsuit") ? "hazmatsuit" : item.info.shortname;
						var itemData = Items.Instance.Get(shortname);
						if (itemData != null)
						{
							var сontainerItemInterface = _containerItemInterface.GetElement(offsets.Item1, offsets.Item2, layer, item.info.shortname, item.uid.Value);
							json += сontainerItemInterface.Remove(сontainerItemInterface.Length - 1, 1).Remove(0, 1);
							continue;
						}
					}

					var blockItemInterface = _blockItemInterface.GetElement(offsets.Item1, offsets.Item2, layer);
					json += blockItemInterface.Remove(blockItemInterface.Length - 1, 1).Remove(0, 1);
				}

				json += "]";

				CuiHelper.AddUi(player, json);
			}

			public void DrawSkins(BasePlayer player, string shortName, ulong selectedItemId, int page)
			{
				if (shortName.Contains("hazmatsuit"))
					shortName = "hazmatsuit";

				var itemData = Items.Instance.Get(shortName);

				if (itemData == null) return;

				var playerData = Players.Instance.Get(player.userID);

				int skinsCount = 24;
				int pageXCount = skinsCount / 3;

				CuiHelper.DestroyUi(player, Layer + "-Search");

				string commandRightButton = "";
				string colorRightButton = "0.65 0.65 0.65 0.65";

				string commandLeftButton = "";
				string colorLeftButton = "0.65 0.65 0.65 0.65";

				if (0 < itemData.Skins.Count - skinsCount * page)
				{
					commandRightButton = $"uiskinmenu page {shortName} {selectedItemId} {page + 1}";
					colorRightButton = "";
				}

				if (page > 1)
				{
					commandLeftButton = $"uiskinmenu page {shortName} {selectedItemId} {page - 1}";
					colorLeftButton = "";
				}

				string json = _searchSkinsInterface.GetElement(
					$"{-400 + 90 * pageXCount} 115",
					commandRightButton,
					colorRightButton,
					commandLeftButton,
					colorLeftButton
				);

				json = json.Remove(json.Length - 1, 1);

				var blockedSkins = Configuration.Instance.GetBlockPermissionSkins(player);
				var whiteSkins = Configuration.Instance.GetPermissionSkins(player);

				bool useSelectable = itemData.Skins.Count > 12;
				bool useDefault = player.HasPermission(Configuration.Instance.DefaultSkinsPermission);

				foreach (var skinItem in itemData.Skins.
					Where(s => !blockedSkins.Contains(s.Id)).
					OrderByDescending(s => s.Id == 0 || whiteSkins.Contains(s.Id)).
					Select((i, t) => new { Skin = i, B = t - (page - 1) * skinsCount }).
					Skip((page - 1) * skinsCount).
					Take(skinsCount))
				{
					string offsetMin = $"{4 + skinItem.B * 90 - Math.Floor((double)skinItem.B / pageXCount) * pageXCount * 90} {60 - Math.Floor((double)skinItem.B / pageXCount) * 98}";
					string offsetMax = $"{90 + skinItem.B * 90 - Math.Floor((double)skinItem.B / pageXCount) * pageXCount * 90} {143 - Math.Floor((double)skinItem.B / pageXCount) * 98}";

					json += GetItem(offsetMin, offsetMax, skinItem.B.ToString(), Layer + "-Search", shortName, skinItem.Skin, selectedItemId, page, useDefault, useSelectable, playerData)
							.Replace("%ImageOffsetMin%", "-30 -30")
							.Replace("%ImageOffsetMax%", "30 30")
							.Replace("%NameOffsetMin%", "-40 -15")
							.Replace("%NameOffsetMax%", "40 0")
							.Replace("%SelectableOffsetMin%", "1 -18")
							.Replace("%SelectableOffsetMax%", "18 -1")
							.Replace("%DefaultOffsetMin%", "-16 -16")
							.Replace("%DefaultOffsetMax%", "-5 -5")
							;
				}
				json += "]";
				CuiHelper.AddUi(player, json);
			}

			private string GetItem(string offsetMin, string offsetMax, string layer, string layerParrent, string shortName, Skin.Skin skin, ulong selectedItemId, int page, bool useDefault, bool useSelectable, Player.Player player)
			{
				string json = "";

				var itemSkinJson = _itemSkinInterface.GetElement(
						offsetMin,
						offsetMax,
						shortName,
						skin,
						selectedItemId,
						layerParrent,
						layer,
						page);

				json += ",";

				json += itemSkinJson.Remove(itemSkinJson.Length - 1, 1).Remove(0, 1);

				if (useSelectable)
				{
					json += ",";
					var itemSkinJsonSelectable = _itemSkinInterface.GetElementSelectable(
								shortName,
								selectedItemId,
								skin.Id,
								page,
								layer,
								layerParrent,
								player.SelectedSkins.ContainsKey(shortName) && player.SelectedSkins[shortName].Contains(skin.Id)
					);

					json += itemSkinJsonSelectable.Remove(itemSkinJsonSelectable.Length - 1, 1).Remove(0, 1);
				}

				if (useDefault)
				{
					json += ",";

					var itemSkinJsonDefault = _itemSkinInterface.GetElementDefault(
								shortName,
								selectedItemId,
								skin.Id,
								page,
								layer,
								layerParrent,
								player.DefaultSkins.ContainsKey(shortName) && player.DefaultSkins[shortName] == skin.Id
					);


					json += itemSkinJsonDefault.Remove(itemSkinJsonDefault.Length - 1, 1).Remove(0, 1);
				}

				return json;
			}

			//Тут юи ещё не закончил 
			public void DrawSelectSkins(BasePlayer player, string shortname, ulong selectedItemId, int page)
			{
				var playerData = Players.Instance.Get(player.userID);

				CuiHelper.DestroyUi(player, Layer + "-Selected");

				if (!playerData.SelectedSkins.ContainsKey(shortname))
				{
					return;
				}

				var item = Items.Instance.Get(shortname);

				var container = new CuiElementContainer
				{
					{
						new CuiPanel()
						{
							Image = { Color = "0 0 0 0" },
							RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "201 111", OffsetMax = "571 231" }
						},
						"Overlay",
						Layer + "-Selected"
					}
				};

				if (item.Skins.Count < 12) return;

				int length = playerData.SelectedSkins[shortname].Count > 12 ? 12 : playerData.SelectedSkins[shortname].Count;

				var json = container.ToJson();

				json = json.Remove(json.Length - 1, 1);

				bool useDefault = player.HasPermission(Configuration.Instance.DefaultSkinsPermission);

				for (int i = 0; i < length && item.Skins.Count - 1 >= i; i++)
				{
					var skin = playerData.SelectedSkins[shortname][i];

					string offsetMin = $"{2 + i * 62 - Math.Floor((double)i / 6) * 6 * 62} {5 - Math.Floor((double)i / 6) * 62}";
					string offsetMax = $"{52 + i * 62 - Math.Floor((double)i / 6) * 6 * 62} {55 - Math.Floor((double)i / 6) * 62}";

					json += GetItem(offsetMin, offsetMax, i.ToString(), Layer + "-Selected", shortname, item.GetSkin(skin), selectedItemId, page, useDefault, true, playerData)
						.Replace("%ImageOffsetMin%", "-25 -25")
						.Replace("%ImageOffsetMax%", "25 25")
						.Replace("%NameOffsetMin%", "-30 -5")
						.Replace("%NameOffsetMax%", "30 10")
						.Replace("%SelectableOffsetMin%", "-5 -10")
						.Replace("%SelectableOffsetMax%", "10 5")
						.Replace("%DefaultOffsetMin%", "-7 -7")
						.Replace("%DefaultOffsetMax%", "3 3")
						.Replace("0.65", "0")
						;
				}

				json += "]";

				CuiHelper.AddUi(player, json);
			}

			public void UpdateItem(BasePlayer player, string shortName, ulong selectedItemId, ulong skinId, int page)
			{
				string json = "[";

				var itemData = Items.Instance.Get(shortName);
				var playerData = Players.Instance.Get(player.userID);

				bool useSelectable = itemData.Skins.Count > 12;
				bool useDefault = player.HasPermission(Configuration.Instance.DefaultSkinsPermission);

				int skinsCount = 24;
				int pageXCount = skinsCount / 3;


				var blockedSkins = Configuration.Instance.GetBlockPermissionSkins(player);
				var whiteSkins = Configuration.Instance.GetPermissionSkins(player);

				var skins = itemData.Skins.
					Where(s => !blockedSkins.Contains(s.Id)).
					OrderByDescending(s => s.Id == 0 || whiteSkins.Contains(s.Id)).
					Select((i, t) => new { Skin = i, B = t - (page - 1) * skinsCount }).
					Skip((page - 1) * skinsCount).
					Take(skinsCount);

				var skin = skins.FirstOrDefault(s => s.Skin.Id.Equals(skinId));

				if (skin == null) return;

				CuiHelper.DestroyUi(player, Layer + "-Search" + skin.B);

				string offsetMin = $"{4 + skin.B * 90 - Math.Floor((double)skin.B / pageXCount) * pageXCount * 90} {60 - Math.Floor((double)skin.B / pageXCount) * 98}";
				string offsetMax = $"{90 + skin.B * 90 - Math.Floor((double)skin.B / pageXCount) * pageXCount * 90} {143 - Math.Floor((double)skin.B / pageXCount) * 98}";

				json += GetItem(offsetMin, offsetMax, skin.B.ToString(), Layer + "-Search", shortName, skin.Skin, selectedItemId, page, useDefault, useSelectable, playerData)
						.Replace("%ImageOffsetMin%", "-30 -30")
						.Replace("%ImageOffsetMax%", "30 30")
						.Replace("%NameOffsetMin%", "-40 -15")
						.Replace("%NameOffsetMax%", "40 0")
						.Replace("%SelectableOffsetMin%", "1 -18")
						.Replace("%SelectableOffsetMax%", "18 -1")
						.Replace("%DefaultOffsetMin%", "-16 -16")
						.Replace("%DefaultOffsetMax%", "-5 -5").Remove(0, 1);


				//if (playerData.SelectedSkins.ContainsKey(shortName) && playerData.SelectedSkins[shortName].Contains(skinId))
				//{
				//	CuiHelper.DestroyUi(player, Layer + "-Selected" + layer);

				//	offsetMin = $"{2 + index * 62 - Math.Floor((double)index / 6) * 6 * 62} {5 - Math.Floor((double)index / 6) * 62}";
				//	offsetMax = $"{52 + index * 62 - Math.Floor((double)index / 6) * 6 * 62} {55 - Math.Floor((double)index / 6) * 62}";
				//	json += GetItem(offsetMin, offsetMax, layer, Layer + "-Selected", shortName, skin, selectedItemId, page, useDefault, true, playerData)
				//			.Replace("%ImageOffsetMin%", "-25 -25")
				//			.Replace("%ImageOffsetMax%", "25 25")
				//			.Replace("%NameOffsetMin%", "-30 -5")
				//			.Replace("%NameOffsetMax%", "30 10")
				//			.Replace("%SelectableOffsetMin%", "-5 -10")
				//			.Replace("%SelectableOffsetMax%", "10 5")
				//			.Replace("%DefaultOffsetMin%", "-7 -7")
				//			.Replace("%DefaultOffsetMax%", "3 3")
				//			.Replace("0.65", "0");

				//}

				json += "]";

				CuiHelper.AddUi(player, json);
			}

			public void DestroyAllUI(BasePlayer player)
			{
				CuiHelper.DestroyUi(player, Layer);
				foreach (var wear in _wears)
					CuiHelper.DestroyUi(player, Layer + "-" + wear.Type.ToString());

				CuiHelper.DestroyUi(player, Layer + "-Selected");
				CuiHelper.DestroyUi(player, Layer + "-Search");
				CuiHelper.DestroyUi(player, Layer + "-LableIzb");
			}
		}


		#endregion

		#region Monobehavior
		private class OpenMenuButton : MonoBehaviour
		{
			private BasePlayer _player;
			private float _lastCheck;

			private void Awake()
			{
				_player = GetComponent<BasePlayer>();
				_lastCheck = Time.realtimeSinceStartup;
			}

			private void FixedUpdate()
			{
				if (_player == null || !_player.IsConnected)
				{
					Destroy();
					return;
				}

				if (!_player.HasPermission(Configuration.Instance.HammerSettings.OpenSkinEntityPermission))
					return;

				if ((_player.GetActiveItem())?.info.shortname != "hammer") return;

				string format = null;

				if (_player.serverInput.WasJustPressed(Configuration.Instance.HammerSettings.OpenSkinEntityButton))
				{
					format = "entity";
                }
                else if (_player.serverInput.WasJustPressed(Configuration.Instance.HammerSettings.OpenSkinBuildingButton))
				{
                    format = "buildings";
                }

				if (string.IsNullOrWhiteSpace(format)) return;

                float currentTime = Time.realtimeSinceStartup;

                if (currentTime - _lastCheck >= 2f)
				{
					_player.SendConsoleCommand($"uiskinmenu openEntityUi {format}");
					_lastCheck = currentTime;
				}
			}

			public void Destroy()
			{
				Destroy(this);
			}
		}
		#endregion

		private void RegisterPermission(string perm)
		{
			if (string.IsNullOrEmpty(perm) || permission.PermissionExists(perm, this))
				return;

			permission.RegisterPermission(perm, this);
		}

		private class Helper : Sigleton<Helper>
		{
			private readonly Dictionary<string, string> _shortNamesEntity = new Dictionary<string, string>();

			public readonly Dictionary<string, string> WorkshopsNames = new Dictionary<string, string>()
			{
				{"ak47", "rifle.ak" },
				{"lr300", "rifle.lr300" },
				{"lr300.item", "rifle.lr300" },
				{"m39", "rifle.m39" },
				{"l96", "rifle.l96" },
				{"longtshirt", "tshirt.long" },
				{"cap", "hat.cap" },
				{"beenie", "hat.beenie" },
				{"boonie", "hat.boonie" },
				{"balaclava", "mask.balaclava" },
				{"pipeshotgun", "shotgun.waterpipe" },
				{"woodstorage", "box.wooden" },
				{"bearrug", "rug.bear" },
				{"boltrifle", "rifle.bolt" },
				{"bandana", "mask.bandana" },
				{"hideshirt", "attire.hide.vest" },
				{"snowjacket", "jacket.snow" },
				{"buckethat", "bucket.helmet" },
				{"semiautopistol", "pistol.semiauto" },
				{"roadsignvest", "roadsign.jacket" },
				{"roadsignpants", "roadsign.kilt" },
				{"burlappants", "burlap.trousers" },
				{"collaredshirt", "shirt.collared" },
				{"mp5", "smg.mp5" },
				{"sword", "salvaged.sword" },
				{"workboots", "shoes.boots" },
				{"vagabondjacket", "jacket" },
				{"hideshoes", "attire.hide.boots" },
				{"deerskullmask", "deer.skull.mask" },
				{"minerhat", "hat.miner" },
				{"burlapgloves", "burlap.gloves" },
				{"burlap.gloves", "burlap.gloves"},
				{"leather.gloves", "burlap.gloves"},
				{"python", "pistol.python" },
				{"woodendoubledoor", "door.double.hinged.wood" }
			};

			public Helper()
			{
				LoadShortNamesEntity();
				UpdateWorkshopShortName();
			}

			private void UpdateWorkshopShortName()
			{
				foreach (var itemDefinition in ItemManager.itemList)
				{
					if (itemDefinition.shortname == "ammo.snowballgun") continue;
					var name = itemDefinition.displayName.english.ToLower().Replace("skin", "").Replace(" ", "").Replace("-", "");
					if (!WorkshopsNames.ContainsKey(name))
						WorkshopsNames.Add(name, itemDefinition.shortname);
					if (!WorkshopsNames.ContainsKey(itemDefinition.shortname))
						WorkshopsNames.Add(itemDefinition.shortname, itemDefinition.shortname);
					if (!WorkshopsNames.ContainsKey(itemDefinition.shortname.Replace(".", "")))
						WorkshopsNames.Add(itemDefinition.shortname.Replace(".", ""), itemDefinition.shortname);
				}
			}
			private void LoadShortNamesEntity()
			{
				foreach (var itemDefinition in ItemManager.GetItemDefinitions())
				{
					var prefab = itemDefinition.GetComponent<ItemModDeployable>()?.entityPrefab?.resourcePath;

					if (string.IsNullOrEmpty(prefab)) continue;

					var shortPrefabName = Utility.GetFileNameWithoutExtension(prefab);

					if (string.IsNullOrEmpty(shortPrefabName) || _shortNamesEntity.ContainsKey(shortPrefabName))
						continue;

					_shortNamesEntity.Add(shortPrefabName, itemDefinition.shortname);
				}
			}

			public string GetShortNamePrefab(string shortPrefabName)
			{
				if (_shortNamesEntity.ContainsKey(shortPrefabName)) return "";

				return _shortNamesEntity[shortPrefabName];
			}

		}

		private class ImageLibrary : PluginSigleton<ImageLibrary>
		{
            public ImageLibrary(Plugin plugin) : base(plugin)
			{
				if (plugin == null)
					throw new Exception("[SSkin] Need add ImageLibrary");
			}

			public string GetImage(string shortname, ulong skin = 0) =>
				(string)Plugin.Call("GetImage", shortname, skin);

			public bool AddImage(string url, string shortname, ulong skin = 0) =>
				(bool)Plugin.Call("AddImage", url, shortname, skin);

			public bool HasImage(string imageName, ulong imageId = 0) => (bool)Plugin.Call("HasImage", imageName, imageId);
		}

		private class SSkinWeb : PluginSigleton<SSkinWeb>
		{
			private readonly WebRequests _webrequest = Interface.Oxide.GetLibrary<WebRequests>();
			
			public SSkinWeb(Plugin plugin) : base(plugin)
			{
			}

			public void DownloadSkin(ulong skinId)
			{
				var shortName = String.Empty;
				var marketName = String.Empty;

				_webrequest.Enqueue($"https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/", $"&itemcount=1&publishedfileids[0]={skinId}",
				(code, res) =>
				{
					if (res.Length < 200)
					{
						Debug.LogError($"[{Plugin.Name}] skind id {skinId} not find");
						return;
					}

					PublishedFileQueryResponse query =
						JsonConvert.DeserializeObject<PublishedFileQueryResponse>(res);

					if (query != null && query.response != null && query.response.publishedfiledetails.Length > 0)
					{
						foreach (var publishedFileQueryDetail in query.response.publishedfiledetails)
						{
							foreach (var tag in publishedFileQueryDetail.tags)
							{
								string adjTag = tag.tag.ToLower().Replace("skin", "").Replace(" ", "").Replace("-", "").Replace(".item", "");
								if (Helper.Instance.WorkshopsNames.ContainsKey(adjTag))
								{
									shortName = Helper.Instance.WorkshopsNames[adjTag];
									marketName = publishedFileQueryDetail.title;

									var itemData = Items.Instance.Get(shortName);

									if (itemData == null)
									{
										Items.Instance.Add(shortName).Skins.Add(new Skin.Skin()
										{
											Id = skinId,
											Name = marketName
										});
										continue;
									}

									if (itemData.GetSkin(skinId) != null) continue;

									itemData.Skins.Add(new Skin.Skin()
									{
										Id = skinId,
										Name = marketName
									});
									return;
								}
							}
						}
					}
				}, Plugin, RequestMethod.POST);
			}

			#region JSON Response Classes
			public class PublishedFileQueryResponse
			{
				public FileResponse response { get; set; }
			}

			public class FileResponse
			{
				public int result { get; set; }
				public int resultcount { get; set; }
				public PublishedFileQueryDetail[] publishedfiledetails { get; set; }
			}

			public class PublishedFileQueryDetail
			{
				public string publishedfileid { get; set; }
				public int result { get; set; }
				public string creator { get; set; }
				public int creator_app_id { get; set; }
				public int consumer_app_id { get; set; }
				public string filename { get; set; }
				public int file_size { get; set; }
				public string preview_url { get; set; }
				public string hcontent_preview { get; set; }
				public string title { get; set; }
				public string description { get; set; }
				public int time_created { get; set; }
				public int time_updated { get; set; }
				public int visibility { get; set; }
				public int banned { get; set; }
				public string ban_reason { get; set; }
				public int subscriptions { get; set; }
				public int favorited { get; set; }
				public int lifetime_subscriptions { get; set; }
				public int lifetime_favorited { get; set; }
				public int views { get; set; }
				public Tag[] tags { get; set; }

				public class Tag
				{
					public string tag { get; set; }
				}
			}

			public class CollectionQueryResponse
			{
				public CollectionResponse response { get; set; }
			}

			public class CollectionResponse
			{
				public int result { get; set; }
				public int resultcount { get; set; }
				public CollectionDetails[] collectiondetails { get; set; }
			}

			public class CollectionDetails
			{
				public string publishedfileid { get; set; }
				public int result { get; set; }
				public CollectionChild[] children { get; set; }
			}

			public class CollectionChild
			{
				public string publishedfileid { get; set; }
				public int sortorder { get; set; }
				public int filetype { get; set; }
			}

			#endregion
		}

		private class SSkinLang : PluginSigleton<SSkinLang>
		{
			protected Lang lang = Interface.Oxide.GetLibrary<Lang>();

			public SSkinLang(Plugin plugin) : base(plugin)
			{
			}

			public string Get(string message, string userId) => lang.GetMessage(message, Plugin, userId);
		}
	}

	namespace Helper
	{
		using Player;
		using Skin;

		#region Items.cs
		public class Items : SigletonData<Items, List<Item>>
		{
			public override string Name => "Items";

			public Item Add(string name)
			{
				Item item = new Item() { Name = name };
				Value.Add(item);
				return item;
			}
			public Item Get(string name) => Value.FirstOrDefault(i => i.Name.Equals(name));

			public Skin GetSkin(string name, ulong id) => Get(name)?.GetSkin(id);

			public void LoadSkinHazmats()
			{
				var item = Get("hazmatsuit") ?? Add("hazmatsuit");

				bool isAdd = false;

				foreach (var hazmat in ItemExtension.GetHazmats())
				{
					if (item.GetSkin(hazmat.Key) == null)
					{
						item.Skins.Add(new Skin()
						{
							Name = ItemManager.FindItemDefinition(hazmat.Value).displayName.english,
							Id = hazmat.Key,
						});
						isAdd = true;
					}
				}

				if (isAdd)
					Unload();
			}

			public void LoadSkinDefines()
			{
				foreach (var itemDefinition in ItemManager.GetItemDefinitions())
				{
					if (itemDefinition == null || itemDefinition.skins2 == null || itemDefinition.skins2.Length == 0) continue;

					var item = Get(itemDefinition.shortname);

					if (item == null)
					{
						item = Add(itemDefinition.shortname);
						item.Skins.Add(new Skin()
						{
							Name = itemDefinition.displayName.english,
						});
					}

					foreach (var playerItemDefinition in itemDefinition.skins2)
					{
						if (playerItemDefinition == null || itemDefinition.shortname == null) continue;

						var skinId = playerItemDefinition.WorkshopDownload == 0 ? ulong.Parse(playerItemDefinition.DefinitionId.ToString()) : playerItemDefinition.WorkshopDownload;

						if (item.GetSkin(skinId) == null)
						{
							item.Skins.Add(new Skin()
							{
								Name = playerItemDefinition.Name,
								Id = skinId,
							});
						}
					}
				}
			}

			public void LoadApprovedSkin()
			{
				foreach (var approvedSkinInfo in Rust.Workshop.Approved.All)
				{
					if (approvedSkinInfo.Value == null || approvedSkinInfo.Value.Skinnable == null || approvedSkinInfo.Value.Marketable == false) continue;
					var item = approvedSkinInfo.Value.Skinnable.ItemName;
					if (item.Contains("lr300")) item = "rifle.lr300";

					Item itemData = Get(item);

					if (itemData == null)
					{
						Add(item).Skins.Add(new Skin()
						{
							Id = approvedSkinInfo.Value.WorkshopdId,
							Name = approvedSkinInfo.Value.Name
						});
						continue;
					}

					if (itemData.GetSkin(approvedSkinInfo.Value.WorkshopdId) != null) continue;

					itemData.Skins.Add(new Skin()
					{
						Id = approvedSkinInfo.Value.WorkshopdId,
						Name = ItemManager.FindItemDefinition(approvedSkinInfo.Value.Name).displayName.english
					});
				}
			}
		}
		#endregion

		#region Players.cs
		public class Players : SigletonData<Players, List<Player>>
		{
			public override string Name => "Players";

			public Player Get(ulong steamId) => Value.FirstOrDefault(p => p.SteamId.Equals(steamId));
		}
		#endregion

		#region Sigleton.cs
		public abstract class PluginSigleton<T> : Sigleton<T> where T : PluginSigleton<T>
		{
			public Plugin Plugin { get; private set; }
			public PluginSigleton(Plugin plugin)
			{
				Plugin = plugin;
			}
		}

		public abstract class Sigleton<T> where T : Sigleton<T>
		{
			public static T Instance;

			public Sigleton()
			{
				Instance = (T)this;
			}
		}

		public abstract class SigletonData<B, T> : Sigleton<B> where B : SigletonData<B, T>
		{
			public T Value { get; protected set; }
			public abstract string Name { get; }

			public virtual void Load()
			{
				Value = Interface.uMod.DataFileSystem.ReadObject<T>("SSkin/" + Name);
				if (Value == null)
				{
					Value = default(T); // JsonConvert.DeserializeObject<T>("");
					Unload();
				}
			}

			public virtual void Unload()
			{
				Interface.uMod.DataFileSystem.WriteObject("SSkin/" + Name, Value);  // JsonConvert.DeserializeObject<T>("");
			}
		}
		#endregion
	}

	namespace Skin
	{
		#region Skin.cs
		public class Skin
		{
			public string Name { get; set; }
			public ulong Id { get; set; }
		}
		#endregion

		#region Item.cs
		public class Item
		{
			public string Name { get; set; }

			public List<Skin> Skins { get; set; } = new List<Skin>();

			public Skin GetSkin(ulong id) => Skins.FirstOrDefault(s => s.Id.Equals(id));
		}
		#endregion
	}

	namespace Player
	{
		#region Player.cs
		public class Player
		{
			public ulong SteamId { get; set; }

			public Dictionary<string, List<ulong>> SelectedSkins { get; set; } = new Dictionary<string, List<ulong>>();

			public Dictionary<string, ulong> DefaultSkins { get; set; } = new Dictionary<string, ulong>();

			public void AddOrRemoveSelectedSkin(string name, ulong skinId)
			{
				if (SelectedSkins.ContainsKey(name))
				{
					if (SelectedSkins[name].Contains(skinId))
					{
						SelectedSkins[name].Remove(skinId);
						return;
					}

					SelectedSkins[name].Add(skinId);
					return;
				}

				SelectedSkins.Add(name, new List<ulong>() { skinId });
				return;
			}
		}
		#endregion
	}

	namespace Extension
	{
		public static class HexExtension
		{
			public static string ToRustFormat(string hex)
			{
				if (string.IsNullOrEmpty(hex)) hex = "#FFFFFFFF";
				var str = hex.Trim('#');
				if (str.Length == 6) str += "FF";
				if (str.Length != 8)
				{
					throw new Exception(hex);
				}

				var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
				var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
				var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
				var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

				Color color = new Color32(r, g, b, a);

				Debug.Log(hex + " " + $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}");

				return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
			}
		}

		public static class PlayerExtension
		{
			public static void SetDefaultSkin(this Player.Player player, Item item)
			{
				ulong skin;
				if (player.DefaultSkins.TryGetValue(item.info.shortname, out skin))
					item.SetItemSkin(skin);
			}

			public static void SetDefaultSkin(this Player.Player player, BaseEntity entity)
			{
				var shortName = entity.GetShortName();

				if (string.IsNullOrEmpty(shortName)) return;

				ulong skin;
				if (player.DefaultSkins.TryGetValue(shortName, out skin))
					entity.SetSkin(skin);
			}

			public static void SetDefaultSkin(this Player.Player player, BuildingBlock block, BuildingGrade.Enum grade = BuildingGrade.Enum.None)
			{
				ulong skin;
				if (player.DefaultSkins.TryGetValue(grade == BuildingGrade.Enum.None ? block.grade.ToString() : grade.ToString(), out skin))
				{
					block.skinID = skin;
					block.UpdateSkin(true);
					block.currentSkin.Refresh(block);
					block.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
				}
			}

			public static BaseEntity FindConstructioninDirectionLook(this BasePlayer player)
			{
				RaycastHit rhit;

				if (!Physics.Raycast(player.eyes.HeadRay(), out rhit, 3f, LayerMask.GetMask("Deployed", "Construction", "Prevent Building"))) return null;
				var entity = rhit.GetEntity();

				if (entity == null) return null;

				return entity;
			}

			public static void SetSkin(this BaseEntity entity, ulong skinId)
			{
				if (entity is BaseVehicle)
				{
					var vehicle = entity as BaseVehicle;

					if (skinId == vehicle.skinID) return;

					BaseVehicle transport = GameManager.server.CreateEntity($"assets/content/vehicles/snowmobiles/{vehicle.ShortPrefabName}.prefab", vehicle.transform.position, vehicle.transform.rotation) as BaseVehicle;
					transport.health = vehicle.health;
					transport.skinID = skinId;

					vehicle.Kill();
					transport.Spawn();
					Effect.server.Run("assets/prefabs/deployable/repair bench/effects/skinchange_spraypaint.prefab", transport.transform.localPosition);
					return;
				}

				if (skinId == entity.skinID) return;

				entity.skinID = skinId;
				entity.SendNetworkUpdate();
				Effect.server.Run("assets/prefabs/deployable/repair bench/effects/skinchange_spraypaint.prefab", entity.transform.localPosition);

			}

			public static string GetShortName(this BaseEntity entity)
			{
				string shortName;

				if (entity is BaseVehicle)
				{
					var vehicle = entity as BaseVehicle;
					shortName = vehicle.ShortPrefabName;
				}
				else if (entity is BuildingBlock)
				{
					var block = entity as BuildingBlock;
					shortName = block.grade.ToString();
				}
				else
				{
					shortName = entity.ShortPrefabName;
				}

				if (string.IsNullOrEmpty(shortName) || Items.Instance.Get(shortName) == null)
					return "";

				return shortName;
			}

			public static bool HasPermission(this BasePlayer player, params string[] perms)
			{
				bool value = true;

				foreach (var perm in perms)
				{
					value &= player.HasPermission(perm);

					if (!value)
						return false;
				}

				return value;
			}

			public static bool HasPermission(this BasePlayer player, string perm)
			{
				if (string.IsNullOrEmpty(perm)) return true;

				return player.IPlayer.HasPermission(perm);
			}

			public static void LootContainer(this BasePlayer player, ItemContainer container)
			{
				player.inventory.loot.Clear();
				player.inventory.loot.PositionChecks = false;
				player.inventory.loot.entitySource = container.entityOwner ?? player;
				player.inventory.loot.itemSource = null;
				player.inventory.loot.MarkDirty();
				player.inventory.loot.AddContainer(container);
				player.inventory.loot.SendImmediate();
				player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", "generic_resizable");
			}
		}

		public static class ItemExtension
		{
			private static readonly Dictionary<string, int> _itemIds = new Dictionary<string, int>();

			private static readonly Dictionary<ulong, string> _hazmats = new Dictionary<ulong, string>()
			{
				[0] = "hazmatsuit",
				[1] = "hazmatsuit.spacesuit",
				[2] = "hazmatsuit.nomadsuit",
				[3] = "hazmatsuit.arcticsuit",
				[4] = "hazmatsuit.lumberjack"
			};

			public static IEnumerable<KeyValuePair<ulong, string>> GetHazmats() => _hazmats;

			public static void SetItemSkin(this Item item, ulong skinId)
			{
				if (item.info.shortname.Contains("hazmatsuit"))
				{
					var pos = item.position;
					var container = item.GetRootContainer();
					var uids = item.uid;
					item.DoRemove();
					item = ItemManager.CreateByName(_hazmats[skinId]);
					item.uid = uids;
					item.MoveToContainer(container, pos);
				}
				else
				{
					item.skin = skinId;
					var hend = item.GetHeldEntity();
					if (hend != null)
					{
						hend.skinID = skinId;
						hend.SendNetworkUpdate();
					}
				}

				item.MarkDirty();
			}

			public static int GetItemId(string name, ulong skinId)
			{
				if (name.Contains("hazmatsuit"))
					return GetItemId(_hazmats[skinId]);

				return GetItemId(name);
			}

			public static int GetItemId(string name)
			{
				int id;
				if (_itemIds.TryGetValue(name, out id))
					return id;

				var def = ItemManager.FindItemDefinition(name);
				if (!def)
					return _itemIds[name] = int.MinValue;

				return _itemIds[name] = def.itemid;
			}
		}
	}
}