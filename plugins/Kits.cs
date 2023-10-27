using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using Oxide.Plugins;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Kits", "qxzxf", "4.4.0"), Description("Create kits containing items that players can redeem")]
    class Kits : RustPlugin
    {
        #region Fields
        [PluginReference]
        private Plugin CopyPaste, ImageLibrary, ServerRewards, Economics;

        private DateTime _deprecatedHookTime = new DateTime(2021, 12, 31);

        private Hash<ulong, KitData.Kit> _kitCreators = new Hash<ulong, KitData.Kit>();

        private const string ADMIN_PERMISSION = "kits.admin";

        private const string BLUEPRINT_BASE = "blueprintbase";
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            LoadData();

            permission.RegisterPermission(ADMIN_PERMISSION, this);
            kitData.RegisterPermissions(permission, this);

            _costType = ParseType<CostType>(Configuration.Currency);

            cmd.AddChatCommand(Configuration.Command, this, cmdKit);
            cmd.AddConsoleCommand(Configuration.Command, this, "ccmdKit");
        }

        protected override void LoadDefaultMessages() => lang.RegisterMessages(Messages, this);

        private void OnServerInitialized()
        {
            LastWipeTime = SaveRestore.SaveCreatedTime.Subtract(Epoch).TotalSeconds;

            kitData.RegisterImages(ImageLibrary);

            CheckForShortnameUpdates();

            if (Configuration.AutoKits.Count == 0)
                Unsubscribe(nameof(OnPlayerRespawned));
        }

        private void OnNewSave(string filename)
        {
            if (Configuration.WipeData)
                playerData.Wipe();
        }

        private void OnServerSave() => SavePlayerData();

        private void OnPlayerRespawned(BasePlayer player)
        {
            if (player == null)
                return;

            if ((Interface.Oxide.CallDeprecatedHook("canRedeemKit", "CanRedeemKit", _deprecatedHookTime, player) ?? Interface.Oxide.CallHook("CanRedeemKit", player)) != null)
                return;

            if (Configuration.AllowAutoToggle && !playerData[player.userID].ClaimAutoKits)
            {
                player.ChatMessage(Message("Error.AutoKitDisabled", player.userID));
                return;
            }

            for (int i = 0; i < Configuration.AutoKits.Count; i++)
            {
                KitData.Kit kit;
                if (!kitData.Find(Configuration.AutoKits[i], out kit))
                    continue;

                object success = CanClaimKit(player, kit, true);
                if (success != null)
                    continue;

                player.inventory.Strip();

                success = GiveKit(player, kit);
                if (success is string)
                    continue;

                OnKitReceived(player, kit);
                return;
            }
        }

        private void Unload()
        {
            if (!Interface.Oxide.IsShuttingDown)
                SavePlayerData();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, UI_MENU);
                CuiHelper.DestroyUi(player, UI_POPUP);
            }

            Configuration = null;
        }
        #endregion

        #region Kit Claiming
        private bool TryClaimKit(BasePlayer player, string name, bool usingUI)
        {
            if (string.IsNullOrEmpty(name))
            {
                if (usingUI)
                    CreateMenuPopup(player, Message("Error.EmptyKitName", player.userID));
                else player.ChatMessage(Message("Error.EmptyKitName", player.userID));
                return false;
            }

            KitData.Kit kit;
            if (!kitData.Find(name, out kit))
            {
                if (usingUI)
                    CreateMenuPopup(player, Message("Error.InvalidKitName", player.userID));
                else player.ChatMessage(Message("Error.InvalidKitName", player.userID));
                return false;
            }

            object success = CanClaimKit(player, kit) ?? GiveKit(player, kit);
            if (success is string)
            {
                if (usingUI)
                    CreateMenuPopup(player, (string)success);
                else player.ChatMessage((string)success);
                return false;
            }

            OnKitReceived(player, kit);
            return true;
        }

        private object CanClaimKit(BasePlayer player, KitData.Kit kit, bool ignoreAuthCost = false)
        {
            object success = Interface.Oxide.CallDeprecatedHook("canRedeemKit", "CanRedeemKit", _deprecatedHookTime, player) ?? Interface.Oxide.CallHook("CanRedeemKit", player);
            if (success != null)
            {
                if (success is string)
                    return (string)success;
                return Message("Error.CantClaimNow", player.userID);
            }

            if (!ignoreAuthCost && kit.RequiredAuth > 0 && player.net.connection.authLevel < kit.RequiredAuth)
                return Message("Error.CanClaim.Auth", player.userID);

            if (Configuration.AdminIgnoreRestrictions && IsAdmin(player))
            {
                if (!kit.HasSpaceForItems(player))
                    return Message("Error.CanClaim.InventorySpace", player.userID);

                return null;
            }

            if (!string.IsNullOrEmpty(kit.RequiredPermission) && !permission.UserHasPermission(player.UserIDString, kit.RequiredPermission))
                return Message("Error.CanClaim.Permission", player.userID);

            int wipeCooldownTime;
            if (Configuration.WipeCooldowns.TryGetValue(kit.Name, out wipeCooldownTime))
            {
                if (kitData.IsOnWipeCooldown(wipeCooldownTime, out wipeCooldownTime))
                    return string.Format(Message("Error.CanClaim.WipeCooldown", player.userID), FormatTime(wipeCooldownTime));
            }

            PlayerData.PlayerUsageData playerUsageData;
            if (playerData.Find(player.userID, out playerUsageData))
            {
                if (kit.Cooldown > 0)
                {
                    double cooldownRemaining = playerUsageData.GetCooldownRemaining(kit.Name);
                    if (cooldownRemaining > 0)
                        return string.Format(Message("Error.CanClaim.Cooldown", player.userID), FormatTime(cooldownRemaining));
                }

                if (kit.MaximumUses > 0)
                {
                    int currentUses = playerUsageData.GetKitUses(kit.Name);
                    if (currentUses >= kit.MaximumUses)
                        return Message("Error.CanClaim.MaxUses", player.userID);
                }
            }

            if (!kit.HasSpaceForItems(player))
                return Message("Error.CanClaim.InventorySpace", player.userID);

            if (!ignoreAuthCost && kit.Cost > 0)
            {
                if (!ChargePlayer(player, kit.Cost))
                    return string.Format(Message("Error.CanClaim.InsufficientFunds", player.userID), kit.Cost, Message($"Cost.{_costType}", player.userID));
            }

            return null;
        }

        private object GiveKit(BasePlayer player, KitData.Kit kit)
        {
            if (!string.IsNullOrEmpty(kit.CopyPasteFile))
            {
                object success = CopyPaste?.CallHook("TryPasteFromSteamId", player.userID, kit.CopyPasteFile, Configuration.CopyPasteParams, null);
                if (success != null)
                    return success;
            }

            kit.GiveItemsTo(player);

            return true;
        }

        private void OnKitReceived(BasePlayer player, KitData.Kit kit)
        {
            playerData[player.userID].OnKitClaimed(kit);

            Interface.CallHook("OnKitRedeemed", player, kit.Name);

            if (Configuration.LogKitsGiven)
                LogToFile("Kits_Received", $"{player.displayName} ({player.userID}) - Received {kit.Name}", this);
        }
        #endregion

        #region Purchase Costs     
        private CostType _costType;

        private enum CostType { Scrap, ServerRewards, Economics }

        private const int SCRAP_ITEM_ID = -932201673;

        private bool ChargePlayer(BasePlayer player, int amount)
        {
            if (amount == 0)
                return true;

            switch (_costType)
            {
                case CostType.Scrap:
                    if (amount <= player.inventory.GetAmount(SCRAP_ITEM_ID))
                    {
                        player.inventory.Take(null, SCRAP_ITEM_ID, amount);
                        return true;
                    }
                    return false;
                case CostType.ServerRewards:
                    {
                        if ((ServerRewards?.Call<int>("CheckPoints", player.UserIDString) ?? 0) < amount)
                            return false;

                        return (bool)ServerRewards?.Call("TakePoints", player.userID, amount);
                    }
                case CostType.Economics:
                    return (bool)Economics?.Call("Withdraw", player.UserIDString, (double)amount);
            }
            return false;
        }
        #endregion

        #region Helpers
        private T ParseType<T>(string type)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), type, true);
            }
            catch
            {
                return default(T);
            }
        }

        private string FormatTime(double time)
        {
            TimeSpan dateDifference = TimeSpan.FromSeconds(time);
            int days = dateDifference.Days;
            int hours = dateDifference.Hours;
            int mins = dateDifference.Minutes;
            int secs = dateDifference.Seconds;

            if (days > 0)
                return string.Format("~{0:00}d:{1:00}h", days, hours);
            else if (hours > 0)
                return string.Format("~{0:00}h:{1:00}m", hours, mins, secs);
            else if (mins > 0)
                return string.Format("{0:00}m:{1:00}s", mins, secs);
            else return string.Format("{0}s", secs);
        }

        private void GetUserValidKits(BasePlayer player, List<KitData.Kit> list, ulong npcId = 0UL)
        {
            bool isAdmin = IsAdmin(player);
            bool viewPermissionKits = Configuration.ShowPermissionKits;

            if (npcId != 0UL)
            {
                ConfigData.NPCKit npcKit;
                if (Configuration.NPCKitMenu.TryGetValue(npcId, out npcKit))
                {
                    npcKit.Kits.ForEach((string kitName) =>
                    {
                        KitData.Kit kit;
                        if (kitData.Find(kitName, out kit))
                        {
                            if (!viewPermissionKits && !string.IsNullOrEmpty(kit.RequiredPermission) && !permission.UserHasPermission(player.UserIDString, kit.RequiredPermission) && !isAdmin)
                                return;

                            if (player.net.connection.authLevel < kit.RequiredAuth)
                                return;

                            list.Add(kit);
                        }
                    });
                }
            }
            else
            {
                kitData.ForEach((KitData.Kit kit) =>
                {
                    if (kit.IsHidden && !isAdmin)
                        return;

                    if (!viewPermissionKits && !string.IsNullOrEmpty(kit.RequiredPermission) && !permission.UserHasPermission(player.UserIDString, kit.RequiredPermission) && !isAdmin)
                        return;

                    if (player.net.connection.authLevel < kit.RequiredAuth)
                        return;

                    list.Add(kit);
                });
            }
        }

        private bool IsAdmin(BasePlayer player) => permission.UserHasPermission(player.UserIDString, ADMIN_PERMISSION);

        private BasePlayer FindPlayer(string partialNameOrID) => BasePlayer.allPlayerList.FirstOrDefault<BasePlayer>((BasePlayer x) => x.UserIDString.Equals(partialNameOrID) ||            
                                                                                                    x.displayName.Equals(partialNameOrID, StringComparison.OrdinalIgnoreCase) ||
                                                                                                    x.displayName.Contains(partialNameOrID, CompareOptions.OrdinalIgnoreCase));

        private BasePlayer RaycastPlayer(BasePlayer player)
        {
            RaycastHit raycastHit;
            if (!Physics.Raycast(new Ray(player.eyes.position, Quaternion.Euler(player.serverInput.current.aimAngles) * Vector3.forward), out raycastHit, 5f))
                return null;

            return raycastHit.collider.GetComponentInParent<BasePlayer>();            
        }

        private static DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0);

        private static double LastWipeTime;

        private static double CurrentTime => DateTime.UtcNow.Subtract(Epoch).TotalSeconds;
        #endregion

        #region ImageLibrary        
        private void RegisterImage(string name, string url) => ImageLibrary?.Call("AddImage", url, name.Replace(" ", ""), 0UL, null);

        private string GetImage(string name, ulong skinId = 0UL) => ImageLibrary?.Call<string>("GetImage", name.Replace(" ", ""), skinId, false);
        #endregion

        #region HumanNPC
        private void OnUseNPC(BasePlayer npcPlayer, BasePlayer player)
        {
            if (Configuration.NPCKitMenu.ContainsKey(npcPlayer.userID))            
                OpenKitGrid(player, 0, npcPlayer.userID);            
        }
        #endregion

        #region Deprecated API 
        [HookMethod("isKit")]
        public bool isKit(string name) => IsKit(name);

        [HookMethod("GetAllKits")]
        public string[] GetAllKits() => kitData.Keys.ToArray();

        [HookMethod("KitImage")]
        public string KitImage(string name) => GetKitImage(name);

        [HookMethod("KitDescription")]
        public string KitDescription(string name) => GetKitDescription(name);

        [HookMethod("KitMax")]
        public int KitMax(string name) => GetKitMaxUses(name);

        [HookMethod("KitCooldown")]
        public double KitCooldown(string name) => (double)GetKitCooldown(name);

        [HookMethod("PlayerKitMax")]
        public int PlayerKitMax(ulong playerId, string name) => GetPlayerKitUses(playerId, name);

        [HookMethod("PlayerKitCooldown")]
        public double PlayerKitCooldown(ulong playerId, string name) => GetPlayerKitCooldown(playerId, name);

        [HookMethod("GetKitContents")]
        public string[] GetKitContents(string name)
        {
            KitData.Kit kit;
            if (kitData.Find(name, out kit))
            {
                List<string> items = Facepunch.Pool.GetList<string>();
                for (int i1 = 0; i1 < kit.BeltItems.Length; i1++)
                {
                    ItemData itemData = kit.BeltItems[i1];
                    string itemstring = $"{itemData.ItemID}_{itemData.Amount}";

                    for (int i2 = 0; i2 < itemData.Contents?.Length; i2++)
                        itemstring = itemstring + $"_{itemData.Contents[i2].ItemID}";

                    items.Add(itemstring);
                }

                for (int i1 = 0; i1 < kit.WearItems.Length; i1++)
                {
                    ItemData itemData = kit.WearItems[i1];
                    string itemstring = $"{itemData.ItemID}_{itemData.Amount}";

                    for (int i2 = 0; i2 < itemData.Contents?.Length; i2++)
                        itemstring = itemstring + $"_{itemData.Contents[i2].ItemID}";

                    items.Add(itemstring);
                }

                for (int i1 = 0; i1 < kit.MainItems.Length; i1++)
                {
                    ItemData itemData = kit.MainItems[i1];
                    string itemstring = $"{itemData.ItemID}_{itemData.Amount}";

                    for (int i2 = 0; i2 < itemData.Contents?.Length; i2++)
                        itemstring = itemstring + $"_{itemData.Contents[i2].ItemID}";

                    items.Add(itemstring);
                }

                string[] array = items.ToArray();
                Facepunch.Pool.FreeList(ref items);

                return array;
            }

            return null;
        }

        [HookMethod("GetKitInfo")]
        public object GetKitInfo(string name)
        {
            KitData.Kit kit;
            if (kitData.Find(name, out kit))
            {
                JObject obj = new JObject
                {
                    ["name"] = kit.Name,
                    ["permission"] = kit.RequiredPermission,
                    ["max"] = kit.MaximumUses,
                    ["image"] = kit.KitImage,
                    ["hide"] = kit.IsHidden,
                    ["description"] = kit.Description,
                    ["cooldown"] = kit.Cooldown,
                    ["building"] = kit.CopyPasteFile,
                    ["authlevel"] = kit.RequiredAuth
                };

                JArray array = new JArray();
                GetItemObject_Old(ref array, kit.BeltItems, "belt");
                GetItemObject_Old(ref array, kit.MainItems, "main");
                GetItemObject_Old(ref array, kit.WearItems, "wear");

                obj["items"] = array;
                return obj;
            }

            return null;
        }

        private void GetItemObject_Old(ref JArray array, ItemData[] items, string container)
        {
            for (int i = 0; i < items.Length; i++)
            {
                ItemData itemData = items[i];
                JObject item = new JObject
                {
                    ["amount"] = itemData.Amount,
                    ["container"] = container,
                    ["itemid"] = itemData.ItemID,
                    ["skinid"] = itemData.Skin,
                    ["weapon"] = !string.IsNullOrEmpty(itemData.Ammotype),
                    ["blueprint"] = itemData.BlueprintItemID
                };

                item["mods"] = new JArray();
                for (int i1 = 0; i1 < itemData.Contents?.Length; i1++)
                    (item["mods"] as JArray).Add(itemData.Contents[i1].ItemID);

                array.Add(item);
            }
        }
        #endregion

        #region API
        [HookMethod("GiveKit")]
        public object GiveKit(BasePlayer player, string name)
        {
            if (player == null)
                return null;

            if (string.IsNullOrEmpty(name))
                return Message("Error.EmptyKitName", player.userID);

            KitData.Kit kit;
            if (!kitData.Find(name, out kit))
                return Message("Error.InvalidKitName", player.userID);

            return GiveKit(player, kit);
        }

        [HookMethod("IsKit")]
        public bool IsKit(string name) => !string.IsNullOrEmpty(name) ? kitData.Exists(name) : false;

        [HookMethod("GetKitNames")]
        public void GetKitNames(List<string> list) => list.AddRange(kitData.Keys);

        [HookMethod("GetKitImage")]
        public string GetKitImage(string name) => kitData[name]?.KitImage ?? string.Empty;

        [HookMethod("GetKitDescription")]
        public string GetKitDescription(string name) => kitData[name]?.Description ?? string.Empty;

        [HookMethod("GetKitMaxUses")]
        public int GetKitMaxUses(string name) => kitData[name]?.MaximumUses ?? 0;

        [HookMethod("GetKitCooldown")]
        public int GetKitCooldown(string name) => kitData[name]?.Cooldown ?? 0;

        [HookMethod("GetPlayerKitUses")]
        public int GetPlayerKitUses(ulong playerId, string name) => playerData.Exists(playerId) ? playerData[playerId].GetKitUses(name) : 0;

        [HookMethod("SetPlayerKitUses")]
        public void SetPlayerKitUses(ulong playerId, string name, int amount)
        {
            if (playerData.Exists(playerId))
                playerData[playerId].SetKitUses(name, amount);
        }

        [HookMethod("GetPlayerKitCooldown")]
        public double GetPlayerKitCooldown(ulong playerId, string name) => playerData.Exists(playerId) ? playerData[playerId].GetCooldownRemaining(name) : 0;

        [HookMethod("SetPlayerKitCooldown")]
        public void SetPlayerCooldown(ulong playerId, string name, double seconds)
        {
            if (playerData.Exists(playerId))
                playerData[playerId].SetCooldownRemaining(name, seconds);
        }

        [HookMethod("GetKitObject")]
        public JObject GetKitObject(string name)
        {
            KitData.Kit kit;
            if (!kitData.Find(name, out kit))
                return null;

            return kit.ToJObject;
        }
        
        [HookMethod("CreateKitItems")]
        public IEnumerable<Item> CreateKitItems(string name)
        {
            KitData.Kit kit;
            if (!kitData.Find(name, out kit))
                yield break;
            foreach (var item in kit.CreateItems())
                yield return item;
        }
        #endregion

        #region UI
        private const string UI_MENU = "kits.menu";
        private const string UI_POPUP = "kits.popup";

        private const string DEFAULT_ICON = "kits.defaultkiticon";
        private const string MAGNIFY_ICON = "kits.magnifyicon";

        #region Kit Grid View
        private void OpenKitGrid(BasePlayer player, int page = 0, ulong npcId = 0UL)
        {
            CuiElementContainer container = UI.Container(UI_MENU, "0 0 0 0.9", new UI4(0.2f, 0.15f, 0.8f, 0.85f), true, "Hud");

            UI.Panel(container, UI_MENU, Configuration.Menu.Panel.Get, new UI4(0.005f, 0.93f, 0.995f, 0.99f));

            UI.Label(container, UI_MENU, Message("UI.Title", player.userID), 20, new UI4(0.015f, 0.93f, 0.99f, 0.99f), TextAnchor.MiddleLeft);

            UI.Button(container, UI_MENU, Configuration.Menu.Color3.Get, "<b>×</b>", 20, new UI4(0.9575f, 0.9375f, 0.99f, 0.9825f), "kits.close");

            if (IsAdmin(player) && npcId == 0UL)            
                UI.Button(container, UI_MENU, Configuration.Menu.Color2.Get, Message("UI.CreateNew", player.userID), 14, new UI4(0.85f, 0.9375f, 0.9525f, 0.9825f), "kits.create");
            
            CreateGridView(player, container, page, npcId);

            CuiHelper.DestroyUi(player, UI_MENU);
            CuiHelper.AddUi(player, container);
        }

        private void CreateGridView(BasePlayer player, CuiElementContainer container, int page = 0, ulong npcId = 0UL)
        {
            List<KitData.Kit> list = Facepunch.Pool.GetList<KitData.Kit>();

            GetUserValidKits(player, list, npcId);

            if (list.Count == 0)
            {
                UI.Label(container, UI_MENU, Message("UI.NoKitsAvailable", player.userID), 14, new UI4(0.015f, 0.88f, 0.99f, 0.92f), TextAnchor.MiddleLeft);
                return;
            }

            PlayerData.PlayerUsageData playerUsageData = playerData[player.userID];

            int max = Mathf.Min(list.Count, (page + 1) * 8);
            int count = 0;
            for (int i = page * 8; i < max; i++)                
            {
                CreateKitEntry(player, playerUsageData, container, list[i], count, page, npcId);                
                count += 1;
            }

            if (page > 0)
                UI.Button(container, UI_MENU, Configuration.Menu.Color1.Get, "◀\n\n◀\n\n◀", 16, new UI4(0.005f, 0.35f, 0.03f, 0.58f), $"kits.gridview page {page - 1} {npcId}");
            if (max < list.Count)
                UI.Button(container, UI_MENU, Configuration.Menu.Color1.Get, "▶\n\n▶\n\n▶", 16, new UI4(0.97f, 0.35f, 0.995f, 0.58f), $"kits.gridview page {page + 1} {npcId}");

            Facepunch.Pool.FreeList(ref list);
        }

        private void CreateKitEntry(BasePlayer player, PlayerData.PlayerUsageData playerUsageData, CuiElementContainer container, KitData.Kit kit, int index, int page, ulong npcId)
        {            
            UI4 position = KitAlign.Get(index);

            UI.Panel(container, UI_MENU, Configuration.Menu.Color4.Get, new UI4(position.xMin, position.yMax, position.xMax, position.yMax + 0.04f));
            UI.Label(container, UI_MENU, kit.Name, 14, new UI4(position.xMin, position.yMax, position.xMax, position.yMax + 0.04f));

            UI.Panel(container, UI_MENU, Configuration.Menu.Panel.Get, position);

            string imageId = string.IsNullOrEmpty(kit.KitImage) ? GetImage(DEFAULT_ICON) : GetImage(kit.Name);
            UI.Image(container, UI_MENU, imageId, new UI4(position.xMin + 0.005f, position.yMax - 0.3f, position.xMax - 0.005f, position.yMax - 0.0075f));
            
            UI.Button(container, UI_MENU, "0 0 0 0", string.Empty, 0, new UI4(position.xMin + 0.005f, position.yMax - 0.3f, position.xMax - 0.005f, position.yMax - 0.0075f), $"kits.gridview inspect {CommandSafe(kit.Name)} {page} {npcId}");

            string buttonText;
            string buttonCommand = string.Empty;
            string buttonColor;

            double cooldown = playerUsageData.GetCooldownRemaining(kit.Name);
            int currentUses = playerUsageData.GetKitUses(kit.Name);

            if (Configuration.AdminIgnoreRestrictions && IsAdmin(player))
            {
                buttonText = Message("UI.Redeem", player.userID);
                buttonColor = Configuration.Menu.Color2.Get;
                buttonCommand = $"kits.gridview redeem {CommandSafe(kit.Name)} {page} {npcId}";
            }
            else
            {
                if (!string.IsNullOrEmpty(kit.RequiredPermission) && !permission.UserHasPermission(player.UserIDString, kit.RequiredPermission))
                {
                    buttonText = Message("UI.NeedsPermission", player.userID);
                    buttonColor = Configuration.Menu.Disabled.Get;
                }
                else if (kit.Cooldown > 0 && cooldown > 0)
                {
                    UI.Label(container, UI_MENU, string.Format(Message("UI.Cooldown", player.userID), FormatTime(cooldown)), 12,
                        new UI4(position.xMin + 0.005f, position.yMin + 0.0475f, position.xMax - 0.005f, position.yMax - 0.3f), TextAnchor.MiddleLeft);

                    buttonText = Message("UI.OnCooldown", player.userID);
                    buttonColor = Configuration.Menu.Disabled.Get;
                }
                else if (kit.MaximumUses > 0 && currentUses >= kit.MaximumUses)
                {
                    buttonText = Message("UI.MaximumUses", player.userID);
                    buttonColor = Configuration.Menu.Disabled.Get;
                }
                else if (kit.Cost > 0)
                {
                    UI.Label(container, UI_MENU, string.Format(Message("UI.Cost", player.userID), kit.Cost, Message($"Cost.{_costType}", player.userID)), 12,
                        new UI4(position.xMin + 0.005f, position.yMin + 0.0475f, position.xMax - 0.005f, position.yMax - 0.3f), TextAnchor.MiddleLeft);

                    buttonText = Message("UI.Purchase", player.userID);
                    buttonColor = Configuration.Menu.Color2.Get;
                    buttonCommand = $"kits.gridview redeem {CommandSafe(kit.Name)} {page} {npcId}";
                }
                else
                {
                    buttonText = Message("UI.Redeem", player.userID);
                    buttonColor = Configuration.Menu.Color2.Get;
                    buttonCommand = $"kits.gridview redeem {CommandSafe(kit.Name)} {page} {npcId}";
                }
            }

            UI.Button(container, UI_MENU, buttonColor, buttonText, 14, 
                new UI4(position.xMin + 0.038f, position.yMin + 0.0075f, position.xMax - 0.005f, position.yMin + 0.0475f), buttonCommand);

            UI.Button(container, UI_MENU, ICON_BACKGROUND_COLOR, GetImage(MAGNIFY_ICON), 
                new UI4(position.xMin + 0.005f, position.yMin + 0.0075f, position.xMin + 0.033f, position.yMin + 0.0475f), $"kits.gridview inspect {CommandSafe(kit.Name)} {page} {npcId}");
        }
        #endregion

        #region Kit View       
        private void OpenKitView(BasePlayer player, string name, int page, ulong npcId)
        {
            KitData.Kit kit;
            if (!kitData.Find(name, out kit))
            {
                OpenKitGrid(player);
                return;
            }

            CuiElementContainer container = UI.Container(UI_MENU, "0 0 0 0.9", new UI4(0.2f, 0.15f, 0.8f, 0.85f), true, "Hud");

            UI.Panel(container, UI_MENU, Configuration.Menu.Panel.Get, new UI4(0.005f, 0.93f, 0.995f, 0.99f));

            UI.Label(container, UI_MENU, $"{Message("UI.Title", player.userID)} - {name}", 20, new UI4(0.015f, 0.93f, 0.99f, 0.99f), TextAnchor.MiddleLeft);

            UI.Button(container, UI_MENU, Configuration.Menu.Color3.Get, "<b>×</b>", 20, new UI4(0.9575f, 0.9375f, 0.99f, 0.9825f), $"kits.gridview page 0 {npcId}");

            bool isAdmin;
            if (isAdmin = IsAdmin(player))
            {
                UI.Button(container, UI_MENU, Configuration.Menu.Color2.Get, Message("UI.EditKit", player.userID), 14, new UI4(0.7525f, 0.9375f, 0.845f, 0.9825f), $"kits.edit {CommandSafe(name)}");
                UI.Button(container, UI_MENU, Configuration.Menu.Color2.Get, Message("UI.CreateNew", player.userID), 14, new UI4(0.85f, 0.9375f, 0.9525f, 0.9825f), "kits.create");
            }

            PlayerData.PlayerUsageData playerUsageData = playerData[player.userID];

            int i = -1;

            if (!string.IsNullOrEmpty(kit.KitImage))
            {
                UI.Image(container, UI_MENU, GetImage(kit.Name), new UI4(0.15f, 0.62f, 0.35f, 0.92f));
                i = 6;
            }

            AddTitleSperator(container, i += 1, Message("UI.Details", player.userID));
            AddLabelField(container, i += 1, Message("UI.Name", player.userID), kit.Name);

            if (!string.IsNullOrEmpty(kit.Description)) 
            {
                int descriptionSlots = Mathf.Min(Mathf.CeilToInt(((float)kit.Description.Length / 38f) / 1.25f), 4);
                AddLabelField(container, i += 1, Message("UI.Description", player.userID), kit.Description, descriptionSlots - 1);
                i += descriptionSlots - 1;
            }

            string buttonText = string.Empty;
            string buttonCommand = string.Empty;
            string buttonColor = string.Empty;
            
            if (kit.Cooldown != 0 || kit.MaximumUses != 0 || kit.Cost != 0)
            {
                AddTitleSperator(container, i += 1, Message("UI.Usage", player.userID));

                if (kit.MaximumUses != 0)
                {
                    int playerUses = playerUsageData.GetKitUses(kit.Name);

                    AddLabelField(container, i += 1, Message("UI.MaxUses", player.userID), kit.MaximumUses.ToString());
                    AddLabelField(container, i += 1, Message("UI.YourUses", player.userID), playerUses.ToString());

                    if (playerUses >= kit.MaximumUses)
                    {
                        buttonText = Message("UI.MaximumUses", player.userID);
                        buttonColor = Configuration.Menu.Disabled.Get;
                    }
                }
                if (kit.Cooldown != 0)
                {
                    double cooldownRemaining = playerUsageData.GetCooldownRemaining(kit.Name);

                    AddLabelField(container, i += 1, Message("UI.CooldownTime", player.userID), FormatTime(kit.Cooldown));
                    AddLabelField(container, i += 1, Message("UI.CooldownRemaining", player.userID), cooldownRemaining == 0 ? Message("UI.None", player.userID) : 
                                                                                                     FormatTime(cooldownRemaining));

                    if (string.IsNullOrEmpty(buttonText) && cooldownRemaining > 0)
                    {
                        buttonText = Message("UI.OnCooldown", player.userID);
                        buttonColor = Configuration.Menu.Disabled.Get;
                    }
                }
                if (kit.Cost != 0)
                {
                    AddLabelField(container, i += 1, Message("UI.PurchaseCost", player.userID), $"{kit.Cost} {(Message($"Cost.{_costType}", player.userID))}");

                    if (string.IsNullOrEmpty(buttonText))
                    {
                        buttonText = Message("UI.Purchase", player.userID);
                        buttonColor = Configuration.Menu.Color2.Get;
                        buttonCommand = $"kits.gridview redeem {CommandSafe(kit.Name)} {page} {npcId}";
                    }
                }
            }

            if (!string.IsNullOrEmpty(kit.RequiredPermission) && !permission.UserHasPermission(player.UserIDString, kit.RequiredPermission))
            {
                buttonText = Message("UI.NeedsPermission", player.userID);
                buttonColor = Configuration.Menu.Disabled.Get;
            }
            
            if (i <= 16 && !string.IsNullOrEmpty(kit.CopyPasteFile))
            {
                AddTitleSperator(container, i += 1, Message("UI.CopyPaste", player.userID));
                AddLabelField(container, i += 1, Message("UI.FileName", player.userID), kit.CopyPasteFile);
            }

            if ((Configuration.AdminIgnoreRestrictions && isAdmin) || string.IsNullOrEmpty(buttonText))
            {
                buttonText = Message("UI.Redeem", player.userID);
                buttonCommand = $"kits.gridview redeem {CommandSafe(kit.Name)} {page} {npcId}";
                buttonColor = Configuration.Menu.Color2.Get;
            }

            CreateKitLayout(player, container, kit);

            UI.Button(container, UI_MENU, buttonColor, buttonText, 14, new UI4(0.005f, 0.005f, 0.495f, 0.045f), buttonCommand);

            CuiHelper.DestroyUi(player, UI_MENU);
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Kit Layout
        private const string ICON_BACKGROUND_COLOR = "1 1 1 0.15";

        private void CreateKitLayout(BasePlayer player, CuiElementContainer container, KitData.Kit kit)
        {
            UI.Panel(container, UI_MENU, Configuration.Menu.Color1.Get, new UI4(0.505f, 0.88f, 0.995f, 0.92f));
            UI.Label(container, UI_MENU, Message("UI.KitItems", player.userID), 14, new UI4(0.51f, 0.88f, 0.995f, 0.92f), TextAnchor.MiddleLeft);

            // Main Items
            UI.Panel(container, UI_MENU, Configuration.Menu.Color4.Get, new UI4(0.505f, 0.835f, 0.995f, 0.875f));
            UI.Label(container, UI_MENU, Message("UI.MainItems", player.userID), 14, new UI4(0.51f, 0.835f, 0.995f, 0.875f), TextAnchor.MiddleLeft);
            CreateInventoryItems(container, MainAlign, kit.MainItems, 24);
            
            // Wear Items
            UI.Panel(container, UI_MENU, Configuration.Menu.Color4.Get, new UI4(0.505f, 0.365f, 0.995f, 0.405f));
            UI.Label(container, UI_MENU, Message("UI.WearItems", player.userID), 14, new UI4(0.51f, 0.365f, 0.995f, 0.405f), TextAnchor.MiddleLeft);
            CreateInventoryItems(container, WearAlign, kit.WearItems, 7);
            
            // Belt Items
            UI.Panel(container, UI_MENU, Configuration.Menu.Color4.Get, new UI4(0.505f, 0.21f, 0.995f, 0.25f));
            UI.Label(container, UI_MENU, Message("UI.BeltItems", player.userID), 14, new UI4(0.51f, 0.21f, 0.995f, 0.25f), TextAnchor.MiddleLeft);
            CreateInventoryItems(container, BeltAlign, kit.BeltItems, 6);            
        }

        #region Item Layout Helpers
        private void CreateInventoryItems(CuiElementContainer container, GridAlignment alignment, ItemData[] items, int capacity)
        {
            for (int i = 0; i < capacity; i++)
                UI.Panel(container, UI_MENU, ICON_BACKGROUND_COLOR, alignment.Get(i));

            for (int i = 0; i < items.Length; i++)
            {
                ItemData itemData = items[i];
                if (itemData.Position > capacity - 1)
                    continue;

                UI4 position = alignment.Get(itemData.Position);

                UI.Image(container, UI_MENU, itemData.ItemID, itemData.Skin /*GetImage(itemData.Shortname, itemData.Skin)*/, position);

                if (itemData.IsBlueprint && !string.IsNullOrEmpty(itemData.BlueprintShortname))
                    UI.Image(container, UI_MENU, itemData.BlueprintItemID, 0UL /*GetImage(itemData.BlueprintShortname, 0UL)*/, position);

                if (itemData.Amount > 1)
                    UI.Label(container, UI_MENU, $"x{itemData.Amount}", 10, position, TextAnchor.LowerRight);
            }
        }
        #endregion
        #endregion

        #region Kit Editor
        private void OpenKitsEditor(BasePlayer player, bool overwrite = false)
        {
            KitData.Kit kit;
            if (!_kitCreators.TryGetValue(player.userID, out kit))
                return;

            CuiElementContainer container = UI.Container(UI_MENU, "0 0 0 0.9", new UI4(0.2f, 0.15f, 0.8f, 0.85f), true, "Hud");

            UI.Panel(container, UI_MENU, Configuration.Menu.Panel.Get, new UI4(0.005f, 0.93f, 0.995f, 0.99f));

            UI.Label(container, UI_MENU, Message("UI.Title.Editor", player.userID), 20, new UI4(0.015f, 0.93f, 0.99f, 0.99f), TextAnchor.MiddleLeft);

            UI.Button(container, UI_MENU, Configuration.Menu.Color3.Get, "<b>×</b>", 20, new UI4(0.9575f, 0.9375f, 0.99f, 0.9825f), "kits.close");

            // Kit Options
            AddTitleSperator(container, 0, Message("UI.Details", player.userID));
            AddInputField(container, 1, Message("UI.Name", player.userID), "name", kit.Name);
            AddInputField(container, 2, Message("UI.Description", player.userID), "description", kit.Description, 3);
            AddInputField(container, 6, Message("UI.IconURL", player.userID), "image", kit.KitImage);

            AddTitleSperator(container, 7, Message("UI.UsageAuthority", player.userID));
            AddInputField(container, 8, Message("UI.Permission", player.userID), "permission", kit.RequiredPermission);
            AddInputField(container, 9, Message("UI.AuthLevel", player.userID), "authLevel", kit.RequiredAuth);
            AddToggleField(container, 10, Message("UI.IsHidden", player.userID), "isHidden", kit.IsHidden);

            AddTitleSperator(container, 11, Message("UI.Usage", player.userID));
            AddInputField(container, 12, Message("UI.MaxUses", player.userID), "maximumUses", kit.MaximumUses);
            AddInputField(container, 13, Message("UI.CooldownSeconds", player.userID), "cooldown", kit.Cooldown);
            AddInputField(container, 14, Message("UI.PurchaseCost", player.userID), "cost", kit.Cost);

            AddTitleSperator(container, 15, Message("UI.CopyPaste", player.userID));
            AddInputField(container, 16, Message("UI.FileName", player.userID), "copyPaste", kit.CopyPasteFile);

            // Kit Items
            CreateKitLayout(player, container, kit);

            // Kit Saving            
            UI.Button(container, UI_MENU, Configuration.Menu.Color2.Get, Message("UI.SaveKit", player.userID), 14, new UI4(0.005f, 0.005f, 0.2475f, 0.045f), $"kits.savekit {overwrite}");
            UI.Toggle(container, UI_MENU, ICON_BACKGROUND_COLOR, 14, new UI4(0.2525f, 0.005f, 0.2825f, 0.045f), $"kits.toggleoverwrite {overwrite}", overwrite);
            UI.Label(container, UI_MENU, Message("UI.Overwrite", player.userID), 14, new UI4(0.2875f, 0.005f, 0.495f, 0.045f), TextAnchor.MiddleLeft);

            // Item Management            
            UI.Button(container, UI_MENU, Configuration.Menu.Color3.Get, Message("UI.ClearItems", player.userID), 14, new UI4(0.505f, 0.005f, 0.7475f, 0.045f), $"kits.clearitems {overwrite}");
            UI.Button(container, UI_MENU, Configuration.Menu.Color2.Get, Message("UI.CopyInv", player.userID), 14, new UI4(0.7525f, 0.005f, 0.995f, 0.045f), $"kits.copyinv {overwrite}");

            CuiHelper.DestroyUi(player, UI_MENU);
            CuiHelper.AddUi(player, container);
        }

        #region Editor Helpers
        private const float EDITOR_ELEMENT_HEIGHT = 0.04f;

        private void AddInputField(CuiElementContainer container, int index, string title, string fieldName, object currentValue, int additionalHeight = 0)
        {
            float yMin = GetVerticalPos(index, 0.88f);
            float yMax = yMin + EDITOR_ELEMENT_HEIGHT;

            if (additionalHeight != 0)
                yMin = GetVerticalPos(index + additionalHeight, 0.88f);

            UI.Panel(container, UI_MENU, Configuration.Menu.Color4.Get, new UI4(0.005f, yMin, 0.175f, yMax));
            UI.Label(container, UI_MENU, title, 12, new UI4(0.01f, yMin, 0.175f, yMax - 0.0075f), TextAnchor.UpperLeft);

            UI.Panel(container, UI_MENU, ICON_BACKGROUND_COLOR, new UI4(0.175f, yMin, 0.495f, yMax));

            string label = GetInputLabel(currentValue);
            if (!string.IsNullOrEmpty(label))
            {
                UI.Label(container, UI_MENU, label, 12, new UI4(0.18f, yMin, 0.47f, yMax - 0.0075f), TextAnchor.UpperLeft);
                UI.Button(container, UI_MENU, Configuration.Menu.Color3.Get, "X", 14, new UI4(0.47f, yMax - EDITOR_ELEMENT_HEIGHT, 0.495f, yMax), $"kits.clear {fieldName}");
            }
            else UI.Input(container, UI_MENU, string.Empty, 12, $"kits.creator {fieldName}", new UI4(0.18f, yMin, 0.495f, yMax - 0.0075f), TextAnchor.UpperLeft);
        }

        private void AddTitleSperator(CuiElementContainer container, int index, string title)
        {
            float yMin = GetVerticalPos(index, 0.88f);
            float yMax = yMin + EDITOR_ELEMENT_HEIGHT;

            UI.Panel(container, UI_MENU, Configuration.Menu.Color1.Get, new UI4(0.005f, yMin, 0.495f, yMax));
            UI.Label(container, UI_MENU, title, 14, new UI4(0.01f, yMin, 0.495f, yMax), TextAnchor.MiddleLeft);
        }

        private void AddLabelField(CuiElementContainer container, int index, string title, string value, int additionalHeight = 0)
        {
            float yMin = GetVerticalPos(index, 0.88f);
            float yMax = yMin + EDITOR_ELEMENT_HEIGHT;

            if (additionalHeight != 0)
                yMin = GetVerticalPos(index + additionalHeight, 0.88f);

            UI.Panel(container, UI_MENU, Configuration.Menu.Color4.Get, new UI4(0.005f, yMin, 0.175f, yMax));
            UI.Label(container, UI_MENU, title, 12, new UI4(0.01f, yMin, 0.175f, yMax - 0.0075f), TextAnchor.UpperLeft);

            UI.Panel(container, UI_MENU, ICON_BACKGROUND_COLOR, new UI4(0.175f, yMin, 0.495f, yMax));
            UI.Label(container, UI_MENU, value, 12, new UI4(0.18f, yMin, 0.495f, yMax - 0.0075f), TextAnchor.UpperLeft);
        }

        private void AddToggleField(CuiElementContainer container, int index, string title, string fieldName, bool currentValue)
        {
            float yMin = GetVerticalPos(index, 0.88f);
            float yMax = yMin + EDITOR_ELEMENT_HEIGHT;

            UI.Panel(container, UI_MENU, Configuration.Menu.Color4.Get, new UI4(0.005f, yMin, 0.175f, yMax));
            UI.Label(container, UI_MENU, title, 14, new UI4(0.01f, yMin, 0.175f, yMax), TextAnchor.MiddleLeft);
            UI.Toggle(container, UI_MENU, ICON_BACKGROUND_COLOR, 14, new UI4(0.175f, yMin, 0.205f, yMax), $"kits.creator {fieldName} {!currentValue}", currentValue);
        }

        private string GetInputLabel(object obj)
        {
            if (obj is string)
                return string.IsNullOrEmpty(obj as string) ? null : obj.ToString();
            else if (obj is int)
                return (int)obj <= 0 ? null : obj.ToString();
            else if (obj is float)
                return (float)obj <= 0 ? null : obj.ToString();
            return null;
        }

        private float GetVerticalPos(int i, float start = 0.9f) => start - (i * (EDITOR_ELEMENT_HEIGHT + 0.005f));
        #endregion
        #endregion

        #region Popup Messages
        private void CreateMenuPopup(BasePlayer player, string text, float duration = 5f)
        {
            CuiElementContainer container = UI.Container(UI_POPUP, Configuration.Menu.Color4.Get, new UI4(0.2f, 0.11f, 0.8f, 0.15f));
            UI.Label(container, UI_POPUP, text, 14, UI4.Full);

            CuiHelper.DestroyUi(player, UI_POPUP);
            CuiHelper.AddUi(player, container);

            player.Invoke(() => CuiHelper.DestroyUi(player, UI_POPUP), duration);
        }
        #endregion

        #region UI Grid Helper
        private readonly GridAlignment KitAlign = new GridAlignment(4, 0.04f, 0.2f, 0.04f, 0.87f, 0.39f, 0.06f);

        private readonly GridAlignment MainAlign = new GridAlignment(6, 0.545f, 0.065f, 0.0035f, 0.8275f, 0.1f, 0.005f);
        private readonly GridAlignment WearAlign = new GridAlignment(7, 0.51f, 0.065f, 0.0035f, 0.3575f, 0.1f, 0.005f);
        private readonly GridAlignment BeltAlign = new GridAlignment(6, 0.545f, 0.065f, 0.0035f, 0.2025f, 0.1f, 0.005f);

        private class GridAlignment
        {
            internal int Columns { get; set; }
            internal float XOffset { get; set; }
            internal float Width { get; set; }
            internal float XSpacing { get; set; }
            internal float YOffset { get; set; }
            internal float Height { get; set; }
            internal float YSpacing { get; set; }

            internal GridAlignment(int columns, float xOffset, float width, float xSpacing, float yOffset, float height, float ySpacing)
            {
                Columns = columns;
                XOffset = xOffset;
                Width = width;
                XSpacing = xSpacing;
                YOffset = yOffset;
                Height = height;
                YSpacing = ySpacing;
            }

            internal UI4 Get(int index)
            {
                int rowNumber = index == 0 ? 0 : Mathf.FloorToInt(index / Columns);
                int columnNumber = index - (rowNumber * Columns);

                float offsetX = XOffset + (Width * columnNumber) + (XSpacing * columnNumber);

                float offsetY = (YOffset - (rowNumber * Height) - (YSpacing * rowNumber));

                return new UI4(offsetX, offsetY - Height, offsetX + Width, offsetY);
            }
        }
        #endregion
        #endregion

        #region UI Commands
        #region View Commands
        [ConsoleCommand("kits.close")]
        private void ccmdKitsClose(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            _kitCreators.Remove(player.userID);

            CuiHelper.DestroyUi(player, UI_MENU);
            CuiHelper.DestroyUi(player, UI_POPUP);
        }

        [ConsoleCommand("kits.gridview")]
        private void ccmdKitsGridView(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            switch (arg.GetString(0).ToLower())
            {
                case "page":
                    OpenKitGrid(player, arg.GetInt(1), arg.GetULong(2));
                    return;
                case "inspect":                    
                    OpenKitView(player, CommandSafe(arg.GetString(1), true), arg.GetInt(2), arg.GetULong(3));                    
                    return;
                case "redeem":
                    {
                        string kit = CommandSafe(arg.GetString(1), true);
                        if (TryClaimKit(player, kit, true))
                        {
                            CuiHelper.DestroyUi(player, UI_MENU);
                            CuiHelper.DestroyUi(player, UI_POPUP);
                            player.ChatMessage(string.Format(Message("Notification.KitReceived", player.userID), kit));
                        }
                        else OpenKitGrid(player, arg.GetInt(2), arg.GetULong(3));
                    }
                    return;
                default:
                    break;
            }            
        }
        #endregion

        #region Editor Commands
        [ConsoleCommand("kits.create")]
        private void ccmdCreateKit(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null)
                return;

            if (IsAdmin(player))
            {
                _kitCreators[player.userID] = new KitData.Kit();
                OpenKitsEditor(player);
            }
        }

        [ConsoleCommand("kits.edit")]
        private void ccmdEditKit(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null)
                return;

            if (IsAdmin(player))
            {
                string name = CommandSafe(arg.GetString(0), true);

                KitData.Kit editKit;
                if (!kitData.Find(name, out editKit))
                {
                    player.ChatMessage(string.Format(Message("Chat.Error.DoesntExist", player.userID), name));
                    return;
                }

                _kitCreators[player.userID] = KitData.Kit.CloneOf(editKit);
                OpenKitsEditor(player);
            }
        }

        [ConsoleCommand("kits.savekit")]
        private void ccmdSaveKit(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null)
                return;

            KitData.Kit kit;
            if (!_kitCreators.TryGetValue(player.userID, out kit))
                return;

            if (string.IsNullOrEmpty(kit.Name))
            {
                CreateMenuPopup(player, Message("SaveKit.Error.NoName", player.userID));
                return;
            }

            if (kit.ItemCount == 0 && string.IsNullOrEmpty(kit.CopyPasteFile))
            {
                CreateMenuPopup(player, Message("SaveKit.Error.NoContents", player.userID));
                return;
            }

            if (kitData.Exists(kit.Name) && !arg.GetBool(0))
            {
                CreateMenuPopup(player, Message("SaveKit.Error.Exists", player.userID));
                return;
            }

            kitData[kit.Name] = kit;
            SaveKitData();

            _kitCreators.Remove(player.userID);

            if (!string.IsNullOrEmpty(kit.RequiredPermission) && !permission.PermissionExists(kit.RequiredPermission))
                permission.RegisterPermission(kit.RequiredPermission, this);

            if (!string.IsNullOrEmpty(kit.KitImage))
                RegisterImage(kit.Name, kit.KitImage);

            OpenKitView(player, kit.Name, 0, 0UL);
            CreateMenuPopup(player, string.Format(Message("SaveKit.Success", player.userID), kit.Name));
        }

        [ConsoleCommand("kits.toggleoverwrite")]
        private void ccmdToggleOverwrite(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null)
                return;

            KitData.Kit kit;
            if (!_kitCreators.TryGetValue(player.userID, out kit))
                return;

            OpenKitsEditor(player, !arg.GetBool(0));
        }

        [ConsoleCommand("kits.clearitems")]
        private void ccmdClearItems(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null)
                return;

            KitData.Kit kit;
            if (!_kitCreators.TryGetValue(player.userID, out kit))
                return;

            kit.ClearItems();

            OpenKitsEditor(player);
        }

        [ConsoleCommand("kits.copyinv")]
        private void ccmdCopyInv(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null)
                return;

            KitData.Kit kit;
            if (!_kitCreators.TryGetValue(player.userID, out kit))
                return;

            kit.CopyItemsFrom(player);

            OpenKitsEditor(player);
        }

        [ConsoleCommand("kits.clear")]
        private void ccmdClearField(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null)
                return;

            KitData.Kit kit;
            if (!_kitCreators.TryGetValue(player.userID, out kit))
                return;

            string fieldName = arg.GetString(0);

            switch (fieldName)
            {
                case "name":
                    kit.Name = string.Empty;
                    break;
                case "description":
                    kit.Description = string.Empty;
                    break;
                case "copyPaste":
                    kit.CopyPasteFile = string.Empty;
                    break;
                case "permission":
                    kit.RequiredPermission = string.Empty;
                    break;
                case "image":
                    kit.KitImage = string.Empty;
                    break;
                case "cost":
                    kit.Cost = 0;
                    break;
                case "cooldown":
                    kit.Cooldown = 0;
                    break;                
                case "maximumUses":
                    kit.MaximumUses = 0;
                    break;
                case "authLevel":
                    kit.RequiredAuth = 0;
                    break;
                
                default:                    
                    break;
            }

            OpenKitsEditor(player);
        }

        [ConsoleCommand("kits.creator")]
        private void ccmdSetField(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null)
                return;

            KitData.Kit kit;
            if (!_kitCreators.TryGetValue(player.userID, out kit))
                return;

            if (arg.HasArgs(2))
            {
                SetParameter(player, kit, arg.GetString(0), string.Join(" ", arg.Args.Skip(1)));
                OpenKitsEditor(player);
            }
        }

        private void SetParameter(BasePlayer player, KitData.Kit kit, string fieldName, object value)
        {
            if (value == null)
                return;

            switch (fieldName)
            {
                case "name":
                    kit.Name = (string)value;
                    break;
                case "description":
                    kit.Description = (string)value;
                    break;
                case "copyPaste":
                    kit.CopyPasteFile = (string)value;
                    break;
                case "permission":
                    if (!((string)value).StartsWith("kits."))
                    {
                        CreateMenuPopup(player, Message("EditKit.PermissionPrefix", player.userID));
                        return;
                    }
                    kit.RequiredPermission = (string)value;
                    break;
                case "image":
                    kit.KitImage = (string)value;
                    break;
                case "cost":
                    {
                        int intValue;
                        if (!TryConvertValue<int>(value, out intValue))
                            CreateMenuPopup(player, Message("EditKit.Number", player.userID));
                        else kit.Cost = intValue;
                    }
                    break;
                case "cooldown":
                    {
                        int intValue;
                        if (!TryConvertValue<int>(value, out intValue))
                            CreateMenuPopup(player, Message("EditKit.Number", player.userID));
                        else kit.Cooldown = intValue;
                    }
                    break;
                case "maximumUses":
                    {
                        int intValue;
                        if (!TryConvertValue<int>(value, out intValue))
                            CreateMenuPopup(player, Message("EditKit.Number", player.userID));
                        else kit.MaximumUses = intValue;
                    }
                    break;
                case "authLevel":
                    {
                        int intValue;
                        if (!TryConvertValue<int>(value, out intValue))
                            CreateMenuPopup(player, Message("EditKit.Number", player.userID));
                        else kit.RequiredAuth = Mathf.Clamp(intValue, 0, 2);                        
                    }
                    break;
                case "isHidden":
                    {
                        bool boolValue;
                        if (!TryConvertValue<bool>(value, out boolValue))
                            CreateMenuPopup(player, Message("EditKit.Bool", player.userID));
                        else kit.IsHidden = boolValue;
                    }
                    break;                
                default:                    
                    return;
            }
        }

        private bool TryConvertValue<T>(object value, out T result)
        {
            try
            {
                result = (T)Convert.ChangeType(value, typeof(T));
                return true;
            }
            catch
            {
                result = default(T);
                return false;
            }
        }
        #endregion
        #endregion

        #region Command Helpers
        private static string CommandSafe(string text, bool unpack = false) => unpack ? text.Replace("▊▊", " ") : text.Replace(" ", "▊▊");
        #endregion

        #region UI Helper
        public static class UI
        {
            public static CuiElementContainer Container(string panel, string color, UI4 dimensions, bool blur = true, string parent = "Overlay")
            {
                CuiElementContainer container = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = { Color = color, Material = blur ? "assets/content/ui/uibackgroundblur-ingamemenu.mat" : string.Empty },
                            RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() },
                            CursorEnabled = true
                        },
                        new CuiElement().Parent = parent,
                        panel
                    }
                };
                return container;
            }

            public static CuiElementContainer Popup(string panel, string text, int size, UI4 dimensions, TextAnchor align = TextAnchor.MiddleCenter, string parent = "Overlay")
            {
                CuiElementContainer container = UI.Container(panel, "0 0 0 0", dimensions);

                UI.Label(container, panel, text, size, UI4.Full, align);

                return container;
            }

            public static void Panel(CuiElementContainer container, string panel, string color, UI4 dimensions)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                },
                panel);
            }

            public static void Label(CuiElementContainer container, string panel, string text, int size, UI4 dimensions, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                },
                panel);
            }

            public static void Button(CuiElementContainer container, string panel, string color, string text, int size, UI4 dimensions, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 0f },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            }

            public static void Button(CuiElementContainer container, string panel, string color, string png, UI4 dimensions, string command)
            {
                UI.Panel(container, panel, color, dimensions);
                UI.Image(container, panel, png, dimensions);
                UI.Button(container, panel, "0 0 0 0", string.Empty, 0, dimensions, command);
            }

            public static void Input(CuiElementContainer container, string panel, string text, int size, string command, UI4 dimensions, TextAnchor anchor = TextAnchor.MiddleLeft)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            Align = anchor,                            
                            CharsLimit = 300,
                            Command = command + text,
                            FontSize = size,
                            IsPassword = false,
                            Text = text,
                            NeedsKeyboard = true
                        },
                        new CuiRectTransformComponent {AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                    }
                });
            }

            public static void Image(CuiElementContainer container, string panel, string png, UI4 dimensions)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent {Png = png },
                        new CuiRectTransformComponent { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                    }
                });
            }

            public static void Image(CuiElementContainer container, string panel, int itemId, ulong skinId, UI4 dimensions)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiImageComponent { ItemId = itemId, SkinId = skinId },
                        new CuiRectTransformComponent { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                    }
                });
            }

            public static void Toggle(CuiElementContainer container, string panel, string boxColor, int fontSize, UI4 dimensions, string command, bool isOn)
            {
                UI.Panel(container, panel, boxColor, dimensions);

                if (isOn)
                    UI.Label(container, panel, "✔", fontSize, dimensions);

                UI.Button(container, panel, "0 0 0 0", string.Empty, 0, dimensions, command);
            }

            public static string Color(string hexColor, float alpha)
            {
                if (hexColor.StartsWith("#"))
                    hexColor = hexColor.TrimStart('#');

                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);

                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }

        public class UI4
        {
            public float xMin, yMin, xMax, yMax;

            public UI4(float xMin, float yMin, float xMax, float yMax)
            {
                this.xMin = xMin;
                this.yMin = yMin;
                this.xMax = xMax;
                this.yMax = yMax;
            }

            public string GetMin() => $"{xMin} {yMin}";

            public string GetMax() => $"{xMax} {yMax}";

            private static UI4 _full;

            public static UI4 Full
            {
                get
                {
                    if (_full == null)
                        _full = new UI4(0, 0, 1, 1);
                    return _full;
                }
            }
        }
        #endregion
                
        #region Chat Commands       
        private void cmdKit(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                if (Configuration.UseUI)
                    OpenKitGrid(player);
                else ReplyHelp(player);

                return;
            }

            bool isAdmin = IsAdmin(player);

            switch (args[0].ToLower())
            {
                case "help":
                    ReplyHelp(player);
                    return;

                case "list":
                    if (isAdmin)                    
                        player.ChatMessage(string.Format(Message("Chat.KitList", player.userID), kitData.Keys.ToSentence()));
                    else
                    {
                        List<KitData.Kit> kits = Facepunch.Pool.GetList<KitData.Kit>();
                        GetUserValidKits(player, kits);

                        player.ChatMessage(string.Format(Message("Chat.KitList", player.userID), kits.Select((KitData.Kit kit) => kit.Name).ToSentence()));
                        Facepunch.Pool.FreeList(ref kits);
                    }  
                    return;

                case "add":
                case "new":
                    if (!isAdmin)
                    {
                        player.ChatMessage(Message("Chat.Error.NotAdmin", player.userID));
                        return;
                    }

                    _kitCreators[player.userID] = new KitData.Kit();
                    OpenKitsEditor(player);

                    return;

                case "edit":
                    if (!isAdmin)
                    {
                        player.ChatMessage(Message("Chat.Error.NotAdmin", player.userID));
                        return;
                    }

                    if (args.Length != 2)
                    {
                        player.ChatMessage(Message("Chat.Error.NoKit", player.userID));
                        return;
                    }

                    KitData.Kit editKit;
                    if (!kitData.Find(args[1], out editKit))
                    {
                        player.ChatMessage(string.Format(Message("Chat.Error.DoesntExist", player.userID), args[1]));
                        return;
                    }

                    _kitCreators[player.userID] = KitData.Kit.CloneOf(editKit);
                    OpenKitsEditor(player);

                    return;

                case "remove":
                case "delete":
                    if (!isAdmin)
                    {
                        player.ChatMessage(Message("Chat.Error.NotAdmin", player.userID));
                        return;
                    }

                    if (args.Length != 2)
                    {
                        player.ChatMessage(Message("Chat.Error.NoKit", player.userID));
                        return;
                    }

                    KitData.Kit deleteKit;
                    if (!kitData.Find(args[1], out deleteKit))
                    {
                        player.ChatMessage(string.Format(Message("Chat.Error.DoesntExist", player.userID), args[1]));
                        return;
                    }

                    kitData.Remove(deleteKit);
                    SaveKitData();
                    player.ChatMessage(string.Format(Message("Chat.KitDeleted", player.userID), args[1]));

                    return;

                case "give":
                    if (!isAdmin)
                    {
                        player.ChatMessage(Message("Chat.Error.NotAdmin", player.userID));
                        return;
                    }

                    if (args.Length != 3)
                    {
                        player.ChatMessage(Message("Chat.Error.GiveArgs", player.userID));
                        return;
                    }

                    BasePlayer target = FindPlayer(args[1]);
                    if (target == null)
                    {
                        player.ChatMessage(Message("Chat.Error.NoPlayer", player.userID));
                        return;
                    }

                    KitData.Kit giveKit;
                    if (!kitData.Find(args[2], out giveKit))
                    {
                        player.ChatMessage(Message("Chat.Error.DoesntExist", player.userID));
                        return;
                    }

                    GiveKit(target, giveKit);
                    player.ChatMessage(string.Format(Message("Chat.KitGiven", player.userID), target.displayName, args[2]));
                    return;

                case "givenpc":
                    if (!isAdmin)
                    {
                        player.ChatMessage(Message("Chat.Error.NotAdmin", player.userID));
                        return;
                    }

                    if (args.Length != 2)
                    {
                        player.ChatMessage(Message("Chat.Error.NPCGiveArgs", player.userID));
                        return;
                    }

                    KitData.Kit npcGiveKit;
                    if (!kitData.Find(args[1], out npcGiveKit))
                    {
                        player.ChatMessage(Message("Chat.Error.DoesntExist", player.userID));
                        return;
                    }

                    BasePlayer npc = RaycastPlayer(player);
                    if (npc == null)
                    {
                        player.ChatMessage(Message("Chat.Error.NoNPCTarget", player.userID));
                        return;
                    }

                    npc.inventory.Strip();
                    GiveKit(npc, npcGiveKit);

                    player.ChatMessage(string.Format(Message("Chat.KitGiven", player.userID), npc.displayName, args[1]));
                    return;

                case "reset":
                    if (!isAdmin)
                    {
                        player.ChatMessage(Message("Chat.Error.NotAdmin", player.userID));
                        return;
                    }

                    playerData.Wipe();
                    SavePlayerData();
                    player.ChatMessage(Message("Chat.ResetPlayers", player.userID));

                    return;

                case "autokit":
                    if (Configuration.AllowAutoToggle)
                    {
                        bool v = playerData[player.userID].ClaimAutoKits = !playerData[player.userID].ClaimAutoKits;
                        player.ChatMessage(string.Format(Message("Chat.AutoKit.Toggle", player.userID), Message($"Chat.AutoKit.{v}", player.userID)));
                    }
                    return;

                default:
                    if (!kitData.Exists(args[0]))
                    {
                        player.ChatMessage(string.Format(Message("Chat.Error.DoesntExist", player.userID), args[0]));
                        return;
                    }

                    if (TryClaimKit(player, args[0], false))
                        player.ChatMessage(string.Format(Message("Notification.KitReceived", player.userID), args[0]));

                    break;
            }
        }

        private void ccmdKit(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection?.player as BasePlayer;
            if (player != null && !IsAdmin(player))
                return;

            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "kit list - List all kits");
                SendReply(arg, "kit delete <kitname> - Delete the specified kit");
                SendReply(arg, "kit give <playername> <kitname> - Give the specified kit to the specified playuer");
                SendReply(arg, "kit reset - Reset player usage data");
                return;
            }

            switch (arg.Args[0].ToLower())
            {               
                case "list":
                    SendReply(arg, string.Format("Kit List: {0}", kitData.Keys.ToSentence()));
                    return;

                case "remove":
                case "delete":                    
                    if (arg.Args.Length != 2)
                    {
                        SendReply(arg, "You must specify a kit name");
                        return;
                    }

                    KitData.Kit deleteKit;
                    if (!kitData.Find(arg.Args[1], out deleteKit))
                    {
                        SendReply(arg, string.Format("The kit {0} does not exist", arg.Args[1]));
                        return;
                    }

                    kitData.Remove(deleteKit);
                    SaveKitData();
                    SendReply(arg, string.Format("You have deleted the kit {0}", arg.Args[1]));

                    return;

                case "give":                   
                    if (arg.Args.Length != 3)
                    {
                        SendReply(arg, "You must specify target player and a kit name");
                        return;
                    }

                    BasePlayer target = FindPlayer(arg.Args[1]);
                    if (target == null)
                    {
                        SendReply(arg, "Failed to find a player with the specified name or ID");
                        return;
                    }

                    KitData.Kit giveKit;
                    if (!kitData.Find(arg.Args[2], out giveKit))
                    {
                        SendReply(arg, "The kit {0} does not exist");
                        return;
                    }

                    GiveKit(target, giveKit);
                    SendReply(arg, string.Format("You have given {0} the kit {1}", target.displayName, arg.Args[2]));
                    return;
                
                case "reset":                    
                    playerData.Wipe();
                    SavePlayerData();
                    SendReply(arg, "You have wiped player usage data");
                    return;
                
                default:
                    SendReply(arg, "Invalid syntax");
                    break;
            }
        }

        private void ReplyHelp(BasePlayer player)
        {
            player.ChatMessage(string.Format(Message("Chat.Help.Title", player.userID), Version));
            player.ChatMessage(Message("Chat.Help.1", player.userID));
            player.ChatMessage(Message("Chat.Help.2", player.userID));
            
            if (Configuration.AllowAutoToggle)
                player.ChatMessage(Message("Chat.Help.9", player.userID));

            if (IsAdmin(player))
            {
                player.ChatMessage(Message("Chat.Help.3", player.userID));
                player.ChatMessage(Message("Chat.Help.4", player.userID));
                player.ChatMessage(Message("Chat.Help.5", player.userID));
                player.ChatMessage(Message("Chat.Help.6", player.userID));
                player.ChatMessage(Message("Chat.Help.7", player.userID));
                player.ChatMessage(Message("Chat.Help.8", player.userID));
                player.ChatMessage(Message("Chat.Help.10", player.userID));
            }
        }
        #endregion

        #region Old Data Conversion
        [ConsoleCommand("kits.convertolddata")]
        private void ccmdConvertKitsData(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg?.Connection?.player as BasePlayer;
            if (player != null)
                return;

            ConvertOldKitData();
        }

        [ConsoleCommand("kits.convertoldplayerdata")]
        private void ccmdConvertPlayerData(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg?.Connection?.player as BasePlayer;
            if (player != null)
                return;

            ConvertOldPlayerData();
        }

        private void ConvertOldPlayerData()
        {
            try
            {
                Dictionary<ulong, Dictionary<string, OldKitData>> oldPlayerData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<string, OldKitData>>>("Kits_Data");

                if (oldPlayerData != null)
                {
                    int success = 0;

                    foreach (KeyValuePair<ulong, Dictionary<string, OldKitData>> oldPlayer in oldPlayerData)
                    {
                        PlayerData.PlayerUsageData playerUsageData = playerData[oldPlayer.Key];

                        foreach(KeyValuePair<string, OldKitData> oldUsageData in oldPlayer.Value)
                        {
                            if (kitData.Exists(oldUsageData.Key))                            
                                playerUsageData.InsertOldData(oldUsageData.Key, oldUsageData.Value.max, oldUsageData.Value.cooldown);                            
                        }

                        success++;
                    }

                    if (success > 0)
                        SavePlayerData();

                    Debug.Log($"Successfully converted {success} / {oldPlayerData.Count} player's data");
                }
            }
            catch { }
        }

        private void ConvertOldKitData()
        {            
            try
            {
                DynamicConfigFile kits = Interface.Oxide.DataFileSystem.GetFile("Kits");
                kits.Settings.NullValueHandling = NullValueHandling.Ignore;
                OldStoredData oldStoredData = kits.ReadObject<OldStoredData>();

                int success = 0;

                foreach (OldKit oldKit in oldStoredData.Kits.Values)
                {
                    Debug.Log($"Converting Kit {oldKit.name} with {oldKit.items.Count} items");
                    KitData.Kit kit = new KitData.Kit()
                    {
                        Name = oldKit.name,
                        Description = oldKit.description ?? string.Empty,
                        Cooldown = Convert.ToInt32(oldKit.cooldown),
                        CopyPasteFile = oldKit.building ?? string.Empty,
                        Cost = 0,
                        IsHidden = oldKit.hide,
                        KitImage = oldKit.image ?? string.Empty,
                        MaximumUses = oldKit.max,
                        RequiredAuth = oldKit.authlevel,
                        RequiredPermission = oldKit.permission ?? string.Empty
                    };

                    TryConvertItems(ref kit, oldKit.items);

                    kitData[oldKit.name] = kit;

                    success++;
                }

                if (success > 0)
                    SaveKitData();

                Debug.Log($"Successfully converted {success} / {oldStoredData.Kits.Count} kits");
            }
            catch { }
        }

        private void TryConvertItems(ref KitData.Kit kit, List<OldKitItem> items)
        {
            List<ItemData> wear = Facepunch.Pool.GetList<ItemData>();
            List<ItemData> belt = Facepunch.Pool.GetList<ItemData>();
            List<ItemData> main = Facepunch.Pool.GetList<ItemData>();

            ConvertItems(ref wear, items.Where((OldKitItem oldKitItem) => oldKitItem.container == "wear"));
            ConvertItems(ref belt, items.Where((OldKitItem oldKitItem) => oldKitItem.container == "belt"));
            ConvertItems(ref main, items.Where((OldKitItem oldKitItem) => oldKitItem.container == "main"));

            kit.WearItems = wear.ToArray();
            kit.BeltItems = belt.ToArray();
            kit.MainItems = main.ToArray();

            Facepunch.Pool.FreeList(ref wear);
            Facepunch.Pool.FreeList(ref belt);
            Facepunch.Pool.FreeList(ref main);
        } 

        private ItemDefinition FindItemDefinition(int itemID)
        {
            ItemDefinition itemDefinition;

            string shortname;
            if (_itemIdShortnameConversions.TryGetValue(itemID, out shortname))
                itemDefinition = ItemManager.FindItemDefinition(shortname);
            else itemDefinition = ItemManager.FindItemDefinition(itemID);

            return itemDefinition;
        }

        private void ConvertItems(ref List<ItemData> list, IEnumerable<OldKitItem> items)
        {
            int position = 0;
            foreach (OldKitItem oldKitItem in items)
            {
                ItemDefinition itemDefinition = FindItemDefinition(oldKitItem.itemid);

                if (itemDefinition == null)
                {
                    Debug.Log($"Failed to find ItemDefinition for item ID {oldKitItem.itemid}");
                    continue;
                }

                ItemData itemData = new ItemData()
                {
                    Shortname = itemDefinition.shortname,
                    Amount = oldKitItem.amount,
                    Skin = oldKitItem.skinid,
                    Position = position
                };

                if (itemDefinition.condition.enabled)                
                    itemData.Condition = itemData.MaxCondition = itemDefinition.condition.max;

                if (itemData.IsBlueprint)
                {
                    itemDefinition = FindItemDefinition(oldKitItem.blueprintTarget);
                    if (itemDefinition == null) 
                    {
                        Debug.Log($"Failed to find ItemDefinition for blueprint target {oldKitItem.blueprintTarget}");
                        continue;
                    }

                    itemData.BlueprintShortname = itemDefinition.shortname;
                }
                
                if (oldKitItem.mods?.Count > 0)                
                {
                    List<ItemData> contents = Facepunch.Pool.GetList<ItemData>();

                    oldKitItem.mods.ForEach((int itemId) =>
                    {
                        itemDefinition = FindItemDefinition(itemId);
                        if (itemDefinition != null)
                        {
                            contents.Add(new ItemData
                            {
                                Shortname = itemDefinition.shortname,
                                Amount = 1
                            });
                        }
                    });

                    itemData.Contents = contents.ToArray();

                    Facepunch.Pool.FreeList(ref contents);
                }

                list.Add(itemData);
                position++;
            }
        }

        private class OldStoredData
        {
            public Dictionary<string, OldKit> Kits = new Dictionary<string, OldKit>();
        }

        private class OldKitData
        {
            public int max;
            public double cooldown;
        }

        private class OldKitItem
        {
            public int itemid;
            public string container;
            public int amount;
            public ulong skinid;
            public bool weapon;
            public int blueprintTarget;
            public List<int> mods = new List<int>();
        }

        private class OldKit
        {
            public string name;
            public string description;
            public int max;
            public double cooldown;
            public int authlevel;
            public bool hide;
            public bool npconly;
            public string permission;
            public string image;
            public string building;
            public List<OldKitItem> items = new List<OldKitItem>();
        }

        private readonly Dictionary<int, string> _itemIdShortnameConversions = new Dictionary<int, string>
        {
            [-1461508848] = "rifle.ak",
            [2115555558] = "ammo.handmade.shell",
            [-533875561] = "ammo.pistol",
            [1621541165] = "ammo.pistol.fire",
            [-422893115] = "ammo.pistol.hv",
            [815896488] = "ammo.rifle",
            [805088543] = "ammo.rifle.explosive",
            [449771810] = "ammo.rifle.incendiary",
            [1152393492] = "ammo.rifle.hv",
            [1578894260] = "ammo.rocket.basic",
            [1436532208] = "ammo.rocket.fire",
            [542276424] = "ammo.rocket.hv",
            [1594947829] = "ammo.rocket.smoke",
            [-1035059994] = "ammo.shotgun",
            [1818890814] = "ammo.shotgun.fire",
            [1819281075] = "ammo.shotgun.slug",
            [1685058759] = "antiradpills",
            [93029210] = "apple",
            [-1565095136] = "apple.spoiled",
            [-1775362679] = "arrow.bone",
            [-1775249157] = "arrow.fire",
            [-1280058093] = "arrow.hv",
            [-420273765] = "arrow.wooden",
            [563023711] = "autoturret",
            [790921853] = "axe.salvaged",
            [-337261910] = "bandage",
            [498312426] = "barricade.concrete",
            [504904386] = "barricade.metal",
            [-1221200300] = "barricade.sandbags",
            [510887968] = "barricade.stone",
            [-814689390] = "barricade.wood",
            [1024486167] = "barricade.woodwire",
            [2021568998] = "battery.small",
            [97329] = "bbq",
            [1046072789] = "trap.bear",
            [97409] = "bed",
            [-1480119738] = "tool.binoculars",
            [1611480185] = "black.raspberries",
            [-1386464949] = "bleach",
            [93832698] = "blood",
            [-1063412582] = "blueberries",
            [-1887162396] = "blueprintbase",
            [-55660037] = "rifle.bolt",
            [919780768] = "bone.club",
            [-365801095] = "bone.fragments",
            [68998734] = "botabag",
            [-853695669] = "bow.hunting",
            [271534758] = "box.wooden.large",
            [-770311783] = "box.wooden",
            [-1192532973] = "bucket.water",
            [-307490664] = "building.planner",
            [707427396] = "burlap.shirt",
            [707432758] = "burlap.shoes",
            [-2079677721] = "cactusflesh",
            [-1342405573] = "tool.camera",
            [-139769801] = "campfire",
            [-1043746011] = "can.beans",
            [2080339268] = "can.beans.empty",
            [-171664558] = "can.tuna",
            [1050986417] = "can.tuna.empty",
            [-1693683664] = "candycaneclub",
            [523409530] = "candycane",
            [1300054961] = "cctv.camera",
            [-2095387015] = "ceilinglight",
            [1428021640] = "chainsaw",
            [94623429] = "chair",
            [1436001773] = "charcoal",
            [1711323399] = "chicken.burned",
            [1734319168] = "chicken.cooked",
            [-1658459025] = "chicken.raw",
            [-726947205] = "chicken.spoiled",
            [-341443994] = "chocholate",
            [1540879296] = "xmasdoorwreath",
            [94756378] = "cloth",
            [3059095] = "coal",
            [3059624] = "corn",
            [2045107609] = "clone.corn",
            [583366917] = "seed.corn",
            [2123300234] = "crossbow",
            [1983936587] = "crude.oil",
            [1257201758] = "cupboard.tool",
            [-1144743963] = "diving.fins",
            [-1144542967] = "diving.mask",
            [-1144334585] = "diving.tank",
            [1066729526] = "diving.wetsuit",
            [-1598790097] = "door.double.hinged.metal",
            [-933236257] = "door.double.hinged.toptier",
            [-1575287163] = "door.double.hinged.wood",
            [-2104481870] = "door.hinged.metal",
            [-1571725662] = "door.hinged.toptier",
            [1456441506] = "door.hinged.wood",
            [1200628767] = "door.key",
            [-778796102] = "door.closer",
            [1526866730] = "xmas.door.garland",
            [1925723260] = "dropbox",
            [1891056868] = "ducttape",
            [1295154089] = "explosive.satchel",
            [498591726] = "explosive.timed",
            [1755466030] = "explosives",
            [726730162] = "facialhair.style01",
            [-1034048911] = "fat.animal",
            [252529905] = "femalearmpithair.style01",
            [471582113] = "femaleeyebrow.style01",
            [-1138648591] = "femalepubichair.style01",
            [305916740] = "female_hairstyle_01",
            [305916742] = "female_hairstyle_03",
            [305916744] = "female_hairstyle_05",
            [1908328648] = "fireplace.stone",
            [-2078972355] = "fish.cooked",
            [-533484654] = "fish.raw",
            [1571660245] = "fishingrod.handmade",
            [1045869440] = "flamethrower",
            [1985408483] = "flameturret",
            [97513422] = "flare",
            [1496470781] = "flashlight.held",
            [1229879204] = "weapon.mod.flashlight",
            [-1722829188] = "floor.grill",
            [1849912854] = "floor.ladder.hatch",
            [-1266285051] = "fridge",
            [-1749787215] = "boots.frog",
            [28178745] = "lowgradefuel",
            [-505639592] = "furnace",
            [1598149413] = "furnace.large",
            [-1779401418] = "gates.external.high.stone",
            [-57285700] = "gates.external.high.wood",
            [98228420] = "gears",
            [1422845239] = "geiger.counter",
            [277631078] = "generator.wind.scrap",
            [115739308] = "burlap.gloves",
            [-522149009] = "gloweyes",
            [3175989] = "glue",
            [718197703] = "granolabar",
            [384204160] = "grenade.beancan",
            [-1308622549] = "grenade.f1",
            [-217113639] = "fun.guitar",
            [-1580059655] = "gunpowder",
            [-1832205789] = "male_hairstyle_01",
            [305916741] = "female_hairstyle_02",
            [936777834] = "attire.hide.helterneck",
            [-1224598842] = "hammer",
            [-1976561211] = "hammer.salvaged",
            [-1406876421] = "hat.beenie",
            [-1397343301] = "hat.boonie",
            [1260209393] = "bucket.helmet",
            [-1035315940] = "burlap.headwrap",
            [-1381682752] = "hat.candle",
            [696727039] = "hat.cap",
            [-2128719593] = "coffeecan.helmet",
            [-1178289187] = "deer.skull.mask",
            [1351172108] = "heavy.plate.helmet",
            [-450738836] = "hat.miner",
            [-966287254] = "attire.reindeer.headband",
            [340009023] = "riot.helmet",
            [124310981] = "hat.wolf",
            [1501403549] = "wood.armor.helmet",
            [698310895] = "hatchet",
            [523855532] = "hazmatsuit",
            [2045246801] = "clone.hemp",
            [583506109] = "seed.hemp",
            [-148163128] = "attire.hide.boots",
            [-132588262] = "attire.hide.skirt",
            [-1666761111] = "attire.hide.vest",
            [-465236267] = "weapon.mod.holosight",
            [-1211618504] = "hoodie",
            [2133577942] = "hq.metal.ore",
            [-1014825244] = "humanmeat.burned",
            [-991829475] = "humanmeat.cooked",
            [-642008142] = "humanmeat.raw",
            [661790782] = "humanmeat.spoiled",
            [-1440143841] = "icepick.salvaged",
            [569119686] = "bone.armor.suit",
            [1404466285] = "heavy.plate.jacket",
            [-1616887133] = "jacket.snow",
            [-1167640370] = "jacket",
            [-1284735799] = "jackolantern.angry",
            [-1278649848] = "jackolantern.happy",
            [776005741] = "knife.bone",
            [108061910] = "ladder.wooden.wall",
            [255101535] = "trap.landmine",
            [-51678842] = "lantern",
            [-789202811] = "largemedkit",
            [516382256] = "weapon.mod.lasersight",
            [50834473] = "leather",
            [-975723312] = "lock.code",
            [1908195100] = "lock.key",
            [-1097452776] = "locker",
            [146685185] = "longsword",
            [-1716193401] = "rifle.lr300",
            [193190034] = "lmg.m249",
            [371156815] = "pistol.m92",
            [3343606] = "mace",
            [825308669] = "machete",
            [830965940] = "mailbox",
            [1662628660] = "male.facialhair.style02",
            [1662628661] = "male.facialhair.style03",
            [1662628662] = "male.facialhair.style04",
            [-1832205788] = "male_hairstyle_02",
            [-1832205786] = "male_hairstyle_04",
            [1625090418] = "malearmpithair.style01",
            [-1269800768] = "maleeyebrow.style01",
            [429648208] = "malepubichair.style01",
            [-1832205787] = "male_hairstyle_03",
            [-1832205785] = "male_hairstyle_05",
            [107868] = "map",
            [997973965] = "mask.balaclava",
            [-46188931] = "mask.bandana",
            [-46848560] = "metal.facemask",
            [-2066726403] = "bearmeat.burned",
            [-2043730634] = "bearmeat.cooked",
            [1325935999] = "bearmeat",
            [-225234813] = "deermeat.burned",
            [-202239044] = "deermeat.cooked",
            [-322501005] = "deermeat.raw",
            [-1851058636] = "horsemeat.burned",
            [-1828062867] = "horsemeat.cooked",
            [-1966381470] = "horsemeat.raw",
            [968732481] = "meat.pork.burned",
            [991728250] = "meat.pork.cooked",
            [-253819519] = "meat.boar",
            [-1714986849] = "wolfmeat.burned",
            [-1691991080] = "wolfmeat.cooked",
            [179448791] = "wolfmeat.raw",
            [431617507] = "wolfmeat.spoiled",
            [688032252] = "metal.fragments",
            [-1059362949] = "metal.ore",
            [1265861812] = "metal.plate.torso",
            [374890416] = "metal.refined",
            [1567404401] = "metalblade",
            [-1057402571] = "metalpipe",
            [-758925787] = "mining.pumpjack",
            [-1411620422] = "mining.quarry",
            [88869913] = "fish.minnows",
            [-2094080303] = "smg.mp5",
            [843418712] = "mushroom",
            [-1569356508] = "weapon.mod.muzzleboost",
            [-1569280852] = "weapon.mod.muzzlebrake",
            [449769971] = "pistol.nailgun",
            [590532217] = "ammo.nailgun.nails",
            [3387378] = "note",
            [1767561705] = "burlap.trousers",
            [106433500] = "pants",
            [-1334615971] = "heavy.plate.pants",
            [-135651869] = "attire.hide.pants",
            [-1595790889] = "roadsign.kilt",
            [-459156023] = "pants.shorts",
            [106434956] = "paper",
            [-578028723] = "pickaxe",
            [-586116979] = "jar.pickle",
            [-1379225193] = "pistol.eoka",
            [-930579334] = "pistol.revolver",
            [548699316] = "pistol.semiauto",
            [142147109] = "planter.large",
            [148953073] = "planter.small",
            [102672084] = "attire.hide.poncho",
            [640562379] = "pookie.bear",
            [-1732316031] = "xmas.present.large",
            [-2130280721] = "xmas.present.medium",
            [-1725510067] = "xmas.present.small",
            [1974032895] = "propanetank",
            [-225085592] = "pumpkin",
            [509654999] = "clone.pumpkin",
            [466113771] = "seed.pumpkin",
            [2033918259] = "pistol.python",
            [2069925558] = "target.reactive",
            [-1026117678] = "box.repair.bench",
            [1987447227] = "research.table",
            [540154065] = "researchpaper",
            [1939428458] = "riflebody",
            [-288010497] = "roadsign.jacket",
            [-847065290] = "roadsigns",
            [3506021] = "rock",
            [649603450] = "rocket.launcher",
            [3506418] = "rope",
            [569935070] = "rug.bear",
            [113284] = "rug",
            [1916127949] = "water.salt",
            [-1775234707] = "salvaged.cleaver",
            [-388967316] = "salvaged.sword",
            [2007564590] = "santahat",
            [-1705696613] = "scarecrow",
            [670655301] = "hazmatsuit_scientist",
            [1148128486] = "hazmatsuit_scientist_peacekeeper",
            [-141135377] = "weapon.mod.small.scope",
            [109266897] = "scrap",
            [-527558546] = "searchlight",
            [-1745053053] = "rifle.semiauto",
            [1223860752] = "semibody",
            [-419069863] = "sewingkit",
            [-1617374968] = "sheetmetal",
            [2057749608] = "shelves",
            [24576628] = "shirt.collared",
            [-1659202509] = "shirt.tanktop",
            [2107229499] = "shoes.boots",
            [191795897] = "shotgun.double",
            [-1009492144] = "shotgun.pump",
            [2077983581] = "shotgun.waterpipe",
            [378365037] = "guntrap",
            [-529054135] = "shutter.metal.embrasure.a",
            [-529054134] = "shutter.metal.embrasure.b",
            [486166145] = "shutter.wood.a",
            [1628490888] = "sign.hanging.banner.large",
            [1498516223] = "sign.hanging",
            [-632459882] = "sign.hanging.ornate",
            [-626812403] = "sign.pictureframe.landscape",
            [385802761] = "sign.pictureframe.portrait",
            [2117976603] = "sign.pictureframe.tall",
            [1338515426] = "sign.pictureframe.xl",
            [-1455694274] = "sign.pictureframe.xxl",
            [1579245182] = "sign.pole.banner.large",
            [-587434450] = "sign.post.double",
            [-163742043] = "sign.post.single",
            [-1224714193] = "sign.post.town",
            [644359987] = "sign.post.town.roof",
            [-1962514734] = "sign.wooden.huge",
            [-705305612] = "sign.wooden.large",
            [-357728804] = "sign.wooden.medium",
            [-698499648] = "sign.wooden.small",
            [1213686767] = "weapon.mod.silencer",
            [386382445] = "weapon.mod.simplesight",
            [1859976884] = "skull_fire_pit",
            [960793436] = "skull.human",
            [1001265731] = "skull.wolf",
            [1253290621] = "sleepingbag",
            [470729623] = "small.oil.refinery",
            [1051155022] = "stash.small",
            [865679437] = "fish.troutsmall",
            [927253046] = "smallwaterbottle",
            [109552593] = "smg.2",
            [-2092529553] = "smgbody",
            [691633666] = "snowball",
            [-2055888649] = "snowman",
            [621575320] = "shotgun.spas12",
            [-2118132208] = "spear.stone",
            [-1127699509] = "spear.wooden",
            [-685265909] = "spikes.floor",
            [552706886] = "spinner.wheel",
            [1835797460] = "metalspring",
            [-892259869] = "sticks",
            [-1623330855] = "stocking.large",
            [-1616524891] = "stocking.small",
            [789892804] = "stone.pickaxe",
            [-1289478934] = "stonehatchet",
            [-892070738] = "stones",
            [-891243783] = "sulfur",
            [889398893] = "sulfur.ore",
            [-1625468793] = "supply.signal",
            [1293049486] = "surveycharge",
            [1369769822] = "fishtrap.small",
            [586484018] = "syringe.medical",
            [110115790] = "table",
            [1490499512] = "targeting.computer",
            [3552619] = "tarp",
            [1471284746] = "techparts",
            [456448245] = "smg.thompson",
            [110547964] = "torch",
            [1588977225] = "xmas.decoration.baubels",
            [918540912] = "xmas.decoration.candycanes",
            [-471874147] = "xmas.decoration.gingerbreadmen",
            [205978836] = "xmas.decoration.lights",
            [-1044400758] = "xmas.decoration.pinecone",
            [-2073307447] = "xmas.decoration.star",
            [435230680] = "xmas.decoration.tinsel",
            [-864578046] = "tshirt",
            [1660607208] = "tshirt.long",
            [260214178] = "tunalight",
            [-1847536522] = "vending.machine",
            [-496055048] = "wall.external.high.stone",
            [-1792066367] = "wall.external.high",
            [562888306] = "wall.frame.cell.gate",
            [-427925529] = "wall.frame.cell",
            [995306285] = "wall.frame.fence.gate",
            [-378017204] = "wall.frame.fence",
            [447918618] = "wall.frame.garagedoor",
            [313836902] = "wall.frame.netting",
            [1175970190] = "wall.frame.shopfront",
            [525244071] = "wall.frame.shopfront.metal",
            [-1021702157] = "wall.window.bars.metal",
            [-402507101] = "wall.window.bars.toptier",
            [-1556671423] = "wall.window.bars.wood",
            [61936445] = "wall.window.glass.reinforced",
            [112903447] = "water",
            [1817873886] = "water.catcher.large",
            [1824679850] = "water.catcher.small",
            [-1628526499] = "water.barrel",
            [547302405] = "waterjug",
            [1840561315] = "water.purifier",
            [-460592212] = "xmas.window.garland",
            [3655341] = "wood",
            [1554697726] = "wood.armor.jacket",
            [-1883959124] = "wood.armor.pants",
            [-481416622] = "workbench1",
            [-481416621] = "workbench2",
            [-481416620] = "workbench3",
            [-1151126752] = "xmas.lightstring",
            [-1926458555] = "xmas.tree"
        };

        
        #endregion
        
        #region Item Shortname Updates
        private void CheckForShortnameUpdates()
        {
            bool hasChanges = false;
            
            kitData.ForEach((KitData.Kit kit) =>
            {
                Action<ItemData[]> action = ((ItemData[] items) =>
                {
                    for (int i = 0; i < items.Length; i++)
                    {
                        ItemData kitItem = items[i];

                        string replacement;
                        if (_itemShortnameReplacements.TryGetValue(kitItem.Shortname, out replacement))
                        {
                            kitItem.Shortname = replacement;
                            hasChanges = true;
                        }
                    }
                });

                action(kit.BeltItems);
                action(kit.WearItems);
                action(kit.MainItems);
            });
            
            if (hasChanges)
                SaveKitData();
        }
        
        private readonly Dictionary<string, string> _itemShortnameReplacements = new Dictionary<string, string>
        {
            ["chocholate"] = "chocolate"
        };
        #endregion

        #region Config        
        private static ConfigData Configuration;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Kit chat command")]
            public string Command { get; set; }

            [JsonProperty(PropertyName = "Currency used for purchase costs (Scrap, Economics, ServerRewards)")]
            public string Currency { get; set; }

            [JsonProperty(PropertyName = "Log kits given")]
            public bool LogKitsGiven { get; set; }

            [JsonProperty(PropertyName = "Wipe player data when the server is wiped")]
            public bool WipeData { get; set; }

            [JsonProperty(PropertyName = "Use the Kits UI menu")]
            public bool UseUI { get; set; }

            [JsonProperty(PropertyName = "Allow players to toggle auto-kits on spawn")]
            public bool AllowAutoToggle { get; set; }

            [JsonProperty(PropertyName = "Show kits with permissions assigned to players without the permission")]
            public bool ShowPermissionKits { get; set; }

            [JsonProperty(PropertyName = "Players with the admin permission ignore usage restrictions")]
            public bool AdminIgnoreRestrictions { get; set; }

            [JsonProperty(PropertyName = "Autokits ordered by priority")]
            public List<string> AutoKits { get; set; }

            [JsonProperty(PropertyName = "Post wipe cooldowns (kit name | seconds)")]
            public Hash<string, int> WipeCooldowns { get; set; }

            [JsonProperty(PropertyName = "Parameters used when pasting a building via CopyPaste")]
            public string[] CopyPasteParams { get; set; }

            [JsonProperty(PropertyName = "UI Options")]
            public MenuOptions Menu { get; set; }

            [JsonProperty(PropertyName = "Kit menu items when opened via HumanNPC (NPC user ID | Items)")]
            public Hash<ulong, NPCKit> NPCKitMenu { get; set; }

            public class MenuOptions
            {
                [JsonProperty(PropertyName = "Panel Color")]
                public UIColor Panel { get; set; }

                [JsonProperty(PropertyName = "Disabled Color")]
                public UIColor Disabled { get; set; }

                [JsonProperty(PropertyName = "Color 1")]
                public UIColor Color1 { get; set; }

                [JsonProperty(PropertyName = "Color 2")]
                public UIColor Color2 { get; set; }

                [JsonProperty(PropertyName = "Color 3")]
                public UIColor Color3 { get; set; }

                [JsonProperty(PropertyName = "Color 4")]
                public UIColor Color4 { get; set; }

                [JsonProperty(PropertyName = "Default kit image URL")]
                public string DefaultKitURL { get; set; }

                [JsonProperty(PropertyName = "View kit icon URL")]
                public string MagnifyIconURL { get; set; }
            }

            public class UIColor
            {
                public string Hex { get; set; }
                public float Alpha { get; set; }

                [JsonIgnore]
                private string _color;

                [JsonIgnore]
                public string Get
                {
                    get
                    {
                        if (string.IsNullOrEmpty(_color))
                            _color = UI.Color(Hex, Alpha);
                        return _color;
                    }
                }
            }

            public class NPCKit
            {
                [JsonProperty(PropertyName = "The list of kits that can be claimed from this NPC")]
                public List<string> Kits { get; set; }

                [JsonProperty(PropertyName = "The NPC's response to opening their kit menu")]
                public string Description { get; set; }
            }

            public VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            Configuration = Config.ReadObject<ConfigData>();

            if (Configuration.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(Configuration, true);
        }

        protected override void LoadDefaultConfig() => Configuration = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                Command = "kit",                
                Currency = "Scrap",
                LogKitsGiven = false,
                WipeData = false,
                ShowPermissionKits = false,
                UseUI = true,
                AllowAutoToggle = false,
                AutoKits = new List<string>
                {
                    "ExampleKitName",
                    "OtherKitName"
                },
                WipeCooldowns = new Hash<string, int>
                {
                    ["ExampleKitName"] = 3600,
                    ["OtherKitName"] = 600
                },
                CopyPasteParams = new string[] { "deployables", "true", "inventories", "true" },
                Menu = new ConfigData.MenuOptions
                {
                    Panel = new ConfigData.UIColor { Hex = "#232323", Alpha = 1f },
                    Disabled = new ConfigData.UIColor { Hex = "#3e3e42", Alpha = 1f },
                    Color1 = new ConfigData.UIColor { Hex = "#007acc", Alpha = 1f },
                    Color2 = new ConfigData.UIColor { Hex = "#6a8b38", Alpha = 1f },
                    Color3 = new ConfigData.UIColor { Hex = "#d85540", Alpha = 1f },
                    Color4 = new ConfigData.UIColor { Hex = "#d08822", Alpha = 1f },
                    DefaultKitURL = "https://chaoscode.io/oxide/Images/kiticon.png",
                    MagnifyIconURL = "https://chaoscode.io/oxide/Images/magnifyingglass.png"
                },
                NPCKitMenu = new Hash<ulong, ConfigData.NPCKit>
                {
                    [0UL] = new ConfigData.NPCKit
                    {
                        Kits = new List<string>
                        {
                            "ExampleKitName",
                            "OtherKitName"
                        },
                        Description = "Welcome to this server! Here are some free kits you can claim"
                    },
                    [1111UL] = new ConfigData.NPCKit
                    {
                        Kits = new List<string>
                        {
                            "ExampleKitName",
                            "OtherKitName"
                        },
                        Description = "Welcome to this server! Here are some free kits you can claim"
                    },
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(Configuration, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (Configuration.Version < new VersionNumber(4, 0, 0))
                Configuration = baseConfig;

            if (Configuration.Version < new VersionNumber(4, 0, 1))
            {
                Configuration.UseUI = true;
                Configuration.NPCKitMenu = baseConfig.NPCKitMenu;
            }

            if (Configuration.Version < new VersionNumber(4, 0, 12))
                Configuration.Command = baseConfig.Command;

            Configuration.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region Data Management
        private KitData kitData;
        private PlayerData playerData;

        private DynamicConfigFile kitdata;
        private DynamicConfigFile playerdata;

        private void SaveKitData() => kitdata.WriteObject(kitData);

        private void SavePlayerData() => playerdata.WriteObject(playerData);

        private void LoadData()
        {
            kitdata = Interface.Oxide.DataFileSystem.GetFile("Kits/kits_data");
            playerdata = Interface.Oxide.DataFileSystem.GetFile("Kits/player_data");

            kitData = kitdata.ReadObject<KitData>();
            playerData = playerdata.ReadObject<PlayerData>();
            
            if (!kitData?.IsValid ?? true)
                kitData = new KitData();

            if (!playerData?.IsValid ?? true)
                playerData = new PlayerData();
        }

        private class KitData
        {
            [JsonProperty]
            private Dictionary<string, Kit> _kits = new Dictionary<string, Kit>(StringComparer.OrdinalIgnoreCase);

            internal Kit this[string key]
            {
                get
                {
                    Kit tValue;
                    if (_kits.TryGetValue(key, out tValue))                    
                        return tValue;
                    
                    return null;
                }
                set
                {
                    if (value == null)
                    {
                        _kits.Remove(key);
                        return;
                    }
                    _kits[key] = value;
                }
            }

            internal int Count => _kits.Count;

            internal bool Find(string name, out Kit kit) => _kits.TryGetValue(name, out kit);

            internal bool Exists(string name) => _kits.ContainsKey(name);

            internal ICollection<string> Keys => _kits.Keys;

            internal ICollection<Kit> Values => _kits.Values;

            internal void ForEach(Action<Kit> action)
            {
                foreach(Kit kit in Values)
                {
                    action.Invoke(kit);
                }
            }

            internal void RegisterPermissions(Permission permission, Plugin plugin)
            {
                foreach(Kit kit in _kits.Values)
                {
                    if (!string.IsNullOrEmpty(kit.RequiredPermission))
                    {                        
                        if(!permission.PermissionExists(kit.RequiredPermission, plugin))
                            permission.RegisterPermission(kit.RequiredPermission, plugin);
                    }
                }
            }

            internal void RegisterImages(Plugin plugin)
            {
                Dictionary<string, string> loadOrder = new Dictionary<string, string>
                {
                    [DEFAULT_ICON] = Configuration.Menu.DefaultKitURL,
                    [MAGNIFY_ICON] = Configuration.Menu.MagnifyIconURL
                };

                foreach (Kit kit in _kits.Values)
                {
                    if (!string.IsNullOrEmpty(kit.KitImage))
                        loadOrder.Add(kit.Name.Replace(" ", ""), kit.KitImage);
                }

                plugin?.CallHook("ImportImageList", "Kits", loadOrder, 0UL, true, null);
            }

            internal bool IsOnWipeCooldown(int seconds, out int remaining)
            {
                double currentTime = CurrentTime;
                double nextUseTime = LastWipeTime + seconds;

                if (currentTime < nextUseTime)
                {
                    remaining = Mathf.RoundToInt((float)nextUseTime - (float)currentTime);
                    return true;
                }

                remaining = 0;
                return false;
            }

            internal void Remove(Kit kit) => _kits.Remove(kit.Name);

            internal bool IsValid => _kits != null;

            public class Kit
            {
                public string Name { get; set; } = string.Empty;
                public string Description { get; set; } = string.Empty;
                public string RequiredPermission { get; set; } = string.Empty;

                public int MaximumUses { get; set; }
                public int RequiredAuth { get; set; }
                public int Cooldown { get; set; }
                public int Cost { get; set; }

                public bool IsHidden { get; set; }

                public string CopyPasteFile { get; set; } = string.Empty;
                public string KitImage { get; set; } = string.Empty;

                public ItemData[] MainItems { get; set; } = new ItemData[0];
                public ItemData[] WearItems { get; set; } = new ItemData[0];
                public ItemData[] BeltItems { get; set; } = new ItemData[0];
                
                [JsonIgnore]
                internal int ItemCount => MainItems.Length + WearItems.Length + BeltItems.Length;

                [JsonIgnore]
                private JObject _jObject;

                [JsonIgnore]
                internal JObject ToJObject
                {
                    get
                    {
                        if (_jObject == null)
                        {
                            _jObject = new JObject
                            {
                                ["Name"] = Name,
                                ["Description"] = Description,
                                ["RequiredPermission"] = RequiredPermission,
                                ["MaximumUses"] = MaximumUses,
                                ["RequiredAuth"] = RequiredAuth,
                                ["Cost"] = Cost,
                                ["IsHidden"] = IsHidden,
                                ["CopyPasteFile"] = CopyPasteFile,
                                ["KitImage"] = KitImage,
                                ["MainItems"] = new JArray(),
                                ["WearItems"] = new JArray(),
                                ["BeltItems"] = new JArray()
                            };

                            for (int i = 0; i < MainItems.Length; i++)                            
                                (_jObject["MainItems"] as JArray).Add(MainItems[i].ToJObject);

                            for (int i = 0; i < WearItems.Length; i++)
                                (_jObject["WearItems"] as JArray).Add(WearItems[i].ToJObject);

                            for (int i = 0; i < BeltItems.Length; i++)
                                (_jObject["BeltItems"] as JArray).Add(BeltItems[i].ToJObject);
                        }

                        return _jObject;
                    }
                }

                internal static Kit CloneOf(Kit other)
                {
                    Kit kit = new Kit();

                    kit.Name = other.Name;
                    kit.Description = other.Description;
                    kit.RequiredPermission = other.RequiredPermission;

                    kit.MaximumUses = other.MaximumUses;
                    kit.RequiredAuth = other.RequiredAuth;
                    kit.Cooldown = other.Cooldown;
                    kit.Cost = other.Cost;

                    kit.IsHidden = other.IsHidden;

                    kit.CopyPasteFile = other.CopyPasteFile;
                    kit.KitImage = other.KitImage;

                    ItemData[] array = kit.MainItems;
                    Array.Resize(ref array, other.MainItems.Length);
                    Array.Copy(other.MainItems, array, other.MainItems.Length);
                    kit.MainItems = array;

                    array = kit.WearItems;
                    Array.Resize(ref array, other.WearItems.Length);
                    Array.Copy(other.WearItems, array, other.WearItems.Length);
                    kit.WearItems = array;

                    array = kit.BeltItems;
                    Array.Resize(ref array, other.BeltItems.Length);
                    Array.Copy(other.BeltItems, array, other.BeltItems.Length);
                    kit.BeltItems = array;

                    return kit;
                }

                internal bool HasSpaceForItems(BasePlayer player)
                {
                    int wearSpacesFree = 7 - player.inventory.containerWear.itemList.Count;
                    int mainSpacesFree = 24 - player.inventory.containerMain.itemList.Count;
                    int beltSpacesFree = 6 - player.inventory.containerBelt.itemList.Count;

                    return (wearSpacesFree >= WearItems.Length &&
                            beltSpacesFree >= BeltItems.Length &&
                            mainSpacesFree >= MainItems.Length) || ItemCount <= mainSpacesFree + beltSpacesFree;
                }

                internal void GiveItemsTo(BasePlayer player)
                {
                    List<ItemData> list = Facepunch.Pool.GetList<ItemData>();

                    GiveItems(MainItems, player.inventory.containerMain, ref list);
                    GiveItems(WearItems, player.inventory.containerWear, ref list, true);
                    GiveItems(BeltItems, player.inventory.containerBelt, ref list);

                    for (int i = 0; i < list.Count; i++)
                    {
                        Item item = CreateItem(list[i]);

                        if (!MoveToIdealContainer(player.inventory, item) && !item.MoveToContainer(player.inventory.containerMain, -1, true) && !item.MoveToContainer(player.inventory.containerBelt, -1, true))                        
                            item.Drop(player.GetDropPosition(), player.GetDropVelocity());                                                
                    }

                    Facepunch.Pool.FreeList(ref list);
                }

                private void GiveItems(ItemData[] items, ItemContainer container, ref List<ItemData> leftOverItems, bool isWearContainer = false)
                {
                    for (int i = 0; i < items.Length; i++)
                    {
                        ItemData itemData = items[i];
                        if (itemData.Amount < 1)
                            continue;

                        if (container.GetSlot(itemData.Position) != null)
                            leftOverItems.Add(itemData);
                        else
                        {
                            Item item = CreateItem(itemData);
                            if (!isWearContainer || (isWearContainer && item.info.isWearable && CanWearItem(container, item)))
                            {
                                item.position = itemData.Position;
                                item.SetParent(container);
                            }
                            else
                            {
                                leftOverItems.Add(itemData);
                                item.Remove(0f);
                            }
                        }
                    }
                }
                
                internal IEnumerable<Item> CreateItems()
                {
                    for (int i = 0; i < MainItems.Length; i++)
                    {
                        ItemData itemData = MainItems[i];
                        if (itemData.Amount > 0)
                            yield return CreateItem(itemData);
                    }
                    
                    for (int i = 0; i < WearItems.Length; i++)
                    {
                        ItemData itemData = WearItems[i];
                        if (itemData.Amount > 0)
                            yield return CreateItem(itemData);
                    }
                    
                    for (int i = 0; i < BeltItems.Length; i++)
                    {
                        ItemData itemData = BeltItems[i];
                        if (itemData.Amount > 0)
                            yield return CreateItem(itemData);
                    }
                }

                private bool MoveToIdealContainer(PlayerInventory playerInventory, Item item)
                {
                    if (item.info.isWearable && CanWearItem(playerInventory.containerWear, item))                    
                        return item.MoveToContainer(playerInventory.containerWear, -1, false);
                    
                    if (item.info.stackable > 1)
                    {
                        if (playerInventory.containerBelt != null && playerInventory.containerBelt.FindItemByItemID(item.info.itemid) != null)                        
                            return item.MoveToContainer(playerInventory.containerBelt, -1, true);
                        

                        if (playerInventory.containerMain != null && playerInventory.containerMain.FindItemByItemID(item.info.itemid) != null)                        
                            return item.MoveToContainer(playerInventory.containerMain, -1, true);
                        
                    }
                    if (item.info.HasFlag(ItemDefinition.Flag.NotStraightToBelt) || !item.info.isUsable)                    
                        return item.MoveToContainer(playerInventory.containerMain, -1, true);                    

                    return item.MoveToContainer(playerInventory.containerBelt, -1, false); 
                }

                private bool CanWearItem(ItemContainer containerWear, Item item)
                {
                    ItemModWearable itemModWearable = item.info.GetComponent<ItemModWearable>();
                    if (itemModWearable == null)                  
                        return false;
                    
                    for (int i = 0; i < containerWear.itemList.Count; i++)
                    {
                        Item otherItem = containerWear.itemList[i];
                        if (otherItem != null)
                        {
                            ItemModWearable otherModWearable = otherItem.info.GetComponent<ItemModWearable>();                          
                            if (otherModWearable != null && !itemModWearable.CanExistWith(otherModWearable))
                                return false;
                        }
                    }

                    return true;
                }
                                
                internal void CopyItemsFrom(BasePlayer player)
                {
                    ItemData[] array = MainItems;
                    CopyItems(ref array, player.inventory.containerMain, 24);
                    MainItems = array;

                    array = WearItems;
                    CopyItems(ref array, player.inventory.containerWear, 7);
                    WearItems = array;

                    array = BeltItems;
                    CopyItems(ref array, player.inventory.containerBelt, 6);
                    BeltItems = array;
                }

                private void CopyItems(ref ItemData[] array, ItemContainer container, int limit)
                {
                    limit = Mathf.Min(container.itemList.Count, limit);

                    Array.Resize(ref array, limit);

                    for (int i = 0; i < limit; i++)                    
                        array[i] = new ItemData(container.itemList[i]);                    
                }

                internal void ClearItems()
                {
                    ItemData[] array = MainItems;
                    Array.Resize(ref array, 0);
                    MainItems = array;

                    array = WearItems;
                    Array.Resize(ref array, 0);
                    WearItems = array;

                    array = BeltItems;
                    Array.Resize(ref array, 0);
                    BeltItems = array;
                }
            }
        }

        private class PlayerData
        {
            [JsonProperty]
            private Dictionary<ulong, PlayerUsageData> _players = new Dictionary<ulong, PlayerUsageData>();

            internal bool Find(ulong playerId, out PlayerUsageData playerUsageData) => _players.TryGetValue(playerId, out playerUsageData);

            internal bool Exists(ulong playerId) => _players.ContainsKey(playerId);

            internal PlayerUsageData this[ulong key]
            {
                get
                {
                    PlayerUsageData tValue;
                    if (_players.TryGetValue(key, out tValue))
                        return tValue;

                    tValue = (PlayerUsageData)Activator.CreateInstance(typeof(PlayerUsageData));
                    _players.Add(key, tValue);
                    return tValue;
                }
                set
                {
                    if (value == null)
                    {
                        _players.Remove(key);
                        return;
                    }
                    _players[key] = value;
                }
            }

            internal void OnKitClaimed(BasePlayer player, KitData.Kit kit)
            {
                if (kit.MaximumUses == 0 && kit.Cooldown == 0)
                    return;

                PlayerUsageData playerUsageData;
                if (!_players.TryGetValue(player.userID, out playerUsageData))
                    playerUsageData = _players[player.userID] = new PlayerUsageData();

                playerUsageData.OnKitClaimed(kit);
            }

            internal void Wipe() => _players.Clear();

            internal bool IsValid => _players != null;

            public class PlayerUsageData
            {
                [JsonProperty]
                private Hash<string, KitUsageData> _usageData = new Hash<string, KitUsageData>();

                public bool ClaimAutoKits { get; set; } = true;

                internal double GetCooldownRemaining(string name)
                {
                    KitUsageData kitUsageData;
                    if (!_usageData.TryGetValue(name, out kitUsageData))
                        return 0;

                    double currentTime = CurrentTime;

                    return currentTime > kitUsageData.NextUseTime ? 0 : kitUsageData.NextUseTime - CurrentTime;
                }

                internal void SetCooldownRemaining(string name, double seconds)
                {
                    KitUsageData kitUsageData;
                    if (!_usageData.TryGetValue(name, out kitUsageData))
                        return;

                    kitUsageData.NextUseTime = CurrentTime + seconds;
                }

                internal int GetKitUses(string name)
                {
                    KitUsageData kitUsageData;
                    if (!_usageData.TryGetValue(name, out kitUsageData))
                        return 0;

                    return kitUsageData.TotalUses;
                }

                internal void SetKitUses(string name, int amount)
                {
                    KitUsageData kitUsageData;
                    if (!_usageData.TryGetValue(name, out kitUsageData))
                        return;

                    kitUsageData.TotalUses = amount;
                }

                internal void OnKitClaimed(KitData.Kit kit)
                {
                    KitUsageData kitUsageData;
                    if (!_usageData.TryGetValue(kit.Name, out kitUsageData))
                        kitUsageData = _usageData[kit.Name] = new KitUsageData();

                    kitUsageData.OnKitClaimed(kit.Cooldown);
                }

                internal void InsertOldData(string name, int totalUses, double nextUse)
                {
                    KitUsageData kitUsageData;
                    if (!_usageData.TryGetValue(name, out kitUsageData))
                        kitUsageData = _usageData[name] = new KitUsageData();

                    kitUsageData.NextUseTime = nextUse;
                    kitUsageData.TotalUses = totalUses;
                }

                public class KitUsageData
                {
                    public int TotalUses { get; set; }

                    public double NextUseTime { get; set; }

                    internal void OnKitClaimed(int cooldownSeconds)
                    {
                        TotalUses += 1;
                        NextUseTime = CurrentTime + cooldownSeconds;
                    }
                }
            }            
        }        
        #endregion

        #region Serialized Items
        private static Item CreateItem(ItemData itemData)
        {
            Item item = ItemManager.CreateByItemID(itemData.ItemID, itemData.Amount, itemData.Skin);
            item.condition = itemData.Condition;
            item.maxCondition = itemData.MaxCondition;

            if (!string.IsNullOrEmpty(itemData.DisplayName))
                item.name = itemData.DisplayName;

            if (!string.IsNullOrEmpty(itemData.Text))
                item.text = itemData.Text;
            
            if (itemData.Frequency > 0)
            {
                ItemModRFListener rfListener = item.info.GetComponentInChildren<ItemModRFListener>();
                if (rfListener != null)
                    (BaseNetworkable.serverEntities.Find(item.instanceData.subEntity) as PagerEntity)?.ChangeFrequency(itemData.Frequency);  
            }

            if (itemData.BlueprintItemID != 0)
            {
                if (item.instanceData == null)
                    item.instanceData = new ProtoBuf.Item.InstanceData();

                item.instanceData.ShouldPool = false;

                item.instanceData.blueprintAmount = 1;
                item.instanceData.blueprintTarget = itemData.BlueprintItemID;

                item.MarkDirty();
            }

            FlameThrower flameThrower = item.GetHeldEntity() as FlameThrower;
            if (flameThrower != null)
                flameThrower.ammo = itemData.Ammo;

            if (itemData.Contents != null)
            {
                foreach (ItemData contentData in itemData.Contents)
                {
                    Item newContent = CreateItem(contentData);
                    if (newContent != null)
                    {
                        if (!newContent.MoveToContainer(item.contents))
                            newContent.Remove(0f);
                    }
                }
            }
            
            BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
            if (weapon != null)
            {
                weapon.DelayedModsChanged();
                
                if (!string.IsNullOrEmpty(itemData.Ammotype))
                    weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(itemData.Ammotype);
                weapon.primaryMagazine.contents = itemData.Ammo;
            }


            item.MarkDirty();

            return item;
        }

        public class ItemData
        {
            public string Shortname { get; set; }
            
            public string DisplayName { get; set; }

            public ulong Skin { get; set; }

            public int Amount { get; set; }

            public float Condition { get; set; }

            public float MaxCondition { get; set; }

            public int Ammo { get; set; }

            public string Ammotype { get; set; }

            public int Position { get; set; }

            public int Frequency { get; set; }

            public string BlueprintShortname { get; set; }
            
            public string Text { get; set; }

            public ItemData[] Contents { get; set; }


            [JsonIgnore]
            private int _itemId = 0;

            [JsonIgnore]
            private int _blueprintItemId = 0;

            [JsonIgnore]
            private JObject _jObject;

           
            [JsonIgnore]
            internal int ItemID
            {
                get
                {
                    if (_itemId == 0)
                        _itemId = ItemManager.itemDictionaryByName[Shortname].itemid;
                    return _itemId;
                }
            }

            [JsonIgnore]
            internal bool IsBlueprint => Shortname.Equals(BLUEPRINT_BASE);

            [JsonIgnore]
            internal int BlueprintItemID
            {
                get
                {
                    if (_blueprintItemId == 0 && !string.IsNullOrEmpty(BlueprintShortname))
                        _blueprintItemId = ItemManager.itemDictionaryByName[BlueprintShortname].itemid;
                    return _blueprintItemId;
                }
            }

            [JsonIgnore]
            internal JObject ToJObject
            {
                get
                {
                    if (_jObject == null)
                    {
                        _jObject = new JObject
                        {
                            ["Shortname"] = Shortname,
                            ["DisplayName"] = DisplayName,
                            ["SkinID"] = Skin,
                            ["Amount"] = Amount,
                            ["Condition"] = Condition,
                            ["MaxCondition"] = MaxCondition,
                            ["IsBlueprint"] = BlueprintItemID != 0,
                            ["Ammo"] = Ammo,
                            ["AmmoType"] = Ammotype,
                            ["Text"] = Text,
                            ["Contents"] = new JArray()
                        };

                        for (int i = 0; i < Contents?.Length; i++)                        
                            (_jObject["Contents"] as JArray).Add(Contents[i].ToJObject);
                    }

                    return _jObject;
                }
            }

            internal ItemData() { }

            internal ItemData(Item item)
            {
                Shortname = item.info.shortname;
                Amount = item.amount;
                DisplayName = item.name;
                Text = item.text;

                BaseEntity heldEntity = item.GetHeldEntity();
                if (heldEntity)
                {
                    Ammotype = heldEntity is BaseProjectile ? (heldEntity as BaseProjectile).primaryMagazine.ammoType.shortname : null;
                    Ammo = heldEntity is BaseProjectile ? (heldEntity as BaseProjectile).primaryMagazine.contents :
                           heldEntity is FlameThrower ? (heldEntity as FlameThrower).ammo : 0;
                }

                Position = item.position;
                Skin = item.skin;

                Condition = item.condition;
                MaxCondition = item.maxCondition;

                Frequency = ItemModAssociatedEntity<PagerEntity>.GetAssociatedEntity(item)?.GetFrequency() ?? -1;

                if (item.instanceData != null && item.instanceData.blueprintTarget != 0)
                    BlueprintShortname = ItemManager.FindItemDefinition(item.instanceData.blueprintTarget).shortname;

                Contents = item.contents?.itemList.Select(item1 => new ItemData(item1)).ToArray();
            }

            public class InstanceData
            {
                public int DataInt { get; set; }
                
                public int BlueprintTarget { get; set; }
                
                public int BlueprintAmount { get; set; }
                
                public uint SubEntityNetID { get; set; }

                internal InstanceData() { }

                internal InstanceData(Item item)
                {
                    if (item.instanceData == null)
                        return;

                    DataInt = item.instanceData.dataInt;
                    BlueprintAmount = item.instanceData.blueprintAmount;
                    BlueprintTarget = item.instanceData.blueprintTarget;
                }

                internal void Restore(Item item)
                {
                    if (item.instanceData == null)
                        item.instanceData = new ProtoBuf.Item.InstanceData();

                    item.instanceData.ShouldPool = false;

                    item.instanceData.blueprintAmount = BlueprintAmount;
                    item.instanceData.blueprintTarget = BlueprintTarget;
                    item.instanceData.dataInt = DataInt;

                    item.MarkDirty();
                }

                internal bool IsValid => DataInt != 0 || BlueprintAmount != 0 || BlueprintTarget != 0;                
            }
        }
        #endregion

        #region Localization
        private string Message(string key, ulong playerId = 0U) => lang.GetMessage(key, this, playerId != 0U ? playerId.ToString() : null);

        private readonly Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["Error.EmptyKitName"] = "No kit name was specified",
            ["Error.InvalidKitName"] = "No kit exists with the specified name",
            ["Error.CantClaimNow"] = "Another plugin is preventing you from receiving a kit",
            ["Error.CanClaim.Auth"] = "You do not have the required auth level to access this kit",
            ["Error.CanClaim.Permission"] = "You do not have the required permission to access this kit",
            ["Error.CanClaim.Cooldown"] = "You have a cooldown of {0} remaining before you can claim this kit",
            ["Error.CanClaim.MaxUses"] = "You have already reached the maximum number of uses allowed for this kit",
            ["Error.CanClaim.WipeCooldown"] = "This kit has a post-wipe cooldown of {0} remaining",
            ["Error.CanClaim.InventorySpace"] = "You do not have enough space in your inventory to claim this kit",
            ["Error.CanClaim.InsufficientFunds"] = "You need {0} {1} to claim this kit",
            ["Error.AutoKitDisabled"] = "Skipped giving auto-kit as you have it disabled",


            ["Cost.Scrap"] = "Scrap",
            ["Cost.ServerRewards"] = "RP",
            ["Cost.Economics"] = "Coins",

            ["Notification.KitReceived"] = "You have received the kit: <color=#ce422b>{0}</color>",

            ["Chat.Help.Title"] = "<size=18><color=#ce422b>Kits </color></size><size=14>v{0}</size>",
            ["Chat.Help.1"] = "<color=#ce422b>/kit</color> - Open the Kit menu",
            ["Chat.Help.9"] = "<color=#ce422b>/kit autokit</color> - Toggle auto-kits on/off",
            ["Chat.Help.2"] = "<color=#ce422b>/kit <name></color> - Claim the specified kit",
            ["Chat.Help.3"] = "<color=#ce422b>/kit new</color> - Create a new kit",
            ["Chat.Help.4"] = "<color=#ce422b>/kit edit <name></color> - Edit the specified kit",
            ["Chat.Help.5"] = "<color=#ce422b>/kit delete <name></color> - Delete the specified kit",
            ["Chat.Help.6"] = "<color=#ce422b>/kit list</color> - List all kits",
            ["Chat.Help.7"] = "<color=#ce422b>/kit give <player name or ID> <kit name></color> - Give the target player the specified kit",
            ["Chat.Help.8"] = "<color=#ce422b>/kit givenpc <kit name></color> - Give the NPC you are looking at the specified kit",
            ["Chat.Help.10"] = "<color=#ce422b>/kit reset</color> - Wipe's all player usage data",

            ["Chat.Error.NotAdmin"] = "You must either be a admin, or have the admin permission to use that command",
            ["Chat.Error.NoKit"] = "You must specify a kit name",
            ["Chat.Error.DoesntExist"] = "The kit <color=#ce422b>{0}</color> does not exist",
            ["Chat.Error.GiveArgs"] = "You must specify target player and a kit name",
            ["Chat.Error.NPCGiveArgs"] = "You must specify a kit name",
            ["Chat.Error.NoNPCTarget"] = "Failed to find the target player",
            ["Chat.Error.NoPlayer"] = "Failed to find a player with the specified name or ID",
            ["Chat.KitList"] = "<color=#ce422b>Kit List:</color> {0}",
            ["Chat.KitDeleted"] = "You have deleted the kit <color=#ce422b>{0}</color>",
            ["Chat.KitGiven"] = "You have given <color=#ce422b>{0}</color> the kit <color=#ce422b>{1}</color>",
            ["Chat.AutoKit.Toggle"] = "Auto-kits have been <color=#ce422b>{0}</color>",
            ["Chat.ResetPlayers"] = "You have wiped player usage data",
            ["Chat.AutoKit.True"] = "enabled",
            ["Chat.AutoKit.False"] = "disabled",

            ["UI.Title"] = "Kits",
            ["UI.Title.Editor"] = "Kit Editor",
            ["UI.OnCooldown"] = "On Cooldown",
            ["UI.Cooldown"] = "Cooldown : {0}",
            ["UI.MaximumUses"] = "At Redeem Limit",
            ["UI.Purchase"] = "Purchase",
            ["UI.Cost"] = "Cost : {0} {1}",
            ["UI.Redeem"] = "Redeem",
            ["UI.Details"] = "Kit Details",
            ["UI.Name"] = "Name",
            ["UI.Description"] = "Description",
            ["UI.Usage"] = "Usage Details",
            ["UI.MaxUses"] = "Maximum Uses",
            ["UI.YourUses"] = "Your Uses",
            ["UI.CooldownTime"] = "Cooldown Time",
            ["UI.CooldownRemaining"] = "Remaining Cooldown",
            ["UI.None"] = "None",
            ["UI.PurchaseCost"] = "Purchase Cost",
            ["UI.CopyPaste"] = "CopyPaste Support",
            ["UI.FileName"] = "File Name",
            ["UI.KitItems"] = "Kit Items",
            ["UI.MainItems"] = "Main Items",
            ["UI.WearItems"] = "Wear Items",
            ["UI.BeltItems"] = "Belt Items",
            ["UI.IconURL"] = "Icon URL",
            ["UI.UsageAuthority"] = "Usage Authority",
            ["UI.Permission"] = "Permission",
            ["UI.AuthLevel"] = "Auth Level",
            ["UI.IsHidden"] = "Is Hidden",
            ["UI.CooldownSeconds"] = "Cooldown (seconds)",
            ["UI.SaveKit"] = "Save Kit",
            ["UI.Overwrite"] = "Overwrite Existing",
            ["UI.ItemManagement"] = "Item Management",
            ["UI.ClearItems"] = "Clear Items",
            ["UI.CopyInv"] = "Copy From Inventory",
            ["UI.CreateNew"] = "Create New",
            ["UI.EditKit"] = "Edit Kit",
            ["UI.NeedsPermission"] = "VIP Kit",
            ["UI.NoKitsAvailable"] = "There are currently no kits available",

            ["SaveKit.Error.NoName"] = "You must enter a kit name",
            ["SaveKit.Error.NoContents"] = "A kit must contain atleast 1 item, or a CopyPaste file reference",
            ["SaveKit.Error.Exists"] = "A kit with that name already exists. If you want to overwrite it check the 'Overwrite' toggle",
            ["SaveKit.Success"] = "You have saved the kit {0}",

            ["EditKit.PermissionPrefix"] = "Permissions must start with the 'kits.' prefix",
            ["EditKit.Number"] = "You must enter a number",
            ["EditKit.Bool"] = "You must enter true or false",
        };
        #endregion
    }
}
