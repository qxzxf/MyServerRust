using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("Stacks", "qxzxf", "1.5.11")]
    public class Stacks : RustPlugin
    {
        #region Fields

        [PluginReference] private Plugin ImageLibrary, Notify;

        private const string Layer = "UI.Stacks";

        private const string AdminPerm = "stacks.admin";

        private readonly Dictionary<Item, float> _multiplierByItem = new Dictionary<Item, float>();

        private readonly List<CategoryInfo> _categories = new List<CategoryInfo>();

        private class CategoryInfo
        {
            public string Title;

            public List<ItemInfo> Items;
        }

        private readonly Dictionary<string, int> _defaultItemStackSize = new Dictionary<string, int>();

        #endregion

        #region Config

        private Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Commands", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public string[] Commands = { "stacks" };

            [JsonProperty(PropertyName = "Work with Notify?")]
            public bool UseNotify = true;

            [JsonProperty(PropertyName = "Changing multiplies in containers using a hammer")]
            public bool UserHammer;

            [JsonProperty(PropertyName = "Default Multiplier for new containers")]
            public float DefaultContainerMultiplier = 1f;

            [JsonProperty(PropertyName = "Blocked List")]
            public BlockList BlockList = new BlockList
            {
                Items = new List<string>
                {
                    "item",
                    "short name"
                },
                Skins = new List<ulong>
                {
                    111111111111,
                    222222222222
                }
            };
        }

        private class BlockList
        {
            [JsonProperty(PropertyName = "Items", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Items;

            [JsonProperty(PropertyName = "Skins", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<ulong> Skins;

            public bool Exists(Item item)
            {
                return Items.Contains(item.info.shortname) || Skins.Contains(item.skin);
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
                SaveConfig();
            }
            catch (Exception ex)
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
                Debug.LogException(ex);
            }
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration();
        }

        #endregion

        #region Data

        private List<ItemInfo> _items;

        private Dictionary<string, ContainerData> _containers;

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject($"{Name}/Containers", _containers);

            Interface.Oxide.DataFileSystem.WriteObject($"{Name}/Items", _items);
        }

        private void LoadData()
        {
            try
            {
                _containers =
                    Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, ContainerData>>($"{Name}/Containers");

                _items = Interface.Oxide.DataFileSystem.ReadObject<List<ItemInfo>>($"{Name}/Items");
            }
            catch (Exception e)
            {
                PrintError(e.ToString());
            }

            if (_containers == null) _containers = new Dictionary<string, ContainerData>();
            if (_items == null) _items = new List<ItemInfo>();
        }

        private class ContainerData
        {
            [JsonProperty(PropertyName = "Image")] public string Image;

            [JsonProperty(PropertyName = "Multiplier")] [DefaultValue(1f)]
            public float Multiplier;

            [JsonIgnore] public int ID;
        }

        private class ItemInfo
        {
            [JsonProperty(PropertyName = "ShortName")]
            public string ShortName;

            [JsonProperty(PropertyName = "Name")] public string Name;

            [JsonProperty(PropertyName = "Default Stack Size")]
            public int DefaultStackSize;

            [JsonProperty(PropertyName = "Custom Stack Size")]
            public int CustomStackSize;

            [JsonIgnore] public int ID;
        }

        private readonly Dictionary<string, string> _constContainers = new Dictionary<string, string>
        {
            ["assets/bundled/prefabs/static/bbq.static.prefab"] = "https://i.imgur.com/L28375p.png",
            ["assets/bundled/prefabs/static/hobobarrel_static.prefab"] = "https://i.imgur.com/v8sDTaP.png",
            ["assets/bundled/prefabs/static/recycler_static.prefab"] = "https://i.imgur.com/V1smQYs.png",
            ["assets/bundled/prefabs/static/repairbench_static.prefab"] = "https://i.imgur.com/8qV6Z10.png",
            ["assets/bundled/prefabs/static/researchtable_static.prefab"] = "https://i.imgur.com/guoVK66.png",
            ["assets/bundled/prefabs/static/small_refinery_static.prefab"] = "https://i.imgur.com/o4iHwpz.png",
            ["assets/bundled/prefabs/static/wall.frame.shopfront.metal.static.prefab"] =
                "https://i.imgur.com/aJIU90I.png",
            ["assets/bundled/prefabs/static/water_catcher_small.static.prefab"] = "https://i.imgur.com/ZdaXU6q.png",
            ["assets/bundled/prefabs/static/workbench1.static.prefab"] = "https://i.imgur.com/0Trejvg.png",
            ["assets/content/props/fog machine/fogmachine.prefab"] = "https://i.imgur.com/v33hmbo.png",
            ["assets/content/structures/excavator/prefabs/engine.prefab"] = "",
            ["assets/content/structures/excavator/prefabs/excavator_output_pile.prefab"] = "",
            ["assets/content/vehicles/boats/rhib/subents/fuel_storage.prefab"] = "https://i.imgur.com/QXjLWzj.png",
            ["assets/content/vehicles/boats/rhib/subents/rhib_storage.prefab"] = "https://i.imgur.com/QXjLWzj.png",
            ["assets/content/vehicles/boats/rowboat/subents/fuel_storage.prefab"] = "https://i.imgur.com/FLr37Mb.png",
            ["assets/content/vehicles/boats/rowboat/subents/rowboat_storage.prefab"] =
                "https://i.imgur.com/FLr37Mb.png",
            ["assets/content/vehicles/minicopter/subents/fuel_storage.prefab"] = "https://i.imgur.com/BRBfPjB.png",
            ["assets/content/vehicles/modularcar/2module_car_spawned.entity.prefab"] =
                "https://i.imgur.com/3CSoGly.png",
            ["assets/content/vehicles/modularcar/3module_car_spawned.entity.prefab"] =
                "https://i.imgur.com/HpteUCe.png",
            ["assets/content/vehicles/modularcar/4module_car_spawned.entity.prefab"] =
                "https://i.imgur.com/QI7tzYJ.png",
            ["assets/content/vehicles/modularcar/subents/modular_car_1mod_storage.prefab"] = "",
            ["assets/content/vehicles/modularcar/subents/modular_car_2mod_fuel_tank.prefab"] = "",
            ["assets/content/vehicles/modularcar/subents/modular_car_fuel_storage.prefab"] = "",
            ["assets/content/vehicles/modularcar/subents/modular_car_i4_engine_storage.prefab"] = "",
            ["assets/content/vehicles/modularcar/subents/modular_car_v8_engine_storage.prefab"] = "",
            ["assets/content/vehicles/scrap heli carrier/subents/fuel_storage_scrapheli.prefab"] = "",
            ["assets/prefabs/building/wall.frame.shopfront/wall.frame.shopfront.metal.prefab"] =
                "https://i.imgur.com/aJIU90I.png",
            ["assets/prefabs/deployable/bbq/bbq.deployed.prefab"] = "https://i.imgur.com/L28375p.png",
            ["assets/prefabs/deployable/campfire/campfire.prefab"] = "https://i.imgur.com/FIznmKI.png",
            ["assets/prefabs/deployable/composter/composter.prefab"] = "https://i.imgur.com/glcIjOS.png",
            ["assets/prefabs/deployable/dropbox/dropbox.deployed.prefab"] = "https://i.imgur.com/HmoyaIU.png",
            ["assets/prefabs/deployable/fireplace/fireplace.deployed.prefab"] = "https://i.imgur.com/XsMSlNY.png",
            ["assets/prefabs/deployable/fridge/fridge.deployed.prefab"] = "https://i.imgur.com/ERNmHjz.png",
            ["assets/prefabs/deployable/furnace.large/furnace.large.prefab"] = "https://i.imgur.com/GWaSIUw.png",
            ["assets/prefabs/deployable/furnace/furnace.prefab"] = "https://i.imgur.com/cnFpbOj.png",
            ["assets/prefabs/deployable/hitch & trough/hitchtrough.deployed.prefab"] =
                "https://i.imgur.com/FiSIYh9.png",
            ["assets/prefabs/deployable/hot air balloon/subents/hab_storage.prefab"] =
                "https://i.imgur.com/KaGFKkM.png",
            ["assets/prefabs/deployable/jack o lantern/jackolantern.angry.prefab"] = "https://i.imgur.com/iPBEYf3.png",
            ["assets/prefabs/deployable/jack o lantern/jackolantern.happy.prefab"] = "https://i.imgur.com/brKtJJj.png",
            ["assets/prefabs/deployable/lantern/lantern.deployed.prefab"] = "https://i.imgur.com/LqfkTKp.png",
            ["assets/prefabs/deployable/large wood storage/box.wooden.large.prefab"] =
                "https://i.imgur.com/wecMrji.png",
            ["assets/prefabs/deployable/liquidbarrel/waterbarrel.prefab"] = "https://i.imgur.com/LAHPuI9.png",
            ["assets/prefabs/deployable/locker/locker.deployed.prefab"] = "https://i.imgur.com/jZ4raNL.png",
            ["assets/prefabs/deployable/mailbox/mailbox.deployed.prefab"] = "https://i.imgur.com/egLTaYb.png",
            ["assets/prefabs/deployable/mixingtable/mixingtable.deployed.prefab"] = "https://i.imgur.com/sbyHOjn.png",
            ["assets/prefabs/deployable/oil jack/crudeoutput.prefab"] = "",
            ["assets/prefabs/deployable/oil jack/fuelstorage.prefab"] = "",
            ["assets/prefabs/deployable/oil refinery/refinery_small_deployed.prefab"] =
                "https://i.imgur.com/1KMt1eu.png",
            ["assets/prefabs/deployable/planters/planter.large.deployed.prefab"] = "https://i.imgur.com/POcQ0Ya.png",
            ["assets/prefabs/deployable/planters/planter.small.deployed.prefab"] = "https://i.imgur.com/fMO8cJF.png",
            ["assets/prefabs/deployable/playerioents/generators/fuel generator/small_fuel_generator.deployed.prefab"] =
                "https://i.imgur.com/fghbYKE.png",
            ["assets/prefabs/deployable/playerioents/poweredwaterpurifier/poweredwaterpurifier.deployed.prefab"] =
                "https://i.imgur.com/Tg2dX8b.png",
            ["assets/prefabs/deployable/playerioents/poweredwaterpurifier/poweredwaterpurifier.storage.prefab"] =
                "https://i.imgur.com/Tg2dX8b.png",
            ["assets/prefabs/deployable/playerioents/waterpump/water.pump.deployed.prefab"] =
                "https://i.imgur.com/FZG19ki.png",
            ["assets/prefabs/deployable/quarry/fuelstorage.prefab"] = "https://i.imgur.com/U1y3pmJ.png",
            ["assets/prefabs/deployable/quarry/hopperoutput.prefab"] = "https://i.imgur.com/U1y3pmJ.png",
            ["assets/prefabs/deployable/repair bench/repairbench_deployed.prefab"] = "https://i.imgur.com/8qV6Z10.png",
            ["assets/prefabs/deployable/research table/researchtable_deployed.prefab"] =
                "https://i.imgur.com/guoVK66.png",
            ["assets/prefabs/deployable/single shot trap/guntrap.deployed.prefab"] = "https://i.imgur.com/rGstq6A.png",
            ["assets/prefabs/deployable/small stash/small_stash_deployed.prefab"] = "https://i.imgur.com/ToPKE7j.png",
            ["assets/prefabs/deployable/survivalfishtrap/survivalfishtrap.deployed.prefab"] =
                "https://i.imgur.com/2D6jZ7j.png",
            ["assets/prefabs/deployable/tier 1 workbench/workbench1.deployed.prefab"] =
                "https://i.imgur.com/0Trejvg.png",
            ["assets/prefabs/deployable/tier 2 workbench/workbench2.deployed.prefab"] =
                "https://i.imgur.com/cM5F6SO.png",
            ["assets/prefabs/deployable/tier 3 workbench/workbench3.deployed.prefab"] =
                "https://i.imgur.com/ToyPHJK.png",
            ["assets/prefabs/deployable/tool cupboard/cupboard.tool.deployed.prefab"] =
                "https://i.imgur.com/mD9KsAL.png",
            ["assets/prefabs/deployable/tuna can wall lamp/tunalight.deployed.prefab"] =
                "https://i.imgur.com/EWXtCJg.png",
            ["assets/prefabs/deployable/vendingmachine/npcvendingmachines/npcvendingmachine_attire.prefab"] = "",
            ["assets/prefabs/deployable/vendingmachine/npcvendingmachines/npcvendingmachine_building.prefab"] = "",
            ["assets/prefabs/deployable/vendingmachine/npcvendingmachines/npcvendingmachine_components.prefab"] = "",
            ["assets/prefabs/deployable/vendingmachine/npcvendingmachines/npcvendingmachine_extra.prefab"] = "",
            ["assets/prefabs/deployable/vendingmachine/npcvendingmachines/npcvendingmachine_farming.prefab"] = "",
            ["assets/prefabs/deployable/vendingmachine/npcvendingmachines/npcvendingmachine_resources.prefab"] = "",
            ["assets/prefabs/deployable/vendingmachine/npcvendingmachines/npcvendingmachine_tools.prefab"] = "",
            ["assets/prefabs/deployable/vendingmachine/npcvendingmachines/npcvendingmachine_vehicleshigh.prefab"] = "",
            ["assets/prefabs/deployable/vendingmachine/npcvendingmachines/npcvendingmachine_weapons.prefab"] = "",
            ["assets/prefabs/deployable/vendingmachine/npcvendingmachines/shopkeeper_vm_invis.prefab"] = "",
            ["assets/prefabs/deployable/vendingmachine/vendingmachine.deployed.prefab"] =
                "https://i.imgur.com/8Kfvfgp.png",
            ["assets/prefabs/deployable/water catcher/water_catcher_large.prefab"] = "https://i.imgur.com/MF90xE7.png",
            ["assets/prefabs/deployable/water catcher/water_catcher_small.prefab"] = "https://i.imgur.com/ZdaXU6q.png",
            ["assets/prefabs/deployable/water well/waterwellstatic.prefab"] = "",
            ["assets/prefabs/deployable/water well/waterwellstatic.prefab"] = "https://i.imgur.com/FyQJnhX.png",
            ["assets/prefabs/deployable/waterpurifier/waterstorage.prefab"] = "https://i.imgur.com/FyQJnhX.png",
            ["assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab"] = "https://i.imgur.com/gwhRYjt.png",
            ["assets/prefabs/io/electric/switches/fusebox/fusebox.prefab"] = "",
            ["assets/prefabs/misc/casino/bigwheel/bigwheelbettingterminal.prefab"] = "",
            ["assets/prefabs/misc/chinesenewyear/chineselantern/chineselantern.deployed.prefab"] =
                "https://i.imgur.com/WUa0fN2.png",
            ["assets/prefabs/misc/halloween/coffin/coffinstorage.prefab"] = "https://i.imgur.com/zHbT59P.png",
            ["assets/prefabs/misc/halloween/cursed_cauldron/cursedcauldron.deployed.prefab"] =
                "https://i.imgur.com/z6QnrT3.png",
            ["assets/prefabs/misc/halloween/skull_fire_pit/skull_fire_pit.prefab"] = "https://i.imgur.com/9phq5Bu.png",
            ["assets/prefabs/misc/halloween/trophy skulls/skulltrophy.deployed.prefab"] =
                "https://i.imgur.com/TmntgJT.png",
            ["assets/prefabs/misc/item drop/item_drop.prefab"] = "",
            ["assets/prefabs/misc/item drop/item_drop_backpack.prefab"] = "",
            ["assets/prefabs/misc/marketplace/marketterminal.prefab"] = "",
            ["assets/prefabs/misc/summer_dlc/abovegroundpool/abovegroundpool.deployed.prefab"] = "",
            ["assets/prefabs/misc/summer_dlc/paddling_pool/paddlingpool.deployed.prefab"] =
                "https://i.imgur.com/v2V6T7d.png",
            ["assets/prefabs/misc/summer_dlc/photoframe/photoframe.landscape.prefab"] =
                "https://i.imgur.com/nH2jf5j.png",
            ["assets/prefabs/misc/summer_dlc/photoframe/photoframe.large.prefab"] = "https://i.imgur.com/sPfBcVt.png",
            ["assets/prefabs/misc/summer_dlc/photoframe/photoframe.portrait.prefab"] =
                "https://i.imgur.com/gvbD7Pm.png",
            ["assets/prefabs/misc/supply drop/supply_drop.prefab"] = "https://i.imgur.com/VAtGtQB.png",
            ["assets/prefabs/misc/xmas/snow_machine/models/snowmachine.prefab"] = "https://i.imgur.com/pAqw9It.png",
            ["assets/prefabs/misc/twitch/hobobarrel/hobobarrel.deployed.prefab"] = "https://i.imgur.com/v8sDTaP.png",
            ["assets/prefabs/misc/xmas/xmastree/xmas_tree.deployed.prefab"] = "https://i.imgur.com/wQU9ojJ.png",
            ["assets/prefabs/npc/autoturret/autoturret_deployed.prefab"] = "https://i.imgur.com/VUiBkC5.png",
            ["assets/prefabs/npc/flame turret/flameturret.deployed.prefab"] = "https://i.imgur.com/TcIOwLa.png",
            ["assets/prefabs/npc/sam_site_turret/sam_site_turret_deployed.prefab"] = "https://i.imgur.com/SNBPqIX.png"
        };

        #endregion

        #region Hooks

        private void Init()
        {
            LoadData();

            if (!_config.UserHammer)
                Unsubscribe(nameof(OnHammerHit));
        }

        private void OnServerInitialized(bool init)
        {
            LoadItems();

            LoadCategories();

            LoadContainers(init);

            LoadImages();

            RegisterPermissions();

            AddCovalenceCommand(_config.Commands, nameof(CmdOpenStacks));
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, Layer);

            foreach (var check in _defaultItemStackSize)
            {
                var info = ItemManager.FindItemDefinition(check.Key);
                if (info != null)
                    info.stackable = check.Value;
            }

            SaveData();
        }

        private object CanMoveItem(Item movedItem, PlayerInventory playerInventory, ulong targetContainerID,
            int targetSlot, int amount) //credits Jake_Rich
        {
            if (movedItem == null || playerInventory == null || _config.BlockList.Exists(movedItem)) return null;

            _multiplierByItem.Remove(movedItem);

            var container = playerInventory.FindContainer(new ItemContainerId(targetContainerID));
            var player = playerInventory.GetComponent<BasePlayer>();

            if (container != null)
            {
                var entity = container.entityOwner;
                if (entity != null)
                {
                    ContainerData data;
                    if (_containers.TryGetValue(entity.name, out data)) _multiplierByItem[movedItem] = data.Multiplier;
                }
            }

            #region Right-Click Overstack into Player Inventory

            if (targetSlot == -1)
            {
                //Right click overstacks into player inventory
                if (container == null)
                {
                    if (movedItem.amount > movedItem.info.stackable)
                    {
                        var loops = 1;
                        if (player.serverInput.IsDown(BUTTON.SPRINT))
                            loops = Mathf.CeilToInt((float)movedItem.amount / movedItem.info.stackable);
                        for (var i = 0; i < loops; i++)
                        {
                            if (movedItem.amount <= movedItem.info.stackable)
                            {
                                playerInventory.GiveItem(movedItem);
                                break;
                            }

                            var itemToMove = movedItem.SplitItem(movedItem.info.stackable);
                            var moved = playerInventory.GiveItem(itemToMove);
                            if (moved == false)
                            {
                                movedItem.amount += itemToMove.amount;
                                itemToMove.Remove();
                                break;
                            }

                            movedItem.MarkDirty();
                        }

                        playerInventory.ServerUpdate(0f);
                        return true;
                    }
                }
                //Shift Right click into storage container
                else
                {
                    if (player.serverInput.IsDown(BUTTON.SPRINT))
                    {
                        foreach (var item in playerInventory.containerMain.itemList.Where(x => x.info == movedItem.info)
                            .ToList()) item.MoveToContainer(container);
                        foreach (var item in playerInventory.containerBelt.itemList.Where(x => x.info == movedItem.info)
                            .ToList()) item.MoveToContainer(container);
                        playerInventory.ServerUpdate(0f);
                        return false;
                    }
                }
            }

            #endregion

            #region Moving Overstacks Around In Chest

            if (amount > movedItem.info.stackable && container != null)
            {
                var targetItem = container.GetSlot(targetSlot);
                if (targetItem == null)
                {
                    if (amount < movedItem.amount)
                        //Split item into chest
                        ItemHelper.SplitMoveItem(movedItem, amount, container, targetSlot);
                    else
                        //Moving items when amount > info.stacksize
                        ItemHelper.SplitMoveItem(movedItem, movedItem.info.stackable, container, targetSlot);
                }
                else
                {
                    if (!targetItem.CanStack(movedItem) && amount == movedItem.amount)
                    {
                        //Swapping positions of items
                        ItemHelper.SwapItems(movedItem, targetItem);
                    }
                    else
                    {
                        if (amount < movedItem.amount)
                            ItemHelper.SplitMoveItem(movedItem, amount, playerInventory);
                        else
                            movedItem.MoveToContainer(container, targetSlot);
                        //Stacking items when amount > info.stacksize
                    }
                }

                playerInventory.ServerUpdate(0f);
                return true;
            }

            #endregion

            #region Prevent Moving Overstacks To Inventory

            if (container != null)
            {
                var targetItem = container.GetSlot(targetSlot);
                if (targetItem != null)
                    if (movedItem.parent.playerOwner == player)
                        if (!movedItem.CanStack(targetItem))
                            if (targetItem.amount > targetItem.info.stackable)
                                return true;
            }

            #endregion

            return null;
        }

        private int? OnMaxStackable(Item item)
        {
            if (item == null || item.info.itemType == ItemContainer.ContentsType.Liquid || item.info.stackable == 1 ||
                _config.BlockList.Exists(item))
                return null;

            float multiplier;
            if (_multiplierByItem.TryGetValue(item, out multiplier))
                return Mathf.FloorToInt(item.info.stackable * multiplier);

            if (item.parent == null || item.parent.entityOwner == null) return null;

            ContainerData data;
            if (_containers.TryGetValue(item.parent.entityOwner.name, out data))
                return Mathf.FloorToInt(data.Multiplier * item.info.stackable);

            return null;
        }

        private void OnItemDropped(Item item, BaseEntity entity)
        {
            item.RemoveFromContainer();
            var stackSize = item.info.stackable;
            if (item.amount <= stackSize) return;

            var loops = Mathf.FloorToInt((float)item.amount / stackSize);
            if (loops > 20) return;
            for (var i = 0; i < loops; i++)
            {
                if (item.amount <= stackSize) break;

                item.SplitItem(stackSize)?.Drop(entity.transform.position,
                    entity.GetComponent<Rigidbody>().velocity + Vector3Ex.Range(-1f, 1f));
            }
        }

        private void OnHammerHit(BasePlayer player, HitInfo info)
        {
            if (!_config.UserHammer || player == null ||
                !permission.UserHasPermission(player.UserIDString, AdminPerm) || info == null)
                return;

            var container = info.HitEntity;
            if (container == null)
                return;

            ContainerData data;
            if (_containers.TryGetValue(container.name, out data))
                SettingsUi(player, 1, 0, 0, data.ID, first: true);
        }

        #endregion

        #region Commands

        private void CmdOpenStacks(IPlayer cov, string command, string[] args)
        {
            var player = cov?.Object as BasePlayer;
            if (player == null) return;

            if (!permission.UserHasPermission(player.UserIDString, AdminPerm))
            {
                SendNotify(player, NoPermission, 1);
                return;
            }

            if (args.Length == 0)
            {
                MainUi(player, first: true);
                return;
            }

            switch (args[0])
            {
                case "sethandstack":
                {
                    int stackSize;
                    if (args.Length < 2 || !int.TryParse(args[1], out stackSize))
                    {
                        cov.Reply($"Error syntax! Use: /{command} {args[0]} [stackSize]");
                        return;
                    }

                    var activeItem = player.GetActiveItem();
                    if (activeItem == null)
                    {
                        cov.Reply("You are missing an item in your hand!");
                        return;
                    }

                    var item = _items.Find(x => x.ShortName == activeItem.info.shortname);
                    if (item == null)
                    {
                        cov.Reply("Item not found!");
                        return;
                    }

                    item.CustomStackSize = stackSize;

                    UpdateItemStack(item);

                    SendNotify(player, SetStack, 0, stackSize, item.Name);

                    SaveData();
                    break;
                }

                case "setstack":
                {
                    int stackSize;
                    if (args.Length < 3 || !int.TryParse(args[2], out stackSize))
                    {
                        cov.Reply($"Error syntax! Use: /{command} {args[0]} [shortName] [stackSize]");
                        return;
                    }

                    var item = _items.Find(x => x.ShortName == args[1]);
                    if (item == null)
                    {
                        cov.Reply($"Item '{args[1]}' not found!");
                        return;
                    }

                    item.CustomStackSize = stackSize;

                    UpdateItemStack(item);

                    SendNotify(player, SetStack, 0, stackSize, item.Name);

                    SaveData();
                    break;
                }
            }
        }

        [ConsoleCommand("UI_Stacks")]
        private void CmdConsoleStacks(ConsoleSystem.Arg arg)
        {
            var player = arg?.Player();
            if (player == null || !arg.HasArgs()) return;

            switch (arg.Args[0])
            {
                case "page":
                {
                    int type, category = -1, page = 0;
                    if (!arg.HasArgs(2) || !int.TryParse(arg.Args[1], out type)) return;

                    if (arg.HasArgs(3))
                        int.TryParse(arg.Args[2], out category);

                    if (arg.HasArgs(4))
                        int.TryParse(arg.Args[3], out page);

                    var search = string.Empty;
                    if (arg.HasArgs(5))
                        search = arg.Args[4];

                    MainUi(player, type, category, page, search);
                    break;
                }

                case "enter_page":
                {
                    int type, category, page;
                    if (!arg.HasArgs(5) ||
                        !int.TryParse(arg.Args[1], out type) ||
                        !int.TryParse(arg.Args[2], out category) ||
                        !int.TryParse(arg.Args[4], out page)) return;

                    var search = arg.Args[3];
                    if (string.IsNullOrEmpty(search)) return;

                    MainUi(player, type, category, page, search);
                    break;
                }

                case "settings":
                {
                    int type, category, page, id;
                    if (!arg.HasArgs(5) ||
                        !int.TryParse(arg.Args[1], out type) ||
                        !int.TryParse(arg.Args[2], out category) ||
                        !int.TryParse(arg.Args[3], out page) ||
                        !int.TryParse(arg.Args[4], out id)) return;

                    var enterValue = -1;
                    if (arg.HasArgs(6))
                        int.TryParse(arg.Args[5], out enterValue);

                    SettingsUi(player, type, category, page, id, enterValue);
                    break;
                }

                case "apply_settings":
                {
                    int type, category, page, id;
                    float nowValue;
                    if (!arg.HasArgs(6) ||
                        !int.TryParse(arg.Args[1], out type) ||
                        !int.TryParse(arg.Args[2], out category) ||
                        !int.TryParse(arg.Args[3], out page) ||
                        !int.TryParse(arg.Args[4], out id) ||
                        !float.TryParse(arg.Args[5], out nowValue) || nowValue <= 0) return;

                    switch (type)
                    {
                        case 0: //item
                        {
                            var item = _items.Find(x => x.ID == id);
                            if (item == null) return;

                            item.CustomStackSize = (int)nowValue;

                            UpdateItemStack(item);

                            SaveData();
                            break;
                        }

                        case 1: //container
                        {
                            if (_containers.All(x => x.Value.ID != id))
                                return;

                            var cont = _containers.FirstOrDefault(x => x.Value.ID == id);

                            cont.Value.Multiplier = nowValue;

                            SaveData();
                            break;
                        }
                    }

                    MainUi(player, type, category, page);
                    break;
                }
            }
        }

        #endregion

        #region Interface

        private void MainUi(BasePlayer player, int type = 0, int category = -1, int page = 0, string search = "",
            bool first = false)
        {
            var lines = 0;
            float height;
            float ySwitch;

            var container = new CuiElementContainer();

            #region Background

            if (first)
            {
                CuiHelper.DestroyUi(player, Layer);

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Image =
                    {
                        Color = "0 0 0 0.9",
                        Material = "assets/content/ui/uibackgroundblur.mat"
                    },
                    CursorEnabled = true
                }, "Overlay", Layer);

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = "" },
                    Button =
                    {
                        Color = "0 0 0 0",
                        Close = Layer
                    }
                }, Layer);
            }

            #endregion

            #region Main

            container.Add(new CuiPanel
            {
                RectTransform =
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
                    OffsetMin = "-300 -250",
                    OffsetMax = "300 255"
                },
                Image =
                {
                    Color = HexToCuiColor("#0E0E10")
                }
            }, Layer, Layer + ".Main");

            #region Header

            container.Add(new CuiPanel
            {
                RectTransform =
                {
                    AnchorMin = "0 1", AnchorMax = "1 1",
                    OffsetMin = "0 -50",
                    OffsetMax = "0 0"
                },
                Image = { Color = HexToCuiColor("#161617") }
            }, Layer + ".Main", Layer + ".Header");

            container.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = "0 0", AnchorMax = "1 1",
                    OffsetMin = "30 0",
                    OffsetMax = "0 0"
                },
                Text =
                {
                    Text = Msg(player, TitleMenu),
                    Align = TextAnchor.MiddleLeft,
                    Font = "robotocondensed-bold.ttf",
                    FontSize = 14,
                    Color = HexToCuiColor("#FFFFFF")
                }
            }, Layer + ".Header");

            float xSwitch = -25;
            float width = 25;
            float margin = 5;

            container.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = "1 1", AnchorMax = "1 1",
                    OffsetMin = $"{xSwitch - width} -37.5",
                    OffsetMax = $"{xSwitch} -12.5"
                },
                Text =
                {
                    Text = Msg(player, CloseButton),
                    Align = TextAnchor.MiddleCenter,
                    Font = "robotocondensed-bold.ttf",
                    FontSize = 10,
                    Color = HexToCuiColor("#FFFFFF")
                },
                Button =
                {
                    Close = Layer,
                    Color = HexToCuiColor("#4B68FF")
                }
            }, Layer + ".Header");

            xSwitch = xSwitch - margin - width;
            width = 80;

            container.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = "1 1", AnchorMax = "1 1",
                    OffsetMin = $"{xSwitch - width} -37.5",
                    OffsetMax = $"{xSwitch} -12.5"
                },
                Text =
                {
                    Text = Msg(player, ContainerTitle),
                    Align = TextAnchor.MiddleCenter,
                    Font = "robotocondensed-regular.ttf",
                    FontSize = 12,
                    Color = HexToCuiColor("#FFFFFF")
                },
                Button =
                {
                    Color = type == 1 ? HexToCuiColor("#4B68FF") : HexToCuiColor("#FFFFFF", 5),
                    Command = "UI_Stacks page 1"
                }
            }, Layer + ".Header");

            xSwitch = xSwitch - margin - width;

            container.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = "1 1", AnchorMax = "1 1",
                    OffsetMin = $"{xSwitch - width} -37.5",
                    OffsetMax = $"{xSwitch} -12.5"
                },
                Text =
                {
                    Text = Msg(player, ItemTitle),
                    Align = TextAnchor.MiddleCenter,
                    Font = "robotocondensed-regular.ttf",
                    FontSize = 12,
                    Color = HexToCuiColor("#FFFFFF")
                },
                Button =
                {
                    Color = type == 0 ? HexToCuiColor("#4B68FF") : HexToCuiColor("#FFFFFF", 5),
                    Command = "UI_Stacks page 0"
                }
            }, Layer + ".Header");

            #region Search

            xSwitch = xSwitch - margin - width;
            width = 140;

            container.Add(new CuiPanel
            {
                RectTransform =
                {
                    AnchorMin = "1 1", AnchorMax = "1 1",
                    OffsetMin = $"{xSwitch - width} -37.5",
                    OffsetMax = $"{xSwitch} -12.5"
                },
                Image =
                {
                    Color = HexToCuiColor("#000000")
                }
            }, Layer + ".Header", Layer + ".Header.Search");

            container.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = "0 0", AnchorMax = "1 1",
                    OffsetMin = "10 0", OffsetMax = "-10 0"
                },
                Text =
                {
                    Text = string.IsNullOrEmpty(search) ? Msg(player, SearchTitle) : $"{search}",
                    Align = TextAnchor.MiddleLeft,
                    Font = "robotocondensed-regular.ttf",
                    FontSize = 12,
                    Color = "1 1 1 0.65"
                }
            }, Layer + ".Header.Search");

            container.Add(new CuiElement
            {
                Parent = Layer + ".Header.Search",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-regular.ttf",
                        Command = $"UI_Stacks page {type} {category} {page} ",
                        Color = "1 1 1 0.95",
                        CharsLimit = 32
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0", AnchorMax = "1 1"
                    }
                }
            });

            #endregion

            #endregion

            var maxCount = 0;

            switch (type)
            {
                case 0: //Item
                {
                    #region Categories

                    var amountOnString = 6;
                    width = 90;
                    margin = 5;
                    height = 25;
                    ySwitch = -60;

                    xSwitch = 20;

                    container.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 1", AnchorMax = "0 1",
                            OffsetMin = $"{xSwitch} {ySwitch - height}",
                            OffsetMax = $"{xSwitch + width} {ySwitch}"
                        },
                        Text =
                        {
                            Text = Msg(player, AllTitle),
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-bold.ttf",
                            FontSize = 12,
                            Color = "1 1 1 1"
                        },
                        Button =
                        {
                            Color = -1 == category ? HexToCuiColor("#4B68FF") : HexToCuiColor("#161617"),
                            Command = $"UI_Stacks page {type} {-1}"
                        }
                    }, Layer + ".Main");

                    xSwitch += width + margin;

                    for (var i = 0; i < _categories.Count; i++)
                    {
                        var info = _categories[i];

                        container.Add(new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin = "0 1", AnchorMax = "0 1",
                                OffsetMin = $"{xSwitch} {ySwitch - height}",
                                OffsetMax = $"{xSwitch + width} {ySwitch}"
                            },
                            Text =
                            {
                                Text = $"{info.Title}",
                                Align = TextAnchor.MiddleCenter,
                                Font = "robotocondensed-bold.ttf",
                                FontSize = 12,
                                Color = "1 1 1 1"
                            },
                            Button =
                            {
                                Color = i == category ? HexToCuiColor("#4B68FF") : HexToCuiColor("#161617"),
                                Command = $"UI_Stacks page {type} {i}"
                            }
                        }, Layer + ".Main");

                        if ((i + 2) % amountOnString == 0)
                        {
                            ySwitch = ySwitch - height - margin;
                            xSwitch = 20;
                        }
                        else
                        {
                            xSwitch += width + margin;
                        }
                    }

                    #endregion

                    #region Items

                    ySwitch = ySwitch - height - 5;

                    margin = 5;
                    height = 50f;
                    xSwitch = 20;
                    width = 565;
                    lines = 6;

                    var categoryInfo = category == -1
                        ? _categories.SelectMany(x => x.Items).ToList()
                        : _categories[category].Items;

                    var items = string.IsNullOrEmpty(search) || search.Length < 2
                        ? categoryInfo
                        : categoryInfo.FindAll(x => x.Name.StartsWith(search) || x.Name.Contains(search) ||
                                                    x.ShortName.StartsWith(search) || x.ShortName.Contains(search));

                    maxCount = items.Count;

                    foreach (var item in items.Skip(page * lines).Take(lines))
                    {
                        container.Add(new CuiPanel
                        {
                            RectTransform =
                            {
                                AnchorMin = "0 1", AnchorMax = "0 1",
                                OffsetMin = $"{xSwitch} {ySwitch - height}",
                                OffsetMax = $"{xSwitch + width} {ySwitch}"
                            },
                            Image =
                            {
                                Color = HexToCuiColor("#161617")
                            }
                        }, Layer + ".Main", Layer + $".Panel.{item.ShortName}");

                        if (ImageLibrary)
                            container.Add(new CuiElement
                            {
                                Parent = Layer + $".Panel.{item.ShortName}",
                                Components =
                                {
                                    new CuiRawImageComponent
                                        { Png = ImageLibrary.Call<string>("GetImage", item.ShortName) },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0 0.5", AnchorMax = "0 0.5",
                                        OffsetMin = "20 -15", OffsetMax = "50 15"
                                    }
                                }
                            });

                        #region Name

                        container.Add(new CuiLabel
                        {
                            RectTransform =
                            {
                                AnchorMin = "0 0", AnchorMax = "1 1",
                                OffsetMin = "55 0", OffsetMax = "0 0"
                            },
                            Text =
                            {
                                Text = $"{item.Name}",
                                Align = TextAnchor.MiddleLeft,
                                Font = "robotocondensed-regular.ttf",
                                FontSize = 12,
                                Color = "1 1 1 1"
                            }
                        }, Layer + $".Panel.{item.ShortName}");

                        #endregion

                        #region Default Stack

                        container.Add(new CuiLabel
                        {
                            RectTransform =
                            {
                                AnchorMin = "0 0.5", AnchorMax = "1 1",
                                OffsetMin = "180 0", OffsetMax = "0 0"
                            },
                            Text =
                            {
                                Text = Msg(player, DefaultStack),
                                Align = TextAnchor.LowerLeft,
                                Font = "robotocondensed-regular.ttf",
                                FontSize = 12,
                                Color = "1 1 1 0.5"
                            }
                        }, Layer + $".Panel.{item.ShortName}");

                        container.Add(new CuiLabel
                        {
                            RectTransform =
                            {
                                AnchorMin = "0 0", AnchorMax = "1 0.5",
                                OffsetMin = "180 0", OffsetMax = "0 0"
                            },
                            Text =
                            {
                                Text = Msg(player, StackFormat, item.DefaultStackSize),
                                Align = TextAnchor.UpperLeft,
                                Font = "robotocondensed-regular.ttf",
                                FontSize = 12,
                                Color = "1 1 1 1"
                            }
                        }, Layer + $".Panel.{item.ShortName}");

                        #endregion

                        #region Custom Stack

                        container.Add(new CuiLabel
                        {
                            RectTransform =
                            {
                                AnchorMin = "0 0.5", AnchorMax = "1 1",
                                OffsetMin = "310 0", OffsetMax = "0 0"
                            },
                            Text =
                            {
                                Text = Msg(player, CustomStack),
                                Align = TextAnchor.LowerLeft,
                                Font = "robotocondensed-regular.ttf",
                                FontSize = 12,
                                Color = "1 1 1 0.5"
                            }
                        }, Layer + $".Panel.{item.ShortName}");

                        container.Add(new CuiLabel
                        {
                            RectTransform =
                            {
                                AnchorMin = "0 0", AnchorMax = "1 0.5",
                                OffsetMin = "310 0", OffsetMax = "0 0"
                            },
                            Text =
                            {
                                Text = Msg(player, StackFormat,
                                    item.CustomStackSize == 0 ? item.DefaultStackSize : item.CustomStackSize),
                                Align = TextAnchor.UpperLeft,
                                Font = "robotocondensed-regular.ttf",
                                FontSize = 12,
                                Color = "1 1 1 1"
                            }
                        }, Layer + $".Panel.{item.ShortName}");

                        #endregion

                        #region Settings

                        container.Add(new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin = "1 0.5", AnchorMax = "1 0.5",
                                OffsetMin = "-110 -12.5",
                                OffsetMax = "-10 12.5"
                            },
                            Text =
                            {
                                Text = Msg(player, SettingsTitle),
                                Align = TextAnchor.MiddleCenter,
                                Font = "robotocondensed-regular.ttf",
                                FontSize = 12,
                                Color = "1 1 1 1"
                            },
                            Button =
                            {
                                Color = HexToCuiColor("#4B68FF"),
                                Command =
                                    $"UI_Stacks settings {type} {category} {(string.IsNullOrEmpty(search) ? page : 0)} {item.ID}"
                            }
                        }, Layer + $".Panel.{item.ShortName}");

                        #endregion

                        ySwitch = ySwitch - height - margin;
                    }

                    #endregion

                    break;
                }

                case 1: //Container
                {
                    #region Items

                    ySwitch = -65;

                    margin = 10;
                    height = 60;
                    xSwitch = 20;
                    width = 565;
                    lines = 6;

                    var containers = string.IsNullOrEmpty(search) || search.Length < 2
                        ? _containers.ToList()
                        : _containers.Where(x => x.Key.EndsWith(search) || x.Key.Contains(search)).ToList();

                    maxCount = containers.Count;

                    foreach (var check in containers.Skip(page * lines).Take(lines))
                    {
                        container.Add(new CuiPanel
                        {
                            RectTransform =
                            {
                                AnchorMin = "0 1", AnchorMax = "0 1",
                                OffsetMin = $"{xSwitch} {ySwitch - height}",
                                OffsetMax = $"{xSwitch + width} {ySwitch}"
                            },
                            Image =
                            {
                                Color = HexToCuiColor("#161617")
                            }
                        }, Layer + ".Main", Layer + $".Panel.{ySwitch}");

                        #region Image

                        container.Add(new CuiPanel
                        {
                            RectTransform =
                            {
                                AnchorMin = "0 0", AnchorMax = "0 0",
                                OffsetMin = "5 5", OffsetMax = "55 55"
                            },
                            Image =
                            {
                                Color = HexToCuiColor("#0E0E10", 50)
                            }
                        }, Layer + $".Panel.{ySwitch}", Layer + $".Panel.{ySwitch}.Image");

                        container.Add(new CuiElement
                        {
                            Parent = Layer + $".Panel.{ySwitch}.Image",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = ImageLibrary.Call<string>("GetImage",
                                        string.IsNullOrEmpty(check.Value.Image) ? "NONE" : check.Value.Image)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0", AnchorMax = "1 1"
                                }
                            }
                        });

                        #endregion

                        #region Prefab

                        container.Add(new CuiPanel
                        {
                            RectTransform =
                            {
                                AnchorMin = "1 1", AnchorMax = "1 1",
                                OffsetMin = "-500 -25", OffsetMax = "0 0 0 0"
                            },
                            Image =
                            {
                                Color = HexToCuiColor("#0E0E10", 50)
                            }
                        }, Layer + $".Panel.{ySwitch}", Layer + $".Panel.{ySwitch}.Prefab");

                        container.Add(new CuiLabel
                        {
                            RectTransform =
                            {
                                AnchorMin = "0 0", AnchorMax = "1 1",
                                OffsetMin = "5 0", OffsetMax = "0 0"
                            },
                            Text =
                            {
                                Text = $"{check.Key}",
                                Align = TextAnchor.MiddleLeft,
                                FontSize = 12,
                                Font = "robotocondensed-regular.ttf",
                                Color = "1 1 1 0.5"
                            }
                        }, Layer + $".Panel.{ySwitch}.Prefab");

                        #endregion

                        #region Info

                        container.Add(new CuiLabel
                        {
                            RectTransform =
                            {
                                AnchorMin = "0 0", AnchorMax = "0 0",
                                OffsetMin = "70 0",
                                OffsetMax = "200 35"
                            },
                            Text =
                            {
                                Text = Msg(player, DefaultMultiplier, _config.DefaultContainerMultiplier),
                                Align = TextAnchor.MiddleLeft,
                                Font = "robotocondensed-regular.ttf",
                                FontSize = 12,
                                Color = "1 1 1 0.5"
                            }
                        }, Layer + $".Panel.{ySwitch}");

                        container.Add(new CuiLabel
                        {
                            RectTransform =
                            {
                                AnchorMin = "0 0", AnchorMax = "0 0",
                                OffsetMin = "260 0",
                                OffsetMax = "400 35"
                            },
                            Text =
                            {
                                Text = Msg(player, CustomMultiplier, check.Value.Multiplier),
                                Align = TextAnchor.MiddleLeft,
                                Font = "robotocondensed-regular.ttf",
                                FontSize = 12,
                                Color = "1 1 1 0.5"
                            }
                        }, Layer + $".Panel.{ySwitch}");

                        #endregion

                        container.Add(new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin = "1 0", AnchorMax = "1 0",
                                OffsetMin = "-90 7.5", OffsetMax = "-10 27.5"
                            },
                            Text =
                            {
                                Text = Msg(player, SettingsTitle),
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 14,
                                Font = "robotocondensed-regular.ttf",
                                Color = "1 1 1 1"
                            },
                            Button =
                            {
                                Color = HexToCuiColor("#4B68FF"),
                                Command =
                                    $"UI_Stacks settings {type} {category} {(string.IsNullOrEmpty(search) ? page : 0)} {check.Value.ID}"
                            }
                        }, Layer + $".Panel.{ySwitch}");

                        ySwitch = ySwitch - height - margin;
                    }

                    #endregion

                    break;
                }
            }

            #region Pages

            container.Add(new CuiPanel
            {
                RectTransform =
                {
                    AnchorMin = "0.5 0", AnchorMax = "0.5 0",
                    OffsetMin = "-50 5", OffsetMax = "50 25"
                },
                Image =
                {
                    Color = HexToCuiColor("#161617")
                }
            }, Layer + ".Main", Layer + ".Pages");

            container.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "0 1",
                    OffsetMin = "-20 0",
                    OffsetMax = "0 0"
                },
                Text =
                {
                    Text = Msg(player, BtnBack),
                    Align = TextAnchor.MiddleCenter,
                    Font = "robotocondensed-regular.ttf",
                    FontSize = 10,
                    Color = "1 1 1 0.95"
                },
                Button =
                {
                    Color = HexToCuiColor("#4B68FF", 33),
                    Command = page != 0 ? $"UI_Stacks page {type} {category} {page - 1} {search}" : ""
                }
            }, Layer + ".Pages");

            container.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = "1 0",
                    AnchorMax = "1 1",
                    OffsetMin = "0 0",
                    OffsetMax = "20 0"
                },
                Text =
                {
                    Text = Msg(player, BtnNext),
                    Align = TextAnchor.MiddleCenter,
                    Font = "robotocondensed-regular.ttf",
                    FontSize = 10,
                    Color = "1 1 1 0.95"
                },
                Button =
                {
                    Color = HexToCuiColor("#4B68FF"),
                    Command = maxCount > (page + 1) * lines
                        ? $"UI_Stacks page {type} {category} {page + 1} {search}"
                        : ""
                }
            }, Layer + ".Pages");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text =
                {
                    Text = $"{page + 1}",
                    Align = TextAnchor.MiddleCenter,
                    Font = "robotocondensed-regular.ttf",
                    FontSize = 12,
                    Color = "1 1 1 0.5"
                }
            }, Layer + ".Pages");

            if (string.IsNullOrEmpty(search))
                container.Add(new CuiElement
                {
                    Parent = Layer + ".Pages",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            FontSize = 12,
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf",
                            Command = string.IsNullOrEmpty(search) || search.Length < 2
                                ? $"UI_Stacks page {type} {category} "
                                : $"UI_Stacks enter_page {type} {category} {search} ",
                            Color = "1 1 1 0.95",
                            CharsLimit = 32
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0", AnchorMax = "1 1"
                        }
                    }
                });

            #endregion

            #endregion

            CuiHelper.DestroyUi(player, Layer + ".Main");
            CuiHelper.AddUi(player, container);
        }

        private void SettingsUi(BasePlayer player, int type, int category, int page, int id, int enterValue = -1,
            bool first = false)
        {
            var nowValue = 0f;

            var container = new CuiElementContainer();

            #region Background

            if (first)
            {
                CuiHelper.DestroyUi(player, Layer);

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Image =
                    {
                        Color = "0 0 0 0.9",
                        Material = "assets/content/ui/uibackgroundblur.mat"
                    },
                    CursorEnabled = true
                }, "Overlay", Layer);

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = "" },
                    Button =
                    {
                        Color = "0 0 0 0",
                        Close = Layer
                    }
                }, Layer);
            }

            #endregion

            container.Add(new CuiPanel
            {
                RectTransform =
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
                    OffsetMin = "-300 -160",
                    OffsetMax = "300 160"
                },
                Image =
                {
                    Color = HexToCuiColor("#0E0E10")
                }
            }, Layer, Layer + ".Main");

            #region Header

            container.Add(new CuiPanel
            {
                RectTransform =
                {
                    AnchorMin = "0 1", AnchorMax = "1 1",
                    OffsetMin = "0 -50",
                    OffsetMax = "0 0"
                },
                Image = { Color = HexToCuiColor("#161617") }
            }, Layer + ".Main", Layer + ".Header");

            switch (type)
            {
                case 0:
                {
                    var item = _items.Find(x => x.ID == id);
                    if (item == null) return;

                    nowValue = item.CustomStackSize == 0 ? item.DefaultStackSize : item.CustomStackSize;

                    container.Add(new CuiElement
                    {
                        Parent = Layer + ".Header",
                        Components =
                        {
                            new CuiRawImageComponent { Png = ImageLibrary.Call<string>("GetImage", item.ShortName) },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0", AnchorMax = "0 0",
                                OffsetMin = "5 5", OffsetMax = "45 45"
                            }
                        }
                    });

                    container.Add(new CuiLabel
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 0", AnchorMax = "1 1",
                            OffsetMin = "50 0", OffsetMax = "0 0"
                        },
                        Text =
                        {
                            Text = $"{item.Name}",
                            Align = TextAnchor.MiddleLeft,
                            Font = "robotocondensed-regular.ttf",
                            FontSize = 12,
                            Color = "1 1 1 1"
                        }
                    }, Layer + ".Header");
                    break;
                }

                case 1:
                {
                    if (_containers.All(x => x.Value.ID != id))
                        return;

                    var cont = _containers.FirstOrDefault(x => x.Value.ID == id);

                    nowValue = cont.Value.Multiplier;

                    container.Add(new CuiElement
                    {
                        Parent = Layer + ".Header",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Png = ImageLibrary.Call<string>("GetImage",
                                    string.IsNullOrEmpty(cont.Value.Image) ? "NONE" : cont.Value.Image)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0", AnchorMax = "0 0",
                                OffsetMin = "5 5", OffsetMax = "45 45"
                            }
                        }
                    });

                    container.Add(new CuiLabel
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 0", AnchorMax = "1 1",
                            OffsetMin = "50 0", OffsetMax = "0 0"
                        },
                        Text =
                        {
                            Text = $"{cont.Key}",
                            Align = TextAnchor.MiddleLeft,
                            Font = "robotocondensed-regular.ttf",
                            FontSize = 12,
                            Color = "1 1 1 1"
                        }
                    }, Layer + ".Header");
                    break;
                }
            }

            container.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = "1 1", AnchorMax = "1 1",
                    OffsetMin = "-80 -37.5",
                    OffsetMax = "-55 -12.5"
                },
                Text =
                {
                    Text = Msg(player, BtnBack),
                    Align = TextAnchor.MiddleCenter,
                    Font = "robotocondensed-bold.ttf",
                    FontSize = 10,
                    Color = HexToCuiColor("#FFFFFF")
                },
                Button =
                {
                    Color = HexToCuiColor("#4B68FF"),
                    Command = $"UI_Stacks page {type} {category} {page}"
                }
            }, Layer + ".Header");

            container.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = "1 1", AnchorMax = "1 1",
                    OffsetMin = "-50 -37.5",
                    OffsetMax = "-25 -12.5"
                },
                Text =
                {
                    Text = Msg(player, CloseButton),
                    Align = TextAnchor.MiddleCenter,
                    Font = "robotocondensed-bold.ttf",
                    FontSize = 10,
                    Color = HexToCuiColor("#FFFFFF")
                },
                Button =
                {
                    Close = Layer,
                    Color = HexToCuiColor("#4B68FF")
                }
            }, Layer + ".Header");

            #endregion

            #region Now

            container.Add(new CuiPanel
            {
                RectTransform =
                {
                    AnchorMin = "0.5 1", AnchorMax = "0.5 1",
                    OffsetMin = "-150 -145",
                    OffsetMax = "150 -105"
                },
                Image =
                {
                    Color = HexToCuiColor("#161617")
                }
            }, Layer + ".Main", Layer + ".Value.Now");

            container.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = "0 1", AnchorMax = "1 1",
                    OffsetMin = "0 0", OffsetMax = "0 30"
                },
                Text =
                {
                    Text = Msg(player, type == 0 ? CurrentStack : CurrentMultiplier),
                    Align = TextAnchor.MiddleLeft,
                    Font = "robotocondensed-regular.ttf",
                    FontSize = 12,
                    Color = "1 1 1 0.4"
                }
            }, Layer + ".Value.Now");

            container.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = "0 0", AnchorMax = "1 1",
                    OffsetMin = "10 0", OffsetMax = "-10 0"
                },
                Text =
                {
                    Text = $"{nowValue}",
                    Align = TextAnchor.MiddleLeft,
                    Font = "robotocondensed-regular.ttf",
                    FontSize = 12,
                    Color = "1 1 1 0.2"
                }
            }, Layer + ".Value.Now");

            #endregion

            #region Input

            container.Add(new CuiPanel
            {
                RectTransform =
                {
                    AnchorMin = "0.5 1", AnchorMax = "0.5 1",
                    OffsetMin = "-150 -235",
                    OffsetMax = "150 -195"
                },
                Image =
                {
                    Color = HexToCuiColor("#161617")
                }
            }, Layer + ".Main", Layer + ".Value.Input");

            container.Add(new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = "0 1", AnchorMax = "1 1",
                    OffsetMin = "0 0", OffsetMax = "0 30"
                },
                Text =
                {
                    Text = Msg(player, type == 0 ? EnterStack : EnterMultiplier),
                    Align = TextAnchor.MiddleLeft,
                    Font = "robotocondensed-regular.ttf",
                    FontSize = 12,
                    Color = "1 1 1 0.4"
                }
            }, Layer + ".Value.Input");

            if (enterValue > 0)
                container.Add(new CuiLabel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 0", AnchorMax = "1 1",
                        OffsetMin = "10 0", OffsetMax = "-10 0"
                    },
                    Text =
                    {
                        Text = $"{enterValue}",
                        Align = TextAnchor.MiddleLeft,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 12,
                        Color = "1 1 1 0.2"
                    }
                }, Layer + ".Value.Input");

            container.Add(new CuiElement
            {
                Parent = Layer + ".Value.Input",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        FontSize = 12,
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-regular.ttf",
                        Color = "1 1 1 0.95",
                        CharsLimit = 32,
                        Command = $"UI_Stacks settings {type} {category} {page} {id} "
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0", AnchorMax = "1 1",
                        OffsetMin = "10 0", OffsetMax = "-10 0"
                    }
                }
            });

            #endregion

            #region Apply

            container.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = "0.5 1", AnchorMax = "0.5 1",
                    OffsetMin = "-150 -300",
                    OffsetMax = "150 -260"
                },
                Text =
                {
                    Text = Msg(player, AcceptTitle),
                    Align = TextAnchor.MiddleCenter,
                    Font = "robotocondensed-bold.ttf",
                    FontSize = 22,
                    Color = "1 1 1 1"
                },
                Button =
                {
                    Color = HexToCuiColor("#4B68FF"),
                    Command = enterValue > 0
                        ? $"UI_Stacks apply_settings {type} {category} {page} {id} {enterValue}"
                        : $"UI_Stacks page {type} {category} {page}"
                }
            }, Layer + ".Main");

            #endregion

            CuiHelper.DestroyUi(player, Layer + ".Main");
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Utils

        private static string HexToCuiColor(string hex, float alpha = 100)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#FFFFFF";

            var str = hex.Trim('#');
            if (str.Length != 6) throw new Exception(hex);
            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);

            return $"{(double)r / 255} {(double)g / 255} {(double)b / 255} {alpha / 100f}";
        }

        private void RegisterPermissions()
        {
            if (!permission.PermissionExists(AdminPerm))
                permission.RegisterPermission(AdminPerm, this);
        }

        private void LoadCategories()
        {
            var dict = new Dictionary<ItemCategory, List<ItemDefinition>>();

            ItemManager.itemList.ForEach(info =>
            {
                if (dict.ContainsKey(info.category))
                    dict[info.category].Add(info);
                else
                    dict.Add(info.category, new List<ItemDefinition>
                    {
                        info
                    });
            });

            foreach (var check in dict)
                _categories.Add(new CategoryInfo
                {
                    Title = check.Key.ToString(),
                    Items = _items.FindAll(x => check.Value.Exists(info => info.shortname == x.ShortName))
                });
        }

        private void LoadContainers(bool init)
        {
            foreach (var check in _constContainers.Where(check => !_containers.ContainsKey(check.Key)))
                _containers[check.Key] = new ContainerData
                {
                    Image = check.Value,
                    Multiplier = _config.DefaultContainerMultiplier
                };

            foreach (var check in _containers)
                check.Value.ID = GetContainerId();
        }

        private int GetContainerId()
        {
            var result = -1;

            do
            {
                var val = Random.Range(int.MinValue, int.MaxValue);
                if (_containers.All(x => x.Value.ID != val))
                    result = val;
            } while (result == -1);

            return result;
        }

        private void LoadItems()
        {
            ItemManager.itemList
                .FindAll(info => !_items.Exists(x => x.ShortName == info.shortname))
                .ForEach(info =>
                {
                    _items.Add(new ItemInfo
                    {
                        ShortName = info.shortname,
                        Name = info.displayName.english,
                        DefaultStackSize = info.stackable,
                        CustomStackSize = 0
                    });
                });

            ItemManager.itemList.ForEach(info => _defaultItemStackSize[info.shortname] = info.stackable);

            _items.FindAll(x => x.CustomStackSize != 0).ForEach(UpdateItemStack);

            _items.ForEach(item => item.ID = GetItemId());
        }

        private void UpdateItemStack(ItemInfo info)
        {
            var def = ItemManager.FindItemDefinition(info.ShortName);
            if (def == null) return;

            def.stackable = info.CustomStackSize;
        }

        private int GetItemId()
        {
            var result = -1;

            do
            {
                var val = Random.Range(int.MinValue, int.MaxValue);
                if (!_items.Exists(x => x.ID == val))
                    result = val;
            } while (result == -1);

            return result;
        }

        private void LoadImages()
        {
            if (!ImageLibrary)
            {
                PrintError("IMAGE LIBRARY IS NOT INSTALLED!");
            }
            else
            {
                var imagesList = new Dictionary<string, string>();

                var itemIcons = new List<KeyValuePair<string, ulong>>();

                _items.ForEach(item => itemIcons.Add(new KeyValuePair<string, ulong>(item.ShortName, 0)));

                foreach (var container in _containers.Values.Where(container => !string.IsNullOrEmpty(container.Image)
                    && !imagesList.ContainsKey(container.Image)))
                    imagesList.Add(container.Image, container.Image);

                if (itemIcons.Count > 0) ImageLibrary?.Call("LoadImageList", Title, itemIcons, null);

                ImageLibrary?.Call("ImportImageList", Title, imagesList, 0UL, true);
            }
        }

        private class ItemHelper
        {
            public static bool SplitMoveItem(Item item, int amount, ItemContainer targetContainer, int targetSlot)
            {
                var splitItem = item.SplitItem(amount);
                if (splitItem == null)
                    return false;

                if (!splitItem.MoveToContainer(targetContainer, targetSlot))
                {
                    item.amount += splitItem.amount;
                    splitItem.Remove();
                }

                return true;
            }

            public static bool SplitMoveItem(Item item, int amount, BasePlayer player)
            {
                return SplitMoveItem(item, amount, player.inventory);
            }

            public static bool SplitMoveItem(Item item, int amount, PlayerInventory inventory)
            {
                var splitItem = item.SplitItem(amount);
                if (splitItem == null) return false;
                if (!inventory.GiveItem(splitItem))
                {
                    item.amount += splitItem.amount;
                    splitItem.Remove();
                }

                return true;
            }

            public static void SwapItems(Item item1, Item item2)
            {
                var container1 = item1.parent;
                var container2 = item2.parent;
                var slot1 = item1.position;
                var slot2 = item2.position;
                item1.RemoveFromContainer();
                item2.RemoveFromContainer();
                item1.MoveToContainer(container2, slot2);
                item2.MoveToContainer(container1, slot1);
            }
        }

        #endregion

        #region Lang

        private const string
            SetStack = "SettedStack",
            NoPermission = "NoPermission",
            AcceptTitle = "AcceptTitle",
            EnterMultiplier = "EnterMultiplier",
            CurrentMultiplier = "CurrentMultiplier",
            EnterStack = "EnterStack",
            CurrentStack = "CurrentStack",
            CustomMultiplier = "CustomMultiplier",
            DefaultMultiplier = "DefaultMultiplier",
            SettingsTitle = "SettingsTitle",
            StackFormat = "StackFormat",
            CustomStack = "CustomStack",
            DefaultStack = "DefaultStack",
            AllTitle = "AllTitle",
            SearchTitle = "SearchTitle",
            BtnBack = "BtnBack",
            BtnNext = "BtnNext",
            ItemTitle = "ItemTitle",
            ContainerTitle = "ContainerTitle",
            CloseButton = "CloseButton",
            TitleMenu = "TitleMenu";

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [NoPermission] = "You don't have the required permission",
                [AllTitle] = "All",
                [CloseButton] = "✕",
                [TitleMenu] = "Stacks",
                [ContainerTitle] = "Container",
                [ItemTitle] = "Item",
                [BtnBack] = "◀",
                [BtnNext] = "▶",
                [SearchTitle] = "Search...",
                [DefaultStack] = "Default stack size",
                [CustomStack] = "Custom stack size",
                [StackFormat] = "x{0}",
                [SettingsTitle] = "Settings",
                [DefaultMultiplier] = "Default Multiplier: <color=white>x{0}</color>",
                [CustomMultiplier] = "Now Multiplier: <color=white>x{0}</color>",
                [CurrentStack] = "Current stack size:",
                [CurrentMultiplier] = "Current multiplier:",
                [EnterStack] = "Enter the new stack size:",
                [EnterMultiplier] = "Enter the multiplier:",
                [AcceptTitle] = "ACCEPT",
                [SetStack] = "You have set the stack size to {0} for the '{1}'"
            }, this);
        }

        private string Msg(string key, string userid = null, params object[] obj)
        {
            return string.Format(lang.GetMessage(key, this, userid), obj);
        }

        private string Msg(BasePlayer player, string key, params object[] obj)
        {
            return string.Format(lang.GetMessage(key, this, player.UserIDString), obj);
        }

        private void Reply(BasePlayer player, string key, params object[] obj)
        {
            SendReply(player, Msg(player, key, obj));
        }

        private void SendNotify(BasePlayer player, string key, int type, params object[] obj)
        {
            if (Notify && _config.UseNotify)
                Notify?.Call("SendNotify", player, type, Msg(player, key, obj));
            else
                Reply(player, key, obj);
        }

        #endregion
    }
}