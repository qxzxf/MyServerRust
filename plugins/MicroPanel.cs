using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("MicroPanel", "qxzxf", "2.0.8")]
    public class MicroPanel : RustPlugin
    {
        private void OnServerInitialized()
        {
            Check();
            AddImage("https://i.ibb.co/cXNqzNd/1.png", "heli");
            AddImage("https://i.ibb.co/pLg31pD/2.png", "bradley");
            AddImage("https://i.ibb.co/02pf3wR/3.png", "air");
            AddImage("https://i.ibb.co/8sxWfrJ/4.png", "cargo");
            AddImage("https://cdn.discordapp.com/attachments/676533751190126631/690265099625431227/5.png", "ch");
            AddImage(cfg.icon, "lyble");
            AddImage("https://i.ibb.co/r0dRwkX/m.png", "MENU");
            _mainPanel.RectTransform.OffsetMin = cfg.offsetmin;
            _mainPanel.RectTransform.OffsetMax = cfg.offsetmax;
            if (cfg.newson)
            {
                timer.Every(cfg.sec, () =>
                {
                    GenerateNews();
                    LoadNews();
                });
                GenerateNews();
            }

            if (cfg.stime)
            {
                timer.Every(cfg.utime, () =>
                { 
                    foreach (var basePlayer in BasePlayer.activePlayerList)
                        UpdateTime(basePlayer);
                });   
            }
            foreach (var basePlayer in BasePlayer.activePlayerList)
                OnPlayerConnected(basePlayer);
        }

        private void Check()
        {
            foreach (var entity in BaseNetworkable.serverEntities.Where(p =>
                p is CargoPlane || p is BradleyAPC || p is BaseHelicopter || p is BaseHelicopter || p is CargoShip ||
                p is CH47Helicopter))
            {
                if (entity is CargoPlane)
                    IsAir = true;
                if (entity is BradleyAPC)
                    isTank = true;
                if (entity is BaseHelicopter)
                    IsHeli = true;
                if (entity is CargoShip)
                    IsCargo = true;
                if (entity is CH47Helicopter)
                    IsCh = true;
            }
        }

        private void OnUserDisconnected(IPlayer player)
        {
            OnlinePlayer();
            if (closePanel.Contains(ulong.Parse(player.Id)))
                closePanel.Remove(ulong.Parse(player.Id));
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            timer.Once(1f, () => StartUi(player));
        }

        private void Unload()
        {
            foreach (var basePlayer in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(basePlayer, Layer);
            }
        }

        private List<ulong> closePanel = new List<ulong>();
        [ChatCommand("panel")]
        void CloseOpenPanel(BasePlayer player)
        {
            if (closePanel.Contains(player.userID))
            {
                closePanel.Remove(player.userID);
                StartUi(player);
            }
            else
            {
                closePanel.Add(player.userID);
                CuiHelper.DestroyUi(player, Layer);
            }
        }
        #region Cfg

        private static ConfigData cfg { get; set; }

        private class ConfigData
        {
            [JsonProperty("Лейбел")] public string ServerName = "<b><color=#ff8200>DRIMFOX.RU PLUG</color></b>";
            [JsonProperty("Иконка")] public string icon = "https://i.imgur.com/XxbjJEU.png";
            [JsonProperty("Цвет полоски")] public string colorpolos = "#ff8200";
            
            [JsonProperty("Включить поддержку плагина IQFakeActive")]
            public bool IQFakeActive = false;
            
            [JsonProperty("Вкдючить показ времени")]
            public bool time = true;

            [JsonProperty("ЦВЕТ(ВКЛЮЧЕННЫХ ИВЕНТОВ И ОНЛАЙНА)")]
            public string coloron = "#ae4fff";

            [JsonProperty("ЦВЕТ(ОФЛАЙНА И ВЫКЛЮЧЕННЫХ ИВЕНТОВ)")]
            public string coloroff = "#ffffff";

            [JsonProperty("Двигать всю панель - MIN")]
            public string offsetmin = "0 0";

            [JsonProperty("Двигать всю панель - MAX")]
            public string offsetmax = "0 0";

            [JsonProperty("Двигать панель ивентов - MIN")]
            public string evoffsetmin = "310 -38";

            [JsonProperty("Двигать панель ивентов - MAX")]
            public string evoffsetmax = "515 -5";
            [JsonProperty("Показывать время?")]
            public bool stime = true;
            [JsonProperty("Как часто обновлять время (СЕК)?")]
            public int utime = 5;
            [JsonProperty("Двигать панель времени - MIN")]
            public string tvoffsetmin = "170 -40";
            
            [JsonProperty("Двигать панель времени  - MAX")]
            public string tvoffsetmax = "220 -28";
            
            [JsonProperty("Показывать слипперов?")]
            public bool sleep = true;

            [JsonProperty("Курсор включать?")] public bool cursor = false;

            [JsonProperty("Включить авто новости?")]
            public bool newson = true;

            [JsonProperty("Время обновление новостей(В секундах)")]
            public float sec = 30f;

            [JsonProperty("АвтоНовости")] public List<string> newsList = new List<string>();
            [JsonProperty("Кнопки")] public List<Buttons> buttonList = new List<Buttons>();

            public static ConfigData GetNewConf()
            {
                var newConfig = new ConfigData();
                newConfig.buttonList = new List<Buttons>()
                {
                    new Buttons()
                    {
                        Commnad = "chat.say /store",
                        Name = "[- МАГАЗИН -]",
                    },
                    new Buttons()
                    {
                        Commnad = "chat.say /pass",
                        Name = "[- SOPASS -]",
                    },
                    new Buttons()
                    {
                        Commnad = "chat.say /fmenu",
                        Name = "[- SOFRIENDS -]",
                    },
                };
                newConfig.newsList = new List<string>()
                {
                    "Сайт: DRIMFOX.RU",
                    "Группа в вк: vk.com/drimfox"
                };
                return newConfig;
            }
        }

        internal class Buttons
        {
            public string Commnad;
            public string Name;
        }

        protected override void LoadDefaultConfig()
        {
            cfg = ConfigData.GetNewConf();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(cfg);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                cfg = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        #endregion
        private string Layer = "MicroPanelByLAGZYA";

        private CuiPanel _mainPanel = new CuiPanel()
        {
            RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 0", OffsetMax = "0 0"},
            Image = {Color = "0.3072 0.233 0.534 0"}
        };

        object OnPlayerSleep(BasePlayer player)
        {
            OnPlayerSleepEnded(null);
            return null;
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            NextTick(OnlinePlayer);
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is BaseHelicopter)
            {
                IsHeli = true;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    EventInit(basePlayer, "heli");
            }
            else if (entity is BradleyAPC)
            {
                isTank = true;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    EventInit(basePlayer, "bradley");
            }
            else if (entity is CargoPlane)
            {
                IsAir = true;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    EventInit(basePlayer, "air");
            }
            else if (entity is CargoShip)
            {
                IsCargo = true;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    EventInit(basePlayer, "cargo");
            }
            else if (entity is CH47Helicopter)
            {
                IsCh = true;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    EventInit(basePlayer, "ch");
            }
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity is CargoPlane)
            {
                IsAir = false;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    EventInit(basePlayer, "air");
            }
            else if (entity is CargoShip)
            {
                IsCargo = false;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    EventInit(basePlayer, "cargo");
            }
            else if (entity is BaseHelicopter)
            {
                IsHeli = false;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    EventInit(basePlayer, "heli");
            }
            else if (entity is BradleyAPC)
            {
                isTank = false;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    EventInit(basePlayer, "bradley");
            }
            else if (entity is CH47Helicopter)
            {
                IsCh = false;
                foreach (var basePlayer in BasePlayer.activePlayerList)
                    EventInit(basePlayer, "ch");
            }
        } 

        #region Time

        private void UpdateTime(BasePlayer player)
        {
            if(closePanel.Contains(player.userID)) return;
            CuiHelper.DestroyUi(player, Layer + "Time");
            var cont = new CuiElementContainer();
            cont.Add(new CuiButton()
            {   
                RectTransform = {AnchorMin = "0.4 0.05", AnchorMax = "1 0.95"},
                Button = {Command = "", Color = "0.15 0.86 0.1 0"},
                Text = 
                {    
                    Text = $"{covalence.Server.Time.ToShortTimeString()} ",
                    Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 9
                }  
            }, Layer + "Timefon", Layer + "Time");
            CuiHelper.AddUi(player, cont);
        }

        [PluginReference] Plugin IQFakeActive;
        
        int FakeOnline => (int)IQFakeActive?.Call("GetOnline");
        void SyncReservedFinish()
        {
            if (!cfg.IQFakeActive) return;
            PrintWarning("MicroPanel- успешно синхронизирована с IQFakeActive");
            PrintWarning("=============SYNC==================");

        }
        #endregion
        #region Menu
        
        private readonly List<ulong> listPlayer = new List<ulong>();

        [ConsoleCommand("Ui_MicroPanel")]
        private void CommandUi(ConsoleSystem.Arg arg)
        {
            if (arg?.Player() == null) return;
            if (arg.Args[0] == "menu")
            {
                if (!listPlayer.Contains(arg.Player().userID))
                {
                    listPlayer.Add(arg.Player().userID);
                    MenuUpdate(arg.Player(), "menu");
                }
                else
                {
                    listPlayer.Remove(arg.Player().userID);
                    CuiHelper.DestroyUi(arg.Player(), Layer + "Mouse");
                    foreach (var buttonse in cfg.buttonList)
                        CuiHelper.DestroyUi(arg.Player(), Layer + "Mouse" + buttonse.Name);
                }
            }
        }

        private void MenuUpdate(BasePlayer player, string type)
        {
            var cont = new CuiElementContainer();
            float i = 0;
            switch (type)
            {
                case "menu":
                    cont.Add(new CuiPanel()
                    {
                        CursorEnabled = cfg.cursor,
                        RectTransform = {AnchorMin = "0 0", AnchorMax = "0 0"},
                        Image = {Color = "0 0 0 0"}
                    }, Layer, Layer + "Mouse");
                    foreach (var check in cfg.buttonList)
                    {
                        cont.Add(new CuiElement
                        {
                            Parent = Layer,
                            Name = Layer + "Mouse" + check.Name,
                            Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = "0.88 0.01 0.58 0.00",
                                    FadeIn = 0 + i * -0.01f,
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 0", AnchorMax = $"0 0", OffsetMin = $"10 {-80 + i}",
                                    OffsetMax = $"150 {-55 + i}"
                                }
                            }
                        });
                        cont.Add(new CuiElement
                        {
                            Parent = Layer + "Mouse" + check.Name,
                            Components =
                            {
                                new CuiTextComponent()
                                {
                                    Text = check.Name, Align = TextAnchor.MiddleLeft,
                                    Font = "robotocondensed-regular.ttf",
                                    FadeIn = 0 + i * -0.01f,
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0 0", AnchorMax = $"1 1"
                                }
                            }
                        });
                        cont.Add(
                            new CuiButton
                            {
                                RectTransform = {AnchorMin = "0 0", AnchorMax = $"1 1"},
                                Button =
                                {
                                    Command = check.Commnad, Color = "0 0 0 0",
                                    FadeIn = 1 + i * -0.01f,
                                },
                                Text = {Text = ""}
                            },
                            Layer + "Mouse" + check.Name);
                        i += -30f;
                    }

                    CuiHelper.AddUi(player, cont);
                    break;
            }
        }

        #endregion
        private void OnlinePlayer()
        {
            int Online = IQFakeActive ? cfg.IQFakeActive ? FakeOnline : BasePlayer.activePlayerList.Count : BasePlayer.activePlayerList.Count; 
            foreach (var basePlayer in BasePlayer.activePlayerList)
            {
                if(closePanel.Contains(basePlayer.userID)) continue;
                CuiHelper.DestroyUi(basePlayer, Layer + "Online");
                CuiHelper.DestroyUi(basePlayer, Layer + "Sleep");
                var cont = new CuiElementContainer();
                cont.Add(new CuiButton()
                {
                    RectTransform = {AnchorMin = "0.4 0.05", AnchorMax = "1 0.95"},
                    Button = {Command = "", Color = "0.15 0.86 0.1 0"},
                    Text =
                    {
                        Text = $"{Online}/{ConVar.Server.maxplayers}",
                        Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 9
                    }
                }, Layer + "Onlinefon", Layer + "Online");
                if (cfg.sleep)
                {
                    cont.Add(new CuiButton()
                    {
                        RectTransform =
                        {
                            AnchorMin = "0.4 0.05", AnchorMax = "1 0.95"
                        },
                        Button =
                        {
                            Command = "", Color = "0.25 0.86 0.86 0"
                        },
                        Text =
                        {
                            Text = $"{BasePlayer.sleepingPlayerList.Count}",
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-regular.ttf", FontSize = 9
                        }
                    }, Layer + "Sleepfon", Layer + "Sleep");
                } 
                CuiHelper.AddUi(basePlayer, cont);
            }
        }

        private bool IsAir, IsHeli, isTank, IsCargo, IsCh;

        private void EventInit(BasePlayer player, string type)
        {
            if(closePanel.Contains(player.userID)) return;
            var cont = new CuiElementContainer();
            switch (type)
            {
                case "air":
                    CuiHelper.DestroyUi(player, Layer + "Air");
                    if (IsAir)
                    {
                        cont.Add(new CuiButton()
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.01147922 0", AnchorMax = "0.1814795 1"
                            },
                            Button =
                            {
                                Command = "", Color = HexToRustFormat(cfg.coloron),
                                Sprite = "assets/icons/circle_open.png"
                            },
                            Text =
                            {
                                Text = $"",
                            }
                        }, Layer + "Events", Layer + "Air");
                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.2241479 0.2363638", AnchorMax = "0.7069132 0.7090913"
                            },
                            Image =
                            {
                                Color = HexToRustFormat(cfg.coloron), Png = GetImage("air")
                            },
                        }, Layer + "Air");
                    }
                    else
                    {
                        cont.Add(new CuiButton()
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.01147922 0", AnchorMax = "0.1814795 1"
                            },
                            Button =
                            {
                                Command = "", Color = HexToRustFormat(cfg.coloroff),
                                Sprite = "assets/icons/circle_open.png"
                            },
                            Text =
                            {
                                Text = $"",
                            }
                        }, Layer + "Events", Layer + "Air");
                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.2241479 0.2363638", AnchorMax = "0.7069132 0.7090913"
                            },
                            Image =
                            {
                                Color = HexToRustFormat(cfg.coloroff), Png = GetImage("air")
                            },
                        }, Layer + "Air");
                    }

                    CuiHelper.AddUi(player, cont);
                    break;
                case "ch":
                    CuiHelper.DestroyUi(player, Layer + "Ch");
                    if (IsCh)
                    {
                        cont.Add(new CuiButton()
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.7178804 0", AnchorMax = "0.8878804 1"
                            },
                            Button =
                            {
                                Command = "", Color = HexToRustFormat(cfg.coloron),
                                Sprite = "assets/icons/circle_open.png"
                            },
                            Text = {Text = $"",}
                        }, Layer + "Events", Layer + "Ch");
                        cont.Add(new CuiPanel()
                        {
                            RectTransform = {AnchorMin = "0.2241479 0.2363638", AnchorMax = "0.7069132 0.7090913"},
                            Image = {Color = HexToRustFormat(cfg.coloron), Png = GetImage("ch")},
                        }, Layer + "Ch");
                    }
                    else
                    {
                        cont.Add(new CuiButton()
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.7178804 0", AnchorMax = "0.8878804 1"
                            },
                            Button =
                            {
                                Command = "", Color = HexToRustFormat(cfg.coloroff),
                                Sprite = "assets/icons/circle_open.png"
                            },
                            Text =
                            {
                                Text = $"",
                            }
                        }, Layer + "Events", Layer + "Ch");
                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.2241479 0.2363638", AnchorMax = "0.7069132 0.7090913"
                            },
                            Image =
                            {
                                Color = HexToRustFormat(cfg.coloroff), Png = GetImage("ch")
                            },
                        }, Layer + "Ch");
                    }

                    CuiHelper.AddUi(player, cont);
                    break;

                case "heli":
                    CuiHelper.DestroyUi(player, Layer + "Heli");
                    if (IsHeli)
                    {
                        cont.Add(new CuiButton()
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.1880797 0", AnchorMax = "0.3580798 1"
                            },
                            Button =
                            {
                                Command = "", Color = HexToRustFormat(cfg.coloron),
                                Sprite = "assets/icons/circle_open.png"
                            },
                            Text =
                            {
                                Text = $"",
                            }
                        }, Layer + "Events", Layer + "Heli");
                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.2241479 0.2363638", AnchorMax = "0.7069132 0.7090913"
                            },
                            Image =
                            {
                                Color = HexToRustFormat(cfg.coloron), Png = GetImage("heli")
                            },
                        }, Layer + "Heli");
                    }
                    else
                    {
                        cont.Add(new CuiButton()
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.1880797 0", AnchorMax = "0.3580798 1"
                            },
                            Button =
                            {
                                Command = "", Color = HexToRustFormat(cfg.coloroff),
                                Sprite = "assets/icons/circle_open.png"
                            },
                            Text =
                            {
                                Text = $"",
                            }
                        }, Layer + "Events", Layer + "Heli");
                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.2241479 0.2363638", AnchorMax = "0.7069132 0.7090913"
                            },
                            Image =
                            {
                                Color = HexToRustFormat(cfg.coloroff), Png = GetImage("heli")
                            },
                        }, Layer + "Heli");
                    }

                    CuiHelper.AddUi(player, cont);
                    break;
                case "bradley":
                    CuiHelper.DestroyUi(player, Layer + "Bradley");
                    if (isTank)
                    {
                        cont.Add(new CuiButton()
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.36468 0", AnchorMax = "0.53468 1"
                            },
                            Button =
                            {
                                Command = "", Color = HexToRustFormat(cfg.coloron),
                                Sprite = "assets/icons/circle_open.png"
                            },
                            Text = {Text = $"",}
                        }, Layer + "Events", Layer + "Bradley");
                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.2241479 0.2363638", AnchorMax = "0.7069132 0.7090913"
                            },
                            Image =
                            {
                                Color = HexToRustFormat(cfg.coloron), Png = GetImage("bradley")
                            },
                        }, Layer + "Bradley");
                    }
                    else
                    {
                        cont.Add(new CuiButton()
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.36468 0", AnchorMax = "0.53468 1"
                            },
                            Button =
                            {
                                Command = "", Color = HexToRustFormat(cfg.coloroff),
                                Sprite = "assets/icons/circle_open.png"
                            },
                            Text =
                            {
                                Text = $"",
                            }
                        }, Layer + "Events", Layer + "Bradley");
                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.2241479 0.2363638", AnchorMax = "0.7069132 0.7090913"
                            },
                            Image =
                            {
                                Color = HexToRustFormat(cfg.coloroff), Png = GetImage("bradley")
                            },
                        }, Layer + "Bradley");
                    }

                    CuiHelper.AddUi(player, cont);
                    break;
                case "cargo":
                    CuiHelper.DestroyUi(player, Layer + "Cargo");
                    if (IsCargo)
                    {
                        cont.Add(new CuiButton()
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.5412802 0", AnchorMax = "0.7112802 1"
                            },
                            Button =
                            {
                                Command = "", Color = HexToRustFormat(cfg.coloron),
                                Sprite = "assets/icons/circle_open.png"
                            },
                            Text = {Text = $"",}
                        }, Layer + "Events", Layer + "Cargo");
                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.2241479 0.2363638", AnchorMax = "0.7069132 0.7090913"
                            },
                            Image =
                            {
                                Color = HexToRustFormat(cfg.coloron), Png = GetImage("cargo")
                            },
                        }, Layer + "Cargo");
                    }
                    else
                    {
                        cont.Add(new CuiButton()
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.5412802 0", AnchorMax = "0.7112802 1"
                            },
                            Button =
                            {
                                Command = "", Color = HexToRustFormat(cfg.coloroff),
                                Sprite = "assets/icons/circle_open.png"
                            },
                            Text =
                            {
                                Text = $"",
                            }
                        }, Layer + "Events", Layer + "Cargo");
                        cont.Add(new CuiPanel()
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.2241479 0.2363638", AnchorMax = "0.7069132 0.7090913"
                            },
                            Image =
                            {
                                Color = HexToRustFormat(cfg.coloroff), Png = GetImage("cargo")
                            },
                        }, Layer + "Cargo");
                    }

                    CuiHelper.AddUi(player, cont);
                    break;
            }
        }

        private void StartUi(BasePlayer player)
        {
            var cont = new CuiElementContainer();

            cont.Add(_mainPanel, "Overlay", Layer);
            cont.Add(new CuiElement()
            {
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = cfg.ServerName, Align = TextAnchor.MiddleLeft, FontSize = 16
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "60 -25", OffsetMax = "200 -5"
                    }
                }
            });
            cont.Add(new CuiElement()
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent()
                    {
                        Png = GetImage("lyble")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "10 -45", OffsetMax = "53 -5"
                    }
                }
            });
            cont.Add(new CuiButton()
            {
                RectTransform =
                {
                    AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "10 -45", OffsetMax = "53 -5"
                },
                Button =
                {
                    Color = "0 0 0 0",
                    Command = "Ui_MicroPanel menu"
                },
                Text = {Text = ""}
            }, Layer);
            cont.Add(new CuiElement()
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat(cfg.colorpolos),
                        Sprite = "assets/content/ui/ui.background.transparent.linearltr.tga"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "60 -25", OffsetMax = $"320 -23"
                    }
                }
            });
            cont.Add(new CuiElement()
            {
                Parent = Layer,
                Name = Layer + "Onlinefon",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = "0.64 0.64 0.64 0.25"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "60 -40", OffsetMax = "110 -28"
                    }
                }
            });
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Onlinefon",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Color = HexToRustFormat(cfg.coloron), Text = "ON", FontSize = 9,
                        Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf",
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.02999999 0.05", AnchorMax = "0.4899999 0.95"
                    }
                }
            });
            if (cfg.sleep)
            {
                cont.Add(new CuiElement()
                {
                    Parent = Layer,
                    Name = Layer + "Sleepfon",
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = "0.64 0.64 0.64 0.25"
                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "115 -40", OffsetMax = "165 -28"
                        }
                    }
                });
                cont.Add(new CuiElement()
                {
                    Parent = Layer + "Sleepfon",
                    Components =
                    {
                        new CuiTextComponent()
                        {
                            Color = HexToRustFormat(cfg.coloroff), Text = "OFF", FontSize = 9,
                            Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf",
                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin = "0.02999999 0.05", AnchorMax = "0.4899999 0.95"
                        }
                    }
                });
            }
            cont.Add(new CuiElement()
            {
                Parent = Layer,
                Name = Layer + "Timefon",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = "0.64 0.64 0.64 0.25"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = cfg.tvoffsetmin, OffsetMax = cfg.tvoffsetmax
                    }
                }
            });
            cont.Add(new CuiElement()
            {
                Parent = Layer + "Timefon",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Color = HexToRustFormat(cfg.coloroff), Text = "TIME", FontSize = 9,
                        Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf",
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.1 0.05", AnchorMax = "0.4899999 0.95"
                    }
                }
            });
            cont.Add(new CuiPanel()
            {
                Image =
                {
                    Color = "0 0 0 0",
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "0 0",
                    OffsetMin = cfg.evoffsetmin,
                    OffsetMax = cfg.evoffsetmax
                }
            }, Layer, Layer + "Events");
            if (cfg.newson)
            {
                CuiHelper.DestroyUi(player, Layer + "News");
                cont.Add(new CuiElement()
                {
                    Parent = Layer,
                    Name = Layer + "News",
                    Components =
                    {
                        new CuiTextComponent()
                        {
                            Text = news, Align = TextAnchor.MiddleLeft, FontSize = 10,
                            Font = "robotocondensed-regular.ttf",
                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "195 -25", OffsetMax = $"320 -5"
                        }
                    }
                });
            } 

            CuiHelper.AddUi(player, cont);
            OnlinePlayer();
            if (cfg.stime)
                UpdateTime(player);
            EventInit(player, "air");
            EventInit(player, "ch");
            EventInit(player, "heli");
            EventInit(player, "cargo");
            EventInit(player, "bradley");
        }

        public string news;
        public int newsId = -1;

        void GenerateNews()
        {
            if (cfg.newsList.Count > 0)
            {
                if (cfg.newsList.Count - 1 <= newsId)
                {
                    newsId = -1;
                }

                newsId++;
                news = cfg.newsList[newsId];
                return;
            }

            news = "";
        }

        void LoadNews()
        {
            foreach (var basePlayer in BasePlayer.activePlayerList)
            {
                var cont = new CuiElementContainer();
                CuiHelper.DestroyUi(basePlayer, Layer + "News");
                cont.Add(new CuiElement()
                {
                    Parent = Layer,
                    Name = Layer + "News",
                    Components =
                    {
                        new CuiTextComponent()
                        {
                            Text = news, Align = TextAnchor.MiddleLeft, FontSize = 10,
                            Font = "robotocondensed-regular.ttf",
                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "195 -25", OffsetMax = $"320 -5"
                        }
                    }
                });
                CuiHelper.AddUi(basePlayer, cont);
            }
        }

        #region [Help]

        private string GetImage(string shortname, ulong skin = 0) =>
            (string) ImageLibrary.Call("GetImage", shortname, skin);

        private bool AddImage(string url, string shortname, ulong skin = 0) =>
            (bool) ImageLibrary.Call("AddImage", url, shortname, skin);

        [PluginReference] private Plugin ImageLibrary;

        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#FFFFFFFF";
            var str = hex.Trim('#');
            if (str.Length == 6) str += "FF";
            if (str.Length != 8)
            {
                throw new Exception(hex);
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);
            Color color = new Color32(r, g, b, a);
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        #endregion
    }
}
