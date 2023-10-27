using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using ConVar;
using System.Linq;
using Oxide.Core;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Oxide.Plugins
{
    [Info("IQFakeActive", "qxzxf", "1.2.15")]
    [Description("Актив вашего сервера, но немного не тот :)")]
    class IQFakeActive : RustPlugin
    {
        /// </summary>
        /// Обновленеи 1.0.х
        /// - Добавлена возможность выбора вариации генераций онлайна (Автоматическая, ручная, Автоматическая + ваш онлайн)
        /// - Изменен метод генерации онлайна
        /// 
        /// 

        #region Vars
        private enum TypeOnline
        {
            Auto,
            Manual,
            Auto_Plus_Server,
        }
        public int FakeOnline = 0;
        public static DateTime TimeCreatedSave = SaveRestore.SaveCreatedTime.Date;
        public static DateTime RealTime = DateTime.Now.Date;
        public static int SaveCreated = RealTime.Subtract(TimeCreatedSave).Days;
        private Timer timeActivateMessage;
        private Timer timeActivateMessagePm;
        private Timer timerSynh;
        private Timer timerGenerateOnline;
        private Timer timerPlaySounds;
        private Coroutine RoutineInitPlugin;
        private Coroutine RoutineAddAvatars;
        private List<Configuration.ActiveSettings.SounActiveSettings.Sounds> SortedSoundList;

        #endregion

        #region Reference
        [PluginReference] Plugin IQChat, ImageLibrary;

        #region IQChat
        private enum IQChatGetType
        {
            Prefix,
            ChatColor,
            NickColor,
        }
        private string GetInfoIQChat(IQChatGetType TypeInfo)
        {
            if (!IQChat) return String.Empty;
            switch (TypeInfo)
            {
                case IQChatGetType.Prefix:
                    {
                        String Prefix = (String)IQChat?.Call("API_GET_DEFAULT_PREFIX");
                        return Prefix;
                    }
                case IQChatGetType.ChatColor:
                    {
                        String ChatColor = (String)IQChat?.Call("API_GET_DEFAULT_MESSAGE_COLOR");
                        return ChatColor;
                    }
                case IQChatGetType.NickColor:
                    {
                        String NickColor = (String)IQChat?.Call("API_GET_DEFAULT_NICK_COLOR");
                        return NickColor;
                    }
            }
            return "";
        }
        #endregion

        #region Image Library
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        public bool HasImage(string imageName) => (bool)ImageLibrary?.Call("HasImage", imageName);


        #endregion

        #endregion

        #region Configuration 
        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Настройка создания фейковых игроков")]
            public FakePlayerSettings FakePlayers = new FakePlayerSettings();
            [JsonProperty("Настройка актива")]
            public ActiveSettings FakeActive = new ActiveSettings();
            [JsonProperty("Настройка онлайна")]
            public FakeOnlineSettings FakeOnline = new FakeOnlineSettings();
            [JsonProperty("Общая настройка")]
            public GeneralSettings GeneralSetting = new GeneralSettings();
            [JsonProperty("Включить лоигрование действий плагина в консоль")]
            public bool UseLogConsole;
            [JsonProperty("Введите ваш стимКлюч для подгрузки аватарок(https://steamcommunity.com/dev/apikey - взять тут.Если потребует домен на сайте - введите абсолютно любой)")]
            public string APIKeySteam;

            internal class GeneralSettings
            {
                [JsonProperty("IQChat : Steam64ID для аватарки в чате")]
                public String AvatarSteamID;
                [JsonProperty("IQChat : Отображаемый префикс в чате")]
                public String PrefixName;
                [JsonProperty("Максимально допустимый предел фейкового онлайна(если вам не нужно это значение, оставьвте 0 - по умолчанию)")]
                public Int32 MaximalOnline;
            }

            internal class FakeOnlineSettings
            {
                [JsonProperty("Настройка интервала обновления кол-во онлайна(сек)")]
                public int IntervalUpdateOnline;
                [JsonProperty("Детальная настройка типов онлайна")]
                public UpdateOnline SettingsUpdateOnline = new UpdateOnline();
                internal class UpdateOnline
                {
                    [JsonProperty("Настройка типа онлайна (0 - Автоматический, 1 - Ручная настройка, 2 - Автоматический + ваш онлайн)")]
                    public TypeOnline TypeOnline;
                    [JsonProperty("Настройка обновления онлайна")]
                    public StandartFormul StandartFormulSetting = new StandartFormul();
                    [JsonProperty("Ручная настройка онлайна")]
                    public ManualFormul ManualFormule = new ManualFormul();
                    internal class StandartFormul
                    {
                        [JsonProperty("Минимальный множитель онлайна(От этого показателя зависит скачок онлайна при обновлении)")]
                        public float MinimumFactor;
                        [JsonProperty("Максимальный множитель онлайна(От этого показателя зависит скачок онлайна при обновлении)")]
                        public float MaximumFactor;
                        [JsonProperty("Включить зависимость генерации оналйна от времени суток?")]
                        public bool DayTimeGerenation;
                    }
                    internal class ManualFormul
                    {
                        [JsonProperty("Ручная настройка онлайна (будет к вашему онлайну добавлять указанный в списке) | [время(цифра)] = количество онлайна ")]
                        public Dictionary<Int32, Int32> ManualTimeOnline = new Dictionary<Int32, Int32>();
                    }
                }
            }
            internal class FakePlayerSettings
            {
                [JsonProperty("Использовать игроков с общей базы игроков(true - да/false - нет, вы сами будете задавать параметры)")]
                public bool PlayersDB;
                [JsonProperty("Использовать сообщение с общей базы игроков(true - да/false - нет, вы сами будете задавать параметры)")]
                public bool ChatsDB;

                [JsonProperty("Локальный - список ников с которыми будут создаваться игроки(Общая база игроков должна быть отключена)")]
                public List<string> ListNickName = new List<string>();
                [JsonProperty("Локальный - список сообщений которые будут отправляться в чат(Общая база игроков должна быть отключена)")]
                public List<string> ListMessages = new List<string>();
            }
            internal class ActiveSettings
            {
                [JsonProperty("Настройка актива в чате")]
                public ChatActiveSetting ChatActive = new ChatActiveSetting();
                [JsonProperty("Настройка иммитации актива с помощью звуков(будь то рейд, будь то кто-то ходит рядом или добывает)")]
                public SounActiveSettings SoundActive = new SounActiveSettings();
                internal class SounActiveSettings
                {
                    [JsonProperty("Использовать звуки?")]
                    public bool UseLocalSoundBase;
                    [JsonProperty("Минимальный интервал проигрывания звука")]
                    public int MinimumIntervalSound;
                    [JsonProperty("Максимальный интервал проигрывания звука")]
                    public int MaximumIntervalSound;
                    [JsonProperty("Локальный лист звуков и их настройка")]
                    public List<Sounds> SoundLists = new List<Sounds>();
                    public class Sounds
                    {
                        [JsonProperty("Ваш звук")]
                        public string SoundPath;
                        [JsonProperty("Минимальная позиция от игрока")]
                        public int MinPos;
                        [JsonProperty("Максимальная позиция от игрока")]
                        public int MaxPos;
                        [JsonProperty("Шанс проигрывания данного звука")]
                        public int Rare;
                        [JsonProperty("На какой день после WIPE будет отыгрываться данный звук")]
                        public int DayFaktor;
                    }
                }
                internal class ChatActiveSetting
                {
                    [JsonProperty("HEX : Цвет ника для ботов")]
                    public String ColorChatNickDefault;
                    [JsonProperty("IQChat : Настройки подключения и отключения для IQChat")]
                    public IQChatNetwork IQChatNetworkSetting = new IQChatNetwork();
                    [JsonProperty("IQChat : Настройки личных сообщений для IQChat")]
                    public IQChatPM IQChatPMSettings = new IQChatPM();
                    [JsonProperty("Использовать черный список слов")]
                    public bool UseBlackList;
                    [JsonProperty("Укажите слова,которые будут запрещены в чате")]
                    public List<string> BlackList = new List<string>();
                    [JsonProperty("Укажите минимальный интервал отправки сообщения в чат(секунды)")]
                    public int MinimumInterval;
                    [JsonProperty("Укажите максимальный интервал отправки сообщения в чат(секунды)")]
                    public int MaximumInterval;

                    internal class IQChatNetwork
                    {
                        [JsonProperty("IQChat : Использовать подключение/отключение в чате")]
                        public bool UseNetwork;
                        [JsonProperty("IQChat : Список стран для подключения")]
                        public List<string> CountryListConnected = new List<string>();
                        [JsonProperty("IQChat : Список причин отсоединения от сервера")]
                        public List<string> ReasonListDisconnected = new List<string>();
                    }
                    internal class IQChatPM
                    {
                        [JsonProperty("IQChat : Использовать случайное сообщение в ЛС")]
                        public bool UseRandomPM;
                        [JsonProperty("IQChat : Список случайных сообщений в ЛС")]
                        public List<string> PMListMessage = new List<string>();
                        [JsonProperty("Укажите минимальный интервал отправки сообщения в ЛС(секунды)")]
                        public int MinimumInterval;
                        [JsonProperty("Укажите максимальный интервал отправки сообщения в ЛС(секунды)")]
                        public int MaximumInterval;
                    }
                }
            }
            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    APIKeySteam = "",
                    UseLogConsole = true,
                    GeneralSetting = new GeneralSettings
                    {
                        MaximalOnline = 0,
                        PrefixName = "",
                        AvatarSteamID = "",
                    },
                    FakeOnline = new FakeOnlineSettings
                    {
                        
                        IntervalUpdateOnline = 20,
                        SettingsUpdateOnline = new FakeOnlineSettings.UpdateOnline
                        {
                            TypeOnline = TypeOnline.Auto,
                            ManualFormule = new FakeOnlineSettings.UpdateOnline.ManualFormul
                            {
                                ManualTimeOnline = new Dictionary<int, int>
                                {
                                    [00] = 3,
                                    [01] = 3,
                                    [02] = 3,
                                    [03] = 3,
                                    [04] = 2,
                                    [05] = 2,
                                    [06] = 5,
                                    [07] = 4,
                                    [08] = 5,
                                    [09] = 7,
                                    [10] = 7,
                                    [11] = 8,
                                    [12] = 12,
                                    [13] = 13,
                                    [14] = 16,
                                    [15] = 19,
                                    [16] = 20,
                                    [17] = 21,
                                    [18] = 24,
                                    [19] = 27,
                                    [20] = 29,
                                    [21] = 22,
                                    [22] = 15,
                                    [23] = 7,
                                }
                            },
                            StandartFormulSetting = new FakeOnlineSettings.UpdateOnline.StandartFormul
                            {
                                DayTimeGerenation = true,
                                MinimumFactor = 1.2f,
                                MaximumFactor = 1.35f,
                            },
                        }
                    },
                    FakePlayers = new FakePlayerSettings
                    {
                        ChatsDB = true,
                        PlayersDB = true,
                        ListNickName = new List<string>
                        {
                            "Mercury",
                            "Debil",
                            "Fake#1",
                            "Fake#2",
                            "Fake#3",
                            "Fake#4",
                            "Fake#5s"
                        },
                        ListMessages = new List<string>
                        {
                            "hi",
                            "привет",
                            "классный сервер"
                        }
                    },
                    FakeActive = new ActiveSettings
                    {
                        SoundActive = new ActiveSettings.SounActiveSettings
                        {
                            UseLocalSoundBase = true,
                            MinimumIntervalSound = 228,
                            MaximumIntervalSound = 1337,
                            SoundLists = new List<ActiveSettings.SounActiveSettings.Sounds>
                            {
                                new ActiveSettings.SounActiveSettings.Sounds
                                {
                                    DayFaktor = 1,
                                    Rare = 30,
                                    MinPos = 30,
                                    MaxPos = 50,
                                    SoundPath = "assets/bundled/prefabs/fx/impacts/stab/concrete/concrete1.prefab"
                                },
                                new ActiveSettings.SounActiveSettings.Sounds
                                {
                                    DayFaktor = 1,
                                    Rare = 60,
                                    MinPos = 30,
                                    MaxPos = 50,
                                    SoundPath = "assets/bundled/prefabs/fx/impacts/stab/concrete/concrete1.prefab"
                                },
                                new ActiveSettings.SounActiveSettings.Sounds
                                {
                                    DayFaktor = 1,
                                    Rare = 50,
                                    MinPos = 30,
                                    MaxPos = 50,
                                    SoundPath = "assets/prefabs/deployable/campfire/effects/campfire-deploy.prefab"
                                },
                                new ActiveSettings.SounActiveSettings.Sounds
                                {
                                    DayFaktor = 1,
                                    Rare = 50,
                                    MinPos = 45,
                                    MaxPos = 70,
                                    SoundPath = "assets/prefabs/locks/keypad/effects/lock.code.updated.prefab"
                                },
                                new ActiveSettings.SounActiveSettings.Sounds
                                {
                                    DayFaktor = 1,
                                    Rare = 50,
                                    MinPos = 70,
                                    MaxPos = 100,
                                    SoundPath = "assets/prefabs/npc/sam_site_turret/effects/tube_launch.prefab"
                                },
                                new ActiveSettings.SounActiveSettings.Sounds
                                {
                                    DayFaktor = 1,
                                    Rare = 80,
                                    MinPos = 10,
                                    MaxPos = 30,
                                    SoundPath = "assets/prefabs/weapons/bow/effects/fire.prefab"
                                },
                                new ActiveSettings.SounActiveSettings.Sounds
                                {
                                    DayFaktor = 1,
                                    Rare = 80,
                                    MinPos = 10,
                                    MaxPos = 30,
                                    SoundPath = "assets/prefabs/weapons/bow/effects/fire.prefab"
                                },
                                new ActiveSettings.SounActiveSettings.Sounds
                                {
                                    DayFaktor = 1,
                                    Rare = 80,
                                    MinPos = 10,
                                    MaxPos = 30,
                                    SoundPath = "assets/prefabs/weapons/knife/effects/strike-soft.prefab"
                                },
                                new ActiveSettings.SounActiveSettings.Sounds
                                {
                                    DayFaktor = 2,
                                    Rare = 30,
                                    MinPos = 30,
                                    MaxPos = 50,
                                    SoundPath = "assets/bundled/prefabs/fx/impacts/stab/concrete/concrete1.prefab"
                                },
                                new ActiveSettings.SounActiveSettings.Sounds
                                {
                                    DayFaktor = 3,
                                    Rare = 30,
                                    MinPos = 30,
                                    MaxPos = 50,
                                    SoundPath = "assets/bundled/prefabs/fx/impacts/stab/concrete/concrete1.prefab"
                                },
                                new ActiveSettings.SounActiveSettings.Sounds
                                {
                                    DayFaktor = 3,
                                    Rare = 30,
                                    MinPos = 30,
                                    MaxPos = 50,
                                    SoundPath = "assets/bundled/prefabs/fx/impacts/stab/concrete/concrete1.prefab"
                                },
                            }
                        },
                        ChatActive = new ActiveSettings.ChatActiveSetting
                        {
                            ColorChatNickDefault = "#44edc0",
                            UseBlackList = true,
                            BlackList = new List<string>
                            {
                                "читы",
                                "mercury",
                                "гадость",
                                "сука",
                                "блядь",
                                "тварь",
                                "сервер",
                                "говно",
                                "хуйня",
                                "накрутка",
                                "фейк",
                                "крутят",
                            },
                            MinimumInterval = 5,
                            MaximumInterval = 30,
                            IQChatNetworkSetting = new ActiveSettings.ChatActiveSetting.IQChatNetwork
                            {
                                UseNetwork = true,
                                CountryListConnected = new List<string>
                                {
                                    "Russia",
                                    "Ukraine",
                                    "Germany"
                                },
                                ReasonListDisconnected = new List<string>
                                {
                                    "Disconnected",
                                    "Time Out",
                                },
                            },
                            IQChatPMSettings = new ActiveSettings.ChatActiveSetting.IQChatPM
                            {
                                UseRandomPM = true,
                                MinimumInterval = 300,
                                MaximumInterval = 900,
                                PMListMessage = new List<string>
                                {
                                    "прив",
                                    "го в тиму",
                                    "хай",
                                    "трейд?",
                                }
                            }
                        }
                    }
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
                else
                {
                    if(config.FakeOnline.SettingsUpdateOnline.ManualFormule.ManualTimeOnline == null || config.FakeOnline.SettingsUpdateOnline.ManualFormule.ManualTimeOnline.Count == 0)
                    {
                        config.FakeOnline.SettingsUpdateOnline.ManualFormule.ManualTimeOnline = new Dictionary<int, int>
                        {
                            [00] = 3,
                            [01] = 3,
                            [02] = 3,
                            [03] = 3,
                            [04] = 2,
                            [05] = 2,
                            [06] = 5,
                            [07] = 4,
                            [08] = 5,
                            [09] = 7,
                            [10] = 7,
                            [11] = 8,
                            [12] = 12,
                            [13] = 13,
                            [14] = 16,
                            [15] = 19,
                            [16] = 20,
                            [17] = 21,
                            [18] = 24,
                            [19] = 27,
                            [20] = 29,
                            [21] = 22,
                            [22] = 15,
                            [23] = 7,
                        };
                    }
                }
            }
            catch
            {
                PrintWarning($"Ошибка чтения # конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Core

        #region Generated Online

        public void GeneratedOnline()
        {
            timerGenerateOnline = timer.Every(config.FakeOnline.IntervalUpdateOnline, () =>
            {
                if (BasePlayer.activePlayerList.Count == 0)
                {
                    if (config.UseLogConsole)
                        PrintWarning("\n\nОбновление отоброжаемоего онлайна не было,т.к онлайн сервера составляет - 0\n\n");
                    return;
                }

                Int32 LastFakeOnline = FakeOnline;
                switch (config.FakeOnline.SettingsUpdateOnline.TypeOnline)
                {
                    case TypeOnline.Auto:
                        {
                            var SettingsOnline = config.FakeOnline.SettingsUpdateOnline.StandartFormulSetting;

                            int MaxOnline = config.GeneralSetting.MaximalOnline != 0 && config.GeneralSetting.MaximalOnline <= ConVar.Server.maxplayers ? config.GeneralSetting.MaximalOnline : ConVar.Server.maxplayers;
                            int ThisOnline = BasePlayer.activePlayerList.Count;
                            float Randoming = Oxide.Core.Random.Range(SettingsOnline.MinimumFactor, SettingsOnline.MaximumFactor);
                            float Time = float.Parse($"1.{DateTime.Now.Hour}{DateTime.Now.Minute}");
                            int DayFactor = SaveCreated <= 1 ? 2 : SaveCreated;
                            float AvaregeOnline = SettingsOnline.DayTimeGerenation ? (((MaxOnline - ThisOnline) / DayFactor * Randoming) / Time) : ((MaxOnline - ThisOnline) / DayFactor * Randoming);

                            FakeOnline = Convert.ToInt32(AvaregeOnline);
                            break;
                        }
                    case TypeOnline.Auto_Plus_Server:
                        {
                            var SettingsOnline = config.FakeOnline.SettingsUpdateOnline.StandartFormulSetting;

                            int MaxOnline = config.GeneralSetting.MaximalOnline != 0 && config.GeneralSetting.MaximalOnline <= ConVar.Server.maxplayers ? config.GeneralSetting.MaximalOnline : ConVar.Server.maxplayers;
                            int ThisOnline = BasePlayer.activePlayerList.Count;
                            float Randoming = Oxide.Core.Random.Range(SettingsOnline.MinimumFactor, SettingsOnline.MaximumFactor);
                            float Time = float.Parse($"1.{DateTime.Now.Hour}{DateTime.Now.Minute}");
                            int DayFactor = SaveCreated <= 1 ? 2 : SaveCreated;
                            float AvaregeOnline = SettingsOnline.DayTimeGerenation ? (((MaxOnline - ThisOnline) / DayFactor * Randoming) / Time) : ((MaxOnline - ThisOnline) / DayFactor * Randoming);

                            FakeOnline = (BasePlayer.activePlayerList.Count + Convert.ToInt32(AvaregeOnline));

                            break;
                        }
                    case TypeOnline.Manual:
                        {
                            Int32 Time = DateTime.Now.Hour;
                            Int32 ManualOnline = config.FakeOnline.SettingsUpdateOnline.ManualFormule.ManualTimeOnline.ContainsKey(Time) ? config.FakeOnline.SettingsUpdateOnline.ManualFormule.ManualTimeOnline[Time] : 0;
                            FakeOnline = BasePlayer.activePlayerList.Count + ManualOnline;
                            break;
                        }
                }

                foreach (var player in BasePlayer.activePlayerList)
                    if (LastFakeOnline > FakeOnline)
                        ChatNetworkConnected(player);
                    else ChatNetworkDisconnected(player);

                if (config.UseLogConsole)
                    PrintWarning($"\n\nКоличество онлайна обновлено :\nОтображаемый онлайн: {FakeOnline}\nНастоящий онлайн: {BasePlayer.activePlayerList.Count}\n\n");
            });
        }

        #endregion

        #region Generation Player Core

        public List<FakePlayer> ReservedPlayer = new List<FakePlayer>();
        public List<FakePlayer> FakePlayerList = new List<FakePlayer>();
        public List<Messages> FakeMessageList = new List<Messages>();
        public class Messages
        {
            public string Message;
        }
        public class FakePlayer
        {
            public String UserID;
            public string DisplayName;
        }
        void SyncReserved()
        {
            if (BasePlayer.activePlayerList.Count == 0)
            {
                if (config.UseLogConsole)
                {
                    PrintWarning("=============SYNC==================");
                    PrintWarning("Синхронизация и резервирование не было т.к онлайн сервера составляет - 0");
                    PrintWarning("=============SYNC==================");
                }
                return;
            }
            ReservedPlayer.Clear();
            for (int i = 0; i < FakeOnline - BasePlayer.activePlayerList.Count; i++)
            {
                int RandomIndex = Oxide.Core.Random.Range(0, FakePlayerList.Count);
                ReservedPlayer.Add(FakePlayerList[RandomIndex]);
            }
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                FakePlayer presetPlayer = new FakePlayer();
                presetPlayer.DisplayName = player.displayName;
                presetPlayer.UserID = player.UserIDString;
                ReservedPlayer.Add(presetPlayer);
            }
            string JSON = JsonConvert.SerializeObject(ReservedPlayer);
            if (config.UseLogConsole)
            {
                PrintWarning("=============SYNC==================");
                PrintWarning("Запущена синхронизация и резервирование игроков под онлайн..");
                PrintWarning($"Всего сгенерировано игроков: {FakePlayerList.Count}");
                PrintWarning($"Онлайн: {FakeOnline}");
                PrintWarning($"Синхронизация завершена, в резерве: {ReservedPlayer.Count}");
                PrintWarning("=============SYNC==================");
            }

            if (!String.IsNullOrWhiteSpace(config.APIKeySteam))
            {
                if (RoutineAddAvatars == null)
                    RoutineAddAvatars = ServerMgr.Instance.StartCoroutine(AddPlayerAvatar());
                else
                {
                    ServerMgr.Instance.StopCoroutine(RoutineAddAvatars);
                    RoutineAddAvatars = ServerMgr.Instance.StartCoroutine(AddPlayerAvatar());
                }
            }
            Interface.Oxide.CallHook("SyncReservedFinish", JSON);
        }
        private void GeneratedAll()
        {
            PrintWarning("Генерация активности..");

            if (config.FakePlayers.PlayersDB)
                GetPlayerDB();
            else GeneratedPlayer();

            if (config.FakePlayers.ChatsDB)
                GetMessageDB();
            else GeneratedMessage();

            PrintWarning("Генерация игроков сообщений в чате завершена..");
        }
        private void GenerateSounds()
        {
            Configuration.ActiveSettings.SounActiveSettings Sound = config.FakeActive.SoundActive;
            if (!Sound.UseLocalSoundBase) return;
            SortedSoundList = config.FakeActive.SoundActive.SoundLists.Where(x => x.SoundPath != null && !String.IsNullOrWhiteSpace(x.SoundPath)).OrderBy(x => x.DayFaktor == SaveCreated).ToList();
        }
        #region Local Base

        private ulong GeneratedSteam64ID()
        {
            ulong GeneratedID = (ulong)Oxide.Core.Random.Range(76561100000000011, 76561199999999999);
            return GeneratedID;
        }
        private string GeneratedNickName()
        {
            int RandomIndexNick = Oxide.Core.Random.Range(0, config.FakePlayers.ListNickName.Count);
            string NickName = config.FakePlayers.ListNickName[RandomIndexNick];
            return NickName;
        }

        private void GeneratedPlayer()
        {
            if (config.FakePlayers.ListNickName.Count == 0)
            {
                PrintError("Ошибка #35354 генерации локальной базы игроков! Введите ники в список ников"); //
                return;
            }
            for (int i = 0; i < config.FakePlayers.ListNickName.Count; i++)
            {
                string DisplayName = GeneratedNickName();
                ulong UserID = GeneratedSteam64ID();

                FakePlayerList.Add(new FakePlayer
                {
                    DisplayName = DisplayName,
                    UserID = UserID.ToString(),
                });
            }
            PrintWarning("Игроки с локальной базы сгенерированы успешно!");
        }

        private void GeneratedMessage()
        {
            if (config.FakePlayers.ListMessages.Count == 0)
            {
                PrintError("Ошибка #35353 генерации локальной базы сообщений! Введите в нее сообщения"); //
                return;
            }
            for (int i = 0; i < config.FakePlayers.ListMessages.Count; i++)
                FakeMessageList.Add(new Messages { Message = config.FakePlayers.ListMessages[i] });
        }

        #endregion

        #region Set Data Base

        private void DumpPlayers(BasePlayer player)
        {
            if (!config.FakePlayers.PlayersDB) return;
            if (player.IsAdmin) return;

            String DisplayName = player.displayName;
            String UserID = player.UserIDString;
            FakePlayerList.Add(new FakePlayer
            {
                DisplayName = DisplayName,
                UserID = UserID
            });
        }

        internal class MessageToJson
        {
            public String Message;
        }

        private void DumpChat(string Message)
        {
            if (!config.FakePlayers.ChatsDB) return;
            if (config.FakeActive.ChatActive.BlackList.Contains(Message)) return;

            FakeMessageList.Add(new Messages { Message = Message });
        }

        #endregion

        #region Get Data Base

        private void GetPlayerDB()
        {
            if (!config.FakePlayers.PlayersDB) return;

            FakePlayerList = Interface.Oxide.DataFileSystem.ReadObject<List<FakePlayer>>($"IQFakeActive/FakePlayer");
            
            PrintWarning("Игроки с базы данных успешно сгенерированы!");
        }

        private void GetMessageDB()
        {
            if (!config.FakePlayers.ChatsDB) return;

            FakeMessageList = Interface.Oxide.DataFileSystem.ReadObject<List<Messages>>($"IQFakeActive/FakeMessage");

            PrintWarning("Чат с базы данных успешно сгенерированы!");
        }

        #endregion

        #endregion

        #region Generate Avatart
        public IEnumerator AddPlayerAvatar()
        {
            if (ImageLibrary)
            {
                foreach (var p in ReservedPlayer)
                {
                    if (HasImage(p.UserID.ToString())) continue;

                    string url = "http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=" + config.APIKeySteam + "&" + "steamids=" + p.UserID;
                    webrequest.Enqueue(url, null, (code, response) =>
                    {
                        string Avatar = (string)JObject.Parse(response)["response"]["players"][0]["avatarfull"];
                        AddImage(Avatar, p.UserID.ToString());
                    }, this);
                    yield return new WaitForSeconds(0.2f);
                }

                PrintWarning("Синхронизация аватарок заверешна");
                PrintWarning("=============SYNC==================");
            }
        }
        #endregion

        #endregion

        #region Active Metods

        private FakePlayer GetFake()
        {
            if (ReservedPlayer == null) return null;
            if (ReservedPlayer.Count == 0) return null;
            return ReservedPlayer[Oxide.Core.Random.Range(0, ReservedPlayer.Count)];
        }
        public bool IsRare(int Rare)
        {
            if (Oxide.Core.Random.Range(0, 100) >= (100 - Rare))
                return true;
            else return false;
        }

        #region Chat Active
        private void StartChat()
        {
            ActivateMessageChat();
            ActivateMessageChatPM();
        }

        private void ActivateMessageChatPM()
        {
            Int32 TimerRandomPM = Oxide.Core.Random.Range(config.FakeActive.ChatActive.IQChatPMSettings.MinimumInterval, config.FakeActive.ChatActive.IQChatPMSettings.MaximumInterval);

            if (timeActivateMessagePm == null || timeActivateMessagePm.Destroyed)
                timeActivateMessagePm = timer.Once(TimerRandomPM, ActivateMessageChatPM);
            else
            {
                timeActivateMessagePm.Destroy();
                timeActivateMessagePm = timer.Once(TimerRandomPM, ActivateMessageChatPM);
            }
            SendRandomPM();
        }
        private void ActivateMessageChat()
        {
            Int32 TimerRandom = Oxide.Core.Random.Range(config.FakeActive.ChatActive.MinimumInterval, config.FakeActive.ChatActive.MaximumInterval);

            if (timeActivateMessage == null || timeActivateMessage.Destroyed)
                timeActivateMessage = timer.Once(TimerRandom, ActivateMessageChat);
            else
            {
                timeActivateMessage.Destroy();
                timeActivateMessage = timer.Once(TimerRandom, ActivateMessageChat);
            }

            String Message = GetMessage();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                SendMessage(player, Message);
        }
        public void SendMessage(BasePlayer player, String Message) 
        {
            if (String.IsNullOrWhiteSpace(Message)) return;
            FakePlayer Player = GetFake();
            if (Player == null) return;
            BasePlayer RealUser = BasePlayer.Find(Player.DisplayName);
            if (RealUser != null && RealUser.IsConnected && !RealUser.IsSleeping())
            {
                ReservedPlayer.Remove(Player);
                return;
            }

            String Prefix = IQChat ? GetInfoIQChat(IQChatGetType.Prefix) : String.Empty;
            String ColorNick = IQChat ? GetInfoIQChat(IQChatGetType.NickColor) : config.FakeActive.ChatActive.ColorChatNickDefault;
            String ColorMessage = IQChat ? GetInfoIQChat(IQChatGetType.ChatColor) : "#ffffff";

            String DisplayName = !String.IsNullOrWhiteSpace(ColorNick) ? $"<color={ColorNick}>{Player.DisplayName}</color> " : Player.DisplayName;
            String FormatPlayer = $"{Prefix} {DisplayName}";
            String FormatMessage = !String.IsNullOrWhiteSpace(ColorMessage) ? $"<color={ColorMessage}>{Message}</color>" : $"{Message}";

            if (IQChat)
                IQChat?.Call("API_SEND_PLAYER", player, FormatPlayer, FormatMessage, $"{Player.UserID}");
            else player.SendConsoleCommand("chat.add", Player.UserID, $"{FormatPlayer}: {FormatMessage}");
        }
        public void ChatNetworkConnected(BasePlayer player)
        {
            var MessageSettings = config.FakeActive.ChatActive.IQChatNetworkSetting;
            if (!MessageSettings.UseNetwork) return;
            if (IQChat)
            {
                FakePlayer Player = GetFake();
                if (Player == null) return;
                BasePlayer RealUser = BasePlayer.Find(Player.DisplayName);
                if (RealUser != null && RealUser.IsConnected && !RealUser.IsSleeping())
                {
                    ReservedPlayer.Remove(Player);
                    return;
                }
                string Country = GetCountry();
                IQChat?.Call("API_SEND_PLAYER_CONNECTED", player, Player.DisplayName, Country, Player.UserID.ToString());

                if (config.UseLogConsole)
                    PrintWarning($"\nПодключение к серверу во время изменения онлайна Fake-Player: {Player.DisplayName}({Player.UserID})\n\n");
            }
        }
        public void ChatNetworkDisconnected(BasePlayer player)
        {
            var MessageSettings = config.FakeActive.ChatActive.IQChatNetworkSetting;
            if (!MessageSettings.UseNetwork) return;
            if (IQChat)
            {
                FakePlayer Player = GetFake();
                if (Player == null) return;
                BasePlayer RealUser = BasePlayer.Find(Player.DisplayName);
                if (RealUser != null && RealUser.IsConnected && !RealUser.IsSleeping())
                {
                    ReservedPlayer.Remove(Player);
                    return;
                }
                string Reason = GetReason();
                IQChat?.Call("API_SEND_PLAYER_DISCONNECTED", player, Player.DisplayName, Reason, Player.UserID.ToString());

                if (config.UseLogConsole)
                    PrintWarning($"\nОтсоединение от сервера во время изменения онлайна Fake-Player: {Player.DisplayName}({Player.UserID})\n\n");
                ReservedPlayer.Remove(Player);
            }
        }
        public void SendRandomPM()
        {
            if (!config.FakeActive.ChatActive.IQChatPMSettings.UseRandomPM) return;
            if (!IQChat) return;

            int IndexRandomPlayer = Oxide.Core.Random.Range(0, BasePlayer.activePlayerList.Count);
            BasePlayer RandomPlayer = BasePlayer.activePlayerList[IndexRandomPlayer];
            if (RandomPlayer == null) return;
            if (!RandomPlayer.IsConnected) return;
            string Message = GetPM();
            FakePlayer Player = GetFake();
            if (Player == null) return;
            BasePlayer RealUser = BasePlayer.Find(Player.DisplayName);
            if (RealUser != null && RealUser.IsConnected && !RealUser.IsSleeping())
            {
                ReservedPlayer.Remove(Player);
                return;
            }
            if (Player == null) return;
            IQChat?.Call("API_SEND_PLAYER_PM", RandomPlayer, Player.DisplayName, Message);

            if (config.UseLogConsole)
                PrintWarning($"\nОтправлено личное сообщение от Fake-Player: {Player.DisplayName}({Player.UserID}) для игрока : {RandomPlayer.displayName}({RandomPlayer.userID})\nСообщение: {Message}\n\n");
        }

        #region Help Metods Chat Active
        public string GetMessage()
        {
            var MessageSettings = config.FakeActive.ChatActive;
            string Message = FakeMessageList[Oxide.Core.Random.Range(0, FakeMessageList.Count)].Message;
            foreach (var BlackList in MessageSettings.BlackList)
                Message = Message.Replace(BlackList, "");
            return Message;
        }
        public string GetCountry()
        {
            var CountryList = config.FakeActive.ChatActive.IQChatNetworkSetting.CountryListConnected;
            int RandomCountry = Oxide.Core.Random.Range(0, CountryList.Count);
            return CountryList[RandomCountry];
        }
        public string GetReason()
        {
            var ReasonList = config.FakeActive.ChatActive.IQChatNetworkSetting.ReasonListDisconnected;
            int RandomReason = Oxide.Core.Random.Range(0, ReasonList.Count);
            return ReasonList[RandomReason];
        }
        public string GetPM()
        {
            var PMList = config.FakeActive.ChatActive.IQChatPMSettings.PMListMessage;
            int RnadomPM = Oxide.Core.Random.Range(0, PMList.Count);
            return PMList[RnadomPM];
        }
        #endregion

        #endregion

        #region Sound Active

        private Configuration.ActiveSettings.SounActiveSettings.Sounds GetSound()
        {
            Int32 RandomSoundList = Oxide.Core.Random.Range(0, SortedSoundList.Count());
            if (!IsRare(SortedSoundList[RandomSoundList].Rare)) return null;

            return SortedSoundList[RandomSoundList];
        }
        void StartSoundEffects()
        {
            Configuration.ActiveSettings.SounActiveSettings Sound = config.FakeActive.SoundActive;
            if (!Sound.UseLocalSoundBase) return;
            
            Int32 RandomTimer = Oxide.Core.Random.Range(Sound.MinimumIntervalSound, Sound.MaximumIntervalSound);

            if (timerPlaySounds == null || timeActivateMessagePm.Destroyed)
                timerPlaySounds = timer.Once(RandomTimer, StartSoundEffects);
            else
            {
                timerPlaySounds.Destroy();
                timerPlaySounds = timer.Once(RandomTimer, StartSoundEffects);
            }

            PlaySoundEffects();
        }
        private void PlaySoundEffects()
        {
            Configuration.ActiveSettings.SounActiveSettings.Sounds Sound = GetSound();
            if (Sound == null) return;

            foreach (var player in BasePlayer.activePlayerList)
            {
                Effect effect = new Effect();
                int RandomXZ = Oxide.Core.Random.Range(Sound.MinPos, Sound.MaxPos);
                Vector3 PosSound = new Vector3(player.transform.position.x + RandomXZ, player.transform.position.y, player.transform.position.z + RandomXZ);
                effect.Init(Effect.Type.Generic, PosSound, PosSound, (Network.Connection)null);
                effect.pooledString = Sound.SoundPath;
                EffectNetwork.Send(effect, player.net.connection);
            }

            if (config.UseLogConsole)
                PrintWarning($"\n\nДля игроков были проиграны звуки");
        }

        #endregion

        #region InfoMetods

        void ShowFakeOnline(BasePlayer player)
        {
            if (IQChat) return;
            if (ReservedPlayer == null) return;
            if (ReservedPlayer.Count == 0) return;

            String OnlinePlayers = String.Join(", ", ReservedPlayer.Select(p => p.DisplayName.Sanitize()).ToArray());
            String Message = GetLang("SHOW_ONLINE_USERS", player.UserIDString, OnlinePlayers, ReservedPlayer.Count);

            player.SendConsoleCommand("chat.add", 0, Message);
        }

        #endregion

        #endregion

        #region Hooks
        private void OnServerInitialized()
        {
            RoutineInitPlugin = ServerMgr.Instance.StartCoroutine(InitializePlugin());
        }
        
        public IEnumerator InitializePlugin()
        {
            PrintWarning("------------------------");
            PrintWarning("IQFakeActive by Mercury");
            PrintWarning($"Текущий реальный онлайн : {BasePlayer.activePlayerList.Count}");
            PrintWarning("Сейчас начнется генерация активности, ожидайте..Process: ..");
            PrintWarning("------------------------");
            yield return new WaitForSeconds(0.3f);

            //Генерация игроков и чата
            GeneratedAll();

            yield return new WaitForSeconds(3f);

            //Генерируем онлайн от множителя и дополнительных факторов
            GeneratedOnline();

            yield return new WaitForSeconds(1f);

            GenerateSounds();

            yield return new WaitForSeconds(1f);

            //Запуск актива в чате
            StartChat();

            yield return new WaitForSeconds(30f);

            //Резервируем игроков
            SyncReserved();
            timerSynh = timer.Every(600f, () => { SyncReserved(); });

            yield return new WaitForSeconds(3f);

            //Запускаем звуки
            StartSoundEffects();
        }
        void Unload()
        {
            if (RoutineAddAvatars != null)
                ServerMgr.Instance.StopCoroutine(RoutineAddAvatars);
            if (RoutineInitPlugin != null)
                ServerMgr.Instance.StopCoroutine(RoutineInitPlugin);

            RoutineAddAvatars = null;
            RoutineInitPlugin = null;
            if (timeActivateMessage != null && !timeActivateMessage.Destroyed)
                timeActivateMessage.Destroy();
            if (timeActivateMessagePm != null && !timeActivateMessagePm.Destroyed)
                timeActivateMessagePm.Destroy();
            if (timerSynh != null && !timerSynh.Destroyed)
                timerSynh.Destroy();
            if (timerGenerateOnline != null && !timerGenerateOnline.Destroyed)
                timerGenerateOnline.Destroy();
            if (timerPlaySounds != null && !timerPlaySounds.Destroyed)
                timerPlaySounds.Destroy();

            Interface.Oxide.DataFileSystem.WriteObject($"FakeMessage", FakeMessageList);
            Interface.Oxide.DataFileSystem.WriteObject($"FakePlayer", FakeMessageList);
        }

        private object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            var message = arg.GetString(0, "").Trim();
            if (player.IsAdmin) return null;
            if (String.IsNullOrWhiteSpace(message)) return null;
            DumpChat(message);
            return null;
        }
        void OnPlayerInit(BasePlayer player) => DumpPlayers(player);
        #endregion

        #region Commands
        [ChatCommand("online")]
        void ChatCommandShowOnline(BasePlayer player) => ShowFakeOnline(player);

        [ChatCommand("players")]
        void ChatCommandShowPlayers(BasePlayer player) => ShowFakeOnline(player);

        [ConsoleCommand("iqfa")]
        void IQFakeActiveCommand(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.Args == null || arg.Args.Length != 1 || arg.Args.Length > 1)
            {
                PrintWarning("===========SYNTAX===========");
                PrintWarning("Используйте команды:");
                PrintWarning("iqfa online - для показателя онлайна");
                PrintWarning("iqfa synh - синхронизация игроков в резерв");
                PrintWarning("===========SYNTAX===========");
                return;
            }
            string ActionCommand = arg.Args[0].ToLower();
            switch(ActionCommand)
            {
                case "online":
                case "player":
                case "players":
                    {
                        PrintWarning("===========INFORMATION===========");
                        PrintWarning($"Настоящий онлайн : {BasePlayer.activePlayerList.Count}");
                        PrintWarning($"Общий онлайн : {FakeOnline}");
                        PrintWarning("===========INFORMATION===========");
                        break;
                    }
                case "synh":
                case "synchronization":
                case "update":
                case "refresh":
                    {
                        SyncReserved();
                        break;
                    }
            }
        }
        #endregion

        #region API


        bool IsFake(ulong userID) => FakePlayerList.Where(x => x.UserID == userID.ToString()).Count() > 0;
        bool IsFake(string DisplayName) => FakePlayerList.Where(x => x.DisplayName.Contains(DisplayName)).Count() > 0;
        int GetOnline() => FakeOnline;
        ulong GetFakeIDRandom() => (ulong)ulong.Parse(FakePlayerList[Oxide.Core.Random.Range(0, FakePlayerList.Count)].UserID);
        string GetFakeNameRandom() => (string)FakePlayerList[Oxide.Core.Random.Range(0, FakePlayerList.Count)].DisplayName;
        string FindFakeName(ulong ID)
        {
            var Fake = ReservedPlayer.FirstOrDefault(x => x.UserID == ID.ToString());
            if (Fake == null) return null;
            return Fake.DisplayName;
        }
        int DayWipe() => SaveCreated;
        void RemoveReserver(UInt64 ID)
        {
            var Fake = ReservedPlayer.FirstOrDefault(x => x.UserID == ID.ToString());
            if (Fake == null) return;

            ReservedPlayer.Remove(Fake);
        }
        #endregion

        #region Lang
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<String, String>
            {
                ["SHOW_ONLINE_USERS"] = "Players Online: <color=#79d36b>{0}</color> (<color=#dda32e>{1}</color>)",
            }, this);

            lang.RegisterMessages(new Dictionary<String, String>
            {
                ["SHOW_ONLINE_USERS"] = "Игроки онлайн: <color=#79d36b>{0}</color> (<color=#dda32e>{1}</color>)",
            }, this, "ru");
            PrintWarning("Языковой файл загружен успешно");
        }

        public static StringBuilder sb = new StringBuilder();
        public String GetLang(String LangKey, String userID = null, params object[] args)
        {
            sb.Clear();
            if (args != null)
            {
                sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb.ToString();
            }
            return lang.GetMessage(LangKey, this, userID);
        }
        #endregion
    }
}
