using System;
using System.Collections.Generic;
using ConVar;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("IQPlagueSkill", "qxzxf", "1.0.5")]
    [Description("Скилл система новых веков")]
    class IQPlagueSkill : RustPlugin
    {
        /// <summary>
        /// Обновление 1.0.х
        /// - Добавил мультиязычность в кофнигурации и датафайле
        /// - Добавлена проверка на null в методе RegisteredData()
        /// - Изменена проверка на кланы, добавлена поддержка еще одного плагина на кланы
        /// - Корректировка метода на выдачу ДНК
        /// </summary>

        #region Vars
        private const Boolean LanguageEn = false;

        public static string PermissionsPatogenArmor = "iqplagueskill.patogenarmory";
        public enum TypeSkill
        {
            Active,
            Neutral
        }
        #endregion

        #region Reference
        [PluginReference] Plugin ImageLibrary, IQChat, Friends, Clans, Battles, Duel, IQEconomic, IQHeadReward, XDNotifications, IQCraftSystem, IQKits;
        private void AddNotify(BasePlayer player, string title, string description, string command = "", string cmdyes = "", string cmdno = "")
        {
            if (!XDNotifications) return;
            var Setting = config.ReferenceSettings.XDNotificationsSettings;
            Interface.Oxide.CallHook("AddNotify", player, title, description, Setting.Color, Setting.AlertDelete, Setting.SoundEffect, command, cmdyes, cmdno);
        }

        #region Image Library
        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin);
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        public void SendImage(BasePlayer player, string imageName, ulong imageId = 0) => ImageLibrary?.Call("SendImage", player, imageName, imageId);
        public bool HasImage(string imageName) => (bool)ImageLibrary?.Call("HasImage", imageName);
        void LoadedImage()
        {
            var Interface = config.InterfaceSettings.IconsPNG;

            #region Interface Panel
            if (!HasImage($"BACKGROUND_PLAGUES_{Interface.BackgroundPNG}"))
                AddImage(Interface.BackgroundPNG, $"BACKGROUND_PLAGUES_{Interface.BackgroundPNG}");
            if (!HasImage($"BACKGROUND_PLAGUE_TAKE_PANEL_{Interface.BackgroundTakePanel}"))
                AddImage(Interface.BackgroundTakePanel, $"BACKGROUND_PLAGUE_TAKE_PANEL_{Interface.BackgroundTakePanel}");
            if (!HasImage($"BACKGROUND_PLAGUE_TAKE_BUTTON_{Interface.ButtonTakeSkill}"))
                AddImage(Interface.ButtonTakeSkill, $"BACKGROUND_PLAGUE_TAKE_BUTTON_{Interface.ButtonTakeSkill}");
            if (!HasImage($"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.BlockSkill}"))
                AddImage(Interface.BlockSkill, $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.BlockSkill}");
            if (!HasImage($"BACKGROUND_PLAGUE_SKILL_AVAILABLE_{Interface.AvailableSkill}"))
                AddImage(Interface.AvailableSkill, $"BACKGROUND_PLAGUE_SKILL_AVAILABLE_{Interface.AvailableSkill}");
            if (!HasImage($"BACKGROUND_PLAGUE_SKILL_RECEIVED_{Interface.ReceivedSkill}"))
                AddImage(Interface.ReceivedSkill, $"BACKGROUND_PLAGUE_SKILL_RECEIVED_{Interface.ReceivedSkill}");

            if (!HasImage($"BACKGROUND_PLAGUE_NEUTRAL_SKILL_BLOCK_{Interface.NeutralBlockSkill}"))
                AddImage(Interface.NeutralBlockSkill, $"BACKGROUND_PLAGUE_NEUTRAL_SKILL_BLOCK_{Interface.NeutralBlockSkill}");
            if (!HasImage($"BACKGROUND_PLAGUE_NEUTRAL_SKILL_AVAILABLE_{Interface.NeutralAvailableSkill}"))
                AddImage(Interface.NeutralAvailableSkill, $"BACKGROUND_PLAGUE_NEUTRAL_SKILL_AVAILABLE_{Interface.NeutralAvailableSkill}");
            if (!HasImage($"BACKGROUND_PLAGUE_NEUTRAL_SKILL_RECEIVED_{Interface.NeutralReceivedSkill}"))
                AddImage(Interface.NeutralReceivedSkill, $"BACKGROUND_PLAGUE_NEUTRAL_SKILL_RECEIVED_{Interface.NeutralReceivedSkill}");

            try
            {
                webrequest.Enqueue($"http://rust.skyplugins.ru/getimage/BACKGROUND_PLAGUE/12/IMAGELIBRARY_{Name}_{Author}_14321", null, (i, s) =>
                {
                    if (i != 200) { }
                    if (s.Contains("success")) { AddImage(i.ToString(), $"{i.ToString()}_BACKGROUND_PLAGUE", ulong.Parse($"14321")); }
                    if (s.Contains("fail")) { return; }
                }, this);
            }
            catch (Exception ex) { }
            #endregion

            #region Skills
            var Skill = config.SkillSettings;
            if (String.IsNullOrWhiteSpace(Skill.AnabioticsSettings.GeneralSettings.Sprite))
                if (!HasImage($"SKILL_ANABIOTICS_{Skill.AnabioticsSettings.GeneralSettings.PNG}"))
                    AddImage(Skill.AnabioticsSettings.GeneralSettings.PNG, $"SKILL_ANABIOTICS_{Skill.AnabioticsSettings.GeneralSettings.PNG}");

            if (String.IsNullOrWhiteSpace(Skill.AnimalFriendsSettings.GeneralSettings.Sprite))
                if (!HasImage($"SKILL_ANIMALFRIENDS_{Skill.AnimalFriendsSettings.GeneralSettings.PNG}"))
                    AddImage(Skill.AnimalFriendsSettings.GeneralSettings.PNG, $"SKILL_ANIMALFRIENDS_{Skill.AnimalFriendsSettings.GeneralSettings.PNG}");

            if (String.IsNullOrWhiteSpace(Skill.CrafterSettings.GeneralSettings.Sprite))
                if (!HasImage($"SKILL_CRAFTER_{Skill.CrafterSettings.GeneralSettings.PNG}"))
                    AddImage(Skill.CrafterSettings.GeneralSettings.PNG, $"SKILL_CRAFTER_{Skill.CrafterSettings.GeneralSettings.PNG}");

            if (String.IsNullOrWhiteSpace(Skill.GatherFriendsSettings.GeneralSettings.Sprite))
                if (!HasImage($"SKILL_GATHERFRIENDS_{Skill.GatherFriendsSettings.GeneralSettings.PNG}"))
                    AddImage(Skill.GatherFriendsSettings.GeneralSettings.PNG, $"SKILL_GATHERFRIENDS_{Skill.GatherFriendsSettings.GeneralSettings.PNG}");

            if (String.IsNullOrWhiteSpace(Skill.GenesisGensSettings.GeneralSettings.Sprite))
                if (!HasImage($"SKILL_GENESISGENS_{Skill.GenesisGensSettings.GeneralSettings.PNG}"))
                    AddImage(Skill.GenesisGensSettings.GeneralSettings.PNG, $"SKILL_GENESISGENS_{Skill.GenesisGensSettings.GeneralSettings.PNG}");

            if (String.IsNullOrWhiteSpace(Skill.MetabolismSettings.GeneralSettings.Sprite))
                if (!HasImage($"SKILL_METABOLISM_{Skill.MetabolismSettings.GeneralSettings.PNG}"))
                    AddImage(Skill.MetabolismSettings.GeneralSettings.PNG, $"SKILL_METABOLISM_{Skill.MetabolismSettings.GeneralSettings.PNG}");

            if (String.IsNullOrWhiteSpace(Skill.MilitarySettings.GeneralSettings.Sprite))
                if (!HasImage($"SKILL_MILITARY_{Skill.MilitarySettings.GeneralSettings.PNG}"))
                    AddImage(Skill.MilitarySettings.GeneralSettings.PNG, $"SKILL_MILITARY_{Skill.MilitarySettings.GeneralSettings.PNG}");

            if (String.IsNullOrWhiteSpace(Skill.MinerSettings.GeneralSettings.Sprite))
                if (!HasImage($"SKILL_MINER_{Skill.MinerSettings.GeneralSettings.PNG}"))
                    AddImage(Skill.MinerSettings.GeneralSettings.PNG, $"SKILL_MINER_{Skill.MinerSettings.GeneralSettings.PNG}");

            if (String.IsNullOrWhiteSpace(Skill.PatogenAmrorySettings.GeneralSettings.Sprite))
                if (!HasImage($"SKILL_PATOGENARMORY_{Skill.PatogenAmrorySettings.GeneralSettings.PNG}"))
                    AddImage(Skill.PatogenAmrorySettings.GeneralSettings.PNG, $"SKILL_PATOGENARMORY_{Skill.PatogenAmrorySettings.GeneralSettings.PNG}");

            if (String.IsNullOrWhiteSpace(Skill.PatogenKillSettings.GeneralSettings.Sprite))
                if (!HasImage($"SKILL_PATOGENKILL_{Skill.PatogenKillSettings.GeneralSettings.PNG}"))
                    AddImage(Skill.PatogenKillSettings.GeneralSettings.PNG, $"SKILL_PATOGENKILL_{Skill.PatogenKillSettings.GeneralSettings.PNG}");

            if (String.IsNullOrWhiteSpace(Skill.RegenerationSettings.GeneralSettings.Sprite))
                if (!HasImage($"SKILL_REGENERATION_{Skill.RegenerationSettings.GeneralSettings.PNG}"))
                    AddImage(Skill.RegenerationSettings.GeneralSettings.PNG, $"SKILL_REGENERATION_{Skill.RegenerationSettings.GeneralSettings.PNG}");

            if (String.IsNullOrWhiteSpace(Skill.ThickSkinSettings.GeneralSettings.Sprite))
                if (!HasImage($"SKILL_THICKSSKIN_{Skill.ThickSkinSettings.GeneralSettings.PNG}"))
                    AddImage(Skill.ThickSkinSettings.GeneralSettings.PNG, $"SKILL_THICKSSKIN_{Skill.ThickSkinSettings.GeneralSettings.PNG}");

            if (String.IsNullOrWhiteSpace(Skill.WoundedShakeSettings.GeneralSettings.Sprite))
                if (!HasImage($"SKILL_WOUNDED_{Skill.WoundedShakeSettings.GeneralSettings.PNG}"))
                    AddImage(Skill.WoundedShakeSettings.GeneralSettings.PNG, $"SKILL_WOUNDED_{Skill.WoundedShakeSettings.GeneralSettings.PNG}");

            if (String.IsNullOrWhiteSpace(Skill.NeutralSkills.SkillIQHeadRewards.GeneralSettings.Sprite))
                if (!HasImage($"SKILL_IQHEAD_REWARD_{Skill.NeutralSkills.SkillIQHeadRewards.GeneralSettings.PNG}"))
                    AddImage(Skill.NeutralSkills.SkillIQHeadRewards.GeneralSettings.PNG, $"SKILL_IQHEAD_REWARD_{Skill.NeutralSkills.SkillIQHeadRewards.GeneralSettings.PNG}");

            if (String.IsNullOrWhiteSpace(Skill.NeutralSkills.IQCraftSystemAdvancedCrafts.GeneralSettings.Sprite))
                if (!HasImage($"SKILL_IQCRAFTSYSTEM_{Skill.NeutralSkills.IQCraftSystemAdvancedCrafts.GeneralSettings.PNG}"))
                    AddImage(Skill.NeutralSkills.IQCraftSystemAdvancedCrafts.GeneralSettings.PNG, $"SKILL_IQCRAFTSYSTEM_{Skill.NeutralSkills.IQCraftSystemAdvancedCrafts.GeneralSettings.PNG}");

            #endregion
        }
        void CahedImages(BasePlayer player)
        {
            var Interface = config.InterfaceSettings.IconsPNG;
            var Skill = config.SkillSettings;

            #region Interface Panel
            SendImage(player, $"BACKGROUND_PLAGUES_{Interface.BackgroundPNG}");
            SendImage(player, $"BACKGROUND_PLAGUE_TAKE_PANEL_{Interface.BackgroundTakePanel}");
            SendImage(player, $"BACKGROUND_PLAGUE_TAKE_BUTTON_{Interface.ButtonTakeSkill}");
            SendImage(player, $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.BlockSkill}");
            SendImage(player, $"BACKGROUND_PLAGUE_SKILL_AVAILABLE_{Interface.AvailableSkill}");
            SendImage(player, $"BACKGROUND_PLAGUE_SKILL_RECEIVED_{Interface.ReceivedSkill}");

            SendImage(player, $"BACKGROUND_PLAGUE_NEUTRAL_SKILL_BLOCK_{Interface.NeutralBlockSkill}");
            SendImage(player, $"BACKGROUND_PLAGUE_NEUTRAL_SKILL_AVAILABLE_{Interface.NeutralAvailableSkill}");
            SendImage(player, $"BACKGROUND_PLAGUE_NEUTRAL_SKILL_RECEIVED_{Interface.NeutralReceivedSkill}");
            #endregion

            #region Skills
            SendImage(player, $"SKILL_ANABIOTICS_{Skill.AnabioticsSettings.GeneralSettings.PNG}");
            SendImage(player, $"SKILL_ANIMALFRIENDS_{Skill.AnimalFriendsSettings.GeneralSettings.PNG}");
            SendImage(player, $"SKILL_CRAFTER_{Skill.CrafterSettings.GeneralSettings.PNG}");
            SendImage(player, $"SKILL_GATHERFRIENDS_{Skill.GatherFriendsSettings.GeneralSettings.PNG}");
            SendImage(player, $"SKILL_GENESISGENS_{Skill.GenesisGensSettings.GeneralSettings.PNG}");
            SendImage(player, $"SKILL_METABOLISM_{Skill.MetabolismSettings.GeneralSettings.PNG}");
            SendImage(player, $"SKILL_MILITARY_{Skill.MilitarySettings.GeneralSettings.PNG}");
            SendImage(player, $"SKILL_MINER_{Skill.MinerSettings.GeneralSettings.PNG}");
            SendImage(player, $"SKILL_PATOGENARMORY_{Skill.PatogenAmrorySettings.GeneralSettings.PNG}");
            SendImage(player, $"SKILL_PATOGENKILL_{Skill.PatogenKillSettings.GeneralSettings.PNG}");
            SendImage(player, $"SKILL_REGENERATION_{Skill.RegenerationSettings.GeneralSettings.PNG}");
            SendImage(player, $"SKILL_THICKSSKIN_{Skill.ThickSkinSettings.GeneralSettings.PNG}");
            SendImage(player, $"SKILL_WOUNDED_{Skill.WoundedShakeSettings.GeneralSettings.PNG}");
            SendImage(player, $"SKILL_IQHEAD_REWARD_{Skill.NeutralSkills.SkillIQHeadRewards.GeneralSettings.PNG}");
            SendImage(player, $"SKILL_IQCRAFTSYSTEM_{Skill.NeutralSkills.IQCraftSystemAdvancedCrafts.GeneralSettings.PNG}");
            #endregion
        }

        #endregion

        #region IQChat
        public void SendChat(BasePlayer player, string Message, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            var Chat = config.ReferenceSettings.IQChatSettings;
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, Chat.CustomPrefix, Chat.CustomAvatar);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        #endregion

        #region Block Farm
        public bool IsFriends(ulong userID, ulong targetID)
        {
            if (Friends)
                return (bool)Friends?.Call("HasFriend", userID, targetID);
            else return false;
        }
        private bool IsClans(String userID, String targetID)
        {
            if (Clans)
            {
                String TagUserID = (String)Clans?.Call("GetClanOf", userID);
                String TagTargetID = (String)Clans?.Call("GetClanOf", targetID);
                if (TagUserID == null && TagTargetID == null)
                    return false;
                return (bool)(TagUserID == TagTargetID);
            }
            else
                return false;
        }
        public bool IsDuel(ulong userID)
        {
            if (Battles)
                return (bool)Battles?.Call("IsPlayerOnBattle", userID);
            else if (Duel) return (bool)Duel?.Call("IsPlayerOnActiveDuel", BasePlayer.FindByID(userID));
            else return false;
        }
        #endregion

        #region IQEconomic
        int GetBalanceUser(ulong UserID) => (int)(IQEconomic?.Call("API_GET_BALANCE", UserID));
        void RemoveBalanceUser(ulong UserID, int Balance) => IQEconomic?.Call("API_REMOVE_BALANCE", UserID, Balance); 
        #endregion

        #endregion

        #region Data

        [JsonProperty(LanguageEn ? "user Information and their DNA" : "Информация пользователях и их ДНК")]
        public Dictionary<ulong, int> DataInformation = new Dictionary<ulong, int>();
        [JsonProperty(LanguageEn ? "information about user skills" : "Информация о скиллах пользователей")]
        public Dictionary<ulong, InformationSkills> DataSkills = new Dictionary<ulong, InformationSkills>();

        public class InformationSkills
        {
            [JsonProperty(LanguageEn ? "Miner" : "Шахтер")]
            public bool Miner;
            [JsonProperty(LanguageEn ? "Regeneration" : "Регенерация")]
            public bool Regeneration;
            [JsonProperty(LanguageEn ? "Military" : "Военный")]
            public bool Military;
            [JsonProperty(LanguageEn ? "Skin" : "Кожа")]
            public bool ThickSkin;
            [JsonProperty(LanguageEn ? "Spirit" : "Дух")]
            public bool WoundedShake;
            [JsonProperty(LanguageEn ? "Metabolism" : "Метаболизм")]
            public bool Metabolism;
            [JsonProperty(LanguageEn ? "pathogen Protection" : "Защита от патогена")]
            public bool PatogenAmrory;
            [JsonProperty(LanguageEn ? "the Genesis gene" : "Генезиз ген")]
            public bool GenesisGens;
            [JsonProperty(LanguageEn ? "Unity with animals" : "Единство с животными")]
            public bool AnimalFriends;
            [JsonProperty(LanguageEn ? "Unity with nature" : "Единство с природой")]
            public bool GatherFriends;
            [JsonProperty(LanguageEn ? "Anabiotics" : "Анабиотики")]
            public bool Anabiotics;
            [JsonProperty(LanguageEn ? "Crafter" : "Крафтер")]
            public bool Crafter;
            [JsonProperty(LanguageEn ? "IQHeadReward" : "IQHeadReward")]
            public bool IQHeadReward;
            [JsonProperty(LanguageEn ? "IQCraftSystem: Advanced Crafter" : "IQCraftSystem : Продвинутый крафтер")]
            public bool IQCraftSystemAdvanced;
            [JsonProperty(LanguageEn ? "IQKits: Reduced recharge" : "IQKits : Уменьшенная перезарядка")]
            public bool IQKitsCooldownPercent;
            [JsonProperty(LanguageEn ? "IQKits: Increased drop chance" : "IQKits : Увеличенный шанс выпадения")]
            public bool IQKitsRareup;

            [JsonProperty(LanguageEn ? "is there a pathogen" : "Имеется ли патоген")]
            public bool PatogenAttack;
        }

        void ReadData()
        {
            DataInformation = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, int>>("IQPlagueSkill/InformationUser");
            DataSkills = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, InformationSkills>>("IQPlagueSkill/InformationSkills");
        }
        void WriteData()
        {
            timer.Every(60f, () => {
                Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQPlagueSkill/InformationUser", DataInformation);
                Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQPlagueSkill/InformationSkills", DataSkills);
            });
        }

        void RegisteredDataUser(BasePlayer player)
        {
            if (player == null) return;
            if (!DataInformation.ContainsKey(player.userID))
                DataInformation.Add(player.userID, 0);
            if (!DataSkills.ContainsKey(player.userID))
                DataSkills.Add(player.userID, new InformationSkills
                {
                    Anabiotics = false,
                    AnimalFriends = false,
                    Crafter = false,
                    GatherFriends = false,
                    GenesisGens = false,
                    Metabolism = false,
                    Military = false,
                    Miner = false,
                    PatogenAmrory = false,
                    PatogenAttack = false,
                    Regeneration = false,
                    ThickSkin = false,
                    WoundedShake = false,
                    IQHeadReward = false,
                    IQCraftSystemAdvanced = false,
                    IQKitsCooldownPercent = false,
                    IQKitsRareup = false,
                });
        }

        #endregion

        #region Configuration

        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty(LanguageEn ? "configuring the plugin" : "Настройка плагина")]
            public GeneralSetting GeneralSettings = new GeneralSetting();
            [JsonProperty(LanguageEn ? "configuring all skills" : "Настройка всех скиллов")]
            public Skills SkillSettings = new Skills();
            [JsonProperty(LanguageEn ? "configuring the interface" : "Настройка интерфейса")]
            public InterfaceSetting InterfaceSettings = new InterfaceSetting();
            [JsonProperty(LanguageEn ? "configuring compatibility with other plugins" : "Настройка совместимостей с другими плагинами")]
            public ReferenceSetting ReferenceSettings = new ReferenceSetting();
            [JsonProperty(LanguageEn ? "configuring getting DNA" : "Настройка получения ДНК")]
            public FarmingDNK FarmingDNKS = new FarmingDNK();

            #region Skills
            internal class Skills
            {
                [JsonProperty(LanguageEn ? "Allow viewing a skill without the required number of DNA" : "Разрешить просмотр навыка без нужного кол-во ДНК")]
                public bool ShowSkillNotDNK;
                [JsonProperty(LanguageEn ? "setting the miner skill (Increases the mining rate)" : "Настройка скилла шахтер(Увеличивает рейт добычи)")]
                public Miner MinerSettings = new Miner();
                [JsonProperty(LanguageEn ? "setting the regeneration skill(HP Regeneration after battle)" : "Настройка скилла регенерации(Регенирация ХП после боя)")]
                public Regeneration RegenerationSettings = new Regeneration();
                [JsonProperty(LanguageEn ? "skill setting(Reduces weapon wear)" : "Настройка скилла военный(Уменьшает изнашивания оружия)")]
                public Military MilitarySettings = new Military();
                [JsonProperty(LanguageEn ? "Hard skin skill setting (Protects from cold in winter biomes)" : "Настройка скилла Твердая кожа(Защищает от холода в зимних биомах)")]
                public ThickSkin ThickSkinSettings = new ThickSkin();   
                [JsonProperty(LanguageEn ? "setting the skill Will not shake(Gives a chance to get up after a fall)" : "Настройка скилла Не поколебим(Дает шанс встать после падения)")]
                public WoundedShake WoundedShakeSettings = new WoundedShake(); 
                [JsonProperty(LanguageEn ? "setting up the skill Metabolism (when revived gives N values of satiety, HP, thirst)" : "Настройка скилла Метаболизм(При возраждении дает N значения сытности,хп,жажды)")]
                public Metabolism MetabolismSettings = new Metabolism(); 
                [JsonProperty(LanguageEn ? "configure spell Protection from a pathogen(one-time protection from the fungus Pathogen(If zarazhenie pathogen , a virus eventually destroys the skills of the player))" : "Настройка скилла Защита от патогена(Одноразовая защита от грибка Патоген(При заражаении патогеном , вирус - со временем разрушает скиллы игрока))")]
                public PatogenAmrory PatogenAmrorySettings = new PatogenAmrory();
                [JsonProperty(LanguageEn ? "setting up skill the Killing of a pathogen(one-time killing of the fungus,if the player was infected by a Pathogen)" : "Настройка скилла Убийство патогена(Одноразовое убийство грибка,если игрок заразился Патогеном)")]
                public PatogenKill PatogenKillSettings = new PatogenKill();
                [JsonProperty(LanguageEn ? "setting up skill the Genesis gene(Keeps the % of DNA at the end of a wipe)" : "Настройка скилла Генезиз ген(Сохраняет % ДНК в конце вайпа)")]
                public GenesisGens GenesisGensSettings = new GenesisGens();
                [JsonProperty(LanguageEn ? "setting up the Unity with nature skill (Adds the ability to get more DNA for animal prey)" : "Настройка скилла Единство с природой(Добавляет возможность получать больше ДНК за добычу животных)")]
                public AnimalFriends AnimalFriendsSettings = new AnimalFriends(); 
                [JsonProperty(LanguageEn ? "setting up the Unity with earth skill (Adds the ability to get more DNA for full resource extraction)" : "Настройка скилла Единство с землей(Добавляет возможность получать больше ДНК за полную добычу ресурса)")]
                public GatherFriends GatherFriendsSettings = new GatherFriends();
                [JsonProperty(LanguageEn ? "setting up the Anabiotics skill(Players will get more treatment from drugs)" : "Настройка скилла Анабиотики(Игроки будут получать больше лечения от препаратов)")]
                public Anabiotics AnabioticsSettings = new Anabiotics();
                [JsonProperty(LanguageEn ? "setting up the Crafter skill(crafting Speed increases)" : "Настройка скилла Крафтер(Скорость крафта увеличивается)")]
                public Crafter CrafterSettings = new Crafter();
                [JsonProperty(LanguageEn ? "setting up NEUTRAL skills" : "Настройка НЕЙТРАЛЬНЫХ навыков")]
                public NeutralSkill NeutralSkills = new NeutralSkill();

                #region Classes

                #region NeutralClasses
                internal class NeutralSkill
                {
                    [JsonProperty(LanguageEn ? "Increases crafting level to advanced opening the possibility to craft items with the advanced crafting condition" : "Увеличивает уровень крафта до продвинутого открывая возможность крафтить предметы с условием продвинутого крафта")]
                    public IQCraftSystemAdvancedCraft IQCraftSystemAdvancedCrafts = new IQCraftSystemAdvancedCraft();
                    [JsonProperty(LanguageEn ? "Increases the drop rate of items in the set" : "Увеличивает шанс выпадения предметов в наборе")]
                    public IQKitsRareSkill IQKitsRare = new IQKitsRareSkill();
                    [JsonProperty(LanguageEn ? "Reduces the cooldown of the skill" : "Уменьшает перезарядку навыков")]
                    public IQKitsCooldownPercenct IQKitsCooldown = new IQKitsCooldownPercenct();
                    [JsonProperty(LanguageEn ? "Iqheadreward setting (Description and setting in the plugin Iqheadreward)" : "Настройка IQHeadReward (Описание и настройка в плагине IQHeadReward)")]
                    public IQHeadReward SkillIQHeadRewards = new IQHeadReward();
                    internal class IQHeadReward
                    {
                        [JsonProperty(LanguageEn ? "Enable the ability?(true-enabled/false-disabled)" : "Включить умение?(true - включено/false - выключено)")]
                        public bool SkillTurn;

                        [JsonProperty(LanguageEn ? "General configuration" : "Общая настройка")]
                        public GeneralSettingsSkill GeneralSettings = new GeneralSettingsSkill();
                    }
                    internal class IQCraftSystemAdvancedCraft
                    {
                        [JsonProperty(LanguageEn ? "Enable the ability?(true-enabled/false-disabled)" : "Включить умение?(true - включено/false - выключено)")]
                        public bool SkillTurn;

                        [JsonProperty(LanguageEn ? "General configuration" : "Общая настройка")]
                        public GeneralSettingsSkill GeneralSettings = new GeneralSettingsSkill();
                    }
                    internal class IQKitsRareSkill
                    {
                        [JsonProperty(LanguageEn ? "Enable the ability?(true-enabled/false-disabled)" : "Включить умение?(true - включено/false - выключено)")]
                        public bool SkillTurn;

                        [JsonProperty(LanguageEn ? "General configuration" : "Общая настройка")]
                        public GeneralSettingsSkill GeneralSettings = new GeneralSettingsSkill();
                        [JsonProperty(LanguageEn ? "how much % increase the chance of dropping items?" : "На сколько % увеличивать шанс выпадения предметов?")]
                        public int RareUP;
                    }
                    internal class IQKitsCooldownPercenct
                    {
                        [JsonProperty(LanguageEn ? "Enable the ability?(true-enabled/false-disabled)" : "Включить умение?(true - включено/false - выключено)")]
                        public bool SkillTurn;

                        [JsonProperty(LanguageEn ? "General configuration" : "Общая настройка")]
                        public GeneralSettingsSkill GeneralSettings = new GeneralSettingsSkill();
                        [JsonProperty(LanguageEn ? "how much % should I reduce the set's cooldown?" : "На сколько % уменьшать перезарядку набора?")]
                        public int PercentDrop;
                    }
                }
                #endregion

                internal class Miner
                {
                    [JsonProperty(LanguageEn ? "Enable the ability?(true-enabled/false-disabled)" : "Включить умение?(true - включено/false - выключено)")]
                    public bool SkillTurn;
                    [JsonProperty(LanguageEn ? "General configuration" : "Общая настройка")]
                    public GeneralSettingsSkill GeneralSettings = new GeneralSettingsSkill();

                    [JsonProperty(LanguageEn ? "how much to multiply rates(all at once)" : "На сколько умножать рейты(все сразу)")]
                    public float Rate;
                    [JsonProperty(LanguageEn ? "Use custom multipliers (true-Yes/false-no)" : "Использовать кастомные множители(true - да/false - нет)")]
                    public bool UseLists;
                    [JsonProperty(LanguageEn ? "Use custom multipliers([Shortname] = multiplier)" : "Использовать кастомные множители([Shortname] = множитель)")]
                    public Dictionary<string, float> CustomRate = new Dictionary<string, float>();
                }
                internal class Regeneration
                {
                    [JsonProperty(LanguageEn ? "Enable the ability?(true-enabled/false-disabled)" : "Включить умение?(true - включено/false - выключено)")]
                    public bool SkillTurn;
                    [JsonProperty(LanguageEn ? "General configuration" : "Общая настройка")]
                    public GeneralSettingsSkill GeneralSettings = new GeneralSettingsSkill();

                    [JsonProperty(LanguageEn ? "how Many lives to regenerate in a time interval" : "Сколько жизней регенерировать в промежуток времени")]
                    public int HealtRegeneration;
                    [JsonProperty(LanguageEn ? "how many seconds to regenerate a player" : "Раз в сколько секунд регенерировать игрока")]
                    public int RegenerationTimer;
                }
                internal class Military
                {
                    [JsonProperty(LanguageEn ? "Enable the ability?(true-enabled/false-disabled)" : "Включить умение?(true - включено/false - выключено)")]
                    public bool SkillTurn;
                    [JsonProperty(LanguageEn ? "General configuration" : "Общая настройка")]
                    public GeneralSettingsSkill GeneralSettings = new GeneralSettingsSkill();

                    [JsonProperty(LanguageEn ? "how many percent to reduce the wear of weapons(0-100%)" : "На сколько процентов снижать изнашивания оружий(0-100%)")]
                    public int PercentNoBroken;
                }
                internal class ThickSkin
                {
                    [JsonProperty(LanguageEn ? "Enable the ability?(true-enabled/false-disabled)" : "Включить умение?(true - включено/false - выключено)")]
                    public bool SkillTurn;  
                    [JsonProperty(LanguageEn ? "General configuration" : "Общая настройка")]
                    public GeneralSettingsSkill GeneralSettings = new GeneralSettingsSkill();
                }
                internal class WoundedShake
                {
                    [JsonProperty(LanguageEn ? "Enable the ability?(true-enabled/false-disabled)" : "Включить умение?(true - включено/false - выключено)")]
                    public bool SkillTurn;
                    [JsonProperty(LanguageEn ? "General configuration" : "Общая настройка")]
                    public GeneralSettingsSkill GeneralSettings = new GeneralSettingsSkill();

                    [JsonProperty(LanguageEn ? "Chance to get up when the player is put down" : "Шанс встать в момент когда игрока положат")]
                    public int Rare;
                    [JsonProperty(LanguageEn ? "how many seconds to raise the player when falling, if the chance is successful" : "Через сколько секунд поднимать игрока при падаение,если шанс успешен")]
                    public int RareStartTime;
                    [JsonProperty(LanguageEn ? "after successful triggering - take the skill(true-Yes/false-no)" : "После успешного срабатывания - забирать скилл(true - да/false - нет)")]
                    public bool DropSkill;
                }
                internal class Metabolism
                {
                    [JsonProperty(LanguageEn ? "Enable the ability?(true-enabled/false-disabled" : "Включить умение?(true - включено/false - выключено)")]
                    public bool SkillTurn;
                    [JsonProperty(LanguageEn ? "General configuration" : "Общая настройка")]
                    public GeneralSettingsSkill GeneralSettings = new GeneralSettingsSkill();

                    [JsonProperty(LanguageEn ? "How many HP will be when reviving" : "Сколько ХП будет при возрождениие")]
                    public int Health;
                    [JsonProperty(LanguageEn ? "how much Satiety will there be when reviving" : "Сколько Сытности будет при возрождении")]
                    public int Calories;
                    [JsonProperty(LanguageEn ? "how much Thirst will there be when reviving" : "Сколько Жажды будет при возрождении")]
                    public int Hydration;
                    [JsonProperty(LanguageEn ? "Chance to Wake up with these indicators" : "Шанс проснуться с данными показателями")]
                    public int RareMetabolisme;
                    [JsonProperty(LanguageEn ? "after successful triggering - take the skill(true-Yes/false-no)" : "После успешного срабатывания - забирать скилл(true - да/false - нет)")]
                    public bool DropSkill;
                }
                internal class PatogenAmrory
                {
                    [JsonProperty(LanguageEn ? "Enable the ability?(true-enabled/false-disabled)" : "Включить умение?(true - включено/false - выключено)")]
                    public bool SkillTurn;
                    [JsonProperty(LanguageEn ? "General setting" : "Общая настройка")]
                    public GeneralSettingsSkill GeneralSettings = new GeneralSettingsSkill();
                }
                internal class PatogenKill
                {
                    [JsonProperty(LanguageEn ? "Enable the ability?(true-enabled/false-disabled)" : "Включить умение?(true - включено/false - выключено)")]
                    public bool SkillTurn;
                    [JsonProperty(LanguageEn ? "General setting" : "Общая настройка")]
                    public GeneralSettingsSkill GeneralSettings = new GeneralSettingsSkill();
                }
                internal class GenesisGens
                {
                    [JsonProperty(LanguageEn ? "Enable the ability?(true-enabled/false-disabled)" : "Включить умение?(true - включено/false - выключено)")]
                    public bool SkillTurn;
                    [JsonProperty(LanguageEn ? "General setting" : "Общая настройка")]
                    public GeneralSettingsSkill GeneralSettings = new GeneralSettingsSkill();

                    [JsonProperty(LanguageEn ? "How much % of DNA to leave at the end of the wipe(will be counted from the amount spent on skills)0 - 100" : "Сколько % ДНК оставлять в конце вайпа(будет отсчитываться от затраченного количества на скиллы)0 - 100")]
                    public int PercentSave;
                }
                internal class AnimalFriends
                {
                    [JsonProperty(LanguageEn ? "Enable the ability?(true-enabled/false-disabled)" : "Включить умение?(true - включено/false - выключено)")]
                    public bool SkillTurn;
                    [JsonProperty(LanguageEn ? "General setting" : "Общая настройка")]
                    public GeneralSettingsSkill GeneralSettings = new GeneralSettingsSkill();

                    [JsonProperty(LanguageEn ? "Min : How much to give out" : "Min : Сколько выдавать ДНК")]
                    public int MinDNKAnimal;
                    [JsonProperty(LanguageEn ? "Max: How much to give out" : "Max : Сколько выдавать ДНК")]
                    public int MaxDNKAnimal;
                    [JsonProperty(LanguageEn ? "Use custom sheet(true-Yes/false-no)" : "Использовать кастомный лист(true - да/false - нет)")]
                    public bool UseLists;
                    [JsonProperty(LanguageEn ? "Custom list, how much to increase the amount of DNA for killing an animal [Animal] = setting" : "Кастомный лист ,на сколько увеличивать количество ДНК за убийство животного [Animal] = Настройка")]
                    public Dictionary<string, CustomSettings> AnimalsList = new Dictionary<string, CustomSettings>();
                    [JsonProperty(LanguageEn ? "Chance of getting additional DNA" : "Шанс получения дополнительного ДНК")]
                    public int RareAll;
                    [JsonProperty(LanguageEn ? "Animals from which DNA will fall" : "Животные с которых будет падать ДНК")]
                    public List<string> AnimalDetected = new List<string>();
                    internal class CustomSettings
                    {
                        [JsonProperty(LanguageEn ? "Chance of getting additional DNA" : "Шанс получения дополнительного ДНК")]
                        public int Rare;
                        [JsonProperty(LanguageEn ? "Min : How much to give out?" : "Min : Сколько выдавать ДНК")]
                        public int MinDNKCustom;
                        [JsonProperty(LanguageEn ? "Max: How much to give out?" : "Max : Сколько выдавать ДНК")]
                        public int MaxDNKCustom;
                    }
                }
                internal class GatherFriends
                {
                    [JsonProperty(LanguageEn ? "Enable the ability?(true-enabled/false-disabled)" : "Включить умение?(true - включено/false - выключено)")]
                    public bool SkillTurn;
                    [JsonProperty(LanguageEn ? "General setting" : "Общая настройка")]
                    public GeneralSettingsSkill GeneralSettings = new GeneralSettingsSkill();

                    [JsonProperty(LanguageEn ? "Custom list, how much to increase the amount of DNA for full resource extraction [Shortname] = setting" : "Кастомный лист ,на сколько увеличивать количество ДНК за полную добычу ресурса [Shortname] = Настройка")]
                    public Dictionary<string, CustomSettings> GatherList = new Dictionary<string, CustomSettings>();

                    internal class CustomSettings
                    {
                        [JsonProperty(LanguageEn ? "Chance of getting additional DNA" : "Шанс получения дополнительного ДНК")]
                        public int Rare;
                        [JsonProperty(LanguageEn ? "Min : How much to give out?" : "Min : Сколько выдавать ДНК")]
                        public int MinDNKCustom;
                        [JsonProperty(LanguageEn ? "Max: How much to give out?" : "Max : Сколько выдавать ДНК")]
                        public int MaxDNKCustom;
                    }
                }
                internal class Anabiotics
                {
                    [JsonProperty(LanguageEn ? "Enable the ability?(true-enabled/false-disabled)" : "Включить умение?(true - включено/false - выключено)")]
                    public bool SkillTurn;
                    [JsonProperty(LanguageEn ? "General setting" : "Общая настройка")]
                    public GeneralSettingsSkill GeneralSettings = new GeneralSettingsSkill();

                    [JsonProperty(LanguageEn ? "how much to increase the number of HP for using medicine[Shortname] = Amount" : "На сколько увеличивать количество ХП за использование медицины[Shortname] = Amount")]
                    public Dictionary<string, int> AnabioticsList = new Dictionary<string, int>();
                }
                internal class Crafter
                {
                    [JsonProperty(LanguageEn ? "Enable the ability?(true-enabled/false-disabled)" : "Включить умение?(true - включено/false - выключено)")]
                    public bool SkillTurn;
                    [JsonProperty(LanguageEn ? "General setting" : "Общая настройка")]
                    public GeneralSettingsSkill GeneralSettings = new GeneralSettingsSkill();

                    [JsonProperty(LanguageEn ? "how many times to increase the crafting speed? (Example: initially 60 seconds, 3x magnification = 60/3 = 20)" : "В сколько раз увеличивать скорость крафта? (Пример : изначально 60 секунд, увеличение в 3 раза = 60/3 = 20)")]
                    public int CraftBoost;
                }
                internal class GeneralSettingsSkill
                {
                    [JsonProperty(LanguageEn ? "Display name" : "Отображаемое имя")]
                    public string DisplayName;
                    [JsonProperty(LanguageEn ? "skill Description" : "Описание скилла")]
                    public string Description;
                    [JsonProperty(LanguageEn ? "sprite for icon" : "Sprite для иконки")]
                    public string Sprite;
                    [JsonProperty(LanguageEn ? "PNG link for the icon(64x64).If you use this,the sprite will be ignored" : "PNG-ссылка для иконки(64х64).Если используете это,спрайт будет игнорироваться")]
                    public string PNG;
                    [JsonProperty(LanguageEn ? "price per study" : "Цена за изучение")]
                    public int PriceDNK;
                }
                
                #endregion
            }
            #endregion

            #region Interface
            internal class InterfaceSetting
            {
                [JsonProperty(LanguageEn ? "configuring design elements" : "Настройка элементов дизайна")]
                public Icons IconsPNG = new Icons();
                [JsonProperty(LanguageEn ? "configuring basic elements" : "Настройка основных элементов")]
                public General GeneralSettings = new General();
                internal class Icons
                {
                    [JsonProperty(LanguageEn ? "PNG background link" : "Ссылка PNG на задний фон")]
                    public string BackgroundPNG;   
                    [JsonProperty(LanguageEn ? "Link link to the blocked skill icon PNG" : "Ссылка ссылка на иконку заблокированного навыка PNG")]
                    public string BlockSkill; 
                    [JsonProperty(LanguageEn ? "Link link to available skill icon PNG" : "Ссылка ссылка на иконку доступного навыка PNG")]
                    public string AvailableSkill;
                    [JsonProperty(LanguageEn ? "Link link to the learned skill icon PNG" : "Ссылка ссылка на иконку изученного навыка PNG")]
                    public string ReceivedSkill;
                    [JsonProperty(LanguageEn ? "Link to the button to get this skill(develop)" : "Ссылка на кнопку для получения данного скилла(развить)")]
                    public string ButtonTakeSkill;  
                    [JsonProperty(LanguageEn ? "Link panel for learning the skill" : "Ссылка панель для изучения скилла")]
                    public string BackgroundTakePanel;

                    [JsonProperty(LanguageEn ? "Link link to the NEUTRAL blocked skill icon PNG" : "Ссылка ссылка на иконку НЕЙТРАЛЬНОГО заблокированного навыка PNG")]
                    public string NeutralBlockSkill;
                    [JsonProperty(LanguageEn ? "Link link to the NEUTRAL available skill icon PNG" : "Ссылка ссылка на иконку НЕЙТРАЛЬНОГО доступного навыка PNG")]
                    public string NeutralAvailableSkill;
                    [JsonProperty(LanguageEn ? "Link link to the NEUTRAL learned skill icon PNG" : "Ссылка ссылка на иконку НЕЙТРАЛЬНОГО изученного навыка PNG")]
                    public string NeutralReceivedSkill;
                }
                internal class General
                {
                    [JsonProperty(LanguageEn ? "text Color" : "Цвет текста")]
                    public string HexLabels;
                    [JsonProperty(LanguageEn ? "skill text color" : "Цвет текста навыков")]
                    public string HexLabelsSkill;
                    [JsonProperty(LanguageEn ? "skill description text Color" : "Цвет текста описания навыков")]
                    public string HexLabelTakePanel;
                }
            }
            #endregion

            #region Generals
            internal class GeneralSetting
            {
                [JsonProperty(LanguageEn ? "settings for automatic date clearing after VAPE" : "Настройки автоматической очисти даты после вайпа")]
                public WipeContoller WipeContollers = new WipeContoller();
                [JsonProperty(LanguageEn ? "managing the PATHOGEN virus" : "Управление вирусом ПАТОГЕН")]
                public VirusPatogen VirusPatogens = new VirusPatogen();
                internal class WipeContoller
                {
                    [JsonProperty(LanguageEn ? "Enable automatic date clearing after server wipe" : "Включить автоматическую очистку даты после вайпа сервера")]
                    public bool WipeDataUse;
                    [JsonProperty(LanguageEn ? "Cleanse skills of players after a wipe" : "Очищать скиллы игроков после вайпа")]
                    public bool WipeDataSkill;
                }
                internal class VirusPatogen
                {
                    [JsonProperty(LanguageEn ? "Enable pathogen virus(this virus will remove 1 random skill from the player after N amount of time,they will also have skills for healing and protection from infection)" : "Включить вирус-патоген(Данный вирус будет удалять у игрока 1 случайный навык через N количество времени,у них так же будут навыки на излечение и защиты от заражаения)")]
                    public bool UsePatogen;
                    [JsonProperty(LanguageEn ? "Chance of virus infection" : "Шанс заражения вирусом")]
                    public int RareInfected;
                    [JsonProperty(LanguageEn ? "time to start player infection(seconds)" : "Раз в сколько времени начинать инфекцию игроков(Секунды)")]
                    public int TimerInfectedVirus;
                    [JsonProperty(LanguageEn ? "How long to delete 1 random skill to the player(seconds)" : "Через сколько удалять 1 случайный навык игроку(секунды)")]
                    public int TimerRemoveSkill;
                }
            }
            #endregion

            #region Reference
            internal class ReferenceSetting
            {
                [JsonProperty(LanguageEn ? "Setting Up IQChat" : "Настройка IQChat")]
                public IQChat IQChatSettings = new IQChat(); 
                [JsonProperty(LanguageEn ? "Enable IQEconomic support (DNA will be replaced with the economy currency)" : "Включить поддержку IQEconomic(ДНК заменится на валюту экономики)")]
                public bool IQEconomicUse;
                [JsonProperty(LanguageEn ? "Enable support for IQHeadReward" : "Включить поддержку IQHeadReward")]
                public bool IQHeadRewardUse;
                [JsonProperty(LanguageEn ? "Enable support for IQCraftSystem" : "Включить поддержку IQCraftSystem")]
                public bool IQCraftSystem;
                [JsonProperty(LanguageEn ? "Enable support for IQKits" : "Включить поддержку IQKits")]
                public bool IQKits;
                [JsonProperty(LanguageEn ? "configuring XDNotifications" : "Настройка XDNotifications")]
                public XDNotifications XDNotificationsSettings = new XDNotifications();
                internal class XDNotifications
                {
                    [JsonProperty(LanguageEn ? "Enable XDNotifications support(Some notifications will come in XDNotifications)" : "Включить поддержку XDNotifications(Некоторые уведомления будут приходить в XDNotifications)")]
                    public bool UseXDNotifications;
                    [JsonProperty(LanguageEn ? "notification background Color (HEX)" : "Цвет заднего фона уведомления(HEX)")]
                    public string Color;
                    [JsonProperty(LanguageEn ? "How long before the notification is deleted" : "Через сколько удалиться уведомление")]
                    public int AlertDelete;
                    [JsonProperty(LanguageEn ? "Sound effect" : "Звуковой эффект")]
                    public string SoundEffect;
                    [JsonProperty(LanguageEn ? "Table Of Contents" : "Оглавление")]
                    public string Title;
                }
                internal class IQChat
                {
                    [JsonProperty(LanguageEn ? "IQChat: Custom prefix in chat" : "IQChat : Кастомный префикс в чате")]
                    public string CustomPrefix;
                    [JsonProperty(LanguageEn ? "IQChat : Custom avatar in the chat(If you want)" : "IQChat : Кастомный аватар в чате(Если требуется)")]
                    public string CustomAvatar;
                }
            }
            #endregion

            #region DNKFarming

            internal class FarmingDNK
            {
                [JsonProperty(LanguageEn ? "setting up getting DNA for killing players" : "Настройка получения ДНК за убийство игроков")]
                public PlayerKill PlayerKills = new PlayerKill();
                [JsonProperty(LanguageEn ? "setting up getting DNA for killing NPCs" : "Настройка получения ДНК за убийство NPC")]
                public NPCKill NPCKills = new NPCKill();
                [JsonProperty(LanguageEn ? "setting up getting DNA for killing animals" : "Настройка получения ДНК за убийство животных")]
                public AnimalKill AnimalKills = new AnimalKill();
                internal class PlayerKill
                {
                    [JsonProperty(LanguageEn ? "getting DNA for killing players" : "Получение ДНК за убийство игроков")]
                    public bool DNKKillUser;
                    [JsonProperty(LanguageEn ? "Parameters" : "Параметры")]
                    public GeneralSettingsFarming GeneralSettingsFarmings = new GeneralSettingsFarming();
                }
                internal class NPCKill
                {
                    [JsonProperty(LanguageEn ? "Getting currency for killing an NPC" : "Получение валюты за убийство NPC")]
                    public bool DNKKillNPC;
                    [JsonProperty(LanguageEn ? "Parameters" : "Параметры")]
                    public GeneralSettingsFarming GeneralSettingsFarmings = new GeneralSettingsFarming();
                }
                internal class AnimalKill
                {
                    [JsonProperty(LanguageEn ? "Receiving currency for the killing of animals" : "Получение валюты за убийство животных")]
                    public bool DNKKillAnimal;
                    [JsonProperty(LanguageEn ? "Parameters" : "Параметры")]
                    public GeneralSettingsFarming GeneralSettingsFarmings = new GeneralSettingsFarming();
                }
                internal class GeneralSettingsFarming
                {
                    [JsonProperty(LanguageEn ? "chance of getting DNA" : "Шанс получения ДНК")]
                    public int RareGiveDNK;
                    [JsonProperty(LanguageEn ? "Minimum amount of DNA" : "Минимальное количество ДНК")]
                    public int MinimumDNK;
                    [JsonProperty(LanguageEn ? "Maximum amount of DNA" : "Максимальное количество ДНК")]
                    public int MaximumDNK;
                }
            }

            #endregion

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    #region Skills
                    SkillSettings = new Skills
                    {
                        ShowSkillNotDNK = true,
                        NeutralSkills = new Skills.NeutralSkill
                        {
                            IQCraftSystemAdvancedCrafts = new Skills.NeutralSkill.IQCraftSystemAdvancedCraft
                            {
                                SkillTurn = true,
                                GeneralSettings = new Skills.GeneralSettingsSkill
                                {
                                    DisplayName = LanguageEn ? "Jack of all trades" : "Мастер на все руки",
                                    Description = LanguageEn ? "You have more opportunities for crafting" : "У вас открывается больше возможностей для крафта",
                                    PNG = "",
                                    PriceDNK = 50,
                                    Sprite = "assets/icons/lightbulb.png",
                                },
                            },
                            SkillIQHeadRewards = new Skills.NeutralSkill.IQHeadReward
                            {
                                SkillTurn = true,
                                GeneralSettings = new Skills.GeneralSettingsSkill
                                {
                                    DisplayName = LanguageEn ? "I can't be found" : "Меня не найти",
                                    Description = LanguageEn ? "you get immunity to search, if you are put on the wanted list, your skill will protect you and reset" :  "Вы получаете иммунитет к розыску,если на вас подадут в розыск, ваш навык защитит вас и сбросится",
                                    PNG = "",
                                    PriceDNK = 50,
                                    Sprite = "assets/content/ui/hypnotized.png",
                                },
                            },
                            IQKitsRare = new Skills.NeutralSkill.IQKitsRareSkill
                            {
                                SkillTurn = true,
                                GeneralSettings = new Skills.GeneralSettingsSkill
                                {
                                    DisplayName = LanguageEn ? "luckiest" : "Самый везучий",
                                    Description = LanguageEn ? " Increases the chance of dropping items from some sets! Command : /kit" :  "Увеличивает шанс выпадения предметов из некоторых наборов! Команда : /kit",
                                    PNG = "",
                                    PriceDNK = 50,
                                    Sprite = "assets/icons/resource.png",
                                },
                                RareUP = 30,
                            },
                            IQKitsCooldown = new Skills.NeutralSkill.IQKitsCooldownPercenct
                            {
                                SkillTurn = true,
                                GeneralSettings = new Skills.GeneralSettingsSkill
                                {
                                    DisplayName = LanguageEn ? "Accelerated recharge" : "Ускоренная перезарядка",
                                    Description = LanguageEn ? " Reduces the recharge time of your sets! Command : /kit" : "Уменьшает время перезарядки ваших наборово! Команда : /kit",
                                    PNG = "",
                                    PriceDNK = 50,
                                    Sprite = "assets/icons/loading.png",
                                },
                                PercentDrop = 30,
                            }
                        },
                        AnabioticsSettings = new Skills.Anabiotics
                        {
                            SkillTurn = true,
                            GeneralSettings = new Skills.GeneralSettingsSkill
                            {
                                DisplayName = LanguageEn ? "perception of anabiotics" : "Восприятие анабиотиков",
                                Description = LanguageEn ? "In the time of taking the medicine you receive more treatment than usual" :  "Во время принятия медицины вы получаете еще больше лечения,чем обычно",
                                PNG = "",
                                PriceDNK = 10,
                                Sprite = "assets/icons/pills.png",
                            },
                            AnabioticsList = new Dictionary<string, int>
                            {
                                ["syringe.medical"] = 10,
                                ["largemedkit"] = 30,
                            },
                        },
                        MinerSettings = new Skills.Miner
                        {
                            SkillTurn = true,
                            GeneralSettings = new Skills.GeneralSettingsSkill
                            {
                                DisplayName = LanguageEn ? "miner Skill" :  "Мастерство шахтера",
                                Description = LanguageEn ? " This skill allows you to extract even more resources than usual" : "Данный навык позволяет добывать еще больше ресурсов чем обычно",
                                PNG = "",
                                PriceDNK = 15,
                                Sprite = "assets/icons/level_wood.png",
                            },
                            UseLists = false,
                            Rate = 3,
                            CustomRate = new Dictionary<string, float>
                            {
                                ["wood"] = 15,
                                ["stones"] = 10,
                            },
                        },
                        GatherFriendsSettings = new Skills.GatherFriends
                        {
                            SkillTurn = true,
                            GeneralSettings = new Skills.GeneralSettingsSkill
                            {
                                DisplayName = LanguageEn ? "Unity with the earth" : "Единство с землей",
                                Description = LanguageEn ? "You are one with the earth and have studied its genetic level, when the resource is fully extracted, you will receive additional DNA points" : "Вы едины с землей и изучили ее генетический уровень, при полной добычи ресурса вы получите дополнительные очки ДНК",
                                PNG = "",
                                PriceDNK = 50,
                                Sprite = "assets/icons/study.png",
                            },
                            GatherList = new Dictionary<string, Skills.GatherFriends.CustomSettings>
                            {
                                ["wood"] = new Skills.GatherFriends.CustomSettings
                                {
                                    Rare = 100,
                                    MinDNKCustom = 10,
                                    MaxDNKCustom = 30
                                }
                            }
                        },
                        AnimalFriendsSettings = new Skills.AnimalFriends
                        {
                            SkillTurn = true,
                            GeneralSettings = new Skills.GeneralSettingsSkill
                            {
                                DisplayName = LanguageEn ? "Unity with nature" : "Единство с природой",
                                Description = LanguageEn ? "You are one with nature and have studied its genetic structure of animals, when you kill animals, you will receive additional DNA points" :  "Вы едины с природой и изучили ее генетическое строение животных, при убийстве животных вы будете получать дополнительные очки ДНК",
                                PNG = "",
                                PriceDNK = 50,
                                Sprite = "assets/icons/bite.png",
                            },
                            MinDNKAnimal = 1,
                            MaxDNKAnimal = 5,
                            RareAll = 30,
                            UseLists = true,
                            AnimalsList = new Dictionary<string, Skills.AnimalFriends.CustomSettings>
                            {
                                ["assets/rust.ai/agents/horse/horse.corpse.prefab"] = new Skills.AnimalFriends.CustomSettings
                                {
                                    MinDNKCustom = 10,
                                    MaxDNKCustom = 20,
                                    Rare = 80,
                                }
                            },
                            AnimalDetected = new List<string>
                            {
                                "assets/rust.ai/agents/horse/horse.corpse.prefab",
                                "assets/rust.ai/agents/boar/boar.corpse.prefab",
                                "assets/rust.ai/agents/chicken/chicken.corpse.prefab",
                                "assets/rust.ai/agents/stag/stag.corpse.prefab",
                                "assets/rust.ai/agents/wolf/wolf.corpse.prefab",
                                "assets/rust.ai/agents/bear/bear.corpse.prefab",
                            }
                        },
                        CrafterSettings = new Skills.Crafter
                        {
                            SkillTurn = true,
                            GeneralSettings = new Skills.GeneralSettingsSkill
                            {
                                DisplayName = LanguageEn ? "craft Skill":  "Мастерство рукоделия",
                                Description = LanguageEn ? " you get a great skill in creating items, you found great genetic bundles for accelerated crafting" : "Вы получаете большой навык создания предметов, вы нашли отличные генетические связки для ускоренного крафта",
                                PNG = "",
                                PriceDNK = 30,
                                Sprite = "assets/icons/tools.png",
                            },
                            CraftBoost = 3,
                        },
                        MetabolismSettings = new Skills.Metabolism
                        {
                            SkillTurn = true,
                            GeneralSettings = new Skills.GeneralSettingsSkill
                            {
                                DisplayName = LanguageEn ? "Cheerful spirit" : "Бодрый дух",
                                Description = LanguageEn ? " you improve your genetic level and get additional parameters when reviving" : "Вы улучшаете свой генетический уровень и получаете дополнительные параметры при возрождении",
                                PNG = "",
                                PriceDNK = 20,
                                Sprite = "assets/icons/upgrade.png",
                            },
                            RareMetabolisme = 50,
                            Calories = 100,
                            Health = 100,
                            Hydration = 100,
                            DropSkill = false,
                        },
                        MilitarySettings = new Skills.Military
                        {
                            SkillTurn = true,
                            GeneralSettings = new Skills.GeneralSettingsSkill
                            {
                                DisplayName = LanguageEn ? "weapon Proficiency" :  "Мастерство владения оружием",
                                Description = LanguageEn ? " you understand the composition of the alloy that your items are made of, their wear is reduced" : "Вы понимаете состав сплава из которого сделаны ваши предметы, их изнашивание уменьшается",
                                PNG = "",
                                PriceDNK = 25,
                                Sprite = "assets/icons/stopwatch.png",
                            },
                            PercentNoBroken = 20,
                        },
                        RegenerationSettings = new Skills.Regeneration
                        {
                            SkillTurn = true,
                            GeneralSettings = new Skills.GeneralSettingsSkill
                            {
                                DisplayName = LanguageEn ? "Regeneration" : "Регенерация",
                                Description = LanguageEn ? " you improve your genetic level and your wounds will heal much faster" : "Вы улучшааете свой генетический уровень и ваши раны будут гораздо быстрее заживать",
                                PNG = "",
                                PriceDNK = 40,
                                Sprite = "assets/icons/bleeding.png",
                            },
                            HealtRegeneration = 5,
                            RegenerationTimer = 300,
                        },
                        ThickSkinSettings = new Skills.ThickSkin
                        {
                            SkillTurn = true,
                            GeneralSettings = new Skills.GeneralSettingsSkill
                            {
                                DisplayName = LanguageEn ? "Hard skin" : "Твердая кожа",
                                Description = LanguageEn ? " you have removed your gene responsible for the need for heat, you will no longer feel cold" :  "Вы удалили свой ген отвечающий за потребность в тепле, больше вы не будете чувствовать холода",
                                PNG = "",
                                PriceDNK = 35,
                                Sprite = "assets/icons/freezing.png",
                            },
                        },
                        WoundedShakeSettings = new Skills.WoundedShake
                        {
                            SkillTurn = true,
                            GeneralSettings = new Skills.GeneralSettingsSkill
                            {
                                DisplayName = LanguageEn ? "willpower" : "Сила воли",
                                Description = LanguageEn ? " After a fall, you willpower appears and you overcome the pain and rush into battle again!" : "После падения у вас появляетсяя сила воли и вы перебариваете боль и снова рветесь в бой!",
                                PNG = "",
                                PriceDNK = 35,
                                Sprite = "assets/icons/fall.png",
                            },
                            Rare = 30,
                            RareStartTime = 10,
                            DropSkill = true,
                        },
                        PatogenAmrorySettings = new Skills.PatogenAmrory
                        {
                            SkillTurn = true,
                            GeneralSettings = new Skills.GeneralSettingsSkill
                            {
                                DisplayName = LanguageEn ? "pathogen Protection" : "Защита от патогена",
                                Description = LanguageEn ? "Adds one-time protection against a virus Pathogen that destroys already studied genes" : "Добавляет единоразовую защиту от вируса Патоген, который разрушает уже изученные гены",
                                PNG = "",
                                PriceDNK = 100,
                                Sprite = "assets/prefabs/misc/chippy arcade/chippyart/bossform0.png",
                            },
                        },
                        PatogenKillSettings = new Skills.PatogenKill
                        {
                            SkillTurn = true,
                            GeneralSettings = new Skills.GeneralSettingsSkill
                            {
                                DisplayName = LanguageEn ? "killing the pathogen" : "Убийство патогена",
                                Description = LanguageEn ? "If you are infected with a Pathogen virus, this skill will completely cure you" : "Если вы заразились вирусом Патоген, данный навык полностью излечит вас",
                                PNG = "",
                                PriceDNK = 35,
                                Sprite = "assets/icons/demolish.png",
                            },
                        },
                        GenesisGensSettings = new Skills.GenesisGens
                        {
                            SkillTurn = true,
                            GeneralSettings = new Skills.GeneralSettingsSkill
                            {
                                DisplayName = LanguageEn ? "saving DNA" : "Сохранение ДНК",
                                Description = LanguageEn ? " after VAPE chat your DNA spent on learning skills will be saved" : "После вайпа чать ваших ДНК потраченных на изучение навыков сохранится",
                                PNG = "",
                                PriceDNK = 150,
                                Sprite = "assets/icons/player_carry.png",
                            },
                            PercentSave = 30,
                        }
                    },
                    #endregion

                    InterfaceSettings = new InterfaceSetting
                    {
                        IconsPNG = new InterfaceSetting.Icons
                        {
                            BackgroundPNG = "https://i.imgur.com/x7ANdnu.png",
                            BackgroundTakePanel = "https://i.imgur.com/4mGgV3P.png",
                            ButtonTakeSkill = "https://i.imgur.com/Aq6ycYr.png",
                            AvailableSkill = "https://i.imgur.com/PmPegqF.png",
                            BlockSkill = "https://i.imgur.com/tuxntXF.png",
                            ReceivedSkill = "https://i.imgur.com/x1EiNLa.png",
                            NeutralBlockSkill = "https://i.imgur.com/g7S7Wrq.png",
                            NeutralAvailableSkill = "https://i.imgur.com/17RpIG0.png",
                            NeutralReceivedSkill = "https://i.imgur.com/XKwIndo.png"
                        },
                        GeneralSettings = new InterfaceSetting.General
                        {
                            HexLabels = "#DAD1C7FF",
                            HexLabelsSkill = "#FFFFFFFF",
                            HexLabelTakePanel = "#FFFFFFFF"
                        }
                    },

                    #region General Settings
                    GeneralSettings = new GeneralSetting
                    {
                        WipeContollers = new GeneralSetting.WipeContoller
                        {
                            WipeDataUse = true,
                            WipeDataSkill = true,
                        },
                        VirusPatogens = new GeneralSetting.VirusPatogen
                        {
                            UsePatogen = true,
                            TimerInfectedVirus = 120,
                            RareInfected = 30,
                            TimerRemoveSkill = 50,
                        }
                    },
                    #endregion

                    #region Reference Setting

                    ReferenceSettings = new ReferenceSetting
                    {
                        IQChatSettings = new ReferenceSetting.IQChat
                        {
                            CustomPrefix = "",
                            CustomAvatar = ""
                        },
                        IQEconomicUse = false,
                        IQHeadRewardUse = false,
                        IQCraftSystem = false,
                        IQKits = false,
                        XDNotificationsSettings = new ReferenceSetting.XDNotifications
                        {
                            UseXDNotifications = false,
                            AlertDelete = 5,
                            Color = "#762424FF",
                            SoundEffect = "",
                            Title = "Генезис"
                        }
                    },

                    #endregion

                    #region FarmingDNK Settings
                    FarmingDNKS = new FarmingDNK
                    {
                        PlayerKills = new FarmingDNK.PlayerKill
                        {
                            DNKKillUser = true,
                            GeneralSettingsFarmings = new FarmingDNK.GeneralSettingsFarming
                            {
                                RareGiveDNK = 10,
                                MinimumDNK = 10,
                                MaximumDNK = 20,
                            }
                        },
                        AnimalKills = new FarmingDNK.AnimalKill
                        {
                            DNKKillAnimal = true,
                            GeneralSettingsFarmings = new FarmingDNK.GeneralSettingsFarming
                            {
                                RareGiveDNK = 10,
                                MinimumDNK = 10,
                                MaximumDNK = 20,
                            }
                        },
                        NPCKills = new FarmingDNK.NPCKill
                        {
                            DNKKillNPC = true,
                            GeneralSettingsFarmings = new FarmingDNK.GeneralSettingsFarming
                            {
                                RareGiveDNK = 10,
                                MinimumDNK = 10,
                                MaximumDNK = 20,
                            }
                        }
                    }
                    #endregion
                };
            }
        }

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
                PrintWarning(LanguageEn ? "Mistake #87" + $"read configuration 'oxide/config/{Name}', create a new configuration! #45" : "Ошибка #87" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию! #45");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Hooks

        #region Server Hooks
        private void Init() => RedistributionHooks();
        private void OnServerInitialized()
        {
            ReadData();
            foreach (var p in BasePlayer.activePlayerList)
                OnPlayerConnected(p);

            if (config.SkillSettings.CrafterSettings.SkillTurn)
                foreach (var bp in ItemManager.bpList)
                    Blueprints.Add(bp.targetItem.shortname, bp.time);

            LoadedImage();
            WriteData();
            PatogenInfected();
            StartRegeneration();

            if (!permission.PermissionExists(PermissionsPatogenArmor, this))
                permission.RegisterPermission(PermissionsPatogenArmor, this);
        }
        void OnPlayerConnected(BasePlayer player)
        {
            CahedImages(player);
            RegisteredDataUser(player);
        }
        void Unload()
        {
            foreach(BasePlayer player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, PLAGUE_PARENT_MAIN);
        }
        #endregion

        #region Farming DNK Hooks
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return;
            if (config.ReferenceSettings.IQEconomicUse) return;
            BasePlayer player = info.InitiatorPlayer;
            if (player == null
                || player.IsNpc
                || player.GetComponent<NPCPlayer>() != null
                || player.GetComponent<Zombie>() != null
                || player.GetComponent<HumanNPC>() != null)
                return;
            var FarmingDNK = config.FarmingDNKS;

            if (FarmingDNK.NPCKills.DNKKillNPC)
                if ((bool)(entity as NPCPlayer) || (bool)(entity as HumanNPC))
                {
                    var FarmingNPC = FarmingDNK.NPCKills.GeneralSettingsFarmings;
                    if (!IsRare(FarmingNPC.RareGiveDNK)) return;

                    GiveDNK(player, FarmingNPC.MinimumDNK, FarmingNPC.MaximumDNK);
                }

            if (FarmingDNK.PlayerKills.DNKKillUser)
                if ((bool)(entity as BasePlayer))
                {
                    if ((bool)(entity as NPCPlayer) || (bool)(entity as HumanNPC)) return;

                    BasePlayer targetPlayer = entity.ToPlayer();
                    if (targetPlayer == null) return;

                    if (targetPlayer.userID != player.userID)
                    {
                        if (IsFriends(player.userID, targetPlayer.userID)) return;
                        if (IsClans(player.UserIDString, targetPlayer.UserIDString)) return;
                        if (IsDuel(player.userID)) return;

                        var FarmingPlayer = FarmingDNK.PlayerKills.GeneralSettingsFarmings;
                        if (!IsRare(FarmingPlayer.RareGiveDNK)) return;

                        GiveDNK(player, FarmingPlayer.MinimumDNK, FarmingPlayer.MaximumDNK);
                    }
                }
            if (FarmingDNK.AnimalKills.DNKKillAnimal)
                if ((bool)(entity as BaseAnimalNPC))
                {
                    var FarmingAmimals = FarmingDNK.AnimalKills.GeneralSettingsFarmings;
                    if (!IsRare(FarmingAmimals.RareGiveDNK)) return;

                    GiveDNK(player, FarmingAmimals.MinimumDNK, FarmingAmimals.MaximumDNK);
                }
        }
        #endregion

        #region Skills Hooks

        #region Anabiotics
        Dictionary<ulong, double> Cd = new Dictionary<ulong, double>();
        object OnHealingItemUse(MedicalTool tool, BasePlayer player)
        {
            if (tool == null) return null;
            if (player == null) return null;
            if (player?.GetActiveItem() == null) return null;
            string Shortname = player?.GetActiveItem()?.info.shortname;

            Anabiotics(player, Shortname);
            return null;
        }
        object OnItemAction(Item item, string action, BasePlayer player)
        {
            if (action == "consume")
            {
                if (Cd.ContainsKey(player.userID))
                {
                    if (Cd[player.userID] > CurrentTime()) return null;
                    Cd[player.userID] = CurrentTime() + 1.0;
                    Anabiotics(player, item?.info?.shortname);
                }
                else
                {
                    Cd.Add(player.userID, CurrentTime() + 1.0);
                    Anabiotics(player, item?.info?.shortname);
                }
            }
            return null;
        }
        #endregion

        #region Gather & Animal Friends & Miner
        
        object OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (player == null) return null;
            if (dispenser == null) return null;
            if (item == null) return null;

            GatherFriends(player,item.info.shortname);

            item.amount = Miner(player, item.info.shortname, item.amount);
            return null;
        }
        object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity as BasePlayer;
            if (player == null) return null;
            
            AnimalFriends(player, dispenser.name);

            item.amount = Miner(player, item.info.shortname, item.amount);
            return null;
        }

        #endregion

        #region Metabolism
        private void OnPlayerRespawned(BasePlayer player)
        {
            if (player == null) return;
            Metabolism(player);
        }
        #endregion

        #region Wounded
        private void OnPlayerWound(BasePlayer player)
        {
            if (player == null 
                || player.IsNpc 
                || player.GetComponent<NPCPlayer>() != null 
                || player.GetComponent<Zombie>() != null 
                || player.GetComponent<HumanNPC>() != null)
                return;
            if (IsDuel(player.userID))
                return;

            NextTick(() =>
            {
                if (player.IsDead())
                    return;

                Wounded(player);
            });
            return;
        }
        #endregion

        #region Genesis Gens
        void OnNewSave(string filename)
        {
            GenesisGens();
            WipeController();
        }
        #endregion

        #region Thicks Skin
        void OnRunPlayerMetabolism(PlayerMetabolism metabolism, BaseCombatEntity combat)
        {
            if (metabolism == null) return;
            BasePlayer player = combat as BasePlayer;
            if (player == null) return;

            ThicksSkin(player);
            return;
        }
        #endregion

        #region Military
        void OnLoseCondition(Item item, ref float amount)
        {
            if (item == null) return;
            BasePlayer player = item.GetOwnerPlayer();
            if (player == null) return;
            amount = Military(player, item, amount);
        }
        #endregion

        #region Crafter
        public Dictionary<string, float> Blueprints { get; } = new Dictionary<string, float>();
        private object OnItemCraft(ItemCraftTask item, BasePlayer crafter) => OnCraft(item, crafter);
        private object OnCraft(ItemCraftTask task, BasePlayer crafter)
        {
            if (crafter == null) return null;
            if (task == null) return null;
            var Crafter = config.SkillSettings.CrafterSettings;
            if (!Crafter.SkillTurn) return null;
            if (!DataSkills[crafter.userID].Crafter)
            {
                if (!Blueprints.ContainsKey(task.blueprint.targetItem.shortname)) return null;
                task.blueprint.time = Blueprints[task.blueprint.targetItem.shortname];
                return null;
            }

            if (task.cancelled == true)
                return null;
            float Time = GetTime(crafter, task.blueprint.targetItem.shortname, Crafter.CraftBoost);
            task.blueprint.time = Time;
            return null;
        }
        public float GetTime(BasePlayer crafter, string CraftItem, float Time)
        {
            float Result = (float)(Blueprints[CraftItem] / Time);
            return Result;
        }

        #endregion

        #endregion

        #endregion

        #region Commands

        [ChatCommand("skill")]
        void ChatCommandSkill(BasePlayer player)
        {
            Interface_Panel(player);
        }

        [ConsoleCommand("iqps")]
        void IQPlagueSkillCommands(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args == null) return;
            switch(arg.Args[0])
            {
                case "give":  
                    {
                        ulong userID = ulong.Parse(arg.Args[1]);
                        if(!userID.IsSteamId())
                        {
                            PrintWarning(LanguageEn ? "You entered the wrong Steam ID" : "Вы неверно указали SteamID");
                            return;
                        }
                        int Amount = Convert.ToInt32(arg.Args[2]);
                        if(!DataInformation.ContainsKey(userID))
                        {
                            PrintWarning(LanguageEn ? "There is no such player" : "Такого игрока нет");
                            return;
                        }
                        DataInformation[userID] += Amount;
                        PrintWarning(LanguageEn ? "Successfully" : "Успешно");
                        break;
                    }
                case "study_all":
                    {
                        ulong UserID = ulong.Parse(arg.Args[1]);
                        if (!UserID.IsSteamId())
                        {
                            PrintError(LanguageEn ? "Invalid Steam ID" : "Неверно указан SteamID");
                            return;
                        }
                        if(!DataSkills.ContainsKey(UserID))
                        {
                            PrintError(LanguageEn ? "This player is not in the datafile" : "Такого игрока нет в датафайле");
                            return;
                        }
                        var UserSkills = DataSkills[UserID];
                        UserSkills.Anabiotics = true;
                        UserSkills.AnimalFriends = true;
                        UserSkills.Crafter = true;
                        UserSkills.GatherFriends = true;
                        UserSkills.GenesisGens = true;
                        UserSkills.Metabolism = true;
                        UserSkills.Military = true;
                        UserSkills.Miner = true;
                        UserSkills.PatogenAmrory = true;
                        UserSkills.Regeneration = true;
                        UserSkills.ThickSkin = true;
                        UserSkills.WoundedShake = true;
                        UserSkills.IQCraftSystemAdvanced = true;
                        UserSkills.IQHeadReward = true;
                        UserSkills.IQKitsCooldownPercent = true;
                        UserSkills.IQKitsRareup = true;


                        if (BasePlayer.FindByID(UserID) != null)
                            SendChat(BasePlayer.FindByID(UserID), lang.GetMessage("STUDY_ALL_YES", this, UserID.ToString()));
                        break;
                    }
                case "study":
                    {
                        BasePlayer player = arg.Player();
                        if (player == null) return;
                        string Skill = arg.Args[1];
                        int Price = Convert.ToInt32(arg.Args[2]);
                        bool IsInfo = Convert.ToBoolean(arg.Args[3]);
                        StudySkill(player, Skill, Price, IsInfo);
                        break;
                    }
                case "debug":
                    {
                        if (arg.Player() != null && !arg.Player().IsAdmin) return;
                        GenesisGens();
                        WipeController();
                        foreach (BasePlayer player in BasePlayer.allPlayerList)
                        {
                            RegisteredDataUser(player);
                            DataInformation[player.userID] += 100;
                            WriteData();
                        }
                        PrintWarning("Debug end..");
                        break;
                    }
                case "close": 
                    {
                        BasePlayer player = arg.Player();
                        if (player == null) return;
                        CuiHelper.DestroyUi(player, PLAGUE_PARENT_MAIN);
                        if (IsOpenSkill.Contains(player.userID))
                            IsOpenSkill.Remove(player.userID);
                        break;
                    }
            }
        }

        #endregion

        #region Metods

        #region Skills

        #region Anabiotics
        public void Anabiotics(BasePlayer player, string Shortname)
        {
            var Anabiotics = config.SkillSettings.AnabioticsSettings;
            if (!Anabiotics.SkillTurn) return;
            if (!DataSkills.ContainsKey(player.userID)) return;
            if (!DataSkills[player.userID].Anabiotics) return;
            if (!Anabiotics.AnabioticsList.ContainsKey(Shortname)) return;

            player.Heal(Anabiotics.AnabioticsList[Shortname]);
        }
        #endregion

        #region AnimalFriends 

        void AnimalFriends(BasePlayer player, string Animal)
        {
            var AnimalFriends = config.SkillSettings.AnimalFriendsSettings;
            if (!AnimalFriends.SkillTurn) return;
            if (!DataSkills[player.userID].AnimalFriends) return;
            if (AnimalFriends.UseLists)
                if(AnimalFriends.AnimalsList.ContainsKey(Animal))
                    if (IsRare(AnimalFriends.AnimalsList[Animal].Rare))
                    {
                        GiveDNK(player, AnimalFriends.AnimalsList[Animal].MinDNKCustom, AnimalFriends.AnimalsList[Animal].MaxDNKCustom);
                        return;
                    }
            if (AnimalFriends.AnimalDetected.Contains(Animal))
            {
                if (IsRare(AnimalFriends.RareAll))
                    GiveDNK(player, AnimalFriends.MinDNKAnimal, AnimalFriends.MaxDNKAnimal);
            }
        }

        #endregion

        #region GatherFriends

        void GatherFriends(BasePlayer player, string Shortname)
        {
            var GatherFriends = config.SkillSettings.GatherFriendsSettings;
            if (!GatherFriends.SkillTurn) return;
            if (!DataSkills[player.userID].GatherFriends) return;
            if (GatherFriends.GatherList.ContainsKey(Shortname))
                if (IsRare(GatherFriends.GatherList[Shortname].Rare))
                {
                    GiveDNK(player, GatherFriends.GatherList[Shortname].MinDNKCustom, GatherFriends.GatherList[Shortname].MaxDNKCustom);
                    return;
                }
        }

        #endregion

        #region Metabolism

        public void Metabolism(BasePlayer player)
        {
            var Metabolism = config.SkillSettings.MetabolismSettings;
            if (!Metabolism.SkillTurn) return;
            if (!DataSkills[player.userID].Metabolism) return;
            if (!IsRare(Metabolism.RareMetabolisme)) return;
            player.health = Metabolism.Health;
            player.metabolism.calories.value = Metabolism.Calories;
            player.metabolism.hydration.value = Metabolism.Hydration;

            if (Metabolism.DropSkill)
                DataSkills[player.userID].Metabolism = false;
        }

        #endregion

        #region Miner

        int Miner(BasePlayer player, string Shortname, int Amount)
        {
            var Miner = config.SkillSettings.MinerSettings;
            int ReturnAmount = Amount;
            if (!Miner.SkillTurn) return ReturnAmount;
            if (!DataSkills[player.userID].Miner) return ReturnAmount;
            if (Miner.UseLists)
                if (Miner.CustomRate.ContainsKey(Shortname))
                {
                    ReturnAmount = (int)(Amount * Miner.CustomRate[Shortname]);
                    return ReturnAmount;
                }
            ReturnAmount = Convert.ToInt32(Amount * Miner.Rate);
            return ReturnAmount;
        }

        #endregion

        #region Wounded

        public void Wounded(BasePlayer player)
        {
            if (player == null) return;
            var Wounded = config.SkillSettings.WoundedShakeSettings;
            if (!Wounded.SkillTurn) return;
            if (!DataSkills[player.userID].WoundedShake) return;
            if (!IsRare(Wounded.Rare)) return;

            timer.Once(Wounded.RareStartTime, () => 
            {
                if (player.IsWounded())
                {
                    player.StopWounded();
                    SendChat(player, String.Format(lang.GetMessage("CHAT_SKILL_WOUNDED", this, player.UserIDString), Wounded.GeneralSettings.DisplayName));
                    if (Wounded.DropSkill)
                        DataSkills[player.userID].WoundedShake = false;
                }
            });
        }

        #endregion

        #region Genesis Gen

        void GenesisGens()
        {
            var SkillSettings = config.SkillSettings;
            if (!SkillSettings.GenesisGensSettings.SkillTurn) return;

            foreach (var PlayerList in DataSkills)
            {
                var Skill = PlayerList.Value;
                ulong userID = PlayerList.Key;

                if (!Skill.GenesisGens)
                {
                    DataInformation[userID] = 0;
                    return;
                }

                int SpentDNK = 0;
                int SaveDNK = 0;

                if (Skill.Anabiotics)
                    SpentDNK += SkillSettings.AnabioticsSettings.GeneralSettings.PriceDNK;
                if (Skill.AnimalFriends)
                    SpentDNK += SkillSettings.AnimalFriendsSettings.GeneralSettings.PriceDNK;
                if (Skill.Crafter)
                    SpentDNK += SkillSettings.CrafterSettings.GeneralSettings.PriceDNK;
                if (Skill.GatherFriends)
                    SpentDNK += SkillSettings.GatherFriendsSettings.GeneralSettings.PriceDNK;
                if (Skill.GenesisGens)
                    SpentDNK += SkillSettings.GenesisGensSettings.GeneralSettings.PriceDNK;
                if (Skill.Metabolism)
                    SpentDNK += SkillSettings.MetabolismSettings.GeneralSettings.PriceDNK;
                if (Skill.Military)
                    SpentDNK += SkillSettings.MilitarySettings.GeneralSettings.PriceDNK;
                if (Skill.Miner)
                    SpentDNK += SkillSettings.MinerSettings.GeneralSettings.PriceDNK;
                if (Skill.PatogenAmrory)
                    SpentDNK += SkillSettings.PatogenAmrorySettings.GeneralSettings.PriceDNK;
                if (Skill.Regeneration)
                    SpentDNK += SkillSettings.RegenerationSettings.GeneralSettings.PriceDNK;
                if (Skill.ThickSkin)
                    SpentDNK += SkillSettings.ThickSkinSettings.GeneralSettings.PriceDNK;
                if (Skill.WoundedShake)
                    SpentDNK += SkillSettings.WoundedShakeSettings.GeneralSettings.PriceDNK;

                SaveDNK = SpentDNK / 100 * SkillSettings.GenesisGensSettings.PercentSave;
                DataInformation[userID] = SaveDNK;
                PrintWarning(LanguageEn ? $"Player {userID} by skill Genesis gene was saved {SaveDNK} DNA" : $"Игроку {userID} по навыку Генезиз ген было сохранено {SaveDNK} ДНК");
            }
        }

        #endregion

        #region Thicks Skin
        void ThicksSkin(BasePlayer player)
        {
            if (player == null) return;
            var ThicksSkin = config.SkillSettings.ThickSkinSettings;
            if (!ThicksSkin.SkillTurn) return;
            if (!DataSkills[player.userID].ThickSkin) return;

            if (player.currentTemperature <= 1)
                player.metabolism.temperature.value = 21;
        }

        #endregion

        #region Military

        public float Military(BasePlayer player, Item item, float amount)
        {
            float Damage = amount;
            if (player == null) return Damage;
            var Military = config.SkillSettings.MilitarySettings;
            if (!Military.SkillTurn) return Damage;
            if (!DataSkills.ContainsKey(player.userID))
            {
                RegisteredDataUser(player);
                return Damage;
            }
            if (!DataSkills[player.userID].Military) return Damage;
            var ItemCategory = ItemManager.FindItemDefinition(item.info.itemid).category;
            if (ItemCategory != ItemCategory.Weapon) return Damage;
            Damage = amount - (amount / 100 * Military.PercentNoBroken);
            return Damage;
        }

        #endregion

        #region Regeneration       
        public void StartRegeneration()
        {
            timer.Every(config.SkillSettings.RegenerationSettings.RegenerationTimer, () =>
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                        Regeneration(player);
            });
        }
        public void Regeneration(BasePlayer player)
        {
            if (player == null) return;
            var Regeneration = config.SkillSettings.RegenerationSettings;
            if (!Regeneration.SkillTurn) return;
            if (!DataSkills[player.userID].Regeneration) return;

            player.Heal(Regeneration.HealtRegeneration);
        }
        #endregion

        #endregion

        #region RedistributionHooks

        void RedistributionHooks()
        {
            var Skill = config.SkillSettings;
            if (!Skill.AnabioticsSettings.SkillTurn)
            {
                Unsubscribe("OnHealingItemUse");
                Unsubscribe("OnItemAction");
            }
            if (!Skill.MetabolismSettings.SkillTurn)
                Unsubscribe("OnPlayerRespawned");
            if (!Skill.GatherFriendsSettings.SkillTurn && !Skill.AnimalFriendsSettings.SkillTurn && !Skill.MinerSettings.SkillTurn)
            {
                Unsubscribe("OnDispenserBonus");
                Unsubscribe("OnDispenserGather");
            }
            if (!Skill.WoundedShakeSettings.SkillTurn)
                Unsubscribe("CanBeWounded");
            if (!Skill.ThickSkinSettings.SkillTurn)
                Unsubscribe("OnRunPlayerMetabolism");
            if (!Skill.MilitarySettings.SkillTurn)
                Unsubscribe("OnLoseCondition");
            if (!Skill.CrafterSettings.SkillTurn)
                Unsubscribe("OnItemCraft");
        }

        #endregion

        #region Wipe Controller

        void WipeController()
        {
            var Controller = config.GeneralSettings.WipeContollers;
            if (!Controller.WipeDataUse) return;
            PrintWarning(LanguageEn ? "Server wipe detected!" : "Обнаружен вайп сервера!");
            if (Controller.WipeDataSkill)
            {
                DataSkills.Clear();
                PrintWarning(LanguageEn ? "Player skills successfully reset" : "Скиллы игроков успешно сброшены");
            }
            PrintWarning(LanguageEn ? "The plugin has successfully completed automatic cleanup" : "Плагин успешно закончил автоматическую очистку");
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQPlagueSkill/InformationUser", DataInformation);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQPlagueSkill/InformationSkills", DataSkills);
        }

        #endregion

        #region StudySkill
        public void StudySkill(BasePlayer player, string Skill, int Price, bool IsInfo)
        {
            var UserSkills = DataSkills[player.userID];
            string DisplayName = string.Empty;

            switch (Skill)
            {
                case "anabiotics":
                    {
                        if (IsInfo)
                        {
                            Interface_Show_Information(player, config.SkillSettings.AnabioticsSettings.GeneralSettings, Skill,TypeSkill.Active);
                            return;
                        }
                        UserSkills.Anabiotics = true;
                        RemoveBalance(player, Price);
                        LoadedSkills(player);
                        DisplayName = config.SkillSettings.AnabioticsSettings.GeneralSettings.DisplayName;
                        break;
                    }
                case "animalfriends":
                    {
                        if (IsInfo)
                        {
                            Interface_Show_Information(player, config.SkillSettings.AnimalFriendsSettings.GeneralSettings, Skill, TypeSkill.Active);
                            return;
                        }
                        UserSkills.AnimalFriends = true;
                        RemoveBalance(player, Price);
                        LoadedSkills(player);
                        DisplayName = config.SkillSettings.AnimalFriendsSettings.GeneralSettings.DisplayName;
                        break;
                    }
                case "crafter":
                    {
                        if (IsInfo)
                        {
                            Interface_Show_Information(player, config.SkillSettings.CrafterSettings.GeneralSettings, Skill, TypeSkill.Active);
                            return;
                        }
                        UserSkills.Crafter = true;
                        RemoveBalance(player, Price);
                        LoadedSkills(player);
                        DisplayName = config.SkillSettings.CrafterSettings.GeneralSettings.DisplayName;
                        break;
                    }
                case "gatherfriends":
                    {
                        if (IsInfo)
                        {
                            Interface_Show_Information(player, config.SkillSettings.GatherFriendsSettings.GeneralSettings, Skill, TypeSkill.Active);
                            return;
                        }
                        UserSkills.GatherFriends = true;
                        RemoveBalance(player, Price);
                        LoadedSkills(player);
                        DisplayName = config.SkillSettings.GatherFriendsSettings.GeneralSettings.DisplayName;
                        break;
                    }
                case "genesisgens":
                    {
                        if (IsInfo)
                        {
                            Interface_Show_Information(player, config.SkillSettings.GenesisGensSettings.GeneralSettings, Skill, TypeSkill.Active);
                            return;
                        }
                        UserSkills.GenesisGens = true;
                        RemoveBalance(player, Price);
                        LoadedSkills(player);
                        DisplayName = config.SkillSettings.GenesisGensSettings.GeneralSettings.DisplayName;
                        break;
                    }
                case "metabolism":
                    {
                        if (IsInfo)
                        {
                            Interface_Show_Information(player, config.SkillSettings.MetabolismSettings.GeneralSettings, Skill, TypeSkill.Active);
                            return;
                        }
                        UserSkills.Metabolism = true;
                        RemoveBalance(player, Price);
                        LoadedSkills(player);
                        DisplayName = config.SkillSettings.MetabolismSettings.GeneralSettings.DisplayName;
                        break;
                    }
                case "military":
                    {
                        if (IsInfo)
                        {
                            Interface_Show_Information(player, config.SkillSettings.MilitarySettings.GeneralSettings, Skill, TypeSkill.Active);
                            return;
                        }
                        UserSkills.Military = true;
                        RemoveBalance(player, Price);
                        LoadedSkills(player);
                        DisplayName = config.SkillSettings.MilitarySettings.GeneralSettings.DisplayName;
                        break;
                    }
                case "miner":
                    {
                        if (IsInfo)
                        {
                            Interface_Show_Information(player, config.SkillSettings.MinerSettings.GeneralSettings, Skill, TypeSkill.Active);
                            return;
                        }
                        UserSkills.Miner = true;
                        RemoveBalance(player, Price);
                        LoadedSkills(player);
                        DisplayName = config.SkillSettings.MinerSettings.GeneralSettings.DisplayName;
                        break;
                    }
                case "patogenarmory":
                    {
                        if (IsInfo)
                        {
                            Interface_Show_Information(player, config.SkillSettings.PatogenAmrorySettings.GeneralSettings, Skill, TypeSkill.Active);
                            return;
                        }
                        UserSkills.PatogenAmrory = true;
                        RemoveBalance(player, Price);
                        LoadedSkills(player);
                        DisplayName = config.SkillSettings.PatogenAmrorySettings.GeneralSettings.DisplayName;
                        break;
                    }
                case "patogenkill":
                    {
                        if (IsInfo)
                        {
                            Interface_Show_Information(player, config.SkillSettings.PatogenKillSettings.GeneralSettings, Skill, TypeSkill.Active);
                            return;
                        }
                        RemoveBalance(player, Price);
                        LoadedSkills(player);
                        PatogenRecovered(player);
                        DisplayName = config.SkillSettings.PatogenKillSettings.GeneralSettings.DisplayName;
                        break;
                    }
                case "regeneration":
                    {
                        if (IsInfo)
                        {
                            Interface_Show_Information(player, config.SkillSettings.RegenerationSettings.GeneralSettings, Skill, TypeSkill.Active);
                            return;
                        }
                        UserSkills.Regeneration = true;
                        RemoveBalance(player, Price);
                        LoadedSkills(player);
                        DisplayName = config.SkillSettings.RegenerationSettings.GeneralSettings.DisplayName;
                        break;
                    }
                case "thicksskin":
                    {
                        if (IsInfo)
                        {
                            Interface_Show_Information(player, config.SkillSettings.ThickSkinSettings.GeneralSettings, Skill, TypeSkill.Active);
                            return;
                        }
                        UserSkills.ThickSkin = true;
                        RemoveBalance(player, Price);
                        LoadedSkills(player);
                        DisplayName = config.SkillSettings.ThickSkinSettings.GeneralSettings.DisplayName;
                        break;
                    }
                case "wounded":
                    {
                        if (IsInfo)
                        {
                            Interface_Show_Information(player, config.SkillSettings.WoundedShakeSettings.GeneralSettings, Skill, TypeSkill.Active);
                            return;
                        }
                        UserSkills.WoundedShake = true;
                        RemoveBalance(player, Price);
                        LoadedSkills(player);
                        DisplayName = config.SkillSettings.WoundedShakeSettings.GeneralSettings.DisplayName;
                        break;
                    }
                case "iqheadreward": 
                    {
                        if (IsInfo)
                        {
                            Interface_Show_Information(player, config.SkillSettings.NeutralSkills.SkillIQHeadRewards.GeneralSettings, Skill, TypeSkill.Neutral);
                            return;
                        }
                        UserSkills.IQHeadReward = true;
                        RemoveBalance(player, Price);
                        LoadedSkills(player);
                        DisplayName = config.SkillSettings.NeutralSkills.SkillIQHeadRewards.GeneralSettings.DisplayName;
                        break;
                    }
                case "iqcraftsystem.advanced":
                    {
                        if (IsInfo)
                        {
                            Interface_Show_Information(player, config.SkillSettings.NeutralSkills.IQCraftSystemAdvancedCrafts.GeneralSettings, Skill, TypeSkill.Neutral);
                            return;
                        }
                        UserSkills.IQCraftSystemAdvanced = true;
                        RemoveBalance(player, Price);
                        LoadedSkills(player);
                        DisplayName = config.SkillSettings.NeutralSkills.IQCraftSystemAdvancedCrafts.GeneralSettings.DisplayName;
                        break;
                    }
               case "iqkits.cooldown":
                    {
                        if (IsInfo)
                        {
                            Interface_Show_Information(player, config.SkillSettings.NeutralSkills.IQKitsCooldown.GeneralSettings, Skill, TypeSkill.Neutral);
                            return;
                        }
                        UserSkills.IQKitsCooldownPercent = true;
                        RemoveBalance(player, Price);
                        LoadedSkills(player);
                        DisplayName = config.SkillSettings.NeutralSkills.IQKitsCooldown.GeneralSettings.DisplayName;
                        break;
                    }
                case "iqkits.rare":
                    {
                        if (IsInfo)
                        {
                            Interface_Show_Information(player, config.SkillSettings.NeutralSkills.IQKitsRare.GeneralSettings, Skill, TypeSkill.Neutral);
                            return;
                        }
                        UserSkills.IQKitsRareup = true;
                        RemoveBalance(player, Price);
                        LoadedSkills(player);
                        DisplayName = config.SkillSettings.NeutralSkills.IQKitsRare.GeneralSettings.DisplayName;
                        break;
                    }
            }
            Interface.Oxide.CallHook("StudySkill", player, DisplayName);
        }
        void RemoveBalance(BasePlayer player, int Price, bool SkillEconomic = false)
        {
            if(SkillEconomic)
                if(IQEconomic)
                {
                    RemoveBalanceUser(player.userID, Price);
                    return;
                }
            if (config.ReferenceSettings.IQEconomicUse)
                if (IQEconomic)
                {
                    RemoveBalanceUser(player.userID, Price);
                    return;
                }
            DataInformation[player.userID] -= Price;          
        }
        #endregion

        #region Patogen
        public void PatogenInfected()
        {
            var PatogenController = config.GeneralSettings.VirusPatogens;
            if (!PatogenController.UsePatogen) return;

            timer.Every(PatogenController.TimerInfectedVirus, () =>
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    if (permission.UserHasPermission(player.UserIDString, PermissionsPatogenArmor)) continue;

                    var DataSkill = DataSkills[player.userID];
                    var Notification = config.ReferenceSettings.XDNotificationsSettings;

                    if (DataSkill.PatogenAttack) return;
                    if(IsRare(PatogenController.RareInfected))
                    {
                        if(DataSkill.PatogenAmrory)
                        {
                            DataSkill.PatogenAmrory = false;
                            if (Notification.UseXDNotifications && XDNotifications)
                                AddNotify(player, Notification.Title, lang.GetMessage("CHAT_VIRUS_PATOGEN_RETURNED", this, player.UserIDString));
                            else SendChat(player, lang.GetMessage("CHAT_VIRUS_PATOGEN_RETURNED", this, player.UserIDString));
                            return;
                        }
                        DataSkill.PatogenAttack = true;
                        if (Notification.UseXDNotifications && XDNotifications)
                            AddNotify(player, Notification.Title, lang.GetMessage("CHAT_VIRUS_PATOGEN_INFECTED", this, player.UserIDString));
                        else SendChat(player, lang.GetMessage("CHAT_VIRUS_PATOGEN_INFECTED", this, player.UserIDString));
                    }
                }
                PrintWarning(LanguageEn ? $"The wave of infections with the PATHOGEN virus has passed" : $"Прошла волна заражений вирусом ПАТОГЕН");
            });
            PatogenAttack();
        }
        public void PatogenAttack()
        {
            var PatogenController = config.GeneralSettings.VirusPatogens;
            if (!PatogenController.UsePatogen) return;

            timer.Every(PatogenController.TimerRemoveSkill, () =>
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    var DataSkill = DataSkills[player.userID];
                    if (!DataSkill.PatogenAttack) return;
                    int Rare = UnityEngine.Random.Range(0, 100);

                    if (DataSkill.Anabiotics)
                        if (IsRare(Rare))
                        {
                            DataSkill.Anabiotics = false;
                            return;
                        }
                    if (DataSkill.AnimalFriends)
                        if (IsRare(Rare))
                        {
                            DataSkill.AnimalFriends = false;
                            return;
                        }
                    if (DataSkill.Crafter)
                        if (IsRare(Rare))
                        {
                            DataSkill.Crafter = false;
                            return;
                        }
                    if (DataSkill.GatherFriends)
                        if (IsRare(Rare))
                        {
                            DataSkill.GatherFriends = false;
                            return;
                        }
                    if (DataSkill.GenesisGens)
                        if (IsRare(Rare))
                        {
                            DataSkill.GenesisGens = false;
                            return;
                        }
                    if (DataSkill.Metabolism)
                        if (IsRare(Rare))
                        {
                            DataSkill.Metabolism = false;
                            return;
                        }
                    if (DataSkill.Military)
                        if (IsRare(Rare))
                        {
                            DataSkill.Military = false;
                            return;
                        }
                    if (DataSkill.Miner)
                        if (IsRare(Rare))
                        {
                            DataSkill.Miner = false;
                            return;
                        }
                    if (DataSkill.PatogenAmrory)
                        if (IsRare(Rare))
                        {
                            DataSkill.PatogenAmrory = false;
                            return;
                        }
                    if (DataSkill.Regeneration)
                        if (IsRare(Rare))
                        {
                            DataSkill.Regeneration = false;
                            return;
                        }
                    if (DataSkill.ThickSkin)
                        if (IsRare(Rare))
                        {
                            DataSkill.ThickSkin = false;
                            return;
                        }
                    if (DataSkill.WoundedShake)
                        if (IsRare(Rare))
                        {
                            DataSkill.WoundedShake = false;
                            return;
                        }
                    if(config.ReferenceSettings.IQHeadRewardUse)
                        if(IQHeadReward)
                            if(DataSkill.IQHeadReward)
                                if (IsRare(Rare))
                                {
                                    DataSkill.IQHeadReward = false;
                                    return;
                                }
                    if (config.ReferenceSettings.IQCraftSystem)
                        if (IQCraftSystem)
                        {
                            if (DataSkill.IQCraftSystemAdvanced)
                                if (IsRare(Rare))
                                {
                                    DataSkill.IQCraftSystemAdvanced = false;
                                    return;
                                }
                        }
                }
            });
        }
        public void PatogenRecovered(BasePlayer player)
        {
            var DataSkill = DataSkills[player.userID];

            DataSkill.PatogenAttack = false;
            var Notification = config.ReferenceSettings.XDNotificationsSettings;
            if (Notification.UseXDNotifications && XDNotifications)
                AddNotify(player, Notification.Title, lang.GetMessage("CHAT_VIRUS_PATOGEN_RECOVERED", this, player.UserIDString));
            else SendChat(player, lang.GetMessage("CHAT_VIRUS_PATOGEN_RECOVERED", this, player.UserIDString));

            Interface_Skill_PatogenKill(player);
            CuiHelper.DestroyUi(player, "PATOGEN_TITLE");
            CuiHelper.DestroyUi(player, "PATOGEN_DESCRIPTION");

            PrintWarning(LanguageEn ? $"Player {player.displayName} cured of the pathogen" : $"Игрок {player.displayName} излечился от ПАТОГЕНА");
        }

        #endregion

        #region Helps
        public List<ulong> IsOpenSkill = new List<ulong>();

        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;
        #endregion

        #region Bool Metods

        public bool IsAvailable(int Balance, int Price)
        {
            if (Balance > 0 && Balance >= Price)
                return true;
            else return false;
        }

        public bool IsRare(int Rare)
        {
            if (UnityEngine.Random.Range(0, 100) >= (100 - Rare))
                return true;
            else return false;
        }

        #endregion

        #region Farming DNK

        public void GiveDNK(BasePlayer player,int Min, int Max)
        {
            if (player == null) return;
            int DNK = UnityEngine.Random.Range(Min, Max);
            if (!DataInformation.ContainsKey(player.userID))
            {
                RegisteredDataUser(player);
                return;
            }
            DataInformation[player.userID] += DNK;

            var Notification = config.ReferenceSettings.XDNotificationsSettings;
            if (Notification.UseXDNotifications && XDNotifications)
                AddNotify(player,Notification.Title, String.Format(lang.GetMessage("CHAT_TAKE_DNK", this, player.UserIDString), DNK));
            else SendChat(player, String.Format(lang.GetMessage("CHAT_TAKE_DNK", this, player.UserIDString), DNK));
        }
        public void GiveDNK(BasePlayer player, int Amount)
        {
            DataInformation[player.userID] += Amount;
            var Notification = config.ReferenceSettings.XDNotificationsSettings;
            if (Notification.UseXDNotifications && XDNotifications)
                AddNotify(player, Notification.Title, String.Format(lang.GetMessage("CHAT_TAKE_DNK", this, player.UserIDString), Amount));
            else SendChat(player, String.Format(lang.GetMessage("CHAT_TAKE_DNK", this, player.UserIDString), Amount));
        }
        #endregion

        #endregion

        #region Interface
        static string PLAGUE_PARENT_MAIN = "PLAGUE_PARENT_MAIN";
        static string PLAGUE_PARENT_PANEL_INFORMATION = "PLAGUE_PARENT_PANEL_INFORMATION";

        #region Interface Panel
        public void Interface_Panel(BasePlayer player)
        {
            if (IsOpenSkill.Contains(player.userID)) return;

            var Interface = config.InterfaceSettings;
            var Balance = DataInformation[player.userID];
            var DataSkill = DataSkills[player.userID];
            var PatogenController = config.GeneralSettings.VirusPatogens;

            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PLAGUE_PARENT_MAIN);

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                FadeOut = 0.15f,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { FadeIn = 0.15f, Color = "0 0 0 0" }
            }, "Overlay", PLAGUE_PARENT_MAIN);

            container.Add(new CuiElement
            {
                Parent = PLAGUE_PARENT_MAIN,
                Components =
                    {
                        new CuiRawImageComponent {  Png = GetImage($"BACKGROUND_PLAGUES_{Interface.IconsPNG.BackgroundPNG}")},
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"},
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.3619792 0", AnchorMax = "0.6354166 0.06944445" },
                Button = { Command = "iqps close", Color = "0 0 0 0" },
                Text = { Text = lang.GetMessage("EXIT_BTN",this, player.UserIDString), Color = HexToRustFormat(Interface.GeneralSettings.HexLabels), Align = TextAnchor.MiddleCenter }
            }, PLAGUE_PARENT_MAIN);

            if (PatogenController.UsePatogen)
                if (DataSkill.PatogenAttack)
                {
                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0 0.9518518", AnchorMax = "0.1609375 0.9962962" },
                        Text = { Text = lang.GetMessage("VIRUS_PATOGEN_TITLE", this, player.UserIDString), Color = HexToRustFormat(Interface.GeneralSettings.HexLabelTakePanel), Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleRight }
                    }, PLAGUE_PARENT_MAIN, "PATOGEN_TITLE");

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.00729166 0.7962962", AnchorMax = "0.1609375 0.9592603" },
                        Text = { Text = lang.GetMessage("VIRUS_PATOGEN_DESCRIPTION", this, player.UserIDString), Color = HexToRustFormat(Interface.GeneralSettings.HexLabelTakePanel), Font = "robotocondensed-regular.ttf", Align = TextAnchor.UpperRight }
                    }, PLAGUE_PARENT_MAIN, "PATOGEN_DESCRIPTION"); 
                }

            container.Add(new CuiElement
            {
                Parent = PLAGUE_PARENT_MAIN,
                Name = "CIRCLE_NEUTRAL",
                Components =
                    {
                         new CuiRawImageComponent { Png = GetImage($"BACKGROUND_PLAGUE_NEUTRAL_SKILL_AVAILABLE_{Interface.IconsPNG.NeutralAvailableSkill}") },
                        new CuiRectTransformComponent{ AnchorMin = "0.01458329 0.02315771", AnchorMax = "0.03281246 0.05278735"},
                    }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = "<b>N</b>", Color = HexToRustFormat(Interface.GeneralSettings.HexLabelTakePanel), Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
            }, "CIRCLE_NEUTRAL", "CIRCLE_NEUTRAL_LABEL");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.03124995 0.02223178", AnchorMax = "0.2911459 0.05186141" },
                Text = { Text = lang.GetMessage("NEUTRAL_HELP_INFO", this, player.UserIDString), FontSize = 10, Color = HexToRustFormat(Interface.GeneralSettings.HexLabelTakePanel), Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft }
            }, PLAGUE_PARENT_MAIN, "CIRCLE_NEUTRAL_LABEL_HELP");

            container.Add(new CuiElement
            {
                Parent = PLAGUE_PARENT_MAIN,
                Name = "CIRCLE_ACTIVE",
                Components =
                    {
                         new CuiRawImageComponent { Png = GetImage($"BACKGROUND_PLAGUE_SKILL_AVAILABLE_{Interface.IconsPNG.AvailableSkill}") }, 
                        new CuiRectTransformComponent{ AnchorMin = "0.02343746 0.0009354877", AnchorMax = "0.04166662 0.03056512"},
                    }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = "<b>A</b>", Color = HexToRustFormat(Interface.GeneralSettings.HexLabelTakePanel), Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
            }, "CIRCLE_ACTIVE", "CIRCLE_ACTIVE_LABEL");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.04114577 0.0009354877", AnchorMax = "0.3010417 0.03056512" },
                Text = { Text = lang.GetMessage("ACTIVE_HELP_INFO", this, player.UserIDString), FontSize = 10, Color = HexToRustFormat(Interface.GeneralSettings.HexLabelTakePanel), Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft }
            }, PLAGUE_PARENT_MAIN, "CIRCLE_ACTIVE_LABEL_HELP");

            CuiHelper.AddUi(player, container);
            LoadedSkills(player);

            IsOpenSkill.Add(player.userID);
        }

        #endregion

        #region Skills UI
        public void LoadedSkills(BasePlayer player)
        {
            var Reference = config.ReferenceSettings;
            var DataSkill = DataSkills[player.userID];
            var PatogenController = config.GeneralSettings.VirusPatogens;

            Interface_Skill_Crafter(player);
            Interface_Skill_Anabiotics(player);
            Interface_Skill_AnimalFriends(player);
            Interface_Skill_GatherFriends(player);
            Interface_Skill_GenesisGen(player);
            Interface_Skill_Metabolism(player);
            Interface_Skill_Military(player);
            Interface_Skill_Miner(player);
            Interface_Skill_Regeneration(player);
            Interface_Skill_ThicksSkin(player);
            Interface_Skill_Wounded(player);

            if (Reference.IQHeadRewardUse)
                if (IQHeadReward)
                    if (config.SkillSettings.NeutralSkills.SkillIQHeadRewards.SkillTurn)
                        Interface_Skill_IQHeadReward(player);

            if(Reference.IQCraftSystem)
                if (IQCraftSystem)
                    if (config.SkillSettings.NeutralSkills.IQCraftSystemAdvancedCrafts.SkillTurn)
                        Interface_Skill_Advanced_IQCraftSystem(player);
                  
            if(Reference.IQKits)
                if (IQKits)
                {
                    if (config.SkillSettings.NeutralSkills.IQKitsRare.SkillTurn)
                        Interface_Skill_Rare_IQKits(player);
                    if (config.SkillSettings.NeutralSkills.IQKitsCooldown.SkillTurn)
                        Interface_Skill_DropPercent_IQKits(player);
                }
            if (PatogenController.UsePatogen)
            {
                Interface_Skill_PatogenArmory(player);
                if (DataSkill.PatogenAttack)
                    Interface_Skill_PatogenKill(player);
            }

            Interface_Balance(player);
        }

        #region Crafter
        void Interface_Skill_Crafter(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PLAGUE_PARENT_PANEL_INFORMATION);
            CuiHelper.DestroyUi(player, "SKILL_CRAFTER");

            var Interface = config.InterfaceSettings;
            var Skills = config.SkillSettings;
            var UserSkills = DataSkills[player.userID];
            var Balance = config.ReferenceSettings.IQEconomicUse ? IQEconomic ? GetBalanceUser(player.userID) : DataInformation[player.userID] : DataInformation[player.userID];
            var Skill = Skills.CrafterSettings;

            string SkillIcon = Skill.SkillTurn ? UserSkills.Crafter ? $"BACKGROUND_PLAGUE_SKILL_RECEIVED_{Interface.IconsPNG.ReceivedSkill}" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"BACKGROUND_PLAGUE_SKILL_AVAILABLE_{Interface.IconsPNG.AvailableSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}";
            CuiImageComponent Comp = Skill.SkillTurn ? String.IsNullOrWhiteSpace(Skill.GeneralSettings.Sprite) ? new CuiImageComponent { Png = GetImage($"SKILL_CRAFTER_{Skill.GeneralSettings.PNG}") } : new CuiImageComponent { Sprite = Skill.GeneralSettings.Sprite } : new CuiImageComponent { Sprite = "assets/icons/lock.png" };
            string Command = Skill.SkillTurn ? UserSkills.Crafter ? "" : config.SkillSettings.ShowSkillNotDNK ? $"iqps study crafter {Skill.GeneralSettings.PriceDNK} true" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"iqps study crafter {Skill.GeneralSettings.PriceDNK} true" : "" : "";

            container.Add(new CuiElement
            {
                Parent = PLAGUE_PARENT_MAIN,
                Name = "SKILL_CRAFTER",
                Components =
                    {
                        new CuiRawImageComponent {  Png = GetImage(SkillIcon) },
                        new CuiRectTransformComponent{ AnchorMin = "0.1546875 0.7685167", AnchorMax = "0.2520857 0.9203678"},
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "SKILL_CRAFTER",
                Components =
                    {
                        Comp,
                        new CuiRectTransformComponent{ AnchorMin = "0.2499933 0.2260868", AnchorMax = "0.749979 0.782609"},
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = Command, Color = "0 0 0 0" },
                Text = { Text = "", Color = "0 0 0 0" }
            }, "SKILL_CRAFTER");

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Anabiotics
        void Interface_Skill_Anabiotics(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PLAGUE_PARENT_PANEL_INFORMATION);
            CuiHelper.DestroyUi(player, "SKILL_ANABIOTICS");

            var Interface = config.InterfaceSettings;
            var Skills = config.SkillSettings;
            var UserSkills = DataSkills[player.userID];
            var Balance = config.ReferenceSettings.IQEconomicUse ? IQEconomic ? GetBalanceUser(player.userID) : DataInformation[player.userID] : DataInformation[player.userID];

            var Skill = Skills.AnabioticsSettings;
            
            string SkillIcon = Skill.SkillTurn ? UserSkills.Anabiotics ? $"BACKGROUND_PLAGUE_SKILL_RECEIVED_{Interface.IconsPNG.ReceivedSkill}" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"BACKGROUND_PLAGUE_SKILL_AVAILABLE_{Interface.IconsPNG.AvailableSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}";
            CuiImageComponent Comp = Skill.SkillTurn ? String.IsNullOrWhiteSpace(Skill.GeneralSettings.Sprite) ? new CuiImageComponent { Png = GetImage($"SKILL_ANABIOTICS_{Skill.GeneralSettings.PNG}") } : new CuiImageComponent { Sprite = Skill.GeneralSettings.Sprite } : new CuiImageComponent { Sprite = "assets/icons/lock.png" };
            string Command = Skill.SkillTurn ? UserSkills.Anabiotics ? "" : config.SkillSettings.ShowSkillNotDNK ? $"iqps study anabiotics {Skill.GeneralSettings.PriceDNK} true" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"iqps study anabiotics {Skill.GeneralSettings.PriceDNK} true" : "" : "";

            container.Add(new CuiElement
            {
                Parent = PLAGUE_PARENT_MAIN,
                Name = "SKILL_ANABIOTICS",
                Components =
                    {
                        new CuiRawImageComponent {  Png = GetImage(SkillIcon) },
                        new CuiRectTransformComponent{ AnchorMin = "0.0249997 0.4648148", AnchorMax = "0.1447917 0.6574074"},
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "SKILL_ANABIOTICS",
                Components =
                    {
                        Comp,
                        new CuiRectTransformComponent{ AnchorMin = "0.2499933 0.2260868", AnchorMax = "0.749979 0.782609"},
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = Command, Color = "0 0 0 0" },
                Text = { Text = "", Color = "0 0 0 0" }
            }, "SKILL_ANABIOTICS");

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Animal Friends
        void Interface_Skill_AnimalFriends(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PLAGUE_PARENT_PANEL_INFORMATION);
            CuiHelper.DestroyUi(player, "SKILL_ANIMALFRIENDS");

            var Interface = config.InterfaceSettings;
            var Skills = config.SkillSettings;
            var UserSkills = DataSkills[player.userID];
            var Balance = config.ReferenceSettings.IQEconomicUse ? IQEconomic ? GetBalanceUser(player.userID) : DataInformation[player.userID] : DataInformation[player.userID];

            var Skill = Skills.AnimalFriendsSettings;
            
            string SkillIcon = Skill.SkillTurn ? UserSkills.AnimalFriends ? $"BACKGROUND_PLAGUE_SKILL_RECEIVED_{Interface.IconsPNG.ReceivedSkill}" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"BACKGROUND_PLAGUE_SKILL_AVAILABLE_{Interface.IconsPNG.AvailableSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}";
            CuiImageComponent Comp = Skill.SkillTurn ? String.IsNullOrWhiteSpace(Skill.GeneralSettings.Sprite) ? new CuiImageComponent { Png = GetImage($"SKILL_ANIMALFRIENDS_{Skill.GeneralSettings.PNG}") } : new CuiImageComponent { Sprite = Skill.GeneralSettings.Sprite } : new CuiImageComponent { Sprite = "assets/icons/lock.png" };
            string Command = Skill.SkillTurn ? UserSkills.AnimalFriends ? "" : config.SkillSettings.ShowSkillNotDNK ? $"iqps study animalfriends {Skill.GeneralSettings.PriceDNK} true" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"iqps study animalfriends {Skill.GeneralSettings.PriceDNK} true" : "" : "";

            container.Add(new CuiElement
            {
                Parent = PLAGUE_PARENT_MAIN,
                Name = "SKILL_ANIMALFRIENDS",
                Components =
                    {
                        new CuiRawImageComponent {  Png = GetImage(SkillIcon) },
                        new CuiRectTransformComponent{ AnchorMin = "0.2593771 0.06944337", AnchorMax = "0.332289 0.1870369"},
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "SKILL_ANIMALFRIENDS",
                Components =
                    {
                        Comp,
                        new CuiRectTransformComponent{ AnchorMin = "0.2499933 0.2260868", AnchorMax = "0.749979 0.782609"},
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = Command, Color = "0 0 0 0" },
                Text = { Text = "", Color = "0 0 0 0" }
            }, "SKILL_ANIMALFRIENDS");

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Gather Friends
        void Interface_Skill_GatherFriends(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PLAGUE_PARENT_PANEL_INFORMATION);
            CuiHelper.DestroyUi(player, "SKILL_GATHERFRIENDS");

            var Interface = config.InterfaceSettings;
            var Skills = config.SkillSettings;
            var UserSkills = DataSkills[player.userID];
            var Balance = config.ReferenceSettings.IQEconomicUse ? IQEconomic ? GetBalanceUser(player.userID) : DataInformation[player.userID] : DataInformation[player.userID];

            var Skill = Skills.GatherFriendsSettings;

            string SkillIcon = Skill.SkillTurn ? UserSkills.GatherFriends ? $"BACKGROUND_PLAGUE_SKILL_RECEIVED_{Interface.IconsPNG.ReceivedSkill}" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"BACKGROUND_PLAGUE_SKILL_AVAILABLE_{Interface.IconsPNG.AvailableSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}";
            CuiImageComponent Comp = Skill.SkillTurn ? String.IsNullOrWhiteSpace(Skill.GeneralSettings.Sprite) ? new CuiImageComponent { Png = GetImage($"SKILL_GATHERFRIENDS_{Skill.GeneralSettings.PNG}") } : new CuiImageComponent { Sprite = Skill.GeneralSettings.Sprite } : new CuiImageComponent { Sprite = "assets/icons/lock.png" };
            string Command = Skill.SkillTurn ? UserSkills.GatherFriends ? "" : config.SkillSettings.ShowSkillNotDNK ? $"iqps study gatherfriends {Skill.GeneralSettings.PriceDNK} true" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"iqps study gatherfriends {Skill.GeneralSettings.PriceDNK} true" : "" : "";

            container.Add(new CuiElement
            {
                Parent = PLAGUE_PARENT_MAIN,
                Name = "SKILL_GATHERFRIENDS",
                Components =
                    {
                        new CuiRawImageComponent {  Png = GetImage(SkillIcon) },
                        new CuiRectTransformComponent{ AnchorMin = "0.1812513 0.2555555", AnchorMax = "0.2494792 0.3648151"},
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "SKILL_GATHERFRIENDS",
                Components =
                    {
                        Comp,
                        new CuiRectTransformComponent{ AnchorMin = "0.2499933 0.2260868", AnchorMax = "0.749979 0.782609"},
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = Command, Color = "0 0 0 0" },
                Text = { Text = "", Color = "0 0 0 0" }
            }, "SKILL_GATHERFRIENDS");

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Genesis Gen
        void Interface_Skill_GenesisGen(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PLAGUE_PARENT_PANEL_INFORMATION);
            CuiHelper.DestroyUi(player, "SKILL_GENESISGENS");

            var Interface = config.InterfaceSettings;
            var Skills = config.SkillSettings;
            var UserSkills = DataSkills[player.userID];
            var Balance = config.ReferenceSettings.IQEconomicUse ? IQEconomic ? GetBalanceUser(player.userID) : DataInformation[player.userID] : DataInformation[player.userID];

            var Skill = Skills.GenesisGensSettings;
            
            string SkillIcon = Skill.SkillTurn ? UserSkills.GenesisGens ? $"BACKGROUND_PLAGUE_SKILL_RECEIVED_{Interface.IconsPNG.ReceivedSkill}" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"BACKGROUND_PLAGUE_SKILL_AVAILABLE_{Interface.IconsPNG.AvailableSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}";
            CuiImageComponent Comp = Skill.SkillTurn ? String.IsNullOrWhiteSpace(Skill.GeneralSettings.Sprite) ? new CuiImageComponent { Png = GetImage($"SKILL_GENESISGENS_{Skill.GeneralSettings.PNG}") } : new CuiImageComponent { Sprite = Skill.GeneralSettings.Sprite } : new CuiImageComponent { Sprite = "assets/icons/lock.png" };
            string Command = Skill.SkillTurn ? UserSkills.GenesisGens ? "" : config.SkillSettings.ShowSkillNotDNK ? $"iqps study genesisgens {Skill.GeneralSettings.PriceDNK} true" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"iqps study genesisgens {Skill.GeneralSettings.PriceDNK} true" : "" : "";

            container.Add(new CuiElement
            {
                Parent = PLAGUE_PARENT_MAIN,
                Name = "SKILL_GENESISGENS",
                Components =
                    {
                        new CuiRawImageComponent {  Png = GetImage(SkillIcon) },
                        new CuiRectTransformComponent{ AnchorMin = "0.5697917 0.3074074", AnchorMax = "0.6718751 0.4722229"},
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "SKILL_GENESISGENS",
                Components =
                    {
                        Comp,
                        new CuiRectTransformComponent{ AnchorMin = "0.2499933 0.2260868", AnchorMax = "0.749979 0.782609"},
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = Command, Color = "0 0 0 0" },
                Text = { Text = "", Color = "0 0 0 0" }
            }, "SKILL_GENESISGENS");

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Metabolism Gen
        void Interface_Skill_Metabolism(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PLAGUE_PARENT_PANEL_INFORMATION);
            CuiHelper.DestroyUi(player, "SKILL_METABOLISM");

            var Interface = config.InterfaceSettings;
            var Skills = config.SkillSettings;
            var UserSkills = DataSkills[player.userID];
            var Balance = config.ReferenceSettings.IQEconomicUse ? IQEconomic ? GetBalanceUser(player.userID) : DataInformation[player.userID] : DataInformation[player.userID];

            var Skill = Skills.MetabolismSettings;
            
            string SkillIcon = Skill.SkillTurn ? UserSkills.Metabolism ? $"BACKGROUND_PLAGUE_SKILL_RECEIVED_{Interface.IconsPNG.ReceivedSkill}" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"BACKGROUND_PLAGUE_SKILL_AVAILABLE_{Interface.IconsPNG.AvailableSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}";
            CuiImageComponent Comp = Skill.SkillTurn ? String.IsNullOrWhiteSpace(Skill.GeneralSettings.Sprite) ? new CuiImageComponent { Png = GetImage($"SKILL_METABOLISM_{Skill.GeneralSettings.PNG}") } : new CuiImageComponent { Sprite = Skill.GeneralSettings.Sprite } : new CuiImageComponent { Sprite = "assets/icons/lock.png" };
            string Command = Skill.SkillTurn ? UserSkills.Metabolism ? "" : config.SkillSettings.ShowSkillNotDNK ? $"iqps study metabolism {Skill.GeneralSettings.PriceDNK} true" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"iqps study metabolism {Skill.GeneralSettings.PriceDNK} true" : "" : "";

            container.Add(new CuiElement
            {
                Parent = PLAGUE_PARENT_MAIN,
                Name = "SKILL_METABOLISM",
                Components =
                    {
                        new CuiRawImageComponent {  Png = GetImage(SkillIcon) },
                        new CuiRectTransformComponent{ AnchorMin = "0.9369792 0", AnchorMax = "1 0.1074074"},
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "SKILL_METABOLISM",
                Components =
                    {
                        Comp,
                        new CuiRectTransformComponent{ AnchorMin = "0.2499933 0.2260868", AnchorMax = "0.749979 0.782609"},
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = Command, Color = "0 0 0 0" },
                Text = { Text = "", Color = "0 0 0 0" }
            }, "SKILL_METABOLISM");

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Military
        void Interface_Skill_Military(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PLAGUE_PARENT_PANEL_INFORMATION);
            CuiHelper.DestroyUi(player, "SKILL_MILITARY");

            var Interface = config.InterfaceSettings;
            var Skills = config.SkillSettings;
            var UserSkills = DataSkills[player.userID];
            var Balance = config.ReferenceSettings.IQEconomicUse ? IQEconomic ? GetBalanceUser(player.userID) : DataInformation[player.userID] : DataInformation[player.userID];

            var Skill = Skills.MilitarySettings;
            
            string SkillIcon = Skill.SkillTurn ? UserSkills.Military ? $"BACKGROUND_PLAGUE_SKILL_RECEIVED_{Interface.IconsPNG.ReceivedSkill}" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"BACKGROUND_PLAGUE_SKILL_AVAILABLE_{Interface.IconsPNG.AvailableSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}";
            CuiImageComponent Comp = Skill.SkillTurn ? String.IsNullOrWhiteSpace(Skill.GeneralSettings.Sprite) ? new CuiImageComponent { Png = GetImage($"SKILL_MILITARY_{Skill.GeneralSettings.PNG}") } : new CuiImageComponent { Sprite = Skill.GeneralSettings.Sprite } : new CuiImageComponent { Sprite = "assets/icons/lock.png" };
            string Command = Skill.SkillTurn ? UserSkills.Military ? "" : config.SkillSettings.ShowSkillNotDNK ? $"iqps study military {Skill.GeneralSettings.PriceDNK} true" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"iqps study military {Skill.GeneralSettings.PriceDNK} true" : "" : "";

            container.Add(new CuiElement
            {
                Parent = PLAGUE_PARENT_MAIN,
                Name = "SKILL_MILITARY",
                Components =
                    {
                        new CuiRawImageComponent {  Png = GetImage(SkillIcon) },
                        new CuiRectTransformComponent{ AnchorMin = "0.4614567 0.6472288", AnchorMax = "0.543224 0.7750065"},
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "SKILL_MILITARY",
                Components =
                    {
                        Comp,
                        new CuiRectTransformComponent{ AnchorMin = "0.2499933 0.2260868", AnchorMax = "0.749979 0.782609"},
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = Command, Color = "0 0 0 0" },
                Text = { Text = "", Color = "0 0 0 0" }
            }, "SKILL_MILITARY");

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Miner
        void Interface_Skill_Miner(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PLAGUE_PARENT_PANEL_INFORMATION);
            CuiHelper.DestroyUi(player, "SKILL_MINER");

            var Interface = config.InterfaceSettings;
            var Skills = config.SkillSettings;
            var UserSkills = DataSkills[player.userID];
            var Balance = config.ReferenceSettings.IQEconomicUse ? IQEconomic ? GetBalanceUser(player.userID) : DataInformation[player.userID] : DataInformation[player.userID];

            var Skill = Skills.MinerSettings;
              
            string SkillIcon = Skill.SkillTurn ? UserSkills.Miner ? $"BACKGROUND_PLAGUE_SKILL_RECEIVED_{Interface.IconsPNG.ReceivedSkill}" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"BACKGROUND_PLAGUE_SKILL_AVAILABLE_{Interface.IconsPNG.AvailableSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}";
            CuiImageComponent Comp = Skill.SkillTurn ? String.IsNullOrWhiteSpace(Skill.GeneralSettings.Sprite) ? new CuiImageComponent { Png = GetImage($"SKILL_MINER_{Skill.GeneralSettings.PNG}") } : new CuiImageComponent { Sprite = Skill.GeneralSettings.Sprite } : new CuiImageComponent { Sprite = "assets/icons/lock.png" };
            string Command = Skill.SkillTurn ? UserSkills.Miner ? "" : config.SkillSettings.ShowSkillNotDNK ? $"iqps study miner {Skill.GeneralSettings.PriceDNK} true" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"iqps study miner {Skill.GeneralSettings.PriceDNK} true" : "" : "";

            container.Add(new CuiElement
            {
                Parent = PLAGUE_PARENT_MAIN,
                Name = "SKILL_MINER",
                Components =
                    {
                        new CuiRawImageComponent {  Png = GetImage(SkillIcon) },
                        new CuiRectTransformComponent{ AnchorMin = "0.3359377 0.8611", AnchorMax = "0.423951432 0.9955704"}, 
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "SKILL_MINER",
                Components =
                    {
                        Comp,
                        new CuiRectTransformComponent{ AnchorMin = "0.2499953 0.2260848", AnchorMax = "0.749939 0.78201432"},  
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = Command, Color = "0 0 0 0" },
                Text = { Text = "", Color = "0 0 0 0" }
            }, "SKILL_MINER");

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Patogen Armory
        void Interface_Skill_PatogenArmory(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PLAGUE_PARENT_PANEL_INFORMATION);
            CuiHelper.DestroyUi(player, "SKILL_PATOGENARMORY");

            var Interface = config.InterfaceSettings;
            var Skills = config.SkillSettings;
            var UserSkills = DataSkills[player.userID];
            var Balance = config.ReferenceSettings.IQEconomicUse ? IQEconomic ? GetBalanceUser(player.userID) : DataInformation[player.userID] : DataInformation[player.userID];

            var Skill = Skills.PatogenAmrorySettings;
            
            string SkillIcon = Skill.SkillTurn ? UserSkills.PatogenAmrory ? $"BACKGROUND_PLAGUE_SKILL_RECEIVED_{Interface.IconsPNG.ReceivedSkill}" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"BACKGROUND_PLAGUE_SKILL_AVAILABLE_{Interface.IconsPNG.AvailableSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}";
            CuiImageComponent Comp = Skill.SkillTurn ? String.IsNullOrWhiteSpace(Skill.GeneralSettings.Sprite) ? new CuiImageComponent { Png = GetImage($"SKILL_PATOGENARMORY_{Skill.GeneralSettings.PNG}") } : new CuiImageComponent { Sprite = Skill.GeneralSettings.Sprite } : new CuiImageComponent { Sprite = "assets/icons/lock.png" };
            string Command = Skill.SkillTurn ? UserSkills.PatogenAmrory ? "" : config.SkillSettings.ShowSkillNotDNK ? $"iqps study patogenarmory {Skill.GeneralSettings.PriceDNK} true" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"iqps study patogenarmory {Skill.GeneralSettings.PriceDNK} true" : "" : "";

            container.Add(new CuiElement
            {
                Parent = PLAGUE_PARENT_MAIN,
                Name = "SKILL_PATOGENARMORY",
                Components =
                    {
                        new CuiRawImageComponent {  Png = GetImage(SkillIcon) },
                        new CuiRectTransformComponent{ AnchorMin = "0.8755208 0.2861111", AnchorMax = "1 0.4861257"},
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "SKILL_PATOGENARMORY",
                Components =
                    {
                        Comp,
                        new CuiRectTransformComponent{ AnchorMin = "0.2499933 0.2260868", AnchorMax = "0.749979 0.782609"},
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = Command, Color = "0 0 0 0" },
                Text = { Text = "", Color = "0 0 0 0" }
            }, "SKILL_PATOGENARMORY");

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Patogen Kill
        void Interface_Skill_PatogenKill(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PLAGUE_PARENT_PANEL_INFORMATION);
            CuiHelper.DestroyUi(player, "SKILL_PATOGENKILL");
            var Interface = config.InterfaceSettings;
            var Skills = config.SkillSettings;
            var UserSkills = DataSkills[player.userID];
            var Balance = config.ReferenceSettings.IQEconomicUse ? IQEconomic ? GetBalanceUser(player.userID) : DataInformation[player.userID] : DataInformation[player.userID];

            var Skill = Skills.PatogenKillSettings;
            if (Skill.SkillTurn)
            {
                if (!UserSkills.PatogenAttack) return;
                
                string SkillIcon = UserSkills.PatogenAttack ? IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"BACKGROUND_PLAGUE_SKILL_AVAILABLE_{Interface.IconsPNG.AvailableSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}";
                CuiImageComponent Comp = String.IsNullOrWhiteSpace(Skill.GeneralSettings.Sprite) ? new CuiImageComponent { Png = GetImage($"SKILL_PATOGENKILL_{Skill.GeneralSettings.PNG}") } : new CuiImageComponent { Sprite = Skill.GeneralSettings.Sprite };
                string Command = UserSkills.PatogenAttack ? config.SkillSettings.ShowSkillNotDNK ? $"iqps study patogenkill {Skill.GeneralSettings.PriceDNK} true" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"iqps study patogenkill {Skill.GeneralSettings.PriceDNK} true" : "" : "";

                container.Add(new CuiElement
                {
                    Parent = PLAGUE_PARENT_MAIN,
                    Name = "SKILL_PATOGENKILL",
                    Components =
                    {
                        new CuiRawImageComponent {  Png = GetImage(SkillIcon) },
                        new CuiRectTransformComponent{ AnchorMin = "0.6380165 0.6805555", AnchorMax = "0.7234375 0.81661432"}, 
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = "SKILL_PATOGENKILL",
                    Components =
                    {
                        Comp,
                        new CuiRectTransformComponent{ AnchorMin = "0.2499933 0.2260868", AnchorMax = "0.749979 0.782609"},
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = Command, Color = "0 0 0 0" },
                    Text = { Text = "", Color = "0 0 0 0" }
                }, "SKILL_PATOGENKILL");

                CuiHelper.AddUi(player, container);
            }
        }
        #endregion

        #region Regeneration
        void Interface_Skill_Regeneration(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PLAGUE_PARENT_PANEL_INFORMATION);
            CuiHelper.DestroyUi(player, "SKILL_REGENERATION");

            var Interface = config.InterfaceSettings;
            var Skills = config.SkillSettings;
            var UserSkills = DataSkills[player.userID];
            var Balance = config.ReferenceSettings.IQEconomicUse ? IQEconomic ? GetBalanceUser(player.userID) : DataInformation[player.userID] : DataInformation[player.userID];

            var Skill = Skills.RegenerationSettings;
            
            string SkillIcon = Skill.SkillTurn ? UserSkills.Regeneration ? $"BACKGROUND_PLAGUE_SKILL_RECEIVED_{Interface.IconsPNG.ReceivedSkill}" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"BACKGROUND_PLAGUE_SKILL_AVAILABLE_{Interface.IconsPNG.AvailableSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}";
            CuiImageComponent Comp = Skill.SkillTurn ? String.IsNullOrWhiteSpace(Skill.GeneralSettings.Sprite) ? new CuiImageComponent { Png = GetImage($"SKILL_REGENERATION_{Skill.GeneralSettings.PNG}") } : new CuiImageComponent { Sprite = Skill.GeneralSettings.Sprite } : new CuiImageComponent { Sprite = "assets/icons/lock.png" };
            string Command = Skill.SkillTurn ? UserSkills.Regeneration ? "" : config.SkillSettings.ShowSkillNotDNK ? $"iqps study regeneration {Skill.GeneralSettings.PriceDNK} true" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"iqps study regeneration {Skill.GeneralSettings.PriceDNK} true" : "" : "";

            container.Add(new CuiElement
            {
                Parent = PLAGUE_PARENT_MAIN,
                Name = "SKILL_REGENERATION",
                Components =
                    {
                        new CuiRawImageComponent {  Png = GetImage(SkillIcon) },
                        new CuiRectTransformComponent{ AnchorMin = "0.7552022 0.903703", AnchorMax = "0.8151039 0.9953704"},
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "SKILL_REGENERATION",
                Components =
                    {
                        Comp,
                        new CuiRectTransformComponent{ AnchorMin = "0.2499933 0.2260868", AnchorMax = "0.749979 0.782609"},
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = Command, Color = "0 0 0 0" },
                Text = { Text = "", Color = "0 0 0 0" }
            }, "SKILL_REGENERATION");

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Thicks Skin
        void Interface_Skill_ThicksSkin(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PLAGUE_PARENT_PANEL_INFORMATION);
            CuiHelper.DestroyUi(player, "SKILL_THICKSSKIN");

            var Interface = config.InterfaceSettings;
            var Skills = config.SkillSettings;
            var UserSkills = DataSkills[player.userID];
            var Balance = config.ReferenceSettings.IQEconomicUse ? IQEconomic ? GetBalanceUser(player.userID) : DataInformation[player.userID] : DataInformation[player.userID];

            var Skill = Skills.ThickSkinSettings;
            
            string SkillIcon = Skill.SkillTurn ? UserSkills.ThickSkin ? $"BACKGROUND_PLAGUE_SKILL_RECEIVED_{Interface.IconsPNG.ReceivedSkill}" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"BACKGROUND_PLAGUE_SKILL_AVAILABLE_{Interface.IconsPNG.AvailableSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}";
            CuiImageComponent Comp = Skill.SkillTurn ? String.IsNullOrWhiteSpace(Skill.GeneralSettings.Sprite) ? new CuiImageComponent { Png = GetImage($"SKILL_THICKSSKIN_{Skill.GeneralSettings.PNG}") } : new CuiImageComponent { Sprite = Skill.GeneralSettings.Sprite } : new CuiImageComponent { Sprite = "assets/icons/lock.png" };
            string Command = Skill.SkillTurn ? UserSkills.ThickSkin ? "" : config.SkillSettings.ShowSkillNotDNK ? $"iqps study thicksskin {Skill.GeneralSettings.PriceDNK} true" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"iqps study thicksskin {Skill.GeneralSettings.PriceDNK} true" : "" : "";

            container.Add(new CuiElement
            {
                Parent = PLAGUE_PARENT_MAIN,
                Name = "SKILL_THICKSSKIN",
                Components =
                    {
                        new CuiRawImageComponent {  Png = GetImage(SkillIcon) },
                        new CuiRectTransformComponent{ AnchorMin = "0.8229095 0.688889", AnchorMax = "0.8947917 0.8037094"},
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "SKILL_THICKSSKIN",
                Components =
                    {
                        Comp,
                        new CuiRectTransformComponent{ AnchorMin = "0.2499933 0.2260868", AnchorMax = "0.749979 0.782609"},
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = Command, Color = "0 0 0 0" },
                Text = { Text = "", Color = "0 0 0 0" }
            }, "SKILL_THICKSSKIN");

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Wounded Shake
        void Interface_Skill_Wounded(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PLAGUE_PARENT_PANEL_INFORMATION);
            CuiHelper.DestroyUi(player, "SKILL_WOUNDED");

            var Interface = config.InterfaceSettings;
            var Skills = config.SkillSettings;
            var UserSkills = DataSkills[player.userID];
            var Balance = config.ReferenceSettings.IQEconomicUse ? IQEconomic ? GetBalanceUser(player.userID) : DataInformation[player.userID] : DataInformation[player.userID];

            var Skill = Skills.WoundedShakeSettings;
            
            string SkillIcon = Skill.SkillTurn ? UserSkills.WoundedShake ? $"BACKGROUND_PLAGUE_SKILL_RECEIVED_{Interface.IconsPNG.ReceivedSkill}" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"BACKGROUND_PLAGUE_SKILL_AVAILABLE_{Interface.IconsPNG.AvailableSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}" : $"BACKGROUND_PLAGUE_SKILL_BLOCK_{Interface.IconsPNG.BlockSkill}";
            CuiImageComponent Comp = Skill.SkillTurn ? String.IsNullOrWhiteSpace(Skill.GeneralSettings.Sprite) ? new CuiImageComponent { Png = GetImage($"SKILL_WOUNDED_{Skill.GeneralSettings.PNG}") } : new CuiImageComponent { Sprite = Skill.GeneralSettings.Sprite } : new CuiImageComponent { Sprite = "assets/icons/lock.png" };
            string Command = Skill.SkillTurn ? UserSkills.WoundedShake ? "" : config.SkillSettings.ShowSkillNotDNK ? $"iqps study wounded {Skill.GeneralSettings.PriceDNK} true" : IsAvailable(Balance, Skill.GeneralSettings.PriceDNK) ? $"iqps study wounded {Skill.GeneralSettings.PriceDNK} true" : "" : "";

            container.Add(new CuiElement
            {
                Parent = PLAGUE_PARENT_MAIN,
                Name = "SKILL_WOUNDED",
                Components =
                    {
                        new CuiRawImageComponent {  Png = GetImage(SkillIcon) },
                        new CuiRectTransformComponent{ AnchorMin = "0.2067708 0.523148", AnchorMax = "0.2885418 0.653703"},
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "SKILL_WOUNDED",
                Components =
                    {
                        Comp,
                        new CuiRectTransformComponent{ AnchorMin = "0.2499933 0.2260868", AnchorMax = "0.749979 0.782609"},
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = Command, Color = "0 0 0 0" },
                Text = { Text = "", Color = "0 0 0 0" }
            }, "SKILL_WOUNDED");

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region IQHeadReward
        void Interface_Skill_IQHeadReward(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PLAGUE_PARENT_PANEL_INFORMATION);
            CuiHelper.DestroyUi(player, "SKILL_IQHEAD_REWARD");

            var Interface = config.InterfaceSettings;
            var Skills = config.SkillSettings.NeutralSkills.SkillIQHeadRewards;
            var UserSkills = DataSkills[player.userID];
            var Balance = config.ReferenceSettings.IQEconomicUse ? IQEconomic ? GetBalanceUser(player.userID) : DataInformation[player.userID] : DataInformation[player.userID];
            string SkillIcon = Skills.SkillTurn ? UserSkills.IQHeadReward ? $"BACKGROUND_PLAGUE_NEUTRAL_SKILL_RECEIVED_{Interface.IconsPNG.NeutralReceivedSkill}" : IsAvailable(Balance, Skills.GeneralSettings.PriceDNK) ? $"BACKGROUND_PLAGUE_NEUTRAL_SKILL_AVAILABLE_{Interface.IconsPNG.NeutralAvailableSkill}" : $"BACKGROUND_PLAGUE_NEUTRAL_SKILL_BLOCK_{Interface.IconsPNG.NeutralBlockSkill}" : $"BACKGROUND_PLAGUE_NEUTRAL_SKILL_BLOCK_{Interface.IconsPNG.NeutralBlockSkill}";

            CuiImageComponent Comp = String.IsNullOrWhiteSpace(Skills.GeneralSettings.Sprite) ? new CuiImageComponent { Png = GetImage($"SKILL_IQHEAD_REWARD_{Skills.GeneralSettings.PNG}") } : new CuiImageComponent { Sprite = Skills.GeneralSettings.Sprite };
            string Command = UserSkills.IQHeadReward ? "" : config.SkillSettings.ShowSkillNotDNK ? $"iqps study iqheadreward {Skills.GeneralSettings.PriceDNK} true" : IsAvailable(Balance, Skills.GeneralSettings.PriceDNK) ? $"iqps study iqheadreward {Skills.GeneralSettings.PriceDNK} true" : "";

            container.Add(new CuiElement
            {
                Parent = PLAGUE_PARENT_MAIN,
                Name = "SKILL_IQHEAD_REWARD",
                Components =
                    {
                        new CuiRawImageComponent {  Png = GetImage(SkillIcon) },
                        new CuiRectTransformComponent{ AnchorMin = "0.4557309 0.118518", AnchorMax = "0.5421875 0.2592593"},
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "SKILL_IQHEAD_REWARD",
                Components =
                    {
                        Comp,
                        new CuiRectTransformComponent{ AnchorMin = "0.2499933 0.2260868", AnchorMax = "0.749979 0.782609"},
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = Command, Color = "0 0 0 0" },
                Text = { Text = "", Color = "0 0 0 0" }
            }, "SKILL_IQHEAD_REWARD");

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region IQCraftSystem
        void Interface_Skill_Advanced_IQCraftSystem(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PLAGUE_PARENT_PANEL_INFORMATION);
            CuiHelper.DestroyUi(player, "SKILL_IQCRAFTSYSTEM");

            var Interface = config.InterfaceSettings;
            var Skills = config.SkillSettings.NeutralSkills.IQCraftSystemAdvancedCrafts;
            var UserSkills = DataSkills[player.userID]; 
            var Balance = config.ReferenceSettings.IQEconomicUse ? IQEconomic ? GetBalanceUser(player.userID) : DataInformation[player.userID] : DataInformation[player.userID];
            string SkillIcon = Skills.SkillTurn ? UserSkills.IQCraftSystemAdvanced ? $"BACKGROUND_PLAGUE_NEUTRAL_SKILL_RECEIVED_{Interface.IconsPNG.NeutralReceivedSkill}" : IsAvailable(Balance, Skills.GeneralSettings.PriceDNK) ? $"BACKGROUND_PLAGUE_NEUTRAL_SKILL_AVAILABLE_{Interface.IconsPNG.NeutralAvailableSkill}" : $"BACKGROUND_PLAGUE_NEUTRAL_SKILL_BLOCK_{Interface.IconsPNG.NeutralBlockSkill}" : $"BACKGROUND_PLAGUE_NEUTRAL_SKILL_BLOCK_{Interface.IconsPNG.NeutralBlockSkill}";
            CuiImageComponent Comp = String.IsNullOrWhiteSpace(Skills.GeneralSettings.Sprite) ? new CuiImageComponent { Png = GetImage($"SKILL_IQCRAFTSYSTEM_{Skills.GeneralSettings.PNG}") } : new CuiImageComponent { Sprite = Skills.GeneralSettings.Sprite };
            string Command = UserSkills.IQCraftSystemAdvanced ? "" : config.SkillSettings.ShowSkillNotDNK ? $"iqps study iqcraftsystem.advanced {Skills.GeneralSettings.PriceDNK} true" : IsAvailable(Balance, Skills.GeneralSettings.PriceDNK) ? $"iqps study iqcraftsystem.advanced {Skills.GeneralSettings.PriceDNK} true" : "";

            container.Add(new CuiElement
            {
                Parent = PLAGUE_PARENT_MAIN,
                Name = "SKILL_IQCRAFTSYSTEM",
                Components =
                    {
                        new CuiRawImageComponent {  Png = GetImage(SkillIcon) },
                        new CuiRectTransformComponent{ AnchorMin = "0.4906229 0.8444452", AnchorMax = "0.5723902 0.9722228"},
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "SKILL_IQCRAFTSYSTEM",
                Components =
                    {
                        Comp,
                        new CuiRectTransformComponent{ AnchorMin = "0.2499933 0.2260868", AnchorMax = "0.749979 0.782609"},
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = Command, Color = "0 0 0 0" },
                Text = { Text = "", Color = "0 0 0 0" }
            }, "SKILL_IQCRAFTSYSTEM");

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region IQKits
        void Interface_Skill_DropPercent_IQKits(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PLAGUE_PARENT_PANEL_INFORMATION);
            CuiHelper.DestroyUi(player, "SKILL_IQKITS_PERCENT");

            var Interface = config.InterfaceSettings;
            var Skills = config.SkillSettings.NeutralSkills.IQKitsCooldown;
            var UserSkills = DataSkills[player.userID];
            var Balance = config.ReferenceSettings.IQEconomicUse ? IQEconomic ? GetBalanceUser(player.userID) : DataInformation[player.userID] : DataInformation[player.userID];
            string SkillIcon = Skills.SkillTurn ? UserSkills.IQKitsCooldownPercent ? $"BACKGROUND_PLAGUE_NEUTRAL_SKILL_RECEIVED_{Interface.IconsPNG.NeutralReceivedSkill}" : IsAvailable(Balance, Skills.GeneralSettings.PriceDNK) ? $"BACKGROUND_PLAGUE_NEUTRAL_SKILL_AVAILABLE_{Interface.IconsPNG.NeutralAvailableSkill}" : $"BACKGROUND_PLAGUE_NEUTRAL_SKILL_BLOCK_{Interface.IconsPNG.NeutralBlockSkill}" : $"BACKGROUND_PLAGUE_NEUTRAL_SKILL_BLOCK_{Interface.IconsPNG.NeutralBlockSkill}";
            CuiImageComponent Comp = String.IsNullOrWhiteSpace(Skills.GeneralSettings.Sprite) ? new CuiImageComponent { Png = GetImage($"SKILL_IQCRAFTSYSTEM_{Skills.GeneralSettings.PNG}") } : new CuiImageComponent { Sprite = Skills.GeneralSettings.Sprite };
            string Command = UserSkills.IQKitsCooldownPercent ? "" : config.SkillSettings.ShowSkillNotDNK ? $"iqps study iqkits.cooldown {Skills.GeneralSettings.PriceDNK} true" : IsAvailable(Balance, Skills.GeneralSettings.PriceDNK) ? $"iqps study iqkits.cooldown {Skills.GeneralSettings.PriceDNK} true" : "";

            container.Add(new CuiElement
            {
                Parent = PLAGUE_PARENT_MAIN,
                Name = "SKILL_IQKITS_PERCENT",
                Components =
                    {
                        new CuiRawImageComponent {  Png = GetImage(SkillIcon) },
                        new CuiRectTransformComponent{ AnchorMin = "0.7119737 0.1592593", AnchorMax = "0.7744792 0.2574214"},
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "SKILL_IQKITS_PERCENT",
                Components =
                    {
                        Comp,
                        new CuiRectTransformComponent{ AnchorMin = "0.2499933 0.2260868", AnchorMax = "0.749979 0.782609"},
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = Command, Color = "0 0 0 0" },
                Text = { Text = "", Color = "0 0 0 0" }
            }, "SKILL_IQKITS_PERCENT");

            CuiHelper.AddUi(player, container);
        }

        void Interface_Skill_Rare_IQKits(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PLAGUE_PARENT_PANEL_INFORMATION);
            CuiHelper.DestroyUi(player, "SKILL_IQKITS_RARE");

            var Interface = config.InterfaceSettings;
            var Skills = config.SkillSettings.NeutralSkills.IQKitsRare;
            var UserSkills = DataSkills[player.userID];
            var Balance = config.ReferenceSettings.IQEconomicUse ? IQEconomic ? GetBalanceUser(player.userID) : DataInformation[player.userID] : DataInformation[player.userID];
            string SkillIcon = Skills.SkillTurn ? UserSkills.IQKitsRareup ? $"BACKGROUND_PLAGUE_NEUTRAL_SKILL_RECEIVED_{Interface.IconsPNG.NeutralReceivedSkill}" : IsAvailable(Balance, Skills.GeneralSettings.PriceDNK) ? $"BACKGROUND_PLAGUE_NEUTRAL_SKILL_AVAILABLE_{Interface.IconsPNG.NeutralAvailableSkill}" : $"BACKGROUND_PLAGUE_NEUTRAL_SKILL_BLOCK_{Interface.IconsPNG.NeutralBlockSkill}" : $"BACKGROUND_PLAGUE_NEUTRAL_SKILL_BLOCK_{Interface.IconsPNG.NeutralBlockSkill}";
            CuiImageComponent Comp = String.IsNullOrWhiteSpace(Skills.GeneralSettings.Sprite) ? new CuiImageComponent { Png = GetImage($"SKILL_IQCRAFTSYSTEM_{Skills.GeneralSettings.PNG}") } : new CuiImageComponent { Sprite = Skills.GeneralSettings.Sprite };
            string Command = UserSkills.IQKitsRareup ? "" : config.SkillSettings.ShowSkillNotDNK ? $"iqps study iqkits.rare {Skills.GeneralSettings.PriceDNK} true" : IsAvailable(Balance, Skills.GeneralSettings.PriceDNK) ? $"iqps study iqkits.rare {Skills.GeneralSettings.PriceDNK} true" : "";

            container.Add(new CuiElement
            {
                Parent = PLAGUE_PARENT_MAIN,
                Name = "SKILL_IQKITS_RARE",
                Components =
                    {
                        new CuiRawImageComponent {  Png = GetImage(SkillIcon) },
                        new CuiRectTransformComponent{ AnchorMin = "0.7807226 0.3379634", AnchorMax = "0.8432281 0.4361256"},
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "SKILL_IQKITS_RARE",
                Components =
                    {
                        Comp,
                        new CuiRectTransformComponent{ AnchorMin = "0.2299933 0.2260868", AnchorMax = "0.729979 0.782609"},
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = Command, Color = "0 0 0 0" },
                Text = { Text = "", Color = "0 0 0 0" }
            }, "SKILL_IQKITS_RARE");

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #endregion

        #region Balance UI
        void Interface_Balance(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "BALANCE");
            var Reference = config.ReferenceSettings;
            int Balance = Reference.IQEconomicUse ? IQEconomic ? GetBalanceUser(player.userID) : DataInformation[player.userID] : DataInformation[player.userID];
            string LangBalance = Reference.IQEconomicUse ? IQEconomic ? "SHOW_DNK_ECONOMIC" : "SHOW_DNK" : "SHOW_DNK";
            var Interface = config.InterfaceSettings;

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.6354166 0.0601852", AnchorMax = "0.8682292 0.1157407" },
                Text = { Text = String.Format(lang.GetMessage(LangBalance, this,player.UserIDString), Balance), Color = HexToRustFormat(Interface.GeneralSettings.HexLabelTakePanel), Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
            },  PLAGUE_PARENT_MAIN, "BALANCE");

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Show Information

        void Interface_Show_Information(BasePlayer player, Configuration.Skills.GeneralSettingsSkill Information, string Skill, TypeSkill typeSkill)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PLAGUE_PARENT_PANEL_INFORMATION);
            var Interface = config.InterfaceSettings;
            var Balance = IQEconomic ? config.ReferenceSettings.IQEconomicUse ? GetBalanceUser(player.userID) : DataInformation[player.userID] : DataInformation[player.userID];
            string LogoTypeSkill = typeSkill == TypeSkill.Active ? $"BACKGROUND_PLAGUE_SKILL_AVAILABLE_{Interface.IconsPNG.AvailableSkill}" : $"BACKGROUND_PLAGUE_NEUTRAL_SKILL_AVAILABLE_{Interface.IconsPNG.NeutralAvailableSkill}";
            string LabelTypeSkill = typeSkill == TypeSkill.Active ? "<b>A</b>" : "<b>N</b>";
            string CMD = $"iqps study {Skill} {Information.PriceDNK} false";

            container.Add(new CuiElement
            {
                Parent = PLAGUE_PARENT_MAIN,
                Name = PLAGUE_PARENT_PANEL_INFORMATION,
                Components =
                    {
                        new CuiRawImageComponent {  Png = GetImage($"BACKGROUND_PLAGUE_TAKE_PANEL_{Interface.IconsPNG.BackgroundTakePanel}")},
                        new CuiRectTransformComponent{ AnchorMin = "0.3333333 0.3935185", AnchorMax = "0.65 0.6453695"},
                    }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.06085521 0.7536785", AnchorMax = "0.7664474 0.9264706"},
                Text = { Text = $"<size=25><b>{Information.DisplayName}</b></size>", Color = HexToRustFormat(Interface.GeneralSettings.HexLabelTakePanel), Font = "robotocondensed-regular.ttf", Align = TextAnchor.UpperLeft }
            },  PLAGUE_PARENT_PANEL_INFORMATION);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.06085521 0.1029415", AnchorMax = "0.7664474 0.7499998" },
                Text = { Text = $"<size=15><b>{Information.Description}</b></size>", Color = HexToRustFormat(Interface.GeneralSettings.HexLabelTakePanel), Font = "robotocondensed-regular.ttf", Align = TextAnchor.UpperLeft }
            },  PLAGUE_PARENT_PANEL_INFORMATION);

            container.Add(new CuiElement
            {
                Parent = PLAGUE_PARENT_PANEL_INFORMATION,
                Name = "CIRCLE",
                Components =
                    {
                         new CuiImageComponent { Sprite = "assets/icons/circle_open.png" },
                        new CuiRectTransformComponent{ AnchorMin = "0.8355262 0.7022083", AnchorMax = "0.9539473 0.9669153"},
                    }
            });

            container.Add(new CuiElement
            {
                Parent = PLAGUE_PARENT_PANEL_INFORMATION,
                Name = "TYPE_SKILL",
                Components =
                    {
                         new CuiRawImageComponent { Png = GetImage(LogoTypeSkill) },
                        new CuiRectTransformComponent{ AnchorMin = "0.8355262 0.4191192", AnchorMax = "0.9539473 0.6580915"},
                    }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = LabelTypeSkill, Color = HexToRustFormat(Interface.GeneralSettings.HexLabelTakePanel), Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
            }, "TYPE_SKILL", "TYPE");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = $"<size=25><b>{Information.PriceDNK}</b></size>", Color = HexToRustFormat(Interface.GeneralSettings.HexLabelTakePanel), Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
            }, "CIRCLE");

            if (Balance >= Information.PriceDNK)
            {
                container.Add(new CuiElement
                {
                    Parent = PLAGUE_PARENT_PANEL_INFORMATION,
                    Name = "BTN_INFO",
                    Components =
                    {
                        new CuiRawImageComponent { Png = GetImage($"BACKGROUND_PLAGUE_TAKE_BUTTON_{Interface.IconsPNG.ButtonTakeSkill}") },
                        new CuiRectTransformComponent{ AnchorMin = "0.7055923 0.08088271", AnchorMax = "0.9424341 0.2132361" },
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = CMD, Color = "0 0 0 0" },
                    Text = { Text = "", Color = "0 0 0 0" }
                }, "BTN_INFO");
            }

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "-30 0", OffsetMax = "-2 182" },
                Button = { Close = PLAGUE_PARENT_PANEL_INFORMATION, Color = HexToRustFormat("#1B0103E1") },
                Text = { Text = lang.GetMessage("EXIT_TAKE_PANEL",this,player.UserIDString), Color = HexToRustFormat("#FFFFFFFF"), Align = TextAnchor.MiddleCenter }
            },  PLAGUE_PARENT_PANEL_INFORMATION);

            CuiHelper.AddUi(player, container);
        }

        #endregion

        private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
        #endregion

        #region Lang
        private new void LoadDefaultMessages()
        {
            PrintWarning(LanguageEn ? "Language file is loading..." : "Языковой файл загружается...");
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["EXIT_BTN"] = "<size=30><b>LEAVE GENESISE</b></size>",
                ["SHOW_DNK"] = "<size=20><b>SCORE DNK : {0}</b></size>",
                ["SHOW_DNK_ECONOMIC"] = "<size=20><b>Balance : {0}</b></size>",
                ["EXIT_TAKE_PANEL"] = "<size=20><b>C\nL\nO\nS\nE</b></size>",

                ["VIRUS_PATOGEN_TITLE"] = "<size=18><b>ATTENTION PATHOGEN</b></size>",
                ["VIRUS_PATOGEN_DESCRIPTION"] = "<size=10>Pathogen virus detected in your genetic compound\nThe virus will destroy your learned skills over time\nYou have the skill to destroy the pathogen, be careful</size>",

                ["CHAT_VIRUS_PATOGEN_INFECTED"] = "You have become infected with the PATHOGEN virus. Your skills will be destroyed by him over time, you can recover by getting a certain skill",
                ["CHAT_VIRUS_PATOGEN_RECOVERED"] = "You are cured of the PATHOGEN virus, congratulations",
                ["CHAT_VIRUS_PATOGEN_RETURNED"] = "The PATHOGEN virus has bypassed you due to your ability to protect against the virus",

                ["CHAT_TAKE_DNK"] = "You discovered the DNK : +{0}",
                ["CHAT_SKILL_WOUNDED"] = "{0} successfully worked! You were able to recover",

                ["STUDY_ALL_YES"] = "You have successfully received a full set of skills!",

                ["REFERENCE_IQHEADREWARD"] = "Your skill has protected you from ordering for your head!",
                ["NEUTRAL_HELP_INFO"] = "This label means that the skill is a neutral skill",
                ["ACTIVE_HELP_INFO"] = "This label means that the skill is an active skill",

            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["EXIT_BTN"] = "<size=30><b>ПОКИНУТЬ ГЕНЕЗИС</b></size>",
                ["SHOW_DNK"] = "<size=20><b>СОЕДИНЕНИЯ ДНК : {0}</b></size>",
                ["SHOW_DNK_ECONOMIC"] = "<size=20><b>БАЛАНС : {0}</b></size>",
                ["EXIT_TAKE_PANEL"] = "<size=20><b>З\nА\nК\nР\nЫ\nТ\nЬ</b></size>",

                ["VIRUS_PATOGEN_TITLE"] = "<size=18><b>ВНИМАНИЕ ПАТОГЕН</b></size>",
                ["VIRUS_PATOGEN_DESCRIPTION"] = "<size=10>В вашем генетическом соединение был обнаружен вирус-патоген\nВирус со временем будет разрушать ваши изученные навыки\nВам доступен навык на уничтожение патогена,будьте бдительны</size>",

                ["CHAT_VIRUS_PATOGEN_INFECTED"] = "Вы заразились вирусом ПАТОГЕН. Ваши навыки со временем будут уничтожаться им,вы можете вылечиться получив определенный навык",
                ["CHAT_VIRUS_PATOGEN_RECOVERED"] = "Вы излечились от вируса ПАТОГЕН, поздравляем",
                ["CHAT_VIRUS_PATOGEN_RETURNED"] = "Вирус ПАТОГЕН обошел вас стороной благодаря вашему навыку на защиту от вируса",

                ["CHAT_TAKE_DNK"] = "Вы обнаружили ДНК : +{0}",

                ["CHAT_SKILL_WOUNDED"] = "{0} успешно сработал! Вы смогли придти в себя",

                ["STUDY_ALL_YES"] = "Вы успешно получили полный набор умений!",

                ["REFERENCE_IQHEADREWARD"] = "Ваш навык защитил вас от заказа за вашу голову!",

                ["NEUTRAL_HELP_INFO"] = "Данная метка означает,что навык относится к нейтральному навыку",
                ["ACTIVE_HELP_INFO"] = "Данная метка означает,что навык относится к активному навыку",
            }, this, "ru");

            PrintWarning(LanguageEn ? "Language file uploaded successfully" : "Языковой файл загружен успешно");
        }
        #endregion

        #region API

        bool API_HEAD_REWARD_SKILL(BasePlayer player)
        {
            if (IQHeadReward)
            {
                if (!config.ReferenceSettings.IQHeadRewardUse)
                    return false;

                if (DataSkills[player.userID].IQHeadReward)
                {
                    SendChat(player, lang.GetMessage("REFERENCE_IQHEADREWARD", this, player.UserIDString));
                    DataSkills[player.userID].IQHeadReward = false;
                    return true;
                }
                else return false;
            }
            else
            {
                PrintWarning(LanguageEn ? "Plugin not found IQHEADREWARD" : "Не найден плагин IQHEADREWARD");
                return false;
            }
        }
        bool API_IS_ALL_STUDY(ulong player)
        {
            if (!DataSkills.ContainsKey(player)) return false;
            Int32 SkillStudy = 0;
            Int32 SkillNotStudy = 0;

            var Skill = config.SkillSettings;
            var Data = DataSkills[player];
            if (config.ReferenceSettings.IQKits && IQKits)
            {
                if (Skill.NeutralSkills.IQKitsRare.SkillTurn)
                    if (Data.IQKitsRareup)
                        SkillStudy++;
                    else SkillNotStudy++;

                if (Skill.NeutralSkills.IQKitsCooldown.SkillTurn)
                    if (Data.IQKitsCooldownPercent)
                        SkillStudy++;
                    else SkillNotStudy++;
            }

            if (config.ReferenceSettings.IQCraftSystem && IQCraftSystem)
            {
                if (Skill.NeutralSkills.IQCraftSystemAdvancedCrafts.SkillTurn)
                    if (Data.IQCraftSystemAdvanced)
                        SkillStudy++;
                    else SkillNotStudy++;
            }

            if (config.ReferenceSettings.IQHeadRewardUse && IQHeadReward)
            {
                if (Skill.NeutralSkills.SkillIQHeadRewards.SkillTurn)
                    if (Data.IQHeadReward)
                        SkillStudy++;
                    else SkillNotStudy++;
            }

            if(Skill.AnabioticsSettings.SkillTurn)
            {
                if(Data.Anabiotics)
                    SkillStudy++;
                else SkillNotStudy++;
            }

            if (Skill.AnimalFriendsSettings.SkillTurn)
            {
                if (Data.AnimalFriends)
                    SkillStudy++;
                else SkillNotStudy++;
            }

            if (Skill.CrafterSettings.SkillTurn)
            {
                if (Data.Crafter)
                    SkillStudy++;
                else SkillNotStudy++;
            }

            if (Skill.GatherFriendsSettings.SkillTurn)
            {
                if (Data.GatherFriends)
                    SkillStudy++;
                else SkillNotStudy++;
            }

            if (Skill.GenesisGensSettings.SkillTurn)
            {
                if (Data.GenesisGens)
                    SkillStudy++;
                else SkillNotStudy++;
            }

            if (Skill.MetabolismSettings.SkillTurn)
            {
                if (Data.Metabolism)
                    SkillStudy++;
                else SkillNotStudy++;
            }

            if (Skill.MilitarySettings.SkillTurn)
            {
                if (Data.Military)
                    SkillStudy++;
                else SkillNotStudy++;
            }

            if (Skill.MinerSettings.SkillTurn)
            {
                if (Data.Miner)
                    SkillStudy++;
                else SkillNotStudy++;
            }

            if (Skill.PatogenAmrorySettings.SkillTurn)
            {
                if (Data.PatogenAmrory)
                    SkillStudy++;
                else SkillNotStudy++;
            }

            if (Skill.RegenerationSettings.SkillTurn)
            {
                if (Data.Regeneration)
                    SkillStudy++;
                else SkillNotStudy++;
            }

            if (Skill.ThickSkinSettings.SkillTurn)
            {
                if (Data.ThickSkin)
                    SkillStudy++;
                else SkillNotStudy++;
            }

            if (Skill.WoundedShakeSettings.SkillTurn)
            {
                if (Data.WoundedShake)
                    SkillStudy++;
                else SkillNotStudy++;
            }

            return SkillStudy >= SkillNotStudy;
        }
        bool API_IS_RARE_SKILL_KITS(BasePlayer player)
        {
            if (!config.ReferenceSettings.IQKits) return false;
            if (!IQKits) return false;
            if (!DataSkills.ContainsKey(player.userID)) return false;
            return DataSkills[player.userID].IQKitsRareup;
        }
        bool API_IS_COOLDOWN_SKILL_KITS(BasePlayer player)
        {
            if (!config.ReferenceSettings.IQKits) return false;
            if (!IQKits) return false;
            if (!DataSkills.ContainsKey(player.userID)) return false;
            return DataSkills[player.userID].IQKitsCooldownPercent;
        }
        bool API_IS_ADVANCED_CRAFT(BasePlayer player)
        {
            if (!config.ReferenceSettings.IQCraftSystem) return false;
            if (!IQCraftSystem) return false;
            if (!DataSkills.ContainsKey(player.userID)) return false;
            return DataSkills[player.userID].IQCraftSystemAdvanced;
        }
        int API_GET_RARE_IQKITS() => config.SkillSettings.NeutralSkills.IQKitsRare.RareUP;
        int API_GET_COOLDOWN_IQKITS() => config.SkillSettings.NeutralSkills.IQKitsCooldown.PercentDrop;
        #endregion
    }
}
