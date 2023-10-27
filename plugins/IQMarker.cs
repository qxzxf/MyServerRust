using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("IQMarker", "qxzxf", "1.1.5")]
    [Description("Маркеры для ваших любимчиков")]
    class IQMarker : RustPlugin
    {
        #region Reference
        [PluginReference] Plugin ImageLibrary, Friends;
        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin);
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        public void SendImage(BasePlayer player, string imageName, ulong imageId = 0) => ImageLibrary?.Call("SendImage", player, imageName, imageId);
        public bool HasImage(string imageName) => (bool)ImageLibrary?.Call("HasImage", imageName);
        public bool IsFriends(ulong TargetID, ulong PlayerID)
        {
             if(Friends == null) return false;
            if ((bool)Friends.Call("HasFriend", PlayerID, TargetID)) return true;
            else return false;
        }
        public void LoadedImage()
        {
            var Icon = config.MarkesSettings.IconSetting.IconList;
            for(int i = 0; i < Icon.Count; i++)
            {
                if (!HasImage($"ICON_{Icon[i].PNG}_{i}"))
                    AddImage(Icon[i].PNG, $"ICON_{Icon[i].PNG}_{i}");
            }
        }
        public void CachedImage(BasePlayer player)
        {
            var Icon = config.MarkesSettings.IconSetting.IconList;
            for (int i = 0; i < Icon.Count; i++)
                SendImage(player, $"ICON_{Icon[i].PNG}_{i}");
        }
        #endregion

        #region Vars
        public static string PermissionUseMarker = "iqmarker.use";
        public static string PermissionUseColorList = "iqmarker.usecolorlist";
        public static string PermissionUseCustomColor = "iqmarker.usecustomcolor";
        public static string PermissionUseSizer = "iqmarker.usesize";
        public void RegisteredPermissions()
        {
            var ColorList = config.AccessSettings.ColorListMarker;
            var Markers = config.MarkesSettings;
            var HealtBar = Markers.HealthBarSetting;
            var DamageText = Markers.DamageTextSetting;
            var Icon = Markers.IconSetting;

            Register(PermissionUseMarker);
            Register(PermissionUseColorList);
            Register(PermissionUseCustomColor);
            Register(PermissionUseSizer);
            Register(HealtBar.PermissionsHealthBar);
            Register(HealtBar.PermissionsHealtBarDamageText);
            Register(HealtBar.PermissionsHealtBarWounded);
            Register(DamageText.PermissionsDamageText);
            Register(DamageText.PermissionsDamageTextWounded);
            Register(Icon.PermissionsIcon);
            Register(Icon.PermissionsIconWounded);
            Register(Icon.PermissionsIconDamageText);

            for (int i = 0; i < Icon.IconList.Count; i++)
                Register(Icon.IconList[i].Permissions);

            for (int i = 0; i < ColorList.Count; i++)
                Register(ColorList[i].Permissions);
        }
        public void ReturnSettingsMore(BasePlayer player)
        {
            if (!DataInformation.ContainsKey(player.userID)) return;
            var Data = DataInformation[player.userID];
            var HealthBar = Data.HealthBarMore;
            var DamageText = Data.DamageTextMore;
            var Icon = Data.IconMore;

            var Markers = config.MarkesSettings;
            var HealtBarCfg = Markers.HealthBarSetting;
            var DamageTextCfg = Markers.DamageTextSetting;
            var IconCfg = Markers.IconSetting;

            if (HealthBar.TurnDamageText)
                if (!permission.UserHasPermission(player.UserIDString, HealtBarCfg.PermissionsHealtBarDamageText))
                    HealthBar.TurnDamageText = false;

            if (HealthBar.TurnWounded)
                if (!permission.UserHasPermission(player.UserIDString, HealtBarCfg.PermissionsHealtBarWounded))
                    HealthBar.TurnWounded = false;

            if (DamageText.TurnWounded)
                if (!permission.UserHasPermission(player.UserIDString, DamageTextCfg.PermissionsDamageTextWounded))
                    DamageText.TurnWounded = false;

            if (Icon.TurnDamageText)
                if (!permission.UserHasPermission(player.UserIDString, IconCfg.PermissionsIconDamageText))
                    Icon.TurnDamageText = false;

            if (Icon.TurnWounded)
                if (!permission.UserHasPermission(player.UserIDString, IconCfg.PermissionsIconWounded))
                    Icon.TurnWounded = false;
        }
        public void Register(string Permissions)
        {
            if (!String.IsNullOrWhiteSpace(Permissions))
                if (!permission.PermissionExists(Permissions, this))
                    permission.RegisterPermission(Permissions, this);
        }
        #endregion

        #region Configuration 
        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Настройка плагина")]
            public AccessSetting AccessSettings = new AccessSetting();
            [JsonProperty("Настройка интерфейса плагина")]
            public SettingsInterface SettingsInterfaces = new SettingsInterface();
            [JsonProperty("Настройка маркеров")]
            public MarkersSetting MarkesSettings = new MarkersSetting();
            [JsonProperty("Настройка для новых пользователей(Будьте внимательны,эти настройки даются по умолчанию вне зависимости от прав,если игрок их выключит и у него не будет прав, они ему потребуются для повторного включения)")]
            public UserSettings UserSetting = new UserSettings();
            internal class AccessSetting
            {
                [JsonProperty("true - включить настройку маркеров/false - отключить настройку")]
                public bool UseSettingsMarker;
                [JsonProperty("Настройка доступных цветов для маркера")]
                public List<ColorList> ColorListMarker = new List<ColorList>();
                internal class ColorList
                {
                    [JsonProperty("Права доступа для этого цвета")]
                    public string Permissions;
                    [JsonProperty("HEX цвет")]
                    public string HexColor;
                }
            }
            internal class UserSettings
            {
                [JsonProperty("Включен ли маркер для новых игроков?(true - да/false - нет)")]
                public bool DefaultTurnOnMarker;
                [JsonProperty("Стандартный цвет маркера для новых игроков(HEX)")]
                public string DefaultHexMarker;
                [JsonProperty("Стандартный вид маркера для новых игркоов (0 - Текст с дамагом | 1 - Полоса ХП | 2 - Иконка)")]
                public MarkerType DefaultMarkerType;
                [JsonProperty("Стандартный размер маркера для новых игркоов (0 - Маленький | 1 - Средний | 2 - Большой)")]
                public TypeSize DefaultMarkerSize;

                [JsonProperty("Дополнительные настройки полосы ХП для новых игрков")]
                public HealthBar DefaultHealthBar = new HealthBar();
                [JsonProperty("Дополнительные настройки текста с уроном для новых игрков")]
                public DamageText DefaultDamageText = new DamageText();
                [JsonProperty("Дополнительные настройки иконки для новых игрков")]
                public Icon DefaultIcon = new Icon();
                internal class HealthBar
                {
                    [JsonProperty("Включить отображение текста с дамагом(true - да/false - нет)")]
                    public bool TurnDamageText;
                    [JsonProperty("Включить отображение иконки падения(true - да/false - нет)")]
                    public bool TurnWounded;
                }
                internal class DamageText
                {
                    [JsonProperty("Включить отображение текста с дамагом(true - да /false - нет)")]
                    public bool TurnWounded;
                }
                internal class Icon
                {
                    [JsonProperty("Включить отображение текста с дамагом(true - да / false - нет)")]
                    public bool TurnDamageText;
                    [JsonProperty("Включить отображение иконки падения(true - да/false - нет)")]
                    public bool TurnWounded;
                }
            }
            internal class MarkersSetting
            {
                [JsonProperty("Настройка маркера HealthBar")]
                public HealthBarSettings HealthBarSetting = new HealthBarSettings();
                [JsonProperty("Настройка маркера DamageText")]
                public DamageText DamageTextSetting = new DamageText();
                [JsonProperty("Настройка маркера Icon")]
                public Icon IconSetting = new Icon();

                internal class HealthBarSettings
                {
                    [JsonProperty("Общая настройка")]
                    public GeneralSettings GeneralSetting = new GeneralSettings();
                    [JsonProperty("Пермишенс для доступа к типу маркера HealthBar")]
                    public string PermissionsHealthBar;
                    [JsonProperty("Пермишенс для доступа дополнению HealthBar , отображение урона рядом с показателем")]
                    public string PermissionsHealtBarDamageText;
                    [JsonProperty("Пермишенс для доступа дополнению HealthBar , отображение иконки с падением игрока")]
                    public string PermissionsHealtBarWounded;
                }
                internal class DamageText
                {
                    [JsonProperty("Общая настройка")]
                    public GeneralSettings GeneralSetting = new GeneralSettings();
                    [JsonProperty("Пермишенс для доступа к типу маркера DamageText")]
                    public string PermissionsDamageText;
                    [JsonProperty("Пермишенс для доступа дополнению DamageText , отображение иконки с падением игрока")]
                    public string PermissionsDamageTextWounded;
                }
                internal class Icon
                {
                    [JsonProperty("Общая настройка")]
                    public GeneralSettings GeneralSetting = new GeneralSettings();
                    [JsonProperty("Пермишенс для доступа к типу маркера Icon")]
                    public string PermissionsIcon;
                    [JsonProperty("Пермишенс для доступа дополнению Icon , отображение урона рядом с показателем")]
                    public string PermissionsIconDamageText;
                    [JsonProperty("Пермишенс для доступа дополнению HealthBar , отображение иконки с падением игрока")]
                    public string PermissionsIconWounded;
                    [JsonProperty("Список иконок(Название - ссылка на иконку 64х64)")]
                    public List<IconListClass> IconList = new List<IconListClass>();
                    internal class IconListClass
                    {
                        public string Permissions;
                        public string PNG;
                    }
                }
                internal class GeneralSettings
                {
                    [JsonProperty("Отображаемое имя")]
                    public string DisplayName;
                    [JsonProperty("Описание")]
                    public string Description;
                }
            }
            internal class SettingsInterface
            {
                [JsonProperty("Настройка основнвой панели")]
                public MainPanel InterfaceMainPanel = new MainPanel(); 
                [JsonProperty("Настройка панели с настройкой маркера")]
                public SettingsMarker InterfaceSettingsMarker = new SettingsMarker();

                [JsonProperty("HEX цвета текста")]
                public string HexLabels;
                internal class MainPanel
                {
                    [JsonProperty("HEX заднего фона")]
                    public string HexBackground;
                    [JsonProperty("Material заднего фона")]
                    public string MaterialBackground;
                    [JsonProperty("Sprite логотипа")]
                    public string SpriteLogoBackground;
                }
                internal class SettingsMarker
                {
                    [JsonProperty("HEX кнопки настройки")]
                    public string HexButtonMain;    
                    [JsonProperty("HEX поля ввода для настройки цвета маркера")]
                    public string HexButtonInputPanel;
                    [JsonProperty("HEX выбранного элемента отображения маркера")]
                    public string HexActiveShowMarker;
                    [JsonProperty("HEX не выбранного элемента отображения маркера")]
                    public string HexInActiveShowMarker;
                    [JsonProperty("Sprite не выбранного элемента отображения маркера")]
                    public string SpriteActiveShowMarker;
                    [JsonProperty("Sprite выбранного элемента отображения маркера")]
                    public string SpriteInActiveShowMarker;
                    [JsonProperty("HEX панели с типом маркера")]
                    public string HexTypeMarkerPanel;
                    [JsonProperty("HEX кнопки с доступным типом маркера")]
                    public string HexTypeMarkerAcces;
                    [JsonProperty("HEX кнопки с недоступным типом маркера")]
                    public string HexTypeMarkerDeading;
                    [JsonProperty("HEX кнопки с выбранным типом маркера")]
                    public string HexTypeMarkerTaking;
                }
            }
            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    #region AccessSettings
                    AccessSettings = new AccessSetting
                    {
                        UseSettingsMarker = true,
                        ColorListMarker = new List<AccessSetting.ColorList>
                        {
                            new AccessSetting.ColorList
                            {
                                HexColor = "#FF6666",
                                Permissions = "iqmarker.red"
                            },
                            new AccessSetting.ColorList
                            {
                                HexColor = "#FF3366",
                                Permissions = "iqmarker.default"
                            },
                            new AccessSetting.ColorList
                            {
                                HexColor = "#FF33CC",
                                Permissions = "iqmarker.default"
                            },
                            new AccessSetting.ColorList
                            {
                                HexColor = "#FF66FF",
                                Permissions = "iqmarker.purple"
                            },               
                            new AccessSetting.ColorList
                            {
                                HexColor = "#9900FF",
                                Permissions = "iqmarker.purple"
                            },
                            new AccessSetting.ColorList
                            {
                                HexColor = "#00CCFF",
                                Permissions = "iqmarker.aqua"
                            },
                            new AccessSetting.ColorList
                            {
                                HexColor = "#33CC66",
                                Permissions = "iqmarker.green"
                            },
                            new AccessSetting.ColorList
                            {
                                HexColor = "#FF9933",
                                Permissions = "iqmarker.default"
                            },
                        }
                    },
                    #endregion

                    #region Markers
                    MarkesSettings = new MarkersSetting
                    {
                        HealthBarSetting = new MarkersSetting.HealthBarSettings
                        {
                            PermissionsHealthBar = "iqmarker.healthbar",
                            PermissionsHealtBarWounded = "iqmarker.healthbarwound",
                            PermissionsHealtBarDamageText = "iqmarker.damagetext",
                            GeneralSetting = new MarkersSetting.GeneralSettings
                            {
                                DisplayName = "<size=16><b>ИНДИКАТОР ЗДОРОВЬЯ</b></size>",
                                Description = "<size=11>ПРИ ПОПАДАНИИ БУДЕТ ПОЯВЛЯТЬСЯ ИНДИКАТОР ЗДОРОВЬЯ ЦЕЛИ,КОТОРУЮ ВЫ АТАКУЕТЕ</size>"
                            }
                        },
                        DamageTextSetting = new MarkersSetting.DamageText
                        {
                            PermissionsDamageText = "iqmarker.damagetext",
                            PermissionsDamageTextWounded = "iqmarker.damagetextwound",
                            GeneralSetting = new MarkersSetting.GeneralSettings
                            {
                                DisplayName = "<size=16><b>ТЕКСТОВЫЙ ИНДИКАТОР</b></size>",
                                Description = "<size=11>ПРИ ПОПАДАНИИ БУДЕТ ОТОБРАЖАТЬСЯ НАНЕСЕННЫЙ УРОН</size>"
                            }
                        },
                        IconSetting = new MarkersSetting.Icon
                        {
                            PermissionsIcon = "iqmarker.icon",
                            PermissionsIconDamageText = "iqmarker.icondamagetext",
                            PermissionsIconWounded = "iqmarker.iconwound",
                            IconList = new List<MarkersSetting.Icon.IconListClass>
                            {
                                new MarkersSetting.Icon.IconListClass
                                {
                                      Permissions = "",
                                      PNG = "https://i.imgur.com/mIbPpj3.png"
                                },
                                new MarkersSetting.Icon.IconListClass
                                {
                                      Permissions = "",
                                      PNG = "https://i.imgur.com/XCSkVNk.png"
                                },
                                new MarkersSetting.Icon.IconListClass
                                {
                                      Permissions = "",
                                      PNG = "https://i.imgur.com/RACMuqg.png"
                                },
                                new MarkersSetting.Icon.IconListClass
                                {
                                      Permissions = "",
                                      PNG = "https://i.imgur.com/tqtF73m.png"
                                },
                                new MarkersSetting.Icon.IconListClass
                                {
                                      Permissions = "",
                                      PNG = "https://i.imgur.com/uIHaR7Q.png"
                                },
                                new MarkersSetting.Icon.IconListClass
                                {
                                      Permissions = "",
                                      PNG = "https://i.imgur.com/Dbxnsm1.png"
                                },
                                new MarkersSetting.Icon.IconListClass
                                {
                                      Permissions = "",
                                      PNG = "https://i.imgur.com/bzsU7kE.png"
                                },
                                new MarkersSetting.Icon.IconListClass
                                {
                                      Permissions = "",
                                      PNG = "https://i.imgur.com/2Wke9lp.png"
                                },
                            },
                            GeneralSetting = new MarkersSetting.GeneralSettings
                            {
                                DisplayName = "<size=16><b>ИНДИКАТОР ИКОНКА</b></size>",
                                Description = "<size=11>ПРИ ПОПАДАНИИ БУДЕТ ОТОБРАЖАТЬСЯ ИКОНКА С ПОПАДАНИЕМ</size>"
                            }
                        }
                    },
                    #endregion

                    #region Interface
                    SettingsInterfaces = new SettingsInterface
                    {
                        HexLabels = "#FFFFFFFF",
                        InterfaceMainPanel = new SettingsInterface.MainPanel
                        {
                            HexBackground = "#444440A6",
                            MaterialBackground = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                            SpriteLogoBackground = "assets/icons/workshop.png",
                        },
                        InterfaceSettingsMarker = new SettingsInterface.SettingsMarker
                        {
                            HexButtonInputPanel = "#93C34680",
                            HexButtonMain = "#93C34680",
                            HexActiveShowMarker = "#93C34680",
                            HexInActiveShowMarker = "#C3454580",
                            SpriteInActiveShowMarker = "assets/icons/vote_down.png",
                            SpriteActiveShowMarker = "assets/icons/vote_up.png",
                            HexTypeMarkerPanel = "#FFFFFF29",
                            HexTypeMarkerAcces = "#93C344FF",
                            HexTypeMarkerTaking = "#93C34480",
                            HexTypeMarkerDeading = "#C3454580",
                        },
                    },
                    #endregion

                    #region DefaultUser
                    UserSetting = new UserSettings
                    {
                        DefaultTurnOnMarker = true,
                        DefaultHexMarker = "#05bec5",
                        DefaultMarkerType = MarkerType.Icon,
                        DefaultMarkerSize = TypeSize.Middle,
                        DefaultDamageText = new UserSettings.DamageText
                        {
                            TurnWounded = false,
                        },
                        DefaultHealthBar = new UserSettings.HealthBar
                        {
                            TurnDamageText = false,
                            TurnWounded = false,
                        },
                        DefaultIcon = new UserSettings.Icon
                        {
                            TurnWounded = true,
                            TurnDamageText = true
                        },
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
                PrintWarning($"Ошибка чтения #57 конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Data
        public enum TypeSize
        {
            Little,
            Middle,
            Big
        }
        public enum MarkerType
        {
            DamageText,
            HealthBar,
            Icon,
        }

        [JsonProperty("Информация о настройках хитов игрока")]
        public Dictionary<ulong, SettingsUser> DataInformation = new Dictionary<ulong, SettingsUser>();

        public class SettingsUser
        {
            public bool TurnMarker;
            public bool HitAnimal;    
            public bool HitPlayer;    
            public bool HitBuilding;    
            public bool HitEntity;    
            public bool HitTransport;    
            public bool HitNPC;
            public string HexMarker;
            public TypeSize typeSize;
            public MarkerType markerType;
            public HealthBar HealthBarMore = new HealthBar();
            public DamageText DamageTextMore = new DamageText();
            public Icon IconMore = new Icon();

            internal class HealthBar
            {
                public bool TurnDamageText;
                public bool TurnWounded;
            }
            internal class DamageText
            {
                public bool TurnWounded;
            }
            internal class Icon
            {
                public bool TurnDamageText;
                public bool TurnWounded;
                public int IndexMarker;
            }
        }

        void ReadData()
        {
            DataInformation = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, SettingsUser>>("IQMarker/SettingsUser");
        }
        void WriteData() => Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQMarker/SettingsUser", DataInformation);

        void RegisteredDataUser(BasePlayer player)
        {
            var Default = config.UserSetting;
            if (!DataInformation.ContainsKey(player.userID))
                DataInformation.Add(player.userID, new SettingsUser
                {
                    TurnMarker = Default.DefaultTurnOnMarker,
                    HitAnimal = true,
                    HitPlayer = true,
                    HitBuilding = true,
                    HitEntity = true,
                    HitNPC = true,
                    HitTransport = true,
                    HexMarker = Default.DefaultHexMarker,
                    typeSize = Default.DefaultMarkerSize,
                    markerType = Default.DefaultMarkerType,

                    HealthBarMore = new SettingsUser.HealthBar
                    {
                        TurnDamageText = Default.DefaultHealthBar.TurnDamageText,
                        TurnWounded = Default.DefaultHealthBar.TurnWounded,
                    },

                    DamageTextMore = new SettingsUser.DamageText
                    {
                        TurnWounded = Default.DefaultDamageText.TurnWounded,
                    },
                    IconMore = new SettingsUser.Icon
                    {
                        IndexMarker = 0,
                        TurnDamageText = Default.DefaultIcon.TurnDamageText,
                        TurnWounded = Default.DefaultIcon.TurnWounded,
                    }
                });
        }
        #endregion

        #region Hooks
        private void Init() => ReadData();
        private void OnServerInitialized()
        {
            PrintWarning("Загружаем изображения..");
            LoadedImage();
            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);

            PrintWarning("Регистрируем права..");
            RegisteredPermissions();
            PrintWarning("Проверяем права игроков..");

            PrintWarning("Плагин загружен успешно!");
        }
        private void Unload() => WriteData();
        void OnPlayerConnected(BasePlayer player)
        {
            RegisteredDataUser(player);
            CachedImage(player);
            ReturnSettingsMore(player);
        }
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (info == null || entity == null) return;
            var Attacker = info?.InitiatorPlayer;
            if (info.damageTypes.Total() < 1) return;
            if (Attacker == null || !(bool)(Attacker is BasePlayer) || Attacker.IsNpc || Attacker.IsDead()) return;
            if (entity is BaseCorpse) return;

            if (!DataInformation.ContainsKey(Attacker.userID)) return;
            var Data = DataInformation[Attacker.userID];

            if (Data.HitAnimal)
                if (entity is BaseAnimalNPC || entity is RidableHorse || entity is Horse)
                    if (!entity.IsDead())
                        Interface_Attack_Hit(Attacker, info);

            if (Data.HitBuilding)
                if (entity is BuildingBlock)
                    if (!entity.IsDestroyed)
                        Interface_Attack_Hit(Attacker, info);

            if (Data.HitNPC)
                if (entity is HumanNPC || entity is NPCPlayer || entity is ScientistNPC)
                    if (!entity.IsDead())
                        Interface_Attack_Hit(Attacker, info);

            if (Data.HitPlayer)
                if (entity is BasePlayer)
                    if (entity.ToPlayer().userID != Attacker.userID)
                        if (!entity.IsDead())
                            Interface_Attack_Hit(Attacker, info);

            if (Data.HitTransport)
                if (entity is ModularCar || entity is BaseVehicleSeat || entity is BaseModularVehicle || entity is BaseVehicleModule)
                    Interface_Attack_Hit(Attacker, info);

            if (Data.HitEntity)
                if (entity.PrefabName.Contains("barrel")
                    || entity.PrefabName.Contains("external")
                    || entity is BaseOven 
                    || entity is StorageContainer 
                    || entity is SleepingBag 
                    || entity is VendingMachine
                    || entity is Composter
                    || entity is Barricade
                    || entity is MiningQuarry
                    || entity is AutoTurret
                    || entity is ElectricWindmill)
                    if(!entity.IsDestroyed)
                    Interface_Attack_Hit(Attacker, info);
        }

        #endregion

        #region Commands

        [ChatCommand("hit")]
        void ChatCommandHit(BasePlayer player)
        {
            Interface_Main_Menu(player);
        }

        [ConsoleCommand("hit")]
        void ConsoleCommandsHit(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            string KeyArg = arg.Args[0].ToLower();
            switch (KeyArg)
            {
                case "debug":
                    {
                        if (player == null || !player.IsConnected) return;
                        if (!player.IsAdmin) return;
                        PrintWarning($"{player.displayName} - использовал DEBUG режим");
                        DebugPlayerHit(player);
                        PrintToConsole("Успешно использован DEBUG");
                        break;
                    }
                case "turn":
                    {
                        string TurnStatus = arg.Args[1].ToLower();
                        switch (TurnStatus)
                        {
                            case "on":
                                {
                                    if(DataInformation.ContainsKey(player.userID))
                                        DataInformation[player.userID].TurnMarker = true;
                                    ButtonUpdateTurn(player);
                                    break;
                                }
                            case "off":
                                {
                                    if (DataInformation.ContainsKey(player.userID))
                                        DataInformation[player.userID].TurnMarker = false;
                                    ButtonUpdateTurn(player);
                                    break;
                                }
                        }
                        break;
                    }
                case "func": 
                    {
                        string Func = arg.Args[1].ToLower();
                        switch (Func)
                        {
                            case "selecttype": 
                                {
                                    string Type = arg.Args[2].ToLower();
                                    SelectType(player, Type);
                                    break;
                                }
                            case "sethex":
                                {
                                    if (arg.Args.Length != 3) return;

                                    if (String.IsNullOrWhiteSpace(arg.Args[2])) return; 
                                    string Hex = arg.Args[2].ToLower();
                                    DataInformation[player.userID].HexMarker = Hex;
                                    Interface_Update_Marker_Show(player);
                                    break;
                                }
                            case "setsize": 
                                {
                                    if (String.IsNullOrWhiteSpace(arg.Args[2])) return;
                                    TypeSize SizeType = (TypeSize)Enum.Parse(typeof(TypeSize), arg.Args[2]);
                                    DataInformation[player.userID].typeSize = SizeType;
                                    Interface_MP_Loaded_Size(player);
                                    Interface_Update_Marker_Show(player);
                                    break;
                                }
                            case "settype":
                                {
                                    if (String.IsNullOrWhiteSpace(arg.Args[2])) return;
                                    MarkerType MarkerType = (MarkerType)Enum.Parse(typeof(MarkerType), arg.Args[2]);
                                    DataInformation[player.userID].markerType = MarkerType;
                                    Interface_MP_Loaded_Markers(player);
                                    Interface_Update_Marker_Show(player);
                                    break;
                                }
                        }
                        break;
                    }
                case "ui":  
                    {
                        string UIElement = arg.Args[1].ToLower();
                        switch (UIElement)
                        {
                            case "settings":
                                {
                                    Interface_Settings_Menu(player);
                                    Interface_Hex_Settings_Menu(player);
                                    break;
                                }
                            case "setting_marker":
                                {
                                    string Action = arg.Args[2].ToLower();
                                    switch (Action)
                                    {
                                        case "healthbar":
                                            {
                                                string HealthBarMore = arg.Args[3].ToLower();
                                                switch (HealthBarMore)
                                                {
                                                    case "damagetext_turn":
                                                        {
                                                            if (DataInformation[player.userID].HealthBarMore.TurnDamageText)
                                                                DataInformation[player.userID].HealthBarMore.TurnDamageText = false;
                                                            else DataInformation[player.userID].HealthBarMore.TurnDamageText = true;

                                                            Interface_Update_Marker_Show(player);
                                                            Show_More_Settings_Marker(player, MarkerType.HealthBar);
                                                            break;
                                                        }
                                                    case "wounded_turn":
                                                        {
                                                            if (DataInformation[player.userID].HealthBarMore.TurnWounded)
                                                                DataInformation[player.userID].HealthBarMore.TurnWounded = false;
                                                            else DataInformation[player.userID].HealthBarMore.TurnWounded = true;

                                                            Interface_Update_Marker_Show(player);
                                                            Show_More_Settings_Marker(player, MarkerType.HealthBar);
                                                            break;
                                                        }
                                                }
                                                break;
                                            }
                                        case "damage_text":
                                            {
                                                string DamageTextMore = arg.Args[3].ToLower();
                                                switch(DamageTextMore)
                                                {
                                                    case "wounded_turn":
                                                        {
                                                            if (DataInformation[player.userID].DamageTextMore.TurnWounded)
                                                                DataInformation[player.userID].DamageTextMore.TurnWounded = false;
                                                            else DataInformation[player.userID].DamageTextMore.TurnWounded = true;

                                                            Interface_Update_Marker_Show(player);
                                                            Show_More_Settings_Marker(player, MarkerType.DamageText);
                                                            break;
                                                        }
                                                }
                                                break;
                                            }
                                        case "icon":
                                            {
                                                string IconMore = arg.Args[3].ToLower();
                                                switch(IconMore)
                                                {
                                                    case "damagetext_turn":
                                                        {
                                                            if (DataInformation[player.userID].IconMore.TurnDamageText)
                                                                DataInformation[player.userID].IconMore.TurnDamageText = false;
                                                            else DataInformation[player.userID].IconMore.TurnDamageText = true;

                                                            Interface_Update_Marker_Show(player);
                                                            Show_More_Settings_Marker(player, MarkerType.Icon);
                                                            break;
                                                        }
                                                    case "wounded_turn":
                                                        {
                                                            if (DataInformation[player.userID].IconMore.TurnWounded)
                                                                DataInformation[player.userID].IconMore.TurnWounded = false;
                                                            else DataInformation[player.userID].IconMore.TurnWounded = true;

                                                            Interface_Update_Marker_Show(player);
                                                            Show_More_Settings_Marker(player, MarkerType.Icon);
                                                            break;
                                                        }
                                                    case "marker_icon_set":
                                                        {
                                                            int IndexMarker = Convert.ToInt32(arg.Args[4]);
                                                            DataInformation[player.userID].IconMore.IndexMarker = IndexMarker;
                                                            Interface_Update_Marker_Show(player);
                                                            break;
                                                        }
                                                }
                                                break;
                                            }
                                    }
                                    break;
                                }
                        }
                        break;
                    }
            }
        }

        #endregion

        #region Metods
        public void SelectType(BasePlayer player,string Type)
        {
            if (String.IsNullOrWhiteSpace(Type)) return;
            if (!DataInformation.ContainsKey(player.userID)) return;
            var Data = DataInformation[player.userID];
            switch (Type.ToLower())
            {
                case "animal": 
                    {
                        if (!Data.HitAnimal)
                            Data.HitAnimal = true;
                        else Data.HitAnimal = false;
                        break;
                    }
                case "player":
                    {
                        if (!Data.HitPlayer)
                            Data.HitPlayer = true;
                        else Data.HitPlayer = false;
                        break;
                    }
                case "building":
                    {
                        if (!Data.HitBuilding)
                            Data.HitBuilding = true;
                        else Data.HitBuilding = false;
                        break;
                    }
                case "entity":
                    {
                        if (!Data.HitEntity)
                            Data.HitEntity = true;
                        else Data.HitEntity = false;
                        break;
                    }
                case "transport":
                    {
                        if (!Data.HitTransport)
                            Data.HitTransport = true;
                        else Data.HitTransport = false;
                        break;
                    }
                case "npc":
                    {
                        if (!Data.HitNPC)
                            Data.HitNPC = true;
                        else Data.HitNPC = false;
                        break;
                    }
            }
            Interface_Settings_Menu(player);
        }

        void DebugPlayerHit(BasePlayer player)
        {
            var ColorList = config.AccessSettings.ColorListMarker;
            var Markers = config.MarkesSettings;
            var HealtBar = Markers.HealthBarSetting;
            var DamageText = Markers.DamageTextSetting;
            var Icon = Markers.IconSetting;
            string ID = player.UserIDString;

            GrantedPermission(ID,PermissionUseMarker);
            GrantedPermission(ID, PermissionUseColorList);
            GrantedPermission(ID, PermissionUseCustomColor);
            GrantedPermission(ID, PermissionUseSizer);
            GrantedPermission(ID, HealtBar.PermissionsHealthBar);
            GrantedPermission(ID, HealtBar.PermissionsHealtBarDamageText);
            GrantedPermission(ID, HealtBar.PermissionsHealtBarWounded);
            GrantedPermission(ID, DamageText.PermissionsDamageText);
            GrantedPermission(ID, DamageText.PermissionsDamageTextWounded);
            GrantedPermission(ID, Icon.PermissionsIcon);
            GrantedPermission(ID, Icon.PermissionsIconWounded);
            GrantedPermission(ID, Icon.PermissionsIconDamageText);

            for (int i = 0; i < Icon.IconList.Count; i++)
                GrantedPermission(ID, Icon.IconList[i].Permissions);

            for (int i = 0; i < ColorList.Count; i++)
                GrantedPermission(ID, ColorList[i].Permissions);
        }
        private void GrantedPermission(string id,string perm)
        {
            if (!permission.UserHasPermission(id, perm))
                permission.GrantUserPermission(id, perm, this);
        }
        #endregion

        #region Interface
        public static string PARENT_IQ_MARKER_MAIN = "PARENT_IQ_MARKER_MAIN";
        public static string PARENT_IQ_MARKER_MARKERS_PANEL = "PARENT_IQ_MARKER_MARKERS_PANEL";
        public static string PARENT_IQ_MARKER_MARKERS_PANEL_MORE_SETTINGS = "PARENT_IQ_MARKER_MARKERS_PANEL_MORE_SETTINGS";
        public static string PARENT_IQ_MARKER_ATTACK_HIT = "PARENT_IQ_MARKER_ATTACK_HIT";

        #region Interface Main 
        public void Interface_Main_Menu(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PARENT_IQ_MARKER_MAIN);
            var Interface = config.SettingsInterfaces;

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = HexToRustFormat(Interface.InterfaceMainPanel.HexBackground), Material = Interface.InterfaceMainPanel.MaterialBackground }
            },  "Overlay", PARENT_IQ_MARKER_MAIN);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.8677083 0.9370371", AnchorMax = "0.9921876 0.9888889" },
                Button = { Close = PARENT_IQ_MARKER_MAIN, Color = "0 0 0 0" },
                Text = { Text = lang.GetMessage("UI_CLOSE_BTN", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Align = TextAnchor.MiddleCenter }
            }, PARENT_IQ_MARKER_MAIN);

            container.Add(new CuiElement
            {
                Parent = PARENT_IQ_MARKER_MAIN,
                Components =
                        {
                        new CuiImageComponent { Sprite = Interface.InterfaceMainPanel.SpriteLogoBackground },
                        new CuiRectTransformComponent{  AnchorMin = $"0.015625 0.8453708", AnchorMax = $"0.08229167 0.9638893" },
                        }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.08645834 0.8611111", AnchorMax = "0.2541667 0.9796296" },
                Text = { Text = lang.GetMessage("UI_TITLE_MAIN",this,player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.LowerLeft }
            },  PARENT_IQ_MARKER_MAIN);

            container.Add(new CuiLabel
            {                                    
                RectTransform = { AnchorMin = "0.088021541 0.9287037", AnchorMax = "0.2541667 0.9796296" },
                Text = { Text = lang.GetMessage("UI_TITLE_DESCRIPTION", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.LowerLeft }
            }, PARENT_IQ_MARKER_MAIN);

            if(config.AccessSettings.UseSettingsMarker)
            {
                container.Add(new CuiLabel
                {                                             
                    RectTransform = { AnchorMin = "0.6880208 0.90831541", AnchorMax = "0.9869792 0.9398148" }, 
                    Text = { Text = lang.GetMessage("UI_DETAL_INFO_TITLE", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperLeft }
                }, PARENT_IQ_MARKER_MAIN);

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.6880208 0.9462963", AnchorMax = "0.8609375 0.9824074" },
                    Button = { Command = "hit ui settings", Color = HexToRustFormat(Interface.InterfaceSettingsMarker.HexButtonMain) },
                    Text = { Text = lang.GetMessage("UI_SETTING_BUTTON", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Align = TextAnchor.MiddleCenter }
                }, PARENT_IQ_MARKER_MAIN);
            }

            CuiHelper.AddUi(player, container);
            Interface_Marker_Panels(player);
            ButtonUpdateTurn(player);
        }

        public void ButtonUpdateTurn(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "TURN_BTN");

            string LangTurn = DataInformation[player.userID].TurnMarker ? "UI_TURN_SKILL_OFF" : "UI_TURN_SKILL_ON";
            string CommandTurn = DataInformation[player.userID].TurnMarker ? "hit turn off" : "hit turn on";
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.08645833 0.8361111", AnchorMax = "0.2494792 0.8685217" },
                Button = { Command = CommandTurn, Color = "0 0 0 0" },
                Text = { Text = lang.GetMessage(LangTurn, this, player.UserIDString), Align = TextAnchor.UpperLeft }
            }, PARENT_IQ_MARKER_MAIN, "TURN_BTN");

            CuiHelper.AddUi(player, container);
            Interface_Update_Marker_Show(player);
        }

        #endregion

        #region Interface Settings Menu
        public void Interface_Settings_Menu(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "TYPE_SHOW_SETTINGS");
            var Interface = config.SettingsInterfaces;
            var Data = DataInformation[player.userID];
            string CommandSelect = "hit func selecttype {0}";

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5010417 0.8314815", AnchorMax = "0.9843747 0.912037" },
                Image = { Color = "0 0 0 0" }
            },  PARENT_IQ_MARKER_MAIN, "TYPE_SHOW_SETTINGS");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.5977009" },
                Text = { Text = lang.GetMessage("UI_SETTING_TYPE_SHOW", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperRight }
            },  "TYPE_SHOW_SETTINGS");

            #region Animal          

            string SpriteAnimal = Data.HitAnimal ? Interface.InterfaceSettingsMarker.SpriteActiveShowMarker : Interface.InterfaceSettingsMarker.SpriteInActiveShowMarker;
            string ColorAnimal = Data.HitAnimal ? Interface.InterfaceSettingsMarker.HexActiveShowMarker : Interface.InterfaceSettingsMarker.HexInActiveShowMarker;

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.002151709 0.6206903", AnchorMax = "0.1648663 1" },
                Button = { Command = String.Format(CommandSelect,"animal"), Color = HexToRustFormat(ColorAnimal) },
                Text = { Text = "" }
            },  "TYPE_SHOW_SETTINGS", "ANIMAL_BTN");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.221476 0", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage("UI_SETTINGS_TYPE_ANIMAL", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Align = TextAnchor.MiddleLeft }
            }, "ANIMAL_BTN");

            container.Add(new CuiElement
            {
                Parent = "ANIMAL_BTN",
                Components =
                        {
                        new CuiImageComponent { Sprite = SpriteAnimal },
                        new CuiRectTransformComponent{  AnchorMin = $"0.03355689 0.1515158", AnchorMax = $"0.19463 0.8787915" },
                        }
            });
            #endregion

            #region Player          

            string SpritePlayer = Data.HitPlayer ? Interface.InterfaceSettingsMarker.SpriteActiveShowMarker : Interface.InterfaceSettingsMarker.SpriteInActiveShowMarker;
            string ColorPlayer = Data.HitPlayer ? Interface.InterfaceSettingsMarker.HexActiveShowMarker : Interface.InterfaceSettingsMarker.HexInActiveShowMarker;

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.1691778 0.6206903", AnchorMax = "0.3318927 1" },
                Button = { Command = String.Format(CommandSelect, "player"), Color = HexToRustFormat(ColorPlayer) },
                Text = { Text = "" }
            }, "TYPE_SHOW_SETTINGS", "PLAYER_BTN");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.221476 0", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage("UI_SETTINGS_TYPE_PLAYER", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Align = TextAnchor.MiddleLeft }
            }, "PLAYER_BTN");

            container.Add(new CuiElement
            {
                Parent = "PLAYER_BTN",
                Components =
                        {
                        new CuiImageComponent { Sprite = SpritePlayer },
                        new CuiRectTransformComponent{  AnchorMin = $"0.03355689 0.1515158", AnchorMax = $"0.19463 0.8787915" },
                        }
            });
            #endregion

            #region Building          

            string SpriteBuilding = Data.HitBuilding ? Interface.InterfaceSettingsMarker.SpriteActiveShowMarker : Interface.InterfaceSettingsMarker.SpriteInActiveShowMarker;
            string ColorBuilding = Data.HitBuilding ? Interface.InterfaceSettingsMarker.HexActiveShowMarker : Interface.InterfaceSettingsMarker.HexInActiveShowMarker;

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.3362046 0.6206903", AnchorMax = "0.4989195 1" },
                Button = { Command = String.Format(CommandSelect, "building"), Color = HexToRustFormat(ColorBuilding) },
                Text = { Text = "" }
            }, "TYPE_SHOW_SETTINGS", "BUILDING_BTN");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.221476 0", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage("UI_SETTINGS_TYPE_BUILDING", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Align = TextAnchor.MiddleLeft }
            }, "BUILDING_BTN");

            container.Add(new CuiElement
            {
                Parent = "BUILDING_BTN",
                Components =
                        {
                        new CuiImageComponent { Sprite = SpriteBuilding },
                        new CuiRectTransformComponent{  AnchorMin = $"0.03355689 0.1515158", AnchorMax = $"0.19463 0.8787915" },
                        }
            });
            #endregion

            #region Entity          

            string SpriteEntity = Data.HitEntity ? Interface.InterfaceSettingsMarker.SpriteActiveShowMarker : Interface.InterfaceSettingsMarker.SpriteInActiveShowMarker;
            string ColorEntity = Data.HitEntity ? Interface.InterfaceSettingsMarker.HexActiveShowMarker : Interface.InterfaceSettingsMarker.HexInActiveShowMarker;

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5032315 0.6206903", AnchorMax = "0.6659464 1" },
                Button = { Command = String.Format(CommandSelect, "entity"), Color = HexToRustFormat(ColorEntity) },
                Text = { Text = "" }
            }, "TYPE_SHOW_SETTINGS", "ENTITY_BTN");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.221476 0", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage("UI_SETTINGS_TYPE_ENTITY", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Align = TextAnchor.MiddleLeft }
            }, "ENTITY_BTN");

            container.Add(new CuiElement
            {
                Parent = "ENTITY_BTN",
                Components =
                        {
                        new CuiImageComponent { Sprite = SpriteEntity },
                        new CuiRectTransformComponent{  AnchorMin = $"0.03355689 0.1515158", AnchorMax = $"0.19463 0.8787915" },
                        }
            });
            #endregion

            #region Transport        

            string SpriteTransport = Data.HitTransport ? Interface.InterfaceSettingsMarker.SpriteActiveShowMarker : Interface.InterfaceSettingsMarker.SpriteInActiveShowMarker;
            string ColorTransport = Data.HitTransport ? Interface.InterfaceSettingsMarker.HexActiveShowMarker : Interface.InterfaceSettingsMarker.HexInActiveShowMarker;

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.6702583 0.6206903", AnchorMax = "0.8329732 1" },
                Button = { Command = String.Format(CommandSelect, "transport"), Color = HexToRustFormat(ColorTransport) },
                Text = { Text = "" }
            }, "TYPE_SHOW_SETTINGS", "TRANSPORT_BTN");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.221476 0", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage("UI_SETTINGS_TYPE_TRANSPORT", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Align = TextAnchor.MiddleLeft }
            }, "TRANSPORT_BTN");

            container.Add(new CuiElement
            {
                Parent = "TRANSPORT_BTN",
                Components =
                        {
                        new CuiImageComponent { Sprite = SpriteTransport },
                        new CuiRectTransformComponent{  AnchorMin = $"0.03355689 0.1515158", AnchorMax = $"0.19463 0.8787915" },
                        }
            });
            #endregion

            #region NPC

            string SpriteNPC = Data.HitNPC ? Interface.InterfaceSettingsMarker.SpriteActiveShowMarker : Interface.InterfaceSettingsMarker.SpriteInActiveShowMarker;
            string ColorNPC = Data.HitNPC ? Interface.InterfaceSettingsMarker.HexActiveShowMarker : Interface.InterfaceSettingsMarker.HexInActiveShowMarker;

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.8372851 0.6206903", AnchorMax = "1 1" },
                Button = { Command = String.Format(CommandSelect, "npc"), Color = HexToRustFormat(ColorNPC) },
                Text = { Text = "" }
            }, "TYPE_SHOW_SETTINGS", "NPC_BTN");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.221476 0", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage("UI_SETTINGS_TYPE_NPC", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Align = TextAnchor.MiddleLeft }
            }, "NPC_BTN");

            container.Add(new CuiElement
            {
                Parent = "NPC_BTN",
                Components =
                        {
                        new CuiImageComponent { Sprite = SpriteNPC },
                        new CuiRectTransformComponent{  AnchorMin = $"0.03355689 0.1515158", AnchorMax = $"0.19463 0.8787915" },
                        }
            });
            #endregion

            CuiHelper.AddUi(player, container);
        }

        public void Interface_Hex_Settings_Menu(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionUseCustomColor)) return;

            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "HEX_SHOW_SETTINGS");
            var Interface = config.SettingsInterfaces;
            var Data = DataInformation[player.userID];
            string CommandSetHex = "hit func sethex {0}";

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5010417 0.7740743", AnchorMax = "0.9843747 0.8435205" },
                Image = { Color = "0 0 0 0" }
            }, PARENT_IQ_MARKER_MAIN, "HEX_SHOW_SETTINGS");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.5977009" },
                Text = { Text = lang.GetMessage("UI_SETTING_HEX_SHOW", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperRight }
            }, "HEX_SHOW_SETTINGS");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.378233 0.4266549", AnchorMax = "0.5959055 1" },
                Text = { Text = lang.GetMessage("UI_SETTING_HEX_SHOW_YOUR", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperRight }
            }, "HEX_SHOW_SETTINGS");

            string SetHex = "";
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.6056038 0.6399806", AnchorMax = "1 0.95" },
                Image = { Color = HexToRustFormat(Interface.InterfaceSettingsMarker.HexButtonInputPanel), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            }, "HEX_SHOW_SETTINGS", "HEX_SHOW_SETTINGS" + ".Input");

            container.Add(new CuiElement
            {
                Parent = "HEX_SHOW_SETTINGS" + ".Input",
                Name = "HEX_SHOW_SETTINGS" + ".Input.Current",
                Components =
                {
                    new CuiInputFieldComponent { Text = SetHex, FontSize = 12,Command = String.Format(CommandSetHex, SetHex), Align = TextAnchor.MiddleCenter, CharsLimit = 10},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Interface Markers

        public void Interface_Marker_Panels(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PARENT_IQ_MARKER_MARKERS_PANEL);
            var Interface = config.SettingsInterfaces;

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.7675926" },
                Image = { Color = "0 0 0 0" }
            }, PARENT_IQ_MARKER_MAIN, PARENT_IQ_MARKER_MARKERS_PANEL);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.003125004 0.8769602", AnchorMax = "0.1854167 0.99031541" },  
                Text = { Text = lang.GetMessage("UI_MARKER_PANEL_SHOW_MARKER", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, PARENT_IQ_MARKER_MARKERS_PANEL);

            CuiHelper.AddUi(player, container);
            Interface_MP_Loaded_Colors(player);
            Interface_MP_Loaded_Size(player);
            Interface_MP_Loaded_Markers(player);
        }

        #region Color List
        public void Interface_MP_Loaded_Colors(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionUseColorList)) return;

            CuiElementContainer container = new CuiElementContainer();
            var ColorList = config.AccessSettings.ColorListMarker;
            var Interface = config.SettingsInterfaces;

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.003125004 0.7334132", AnchorMax = "0.4296875 0.7780446" },
                Text = { Text = lang.GetMessage("UI_MARKER_PANEL_SHOW_MARKER_COLOR", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, PARENT_IQ_MARKER_MARKERS_PANEL);

            for (int i = 0,x = 0, y = 0; i < ColorList.Count; i++)
            {
                string Permissions = ColorList[i].Permissions;
                string Command = String.IsNullOrWhiteSpace(Permissions) ? $"hit func sethex {ColorList[i].HexColor}" : permission.UserHasPermission(player.UserIDString, Permissions) ? $"hit func sethex {ColorList[i].HexColor}" : "";
                
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{0.003645283 + (x * 0.105)} {0.6851619 - (y * 0.06)}", AnchorMax = $"{0.1041679 + (x * 0.105)} {0.7297935 - (y * 0.06)}" },
                    Button = { Command = Command, Color = HexToRustFormat(ColorList[i].HexColor) },
                    Text = { Text = ColorList[i].HexColor, Align = TextAnchor.MiddleCenter }
                },  PARENT_IQ_MARKER_MARKERS_PANEL, $"HEX_{i}");
                 
                if (!String.IsNullOrWhiteSpace(Permissions))
                    if (!permission.UserHasPermission(player.UserIDString, Permissions))
                    {
                        container.Add(new CuiElement
                        {
                            Parent = $"HEX_{i}",
                            Components =
                            {
                                new CuiImageComponent {  Color = HexToRustFormat(Interface.HexLabels), Sprite = "assets/icons/lock.png" },
                                new CuiRectTransformComponent { AnchorMin = $"0.01554396 0.08108196", AnchorMax = $"0.1813461 0.9459562" }
                            }
                        });
                    }

                if (x == 3 && y == 11) break;
                x++;
                if (x == 4)
                {
                    x = 0;
                    y++;
                }
            }

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Sizer
        public void Interface_MP_Loaded_Size(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionUseSizer)) return;

            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "SIZE_MARKER_LABEL");
            CuiHelper.DestroyUi(player, "SIZE_MARKER_LITTLE");
            CuiHelper.DestroyUi(player, "SIZE_MARKER_MIDDLE");
            CuiHelper.DestroyUi(player, "SIZE_MARKER_BIG");

            var Interface = config.SettingsInterfaces;
            var InterfaceMarker = Interface.InterfaceSettingsMarker;
            var Data = DataInformation[player.userID];
            string ColorButtonLittle = Data.typeSize == TypeSize.Little ? InterfaceMarker.HexTypeMarkerTaking : InterfaceMarker.HexTypeMarkerAcces;
            string ColorButtonMiddle = Data.typeSize == TypeSize.Middle ? InterfaceMarker.HexTypeMarkerTaking : InterfaceMarker.HexTypeMarkerAcces;
            string ColorButtonBig = Data.typeSize == TypeSize.Big ? InterfaceMarker.HexTypeMarkerTaking : InterfaceMarker.HexTypeMarkerAcces;


            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.003125004 0.8383594", AnchorMax = "0.4296875 0.8829909" },
                Text = { Text = lang.GetMessage("UI_MARKER_PANEL_SHOW_MARKER_SIZE", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, PARENT_IQ_MARKER_MARKERS_PANEL, "SIZE_MARKER_LABEL");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.003125004 0.7913145", AnchorMax = $"0.1401042 0.835946" },
                Button = { Command = "hit func setsize Little", Color = HexToRustFormat(ColorButtonLittle) },
                Text = { Text = lang.GetMessage("UI_MARKER_PANEL_SHOW_MARKER_SIZE_LITTLE",this,player.UserIDString), Align = TextAnchor.MiddleCenter }
            },  PARENT_IQ_MARKER_MARKERS_PANEL, "SIZE_MARKER_LITTLE");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.1421877 0.7913145", AnchorMax = $"0.2791662 0.835946" },
                Button = { Command = "hit func setsize Middle", Color = HexToRustFormat(ColorButtonMiddle) },
                Text = { Text = lang.GetMessage("UI_MARKER_PANEL_SHOW_MARKER_SIZE_MIDDLE", this, player.UserIDString), Align = TextAnchor.MiddleCenter }
            }, PARENT_IQ_MARKER_MARKERS_PANEL, "SIZE_MARKER_MIDDLE");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.2812511 0.7913145", AnchorMax = $"0.4182266 0.835946" },
                Button = { Command = "hit func setsize Big", Color = HexToRustFormat(ColorButtonBig) },
                Text = { Text = lang.GetMessage("UI_MARKER_PANEL_SHOW_MARKER_SIZE_BIG", this, player.UserIDString), Align = TextAnchor.MiddleCenter }
            }, PARENT_IQ_MARKER_MARKERS_PANEL, "SIZE_MARKER_BIG");

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Markers

        #region Loaded Marker Type
        public void Interface_MP_Loaded_Markers(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "HEALTH_BAR");
            CuiHelper.DestroyUi(player, "DAMAGE_TEXT");
            CuiHelper.DestroyUi(player, "ICON");
            CuiHelper.DestroyUi(player, "TITLE_MARKERS");


            var Interface = config.SettingsInterfaces;
            var InterfaceMarker = Interface.InterfaceSettingsMarker;
            var Data = DataInformation[player.userID];
            var HealthBar = config.MarkesSettings.HealthBarSetting;
            var DamageText = config.MarkesSettings.DamageTextSetting;
            var Icon = config.MarkesSettings.IconSetting;
            string Command = "hit func settype {0}";

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.4963542 0.8431841", AnchorMax = "0.9947917 0.9191793" },
                Text = { Text = lang.GetMessage("UI_MARKER_PANEL_SHOW_MARKER_LIST_TITLE", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.LowerRight }
            }, PARENT_IQ_MARKER_MARKERS_PANEL, "TITLE_MARKERS");

            #region Healt Bar
           
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.4395843 0.6682748", AnchorMax = "0.6182302 0.8359461" },
                Image = { Color = HexToRustFormat(InterfaceMarker.HexTypeMarkerPanel) }
            },  PARENT_IQ_MARKER_MARKERS_PANEL, "HEALTH_BAR");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.705038", AnchorMax = "1 0.9856114" },
                Text = { Text = HealthBar.GeneralSetting.DisplayName, Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            },  "HEALTH_BAR");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.05035979", AnchorMax = "1 0.6690668" },
                Text = { Text = HealthBar.GeneralSetting.Description, Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            },  "HEALTH_BAR");

            string ColorButtonHealthBar = Data.markerType == MarkerType.HealthBar ? InterfaceMarker.HexTypeMarkerTaking : permission.UserHasPermission(player.UserIDString, HealthBar.PermissionsHealthBar) ? InterfaceMarker.HexTypeMarkerAcces : InterfaceMarker.HexTypeMarkerDeading;
            string LangButtonHealthBar = Data.markerType == MarkerType.HealthBar ? "UI_MARKER_PANEL_SHOW_MARKER_BTN_TAKING" : permission.UserHasPermission(player.UserIDString, HealthBar.PermissionsHealthBar) ? "UI_MARKER_PANEL_SHOW_MARKER_BTN_ACCES" : "UI_MARKER_PANEL_SHOW_MARKER_BTN_DEADING";
            string CommandHealthBar = Data.markerType == MarkerType.HealthBar ? "" : permission.UserHasPermission(player.UserIDString, HealthBar.PermissionsHealthBar) ? String.Format(Command, MarkerType.HealthBar) : "";

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 -35", OffsetMax = "230 -5" },
                Button = { Command = CommandHealthBar, Color = HexToRustFormat(ColorButtonHealthBar) },
                Text = { Text = lang.GetMessage(LangButtonHealthBar, this,player.UserIDString), Align = TextAnchor.MiddleCenter }
            }, "HEALTH_BAR");

            #endregion

            #region Damage Text

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.6265606 0.6682748", AnchorMax = "0.8052065 0.8359461" },
                Image = { Color = HexToRustFormat(InterfaceMarker.HexTypeMarkerPanel) }
            }, PARENT_IQ_MARKER_MARKERS_PANEL, "DAMAGE_TEXT");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.705038", AnchorMax = "1 0.9856114" },
                Text = { Text = DamageText.GeneralSetting.DisplayName, Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, "DAMAGE_TEXT");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.05035979", AnchorMax = "1 0.6690668" },
                Text = { Text = DamageText.GeneralSetting.Description, Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, "DAMAGE_TEXT");

            string ColorButtonDamageText = Data.markerType == MarkerType.DamageText ? InterfaceMarker.HexTypeMarkerTaking : permission.UserHasPermission(player.UserIDString, DamageText.PermissionsDamageText) ? InterfaceMarker.HexTypeMarkerAcces : InterfaceMarker.HexTypeMarkerDeading;
            string LangButtonDamageText = Data.markerType == MarkerType.DamageText ? "UI_MARKER_PANEL_SHOW_MARKER_BTN_TAKING" : permission.UserHasPermission(player.UserIDString, DamageText.PermissionsDamageText) ? "UI_MARKER_PANEL_SHOW_MARKER_BTN_ACCES" : "UI_MARKER_PANEL_SHOW_MARKER_BTN_DEADING";
            string CommandDamageText = Data.markerType == MarkerType.DamageText ? "" : permission.UserHasPermission(player.UserIDString, DamageText.PermissionsDamageText) ? String.Format(Command, MarkerType.DamageText) : "";

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 -35", OffsetMax = "230 -5" },
                Button = { Command = CommandDamageText, Color = HexToRustFormat(ColorButtonDamageText) },
                Text = { Text = lang.GetMessage(LangButtonDamageText, this, player.UserIDString), Align = TextAnchor.MiddleCenter }
            }, "DAMAGE_TEXT");

            #endregion

            #region Icon

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.8140578 0.6682748", AnchorMax = "0.9927037 0.8359461" },
                Image = { Color = HexToRustFormat(InterfaceMarker.HexTypeMarkerPanel) }
            }, PARENT_IQ_MARKER_MARKERS_PANEL, "ICON");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.705038", AnchorMax = "1 0.9856114" },
                Text = { Text = Icon.GeneralSetting.DisplayName, Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, "ICON");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.05035979", AnchorMax = "1 0.6690668" },
                Text = { Text = Icon.GeneralSetting.Description, Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, "ICON");

            string ColorButtonIcon = Data.markerType == MarkerType.Icon ? InterfaceMarker.HexTypeMarkerTaking : permission.UserHasPermission(player.UserIDString, Icon.PermissionsIcon) ? InterfaceMarker.HexTypeMarkerAcces : InterfaceMarker.HexTypeMarkerDeading;
            string LangButtonIcon = Data.markerType == MarkerType.Icon ? "UI_MARKER_PANEL_SHOW_MARKER_BTN_TAKING" : permission.UserHasPermission(player.UserIDString, Icon.PermissionsIcon) ? "UI_MARKER_PANEL_SHOW_MARKER_BTN_ACCES" : "UI_MARKER_PANEL_SHOW_MARKER_BTN_DEADING";
            string CommandIcon = Data.markerType == MarkerType.Icon ? "" : permission.UserHasPermission(player.UserIDString, Icon.PermissionsIcon) ? String.Format(Command, MarkerType.Icon) : "";
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 -35", OffsetMax = "230 -5" },
                Button = { Command = CommandIcon, Color = HexToRustFormat(ColorButtonIcon) },
                Text = { Text = lang.GetMessage(LangButtonIcon, this, player.UserIDString), Align = TextAnchor.MiddleCenter }
            }, "ICON");

            #endregion

            CuiHelper.AddUi(player, container);
            Show_More_Settings_Marker(player, Data.markerType);
        }
        #endregion

        #region More Settings Marker  

        public void Show_More_Settings_Marker(BasePlayer player,MarkerType markerType)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PARENT_IQ_MARKER_MARKERS_PANEL_MORE_SETTINGS);
            var Interface = config.SettingsInterfaces;
            var InterfaceMarker = Interface.InterfaceSettingsMarker;
            var Data = DataInformation[player.userID];

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.4963542 0.01447529", AnchorMax = "0.9947917 0.5307575" },
                Image = { Color = "0 0 0 0" }
            },  PARENT_IQ_MARKER_MARKERS_PANEL, PARENT_IQ_MARKER_MARKERS_PANEL_MORE_SETTINGS);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.86449", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage("UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_TITLE", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleRight }
            }, PARENT_IQ_MARKER_MARKERS_PANEL_MORE_SETTINGS);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.8271067", AnchorMax = "1 0.9042059" },
                Text = { Text = lang.GetMessage("UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_DESCRIPTION", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleRight }
            }, PARENT_IQ_MARKER_MARKERS_PANEL_MORE_SETTINGS);

            switch (markerType)
            {
                case MarkerType.HealthBar:
                    {
                        var HealthBar = config.MarkesSettings.HealthBarSetting;
                        string ColorDamageText = !String.IsNullOrWhiteSpace(HealthBar.PermissionsHealtBarDamageText) ? permission.UserHasPermission(player.UserIDString, HealthBar.PermissionsHealtBarDamageText) ? Data.HealthBarMore.TurnDamageText ? InterfaceMarker.HexTypeMarkerTaking : InterfaceMarker.HexTypeMarkerAcces : InterfaceMarker.HexTypeMarkerDeading : InterfaceMarker.HexTypeMarkerDeading;
                        string ColorWounded = !String.IsNullOrWhiteSpace(HealthBar.PermissionsHealtBarWounded) ? permission.UserHasPermission(player.UserIDString, HealthBar.PermissionsHealtBarWounded) ? Data.HealthBarMore.TurnWounded ? InterfaceMarker.HexTypeMarkerTaking : InterfaceMarker.HexTypeMarkerAcces : InterfaceMarker.HexTypeMarkerDeading : InterfaceMarker.HexTypeMarkerDeading;

                        string LangDamageText = !String.IsNullOrWhiteSpace(HealthBar.PermissionsHealtBarDamageText) ? permission.UserHasPermission(player.UserIDString, HealthBar.PermissionsHealtBarDamageText) ? Data.HealthBarMore.TurnDamageText ? "UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_NO" : "UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_YES" : "UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_DEADING" : "UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_DEADING";
                        string LangWounded = !String.IsNullOrWhiteSpace(HealthBar.PermissionsHealtBarWounded) ? permission.UserHasPermission(player.UserIDString, HealthBar.PermissionsHealtBarWounded) ? Data.HealthBarMore.TurnWounded ? "UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_NO" : "UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_YES" : "UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_DEADING" : "UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_DEADING";

                        string CommandDamageText = !String.IsNullOrWhiteSpace(HealthBar.PermissionsHealtBarDamageText) ? permission.UserHasPermission(player.UserIDString, HealthBar.PermissionsHealtBarDamageText) ? "hit ui setting_marker healthbar damagetext_turn" : "" : "";
                        string CommandWoundedText = !String.IsNullOrWhiteSpace(HealthBar.PermissionsHealtBarWounded) ? permission.UserHasPermission(player.UserIDString, HealthBar.PermissionsHealtBarWounded) ? "hit ui setting_marker healthbar wounded_turn" : "" : "";

                        container.Add(new CuiLabel
                        {
                            RectTransform = { AnchorMin = "0.3500522 0.6658914", AnchorMax = "1 0.771032" },
                            Text = { Text = String.Format(lang.GetMessage("UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_TITLE_DAMAGE_TEXT", this, player.UserIDString), HealthBar.GeneralSetting.DisplayName), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleRight }
                        }, PARENT_IQ_MARKER_MARKERS_PANEL_MORE_SETTINGS);

                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = $"0.6353186 0.5560784", AnchorMax = $"1 0.661219" },
                            Button = { Command = CommandDamageText, Color = HexToRustFormat(ColorDamageText) },
                            Text = { Text = lang.GetMessage(LangDamageText, this, player.UserIDString), Align = TextAnchor.MiddleCenter }
                        }, PARENT_IQ_MARKER_MARKERS_PANEL_MORE_SETTINGS);

                        container.Add(new CuiLabel
                        {
                            RectTransform = { AnchorMin = "0.3500522 0.4135545", AnchorMax = "1 0.518694" },
                            Text = { Text = String.Format(lang.GetMessage("UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_TITLE_WOUNDED_ICO", this, player.UserIDString), HealthBar.GeneralSetting.DisplayName), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleRight }
                        }, PARENT_IQ_MARKER_MARKERS_PANEL_MORE_SETTINGS);

                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = $"0.6353186 0.3060766", AnchorMax = $"1 0.4112158" },
                            Button = { Command = CommandWoundedText, Color = HexToRustFormat(ColorWounded) },
                            Text = { Text = lang.GetMessage(LangWounded, this, player.UserIDString), Align = TextAnchor.MiddleCenter }
                        }, PARENT_IQ_MARKER_MARKERS_PANEL_MORE_SETTINGS);

                        break;
                    }
                case MarkerType.DamageText:
                    {
                        var DamageText = config.MarkesSettings.DamageTextSetting;

                        string ColorWounded = !String.IsNullOrWhiteSpace(DamageText.PermissionsDamageTextWounded) ? permission.UserHasPermission(player.UserIDString, DamageText.PermissionsDamageTextWounded) ? Data.DamageTextMore.TurnWounded ? InterfaceMarker.HexTypeMarkerTaking : InterfaceMarker.HexTypeMarkerAcces : InterfaceMarker.HexTypeMarkerDeading : InterfaceMarker.HexTypeMarkerDeading;
                        string LangWounded = !String.IsNullOrWhiteSpace(DamageText.PermissionsDamageTextWounded) ? permission.UserHasPermission(player.UserIDString, DamageText.PermissionsDamageTextWounded) ? Data.DamageTextMore.TurnWounded ? "UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_NO" : "UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_YES" : "UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_DEADING" : "UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_DEADING";
                        string CommandWoundedText = !String.IsNullOrWhiteSpace(DamageText.PermissionsDamageTextWounded) ? permission.UserHasPermission(player.UserIDString, DamageText.PermissionsDamageTextWounded) ? "hit ui setting_marker damage_text wounded_turn" : "" : "";

                        container.Add(new CuiLabel
                        {
                            RectTransform = { AnchorMin = "0.3500522 0.6658914", AnchorMax = "1 0.771032" },
                            Text = { Text = String.Format(lang.GetMessage("UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_TITLE_WOUNDED_ICO", this, player.UserIDString), DamageText.GeneralSetting.DisplayName), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleRight }
                        }, PARENT_IQ_MARKER_MARKERS_PANEL_MORE_SETTINGS);

                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = $"0.6353186 0.5560784", AnchorMax = $"1 0.661219" },
                            Button = { Command = CommandWoundedText, Color = HexToRustFormat(ColorWounded) },
                            Text = { Text = lang.GetMessage(LangWounded, this, player.UserIDString), Align = TextAnchor.MiddleCenter }
                        }, PARENT_IQ_MARKER_MARKERS_PANEL_MORE_SETTINGS);
                        break;
                    }
                case MarkerType.Icon:
                    {
                        var Icon = config.MarkesSettings.IconSetting;

                        string ColorDamageText = !String.IsNullOrWhiteSpace(Icon.PermissionsIconDamageText) ? permission.UserHasPermission(player.UserIDString, Icon.PermissionsIconDamageText) ? Data.IconMore.TurnDamageText ? InterfaceMarker.HexTypeMarkerTaking : InterfaceMarker.HexTypeMarkerAcces : InterfaceMarker.HexTypeMarkerDeading : InterfaceMarker.HexTypeMarkerDeading;
                        string ColorWounded = !String.IsNullOrWhiteSpace(Icon.PermissionsIconWounded) ? permission.UserHasPermission(player.UserIDString, Icon.PermissionsIconWounded) ? Data.IconMore.TurnWounded ? InterfaceMarker.HexTypeMarkerTaking : InterfaceMarker.HexTypeMarkerAcces : InterfaceMarker.HexTypeMarkerDeading : InterfaceMarker.HexTypeMarkerDeading;

                        string LangDamageText = !String.IsNullOrWhiteSpace(Icon.PermissionsIconDamageText) ? permission.UserHasPermission(player.UserIDString, Icon.PermissionsIconDamageText) ? Data.IconMore.TurnDamageText ? "UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_NO" : "UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_YES" : "UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_DEADING" : "UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_DEADING";
                        string LangWounded = !String.IsNullOrWhiteSpace(Icon.PermissionsIconWounded) ? permission.UserHasPermission(player.UserIDString, Icon.PermissionsIconWounded) ? Data.IconMore.TurnWounded ? "UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_NO" : "UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_YES" : "UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_DEADING" : "UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_DEADING";

                        string CommandDamageText = !String.IsNullOrWhiteSpace(Icon.PermissionsIconDamageText) ? permission.UserHasPermission(player.UserIDString, Icon.PermissionsIconDamageText) ? "hit ui setting_marker icon damagetext_turn" : "" : "";
                        string CommandWoundedText = !String.IsNullOrWhiteSpace(Icon.PermissionsIconWounded) ? permission.UserHasPermission(player.UserIDString, Icon.PermissionsIconWounded) ? "hit ui setting_marker icon wounded_turn" : "" : "";


                        container.Add(new CuiLabel
                        {
                            RectTransform = { AnchorMin = "0.3500522 0.6658914", AnchorMax = "1 0.771032" },
                            Text = { Text = String.Format(lang.GetMessage("UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_TITLE_DAMAGE_TEXT", this, player.UserIDString), Icon.GeneralSetting.DisplayName), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleRight }
                        }, PARENT_IQ_MARKER_MARKERS_PANEL_MORE_SETTINGS);

                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = $"0.6353186 0.5560784", AnchorMax = $"1 0.661219" },
                            Button = { Command = CommandDamageText, Color = HexToRustFormat(ColorDamageText) },
                            Text = { Text = lang.GetMessage(LangDamageText, this, player.UserIDString), Align = TextAnchor.MiddleCenter }
                        }, PARENT_IQ_MARKER_MARKERS_PANEL_MORE_SETTINGS);

                        container.Add(new CuiLabel
                        {
                            RectTransform = { AnchorMin = "0.3500522 0.4135545", AnchorMax = "1 0.518694" },
                            Text = { Text = String.Format(lang.GetMessage("UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_TITLE_WOUNDED_ICO", this, player.UserIDString), Icon.GeneralSetting.DisplayName), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleRight }
                        }, PARENT_IQ_MARKER_MARKERS_PANEL_MORE_SETTINGS);

                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = $"0.6353186 0.3060766", AnchorMax = $"1 0.4112158" },
                            Button = { Command = CommandWoundedText, Color = HexToRustFormat(ColorWounded) },
                            Text = { Text = lang.GetMessage(LangWounded, this, player.UserIDString), Align = TextAnchor.MiddleCenter }
                        }, PARENT_IQ_MARKER_MARKERS_PANEL_MORE_SETTINGS);

                        container.Add(new CuiLabel
                        {
                            RectTransform = { AnchorMin = "0.3500522 0.1799077", AnchorMax = "1 0.2850475" },
                            Text = { Text = String.Format(lang.GetMessage("UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_TITLE_ICOLIST_ICO", this, player.UserIDString)), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleRight }
                        }, PARENT_IQ_MARKER_MARKERS_PANEL_MORE_SETTINGS);

                        for (int i = 0; i < Icon.IconList.Count; i++)
                        {
                            string Image = $"ICON_{Icon.IconList[i].PNG}_{i}";

                            container.Add(new CuiElement
                            {
                                Parent = PARENT_IQ_MARKER_MARKERS_PANEL_MORE_SETTINGS,
                                Name = $"ICON_{i}",
                                Components =
                                {
                                    new CuiRawImageComponent { Png = GetImage(Image), Color = HexToRustFormat("#FFFFFFFF") },
                                    new CuiRectTransformComponent{ AnchorMin = $"{0.9226741 - (i * 0.08)} 0.02570126", AnchorMax = $"{0.9895497 - (i * 0.08)} 0.1752347"},
                                }
                            });
                            if (permission.UserHasPermission(player.UserIDString, Icon.IconList[i].Permissions) || String.IsNullOrWhiteSpace(Icon.IconList[i].Permissions))
                            {
                                container.Add(new CuiButton
                                {
                                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                                    Button = { Command = $"hit ui setting_marker icon marker_icon_set {i}", Color = "0 0 0 0" },
                                    Text = { Text = "", Align = TextAnchor.MiddleCenter }
                                }, $"ICON_{i}");
                            }
                            else
                            {
                                container.Add(new CuiButton
                                {
                                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                                    Button = { Command = "", Color = HexToRustFormat("#C34545FF"), Sprite = "assets/icons/lock.png" },
                                    Text = { Text = "", Align = TextAnchor.MiddleCenter }
                                }, $"ICON_{i}");
                            }
                            if (i == 13) break;
                        }
                        break;
                    }
            }
            

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Update Marker Show

        #region Resize Icon
        public readonly Dictionary<TypeSize, ResizeClass> Resizer = new Dictionary<TypeSize, ResizeClass>
        {  
            [TypeSize.Big] = new ResizeClass
            {
                AnchorMin = "0.157292 0.8950544",
                AnchorMax = "0.1906253 0.9722558",
                FontSize = 25,
                healthBarCoordinate = new ResizeClass.HealthBar
                {
                    AnchorMin = "0.1609375 0.8950544",
                    AnchorMax = "0.1661458 0.9722558"
                }
            },
            [TypeSize.Middle] = new ResizeClass
            {
                AnchorMin = "0.1598962 0.9059109",
                AnchorMax = "0.1848962 0.963812",
                FontSize = 20,
                healthBarCoordinate = new ResizeClass.HealthBar
                {
                    AnchorMin = "0.1609375 0.9034982",
                    AnchorMax = "0.1645833 0.9613993"
                }
            },
            [TypeSize.Little] = new ResizeClass
            {
                AnchorMin = "0.1625004 0.9119423",
                AnchorMax = "0.1791671 0.950543",
                FontSize = 15,
                healthBarCoordinate = new ResizeClass.HealthBar
                {
                    AnchorMin = "0.1609375 0.9107358",
                    AnchorMax = "0.1625 0.9493366"
                }
            },
        };
        public class ResizeClass
        {
            public HealthBar healthBarCoordinate;
            public string AnchorMin;
            public string AnchorMax;
            public int FontSize;
            internal class HealthBar
            {
                public string AnchorMin;
                public string AnchorMax;
            }
        }
        #endregion

        public void Interface_Update_Marker_Show(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "MARKER_UPDATE_ME");
            CuiHelper.DestroyUi(player, "DAMAGE_TEXT_TURN");
            CuiHelper.DestroyUi(player, "WOUNDED_TURN_ICO_TEXT");
            CuiHelper.DestroyUi(player, "WOUNDED_TURN_ICO");
            var Data = DataInformation[player.userID];
            var Interface = config.SettingsInterfaces;
            string HexMarker = Data.HexMarker;

            if (!DataInformation[player.userID].TurnMarker)
            {
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.1598962 0.8769602", AnchorMax = "0.4145834 0.9903497" },
                    Text = { Text = lang.GetMessage("UI_MARKER_PANEL_SHOW_MARKER_OFF_TURN",this,player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
                }, PARENT_IQ_MARKER_MARKERS_PANEL, "MARKER_UPDATE_ME");

                CuiHelper.AddUi(player, container);
                return;
            }

            switch (Data.markerType)
            {
                case MarkerType.DamageText:
                    {
                        container.Add(new CuiLabel
                        {
                            RectTransform = { AnchorMin = Resizer[Data.typeSize].AnchorMin, AnchorMax = Resizer[Data.typeSize].AnchorMax },
                            Text = { Text = Math.Round(player.health).ToString(), FontSize = Resizer[Data.typeSize].FontSize, Color = HexToRustFormat(HexMarker), Font = "robotocondensed-bold.ttf", Align = TextAnchor.LowerCenter }
                        }, PARENT_IQ_MARKER_MARKERS_PANEL, "MARKER_UPDATE_ME");

                        if (Data.DamageTextMore.TurnWounded)
                        {
                            container.Add(new CuiElement
                            {
                                Parent = PARENT_IQ_MARKER_MARKERS_PANEL,
                                Name = "WOUNDED_TURN_ICO",
                                Components =
                                                     {
                                                         new CuiImageComponent { Sprite = "assets/icons/fall.png" },
                                                         new CuiRectTransformComponent{ AnchorMin = "0.1911466 0.9372742", AnchorMax = "0.2036465 0.9662247" },
                                                     }
                            });

                            container.Add(new CuiLabel
                            {
                                RectTransform = { AnchorMin = "0.20625 0.9312425", AnchorMax = "0.4229166 0.9698432" },
                                Text = { Text = lang.GetMessage("UI_ELEMENT_MORE_MARKER_WOUNDED", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
                            }, PARENT_IQ_MARKER_MARKERS_PANEL, "WOUNDED_TURN_ICO_TEXT");
                        }
                        break;
                    }
                case MarkerType.HealthBar:
                    {
                        container.Add(new CuiPanel
                        {
                            RectTransform = { AnchorMin = Resizer[Data.typeSize].healthBarCoordinate.AnchorMin, AnchorMax = Resizer[Data.typeSize].healthBarCoordinate.AnchorMax },
                            Image = { Color = HexToRustFormat(HexMarker + "5D") }
                        }, PARENT_IQ_MARKER_MARKERS_PANEL, "MARKER_UPDATE_ME");

                        container.Add(new CuiPanel
                        {
                            RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 0.{Math.Round(player.health)}", OffsetMin = "0.8 0.8", OffsetMax = "-1 0.8" },
                            Image = { Color = HexToRustFormat(HexMarker) }
                        }, "MARKER_UPDATE_ME");

                        if (Data.HealthBarMore.TurnDamageText)
                        {
                            container.Add(new CuiLabel
                            {
                                RectTransform = { AnchorMin = "0.1921883 0.9022918", AnchorMax = "0.4255208 0.9408925" },
                                Text = { Text = String.Format(lang.GetMessage("UI_ELEMENT_MORE_MARKER_DAMAGE_TEXT", this, player.UserIDString), Math.Round(player.health)), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
                            }, PARENT_IQ_MARKER_MARKERS_PANEL, "DAMAGE_TEXT_TURN");
                        }
                        if (Data.HealthBarMore.TurnWounded)
                        {
                            container.Add(new CuiElement
                            {
                                Parent = PARENT_IQ_MARKER_MARKERS_PANEL,
                                Name = "WOUNDED_TURN_ICO",
                                Components =
                                                     {
                                                         new CuiImageComponent { Sprite = "assets/icons/fall.png" },
                                                         new CuiRectTransformComponent{ AnchorMin = "0.1911466 0.9372742", AnchorMax = "0.2036465 0.9662247" },
                                                     }
                            });

                            container.Add(new CuiLabel
                            {
                                RectTransform = { AnchorMin = "0.20625 0.9312425", AnchorMax = "0.4229166 0.9698432" },
                                Text = { Text = lang.GetMessage("UI_ELEMENT_MORE_MARKER_WOUNDED", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
                            }, PARENT_IQ_MARKER_MARKERS_PANEL, "WOUNDED_TURN_ICO_TEXT");
                        }

                        break;
                    }
                case MarkerType.Icon:
                    {
                        string MarkerKey = "ICON_{0}_{1}";
                        string Marker = String.Format(MarkerKey, config.MarkesSettings.IconSetting.IconList[Data.IconMore.IndexMarker].PNG, Data.IconMore.IndexMarker);

                        container.Add(new CuiElement
                        {
                            Parent = PARENT_IQ_MARKER_MARKERS_PANEL,
                            Name = $"MARKER_UPDATE_ME",
                            Components =
                                                     {
                                                         new CuiRawImageComponent { Png = GetImage(Marker), Color = HexToRustFormat(HexMarker) },
                                                         new CuiRectTransformComponent{ AnchorMin = Resizer[Data.typeSize].AnchorMin, AnchorMax = Resizer[Data.typeSize].AnchorMax },
                                                     }
                        });
                        if(Data.IconMore.TurnDamageText)
                        {
                            container.Add(new CuiLabel
                            {
                                RectTransform = { AnchorMin = "0.1921883 0.9022918", AnchorMax = "0.4255208 0.9408925" },
                                Text = { Text = String.Format(lang.GetMessage("UI_ELEMENT_MORE_MARKER_DAMAGE_TEXT",this,player.UserIDString), Math.Round(player.health)), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
                            },  PARENT_IQ_MARKER_MARKERS_PANEL,"DAMAGE_TEXT_TURN");
                        }
                        if (Data.IconMore.TurnWounded) 
                        {
                            container.Add(new CuiElement
                            {
                                Parent = PARENT_IQ_MARKER_MARKERS_PANEL,
                                Name = "WOUNDED_TURN_ICO",
                                Components =
                                                     {
                                                         new CuiImageComponent { Sprite = "assets/icons/fall.png" },
                                                         new CuiRectTransformComponent{ AnchorMin = "0.1911466 0.9372742", AnchorMax = "0.2036465 0.9662247" },
                                                     }
                            });

                            container.Add(new CuiLabel
                            {
                                RectTransform = { AnchorMin = "0.20625 0.9312425", AnchorMax = "0.4229166 0.9698432" },
                                Text = { Text = lang.GetMessage("UI_ELEMENT_MORE_MARKER_WOUNDED", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
                            }, PARENT_IQ_MARKER_MARKERS_PANEL, "WOUNDED_TURN_ICO_TEXT");
                        }

                        break;
                    }
            }

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #endregion

        #endregion

        #region Interface Attack Hit
        public class ResizerAttackHit
        {
            public readonly Dictionary<TypeSize, TypeResize> ResizerAttack = new Dictionary<TypeSize, TypeResize>
            {
                [TypeSize.Big] = new TypeResize
                {
                    IconParams = new TypeResize.Params
                    {
                        AnchorMin = "0.4833336 0.4666665",
                        AnchorMax = "0.5166664 0.5259269",
                    },
                    HealthBar = new TypeResize.Params
                    {
                        AnchorMin = "0.4359384 0.4731481",
                        AnchorMax = "0.4406259 0.5277778",
                    },
                    FontSizeText = 20,
                },
                [TypeSize.Middle] = new TypeResize
                {
                    IconParams = new TypeResize.Params
                    {
                        AnchorMin = "0.4875002 0.473148",
                        AnchorMax = "0.5125002 0.5175922"
                    },
                    HealthBar = new TypeResize.Params
                    {
                        AnchorMin = "0.43698 0.4805555",
                        AnchorMax = "0.4395834 0.5166664",
                    },
                    FontSizeText = 15,
                },
                [TypeSize.Little] = new TypeResize
                {
                    IconParams = new TypeResize.Params
                    {
                        AnchorMin = "0.4916668 0.4796295",
                        AnchorMax = "0.5083334 0.509259"
                    },
                    HealthBar = new TypeResize.Params
                    {
                        AnchorMin = "0.4375008 0.487037",
                        AnchorMax = "0.4390625 0.5101849",
                    },
                    FontSizeText = 10,
                }
            };
            internal class TypeResize
            {
                public Params IconParams = new Params();
                public Params HealthBar = new Params();
                public int FontSizeText;

                internal class Params
                {
                    public string AnchorMin;
                    public string AnchorMax;
                }
            }
        }

        public void Interface_Attack_Hit(BasePlayer player, HitInfo targetInfo)
        {
            var Data = DataInformation[player.userID];
            if (!Data.TurnMarker) return;
            if (targetInfo == null || targetInfo.Initiator == null || targetInfo.HitEntity == null) return;
            try
            {
                string HexMarker = Data.HexMarker;
                var Interface = config.SettingsInterfaces;

                ResizerAttackHit resizerAttackHit = new ResizerAttackHit();
                var ResizeIcon = resizerAttackHit.ResizerAttack[Data.typeSize].IconParams;
                int FontSize = resizerAttackHit.ResizerAttack[Data.typeSize].FontSizeText;
                var ResizeHealthBar = resizerAttackHit.ResizerAttack[Data.typeSize].HealthBar;

                string DamageCount = String.Empty;
                float HealtSave = targetInfo.HitEntity.Health();
                NextTick(() =>
                {

                    CuiElementContainer container = new CuiElementContainer();
                    CuiHelper.DestroyUi(player, PARENT_IQ_MARKER_ATTACK_HIT);

                    DamageCount = targetInfo.Initiator is BasePlayer ? targetInfo.HitEntity is BasePlayer ? IsFriends(player.userID, targetInfo.HitEntity.ToPlayer().userID) ? lang.GetMessage("UI_ELEMENT_MORE_MARKER_WOUNDED_HIT_ATTACK_FRIENDS", this, player.UserIDString) : (HealtSave - targetInfo.HitEntity.Health()) <= 0 ? lang.GetMessage("UI_ATTAKER_KILLEN", this, player.UserIDString) : targetInfo.isHeadshot ? lang.GetMessage("UI_ELEMENT_MORE_MARKER_WOUNDED_HIT_ATTACK_HEADSHOT", this, player.UserIDString)
                : (HealtSave - targetInfo.HitEntity.Health()).ToString("F0") : (HealtSave - targetInfo.HitEntity.Health()).ToString("F0") : (HealtSave - targetInfo.HitEntity.Health()).ToString("F0");

                    switch (Data.markerType)
                    {
                        case MarkerType.Icon:
                            {
                                string MarkerKey = "ICON_{0}_{1}";
                                string Marker = String.Format(MarkerKey, config.MarkesSettings.IconSetting.IconList[Data.IconMore.IndexMarker].PNG, Data.IconMore.IndexMarker);

                                container.Add(new CuiElement
                                {
                                    Parent = "Overlay",
                                    Name = PARENT_IQ_MARKER_ATTACK_HIT,
                                    Components =
                                                         {
                                                         new CuiRawImageComponent { Png = GetImage(Marker), Color = HexToRustFormat(HexMarker) },
                                                         new CuiRectTransformComponent{ AnchorMin = ResizeIcon.AnchorMin, AnchorMax = ResizeIcon.AnchorMax },
                                                         }
                                });

                                if (Data.IconMore.TurnWounded)
                                    if (targetInfo.HitEntity is BasePlayer)
                                    {
                                        var target = targetInfo.HitEntity as BasePlayer;
                                        if (target.IsWounded() && !target.IsDead())
                                        {
                                            container.Add(new CuiElement
                                            {
                                                Parent = PARENT_IQ_MARKER_ATTACK_HIT,
                                                Components =
                                                         {
                                                         new CuiImageComponent { Sprite = "assets/icons/fall.png" },
                                                         new CuiRectTransformComponent{ AnchorMin = $"1 1", AnchorMax = $"1 1", OffsetMin = "10 -25", OffsetMax = "30 -5" },
                                                         }
                                            });
                                        }
                                    }

                                if (Data.IconMore.TurnDamageText)
                                {
                                    container.Add(new CuiLabel
                                    {
                                        RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0 0", OffsetMin = "-100 0", OffsetMax = "-10 30" },
                                        Text = { Text = $"<b>{DamageCount}</b>", Color = HexToRustFormat(Interface.HexLabels), FontSize = FontSize, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleRight }
                                    }, PARENT_IQ_MARKER_ATTACK_HIT);
                                }

                                break;
                            }
                        case MarkerType.DamageText:
                            {
                                container.Add(new CuiLabel
                                {
                                    RectTransform = { AnchorMin = "0.4651042 0.4666665", AnchorMax = "0.5390625 0.5259269" },
                                    Text = { Text = $"<b>{DamageCount}</b>", Color = HexToRustFormat(HexMarker), FontSize = FontSize, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                                }, "Overlay", PARENT_IQ_MARKER_ATTACK_HIT);

                                if (Data.DamageTextMore.TurnWounded)
                                    if (targetInfo.HitEntity is BasePlayer)
                                    {
                                        var target = targetInfo.HitEntity as BasePlayer;
                                        if (target.IsWounded() && !target.IsDead())
                                        {
                                            container.Add(new CuiElement
                                            {
                                                Parent = PARENT_IQ_MARKER_ATTACK_HIT,
                                                Components =
                                                         {
                                                         new CuiImageComponent { Sprite = "assets/icons/fall.png" },
                                                         new CuiRectTransformComponent{ AnchorMin = $"1 1", AnchorMax = $"1 1", OffsetMin = "10 -25", OffsetMax = "30 -5" },
                                                         }
                                            });
                                        }
                                    }

                                break;
                            }
                        case MarkerType.HealthBar:
                            {
                                container.Add(new CuiPanel
                                {
                                    RectTransform = { AnchorMin = ResizeHealthBar.AnchorMin, AnchorMax = ResizeHealthBar.AnchorMax },
                                    Image = { Color = HexToRustFormat(HexMarker + "5D") }
                                }, "Overlay", PARENT_IQ_MARKER_ATTACK_HIT);

                                float MaxHealt = targetInfo.HitEntity == null ? 0 : targetInfo.HitEntity.MaxHealth();
                                container.Add(new CuiPanel
                                {
                                    RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.8 {targetInfo.HitEntity.Health() / MaxHealt}", OffsetMin = "0.8 0.8", OffsetMax = "0.1 0.8" },
                                    Image = { Color = HexToRustFormat(HexMarker) }
                                }, PARENT_IQ_MARKER_ATTACK_HIT);

                                if (Data.HealthBarMore.TurnDamageText)
                                {
                                    container.Add(new CuiLabel
                                    {
                                        RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0 0", OffsetMin = "-100 0", OffsetMax = "-10 30" },
                                        Text = { Text = $"<b>{DamageCount}</b>", Color = HexToRustFormat(Interface.HexLabels), FontSize = FontSize, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleRight }
                                    }, PARENT_IQ_MARKER_ATTACK_HIT);
                                }

                                if (Data.HealthBarMore.TurnWounded)
                                    if (targetInfo.HitEntity is BasePlayer)
                                    {
                                        var target = targetInfo.HitEntity as BasePlayer;
                                        if (target.IsWounded() && !target.IsDead())
                                        {
                                            container.Add(new CuiElement
                                            {
                                                Parent = PARENT_IQ_MARKER_ATTACK_HIT,
                                                Components =
                                                         {
                                                         new CuiImageComponent { Sprite = "assets/icons/fall.png" },
                                                         new CuiRectTransformComponent{ AnchorMin = $"1 1", AnchorMax = $"1 1", OffsetMin = "10 -25", OffsetMax = "30 -5" },
                                                         }
                                            });
                                        }
                                    }
                                break;
                            }
                    }

                    CuiHelper.AddUi(player, container);
                });
                LogToFile("iqmarkerdebug", $"DamageCount = {DamageCount} HealtSave {HealtSave} Entity = {targetInfo.ProjectilePrefab} Entity#2 = {targetInfo.HitEntity.PrefabName}", this);


                timer.Once(0.5f, () =>
                    {
                        CuiHelper.DestroyUi(player, PARENT_IQ_MARKER_ATTACK_HIT);
                    });
            }
            catch(Exception ex)
            {
                LogToFile("iqmarkerdebug", $"{ex}", this);
            }
        }

        #endregion

        #region Helps
        private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
        #endregion

        #endregion

        #region Lang
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_TITLE_DESCRIPTION"] = "<size=15>CHOOSE YOURSELF</size>",
                ["UI_TITLE_MAIN"] = "<size=50><b>MARKER</b></size>",
                ["UI_CLOSE_BTN"] = "<size=30><b>CLOSE</b></size>",
                ["UI_TURN_SKILL_ON"] = "<size=20><b><color=#93C346FF>ON MARKER</color></b></size>",
                ["UI_TURN_SKILL_OFF"] = "<size=20><b><color=#FF5331FF>OFF MARKER</color></b></size>",

                ["UI_DETAL_INFO_TITLE"] = "<b><size=14>YOU CAN CUSTOMIZE THE MARKER MORE DETAILS</size></b>",
                ["UI_SETTING_BUTTON"] = "<b><size=15>SETTING MARKER</size></b>",

                ["UI_SETTING_TYPE_SHOW"] = "<b><size=17>CHOOSE WHERE YOU WILL HAVE A MARKER TO BE DISPLAYED</size></b>",
                ["UI_SETTING_HEX_SHOW"] = "<b><size=17>ENTER YOUR OWN COLOR IN HEX FOR MARKER</size></b>",
                ["UI_SETTING_HEX_SHOW_YOUR"] = "<b><size=20>YOUR HEX :</size></b>",

                ["UI_SETTINGS_TYPE_ANIMAL"] = "<size=12><b>ANIMAL</b></size>",
                ["UI_SETTINGS_TYPE_PLAYER"] = "<size=12><b>PLAYER</b></size>",
                ["UI_SETTINGS_TYPE_BUILDING"] = "<size=12><b>BUILDING</b></size>",
                ["UI_SETTINGS_TYPE_ENTITY"] = "<size=12><b>ENTITY</b></size>",
                ["UI_SETTINGS_TYPE_TRANSPORT"] = "<size=12><b>TRANSPORT</b></size>",
                ["UI_SETTINGS_TYPE_NPC"] = "<size=12><b>SCIENTIEST</b></size>",

                ["UI_MARKER_PANEL_SHOW_MARKER"] = "<size=50><b>MARKERK:</b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_OFF_TURN"] = "<size=35><b><color=#FF5331FF>MARKER TURN OFF</color></b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_SIZE"] = "<size=17><b>CHOOSE ONE OF THE AVAILABLE SIZES FOR THE MARKER</b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_COLOR"] = "<size=17><b>CHOOSE ONE OF THE AVAILABLE COLORS FOR THE MARKER</b></size>",

                ["UI_MARKER_PANEL_SHOW_MARKER_SIZE_LITTLE"] = "<size=18>LITTLE</size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_SIZE_MIDDLE"] = "<size=18>MIDDLE</size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_SIZE_BIG"] = "<size=18>BIG</size>",

                ["UI_ATTAKER_KILLEN"] = "KILLED",

                ["UI_MARKER_PANEL_SHOW_MARKER_LIST_TITLE"] = "<size=20><b>CHOOSE A CONVENIENT MARKER TYPE </b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_BTN_DEADING"] = "<size=15><b>NOT AVAILABLE FOR SELECTION</b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_BTN_TAKING"] = "<size=15><b>SELECTED TYPE</b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_BTN_ACCES"] = "<size=15><b>SELECT THIS TYPE</b></size>",

                ["UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_TITLE"] = "<size=20><b>YOU CAN CHOOSE AN ADDITIONAL SETTING</b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_DESCRIPTION"] = "<size=12><b>SET YOUR OWN MARKER AS YOU WILL BE COMFORTABLE</b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_TITLE_DAMAGE_TEXT"] = "<size=16><b>DISPLAY DAMAGE NEAR THE {0}</b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_TITLE_WOUNDED_ICO"] = "<size=16><b>DISPLAYING THE FALL ICON WHEN THE PLAYER IS FALLING</b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_TITLE_ICOLIST_ICO"] = "<size=16><b>DISPLAYING THE MARKER ICON</b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_YES"] = "<size=18><b>TURN ON</b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_NO"] = "<size=18><b>TURN OFF</b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_DEADING"] = "<size=18><b>NO ACCESS</b></size>",

                ["UI_ELEMENT_MORE_MARKER_DAMAGE_TEXT"] = "<size=13>{0} - damage done near markerr</size>",
                ["UI_ELEMENT_MORE_MARKER_WOUNDED"] = "<size=13>- player fall display </size>",
                ["UI_ELEMENT_MORE_MARKER_WOUNDED_HIT_ATTACK_HEADSHOT"] = "<color=#CC3300><b>HEADSHOT</b></color>",
                ["UI_ELEMENT_MORE_MARKER_WOUNDED_HIT_ATTACK_FRIENDS"] = "<color=#4EB145FF><b>FRIENDS</b></color>",

            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_TITLE_DESCRIPTION"] = "<size=15>ВЫБЕРИТЕ СЕБЕ УДОБНЫЙ</size>",
                ["UI_TITLE_MAIN"] = "<size=50><b>МАРКЕР</b></size>",
                ["UI_CLOSE_BTN"] = "<size=30><b>ЗАКРЫТЬ</b></size>",
                ["UI_TURN_SKILL_ON"] = "<size=20><b><color=#93C346FF>ВКЛЮЧИТЬ МАРКЕР</color></b></size>",
                ["UI_TURN_SKILL_OFF"] = "<size=20><b><color=#FF5331FF>ВЫКЛЮЧИТЬ МАРКЕР</color></b></size>",

                ["UI_DETAL_INFO_TITLE"] = "<b><size=14>ВЫ МОЖЕТЕ НАСТРОИТЬ МАРКЕР БОЛЕЕ ДЕТАЛЬНО</size></b>",
                ["UI_SETTING_BUTTON"] = "<b><size=15>НАСТРОИТЬ МАРКЕР</size></b>",

                ["UI_SETTING_TYPE_SHOW"] = "<b><size=17>ВЫБЕРИТЕ ПРИ ПОПАДАНИИ В КОГО БУДЕТ ОТОБРАЖАТЬСЯ МАРКЕР</size></b>",
                ["UI_SETTING_HEX_SHOW"] = "<b><size=17>ВВЕДИТЕ СОБСТВЕННЫЙ ЦВЕТ В ФОРМАТЕ HEX ДЛЯ МАРКЕРА</size></b>",
                ["UI_SETTING_HEX_SHOW_YOUR"] = "<b><size=20>ВАШ HEX :</size></b>",

                ["UI_SETTINGS_TYPE_ANIMAL"] = "<size=12><b>ЖИВОТНЫЕ</b></size>",
                ["UI_SETTINGS_TYPE_PLAYER"] = "<size=12><b>ИГРОКИ</b></size>",
                ["UI_SETTINGS_TYPE_BUILDING"] = "<size=12><b>ПОСТРОЙКИ</b></size>",
                ["UI_SETTINGS_TYPE_ENTITY"] = "<size=12><b>ПРЕДМЕТЫ</b></size>",
                ["UI_SETTINGS_TYPE_TRANSPORT"] = "<size=12><b>ТРАНСПОРТ</b></size>",
                ["UI_SETTINGS_TYPE_NPC"] = "<size=12><b>УЧЕНЫЕ</b></size>",

                ["UI_MARKER_PANEL_SHOW_MARKER"] = "<size=50><b>МАРКЕР:</b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_OFF_TURN"] = "<size=35><b><color=#FF5331FF>МАРКЕР ВЫКЛЮЧЕН</color></b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_SIZE"] = "<size=17><b>ВЫБЕРИТЕ ОДИН ИЗ ДОСТУПНЫХ РАЗМЕРОВ ДЛЯ МАРКЕРА</b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_COLOR"] = "<size=17><b>ВЫБЕРИТЕ ОДИН ИЗ ДОСТУПНЫХ ЦВЕТОВ ДЛЯ МАРКЕРА</b></size>",

                ["UI_MARKER_PANEL_SHOW_MARKER_SIZE_LITTLE"] = "<size=18>МАЛЕНЬКИЙ</size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_SIZE_MIDDLE"] = "<size=18>СРЕДНИЙ</size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_SIZE_BIG"] = "<size=18>БОЛЬШОЙ</size>",

                ["UI_ATTAKER_KILLEN"] = "УБИТ",

                ["UI_MARKER_PANEL_SHOW_MARKER_LIST_TITLE"] = "<size=20><b>ВЫБЕРИТЕ УДОБНЫЙ ТИП МАРКЕРА</b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_BTN_DEADING"] = "<size=15><b>НЕДОСТУПЕН ДЛЯ ВЫБОРА</b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_BTN_TAKING"] = "<size=15><b>ВЫБРАННЫЙ ТИП</b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_BTN_ACCES"] = "<size=15><b>ВЫБРАТЬ ДАННЫЙ ТИП</b></size>",

                ["UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_TITLE"] = "<size=20><b>ВЫ МОЖЕТЕ ВЫБРАТЬ ДОПОЛНИТЕЛЬНУЮ НАСТРОЙКУ</b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_DESCRIPTION"] = "<size=12><b>НАСТРОЙТЕ СВОЙ МАРКЕР ТАК,КАК ВАМ БУДЕТ УДОБНО</b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_TITLE_DAMAGE_TEXT"] = "<size=16><b>ОТОБРАЖЕНИЕ УРОНА РЯДОМ С {0}</b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_TITLE_WOUNDED_ICO"] = "<size=16><b>ОТОБРАЖЕНИЕ ИКОНКИ ПАДЕНИЯ ПРИ ПАДЕНИИ ИГРОКА</b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_TITLE_ICOLIST_ICO"] = "<size=16><b>ОТОБРАЖЕНИЕ ИКОНКИ МАРКЕРА</b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_YES"] = "<size=18><b>ВКЛЮЧИТЬ</b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_NO"] = "<size=18><b>ВЫКЛЮЧИТЬ</b></size>",
                ["UI_MARKER_PANEL_SHOW_MARKER_MORE_SETTINGS_BTN_DEADING"] = "<size=18><b>НЕДОСТУПНО</b></size>",

                ["UI_ELEMENT_MORE_MARKER_DAMAGE_TEXT"] = "<size=13>{0} - нанесенный урон возле маркера</size>",
                ["UI_ELEMENT_MORE_MARKER_WOUNDED"] = "<size=13>- отображение падения игрока</size>",
                ["UI_ELEMENT_MORE_MARKER_WOUNDED_HIT_ATTACK_HEADSHOT"] = "<color=#CC3300><b>ГОЛОВА</b></color>",
                ["UI_ELEMENT_MORE_MARKER_WOUNDED_HIT_ATTACK_FRIENDS"] = "<color=#4EB145FF><b>ДРУГ</b></color>",
            }, this, "ru");
            PrintWarning("Языковой файл загружен успешно");
        }
        #endregion
    }
}
