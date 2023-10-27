using VLB;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("ExtractionRareMinerals", "qxzxf", "1.2.0")]
    [Description("During the extraction of resources, a rare stone may fall out, which, when melted or processed, gives more resources")]
    public class ExtractionRareMinerals : RustPlugin
    {
        #region Version
        class PluginVersion
        {
            public VersionNumber Configuration = new VersionNumber(1, 0, 1);
        }
        #endregion

        #region Configuration
        private PluginVersion version;
        private Configuration config;
        private class Configuration
        {
            public PluginVersion Version;
            public int MaxMineralsPerHit;
            public int? MaxStackable;
            public int TimeToSmelting;
            public string ItemShortName;
            public List<DefaultItem> Items;

            public static Configuration CreateDefault()
            {
                return new Configuration
                {
                    Version = new PluginVersion(),
                    MaxMineralsPerHit = 1,
                    MaxStackable = null,
                    TimeToSmelting = 30,
                    ItemShortName = "sticks",
                    Items = new List<DefaultItem>
                    {
                        // large.sulfur
                        new DefaultItem
                        {
                            ID = "large.sulfur",
                            SkinID = 2893225931,
                            Name = "Large Sulfur Crystal",
                            PermittedTool = new List<string> { "stone.pickaxe", "pickaxe", "hammer.salvaged", "icepick.salvaged", "jackhammer" },
                            ExtractionInfo = new List<DefaultExtractionInfo>
                            {
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "sulfur-ore",
                                    DropChance = 3.5f,
                                    Amount = 1
                                }
                            },
                            ItemAfterSmelting = new DefaultDropItem
                            {
                                ShortName = "sulfur",
                                MinAmount = 1000,
                                MaxAmount = 2500
                            },
                            PossibleItemsAfterRecycler = null
                        },
                        // large.metal
                        new DefaultItem
                        {
                            ID = "large.metal",
                            SkinID = 2893226249,
                            Name = "Large Metal Piece",
                            PermittedTool = new List<string> { "stone.pickaxe", "pickaxe", "hammer.salvaged", "icepick.salvaged", "jackhammer" },
                            ExtractionInfo = new List<DefaultExtractionInfo>
                            {
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "metal-ore",
                                    DropChance = 5,
                                    Amount = 1
                                }
                            },
                            ItemAfterSmelting = new DefaultDropItem
                            {
                                ShortName = "metal.fragments",
                                MinAmount = 1000,
                                MaxAmount = 2500
                            },
                            PossibleItemsAfterRecycler = null
                        },
                        // large.stone
                        new DefaultItem
                        {
                            ID = "large.stone",
                            SkinID = 2893226068,
                            Name = "Large Stone",
                            PermittedTool = new List<string> { "stone.pickaxe", "pickaxe", "hammer.salvaged", "icepick.salvaged", "jackhammer" },
                            ExtractionInfo = new List<DefaultExtractionInfo>
                            {
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "stone-ore",
                                    DropChance = 4,
                                    Amount = 1
                                }
                            },
                            ItemAfterSmelting = null,
                            PossibleItemsAfterRecycler = new List<DefaultDropItem>
                            {
                                new DefaultDropItem
                                {
                                    ShortName = "stones",
                                    MinAmount = 1500,
                                    MaxAmount = 3500
                                }
                            }
                        },
                        // emerald
                        new DefaultItem
                        {
                            ID = "emerald",
                            SkinID = 2893105244,
                            Name = "Emerald",
                            PermittedTool = new List<string> { "pickaxe", "hammer.salvaged", "icepick.salvaged", "jackhammer" },
                            ExtractionInfo = new List<DefaultExtractionInfo>
                            {
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "sulfur-ore",
                                    DropChance = 2.5f,
                                    Amount = 1
                                },
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "metal-ore",
                                    DropChance = 2.5f,
                                    Amount = 1
                                }
                            },
                            ItemAfterSmelting = null,
                            PossibleItemsAfterRecycler = new List<DefaultDropItem>
                            {
                                new DefaultDropItem
                                {
                                    ShortName = "wood",
                                    MinAmount = 2000,
                                    MaxAmount = 3500
                                }
                            }
                        },
                        // jade
                        new DefaultItem
                        {
                            ID = "jade",
                            SkinID = 2901473542,
                            Name = "Jade",
                            PermittedTool = new List<string> { "pickaxe", "hammer.salvaged", "icepick.salvaged", "jackhammer" },
                            ExtractionInfo = new List<DefaultExtractionInfo>
                            {
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "sulfur-ore",
                                    DropChance = 2.5f,
                                    Amount = 1
                                },
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "metal-ore",
                                    DropChance = 2.5f,
                                    Amount = 1
                                }
                            },
                            ItemAfterSmelting = null,
                            PossibleItemsAfterRecycler = new List<DefaultDropItem>
                            {
                                new DefaultDropItem
                                {
                                    ShortName = "rope",
                                    MinAmount = 3,
                                    MaxAmount = 15
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "cloth",
                                    MinAmount = 50,
                                    MaxAmount = 200
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "leather",
                                    MinAmount = 50,
                                    MaxAmount = 200
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "fat.animal",
                                    MinAmount = 70,
                                    MaxAmount = 300
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "lowgradefuel",
                                    MinAmount = 30,
                                    MaxAmount = 120
                                },
                            }
                        },
                        // tanzanite
                        new DefaultItem
                        {
                            ID = "tanzanite",
                            SkinID = 2901473839,
                            Name = "Tanzanite",
                            PermittedTool = new List<string> { "pickaxe", "hammer.salvaged", "icepick.salvaged", "jackhammer" },
                            ExtractionInfo = new List<DefaultExtractionInfo>
                            {
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "sulfur-ore",
                                    DropChance = 2.5f,
                                    Amount = 1
                                },
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "metal-ore",
                                    DropChance = 2.5f,
                                    Amount = 1
                                }
                            },
                            ItemAfterSmelting = null,
                            PossibleItemsAfterRecycler = new List<DefaultDropItem>
                            {
                                new DefaultDropItem
                                {
                                    ShortName = "chocholate",
                                    MinAmount = 3,
                                    MaxAmount = 15
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "can.beans",
                                    MinAmount = 3,
                                    MaxAmount = 15
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "can.tuna",
                                    MinAmount = 3,
                                    MaxAmount = 15
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "bandage",
                                    MinAmount = 10,
                                    MaxAmount = 30
                                }
                            }
                        },
                        // amethyst
                        new DefaultItem
                        {
                            ID = "amethyst",
                            SkinID = 2893105387,
                            Name = "Amethyst",
                            PermittedTool = new List<string> { "pickaxe", "hammer.salvaged", "icepick.salvaged", "jackhammer" },
                            ExtractionInfo = new List<DefaultExtractionInfo>
                            {
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "sulfur-ore",
                                    DropChance = 2,
                                    Amount = 1
                                },
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "metal-ore",
                                    DropChance = 2,
                                    Amount = 1
                                }
                            },
                            ItemAfterSmelting = null,
                            PossibleItemsAfterRecycler = new List<DefaultDropItem>
                            {
                                new DefaultDropItem
                                {
                                    ShortName = "largemedkit",
                                    MinAmount = 1,
                                    MaxAmount = 3
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "syringe.medical",
                                    MinAmount = 3,
                                    MaxAmount = 7
                                }
                            }
                        },
                        // topaz
                        new DefaultItem
                        {
                            ID = "topaz",
                            SkinID = 2893105314,
                            Name = "Topaz",
                            PermittedTool = new List<string> { "pickaxe", "hammer.salvaged", "icepick.salvaged", "jackhammer" },
                            ExtractionInfo = new List<DefaultExtractionInfo>
                            {
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "sulfur-ore",
                                    DropChance = 1.5f,
                                    Amount = 1
                                },
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "metal-ore",
                                    DropChance = 1.5f,
                                    Amount = 1
                                }
                            },
                            ItemAfterSmelting = null,
                            PossibleItemsAfterRecycler = new List<DefaultDropItem>
                            {
                                new DefaultDropItem
                                {
                                    ShortName = "scrap",
                                    MinAmount = 50,
                                    MaxAmount = 100
                                }
                            }
                        },
                        // musgravite
                        new DefaultItem
                        {
                            ID = "musgravite",
                            SkinID = 2901990088,
                            Name = "Musgravite",
                            PermittedTool = new List<string> { "pickaxe", "hammer.salvaged", "icepick.salvaged", "jackhammer" },
                            ExtractionInfo = new List<DefaultExtractionInfo>
                            {
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "sulfur-ore",
                                    DropChance = 1.5f,
                                    Amount = 1
                                },
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "metal-ore",
                                    DropChance = 1.5f,
                                    Amount = 1
                                }
                            },
                            ItemAfterSmelting = null,
                            PossibleItemsAfterRecycler = new List<DefaultDropItem>
                            {
                                new DefaultDropItem
                                {
                                    ShortName = "hazmatsuit",
                                    MinAmount = 1,
                                    MaxAmount = 1
                                }
                            }
                        },
                        // ruby
                        new DefaultItem
                        {
                            ID = "ruby",
                            SkinID = 2893105456,
                            Name = "Ruby",
                            PermittedTool = new List<string> { "pickaxe", "hammer.salvaged", "icepick.salvaged", "jackhammer" },
                            ExtractionInfo = new List<DefaultExtractionInfo>
                            {
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "sulfur-ore",
                                    DropChance = 1.5f,
                                    Amount = 1
                                },
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "metal-ore",
                                    DropChance = 1.5f,
                                    Amount = 1
                                }
                            },
                            ItemAfterSmelting = null,
                            PossibleItemsAfterRecycler = new List<DefaultDropItem>
                            {
                                new DefaultDropItem
                                {
                                    ShortName = "metalpipe",
                                    MinAmount = 2,
                                    MaxAmount = 8
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "gears",
                                    MinAmount = 2,
                                    MaxAmount = 8
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "metalblade",
                                    MinAmount = 2,
                                    MaxAmount = 8
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "metalspring",
                                    MinAmount = 2,
                                    MaxAmount = 8
                                }
                            }
                        },
                        // obsidian
                        new DefaultItem
                        {
                            ID = "obsidian",
                            SkinID = 2901473758,
                            Name = "Obsidian",
                            PermittedTool = new List<string> { "pickaxe", "hammer.salvaged", "icepick.salvaged", "jackhammer" },
                            ExtractionInfo = new List<DefaultExtractionInfo>
                            {
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "metal-ore",
                                    DropChance = 1.5f,
                                    Amount = 1
                                }
                            },
                            ItemAfterSmelting = new DefaultDropItem
                            {
                                ShortName = "metal.refined",
                                MinAmount = 10,
                                MaxAmount = 30
                            },
                            PossibleItemsAfterRecycler = null
                        },
                       // black-opal
                        new DefaultItem
                        {
                            ID = "black-opal",
                            SkinID = 2901473926,
                            Name = "Black Opal",
                            PermittedTool = new List<string> { "pickaxe", "hammer.salvaged", "icepick.salvaged", "jackhammer" },
                            ExtractionInfo = new List<DefaultExtractionInfo>
                            {
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "sulfur-ore",
                                    DropChance = 1.5f,
                                    Amount = 1
                                },
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "metal-ore",
                                    DropChance = 1.5f,
                                    Amount = 1
                                }
                            },
                            ItemAfterSmelting = new DefaultDropItem
                            {
                                ShortName = "charcoal",
                                MinAmount = 100,
                                MaxAmount = 1500
                            },
                            PossibleItemsAfterRecycler = null
                        },
                       // pink-diamond
                        new DefaultItem
                        {
                            ID = "pink-diamond",
                            SkinID = 2901473998,
                            Name = "Pink Diamond",
                            PermittedTool = new List<string> { "pickaxe", "hammer.salvaged", "icepick.salvaged", "jackhammer" },
                            ExtractionInfo = new List<DefaultExtractionInfo>
                            {
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "sulfur-ore",
                                    DropChance = 1.5f,
                                    Amount = 1
                                },
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "metal-ore",
                                    DropChance = 1.5f,
                                    Amount = 1
                                }
                            },
                            ItemAfterSmelting = null,
                            PossibleItemsAfterRecycler = new List<DefaultDropItem>
                            {
                                new DefaultDropItem
                                {
                                    ShortName = "riflebody",
                                    MinAmount = 1,
                                    MaxAmount = 2
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "semibody",
                                    MinAmount = 1,
                                    MaxAmount = 4
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "smgbody",
                                    MinAmount = 1,
                                    MaxAmount = 2
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "targeting.computer",
                                    MinAmount = 1,
                                    MaxAmount = 2
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "cctv.camera",
                                    MinAmount = 1,
                                    MaxAmount = 2
                                }
                            }
                        },
                        // diamond
                        new DefaultItem
                        {
                            ID = "diamond",
                            SkinID = 2893105180,
                            Name = "Diamond",
                            PermittedTool = new List<string> { "pickaxe", "hammer.salvaged", "icepick.salvaged", "jackhammer" },
                            ExtractionInfo = new List<DefaultExtractionInfo>
                            {
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "sulfur-ore",
                                    DropChance = 1.5f,
                                    Amount = 1
                                },
                                new DefaultExtractionInfo
                                {
                                    PrefabShortName = "metal-ore",
                                    DropChance = 1.5f,
                                    Amount = 1
                                }
                            },
                            ItemAfterSmelting = null,
                            PossibleItemsAfterRecycler = new List<DefaultDropItem>
                            {
                                new DefaultDropItem
                                {
                                    ShortName = "pistol.semiauto",
                                    MinAmount = 1,
                                    MaxAmount = 1
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "rifle.semiauto",
                                    MinAmount = 1,
                                    MaxAmount = 1
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "pistol.python",
                                    MinAmount = 1,
                                    MaxAmount = 1
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "pistol.revolver",
                                    MinAmount = 1,
                                    MaxAmount = 1
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "smg.thompson",
                                    MinAmount = 1,
                                    MaxAmount = 1
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "shotgun.pump",
                                    MinAmount = 1,
                                    MaxAmount = 1
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "shotgun.double",
                                    MinAmount = 1,
                                    MaxAmount = 1
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "shotgun.waterpipe",
                                    MinAmount = 1,
                                    MaxAmount = 1
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "smg.mp5",
                                    MinAmount = 1,
                                    MaxAmount = 1
                                },
                                new DefaultDropItem
                                {
                                    ShortName = "pistol.nailgun",
                                    MinAmount = 1,
                                    MaxAmount = 1
                                }
                            }
                        },
                    },
                };
            }
        }

        private class DefaultItem
        {
            public string ID;
            public ulong SkinID;
            public string Name;
            public List<string> PermittedTool = new List<string>();
            public List<DefaultExtractionInfo> ExtractionInfo = new List<DefaultExtractionInfo>();
            public DefaultDropItem ItemAfterSmelting;
            public List<DefaultDropItem> PossibleItemsAfterRecycler;
        }

        private class DefaultExtractionInfo
        {
            public string PrefabShortName;
            public float DropChance;
            public int Amount;
        }

        private class DefaultDropItem
        {
            public string ShortName;
            public int MinAmount;
            public int MaxAmount;
        }

        private readonly string smeltingPermission = "extractionrareminerals.allowSmelting";
        private readonly string recyclerPermission = "extractionrareminerals.allowRecycler";
        #endregion

        #region Language
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["SmeltingNotPermission"] = "You don't have the permission to melt it down.",
                ["RecyclerNotPermission"] = "You don't have the permission to recycle this."
            }, this, "en");
        }
        #endregion

        #region Init
        private void Loaded()
        {
            LoadConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            version = new PluginVersion();
            config = Config.ReadObject<Configuration>();
            if (config.Version == null || config.Version.Configuration < version.Configuration)
            {
                PrintError("The config has been updated! You should delete the old one and restart the plugin!");
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = Configuration.CreateDefault();
        }

        void OnServerInitialized()
        {
            CreateBaseItem();
            permission.RegisterPermission(smeltingPermission, this);
            permission.RegisterPermission(recyclerPermission, this);
        }
        #endregion

        #region Commands
        [ConsoleCommand("give.rare.mineral")]
        private void GiveConsoleCommand(ConsoleSystem.Arg args)
        {
            if (args.Connection != null)
            {
                BasePlayer player = args.Player();
                if (player != null && !player.IsAdmin) return;
                if (args.Args == null || args.Args.Length < 1)
                {
                    PrintToConsole(player, "Command invalid. Format: give.rare.mineral PLAYER ID AMOUNT");
                    return;
                }
                if (args.Args.Length > 1)
                {
                    BasePlayer target = BasePlayer.Find(args.Args[0]);
                    string id = args.Args[1];
                    int amount = 1;
                    if (args.Args.Length > 2)
                    {
                        amount = System.Int32.Parse(args.Args[2]);
                    }
                    if (target != null)
                    {
                        Item item = GetItemByID(id, amount);
                        if (item != null)
                        {
                            target.GiveItem(item);
                            PrintToConsole(player, $"The player {target.displayName} has been successfully issued items");
                        }
                        else
                        {
                            PrintToConsole(player, "No such ID has been found");
                        }
                    }
                }
                else
                {
                    PrintToConsole(player, "You have not entered the ID of the subject");
                    return;
                }
            }
            else
            {
                if (args.Args.Length > 1)
                {
                    BasePlayer target = BasePlayer.Find(args.Args[0]);
                    string id = args.Args[1];
                    int amount = 1;
                    if (args.Args.Length > 2)
                    {
                        amount = System.Int32.Parse(args.Args[2]);
                    }
                    if (target != null)
                    {
                        Item item = GetItemByID(id, amount);
                        if (item != null)
                        {
                            target.GiveItem(item);
                            Puts($"The player {target.displayName} has been successfully issued items");
                        }
                        else
                        {
                            Puts("No such ID has been found");
                        }
                    }
                }
            }
        }

        private Item GetItemByID(string id, int amount)
        {
            DefaultItem defItem = config.Items.FirstOrDefault(a => a.ID == id);
            if (defItem == null)
            {
                return null;
            }
            Item item = ItemManager.CreateByName(config.ItemShortName, amount, defItem.SkinID);
            item.name = defItem.Name;
            return item;
        }
        #endregion

        #region Logic
        private void CreateBaseItem()
        {
            ItemDefinition itemInfo = ItemManager.FindItemDefinition(config.ItemShortName);
            if (config.MaxStackable != null)
            {
                itemInfo.stackable = (int)config.MaxStackable;
            }
            ItemModCookable cookable = itemInfo.gameObject.GetOrAddComponent<ItemModCookable>();
            if (itemInfo.itemMods.FirstOrDefault(a => a == cookable) == null)
            {
                itemInfo.itemMods = itemInfo.itemMods.Concat(new ItemMod[] { cookable }).ToArray();
            }
            cookable.highTemp = 1200;
            cookable.lowTemp = 800;
            cookable.cookTime = config.TimeToSmelting;
            cookable.ModInit();
        }

        object CanCombineDroppedItem(DroppedItem dItem, DroppedItem dTargetItem)
        {
            Item item = dItem.GetItem();
            Item targetItem = dTargetItem.GetItem();
            if (item != null && targetItem != null)
            {
                if (IsCustomItem(item) || IsCustomItem(targetItem))
                {
                    return false;
                }
            }
            return null;
        }

        object CanRecycle(Recycler recycler, Item item)
        {
            if (IsCustomItem(item))
            {
                return CanRecyclerItem(item);
            }
            return null;
        }

        object OnItemRemove(Item item)
        {
            if (item == null || item.info == null)
            {
                return null;
            }
            ItemContainer container = item.parent;
            if (container == null || container.entityOwner == null)
            {
                return null;
            }
            DefaultItem defItem = FindItem(item);
            if (defItem != null && container.entityOwner is BaseOven)
            {
                BaseOven oven = container.entityOwner as BaseOven;
                Item createdItem = GetRandomAmountSmeltedItem(defItem);
                int slotIndex = GetSlotIndexMoveToOutput(oven, container, createdItem);
                if (slotIndex == -1 || !createdItem.MoveToContainer(container, slotIndex))
                {
                    oven.StopCooking();
                    return false;
                }
            }
            return null;
        }

        private object OnItemRecycle(Item item, Recycler recycler)
        {
            if (recycler == null || item == null) return null;
            DefaultItem defItem = FindItem(item);
            if (defItem != null)
            {
                item.UseItem(1);
                Item createdItem = GetRandomRecycledItem(defItem);
                if (!recycler.MoveItemToOutput(createdItem))
                {
                    recycler.StopRecycling();
                    return false;
                }
                return true;
            }
            return null;
        }

        private int GetSlotIndexMoveToOutput(BaseEntity entity, ItemContainer container, Item item)
        {
            if (entity.ShortPrefabName == "furnace")
            {
                for (int i = 3; i < 6; i++)
                {
                    Item slot = container.GetSlot(i);
                    if (slot == null || (slot.info.shortname == item.info.shortname && (slot.info.stackable - slot.amount) >= item.amount))
                    {
                        return i;
                    }
                }
            }
            else if (entity.ShortPrefabName == "furnace.large")
            {
                for (int i = 7; i < 17; i++)
                {
                    Item slot = container.GetSlot(i);
                    if (slot == null || (slot.info.shortname == item.info.shortname && (slot.info.stackable - slot.amount) >= item.amount))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        object OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            BaseEntity entity = info.HitEntity;
            if (entity == null || attacker == null)
            {
                return null;
            }
            Item activeItem = attacker.GetActiveItem();
            if (activeItem == null)
            {
                return null;
            }
            CastCustomItem(entity, attacker, activeItem);
            return null;
        }

        private void CastCustomItem(BaseEntity hitEntity, BasePlayer attacker, Item activeItem)
        {
            int count = 0;
            List<DefaultItem> items = config.Items.FindAll(a => a.ExtractionInfo.FirstOrDefault(b => b.PrefabShortName == hitEntity.ShortPrefabName) != null);
            items = Shuffle(items);
            foreach (var item in items)
            {
                if (count >= config.MaxMineralsPerHit)
                {
                    break;
                }
                if (item.PermittedTool != null && !item.PermittedTool.Contains(activeItem.info.shortname))
                {
                    return;
                }
                DefaultExtractionInfo info = item.ExtractionInfo.FirstOrDefault(a => a.PrefabShortName == hitEntity.ShortPrefabName);
                if (TryPull(info.DropChance))
                {
                    Item addedItem = ItemManager.CreateByName(config.ItemShortName, info.Amount, item.SkinID);
                    addedItem.name = item.Name;
                    attacker.GiveItem(addedItem, BaseEntity.GiveItemReason.PickedUp);
                    count += info.Amount;
                }
            }
        }

        ItemContainer.CanAcceptResult? CanAcceptItem(ItemContainer container, Item item, int targetPos)
        {
            if (container.entityOwner == null || item == null)
            {
                return null;
            }
            if (container.entityOwner is BaseOven && IsCustomItem(item))
            {
                if (CanCookableItem(item))
                {
                    BasePlayer player = item.GetOwnerPlayer();
                    if (player != null && player.IPlayer != null && !player.IPlayer.HasPermission(smeltingPermission))
                    {
                        PlayerReply(player.IPlayer, GetLang("SmeltingNotPermission"));
                        return ItemContainer.CanAcceptResult.CannotAccept;
                    }
                    Item slot = container.GetSlot(targetPos);
                    if (slot == null)
                    {
                        if (item.amount > 1)
                        {
                            Item movedItem = item.SplitItem(1);
                            movedItem.name = item.name;
                            movedItem.MoveToContainer(container, targetPos);
                        }
                        else
                        {
                            return ItemContainer.CanAcceptResult.CanAccept;
                        }
                    }
                }
                return ItemContainer.CanAcceptResult.CannotAccept;
            }
            else if (container.entityOwner is Recycler && IsCustomItem(item))
            {
                BasePlayer player = item.GetOwnerPlayer();
                if (player != null && player.IPlayer != null && !player.IPlayer.HasPermission(recyclerPermission))
                {
                    PlayerReply(player.IPlayer, GetLang("RecyclerNotPermission"));
                    return ItemContainer.CanAcceptResult.CannotAccept;
                }
                if (!CanRecyclerItem(item))
                {
                    return ItemContainer.CanAcceptResult.CannotAccept;
                }
            }
            return null;
        }

        private Item GetRandomAmountSmeltedItem(DefaultItem item)
        {
            int randomAmount = UnityEngine.Random.Range(item.ItemAfterSmelting.MinAmount, item.ItemAfterSmelting.MaxAmount + 1);
            return ItemManager.CreateByName(item.ItemAfterSmelting.ShortName, randomAmount);
        }

        private Item GetRandomRecycledItem(DefaultItem item)
        {
            int randomIndex = UnityEngine.Random.Range(0, item.PossibleItemsAfterRecycler.Count);
            DefaultDropItem recycledItem = item.PossibleItemsAfterRecycler[randomIndex];
            int randomAmount = UnityEngine.Random.Range(recycledItem.MinAmount, recycledItem.MaxAmount + 1);
            return ItemManager.CreateByName(recycledItem.ShortName, randomAmount);
        }

        private DefaultItem FindItem(Item item)
        {
            return config.Items.FirstOrDefault(a => config.ItemShortName == item.info.shortname && a.SkinID == item.skin);
        }

        private bool IsCustomItem(Item item)
        {
            return FindItem(item) != null;
        }

        private bool CanCookableItem(Item item)
        {
            DefaultItem defItem = FindItem(item);
            return defItem != null && defItem.ItemAfterSmelting != null;
        }
         
        private bool CanRecyclerItem(Item item)
        {
            DefaultItem defItem = FindItem(item);
            return defItem != null && defItem.PossibleItemsAfterRecycler != null;
        }
        #endregion

        #region Utils
        private readonly Dictionary<string, long> replyDelay = new Dictionary<string, long>();

        private void PlayerReply(IPlayer player, string messageId)
        {
            if (player == null)
            {
                return;
            }
            long lastUnix;
            System.DateTimeOffset now = System.DateTime.UtcNow;
            var currentUnix = now.ToUnixTimeSeconds();
            if (replyDelay.TryGetValue(player.Id, out lastUnix))
            {
                if (currentUnix < lastUnix)
                {
                    return;
                }
            }
            replyDelay[player.Id] = currentUnix + 2;
            player.Reply(GetLang(messageId));
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        string GetLang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        private bool TryPull(float chance)
        {
            var random = new System.Random();
            double percent = random.NextDouble() * 100;
            if (percent < chance)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private List<T> Shuffle<T>(List<T> list)
        {
            var rng = new System.Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            return list;
        }
        #endregion
    }
}