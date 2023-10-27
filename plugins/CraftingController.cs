using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;


namespace Oxide.Plugins
{
    [Info("Crafting Controller", "Whispers88", "3.2.9")]
    [Description("Allows you to modify the time spend crafting and which items can be crafted")]

    //Credits to previous authors Nivex & Mughisi
    public class CraftingController : RustPlugin
    {
        #region Config
        private Configuration config;
        private static Dictionary<string, float> defaultsetup = new Dictionary<string, float>();
        public class CraftingData
        {
            public bool canCraft;
            public bool canResearch;
            public bool useCrafteRateMultiplier;
            public float craftTime;
            public int workbenchLevel;
            public ulong defaultskinid;

            public CraftingData()
            {
                canCraft = true;
                canResearch = true;
                useCrafteRateMultiplier = true;
                craftTime = 0;
                workbenchLevel = -1;
                defaultskinid = 0;
            }
        }

        public class Configuration
        {

            [JsonProperty("Default crafting rate percentage")]
            public float CraftingRate = 50;

            [JsonProperty("Save commands to config (save config changes via command to the configuration)")]
            public bool SaveCommands = true;

            [JsonProperty("Simple Mode (disables: instant bulk craft, skin options and full inventory checks for better performance)")]
            public bool SimpleMode = false;

            [JsonProperty("Allow crafting when inventory is full")]
            public bool FullInventory = false;

            [JsonProperty("Complete crafting on server shut down")]
            public bool CompleteCrafting = false;

            [JsonProperty("Craft items with random skins if not already skinned")]
            public bool RandomSkins = false;

            [JsonProperty("Show Crafting Notes")]
            public bool ShowCraftNotes = false;

            [JsonProperty("Crafting rate bonus mulitplier (apply oxide perms for additional mulitpliers")]
            public Dictionary<string, float> BonusMultiplier = new Dictionary<string, float>() { { "vip1", 90 }, { "vip2", 80 } };

            [JsonProperty("Advanced Crafting Options")]
            public Dictionary<string, CraftingData> CraftingOptions = new Dictionary<string, CraftingData>();

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new JsonException();
                }
            }
            catch
            {
                Puts($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Puts($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }

        #endregion Config

        #region Init
        private const string perminstantbulkcraft = "craftingcontroller.instantbulkcraft";
        private const string permblockitems = "craftingcontroller.blockitems";
        private const string permitemrate = "craftingcontroller.itemrate";
        private const string permcraftingrate = "craftingcontroller.craftingrate";
        private const string permsetbenchlvl = "craftingcontroller.setbenchlvl";
        private const string permsetskins = "craftingcontroller.setskins";

        private List<string> permissions = new List<string> { perminstantbulkcraft, permblockitems, permitemrate, permcraftingrate, permsetbenchlvl, permsetskins };
        private List<string> permissionsBonusMultiplier = new List<string>();
        private List<string> commands = new List<string> { nameof(CommandCraftingRate), nameof(CommandCraftTime), nameof(CommandBlockItem), nameof(CommandUnblockItem), nameof(CommandSetDefaultSkin), nameof(CommandWorkbenchLVL) };
        private void OnServerInitialized()
        {
            ItemManager.bpList.ForEach(bp => defaultsetup[bp.name] = bp.time);
            foreach (var key in config.BonusMultiplier.Keys)
            {
                permissionsBonusMultiplier.Add("craftingcontroller." + key);
            }
            //register permissions
            permissions.ForEach(perm => permission.RegisterPermission(perm, this));
            permissionsBonusMultiplier.ForEach(perm => permission.RegisterPermission(perm, this));
            //register commands
            commands.ForEach(command => AddLocalizedCommand(command));

            if (config.SimpleMode)
            {
                Unsubscribe("OnItemCraft");
                Unsubscribe("OnItemCraftFinished");
                Unsubscribe("OnItemCraftCancelled");
            }
            ItemManager.bpList.ForEach(item => {
                if (!config.CraftingOptions.ContainsKey(item.targetItem.shortname))
                    config.CraftingOptions.Add(item.targetItem.shortname, new CraftingData() { craftTime = item.time, workbenchLevel = item.workbenchLevelRequired });
            });


            SaveConfig();
            UpdateCraftingRate();
        }

        private void Unload()
        {
            ItemManager.bpList.ForEach(bp => { if (defaultsetup.ContainsKey(bp.name)) bp.time = defaultsetup[bp.name]; });
        }

        #endregion Init

        #region Localization
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoInvSpace"] = "You don't have enough room to craft this item!",
                ["NoPerms"] = "You don't have permission to use this command.",
                ["CannotFindItem"] = "Cannot find item {0}.",
                ["ItemBlocked"] = "{0} has been blocked from crafting.",
                ["ItemUnblocked"] = "{0} has been unblocked from crafting.",
                ["NeedsAdvancedOptions"] = "You need to enable advanced crafting options in your config to use this.",
                ["WrongNumberInput"] = "Your input needs to be a number.",
                ["ItemCraftTimeSet"] = "{0} craft time set to {1} seconds",
                ["WorkbenchLevelSet"] = "{0} workbench level set to {1}",
                ["CurrentCraftinRate"] = "The current crafting rate is {0 }%",
                ["CraftingRateUpdated"] = "The crafting rate was updated to {0} %",
                ["CraftTime2Args"] = "This command needs two arguments in the format /crafttime item.shortname timetocraft",
                ["BlockItem1Args"] = "This command needs one argument in the format /blockitem item.shortname",
                ["UnblockItem1Args"] = "This command needs one argument in the format /unblockitem item.shortname",
                ["WorckBenchLvl2Args"] = "This command needs two arguments in the format /benchlvl item.shortname workbenchlvl",
                ["BenchLevelInput"] = "The work bench level must be between 0 and 3",
                ["SetSkin2Args"] = "This command needs one argument in the format /setcraftskin item.shortname skinworkshopid",
                ["SkinSet"] = "The default skin for {0} was set to {1}",
                ["CraftTimeCheck"] = "The craft time of this item is {0}",
                //Commands
                ["CommandCraftingRate"] = "craftrate",
                ["CommandCraftTime"] = "crafttime",
                ["CommandBlockItem"] = "blockitem",
                ["CommandUnblockItem"] = "unblockitem",
                ["CommandWorkbenchLVL"] = "benchlvl",
                ["CommandSetDefaultSkin"] = "setcraftskin"

            }, this);
        }

        #endregion Localization

        #region Commands
        private void CommandCraftingRate(IPlayer iplayer, string command, string[] args)
        {
            if (!HasPerm(iplayer.Id, permitemrate))
            {
                Message(iplayer, "NoPerms");
                return;
            }
            int craftingrate;
            if (args.Length == 0)
            {
                Message(iplayer, "CurrentCraftinRate", config.CraftingRate);
                return;
            }
            if (!int.TryParse(args[0], out craftingrate))
            {
                Message(iplayer, "WrongNumberInput");
                return;
            }
            if (craftingrate < 0) craftingrate = 0;
            config.CraftingRate = craftingrate;
            UpdateCraftingRate();
            Message(iplayer, "CraftingRateUpdated", config.CraftingRate);
            if (config.SaveCommands) SaveConfig();

        }

        private void CommandCraftTime(IPlayer iplayer, string command, string[] args)
        {
            if (!HasPerm(iplayer.Id, permitemrate))
            {
                Message(iplayer, "NoPerms");
                return;
            }
            if (args.Length == 1)
            {
                var itemcheck = FindItem(args[0]);
                if (itemcheck)
                {
                    Message(iplayer, "CraftTimeCheck", itemcheck.Blueprint.time);
                    return;
                }
            }
            if (args.Length < 2)
            {
                Message(iplayer, "CraftTime2Args");
                return;
            }
            var setitem = FindItem(args[0]);
            if (!setitem)
            {
                Message(iplayer, "CannotFindItem", args[0]);
                return;
            }
            if (args[1].ToLower() == "default")
            {
                config.CraftingOptions[setitem.shortname].useCrafteRateMultiplier = true;
                config.CraftingOptions[setitem.shortname].craftTime = (defaultsetup[setitem.Blueprint.name] * (config.CraftingRate / 100));
                Message(iplayer, "ItemCraftTimeSet", setitem.shortname, (setitem.Blueprint.time).ToString());
                if (config.SaveCommands) SaveConfig();
                return;
            }
            int crafttime;
            if (!int.TryParse(args[1], out crafttime))
            {
                Message(iplayer, "WrongNumberInput");
                return;
            }
            config.CraftingOptions[setitem.shortname].craftTime = crafttime;
            config.CraftingOptions[setitem.shortname].useCrafteRateMultiplier = false;
            ItemBlueprint bp = ItemManager.itemDictionaryByName[setitem.shortname].Blueprint;
            bp.time = crafttime;
            Message(iplayer, "ItemCraftTimeSet", setitem.shortname, crafttime.ToString());
            if (config.SaveCommands) SaveConfig();
        }

        private void CommandBlockItem(IPlayer iplayer, string command, string[] args)
        {
            if (!HasPerm(iplayer.Id, permblockitems))
            {
                Message(iplayer, "NoPerms");
                return;
            }
            if (args.Length < 1)
            {
                Message(iplayer, "BlockItem1Args");
                return;
            }
            var blockitem = FindItem(args[0]);
            if (!blockitem)
            {
                Message(iplayer, "CannotFindItem", args[0]);
                return;
            }
            config.CraftingOptions[blockitem.shortname].canCraft = false;
            config.CraftingOptions[blockitem.shortname].canResearch = false;
            ItemBlueprint bp = ItemManager.itemDictionaryByName[blockitem.shortname].Blueprint;
            bp.userCraftable = false;
            bp.isResearchable = false;
            Message(iplayer, "ItemBlocked", blockitem.shortname);
            if (config.SaveCommands) SaveConfig();
        }

        private void CommandUnblockItem(IPlayer iplayer, string command, string[] args)
        {
            if (!HasPerm(iplayer.Id, permblockitems))
            {
                Message(iplayer, "NoPerms");
                return;
            }
            if (args.Length < 1)
            {
                Message(iplayer, "UnbockItem1Args");
                return;
            }
            var blockitem = FindItem(args[0]);
            if (!blockitem)
            {
                Message(iplayer, "CannotFindItem", args[0]);
                return;
            }
            config.CraftingOptions[blockitem.shortname].canCraft = true;
            config.CraftingOptions[blockitem.shortname].canResearch = true;
            ItemBlueprint bp = ItemManager.itemDictionaryByName[blockitem.shortname].Blueprint;
            bp.userCraftable = true;
            bp.isResearchable = true;
            Message(iplayer, "ItemUnblocked", blockitem.shortname);
            if (config.SaveCommands) SaveConfig();
        }
        private void CommandWorkbenchLVL(IPlayer iplayer, string command, string[] args)
        {
            if (!HasPerm(iplayer.Id, permsetbenchlvl))
            {
                Message(iplayer, "NoPerms");
                return;
            }
            if (args.Length < 2)
            {
                Message(iplayer, "WorckBenchLvl2Args");
                return;
            }
            var item = FindItem(args[0]);
            if (!item)
            {
                Message(iplayer, "CannotFindItem", args[0]);
                return;
            }
            int benchlvl;
            if (!int.TryParse(args[1], out benchlvl))
            {
                Message(iplayer, "WrongNumberInput");
                return;
            }
            if (benchlvl < 0 || benchlvl > 3)
            {
                Message(iplayer, "BenchLevelInput");
                return;
            }
            config.CraftingOptions[item.shortname].workbenchLevel = benchlvl;
            ItemBlueprint bp = ItemManager.itemDictionaryByName[item.shortname].Blueprint;
            bp.workbenchLevelRequired = benchlvl;
            Message(iplayer, "WorkbenchLevelSet", item.shortname, benchlvl.ToString());
            if (config.SaveCommands) SaveConfig();
        }
        private void CommandSetDefaultSkin(IPlayer iplayer, string command, string[] args)
        {
            if (!HasPerm(iplayer.Id, permsetskins))
            {
                Message(iplayer, "NoPerms");
                return;
            }
            if (args.Length < 2)
            {
                Message(iplayer, "SetSkin2Args");
                return;
            }
            var setitem = FindItem(args[0]);
            if (!setitem)
            {
                Message(iplayer, "CannotFindItem", args[0]);
                return;
            }
            ulong skinid;
            if (!ulong.TryParse(args[1], out skinid))
            {
                Message(iplayer, "WrongNumberInput", args);
                return;
            }
            config.CraftingOptions[setitem.shortname].defaultskinid = skinid;
            Message(iplayer, "SkinSet", setitem.shortname, skinid.ToString());
            if (config.SaveCommands) SaveConfig();
        }
        #endregion Commands

        #region Methods
        private void UpdateCraftingRate()
        {
            foreach (var bp in ItemManager.bpList)
            {
                CraftingData data;
                if (!config.CraftingOptions.TryGetValue(bp.targetItem.shortname, out data)) continue;
                bp.userCraftable = data.canCraft;
                bp.isResearchable = data.canResearch;
                if (config.CraftingRate == 0f)
                    bp.time = 0f;
                else if (!data.useCrafteRateMultiplier)
                    bp.time = data.craftTime;
                else
                    bp.time *= (float)(config.CraftingRate / 100);

                if (bp.workbenchLevelRequired > 4) data.workbenchLevel = 3;
                if (bp.workbenchLevelRequired > 0)
                    bp.workbenchLevelRequired = data.workbenchLevel;
            }
        }

        private void InstantBulkCraft(BasePlayer player, ItemCraftTask task, ItemDefinition item, List<int> stacks, int craftSkin, ulong skin)
        {
            if (skin == 0uL)
            {
                skin = ItemDefinition.FindSkin(item.itemid, craftSkin);
            }
            foreach (var stack in stacks)
            {
                var itemtogive = ItemManager.Create(item, stack, craftSkin != 0 && skin == 0uL ? (ulong)craftSkin : skin);
                var held = itemtogive.GetHeldEntity();
                if (held != null)
                {
                    held.skinID = skin == 0uL ? (ulong)craftSkin : skin;
                    held.SendNetworkUpdate();
                }
                player.GiveItem(itemtogive);
                if (config.ShowCraftNotes) player.Command(string.Concat(new object[] { "note.inv ", item.itemid, " ", stack }), new object[0]);
                Interface.CallHook("OnItemCraftFinished", task, itemtogive, player.inventory.crafting);
            }
        }

        private static void CompleteCrafting(BasePlayer player)
        {
            if (player.inventory.crafting.queue.Count == 0) return;
            player.inventory.crafting.FinishCrafting(player.inventory.crafting.queue.First.Value);
            player.inventory.crafting.queue.RemoveFirst();
        }
        private static void CancelAllCrafting(BasePlayer player)
        {
            ItemCrafter crafter = player.inventory.crafting;
            crafter.CancelAll(true);
        }

        #endregion Methods

        #region Hooks
        private Dictionary<ItemCraftTask, ulong> skinupdate = new Dictionary<ItemCraftTask, ulong>();
        private object OnItemCraft(ItemCraftTask task, BasePlayer player)
        {
            var target = task.blueprint.targetItem;
            if (task.instanceData?.dataInt != null) return null;
            var stacks = GetStacks(target, task.amount * task.blueprint.amountToCreate);
            ulong defaultskin = 0uL;
            int freeslots = FreeSlots(player);
            bool f = false;
            if (!config.FullInventory && stacks.Count >= freeslots)
            {
                f = true;
                int space = FreeSpace(player, target);
                if (space < 1)
                {
                    ReturnCraft(task, player);
                    return false;
                }
                int taskamt = task.amount * task.blueprint.amountToCreate;
                for (int i = 0; i < 20 && taskamt > space; i++)
                {
                    var oldtaskamt = taskamt;
                    taskamt = space;
                    foreach (var item in task.takenItems)
                    {
                        var itemtogive = item;
                        double fraction = (double)taskamt / (double)oldtaskamt;
                        int amttogive = (int)(item.amount * (1 - fraction));
                        if (amttogive <= 1)
                        {
                            ReturnCraft(task, player);
                            return false;
                        }
                        itemtogive = ItemManager.Create(item.info, amttogive, 0uL);
                        item.amount -= amttogive;

                        player.GiveItem(itemtogive);
                    }
                    space -= (freeslots - FreeSlots(player)) * target.stackable;
                    if (space < 1 || taskamt < 1)
                    {
                        ReturnCraft(task, player);
                        return false;
                    }
                    if (taskamt <= space) break;

                }
                task.amount = (int)(taskamt / task.blueprint.amountToCreate);
            }


            if (task.skinID == 0)
            {
                CraftingData data;
                if (config.CraftingOptions.TryGetValue(target.shortname, out data))
                {
                    defaultskin = data.defaultskinid;
                }

                if (config.RandomSkins && defaultskin == 0)
                {
                    List<ulong> skins = GetSkins(ItemManager.FindItemDefinition(target.itemid));
                    defaultskin = skins.GetRandom();
                }

                if (defaultskin > 999999)
                    skinupdate[task] = defaultskin;
                else
                    task.skinID = (int)defaultskin;
            }

            float bonusperm_time = 100;
            foreach (var bonusperm in permissionsBonusMultiplier)
            {
                if (!HasPerm(player.UserIDString, bonusperm)) continue;
                if (bonusperm_time > (float)config.BonusMultiplier[bonusperm.Split('.')[1]]) continue;
                bonusperm_time = (float)config.BonusMultiplier[bonusperm.Split('.')[1]];
                task.blueprint = UnityEngine.Object.Instantiate(task.blueprint);
                task.blueprint.time *= bonusperm_time / 100;
                break;
            }

            if (task.blueprint.time == 0f || HasPerm(player.UserIDString, perminstantbulkcraft))
            {
                skinupdate.Remove(task);
                if (f)
                    stacks = GetStacks(target, task.amount * task.blueprint.amountToCreate);
                InstantBulkCraft(player, task, target, stacks, task.skinID, defaultskin);
                task.cancelled = true;
                return false;
            }
            return null;
        }

        private void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            ulong skinid;
            if (!skinupdate.TryGetValue(task, out skinid)) return;

            item.skin = skinid;
            var held = item.GetHeldEntity();

            if (held != null)
            {
                held.skinID = skinid;
                held.SendNetworkUpdate();
            }
            if (task.amount == 0)
                skinupdate.Remove(task);
        }

        void OnItemCraftCancelled(ItemCraftTask task)
        {
            ulong skinid;
            if (!skinupdate.TryGetValue(task, out skinid)) return;
            skinupdate.Remove(task);
        }

        private void OnServerQuit()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.inventory.crafting.queue.Count == 0) continue;
                if (config.CompleteCrafting)
                    CompleteCrafting(player);
                CancelAllCrafting(player);
            }
        }

        #endregion Hooks

        #region Helpers
        private void ReturnCraft(ItemCraftTask task, BasePlayer crafter)
        {
            task.cancelled = true;
            Message(crafter.IPlayer, "NoInvSpace");
            foreach (var item in task.takenItems)
            {
                if (item.amount > 0)
                    crafter.GiveItem(item);
            }
        }

        private ItemDefinition FindItem(string itemNameOrId)
        {
            ItemDefinition itemDef = ItemManager.FindItemDefinition(itemNameOrId.ToLower());
            if (itemDef == null)
            {
                int itemId;
                if (int.TryParse(itemNameOrId, out itemId))
                {
                    itemDef = ItemManager.FindItemDefinition(itemId);
                }
            }
            return itemDef;
        }
        private int FreeSpace(BasePlayer player, ItemDefinition item)
        {
            var slots = player.inventory.containerMain.capacity + player.inventory.containerBelt.capacity;
            List<Item> containeritems = new List<Item>();
            Dictionary<ItemDefinition, int> queueamts = new Dictionary<ItemDefinition, int>();
            containeritems.AddRange(player.inventory.containerMain.itemList);
            containeritems.AddRange(player.inventory.containerBelt.itemList);

            int value = 0;
            //Sum all items in crafting queue not including the item to be crafted
            foreach (var queueitem in player.inventory.crafting.queue)
            {
                if (queueitem.blueprint.targetItem == item) continue;
                if (queueamts.TryGetValue(queueitem.blueprint.targetItem, out value))
                {
                    queueamts[queueitem.blueprint.targetItem] += queueitem.amount * queueitem.blueprint.amountToCreate;
                    continue;
                }
                queueamts[queueitem.blueprint.targetItem] = (queueitem.amount * queueitem.blueprint.amountToCreate);
            }
            //Take into account room of other stacks
            int queuestacks = 0;
            foreach (var i in queueamts)
            {
                queuestacks += GetStacks(i.Key, i.Value - Stackroom(containeritems, i.Key.shortname)).Count;
            }

            //calculate total room in inventory for the item required
            int invstackroom = (slots - containeritems.Count - queuestacks) * item.stackable;
            containeritems.ForEach(x => { if (x.info == item && x.amount < x.MaxStackable()) invstackroom += x.MaxStackable() - x.amount; });
            foreach (var x in player.inventory.crafting.queue)
            {
                if (x.blueprint.targetItem.shortname == item.shortname)
                {
                    invstackroom -= x.amount * x.blueprint.amountToCreate;
                }
            }
            return invstackroom;
        }
        private int FreeSlots(BasePlayer player)
        {
            var slots = player.inventory.containerMain.capacity + player.inventory.containerBelt.capacity;
            var taken = player.inventory.containerMain.itemList.Count + player.inventory.containerBelt.itemList.Count;
            return slots - taken;
        }

        private int Stackroom(List<Item> items, string item)
        {
            int stackroom = 0;
            items.ForEach(x => { if (x.info.shortname == item && x.amount < x.MaxStackable()) stackroom += x.MaxStackable() - x.amount; });
            return stackroom;
        }

        private List<int> GetStacks(ItemDefinition item, int amount)
        {
            List<int> list = new List<int>();
            int maxStack = item.stackable;
            int maxstacks = amount / maxStack;
            if (maxStack == 0) return list;
            for (int i = 0; i <= maxstacks; i++)
            {
                if (maxStack > amount)
                {
                    if (amount >= 1)
                        list.Add(amount);
                    return list;
                }
                list.Add(maxStack);
                amount -= maxStack;
            }
            return list;
        }
        private readonly Dictionary<string, List<ulong>> skinsCache = new Dictionary<string, List<ulong>>();
        private List<ulong> GetSkins(ItemDefinition def)
        {
            List<ulong> skins;
            if (skinsCache.TryGetValue(def.shortname, out skins)) return skins;
            skins = new List<ulong>();
            foreach (var skin in ItemSkinDirectory.ForItem(def))
            {
                skins.Add((ulong)skin.id);
            }
            foreach (var skin in Rust.Workshop.Approved.All.Values)
            {
                if (skin.Skinnable.ItemName == def.shortname)
                    skins.Add(skin.WorkshopdId);
            }
            skinsCache.Add(def.shortname, skins);
            return skins;
        }

        private string GetLang(string langKey, string playerId = null, params object[] args)
        {
            return string.Format(lang.GetMessage(langKey, this, playerId), args);
        }
        private void Message(IPlayer player, string langKey, params object[] args)
        {
            if (player.IsConnected) player.Message(GetLang(langKey, player.Id, args));
        }

        private bool HasPerm(string id, string perm) => permission.UserHasPermission(id, perm);

        private void AddLocalizedCommand(string command)
        {
            foreach (string language in lang.GetLanguages(this))
            {
                Dictionary<string, string> messages = lang.GetMessages(language, this);
                foreach (KeyValuePair<string, string> message in messages)
                {
                    if (!message.Key.Equals(command)) continue;

                    if (string.IsNullOrEmpty(message.Value)) continue;

                    AddCovalenceCommand(message.Value, command);
                }
            }
        }
        #endregion Helpers
    }
}