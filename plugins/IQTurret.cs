using System;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using Oxide.Core;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("IQTurret", "qxzxf", "1.3.7")]
    [Description("Турели без электричества с лимитами на игрока/шкаф")]
    internal class IQTurret : RustPlugin
    {
        
                void Init() => Unsubscribe(nameof(OnEntitySpawned));

        private readonly List<UInt64> IDsPrefabs = new List<UInt64> { 3312510084, 2059775839 };

        private BaseEntity GetTurretForSwitch(ElectricSwitch Switch)
        {
            if (Switch == null || Switch.IsDestroyed || !TurretList.ContainsKey(Switch.skinID)) return null;
            List<ControllerInformation> InformationList = TurretList[Switch.skinID];
            if (InformationList == null)
                return null;

            foreach (ControllerInformation Info in InformationList)
                if (Info.electricSwitch.skinID.Equals(Switch.skinID))
                    return Info.turrel;

            return null;
        }
        void OnServerInitialized()
        {
            ImmortalProtection = ScriptableObject.CreateInstance<ProtectionProperties>();
            ImmortalProtection.name = "TurretsSwitchProtection";
            ImmortalProtection.Add(1);

            foreach (String Permissions in config.LimitController.PermissionsLimits.Keys)
                permission.RegisterPermission(Permissions, this);

            permission.RegisterPermission(PermissionTurnAllTurretsOn, this);
            permission.RegisterPermission(PermissionTurnAllTurretsOff, this);

            NextTick(InitializeData);
        }
        private readonly String PermissionTurnAllTurretsOff = "iqturret.turnoffall";

        private ProtectionProperties ImmortalProtection;

        private void TurretToggle(BasePlayer player, ElectricSwitch electricSwitch)
        {
            if(electricSwitch == null) return;

            BaseEntity turrel = GetTurretForSwitch(electricSwitch);
            Boolean IsFlag = false;
		   		 		  						  	   		  	 	 		  	 				   		 		  		 	
            if (turrel != null)
            {
                BaseEntity.Flags flags = BaseEntity.Flags.On;
                if (turrel is AutoTurret)
                    flags = BaseEntity.Flags.On;
               
                else if (turrel is SamSite)
                    flags = BaseEntity.Flags.Reserved8;

                if (!turrel.HasFlag(flags))
                    turrel.SetFlag(flags, true);
                else turrel.SetFlag(flags, false);

                IsFlag = turrel.HasFlag(flags);
            }

            if (config.LimitController.UseLimitControll)
            {
                UInt64 ID = 0;
                if (config.LimitController.typeLimiter == TypeLimiter.Building && player.GetBuildingPrivilege() != null)
                    ID = player.GetBuildingPrivilege().buildingID;
                
                Int32 LimitCount = (GetLimitPlayer(electricSwitch.OwnerID) - GetAmountTurretPlayer(electricSwitch.OwnerID, ID));
                SendChat(GetLang(IsFlag ? (electricSwitch.OwnerID != player.userID ? "INFORMATION_USER_ON_OTHER" : "INFORMATION_USER_ON") : (electricSwitch.OwnerID != player.userID ? "INFORMATION_USER_OFF_OTHER" : "INFORMATION_USER_OFF"), player.UserIDString, LimitCount), player);
            }
        }

        void Unload() => UnloadPlugin();
        /// <summary>
        /// Обновление 1.3.x
        /// - Исправлено удаление турелей из-за IQCraftSystem
        /// - Исправлен лимит на игроков, когда они могли его превышать использую чужие турели
        /// - Корректировка работы методов с лимитами
        /// </summary>
        
        
        [PluginReference] private Plugin IQChat;

        [ConsoleCommand("t")]
        void TurretControllConsoleCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null || arg == null || arg.Args.Length == 0)
            {
                SendChat(GetLang("SYNTAX_COMMAND_ERROR", player.UserIDString), player);
                return;
            }

            String Action = arg.Args[0];
            if (String.IsNullOrWhiteSpace(Action)) return;
            switch (Action)
            {
                case "limit":
                    {
                        UInt64 ID = 0;
                        if (config.LimitController.typeLimiter == TypeLimiter.Building && player.GetBuildingPrivilege() != null)
                            ID = player.GetBuildingPrivilege().buildingID;

                        String Lang = GetLang("INFORMATION_MY_LIMIT", player.UserIDString, (GetLimitPlayer(player.userID) - GetAmountTurretPlayer(player.userID, ID)));
                        SendChat(Lang, player);
                        break;
                    }
                case "off":
                    {
                        if (!permission.UserHasPermission(player.UserIDString, PermissionTurnAllTurretsOff))
                        {
                            SendChat(GetLang("PERMISSION_COMMAND_ERROR", player.UserIDString), player);
                            return;
                        }

                        Int32 LimitPlayer = GetLimitPlayer(player.userID);
                        Dictionary<BaseEntity, ElectricSwitch> PlayerTurrets = GetPlayerTurretAndSwitch(player);
                        if (PlayerTurrets != null)
                        {
                            foreach (KeyValuePair<BaseEntity, ElectricSwitch> Item in PlayerTurrets.Where(x => x.Value.HasFlag(BaseEntity.Flags.On)).Take(LimitPlayer))
                                if (Item.Key is AutoTurret)
                                {
                                    if (Item.Key.HasFlag(BaseEntity.Flags.On))
                                    {
                                        Item.Key.SetFlag(BaseEntity.Flags.On, false);
                                        Item.Value.SetFlag(BaseEntity.Flags.On, false);
                                        LimitPlayer--;
                                    }
                                }
                                else if (Item.Key is SamSite)
                                {
                                    if (Item.Key.HasFlag(BaseEntity.Flags.Reserved8))
                                    {
                                        Item.Key.SetFlag(BaseEntity.Flags.Reserved8, false);
                                        Item.Value.SetFlag(BaseEntity.Flags.On, false);
                                        LimitPlayer--;
                                    }
                                }
                        }
                        break;
                    }
                case "on":
                    {
                        if (!permission.UserHasPermission(player.UserIDString, PermissionTurnAllTurretsOn))
                        {
                            SendChat(GetLang("PERMISSION_COMMAND_ERROR", player.UserIDString), player);
                            return;
                        }

                        Int32 LimitPlayer = GetLimitPlayer(player.userID);
                        Dictionary<BaseEntity, ElectricSwitch> PlayerTurrets = GetPlayerTurretAndSwitch(player);
                        if (PlayerTurrets != null)
                        {
                            foreach (KeyValuePair<BaseEntity, ElectricSwitch> Item in PlayerTurrets.Where(x => !x.Value.HasFlag(BaseEntity.Flags.On)).Take(LimitPlayer))
                                if (Item.Key is AutoTurret)
                                {
                                    if (!Item.Key.HasFlag(BaseEntity.Flags.On))
                                    {
                                        Item.Key.SetFlag(BaseEntity.Flags.On, true);
                                        Item.Value.SetFlag(BaseEntity.Flags.On, true);
                                        LimitPlayer--;
                                    }
                                }
                                else if (Item.Key is SamSite)
                                {
                                    if (!Item.Key.HasFlag(BaseEntity.Flags.Reserved8))
                                    {
                                        Item.Key.SetFlag(BaseEntity.Flags.Reserved8, true);
                                        Item.Value.SetFlag(BaseEntity.Flags.On, true);
                                        LimitPlayer--;
                                    }
                                }
                        }
                        break;
                    }
            }
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            BasePlayer damager = hitInfo.InitiatorPlayer;
            if (entity == null || hitInfo == null || damager == null) return;

            ElectricSwitch Switch = entity as ElectricSwitch;
            if (Switch != null && Switch.skinID != 0 && TurretList.ContainsKey(Switch.skinID))
                hitInfo.damageTypes.ScaleAll(0);
        }
        Boolean API_IS_TURRETLIST(BaseEntity entity)
        {
            if (entity.skinID == 0) return false;
            return TurretList.ContainsKey(entity.skinID);
        }

        
        
                private static StringBuilder sb = new StringBuilder(); 
		   		 		  						  	   		  	 	 		  	 				   		 		  		 	
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning(LanguageEn ? $"Error #821205 configuration readings 'oxide/config/{Name}', creating a new configuration!" : $"Ошибка #821205 чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!"); //
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }
        
        
        Boolean API_IS_TURRETLIST(UInt64 ID)
        {
            if (ID == 0) return false;
            return TurretList.ContainsKey(ID);
        }

        private Int32 GetLimitPlayer(UInt64 PlayerID)
        {
            foreach (KeyValuePair<String, Int32> LimitPrivilage in config.LimitController.PermissionsLimits)
                if (permission.UserHasPermission(PlayerID.ToString(), LimitPrivilage.Key))
                    return LimitPrivilage.Value;

            return config.LimitController.LimitAmount;
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();

        private readonly String PermissionTurnAllTurretsOn = "iqturret.turnonall";
        public string GetLang(String LangKey, String userID = null, params object[] args)
        {
            sb.Clear();
            if (args != null)
            {
                sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb.ToString();
            }
            return lang.GetMessage(LangKey, this, userID);
        }

        object OnWireClear(BasePlayer player, IOEntity entity1, int connected, IOEntity entity2, bool flag)
        {
            ElectricSwitch switchConnected = entity1 as ElectricSwitch;
            if (switchConnected != null && switchConnected.skinID != 0 && TurretList.ContainsKey(switchConnected.skinID))
                return false;
		   		 		  						  	   		  	 	 		  	 				   		 		  		 	
            return null;
        }

        
        
        private Dictionary<UInt64, List<ControllerInformation>> TurretList = new Dictionary<UInt64, List<ControllerInformation>>();
        BaseEntity API_GET_TURRET(BasePlayer player, ElectricSwitch electricSwitch) => GetTurretForSwitch(electricSwitch);
        
                public Boolean IsRaidBlocked(BasePlayer player) 
        {
            if (!config.ReferencesPlugin.BlockedTumblerRaidblock) return false;
            String ret = Interface.Call("CanTeleport", player) as String;
            return ret != null;
        }
        void OnEntitySpawned(AutoTurret turret) => SetupTurret(turret);

        bool CanPickupEntity(BasePlayer player, AutoTurret turret)
        {
            if (turret != null && turret.skinID != 0 && turret.OwnerID != 0 && TurretList.ContainsKey(turret.skinID))
            {
                RemoveSwitch(turret);

                turret.skinID = 0;
                return true;
            }
            return true;
        }
        private void RegisteredTurret(UInt64 ID, UInt64 PlayerID, ElectricSwitch smartSwitch, BaseEntity turrel, BasePlayer player = null, Boolean IsInit = false)
        {
            ControllerInformation information = new ControllerInformation();
            information.turrel = turrel;
            information.electricSwitch = smartSwitch;
            information.PlayerID = PlayerID;

            UInt64 BuildingID = 0;
            if (player != null && player.GetBuildingPrivilege() != null)
                BuildingID = player.GetBuildingPrivilege().buildingID;
            else if (turrel != null && turrel.GetBuildingPrivilege() != null)
                BuildingID = turrel.GetBuildingPrivilege().buildingID;
            else if (smartSwitch != null && smartSwitch.GetBuildingPrivilege() != null)
                BuildingID = smartSwitch.GetBuildingPrivilege().buildingID;

            information.BuildingID = BuildingID;

            if (!TurretList.ContainsKey(ID))
                TurretList.Add(ID, new List<ControllerInformation> { information });
            else TurretList[ID].Add(information);

            if (IsInit)
                if (!IsLimitPlayer(PlayerID, BuildingID))
                {
                    if (turrel is AutoTurret && (turrel as AutoTurret).currentEnergy <= 0)
                    {
                        turrel.SetFlag(BaseEntity.Flags.On, true);
                        smartSwitch.SetFlag(BaseEntity.Flags.On, true);
                    }
                    else if (turrel is SamSite && (turrel as SamSite).currentEnergy <= 0)
                    {
                        turrel.SetFlag(BaseEntity.Flags.Reserved8, true);
                        smartSwitch.SetFlag(BaseEntity.Flags.On, true);
                    }
                }
        }
        protected override void SaveConfig() => Config.WriteObject(config);
        private const String SwitchPrefab = "assets/prefabs/deployable/playerioents/simpleswitch/switch.prefab";

        public void SendChat(String Message, BasePlayer player, ConVar.Chat.ChatChannel channel = ConVar.Chat.ChatChannel.Global)
        {
            if (IQChat)
                if (config.ReferencesPlugin.IQChatSetting.UIAlertUse)
                    IQChat?.Call("API_ALERT_PLAYER_UI", player, Message);
                else IQChat?.Call("API_ALERT_PLAYER", player, Message, config.ReferencesPlugin.IQChatSetting.CustomPrefix, config.ReferencesPlugin.IQChatSetting.CustomAvatar);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
		   		 		  						  	   		  	 	 		  	 				   		 		  		 	
        
                private void RemoveSwitch(BaseEntity entity)
        {
            if (entity == null || entity.skinID == 0) return;
            UInt64 ID = entity.skinID;

            if (TurretList.ContainsKey(ID))
            {
                ControllerInformation controller = TurretList[ID].FirstOrDefault(x => x.electricSwitch.skinID == ID);
                if (controller != null)
                    controller.electricSwitch.Kill();

                TurretList.Remove(ID);
            }
        }
        ElectricSwitch API_GET_SWITCH(BasePlayer player, BaseEntity turret) => GetSwitchForTurret(turret);
        object OnSwitchToggle(IOEntity entity, BasePlayer player)
        {
            if (entity == null || player == null || entity.skinID == 0) return null;

            if (IsRaidBlocked(player))
                return false;
            
            ElectricSwitch Switch = entity as ElectricSwitch;
            if (Switch == null) return null;

            if (!player.IsBuildingAuthed())
            {
                SendChat(GetLang("IS_BUILDING_BLOCK_TOGGLE", player.UserIDString), player);
                return false;
            }

            if (Switch.HasFlag(BaseEntity.Flags.On))
            {
                TurretToggle(player, Switch);
                return null;
            }

            if (IsTurretElectricalTurned(Switch))
            {
                SendChat(GetLang("IS_TURRET_ELECTRIC_TRUE", player.UserIDString), player);
                return false;
            }

            if (config.LimitController.UseLimitControll)
            {
                UInt64 ID = 0;
                if (config.LimitController.typeLimiter == TypeLimiter.Building && player.GetBuildingPrivilege() != null)
                {
                    ID = player.GetBuildingPrivilege().buildingID;
                    UInt64 IDTurret = Switch.skinID;
                    if (TurretList.ContainsKey(IDTurret))
                    {
                        ControllerInformation controller = TurretList[IDTurret].FirstOrDefault(x => x.electricSwitch == Switch);
                        if (controller == null) return null;

                        controller.BuildingID = ID;
                    }
                }

                // ["IS_LIMIT_TRUE_OTHER"] = "У владельца турели <color=#dd6363>превышен</color> лимит активных турелей <color=#dd6363>БЕЗ ЭЛЕКТРИЧЕСТВА</color>",
                // ["INFORMATION_USER_ON_OTHER"] = "Вы успешно <color=#66e28b>включили</color> турель игрока, игроку доступно еще для включения <color=#dd6363>{0}</color> турели",
                // ["INFORMATION_USER_OFF_OTHER"] = "Вы успешно <color=#dd6363>выключили</color> турель игрока, игрока доступно еще для включения <color=#dd6363>{0}</color> турели",
              
                if (IsLimitPlayer(Switch.OwnerID, ID))
                {
                    SendChat(GetLang(Switch.OwnerID != player.userID ? "IS_LIMIT_TRUE_OTHER" : "IS_LIMIT_TRUE", player.UserIDString), player);
                    return false;
                }
            }

            TurretToggle(player, Switch);
            return null;
        }

        private Boolean IsTurretElectricalTurned(ElectricSwitch Switch)
        {
            if (Switch == null) return false;
            BaseEntity turrel = GetTurretForSwitch(Switch);

            if (turrel != null)
            {
                if (turrel is AutoTurret)
                    return (turrel as AutoTurret)?.GetConnectedInputCount() > 0;

                if (turrel is SamSite)
                    return (turrel as SamSite)?.GetConnectedInputCount() > 0;
            }
		   		 		  						  	   		  	 	 		  	 				   		 		  		 	
            return false;
        }
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["IS_LIMIT_TRUE"] = "At you <color=#dd6363>exceeded</color> limit of active turrets <color=#dd6363>WITHOUT ELECTRICITY</color>",
                ["IS_TURRET_ELECTRIC_TRUE"] = "This turret is connected <color=#dd6363>to electricity</color>, you can't use the switch!",
                ["IS_BUILDING_BLOCK_TOGGLE"] = "You cannot use the switch in <color=#dd6363>someone else's house</color>",
                ["INFORMATION_USER_ON"] = "You have successfully <color=#66e28b>enabled</color> the turret, you can still enable <color=#dd6363>{0}</color> turret",
                ["INFORMATION_USER_OFF"] = "You have successfully <color=#dd6363>disabled</color> the turret, you can still enable <color=#dd6363>{0}</color> turret",
                ["INFORMATION_MY_LIMIT"] = "<color=#dd6363> is available to you</color> to enable <color=#dd6363>{0}</color> turrets",
                ["SYNTAX_COMMAND_ERROR"] = "<color=#dd6363>Syntax error : </color>\nUse the commands :\n1. t on - enables all disabled turrets\n2. t off - turns off all enabled turrets\n3. t limit - shows how many turrets are still available to you without electricity",
                ["PERMISSION_COMMAND_ERROR"] = "<color=#dd6363>Access error : </color>\nYou don't have enough rights to use this command!",

                ["IS_LIMIT_TRUE_OTHER"] = "The owner of the turret <color=#dd6363>exceeded</color> limit of active turrets <color=#dd6363>WITHOUT ELECTRICITY</color>",
                ["INFORMATION_USER_ON_OTHER"] = "You have successfully <color=#66e28b>enabled</color> the player's turret, the player can still turn on <color=#dd6363>{0}</color> turret",
                ["INFORMATION_USER_OFF_OTHER"] = "You have successfully <color=#dd6363>disabled</color> тthe player's turret, the player is still available for inclusion <color=#dd6363>{0}</color> turret",
            }, this);
		   		 		  						  	   		  	 	 		  	 				   		 		  		 	
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["IS_LIMIT_TRUE"] = "У вас <color=#dd6363>превышен</color> лимит активных турелей <color=#dd6363>БЕЗ ЭЛЕКТРИЧЕСТВА</color>",
                ["IS_TURRET_ELECTRIC_TRUE"] = "Данная турель подключена <color=#dd6363>к электричеству</color>, вы не можете использовать рубильник!",
                ["IS_BUILDING_BLOCK_TOGGLE"] = "Вы не можете использовать рубильник в <color=#dd6363>чужом доме</color>",
                ["INFORMATION_USER_ON"] = "Вы успешно <color=#66e28b>включили</color> турель, вам доступно еще для включения <color=#dd6363>{0}</color> турели",
                ["INFORMATION_USER_OFF"] = "Вы успешно <color=#dd6363>выключили</color> турель, вам доступно еще для включения <color=#dd6363>{0}</color> турели",
                ["INFORMATION_MY_LIMIT"] = "Вам <color=#dd6363>доступно</color> для включения <color=#dd6363>{0}</color> турелей",
                ["SYNTAX_COMMAND_ERROR"] = "<color=#dd6363>Ошибка синтаксиса : </color>\nИспользуйте команды :\n1. t on - включает все выключенные\n2. t off - выключает все включенные турели\n3. t limit - показывает сколько вам еще доступно турелей без электричества",
                ["PERMISSION_COMMAND_ERROR"] = "<color=#dd6363>Ошибка доступа : </color>\nУ вас недостаточно прав для использования данной команды!",
                
                ["IS_LIMIT_TRUE_OTHER"] = "У владельца турели <color=#dd6363>превышен</color> лимит активных турелей <color=#dd6363>БЕЗ ЭЛЕКТРИЧЕСТВА</color>",
                ["INFORMATION_USER_ON_OTHER"] = "Вы успешно <color=#66e28b>включили</color> турель игрока, игроку доступно еще для включения <color=#dd6363>{0}</color> турели",
                ["INFORMATION_USER_OFF_OTHER"] = "Вы успешно <color=#dd6363>выключили</color> турель игрока, игрока доступно еще для включения <color=#dd6363>{0}</color> турели",

            }, this, "ru");
            PrintWarning("Logs : #32121202 | Языковой файл загружен успешно"); 
        }
        private const Boolean LanguageEn = false;


        
        
        [ChatCommand("t")]
        void TurretControllChatCommand(BasePlayer player, String cmd, String[] arg)
        {
            if (player == null || arg == null || arg.Length == 0)
            {
                SendChat(GetLang("SYNTAX_COMMAND_ERROR", player.UserIDString), player);
                return;
            }

            String Action = arg[0];
            if (String.IsNullOrWhiteSpace(Action)) return;
            switch (Action)
            {
                case "limit":
                    {
                        UInt64 ID = 0;
                        if (config.LimitController.typeLimiter == TypeLimiter.Building && player.GetBuildingPrivilege() != null)
                            ID = player.GetBuildingPrivilege().buildingID;

                        String Lang = GetLang("INFORMATION_MY_LIMIT", player.UserIDString, (GetLimitPlayer(player.userID) - GetAmountTurretPlayer(player.userID, ID)));
                        SendChat(Lang, player);
                        break;
                    }
                case "off":
                    {
                        if (!permission.UserHasPermission(player.UserIDString, PermissionTurnAllTurretsOff))
                        {
                            SendChat(GetLang("PERMISSION_COMMAND_ERROR", player.UserIDString), player);
                            return;
                        }
		   		 		  						  	   		  	 	 		  	 				   		 		  		 	
                        Int32 LimitPlayer = GetLimitPlayer(player.userID);
                        Dictionary<BaseEntity, ElectricSwitch> PlayerTurrets = GetPlayerTurretAndSwitch(player);
                        if (PlayerTurrets != null)
                        {
                            foreach (KeyValuePair<BaseEntity, ElectricSwitch> Item in PlayerTurrets.Where(x => x.Value.HasFlag(BaseEntity.Flags.On)).Take(LimitPlayer))
                                if (Item.Key is AutoTurret)
                                {
                                    if (Item.Key.HasFlag(BaseEntity.Flags.On))
                                    {
                                        Item.Key.SetFlag(BaseEntity.Flags.On, false);
                                        Item.Value.SetFlag(BaseEntity.Flags.On, false);
                                        LimitPlayer--;
                                    }
                                }
                                else if (Item.Key is SamSite)
                                {
                                    if (Item.Key.HasFlag(BaseEntity.Flags.Reserved8))
                                    {
                                        Item.Key.SetFlag(BaseEntity.Flags.Reserved8, false);
                                        Item.Value.SetFlag(BaseEntity.Flags.On, false);
                                        LimitPlayer--;
                                    }
                                }
                        }
                        break;
                    }
                case "on":
                    {
                        if (!permission.UserHasPermission(player.UserIDString, PermissionTurnAllTurretsOn))
                        {
                            SendChat(GetLang("PERMISSION_COMMAND_ERROR", player.UserIDString), player);
                            return;
                        }

                        Int32 LimitPlayer = GetLimitPlayer(player.userID);
                        Dictionary<BaseEntity, ElectricSwitch> PlayerTurrets = GetPlayerTurretAndSwitch(player);
                        if (PlayerTurrets != null)
                        {
                            foreach (KeyValuePair<BaseEntity, ElectricSwitch> Item in PlayerTurrets.Where(x => !x.Value.HasFlag(BaseEntity.Flags.On)).Take(LimitPlayer))
                                if (Item.Key is AutoTurret)
                                {
                                    if (!Item.Key.HasFlag(BaseEntity.Flags.On))
                                    {
                                        Item.Key.SetFlag(BaseEntity.Flags.On, true);
                                        Item.Value.SetFlag(BaseEntity.Flags.On, true);
                                        LimitPlayer--;
                                    }
                                }
                                else if (Item.Key is SamSite)
                                {
                                    if (!Item.Key.HasFlag(BaseEntity.Flags.Reserved8))
                                    {
                                        Item.Key.SetFlag(BaseEntity.Flags.Reserved8, true);
                                        Item.Value.SetFlag(BaseEntity.Flags.On, true);
                                        LimitPlayer--;
                                    }
                                }
                        }
                        break;
                    }
            }
        }

        
        
        private static Configuration config = new Configuration();

        void OnEntityKill(AutoTurret turret) => RemoveSwitch(turret);
      
        private ElectricSwitch GetSwitchForTurret(BaseEntity Turret)
        {
            if (Turret == null || Turret.IsDestroyed || !TurretList.ContainsKey(Turret.skinID)) return null;
            List<ControllerInformation> InformationList = TurretList[Turret.skinID];
            if (InformationList == null) return null;

            foreach (ControllerInformation Info in InformationList)
                if (Info.electricSwitch.skinID.Equals(Turret.skinID))
                    return Info.electricSwitch;

            return null;
        }

        object OnWireConnect(BasePlayer player, IOEntity entity1, int inputs, IOEntity entity2, int outputs)
        {
            if ((entity1 is AutoTurret) || (entity1 is SamSite))
            {
                ElectricSwitch Switch = GetSwitchForTurret(entity1);
                if (Switch == null) return null;

                if (Switch.HasFlag(BaseEntity.Flags.On))
                {
                    Switch.SetFlag(BaseEntity.Flags.On, false);
                    Switch.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                    Switch.MarkDirty();
                }
            }
            return null;
        }
                
        
                private enum TypeLimiter
        {
            Player,
            Building
        }
        
        private Int32 GetAmountTurretPlayer(UInt64 playerID, UInt64 ID)
        {
            TypeLimiter Type = config.LimitController.typeLimiter;
            
            Int32 CountTurret = 0;

            if (Type == TypeLimiter.Player)
            {
                foreach (KeyValuePair<UInt64, List<ControllerInformation>> Turrets in TurretList)
                foreach (ControllerInformation ControllerInformation in Turrets.Value)
                {
                    if (ControllerInformation.PlayerID == playerID
                        && (ControllerInformation.turrel != null && !ControllerInformation.turrel.IsDestroyed)
                        && ControllerInformation.electricSwitch != null
                        && !ControllerInformation.electricSwitch.IsDestroyed
                        && !IsTurretElectricalTurned(ControllerInformation.electricSwitch)
                        && (ControllerInformation.turrel != null &&
                            ((ControllerInformation.turrel is SamSite &&
                              ControllerInformation.turrel.HasFlag(BaseEntity.Flags.Reserved8)) ||
                             (ControllerInformation.turrel is AutoTurret &&
                              ControllerInformation.turrel.HasFlag(BaseEntity.Flags.On)))))
                        CountTurret++;
                }
            }
            else
            {
                foreach (KeyValuePair<UInt64, List<ControllerInformation>> Turrets in TurretList)
                    foreach (ControllerInformation ControllerInformation in Turrets.Value)
                        if (ControllerInformation.BuildingID == ID
                        && (ControllerInformation.turrel != null && !ControllerInformation.turrel.IsDestroyed)
                        && ControllerInformation.electricSwitch != null
                        && !ControllerInformation.electricSwitch.IsDestroyed
                        && !IsTurretElectricalTurned(ControllerInformation.electricSwitch)
                        && (ControllerInformation.turrel != null && ((ControllerInformation.turrel is SamSite && ControllerInformation.turrel.HasFlag(BaseEntity.Flags.Reserved8)) || (ControllerInformation.turrel is AutoTurret && ControllerInformation.turrel.HasFlag(BaseEntity.Flags.On)))))
                            CountTurret++;
            }

            return CountTurret;
        }

        private void InitializeData()
        {
            List<BaseNetworkable> Turrets = BaseNetworkable.serverEntities.Where(b => b != null && IDsPrefabs.Contains(b.prefabID) && !TurretList.ContainsKey((b as BaseEntity).skinID)).ToList();
            if (Turrets == null) return;

            for (Int32 index = 0; index < Turrets.Count; index++)
            {
                BaseEntity turrel = Turrets[index] as BaseEntity;

                if (turrel != null)
                    SetupTurret(turrel, true);
            }
            Subscribe(nameof(OnEntitySpawned));
        }
        private class Configuration
        {

            internal class LimitControll
            {
                [JsonProperty(LanguageEn ? "Limit Type: 0 - Player, 1 - Building" : "Тип лимита : 0 - На игрока, 1 - На шкаф")]
                public TypeLimiter typeLimiter;
                [JsonProperty(LanguageEn ? "Limit turrets WITHOUT electricity (If the player does not have privileges)" : "Лимит турелей БЕЗ электричества (Если у игрока нет привилегий)")]
                public Int32 LimitAmount;
                [JsonProperty(LanguageEn ? "Use the limit on turrets WITHOUT electricity? (true - yes/false - no)" : "Использовать лимит на туррели БЕЗ электричества? (true - да/false - нет)")]
                public Boolean UseLimitControll;
                [JsonProperty(LanguageEn ? "The limit of turrets WITHOUT electricity by privileges [Permission] = Limit (Make a list from more to less)" : "Лимит турелей БЕЗ электричества по привилегиям [Права] = Лимит (Составляйте список от большего - к меньшему)")]
                public Dictionary<String, Int32> PermissionsLimits = new Dictionary<String, Int32>();
            }
            [JsonProperty(LanguageEn ? "Configuring plugins for Collaboration" : "Настройка плагинов для совместной работы")]
            public ReferenceSettings ReferencesPlugin = new ReferenceSettings();
            internal class ReferenceSettings
            {
                [JsonProperty(LanguageEn ? "Setting up collaboration with IQChat" : "Настройка совместной работы с IQChat")]
                public IQChatPlugin IQChatSetting = new IQChatPlugin();
                internal class IQChatPlugin
                {
                    [JsonProperty(LanguageEn ? "IQChat :Custom prefix in the chat" : "IQChat : Кастомный префикс в чате")]
                    public String CustomPrefix;
                    [JsonProperty(LanguageEn ? "IQChat : Custom avatar in the chat(If required)" : "IQChat : Кастомный аватар в чате(Если требуется)")]
                    public String CustomAvatar;
                    [JsonProperty(LanguageEn ? "IQChat : Use UI notifications" : "IQChat : Использовать UI-уведомления")]
                    public Boolean UIAlertUse = false;
                }
                
                [JsonProperty(LanguageEn ? "Prohibit the use of a switch during a raidBlock?" : "Запретить использовать рубильник во время рейдблока?")]
                public Boolean BlockedTumblerRaidblock;
            }
            [JsonProperty(LanguageEn ? "Setting limits on turrets WITHOUT electricity" : "Настройка лимитов на турели БЕЗ электричества")]
            public LimitControll LimitController = new LimitControll();

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    LimitController = new LimitControll
                    {
                        typeLimiter = TypeLimiter.Building,
                        UseLimitControll = true,
                        LimitAmount = 3,
                        PermissionsLimits = new Dictionary<String, Int32>()
                        {
                            ["iqturret.ultra"] = 150,
                            ["iqturret.king"] = 15,
                            ["iqturret.premium"] = 10,
                            ["iqturret.vip"] = 6,
                        }
                    },
                    ReferencesPlugin = new ReferenceSettings
                    {
                        IQChatSetting = new ReferenceSettings.IQChatPlugin
                        {
                            CustomPrefix = "[<color=#ffff40>IQTurret</color>] ",
                            CustomAvatar = "0",
                            UIAlertUse = false,
                        },
                        BlockedTumblerRaidblock = true,
                    }
                };
            }
        }
        internal class ControllerInformation
        {
            public BaseEntity turrel;
            public ElectricSwitch electricSwitch;
            public UInt64 PlayerID;
            public UInt64 BuildingID;
        }

        
        
        
        private Boolean IsLimitPlayer(UInt64 playerID, UInt64 ID) => GetAmountTurretPlayer(playerID, ID) >= GetLimitPlayer(playerID);
        void OnServerShutdown() => UnloadPlugin();
        void OnEntitySpawned(SamSite samSite) => SetupTurret(samSite);


        
        
        private void UnloadPlugin()
        {
            UnityEngine.Object.Destroy(ImmortalProtection);

            foreach (List<ControllerInformation> TurretInformation in TurretList.Values)
            foreach (ControllerInformation controllerInformation in TurretInformation)
            {
                BaseEntity turrel = controllerInformation.turrel;

                if (turrel != null)
                {
                    if (turrel is AutoTurret && turrel.HasFlag(BaseEntity.Flags.On) &&
                        (turrel as AutoTurret).currentEnergy == 0)
                        turrel.SetFlag(BaseEntity.Flags.On, false);

                    if (turrel is SamSite && turrel.HasFlag(BaseEntity.Flags.Reserved8) &&
                        (turrel as SamSite).currentEnergy == 0)
                        turrel.SetFlag(BaseEntity.Flags.Reserved8, false);
		   		 		  						  	   		  	 	 		  	 				   		 		  		 	
                    turrel.SendNetworkUpdate();
                }

                ElectricSwitch electricSwitch = controllerInformation.electricSwitch;

                if (electricSwitch != null)
                    electricSwitch.Kill();
            }
        }
        void OnEntityKill(SamSite samSite) => RemoveSwitch(samSite);

        private Dictionary<BaseEntity, ElectricSwitch> GetPlayerTurretAndSwitch(BasePlayer player)
        {
            Dictionary<BaseEntity, ElectricSwitch> keyValuePairs = new Dictionary<BaseEntity, ElectricSwitch>();

            if (config.LimitController.typeLimiter == TypeLimiter.Player)
            {
                foreach (KeyValuePair<UInt64, List<ControllerInformation>> Turrets in TurretList)
                    foreach (ControllerInformation ControllerInformation in Turrets.Value)
                        if (ControllerInformation.PlayerID == player.userID
                        && ControllerInformation.turrel != null
                        && !ControllerInformation.turrel.IsDestroyed
                        && ControllerInformation.electricSwitch != null
                        && !ControllerInformation.electricSwitch.IsDestroyed
                        && !IsTurretElectricalTurned(ControllerInformation.electricSwitch))
                            keyValuePairs.Add(ControllerInformation.turrel, ControllerInformation.electricSwitch);
            }
            else
            {
                foreach (KeyValuePair<UInt64, List<ControllerInformation>> Turrets in TurretList)
                    foreach (ControllerInformation ControllerInformation in Turrets.Value)
                        if (ControllerInformation.BuildingID == (player.GetBuildingPrivilege() == null ? 0 : player.GetBuildingPrivilege().buildingID)
                        && ControllerInformation.turrel != null
                        && !ControllerInformation.turrel.IsDestroyed
                        && ControllerInformation.electricSwitch != null
                        && !ControllerInformation.electricSwitch.IsDestroyed
                        && !IsTurretElectricalTurned(ControllerInformation.electricSwitch))
                            keyValuePairs.Add(ControllerInformation.turrel, ControllerInformation.electricSwitch);
            }

            return keyValuePairs;
        }
        private void SetupTurret(BaseEntity turrel, Boolean IsInit = false)
        {
            if (turrel == null || turrel is NPCAutoTurret || !IDsPrefabs.Contains(turrel.prefabID) && TurretList.ContainsKey(turrel.skinID) || turrel.skinID == 1587601905) return;

            UInt64 PlayerID = turrel.OwnerID;
            UInt64 ID = PlayerID + (UInt64)Oxide.Core.Random.Range(999999999);

            Vector3 PositionSwitch = turrel is AutoTurret ? new Vector3(0f, -0.65f, 0.32f) : new Vector3(0f, -0.65f, 0.95f);

            ElectricSwitch smartSwitch = GameManager.server.CreateEntity(SwitchPrefab, turrel.transform.TransformPoint(PositionSwitch), Quaternion.Euler(turrel.transform.rotation.eulerAngles.x, turrel.transform.rotation.eulerAngles.y, 0f), true) as ElectricSwitch;
            if (smartSwitch == null) return;

            smartSwitch.OwnerID = PlayerID;
            smartSwitch.skinID = ID;
            turrel.skinID = ID;

            smartSwitch.pickup.enabled = false;
            smartSwitch.SetFlag(IOEntity.Flag_HasPower, true);
            smartSwitch.baseProtection = ImmortalProtection;

                        foreach (var meshCollider in smartSwitch.GetComponentsInChildren<MeshCollider>())
                UnityEngine.Object.DestroyImmediate(meshCollider);

            UnityEngine.Object.DestroyImmediate(smartSwitch.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.DestroyImmediate(smartSwitch.GetComponent<GroundWatch>());
            
                        foreach (var input in smartSwitch.inputs)
                input.type = IOEntity.IOType.Generic;

            foreach (var output in smartSwitch.outputs)
                output.type = IOEntity.IOType.Generic;
            
            smartSwitch.Spawn();

            smartSwitch.SetFlag(BaseEntity.Flags.Reserved8, true);
            smartSwitch.SetFlag(BaseEntity.Flags.On, false);

            BasePlayer player = BasePlayer.FindByID(PlayerID);
            RegisteredTurret(ID, PlayerID, smartSwitch, turrel, player, IsInit);
        }

            }
}
