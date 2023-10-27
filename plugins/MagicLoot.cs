using System;
using System.Collections.Generic;
using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Rust;

namespace Oxide.Plugins
{
    [Info("Magic Loot", "qxzxf", "1.0.5")]
    [Description("Simple components multiplier and loot system")]
    public class MagicLoot : CovalencePlugin
    {
        #region Fields

        private bool _initialized = false;

        private readonly int _maxContainerSlots = 18;

        private int _scrapStack;

        private Dictionary<Rarity, List<string>> _sortedRarities;

        private Dictionary<int, List<ulong>> _skinsCache = new Dictionary<int, List<ulong>>();

        private Dictionary<LootSpawn, ItemAmountRanged[]> _originItemAmountRange = new Dictionary<LootSpawn, ItemAmountRanged[]>();

        #endregion

        #region Configuration

        private ConfigurationFile _configuration;

        public class ConfigurationFile
        {
            [JsonProperty(PropertyName = "General Settings")]
            public SettingsFile Settings = new SettingsFile();

            [JsonProperty(PropertyName = "Extra Loot")]
            public ExtraLootFile ExtraLoot = new ExtraLootFile();

            [JsonProperty(PropertyName = "Blacklisted Items (Item-Shortnames)",
                ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> BlacklistedItems = new List<string>()
            { 
                "ammo.rocket.smoke"
            };            
            
            [JsonProperty(PropertyName = "Blacklisted Workshop Skins (Workshop Ids)",
                ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<ulong> BlacklistSkins = new List<ulong>()
            {
                10180
            };

            [JsonProperty(PropertyName = "Manual Item Multipliers (Key: Item-Shortname, Value: Multiplier)",
                ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, float> ManualItemMultipliers = new Dictionary<string, float>();

            [JsonProperty(PropertyName = "Containers Data (Key: Container-Shortname, Value: Container Settings)",
                ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, ContainerData> ContainersData = new Dictionary<string, ContainerData>();

            [JsonProperty(PropertyName = "Debug")]
            public bool Debug = false;
        }

        public class SettingsFile
        {
            [JsonProperty(PropertyName = "General Item List Multiplier (All items in the 'Manual Item Multipliers' List)")]
            public float ItemListMultiplier = 1f;

            [JsonProperty(PropertyName = "Non Item List Multiplier (All items not listed in the 'Manual Item Multipliers' List)")]
            public float NonItemListMultiplier = 1f;

            [JsonProperty(PropertyName = "Limit Multipliers to Stacksizes")]
            public bool LimitToStacksizes = true;

            [JsonProperty(PropertyName = "Multiply Blueprints")]
            public bool BlueprintDuplication = false;              
            
            [JsonProperty(PropertyName = "Disable Blueprint Drops")]
            public bool DisableBlueprints = false;            
            
            [JsonProperty(PropertyName = "Random Workshop Skins")]
            public bool RandomWorkshopSkins = false;            
            
            [JsonProperty(PropertyName = "Multiply Tea Buffs")]
            public bool MultiplyTeaBuff = false;            
            
            [JsonProperty(PropertyName = "Force Custom Loot Tables for Default Loot on all Containers")]
            public bool ForceCustomLootTables = false;
        }

        public class ExtraLootFile
        {
            [JsonProperty(PropertyName = "Enabled")]
            public bool Enabled = false;

            [JsonProperty(PropertyName = "Extra Items Min")]
            public int ExtraItemsMin = 0;

            [JsonProperty(PropertyName = "Extra Items Max")]
            public int ExtraItemsMax = 0;

            [JsonProperty(PropertyName = "Prevent Duplicates")]
            public bool PreventDuplicates = true;

            [JsonProperty(PropertyName = "Prevent Duplicates Retries")]
            public int PreventDuplicatesRetries = 10;

            [JsonProperty(PropertyName = "Force Custom Loot Tables for Extra Loot on all Containers")]
            public bool ForceCustomLootTables = false;
        }

        public class ContainerData
        {
            [JsonProperty(PropertyName = "Enabled")]
            public bool Enabled = true;

            [JsonProperty(PropertyName = "Extra Items Min")]
            public int ExtraItemsMin = 0;

            [JsonProperty(PropertyName = "Extra Items Max")]
            public int ExtraItemsMax = 0;

            [JsonProperty(PropertyName = "Loot Multiplier")]
            public float Multiplier = 1f;

            [JsonProperty(PropertyName = "Utilize Vanilla Loot Tables on Default Loot")]
            public bool VanillaLootTablesDefault = true;            
            
            [JsonProperty(PropertyName = "Utilize Vanilla Loot Tables on Extra Loot")]
            public bool VanillaLootTablesExtra = true;

            [JsonProperty(PropertyName = "Utilize Random Rarity (depending on Items ALREADY in the container)")]
            public bool RandomRarities = true;

            [JsonProperty(PropertyName = "Rarity To Use (ONLY if 'Utilize Vanilla Loot Tables' is FALSE & 'Utilize Random Rarity' is FALSE | 0 = None, 1 = Common, 2 = Uncommon, 3 = Rare, 4 = Very Rare)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<Rarity> Rarities = new List<Rarity>() { Rarity.Common, Rarity.Uncommon, Rarity.Rare, Rarity.VeryRare };
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");

            _configuration = new ConfigurationFile();

            foreach (var itemDef in ItemManager.itemList)
            {
                if (itemDef.category != ItemCategory.Component)
                {
                    continue;
                }

                _configuration.ManualItemMultipliers.Add(itemDef.shortname, 1f);
            }
    
            _configuration.ManualItemMultipliers.Add("scrap", 1f);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _configuration = Config.ReadObject<ConfigurationFile>();
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_configuration);

        #endregion

        #region Data

        private void LoadData()
        {
            _data = Interface.Oxide.DataFileSystem.ReadObject<DataFile>(Name);

            int count = 0;
            foreach (var item in ItemManager.itemList)
            {
                if (_data.Items.ContainsKey(item.shortname))
                {
                    continue;
                }

                _data.Items.Add(item.shortname, item.stackable);
                count++;
            }

            SaveData();

            if (count > 0)
            {
                Puts($"Added {count} new items to the item list!");
            }

            //Determine if rarity system is needed
            if (_configuration.Settings.ForceCustomLootTables || _configuration.ExtraLoot.ForceCustomLootTables)
            {
                _sortedRarities = GetSortedRarities();
            }
            else
            {
                foreach (var container in _configuration.ContainersData)
                {
                    if (!container.Value.VanillaLootTablesDefault
                        || !container.Value.VanillaLootTablesExtra)
                    {
                        _sortedRarities = GetSortedRarities();
                        break;
                    }
                }
            }

            _scrapStack = _data.Items["scrap"];
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, _data);
        }

        private DataFile _data;

        public class DataFile
        {
            [JsonProperty(PropertyName = "Item Data (Key: Item Shortname, Value: Stacksize)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, int> Items = new Dictionary<string, int>();
        }

        #endregion

        #region Hooks

        private void OnServerInitialized()
        {
            _initialized = true;

            if (!_configuration.Settings.MultiplyTeaBuff)
            {
                Unsubscribe(nameof(OnBonusItemDrop));
            }

            LoadData();

            ModifyItemAmountRanges();

            AddMissingContainers();

            Puts($"Loaded at x{_configuration.Settings.NonItemListMultiplier} vanilla rate" +
                $" | Manual Item List at x{_configuration.Settings.ItemListMultiplier} rate [Extra Loot: {_configuration.ExtraLoot.Enabled}, Multiply Tea Buffs: {_configuration.Settings.MultiplyTeaBuff}]");

            RepopulateContainers(); 
        }

        private object OnLootSpawn(LootContainer container)
        {
            if (!_initialized || container?.inventory?.itemList == null)
            {
                return null;
            }
            
            ContainerData containerData;
            if (!_configuration.ContainersData.TryGetValue(container.ShortPrefabName, out containerData))
            {
                _configuration.ContainersData.Add(container.ShortPrefabName, containerData = new ContainerData());
                SaveConfig();
            }

            if (IgnoreContainer(containerData))
            {
                return null;
            }

            PopulateContainer(container, containerData);

            if (container.shouldRefreshContents && container.isLootable)
            {
                container.Invoke(new Action(container.SpawnLoot), UnityEngine.Random.Range(
                    container.minSecondsBetweenRefresh, container.maxSecondsBetweenRefresh));
            }

            return container;
        }

        private void OnBonusItemDrop(Item item, BasePlayer player)
        {
            ReinforceRules(item, null);
        }

        private void Unload()
        {
            _initialized = false;

            RestoreItemAmountRanges();

            foreach (var container in UnityEngine.Object.FindObjectsOfType<LootContainer>())
            {
                if (container?.inventory == null)
                {
                    continue;
                }

                container.SpawnLoot();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds missing containers to the configuration file
        /// </summary>
        private void AddMissingContainers()
        {
            int count = 0;
            foreach (var container in UnityEngine.Resources.FindObjectsOfTypeAll<LootContainer>())
            {
                if (!container.enabled)
                {
                    continue;
                }

                ContainerData containerData;
                if (_configuration.ContainersData.TryGetValue(container.ShortPrefabName, out containerData))
                {
                    continue;
                }

                _configuration.ContainersData.Add(container.ShortPrefabName, containerData = new ContainerData());

                count++;
            }

            if (count > 0)
            {
                Puts($"Added {count} missing containers to the config file.");
            }            
        }

        /// <summary>
        /// Repopulates all LootContainers
        /// </summary>
        private void RepopulateContainers()
        {
            int count = 0;
            foreach (var container in UnityEngine.Object.FindObjectsOfType<LootContainer>())
            {
                var containerData = _configuration.ContainersData[container.ShortPrefabName];
                
                if (IgnoreContainer(containerData))
                {
                    continue;
                }

                container.inventory.Clear();
                ItemManager.DoRemoves();

                PopulateContainer(container, containerData);

                count++;
            }
           
            SaveConfig();

            Puts("Repopulated " + count.ToString() + " loot containers.");
        }

        /// <summary>
        /// Populates the Container
        /// </summary>
        /// <param name="container"></param>
        /// <param name="containerData"></param>
        private void PopulateContainer(LootContainer container, ContainerData containerData)
        {
            container.inventory.capacity = _maxContainerSlots;

            AddDefaultLoot(container, containerData);

            AddExtraLoot(container, containerData);

            RandomizeDurability(container);

            //Generate scrap late so it is in the last slot
            if (_scrapStack > 0)
            {
                container.GenerateScrap();
            }
           
            ReinforceRules(container, containerData);
        }

        /// <summary>
        /// Removes blacklisted items, applies item skins and applies multipliers
        /// </summary>
        /// <param name="container"></param>
        private void ReinforceRules(LootContainer container, ContainerData containerData)
        {
            for (int i = 0; i < container.inventory.itemList.Count; i++)
            {
                var item = container.inventory.itemList[i];
                ReinforceRules(item, containerData);                         
            }
        }

        /// <summary>
        /// Removes blacklisted item, applies item skin and applies multipliers
        /// </summary>
        /// <param name="item"></param>
        /// <param name="containerData"></param>
        private void ReinforceRules(Item item, ContainerData containerData = null)
        {
            if (_configuration.Settings.DisableBlueprints && item.IsBlueprint())
            {
                item.info = ItemManager.FindItemDefinition(item.blueprintTarget);
                item.blueprintTarget = 0;

                item.maxCondition = item.info.condition.max;
                item.condition = item.info.condition.max;
                item.OnItemCreated();
            }

            if (!_configuration.Settings.BlueprintDuplication && item.IsBlueprint())
            {
                return;
            }

            if (_configuration.Settings.RandomWorkshopSkins && item.info.shortname != "hazmatsuit")
            {
                var skinId = GetRandomSkin(item.info);
                ApplySkinToItem(item, skinId);
            }

            float multiplier = 1f;
            var inItemList = _configuration.ManualItemMultipliers.TryGetValue(
                item.info.shortname, out multiplier);

            if (!inItemList)
            {
                multiplier = 1f;
            }

            //Do not multiply
            if (multiplier == 0f)
            {
                return;
            }

            multiplier *= inItemList ? _configuration.Settings.ItemListMultiplier
                : _configuration.Settings.NonItemListMultiplier;

            if (containerData != null)
            {
                multiplier *= containerData.Multiplier;
            }

            var maxAmount = _data.Items[item.info.shortname];
            if (maxAmount <= 0)
            {
                PrintWarning($"Item '{item.info.shortname}' spawned with 0 amount...");
            }

            item.amount = _configuration.Settings.LimitToStacksizes ? (int)Math.Min(item.amount * multiplier, maxAmount)
                : (int)(item.amount * multiplier);
        }

        /// <summary>
        /// Returns a list of items where duplicated items are found
        /// </summary>
        /// <param name="item"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        private void FindDuplicates(int itemId, List<Item> items, ref List<Item> duplicateItems)
        {
            foreach (var cItem in items)
            {
                if (cItem.info.itemid != itemId)
                {
                    continue;
                }

                duplicateItems.Add(cItem);
            }
        }

        private bool IsValid(ItemDefinition itemDefintion)
        {
            if (!_configuration.Settings.LimitToStacksizes)
            {
                return true;
            }
            else if (_configuration.BlacklistedItems.Contains(itemDefintion.shortname))
            {
                return false;
            }
            else if (_data.Items[itemDefintion.shortname] <= 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates all Loot Spawns found
        /// </summary>
        private void ModifyItemAmountRanges()
        {
            if (_configuration.Settings.ForceCustomLootTables && (_configuration.ExtraLoot.Enabled && _configuration.ExtraLoot.ForceCustomLootTables))
            {
                Puts("Skipping modifying default loot tables...");
                return;
            }

            var lootSpawns = UnityEngine.Resources.FindObjectsOfTypeAll<LootSpawn>();
            foreach (var lootSpawn in lootSpawns)
            {
                if (lootSpawn?.items == null)
                {
                    PrintDebug("Loot spawn data is null");
                    continue;
                }
                
                ModifyItemAmountRange(lootSpawn);
            }

            if (_originItemAmountRange.Count > 0)
            {
                Puts($"Modified '{_originItemAmountRange.Count}' loot data item ranges out of a total of '{lootSpawns.Length}' ...");
            }
        }

        /// <summary>
        /// Validates given item amount ranged array and modifies/removes items if needed
        /// </summary>
        /// <param name="origin"></param>
        private void ModifyItemAmountRange(LootSpawn origin)
        {
            var tempItemsToSpawn = Pool.GetList<ItemAmountRanged>();
            for (int i = 0; i < origin.items.Length; i++)
            {
                var itemRangeToSpawn = origin.items[i];

                if (!IsValid(itemRangeToSpawn.itemDef))
                {                    
                    continue;
                }

                tempItemsToSpawn.Add(itemRangeToSpawn);
            }

            if (tempItemsToSpawn.Count != origin.items.Length)
            {
                _originItemAmountRange.Add(origin, origin.items.Clone() as ItemAmountRanged[]);
                origin.items = tempItemsToSpawn.ToArray();
                PrintDebug($"Modified loot data of '{origin.name}'...");
            }
            
            Pool.FreeList(ref tempItemsToSpawn);
        }

        /// <summary>
        /// Resets all initially stored loot spawns to their default item ranges
        /// </summary>
        private void RestoreItemAmountRanges()
        {
            foreach (var lootSpawn in _originItemAmountRange)
            {
                lootSpawn.Key.items = lootSpawn.Value;
            }

            Puts($"Restored '{_originItemAmountRange.Count}' loot data item ranges to default...");
        }

        /// <summary>
        /// Adds default loot to the container
        /// </summary>
        /// <param name="container"></param>
        /// <param name="containerData"></param>
        private void AddDefaultLoot(LootContainer container, ContainerData containerData)
        {
            var useDefaultLootTables = !(containerData.Enabled && !containerData.VanillaLootTablesDefault);
            if (useDefaultLootTables)
            {
                useDefaultLootTables = !_configuration.Settings.ForceCustomLootTables;
            }

            if (container.LootSpawnSlots.Length != 0)
            {
                for (int i = 0; i < container.LootSpawnSlots.Length; i++)
                {
                    var lootSpawnSlot = container.LootSpawnSlots[i];

                    for (int j = 0; j < lootSpawnSlot.numberToSpawn; j++)
                    {
                        if (UnityEngine.Random.Range(0f, 1f) <= lootSpawnSlot.probability)
                        {
                            if (useDefaultLootTables)
                            {                               
                                lootSpawnSlot.definition.SpawnIntoContainer(container.inventory);                               
                                continue;
                            }

                            AddRandomItem(container, containerData);
                        }
                    }
                }
            }
            else if (container.lootDefinition != null)
            {
                for (int k = 0; k < container.maxDefinitionsToSpawn; k++)
                {
                    if (useDefaultLootTables)
                    {
                        container.lootDefinition.SpawnIntoContainer(container.inventory);
                        continue;
                    }

                    AddRandomItem(container, containerData);
                }
            }
        }

        /// <summary>
        /// Adds extra loot to the container, depending on both the Extra Loot and Specific Container Data
        /// </summary>
        /// <param name="container"></param>
        private void AddExtraLoot(LootContainer container, ContainerData containerData)
        {
            var useDefaultLootTables = !(containerData.Enabled && !containerData.VanillaLootTablesExtra);
            if (useDefaultLootTables)
            {
                useDefaultLootTables = !_configuration.ExtraLoot.ForceCustomLootTables;
            }

            var additionalItemsMin = 0;
            var additionalItemsMax = 0;

            if (_configuration.ExtraLoot.Enabled)
            {
                additionalItemsMin += _configuration.ExtraLoot.ExtraItemsMin;
                additionalItemsMax += _configuration.ExtraLoot.ExtraItemsMax;
            }

            if (containerData.Enabled) 
            {
                additionalItemsMin += containerData.ExtraItemsMin;
                additionalItemsMax += containerData.ExtraItemsMax;
            }

            var additionalItems = UnityEngine.Random.Range(additionalItemsMin, additionalItemsMax + 1);
            for (int i = 0; i < additionalItems; i++)
            {
                if (useDefaultLootTables)
                {
                    AddRandomVanillaItem(container);
                }
                else
                {
                    AddRandomItem(container, containerData);
                }
            }
        }

        /// <summary>
        /// Adds a random Vanilla-LootTable Item to the Container
        /// </summary>
        /// <param name="container"></param>
        private void AddRandomVanillaItem(LootContainer container)
        {
            if (container.lootDefinition != null)
            {
                container.lootDefinition.SpawnIntoContainer(container.inventory);
            }
        }

        /// <summary>
        /// Adds a random Item to the Container
        /// </summary>
        /// <param name="container"></param>
        private void AddRandomItem(LootContainer container, ContainerData containerData)
        {
            if (containerData.Rarities.Count == 0)
            {
                PrintWarning("Using Non-Vanilla-LootTables but no set Rarity found in list for container : " + container.ShortPrefabName);
                containerData.Rarities.Add(Rarity.Common);
                SaveConfig();
            }

            var randomContainerRarity = Rarity.Common;            
            if (containerData.RandomRarities && container.inventory.itemList.Count > 0)
            {
                randomContainerRarity = container.inventory.itemList[UnityEngine.Random.Range(0, container.inventory.itemList.Count)].info.rarity;
            }
            else
            {
                randomContainerRarity = containerData.Rarities[UnityEngine.Random.Range(0, containerData.Rarities.Count)];
            }
            
            var rarities = _sortedRarities[randomContainerRarity];
            var randomItemShortname = rarities[UnityEngine.Random.Range(0, rarities.Count)];
            var randomItem = ItemManager.itemDictionaryByName[randomItemShortname];

            if (_configuration.ExtraLoot.PreventDuplicates)
            {
                var duplicateItems = Pool.GetList<Item>();

                for (int i = 0; i < _configuration.ExtraLoot.PreventDuplicatesRetries; i++)
                {
                    FindDuplicates(randomItem.itemid, container.inventory.itemList, ref duplicateItems);

                    if (duplicateItems.Count > 0)
                    {
                        randomItemShortname = rarities[UnityEngine.Random.Range(0, rarities.Count)];
                        randomItem = ItemManager.itemDictionaryByName[randomItemShortname];
                        
                        duplicateItems.Clear();

                        if (i >= _configuration.ExtraLoot.PreventDuplicatesRetries - 1)
                        {
                            PrintDebug("Unable to solve duplicate conflict with " + container.ShortPrefabName + " " + container.transform.position);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
              
                Pool.FreeList(ref duplicateItems);
            }

            var maxAmount = _data.Items[randomItemShortname];
            if (maxAmount > 0)
            {
                container.inventory.AddItem(randomItem, UnityEngine.Random.Range(1, maxAmount));
            }
        }

        /// <summary>
        /// Randomizes the durability of the Items in the Container if ROADSIDE or TOWN Container
        /// </summary>
        /// <param name="container"></param>
        private void RandomizeDurability(LootContainer container)
        {
            if (container.SpawnType == LootContainer.spawnType.ROADSIDE || container.SpawnType == LootContainer.spawnType.TOWN)
            {
                foreach (Item item in container.inventory.itemList)
                {
                    if (!item.hasCondition)
                    {
                        continue;
                    }

                    item.condition = UnityEngine.Random.Range(item.info.condition.foundCondition.fractionMin, item.info.condition.foundCondition.fractionMax) * item.info.condition.max;
                }
            }
        }

        /// <summary>
        /// Generates a Rarity Dictionary containing only Item Definitions that are not Blacklisted
        /// </summary>
        /// <returns></returns>
        private Dictionary<Rarity, List<string>> GetSortedRarities()
        {
            var sortedRarities = new Dictionary<Rarity, List<string>>();

            foreach (var itemDef in ItemManager.itemList)
            {
                if (_configuration.BlacklistedItems.Contains(itemDef.shortname))
                {
                    PrintDebug($"Filtering out '{itemDef.shortname}'");
                    continue;
                }

                if (_data.Items[itemDef.shortname] <= 0)
                {
                    continue;
                }

                if (!sortedRarities.ContainsKey(itemDef.rarity))
                {
                    sortedRarities.Add(itemDef.rarity, new List<string>() { itemDef.shortname });
                    continue;
                }

                sortedRarities[itemDef.rarity].Add(itemDef.shortname);
            }

            Puts("Loaded Rarities...");

            return sortedRarities;
        }

        /// <summary>
        /// Returns if the container is NOT needed to be custom populated
        /// </summary>
        /// <param name="container"></param>
        /// <returns></returns>
        private bool IgnoreContainer(ContainerData containerData)
        {
            return !containerData.Enabled;
        }

        private ulong GetRandomSkin(ItemDefinition itemDef)
        {
            List<ulong> possibleSkins;
            if (_skinsCache.TryGetValue(itemDef.itemid, out possibleSkins))
            {
                return possibleSkins[UnityEngine.Random.Range(0, possibleSkins.Count)];
            }

            possibleSkins = new List<ulong>() { 0 };

            foreach (var skin in ItemSkinDirectory.ForItem(itemDef))
            {
                var skinId = (ulong)skin.id;

                if (_configuration.BlacklistSkins.Contains(skinId))
                {
                    continue;
                }

                possibleSkins.Add(skinId);
            }

            foreach (var skin in Rust.Workshop.Approved.All.Values)
            {
                if (skin.Skinnable.ItemName != itemDef.shortname)
                {
                    continue;
                }

                if (_configuration.BlacklistSkins.Contains(skin.WorkshopdId))
                {
                    continue;
                }

                possibleSkins.Add(skin.WorkshopdId);
            }

            _skinsCache.Add(itemDef.itemid, possibleSkins);

            return possibleSkins[UnityEngine.Random.Range(0, possibleSkins.Count)];
        }

        private void ApplySkinToItem(Item item, ulong skinId)
        {
            if (skinId == 0)
            {
                return;
            }

            item.skin = skinId;
            item.MarkDirty();
            BaseEntity heldEntity = item.GetHeldEntity();
            if (heldEntity != null)
            {
                heldEntity.skinID = skinId;
                heldEntity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            }
        }

        private void PrintDebug(object message)
        {
            if (!_configuration.Debug)
            {
                return;
            }

            Puts(message.ToString());
        }

        #endregion
    }
}