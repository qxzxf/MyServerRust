using Oxide.Core.Plugins;
using ConVar;
using Oxide.Game.Rust;
using System;
using ProtoBuf;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core;

namespace Oxide.Plugins
{
 [Info("Friends", "qxzxf", "4.0.3")]
    [Description("Friends system rust")]
    public class Friends : RustPlugin
    {

        
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TEAM_FOUND"] = "Player not found",
                ["TEAM_FOUND_MULTIPLE"] = "Multiple players found with this name\n{0}",
                ["TEAM_SENDINVITETILE"] = "TEAM SYSTEM",
                ["TEAM_SENDINVITE"] = "Player <color=#89f5bf>{0}</color> has sent you a team invitation",
                ["TEAM_NULL"] = "You don't have a team yet.\nTo create one, press (<color=#3eb2f0>TAB</color>)",
                ["TEAM_NULLNICKNAME"] = "You didn't enter a player's name!",
                ["TEAM_NULLNICKNAMENULL"] = "You can't add yourself to the team",
                ["TEAM_ISCOMMAND"] = "Player <color=#89f5bf>{0}</color> is already in a team",
                ["TEAM_MAXTEAMSIZE"] = "There are no available spots in the team",
                ["TEAM_TIMENULL"] = "Your invitation has been declined, the response time has expired",
                ["TEAM_TIMENULS"] = "Invitation from <color=#89f5bf>{0}</color> has been cancelled, the response time has expired",
                ["TEAM_IVITE"] = "You have successfully sent a team invitation to player <color=#89f5bf>{0}</color>",
                ["TEAM_INVITETARGET"] = "Player <color=#89f5bf>{0}</color> has invited you to their team.\nTo accept or decline the invitation, press (<color=#3eb2f0>TAB</color>)",
                ["TEAM_CUPBOARCLEAR"] = "You have kicked a team member.\nThey have been automatically removed from the cupboard access list!",
                ["TEAM_CUPBOARADD"] = "Your new team member (<color=#89f5bf>{0}</color>) has been successfully authorized in cupboards!",
                ["TEAM_CUPBOARADDLEAVE"] = "You have left the team and have been deauthorized from the cupboards",
                ["TEAM_FFON"] = "You've <color=#64f578>enabled</color> friendly fire",
                ["TEAM_FFOFF"] = "You've <color=#f03e3e>disabled</color> friendly fire",
                ["TEAM_FFATTACK"] = "Player: <color=#89f5bf>{0}</color> is your friend!\nYou can't <color=#ff9696>kill</color> them\nTo enable friendly fire, type /team ff",
                ["TEAM_INFO"] = "To create a team, press (<color=#3eb2f0>TAB</color>) and select 'Create Team'.\n" +
                                "Invite a player to the team via (<color=#3eb2f0>TAB</color>) or by the command:\n" +
                                "1. <color=#46bec2>/Team add nickname</color> - Invite to team at a distance\n" +
                                "2. <color=#46bec2>/team ff</color> - Enables or disables friendly fire\n" +
                                "When adding a player to the team, they will automatically be added to <color=#89f5af>Turrets</color>, <color=#89f5af>Lockers</color>, <color=#89f5af>Doors</color>." +
                                "\nWhen removing from the team, the player's access will be <color=#ff9696>revoked</color>.",
                ["TEAM_LOG_1"] = "Player {0} created team #{1}",
                ["TEAM_LOG_2"] = "Player {0} accepted an invitation to team #{1} from player {2}\nCurrent team member list:\n{3}",
                ["TEAM_LOG_3"] = "Player {0} kicked player {2} from team #{1}\nCurrent team member list:\n{3}",
                ["TEAM_LOG_4"] = "The leader of team #{0} left. The new team leader is {1}\nCurrent team member list:\n{2}",
                ["TEAM_LOG_5"] = "Player {0} left team #{1} of player {2}\nCurrent team member list:\n{3}",
                ["TEAM_LOG_6"] = "Team #{0} of player {1} has been disbanded",

            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TEAM_FOUND"] = "Игрок не обнаружен",
                ["TEAM_FOUND_MULTIPLE"] = "Обнаружено несколько игроков с таким именем\n{0}",
                ["TEAM_SENDINVITETILE"] = "СИСТЕМА КОМАНДЫ",
                ["TEAM_SENDINVITE"] = "Игрок <color=#89f5bf>{0}</color> предлагает вам присоединиться к команде",
                ["TEAM_NULL"] = "У вас пока нет команды.\nЧтобы создать команду, нажмите (<color=#3eb2f0>TAB</color>)",
                ["TEAM_NULLNICKNAME"] = "Вы не ввели имя игрока!",
                ["TEAM_NULLNICKNAMENULL"] = "Вы не можете добавить себя в команду",
                ["TEAM_ISCOMMAND"] = "Игрок <color=#89f5bf>{0}</color> уже является членом команды",
                ["TEAM_MAXTEAMSIZE"] = "В команде нет места для новых участников",
                ["TEAM_TIMENULL"] = "Ваше приглашение отклонено, время на ответ истекло",
                ["TEAM_TIMENULS"] = "Приглашение от <color=#89f5bf>{0}</color> отменено, время на ответ истекло",
                ["TEAM_IVITE"] = "Вы успешно отправили приглашение в команду игроку <color=#89f5bf>{0}</color>",
                ["TEAM_INVITETARGET"] = "Игрок <color=#89f5bf>{0}</color> пригласил вас в команду.\nЧтобы принять или отклонить приглашение, нажмите (<color=#3eb2f0>TAB</color>)",
                ["TEAM_CUPBOARCLEAR"] = "Вы исключили участника из команды.\nОн был автоматически удален из доступа к шкафам!",
                ["TEAM_CUPBOARADD"] = "Ваш новый член команды (<color=#89f5bf>{0}</color>) успешно добавлен в доступ к шкафам!",
                ["TEAM_CUPBOARADDLEAVE"] = "Вы покинули команду и были удалены из доступа к шкафам",
                ["TEAM_FFON"] = "Вы <color=#64f578>включили</color> возможность нанесения урона друзьям",
                ["TEAM_FFOFF"] = "Вы <color=#f03e3e>отключили</color> возможность нанесения урона друзьям",
                ["TEAM_FFATTACK"] = "Игрок <color=#89f5bf>{0}</color> - ваш друг!\nВы не можете его <color=#ff9696>атаковать</color>\nЧтобы включить урон по друзьям, напишите /team ff",
                ["TEAM_INFO"] = "Чтобы создать команду, нажмите (<color=#3eb2f0>TAB</color>) и выберите 'Создать команду'.n" +
                                "Пригласить игрока в команду можно через (<color=#3eb2f0>TAB</color>) или командой:\n" +
                                "1. <color=#46bec2>/Team add ник</color> - Пригласить в команду на расстоянии\n" +
                                "2. <color=#46bec2>/team ff</color> - Включает и выключает возможность нанесения урона друзьям\n" +
                                "При добавлении игрока в команду, он будет автоматически добавлен в <color=#89f5af>Турели</color>, <color=#89f5af>шкафы</color>, <color=#89f5af>двери</color>." +
                                "\nПри исключении из команды, доступ игрока будет <color=#ff9696>отменен</color>.",
                ["TEAM_LOG_1"] = "Игрок {0} основал команду #{1}",
                ["TEAM_LOG_2"] = "Игрок {0} присоединился к команде #{1} по приглашению от {2}\nАктуальный список участников команды:\n{3}",
                ["TEAM_LOG_3"] = "Игрок {0} исключил из команды #{1} игрока {2}\nАктуальный список участников команды:\n{3}",
                ["TEAM_LOG_4"] = "Лидер команды #{0} покинул ее. Новым лидером становится {1}\nАктуальный список участников команды:\n{2}",
                ["TEAM_LOG_5"] = "Игрок {0} покинул команду #{1} под управлением игрока {2}\nАктуальный список участников команды:\n{3}",
                ["TEAM_LOG_6"] = "Команда #{0}, возглавляемая игроком {1}, расформирована",
            }, this, "ru");
        }

                
                private List<PlayerNameID> GetTeamMembers(ulong ownerId)
        {
            if (!RelationshipManager.TeamsEnabled()) return null;
            RelationshipManager.PlayerTeam playerTeam = RelationshipManager.ServerInstance.FindPlayersTeam(ownerId);
            List<PlayerNameID> playerNameIDList = new List<PlayerNameID>();

            if (playerTeam == null)
            {
                playerNameIDList.Add(
                    new PlayerNameID
                    { 
                        userid = ownerId, 
                        username = RustCore.FindPlayerById(ownerId)?.displayName ?? string.Empty 
                    }
                );
            }
            else
            {
                if (playerTeam.members != null)
                {
                    foreach (ulong userid in playerTeam.members)
                    {
                        string username = RustCore.FindPlayerById(userid)?.displayName ?? string.Empty;
                        playerNameIDList.Add(new PlayerNameID { userid = userid, username = username });
                    }
                }
            }

            return playerNameIDList;
        }

        
        
        private object CanUseLockedEntity(BasePlayer player, KeyLock keyLock)
        {
            if (player == null || keyLock == null || !keyLock.IsLocked())
                return null;
            BaseEntity parentEntity = keyLock.GetParentEntity();
            ulong ownerID = keyLock.OwnerID.IsSteamId() ? keyLock.OwnerID : parentEntity != null ? parentEntity.OwnerID : 0;
            if (!ownerID.IsSteamId() || ownerID == player.userID)
                return null;
            if (!HasFriend(ownerID, player.userID)) 
                return null;
            return true;
        }
        private const bool RU = true;

        private string[] IsFriendOf(string playerS)
        {
            ulong playerId;
            if (ulong.TryParse(playerS, out playerId))
            {
                ulong[] friendIds = IsFriendOf(playerId);
                string[] friendStrings = new string[friendIds.Length];
		   		 		  						  	   		  	   		  		 			   		 		  		 	
                for (int i = 0; i < friendIds.Length; i++)
                {
                    friendStrings[i] = friendIds[i].ToString();
                }

                return friendStrings;
            }

            return new string[] { };
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info) => TruePVE != null ? null : CanEntityTakeDamage(entity, info);

        private class Configuration
        {

            [JsonProperty(PropertyName = RU ? "Префикс в чате (IQChat)" : "Chat prefix (IQChat)")]
            public string ChatPrefix = "<color=#5cd65070>[Friends]</color>\n";

            [JsonProperty(PropertyName = RU ? "Активировать авторизацию друзей для ключевых замков?" : "Enable friend authorization for key locks?")]
            public bool KeyLockAuthUse = true;

            [JsonProperty(PropertyName = RU ? "Время ожидания принятия приглашения в команду (в секундах)" : "Time to accept team invite (in seconds)")]
            public float InviteAcceptTime = 20f;

            [JsonProperty(PropertyName = RU ? "Метод авторизации для кодовых замков (guestPlayers - гостевая авторизация, whitelistPlayers - полная авторизация)" : "Authorization method for code locks (guestPlayers - guest authorization, whitelistPlayers - full authorization)")]
            public string MethodCodeLockAuthUse = "guestPlayers";

            [JsonProperty(PropertyName = RU ? "Активировать авторизацию друзей для турелей?" : "Enable friend authorization for turrets?")]
            public bool TurretAuthUse = true;

            [JsonProperty(PropertyName = RU ? "Активировать авторизацию друзей для кодовых замков?" : "Enable friend authorization for code locks?")]
            public bool CodeLockAuthUse = true;

            [JsonProperty(PropertyName = RU ? "Включить систему логирования ?" : "Enable logging system ?")]
            public bool LoggerUse = false;

            [JsonProperty(PropertyName = RU ? "Активировать авторизацию друзей для шкафов?" : "Enable friend authorization for cupboards?")]
            public bool CupboardAuthUse = true;

            [JsonProperty(PropertyName = RU ? "Максимальное число друзей" : "Maximum number of friends")]
            public int MaxTeamSize = 3;
            [JsonProperty(PropertyName = RU ? "Команды чата" : "Chat commands")]
            public string[] ChatCommands = {"team", "ff", "friend"};

            [JsonProperty(PropertyName = RU ? "Активировать авторизацию друзей для ПВО?" : "Enable friend authorization for SAM sites?")]
            public bool SamSiteAuthUse = true;
            
        }
        
                [PluginReference]
        private readonly Plugin IQChat, TruePVE, EventHelper, Battles, Duel, Duelist, ArenaTournament;

        
        
        private object OnSamSiteTarget(SamSite entity, BaseCombatEntity target)
        {
            BaseVehicle vehicle = target as BaseVehicle;
            if (ReferenceEquals(vehicle, null) || entity.OwnerID <= 0)
                return null;
            BasePlayer player = vehicle.GetDriver();
            if (player == null)
                return null;
            if(player.userID == entity.OwnerID)
                return false;
            if (HasFriend(entity.OwnerID, player.userID))
                return false;
            return null;
        }

        private bool WasFriend(ulong player, ulong target) => HasFriend(player, target);

        private void Unsubscribes()
        {
            foreach (string hook in _hooks)
            {
                Unsubscribe(hook);
            }
        }
        private bool WereFriendsS(string playerS, string friendS) => HasFriend(ulong.Parse(playerS), ulong.Parse(friendS));
        private readonly Dictionary<ulong, List<BuildingPrivlidge>> _playerCupboardsCache = new Dictionary<ulong, List<BuildingPrivlidge>>();


        private void RemoveCupboardCache(BuildingPrivlidge buildingPrivlidge)
        {
            if (buildingPrivlidge == null || !buildingPrivlidge.OwnerID.IsSteamId()) return;

            List<BuildingPrivlidge> buildingPrivlidges;
            if (_playerCupboardsCache.TryGetValue(buildingPrivlidge.OwnerID, out buildingPrivlidges) 
                && buildingPrivlidges.Contains(buildingPrivlidge))
                buildingPrivlidges.Remove(buildingPrivlidge);
        }
        
        
        private Configuration _config;
		   		 		  						  	   		  	   		  		 			   		 		  		 	
        private bool HadFriends(string playerS, string targetS) => HasFriend(ulong.Parse(playerS), ulong.Parse(targetS));
        
        private void LogMessage(string message)
        {
            if(!_config.LoggerUse) return;
            _logQueue.Enqueue(new LogEntry("history", GetCurrentTimestamp(), message));   
            while(_logQueue.Count > 0)
            {                   
                if (AttemptWriteToLogFile(_logQueue.Peek().Prefix, _logQueue.Peek().Timestamp, _logQueue.Peek().Message))
                    _logQueue.Dequeue();
                else
                    break;
            }               
        }  

        private bool WereFriends(ulong player, ulong target) => HasFriend(player, target);

        private void LoadData() =>
            _ffTeam = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, bool>>("FriendlyFireData");

        private void UpdateLockAuth(List<CodeLock> codeLocks, ulong ownerId)
        {
            List<PlayerNameID> team = GetTeamMembers(ownerId);
            if (team == null || codeLocks == null || codeLocks.Count == 0) return;

            foreach (CodeLock codeLock in codeLocks)
            {
                if (codeLock.IsDestroyed) continue;
                ReAuthTeamInCodeLock(team, codeLock, ownerId);
            }
        }

        private bool AreFriends(string playerS, string friendS)
        {
            if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(friendS)) return false;

            ulong playerId, friendId;
            if (ulong.TryParse(playerS, out playerId) && ulong.TryParse(friendS, out friendId))
            {
                return AreFriends(playerId, friendId);
            }

            return false;
        }
        
        private string[] GetFriendList(string playerS)
        {
            ulong playerId;
            if (ulong.TryParse(playerS, out playerId))
            {
                return GetFriendList(playerId);
            }
            return new string[] { }; 
        }
        public static StringBuilder StringBuilderInstance;
        private void OnEntitySpawned(CodeLock codeLock) => AddCodeLockCache(codeLock, true);

        
        
        private void OnTeamCreated(BasePlayer player, RelationshipManager.PlayerTeam team)
        {
            LogMessage(GetLang("TEAM_LOG_1", null, player.userID, team.teamID));
        }

        private bool AreFriends(ulong playerId, ulong friendId)
        {
            RelationshipManager.PlayerTeam playerTeam, friendTeam;

            if (RelationshipManager.ServerInstance.playerToTeam.TryGetValue(playerId, out playerTeam) &&
                RelationshipManager.ServerInstance.playerToTeam.TryGetValue(friendId, out friendTeam))
            {
                return playerTeam.members.Contains(friendId) && friendTeam.members.Contains(playerId);
            }

            return false;
        }

        
        
        private Dictionary<ulong, bool> _ffTeam = new Dictionary<ulong, bool>();
		   		 		  						  	   		  	   		  		 			   		 		  		 	
        private class LogEntry
        {
            public string Prefix;
            public string Timestamp;
            public string Message;

            public LogEntry(string prefix, string timestamp, string message)
            {
                this.Prefix = prefix;
                this.Timestamp = timestamp;
                this.Message = message;
            }
        }

        private void SaveData() => Interface.GetMod().DataFileSystem.WriteObject("FriendlyFireData", _ffTeam);
        private bool HadFriend(ulong player, ulong target) => HasFriend(player, target);
        private void OnEntityKill(CodeLock codeLock) => RemoveCodeLockCache(codeLock);

                
                private bool HasFriend(ulong playerId, ulong friendId)
        {
            RelationshipManager.PlayerTeam team;
            if (RelationshipManager.ServerInstance.playerToTeam.TryGetValue(playerId, out team))
            {
                return team.members.Contains(friendId);
            }

            return false;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null)
                    LoadDefaultConfig();
                ValidateConfig();
                SaveConfig();
            }
            catch (JsonException ex)
            {
                Debug.LogException(ex);
                LoadDefaultConfig();
            }
        }

        private string GetLang(string langKey, string userID = null, params object[] args)
        {
            StringBuilderInstance.Clear();
            if (args != null)
            {
                StringBuilderInstance.AppendFormat(lang.GetMessage(langKey, this, userID), args);
                return StringBuilderInstance.ToString();
            }
            return lang.GetMessage(langKey, this, userID);
        }
                
        
        private void ReAuthTeamInCodeLock(List<PlayerNameID> teamMember, CodeLock codeLock, ulong ownerId)
        {
            if (teamMember == null) return;

            bool isGuest = _config.MethodCodeLockAuthUse == "guestPlayers";
            List<ulong> authList = isGuest ? codeLock.guestPlayers : codeLock.whitelistPlayers;

            if (isGuest)
                authList.Clear();
            else
                authList.RemoveAll(owner => owner != ownerId);
            
            foreach (PlayerNameID friend in teamMember)
                authList.Add(friend.userid);
            codeLock.SendNetworkUpdate();
        }

        private bool IsDuel(UInt64 userID)
        {
            object playerId = ObjectCache.Get(userID);
            BasePlayer player = null;
            if(Duel != null || Duelist != null)
                player = BasePlayer.FindByID(userID);
            
            if (EventHelper != null)
            {
                object result = EventHelper.Call("EMAtEvent", playerId);
                if (result is bool && ((bool) result) == true)
                    return true;
            }

            
            if (Battles != null && Battles.Call<bool>("IsPlayerOnBattle", playerId))
                return true;

            
            if (Duel != null && Duel.Call<bool>("IsPlayerOnActiveDuel", player))
                return true;
            if (Duelist != null && Duelist.Call<bool>("inEvent", player)) 
                return true;
            
            if (ArenaTournament != null && ArenaTournament.Call<bool>("IsOnTournament", playerId)) 
                return true;
            
            return false;
        }
		   		 		  						  	   		  	   		  		 			   		 		  		 	
        private bool HasFriends(string playerS, string friendS)
        {
            if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(friendS)) return false;

            ulong playerId, friendId;
            if (ulong.TryParse(playerS, out playerId) && ulong.TryParse(friendS, out friendId))
            {
                return HasFriend(playerId, friendId);
            }

            return false;
        }


        
                
        
        private readonly List<string> _hooks = new List<string>
        {
            nameof(OnTeamCreated),
            nameof(OnTeamAcceptInvite),
            nameof(OnTeamKick),
            nameof(OnTeamLeave),
            nameof(OnTeamDisbanded),
            nameof(CanEntityTakeDamage),
            nameof(OnEntityTakeDamage), 
            nameof(OnEntitySpawned),
            nameof(OnEntityKill), 
            nameof(OnTurretTarget), 
            nameof(OnSamSiteTarget), 
            nameof(CanChangeCode), 
            nameof(CanUseLockedEntity)
        };

        
                
                private void UpdateCupboardAuth(List<BuildingPrivlidge> buildingPrivlidges, ulong ownerId)
        {
            if (buildingPrivlidges == null || buildingPrivlidges.Count == 0) return;

            List<PlayerNameID> team = GetTeamMembers(ownerId);
            if (team == null) return;
            foreach (BuildingPrivlidge buildingPrivlidge in buildingPrivlidges)
            {
                if (buildingPrivlidge.IsDestroyed) continue;
                if (!buildingPrivlidge.authorizedPlayers.Exists(p => p.userid == ownerId)) continue;
                buildingPrivlidge.authorizedPlayers.Clear();
                foreach (PlayerNameID friend in team)
                {
                    buildingPrivlidge.authorizedPlayers.Add(friend);
                }

                buildingPrivlidge.SendNetworkUpdate();
            }
        }
        
        private bool AttemptWriteToLogFile(string prefix, string timestamp, string message)
        {
            try
            {
                LogToFile(prefix, timestamp + message, this);
            }
            catch (Exception ex)
            {
                PrintError(ex.ToString());
                return false;
            }
            return true;
        }
        private void OnTeamDisbanded(RelationshipManager.PlayerTeam playerTeam)
        {
            if (playerTeam == null) return;
            LogMessage(GetLang("TEAM_LOG_6", null, playerTeam.teamID, playerTeam.teamLeader));
            UpdateTeamAuth(playerTeam.members);
        }

        private bool IsFriend(ulong playerId, ulong friendId)
        {
            return HasFriend(playerId, friendId);
        }

        
        
        private static class ObjectCache
        {
            private static readonly object True = true;
            private static readonly object False = false;

            private static class StaticObjectCache<T>
            {
                private static readonly Dictionary<T, object> CacheByValue = new Dictionary<T, object>();

                public static object Get(T value)
                {
                    object cachedObject;
                    if (!CacheByValue.TryGetValue(value, out cachedObject))
                    {
                        cachedObject = value;
                        CacheByValue[value] = cachedObject;
                    }
                    return cachedObject;
                }
            }

            public static object Get<T>(T value)
            {
                return StaticObjectCache<T>.Get(value);
            }

            public static object Get(bool value)
            {
                return value ? True : False;
            }
        }

        
        
        private static Queue<LogEntry> _logQueue = new Queue<LogEntry>();

                
                private void UpdateTeamAuth(List<ulong> teamMembers)
        {
            if (teamMembers == null || teamMembers.Count == 0) return;

            bool codeLock = _config.CodeLockAuthUse;
            bool cupboard = _config.CupboardAuthUse;
            
            if(!codeLock && !cupboard) return;

            foreach (ulong member in teamMembers)
            {
                if (cupboard)
                {
                    List<BuildingPrivlidge> cupboards;
                    if (_playerCupboardsCache.TryGetValue(member, out cupboards) && cupboards != null && cupboards.Count > 0)
                    {
                        UpdateCupboardAuth(cupboards, member);
                    }
                }
                if (codeLock)
                {
                    List<CodeLock> memberLocks;
                    if (_playerLocksCache.TryGetValue(member, out memberLocks) && memberLocks != null && memberLocks.Count > 0)
                    {
                        UpdateLockAuth(memberLocks, member);
                    }
                }   
            }
        }

        private ulong[] GetFriends(ulong playerId)
        {
            HashSet<ulong> members = new HashSet<ulong>();
            foreach (string member in GetFriendList(playerId))
            {
                ulong parsedMember;
                if (ulong.TryParse(member, out parsedMember))
                {
                    members.Add(parsedMember);
                }
            }
            
            ulong[] memberArray = new ulong[members.Count];
            members.CopyTo(memberArray);
            return memberArray;
        }

        private ulong[] IsFriendOf(ulong playerId)
        {
            HashSet<ulong> members = new HashSet<ulong>();
            foreach (string member in GetFriendList(playerId))
            {
                ulong parsedMember;
                if (ulong.TryParse(member, out parsedMember) && parsedMember != playerId)
                {
                    members.Add(parsedMember);
                }
            }
            ulong[] memberArray = new ulong[members.Count];
            members.CopyTo(memberArray);
            return memberArray;
        }
        private bool WasFriendS(string playerS, string friendS) => HasFriend(ulong.Parse(playerS), ulong.Parse(friendS));
        
        private string[] GetFriends(string playerS)
        {
            ulong playerId;
            if (ulong.TryParse(playerS, out playerId))
            {
                ulong[] friendIds = GetFriends(playerId);
                string[] friendStrings = new string[friendIds.Length];

                for (int i = 0; i < friendIds.Length; i++)
                {
                    friendStrings[i] = friendIds[i].ToString();
                }

                return friendStrings;
            }

            return new string[] { };
        }

        private void OnServerInitialized()
        {
            StringBuilderInstance = new StringBuilder();
            RelationshipManager.maxTeamSize = _config.MaxTeamSize;
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
            Subscribes();
            
            foreach (string command in _config.ChatCommands)
                cmd.AddChatCommand(command, this, nameof(CmdTeamCommand));
            
            timer.Once(3F, TriggerDelayedInitialization);
            
        }

        
        
        void CmdTeamCommand(BasePlayer player, string command, string[] arg)
        {
            if (arg?.Length == 0)
            {
                SendChat(player, GetLang("TEAM_INFO", player.UserIDString));
                return;
            }

            RelationshipManager.PlayerTeam team = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
            
            if (team == null)
            {
                SendChat(player, GetLang("TEAM_NULL", player.UserIDString));
                return;
            }
            
            switch (arg[0])
            {
                case "invite":
                case "add":
                {
                    if (arg.Length != 2)
                    {
                        SendChat(player, GetLang("TEAM_NULLNICKNAME", player.UserIDString));
                        return;
                    }

                    BasePlayer target = FindOnlinePlayer(player, arg[1]);

                    if (target == null)
                    {
                        return;
                    }

                    if (target == player)
                    {
                        SendChat(player, GetLang("TEAM_NULLNICKNAMENULL", player.UserIDString));
                        return;
                    }

                    if (target.currentTeam != 0)
                    {
                        SendChat(player, GetLang("TEAM_ISCOMMAND", player.UserIDString, target.displayName));
                        return;
                    }

                    if (team.members.Count >= _config.MaxTeamSize)
                    {
                        SendChat(player, GetLang("TEAM_MAXTEAMSIZE", player.UserIDString));
                        return;
                    }
                    
                    timer.Once(_config.InviteAcceptTime, () =>
                    {
                        if (!team.members.Contains(target.userID))
                        {
                            if (team == null)
                            {
                                player.ClearPendingInvite();
                            }
                            else
                            {
                                team.RejectInvite(target);
                            }

                            SendChat(player, GetLang("TEAM_TIMENULL", player.UserIDString));
                            SendChat(target, GetLang("TEAM_TIMENULS", target.UserIDString, player.displayName));
                        }
                    });
                    team.SendInvite(target);
                    team.MarkDirty();
                    SendChat(player, GetLang("TEAM_IVITE", player.UserIDString, target.displayName));
                    SendChat(target, GetLang("TEAM_INVITETARGET", target.UserIDString, player.displayName));
                    break;
                }
                case "ff":
                {
                    if (!_ffTeam.ContainsKey(player.userID))
                        _ffTeam[player.userID] = false;
		   		 		  						  	   		  	   		  		 			   		 		  		 	
                    if (!_ffTeam[player.userID])
                    {
                        _ffTeam[player.userID] = true;
                        SendChat(player, GetLang("TEAM_FFON", player.UserIDString));
                    }
                    else
                    {
                        _ffTeam[player.userID] = false;
                        SendChat(player, GetLang("TEAM_FFOFF", player.UserIDString));
                    }

                    break;
                }
            }
        }
        
        private bool IsFriends(string playerS, string friendS)
        {
            if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(friendS)) return false;

            ulong playerId, friendId;
            if (ulong.TryParse(playerS, out playerId) && ulong.TryParse(friendS, out friendId))
            {
                return IsFriend(playerId, friendId);
            }

            return false;
        }
        private string[] GetFriendList(ulong playerId)
        {
            RelationshipManager.PlayerTeam teamFind = RelationshipManager.ServerInstance.FindPlayersTeam(playerId);
    
            if (teamFind != null)
            {
                List<ulong> teamMembers = teamFind.members;
                string[] memberStrings = new string[teamMembers.Count];

                for (int i = 0; i < teamMembers.Count; i++)
                {
                    memberStrings[i] = teamMembers[i].ToString();
                }

                return memberStrings;
            }
		   		 		  						  	   		  	   		  		 			   		 		  		 	
            return new string[] { };
        }
        private void OnEntitySpawned(BuildingPrivlidge buildingPrivlidge) => AddCupboardCache(buildingPrivlidge, true);

        private void Unload()
        {
            SaveData();
            StringBuilderInstance = null;
        } 
		   		 		  						  	   		  	   		  		 			   		 		  		 	
        private void RemoveCodeLockCache(CodeLock codeLock)
        {
            if (codeLock == null || !codeLock.OwnerID.IsSteamId()) return;

            List<CodeLock> codeLocks;
            if (_playerLocksCache.TryGetValue(codeLock.OwnerID, out codeLocks) 
                && codeLocks.Contains(codeLock))
                codeLocks.Remove(codeLock);
        }

        private void OnTeamKick(RelationshipManager.PlayerTeam team, BasePlayer player, ulong target)
        {
            NextTick(() =>
            {
                if (team == null) return;
                if (!team.members.Contains(target))
                {
                    UpdateTeamAuth(new List<ulong>(team.members) { target });
                    LogMessage(GetLang("TEAM_LOG_3", null, player.userID, team.teamID, target, ListToStringWithIndex(team.members)));
                }
            });
            SendChat(player, lang.GetMessage("TEAM_CUPBOARCLEAR", this));
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }
        private void Subscribes()
        {
            foreach (string hook in _hooks)
            {
                if (hook == nameof(OnSamSiteTarget)  && !_config.SamSiteAuthUse)
                {
                    continue;
                }

                if (hook == nameof(OnTurretTarget) && !_config.TurretAuthUse)
                {
                    continue;
                }

                if (hook == nameof(CanUseLockedEntity) && !_config.KeyLockAuthUse)
                {
                    continue;
                }

                if (hook == nameof(OnTeamCreated) && !_config.LoggerUse)
                {
                    continue;
                }

                if (hook == nameof(CanChangeCode) && !_config.CodeLockAuthUse)
                {
                    continue;
                }

                if ((hook == nameof(OnTeamAcceptInvite) || hook == nameof(OnTeamKick) || hook == nameof(OnTeamLeave) ||
                     hook == nameof(OnTeamDisbanded) || hook == nameof(OnEntitySpawned) || hook == nameof(OnEntityKill)) &&
                    ((!_config.CupboardAuthUse && !_config.CodeLockAuthUse && !_config.LoggerUse)))
                {
                    continue;
                }

                Subscribe(hook);
            }
        }
		   		 		  						  	   		  	   		  		 			   		 		  		 	
        private object CanEntityTakeDamage(BaseEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return null;
            BasePlayer target = entity as BasePlayer;
            BasePlayer attacker = info.InitiatorPlayer;
            if (attacker == null || target == null || attacker == target) return null;
            if (!HasFriend(attacker.userID, target.userID)) return null;
            if (IsDuel(attacker.userID)) return null;
            if (_ffTeam[attacker.userID]) return null;
            if (_friendFire.Contains(attacker.userID)) return ObjectCache.Get(false);
            _friendFire.Add(attacker.userID);
            timer.Once(5f, () =>
            {
                if (_friendFire.Contains(attacker.userID))
                    _friendFire.Remove(attacker.userID);
            });
            SendChat(attacker, string.Format(lang.GetMessage("TEAM_FFATTACK", this), target.displayName));
            return ObjectCache.Get(false);
        }
        
        private void AddCodeLockCache(CodeLock codeLock, bool nowSpawned = false)
        {
            if (codeLock == null || !codeLock.OwnerID.IsSteamId()) return;

            List<CodeLock> codeLocks;
            if (!_playerLocksCache.TryGetValue(codeLock.OwnerID, out codeLocks))
            {
                codeLocks = new List<CodeLock>();
                _playerLocksCache.Add(codeLock.OwnerID, codeLocks);
            }

            if (!codeLocks.Contains(codeLock))
                codeLocks.Add(codeLock);

            if (nowSpawned)
                UpdateLockAuth(new List<CodeLock> { codeLock }, codeLock.OwnerID);
        }


        private void CanChangeCode(BasePlayer player, CodeLock codeLock, string code, bool isGuest)
        {
            NextTick(() =>
            {
                ulong ownerId = codeLock.OwnerID;
                if (!ownerId.IsSteamId())
                    return;
                List<PlayerNameID> teamMembers = GetTeamMembers(ownerId);
                ReAuthTeamInCodeLock(teamMembers, codeLock, ownerId);
            });
        }

        private void TriggerDelayedInitialization()
        {
            foreach (BaseNetworkable serverEntity in BaseNetworkable.serverEntities)
            {
                if (_config.CupboardAuthUse)
                {
                    BuildingPrivlidge buildingPrivlidge = serverEntity as BuildingPrivlidge;
                    if (buildingPrivlidge != null)
                    {
                        AddCupboardCache(buildingPrivlidge);
                        continue;
                    }
                }
                if (_config.CodeLockAuthUse)
                {
                    CodeLock codeLock = serverEntity as CodeLock;
                    if (codeLock != null)
                    {
                        AddCodeLockCache(codeLock);
                    }
                }
            }
        }

        
        
        private List<ulong> _friendFire = new List<ulong>();


        private void SendChat(BasePlayer player, string message, string hexColorMsg = "#ffffff", string colortwo = "#fff5070", string customAvatar = "", Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, message, _config.ChatPrefix, customAvatar, hexColorMsg);
            else player.SendConsoleCommand("chat.add", channel, 0, message);
        }
        private void OnTeamAcceptInvite(RelationshipManager.PlayerTeam team, BasePlayer player)
        {
            NextTick(() =>
            {
                if (team == null || player == null) return;
                if (team.members.Contains(player.userID))
                {
                    UpdateTeamAuth(team.members);
                    LogMessage(GetLang("TEAM_LOG_2", null, player.userID, team.teamID, team.teamLeader, ListToStringWithIndex(team.members)));
                }
            });
        }

        
        
        private void Init()
        {
            Unsubscribes();
            LoadData();
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration();
        }

        private void ValidateConfig()
        {
            if (_config.MethodCodeLockAuthUse != "guestPlayers" && _config.MethodCodeLockAuthUse != "whitelistPlayers")
            {
                _config.MethodCodeLockAuthUse = "guestPlayers";
            }
        }

        private void AddCupboardCache(BuildingPrivlidge buildingPrivlidge, bool nowSpawned = false)
        {
            if (buildingPrivlidge == null || !buildingPrivlidge.OwnerID.IsSteamId()) return;

            List<BuildingPrivlidge> buildingPrivlidges;
            if (!_playerCupboardsCache.TryGetValue(buildingPrivlidge.OwnerID, out buildingPrivlidges))
            {
                buildingPrivlidges = new List<BuildingPrivlidge>();
                _playerCupboardsCache.Add(buildingPrivlidge.OwnerID, buildingPrivlidges);
            }

            if (!buildingPrivlidges.Contains(buildingPrivlidge))
                buildingPrivlidges.Add(buildingPrivlidge);

            if (nowSpawned)
                NextTick(() =>
                {
                    UpdateCupboardAuth(new List<BuildingPrivlidge> { buildingPrivlidge }, buildingPrivlidge.OwnerID);
                });
        }
        
        private string ListToStringWithIndex(List<ulong> list)
        {
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < list.Count; i++)
            {
                sb.AppendLine($"\t{i+1}. {list[i]}");
            }
            return sb.ToString();
        }

        
        private BasePlayer FindOnlinePlayer(BasePlayer player, string nameOrID)
        {
            if (nameOrID.IsSteamId())
            {
                BasePlayer target = BasePlayer.FindByID(ulong.Parse(nameOrID));
                if (target != null)
                    return target;

                SendChat(player, GetLang("TEAM_FOUND", player.UserIDString));
                return null;
            }
		   		 		  						  	   		  	   		  		 			   		 		  		 	
            List<BasePlayer> matches = new List<BasePlayer>();
		   		 		  						  	   		  	   		  		 			   		 		  		 	
            foreach (BasePlayer p in BasePlayer.activePlayerList)
            {
                if (p.displayName == nameOrID || 
                    string.Equals(p.displayName, nameOrID, StringComparison.CurrentCultureIgnoreCase) ||
                    p.displayName.Contains(nameOrID) ||
                    p.displayName.ToLower().Contains(nameOrID.ToLower()))
                {
                    matches.Add(p);
                }
            }

            if (matches.Count == 0)
            {
                SendChat(player, GetLang("TEAM_FOUND", player.UserIDString));
                return null;
            }

            if (matches.Count == 1)
                return matches[0];

            string playersMore = "";
            foreach (BasePlayer plr in matches)
                playersMore = playersMore + "\n" + plr.displayName + " - " + plr.UserIDString;

            SendChat(player, GetLang("TEAM_FOUND_MULTIPLE", player.UserIDString, playersMore));
            return null;
        }
        private readonly Dictionary<ulong, List<CodeLock>> _playerLocksCache = new Dictionary<ulong, List<CodeLock>>();
        
        private void OnPlayerConnected(BasePlayer player)
        {
            if (!_ffTeam.ContainsKey(player.userID))
                _ffTeam.Add(player.userID, false);
        }


        
        
        
        private object OnTurretTarget(AutoTurret turret, BasePlayer player)
        {
            if (ReferenceEquals(player, null) || turret.OwnerID <= 0) return null;
            if (HasFriend(turret.OwnerID, player.userID)) return false;
            return null;
        }

        private void OnEntityKill(BuildingPrivlidge buildingPrivlidge) => RemoveCupboardCache(buildingPrivlidge);

        private void OnTeamLeave(RelationshipManager.PlayerTeam team, BasePlayer player)
        {
            ulong teamLead = team?.teamLeader ?? 0;
            NextTick(() =>
            {
                if (team == null || player == null) return;
                if (!team.members.Contains(player.userID))
                {
                    UpdateTeamAuth(new List<ulong>(team.members) { player.userID  });
                }
                string msg = "";
                if (player.userID == teamLead && team.members.Count > 0)
                {
                    msg = GetLang("TEAM_LOG_4", null, team.teamID, team.teamLeader, ListToStringWithIndex(team.members));
                }
                else if(team.members.Count > 0)
                {
                    msg = GetLang("TEAM_LOG_5", null, player.userID, team.teamID, team.teamLeader, ListToStringWithIndex(team.members));
                }
                if(!string.IsNullOrEmpty(msg))
                    LogMessage(msg);
            });
            if(player.userID != team.teamLeader)
                SendChat(player, lang.GetMessage("TEAM_CUPBOARADDLEAVE", this));
        }
        private string GetCurrentTimestamp() => "[" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "] ";
            }
}
