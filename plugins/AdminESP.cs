using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("AdminESP", "qxzxf", "0.1.01")]
    public class AdminESP : RustPlugin
    {
        static Dictionary<ulong, PlayerSetting> PlayerSettings = new Dictionary<ulong, PlayerSetting>();

        public class PlayerSetting
        {
            public bool Enabled;
            public float UpdateTime;
            public int PlayerDistance;
            public bool ShowAdmins;
            public bool DrawNames;
            public bool DrawBoxes;
            public bool DrawEyeLine;
            public int EyeLineDistance;
            public bool Sleepers;

        }

        [ConsoleCommand("adminesp.toggle")]
        void cmdConsoleEnabledESP(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null) return;
            if (PlayerSettings.ContainsKey(player.userID) && permission.UserHasPermission(player.UserIDString, AdminPermission))
            {
                var data = PlayerSettings[player.userID];
                if (data.Enabled)
                {
                    data.Enabled = false;
                    SendReply(player, "ESP выключен");
                    if (player.GetComponent<ESPPlayer>() != null)
                        player.GetComponent<ESPPlayer>().DestroyComponent();
                }
                else
                {
                    data.Enabled = true;
                    SendReply(player, "ESP включен");

                    if (player.GetComponent<ESPPlayer>() == null)
                        player.gameObject.AddComponent<ESPPlayer>()?.Init(PlayerSettings[player.userID]);

                }
            }
        }

        [ConsoleCommand("adminespUI_toggle")]
        void cmdConsoleEnabledESPUI(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null) return;
            if (!permission.UserHasPermission(player.UserIDString, AdminPermission))
                return;
            var type = args.Args[0];
            var value = args.Args[1];

            if (PlayerSettings.ContainsKey(player.userID))
            {
                var data = PlayerSettings[player.userID];
                switch (type)
                {
                    case "updatetime":
                        if (float.Parse(value) > 0.1f && float.Parse(value) < 15)
                            data.UpdateTime = float.Parse(value);
                        if (float.Parse(value) > 0.1f && float.Parse(value) < 15)
                            SendReply(player, $"Время обновления изменено до {data.UpdateTime} с.");
                        CrateMainMenu(player);

                        break;
                    case "playerdistance":
                        if (int.Parse(value) >= 50 && int.Parse(value) <= 3000)
                            data.PlayerDistance = int.Parse(value);
                        if (int.Parse(value) >= 50 && int.Parse(value) <= 3000)
                            SendReply(player, $"Дистанция видимости измнена до {data.PlayerDistance} м.");
                        CrateMainMenu(player);

                        break;
                    case "showadmins":
                        data.ShowAdmins = bool.Parse(value);
                        SendReply(player, $"Отображение админов изменено на {data.ShowAdmins}");
                        CrateMainMenu(player);

                        break;
                    case "drawnames":
                        data.DrawNames = bool.Parse(value);
                        SendReply(player, $"Отображение имен изменено на {data.DrawNames}");
                        CrateMainMenu(player);

                        break;
                    case "drawboxes":
                        data.DrawBoxes = bool.Parse(value);
                        SendReply(player, $"Отображение боксов изменено на {data.DrawBoxes}");
                        CrateMainMenu(player);

                        break;
                    case "draweyeline":
                        data.DrawEyeLine = bool.Parse(value);
                        SendReply(player, $"Отображение куда смотрит игрок изменено на {data.DrawEyeLine}");
                        CrateMainMenu(player);

                        break;
                    case "sleeping":
                        data.Sleepers = bool.Parse(value);
                        SendReply(player, $"Отображение спящих игроков изменено на {data.Sleepers}");
                        CrateMainMenu(player);
                        break;
                    case "eyelinedistance":
                        data.EyeLineDistance = int.Parse(value);
                        SendReply(player, $"Длина линии взгляда игроков изменено на {data.EyeLineDistance}");
                        CrateMainMenu(player);
                        break;
                    case "enable":
                        data.Enabled = bool.Parse(value);
                        if (!data.Enabled)
                        {
                            SendReply(player, "ESP выключен");
                            if (player.GetComponent<ESPPlayer>() != null)
                                player.GetComponent<ESPPlayer>().DestroyComponent();
                        }
                        else
                        {
                            SendReply(player, "ESP включен");

                            if (player.GetComponent<ESPPlayer>() == null)
                                player.gameObject.AddComponent<ESPPlayer>()?.Init(PlayerSettings[player.userID]);

                        }
                        CrateMainMenu(player);
                        break;
                }
            }
        }

        [ChatCommand("ae")]
        void cmdAdminESP(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, AdminPermission))
            {
                SendReply(player, "У Вас нету доступа к данной команде");
                return;
            }

            if (!PlayerSettings.ContainsKey(player.userID)) OnPlayerConnected(player);
            var data = PlayerSettings[player.userID];
            if (args.Length < 1)
            {
                if (data.Enabled)
                {
                    data.Enabled = false;
                    SendReply(player, "ESP выключен");
                    if (player.GetComponent<ESPPlayer>() != null)
                        player.GetComponent<ESPPlayer>().DestroyComponent();
                }
                else
                {
                    data.Enabled = true;
                    SendReply(player, "ESP включен");
                    if (player.GetComponent<ESPPlayer>() == null)
                        player.gameObject.AddComponent<ESPPlayer>()?.Init(PlayerSettings[player.userID]);

                }
                return;
            }

            if (args.Length < 3 && args[0] == "settings")
            {
                statsUIDisabled.Add(player);
                CrateMainMenu(player);
            }

            if (args.Length < 3 && args[0] == "help")
            {
                SendReply(player, "ESP Настройки:\n"
                     + $"\n/ae settings UpdateTime {data.UpdateTime} - частота обновления данных (0.15 сек)"
                     + $"\n/ae settings PlayerDistance {data.PlayerDistance} - максимальная дистанция отображения игроков (500 метров)"
                     + $"\n/ae settings ShowAdmins {data.ShowAdmins} - нужно ли показывать админов (true - да, false - нет)"
                     + $"\n/ae settings DrawNames {data.DrawNames} - нужно ли показывать имена игроков (true - да, false - нет)"
                     + $"\n/ae settings DrawBoxes {data.DrawBoxes} - нужно ли показывать боксы игроков (true - да, false - нет)"
                     + $"\n/ae settings DrawEyeLine {data.DrawEyeLine} - нужно ли показывать взгляд игроков (true - да, false - нет)"
                     + $"\n/ae settings Sleeping {data.Sleepers} - нужно ли показывать спящих игроков (true - да, false - нет)"
                     + $"\n/ae settings EyeLineDistance {data.EyeLineDistance} - длина линии взгляда игроков (50 метров)");
            }

            if (args.Length == 3 && args[0] == "settings")
            {
                switch (args[1].ToLower())
                {
                    case "updatetime":
                        data.UpdateTime = float.Parse(args[2]);
                        SendReply(player, $"Время обновления изменено до {data.UpdateTime} с.");
                        break;
                    case "playerdistance":
                        data.PlayerDistance = int.Parse(args[2]);
                        SendReply(player, $"Дистанция видимости измнена до {data.PlayerDistance} м.");
                        break;
                    case "showadmins":
                        data.ShowAdmins = bool.Parse(args[2]);
                        SendReply(player, $"Отображение админов изменено на {data.ShowAdmins}");
                        break;
                    case "drawnames":
                        data.DrawNames = bool.Parse(args[2]);
                        SendReply(player, $"Отображение имен изменено на {data.DrawNames}");
                        break;
                    case "drawboxes":
                        data.DrawBoxes = bool.Parse(args[2]);
                        SendReply(player, $"Отображение боксов изменено на {data.DrawBoxes}");
                        break;
                    case "sleeping":
                        data.Sleepers = bool.Parse(args[2]);
                        SendReply(player, $"Отображение боксов изменено на {data.Sleepers}");
                        break;
                    case "draweyeline":
                        data.DrawEyeLine = bool.Parse(args[2]);
                        SendReply(player, $"Отображение куда смотрит игрок изменено на {data.DrawEyeLine}");
                        break;
                    case "eyelinedistance":
                        data.EyeLineDistance = int.Parse(args[2]);
                        SendReply(player, $"Длина линии взгляда игроков изменено на {data.EyeLineDistance}");
                        break;

                }

            }
        }

        static AdminESP ins;

        string AdminPermission = "adminesp.use";

        void OnServerInitialized()
        {
            ins = this;
            LoadData();
            permission.RegisterPermission(AdminPermission, this);
            BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (player == null) return;
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.In(1f, () => OnPlayerConnected(player));
                return;
            }
            if (PlayerSettings.ContainsKey(player.userID) && !permission.UserHasPermission(player.UserIDString, AdminPermission))
            {
                PlayerSettings.Remove(player.userID);
                return;
            }
            if (!permission.UserHasPermission(player.UserIDString, AdminPermission)) return;
            if (PlayerSettings.ContainsKey(player.userID))
            {
                if (PlayerSettings[player.userID].Enabled)
                {
                    if (player.GetComponent<ESPPlayer>() == null)
                        player.gameObject.AddComponent<ESPPlayer>()?.Init(PlayerSettings[player.userID]);
                }
            }
            else
            {
                PlayerSettings.Add(player.userID, new PlayerSetting()
                {
                    Enabled = false,
                    DrawBoxes = false,
                    DrawEyeLine = true,
                    DrawNames = true,
                    EyeLineDistance = 50,
                    PlayerDistance = 200,
                    ShowAdmins = true,
                    UpdateTime = 0.15f
                });
            }
            CrateButtonMenu(player);

        }

        public List<BasePlayer> statsUIDisabled = new List<BasePlayer>();

        public static string Main = "AdminESP";

        void CrateButtonMenu(BasePlayer player)
        {

            CuiHelper.DestroyUi(player, Main);
            if (!permission.UserHasPermission(player.UserIDString, AdminPermission)) return;

            var elements = new CuiElementContainer();

            var mainName = elements.Add(new CuiPanel
            {
                Image =
                {
                    Color = "1 1 1 0.2"
                },
                RectTransform =
                {
                    AnchorMin = "0 0.025",  AnchorMax = $"0.05 0.053"
                }
            }, "Hud.Menu", Main);

            var type = statsUIDisabled.Contains(player) ? "<" : ">";
            elements.Add(new CuiElement
            {
                Parent = Main,
                Components =
                            {
                                new CuiTextComponent { Text = "ESP", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"},
                                new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1" }
                            }
            });

            elements.Add(new CuiElement
            {
                Parent = Main,
                Components =
                            {
                                new CuiButtonComponent { Color = "1 1 1 0", Command = "adminEsp_mainmenu" },
                                new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1" }
                            }
            });

            CuiHelper.AddUi(player, elements);

        }

        [ConsoleCommand("adminEsp_mainmenu")]
        void cmdMainMenuESP(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (!permission.UserHasPermission(player.UserIDString, AdminPermission)) return;
            if (!statsUIDisabled.Contains(player))
            {
                statsUIDisabled.Add(player);
                CrateMainMenu(player);
            }
            else
            {
                CuiHelper.DestroyUi(player, $"{Main}MenuPanel");
                statsUIDisabled.Remove(player);
            }
        }

        void CrateMainMenu(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, $"{Main}MenuPanel");
            var elements = new CuiElementContainer();

            elements.Add(new CuiPanel
            {
                Image =
                {
                    Color = "1 1 1 0"
                },
                RectTransform =
                {
                    AnchorMin = "0.052 0.045",  AnchorMax = $"0.3 0.26"
                },
                CursorEnabled = true
            }, "Hud.Menu", $"{Main}MenuPanel");

            elements.Add(new CuiElement
            {
                Parent = $"{Main}MenuPanel",
                Components =
                            {
                                new CuiTextComponent { Text = "НАСТРОЙКА ESP", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"},
                                new CuiRectTransformComponent{ AnchorMin = "0 0.8", AnchorMax = "1 1" }
                            }
            });

            elements.Add(new CuiPanel
            {
                Image =
                {
                    Color = "1 1 1 0.2"
                },
                RectTransform =
                {
                    AnchorMin = "0 0",  AnchorMax = $"1 0.8"
                },
            }, $"{Main}MenuPanel", $"{Main}CurrentPanel");




            var position = GetPositions(2, 4, 0.01f, 0.05f);
            for (int i = 0; i < 8; i++)
            {
                elements.Add(new CuiPanel
                {
                    Image =
                {
                    Color = "1 1 1 0"
                },
                    RectTransform =
                {
                    AnchorMin = position[i].AnchorMin,  AnchorMax = position[i].AnchorMax
                },
                }, $"{Main}CurrentPanel", $"{Main}CurrentPanel.{i}");


            }
            if (!PlayerSettings.ContainsKey(player.userID)) OnPlayerConnected(player);
            var data = PlayerSettings[player.userID];
            elements.Add(new CuiButton
            {
                Button =
                        {
                            Color = !data.Enabled ? "0.60 0.82 0.55 0.5" : "0.94 0.43 0.44 0.5",
                            Command = $"adminespUI_toggle enable {!data.Enabled}",
                        },
                RectTransform =
                        {
                            AnchorMin = "0 -0.2", AnchorMax = "1 0"
                        },
                Text =
                        {
                            Text = data.Enabled ? "ВЫКЛЮЧИТЬ" : "ВКЛЮЧИТЬ",
                            FontSize = 14,
                            Align = TextAnchor.MiddleCenter,
                        }
            }, $"{Main}MenuPanel");


            elements.Add(new CuiButton
            {
                Button =
                        {
                            Color = "0.94 0.43 0.44 0",
                        },
                RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        },
                Text =
                        {
                            Text = "",
                            FontSize = 14,
                            Align = TextAnchor.MiddleCenter,
                            //Color = labelColor
                        }
            }, $"{Main}CurrentPanel.0");

            elements.Add(new CuiElement
            {
                Parent = $"{Main}CurrentPanel.0",
                Components =
                            {
                                new CuiTextComponent { Text = "Спящие: ", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"},
                                new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "0.5 1" }
                            }
            });

            elements.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "1 0.997" },
                Button = { Color = !data.Sleepers ? "0.60 0.82 0.55 0.5" : "0.94 0.43 0.44 0.5", Command = $"adminespUI_toggle sleeping {!data.Sleepers}" },
                Text = { Text = data.Sleepers ? "ВЫКЛЮЧИТЬ" : "ВКЛЮЧИТЬ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 12 },
            }, $"{Main}CurrentPanel.0");

            elements.Add(new CuiPanel
            {
                Image =
                {
                    Color = "1 1 1 0.5"
                },
                RectTransform =
                {
                   AnchorMin = "0.5 0", AnchorMax = "1 1"
                },
            }, $"{Main}CurrentPanel.1");

            elements.Add(new CuiElement
            {
                Parent = $"{Main}CurrentPanel.1",
                Components =
                            {
                                new CuiTextComponent { Text = "Обновление: ", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"},
                                new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "0.5 1" }
                            }
            });

            elements.Add(new CuiElement
            {
                Parent = $"{Main}CurrentPanel.1",
                Components =
                            {
                                new CuiTextComponent { Text = $"{data.UpdateTime.ToString("0.0")}", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"},
                                new CuiRectTransformComponent{ AnchorMin = "0.5 0", AnchorMax = "1 1" }
                            }
            });

            elements.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.85 0", AnchorMax = "1 0.997" },
                Button = { Color = "1 1 1 0.5", Command = $"adminespUI_toggle updatetime {data.UpdateTime + 0.1f}" },
                Text = { Text = "+", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 18 },
            }, $"{Main}CurrentPanel.1");

            elements.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.65 0.997" },
                Button = { Color = "1 1 1 0.5", Command = $"adminespUI_toggle updatetime {data.UpdateTime - 0.1f}" },
                Text = { Text = "-", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 20 },
            }, $"{Main}CurrentPanel.1");

            elements.Add(new CuiPanel
            {
                Image =
                {
                    Color = "1 1 1 0.5"
                },
                RectTransform =
                {
                   AnchorMin = "0.5 0", AnchorMax = "1 1"
                },
            }, $"{Main}CurrentPanel.2");

            elements.Add(new CuiElement
            {
                Parent = $"{Main}CurrentPanel.2",
                Components =
                            {
                                new CuiTextComponent { Text = "Дистанция: ", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"},
                                new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "0.5 1" }
                            }
            });


            elements.Add(new CuiElement
            {
                Parent = $"{Main}CurrentPanel.2",
                Components =
                            {
                                new CuiTextComponent { Text = $"{data.PlayerDistance.ToString("0")}", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"},
                                new CuiRectTransformComponent{ AnchorMin = "0.5 0", AnchorMax = "1 1" }
                            }
            });

            elements.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.85 0", AnchorMax = "1 0.997" },
                Button = { Color = "1 1 1 0.5", Command = $"adminespUI_toggle playerdistance {data.PlayerDistance + 50}" },
                Text = { Text = "+", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 18 },
            }, $"{Main}CurrentPanel.2");

            elements.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.65 0.997" },
                Button = { Color = "1 1 1 0.5", Command = $"adminespUI_toggle playerdistance {data.PlayerDistance - 50}" },
                Text = { Text = "-", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 20 },
            }, $"{Main}CurrentPanel.2");


            elements.Add(new CuiPanel
            {
                Image =
                {
                    Color = "1 1 1 0.5"
                },
                RectTransform =
                {
                   AnchorMin = "0.5 0", AnchorMax = "1 1"
                },
            }, $"{Main}CurrentPanel.3");

            elements.Add(new CuiElement
            {
                Parent = $"{Main}CurrentPanel.3",
                Components =
                            {
                                new CuiTextComponent { Text = "Админы: ", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"},
                                new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "0.5 1" }
                            }
            });


            elements.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "1 0.997" },
                Button = { Color = !data.ShowAdmins ? "0.60 0.82 0.55 0.5" : "0.94 0.43 0.44 0.5", Command = $"adminespUI_toggle showadmins {!data.ShowAdmins}" },
                Text = { Text = data.ShowAdmins ? "ВЫКЛЮЧИТЬ" : "ВКЛЮЧИТЬ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 12 },
            }, $"{Main}CurrentPanel.3");


            elements.Add(new CuiPanel
            {
                Image =
                {
                    Color = "1 1 1 0.5"
                },
                RectTransform =
                {
                   AnchorMin = "0.5 0", AnchorMax = "1 1"
                },
            }, $"{Main}CurrentPanel.4");

            elements.Add(new CuiElement
            {
                Parent = $"{Main}CurrentPanel.4",
                Components =
                            {
                                new CuiTextComponent { Text = "Имена: ", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"},
                                new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "0.5 1" }
                            }
            });

            elements.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "1 0.997" },
                Button = { Color = !data.DrawNames ? "0.60 0.82 0.55 0.5" : "0.94 0.43 0.44 0.5", Command = $"adminespUI_toggle drawnames {!data.DrawNames}" },
                Text = { Text = data.DrawNames ? "ВЫКЛЮЧИТЬ" : "ВКЛЮЧИТЬ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 12 },
            }, $"{Main}CurrentPanel.4");

            elements.Add(new CuiPanel
            {
                Image =
                {
                    Color = "1 1 1 0.5"
                },
                RectTransform =
                {
                   AnchorMin = "0.5 0", AnchorMax = "1 1"
                },
            }, $"{Main}CurrentPanel.5");

            elements.Add(new CuiElement
            {
                Parent = $"{Main}CurrentPanel.5",
                Components =
                            {
                                new CuiTextComponent { Text = "Боксы: ", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"},
                                new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "0.5 1" }
                            }
            });

            elements.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "1 0.997" },
                Button = { Color = !data.DrawBoxes ? "0.60 0.82 0.55 0.5" : "0.94 0.43 0.44 0.5", Command = $"adminespUI_toggle drawboxes {!data.DrawBoxes}" },
                Text = { Text = data.DrawBoxes ? "ВЫКЛЮЧИТЬ" : "ВКЛЮЧИТЬ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 12 },
            }, $"{Main}CurrentPanel.5");

            elements.Add(new CuiPanel
            {
                Image =
                {
                    Color = "1 1 1 0.5"
                },
                RectTransform =
                {
                   AnchorMin = "0.5 0", AnchorMax = "1 1"
                },
            }, $"{Main}CurrentPanel.6");

            elements.Add(new CuiElement
            {
                Parent = $"{Main}CurrentPanel.6",
                Components =
                            {
                                new CuiTextComponent { Text = "Линия взгяда: ", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"},
                                new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "0.5 1" }
                            }
            });

            elements.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "1 0.997" },
                Button = { Color = !data.DrawEyeLine ? "0.60 0.82 0.55 0.5" : "0.94 0.43 0.44 0.5", Command = $"adminespUI_toggle draweyeline {!data.DrawEyeLine}" },
                Text = { Text = data.DrawEyeLine ? "ВЫКЛЮЧИТЬ" : "ВКЛЮЧИТЬ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 12 },
            }, $"{Main}CurrentPanel.6");


            elements.Add(new CuiPanel
            {
                Image =
                {
                    Color = "1 1 1 0.5"
                },
                RectTransform =
                {
                   AnchorMin = "0.5 0", AnchorMax = "1 1"
                },
            }, $"{Main}CurrentPanel.7");

            elements.Add(new CuiElement
            {
                Parent = $"{Main}CurrentPanel.7",
                Components =
                            {
                                new CuiTextComponent { Text = "Длина линии: ", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"},
                                new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "0.5 1" }
                            }
            });

            elements.Add(new CuiElement
            {
                Parent = $"{Main}CurrentPanel.7",
                Components =
                            {
                                new CuiTextComponent { Text = $"{data.EyeLineDistance.ToString("0")}", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1"},
                                new CuiRectTransformComponent{ AnchorMin = "0.5 0", AnchorMax = "1 1" }
                            }
            });

            elements.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.85 0", AnchorMax = "1 0.997" },
                Button = { Color = "1 1 1 0.5", Command = $"adminespUI_toggle eyelinedistance {data.EyeLineDistance + 10}" },
                Text = { Text = "+", Align = TextAnchor.MiddleCenter, /*Font = "robotocondensed-bold.ttf",*/ FontSize = 18 },
            }, $"{Main}CurrentPanel.7");

            elements.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.65 0.997" },
                Button = { Color = "1 1 1 0.5", Command = $"adminespUI_toggle eyelinedistance {data.EyeLineDistance - 10}" },
                Text = { Text = "-", Align = TextAnchor.MiddleCenter, /*Font = "robotocondensed-bold.ttf", */FontSize = 20 },
            }, $"{Main}CurrentPanel.7");


            CuiHelper.AddUi(player, elements);
        }

        void OnPlayerDisconected(BasePlayer player)
        {
            if (player == null) return;
            if (player.GetComponent<ESPPlayer>() != null)
                UnityEngine.GameObject.Destroy(player.GetComponent<ESPPlayer>());
        }

        static void DestroyAll<T>()
        {
            var objects = GameObject.FindObjectsOfType(typeof(T));
            if (objects != null)
                foreach (var gameObj in objects)
                    GameObject.Destroy(gameObj);
        }

        void LoadData()
        {
            try
            {
                PlayerSettings = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerSetting>>($"{Title}");
                if (PlayerSettings == null)
                {
                    PrintError("File is null! Create new data files");
                    PlayerSettings = new Dictionary<ulong, PlayerSetting>();
                }
            }
            catch
            {
                PlayerSettings = new Dictionary<ulong, PlayerSetting>();
            }
        }

        void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Main);
            CuiHelper.DestroyUi(player, $"{Main}MenuPanel");
        }

        void Unload()
        {
            DestroyAll<ESPPlayer>();
            BasePlayer.activePlayerList.ToList().ForEach(DestroyUI);
            SaveData();
        }

        void SaveData()
        {
            if (PlayerSettings != null) Interface.Oxide.DataFileSystem.WriteObject($"{Title}", PlayerSettings);
        }

        class ESPPlayer : BaseEntity
        {
            BasePlayer player;
            PlayerSetting data;

            public float LastUpdate = 0f;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
            }

            public void Init(PlayerSetting dates)
            {
                data = dates;
            }

            void FixedUpdate()
            {
                if (player == null || player.IsSleeping()) return;
                if (data == null) return;
                if (!player.IsConnected)
                {
                    OnDestroy();
                    return;
                }
                LastUpdate += Time.deltaTime;
                if (LastUpdate > data.UpdateTime)
                {
                    LastUpdate = 0;
                    foreach (var target in BasePlayer.activePlayerList)
                    {
                        if (target == null || target.transform == null || target == player)
                            continue;
                        var currDistance = Math.Floor(Vector3.Distance(target.transform.position, player.transform.position));
                        if (target.IsAdmin && !data.ShowAdmins) continue;
                        if (currDistance > data.PlayerDistance)
                            continue;
                        if (currDistance < data.PlayerDistance)
                        {
                            if (currDistance > 2 && data.DrawNames)
                                DDraw("text", target, Color.cyan, $"{target.displayName} ({currDistance.ToString("#0")} м.)\nHealth: {Math.Floor(target.health)}");
                            if (currDistance > 2 && data.DrawBoxes)
                                DDraw("box", target, Color.cyan);
                            if (data.DrawEyeLine)
                                DDraw("line", target, Color.cyan);

                        }
                    }
                    if (data.Sleepers)
                        foreach (var target in BasePlayer.sleepingPlayerList)
                        {
                            if (target == null || target.transform == null || target == player)
                                continue;
                            var currDistance = Math.Floor(Vector3.Distance(target.transform.position, player.transform.position));
                            if (target.IsAdmin && !data.ShowAdmins) continue;
                            if (currDistance > data.PlayerDistance)
                                continue;
                            if (currDistance < data.PlayerDistance)
                            {
                                if (currDistance > 2 && data.DrawNames)
                                    DDraw("text", target, Color.white, $"СПИТ\n{target.displayName} ({currDistance.ToString("#0")} м.)\nHealth: {Math.Floor(target.health)}");
                                if (currDistance > 2 && data.DrawBoxes && currDistance < 20)
                                    DDraw("box", target, Color.white);
                            }
                        }
                }
            }

            void DDraw(string type, BasePlayer target, Color color, string messages = "")
            {
                switch (type)
                {
                    case "text":
                        if (player.Connection.authLevel < 2) SetPlayerFlag(player, BasePlayer.PlayerFlags.IsAdmin, true);
                        player.SendConsoleCommand("ddraw.text", data.UpdateTime + Time.deltaTime, color, target.eyes.position + new Vector3(0, 0.4f, 0), messages);
                        if (player.Connection.authLevel < 2) SetPlayerFlag(player, BasePlayer.PlayerFlags.IsAdmin, false);
                        break;
                    case "box":
                        if (player.Connection.authLevel < 2) SetPlayerFlag(player, BasePlayer.PlayerFlags.IsAdmin, true);
                        player.SendConsoleCommand("ddraw.box", data.UpdateTime + Time.deltaTime, color, target.transform.position + new Vector3(0f, 1f, 0f), target.GetHeight(target.modelState.ducked));
                        if (player.Connection.authLevel < 2) SetPlayerFlag(player, BasePlayer.PlayerFlags.IsAdmin, false);

                        break;
                    case "line":
                        if (player.Connection.authLevel < 2) SetPlayerFlag(player, BasePlayer.PlayerFlags.IsAdmin, true);
                        player.SendConsoleCommand("ddraw.line", data.UpdateTime + Time.deltaTime, color, target.eyes.position, target.eyes.position + target.eyes.HeadRay().direction * data.EyeLineDistance);
                        if (player.Connection.authLevel < 2) SetPlayerFlag(player, BasePlayer.PlayerFlags.IsAdmin, false);

                        break;

                }

            }

            void SetPlayerFlag(BasePlayer player, BasePlayer.PlayerFlags f, bool b)
            {
                var reply = 5278;
                if (reply == 0) { }
                if (b)
                {
                    if (player.HasPlayerFlag(f)) return;
                    player.playerFlags |= f;
                }
                else
                {
                    if (!player.HasPlayerFlag(f)) return;
                    player.playerFlags &= ~f;
                }
                player.SendNetworkUpdateImmediate(false);
            }

            public void DestroyComponent() => OnDestroy();
            void OnDestroy() =>
                Destroy(this);
        }


        class Position
        {
            public float Xmin;
            public float Xmax;
            public float Ymin;
            public float Ymax;

            public string AnchorMin =>
                $"{Math.Round(Xmin, 4).ToString(CultureInfo.InvariantCulture)} {Math.Round(Ymin, 4).ToString(CultureInfo.InvariantCulture)}";
            public string AnchorMax =>
                $"{Math.Round(Xmax, 4).ToString(CultureInfo.InvariantCulture)} {Math.Round(Ymax, 4).ToString(CultureInfo.InvariantCulture)}";

            public override string ToString()
            {
                return "----------\nAmin:{AnchorMin}\nAmax:{AnchorMax}\n----------";
            }
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        private static List<Position> GetPositions(int colums, int rows, float colPadding = 0, float rowPadding = 0, bool columsFirst = false, ulong playerid = 5404672)
        {
            if (colums == 0)
                throw new ArgumentException("Can't create positions for gui!", nameof(colums));
            if (rows == 0)
                throw new ArgumentException("Can't create positions for gui!", nameof(rows));

            List<Position> result = new List<Position>();
            result.Clear();
            var colsDiv = 1f / colums;
            var rowsDiv = 1f / rows;
            if (colPadding == 0) colPadding = colsDiv / 2;
            if (rowPadding == 0) rowPadding = rowsDiv / 2;
            if (!columsFirst)
                for (int j = rows; j >= 1; j--)
                {
                    for (int i = 1; i <= colums; i++)
                    {
                        Position pos = new Position
                        {
                            Xmin = (i - 1) * colsDiv + colPadding / 2f,
                            Xmax = i * colsDiv - colPadding / 2f,
                            Ymin = (j - 1) * rowsDiv + rowPadding / 2f,
                            Ymax = j * rowsDiv - rowPadding / 2f
                        };
                        result.Add(pos);
                    }
                }
            else
                for (int i = 1; i <= colums; i++)
                {
                    for (int j = rows; j >= 1; j--)
                    {
                        Position pos = new Position
                        {
                            Xmin = (i - 1) * colsDiv + colPadding / 2f,
                            Xmax = i * colsDiv - colPadding / 2f,
                            Ymin = (j - 1) * rowsDiv + rowPadding / 2f,
                            Ymax = j * rowsDiv - rowPadding / 2f
                        };
                        result.Add(pos);
                    }
                }
            return result;
        }

        public string ParseColorFromRGBA(string cssColor)
        {
            cssColor = cssColor.Trim();
            string[] parts = cssColor.Split(' ');
            int r = int.Parse(parts[0], CultureInfo.InvariantCulture);
            int g = int.Parse(parts[1], CultureInfo.InvariantCulture);
            int b = int.Parse(parts[2], CultureInfo.InvariantCulture);
            float a = float.Parse(parts[3], CultureInfo.InvariantCulture);
            var finish = System.Drawing.Color.FromArgb((int)(a * 255), r, g, b);
            cssColor = "#" + finish.R.ToString("X2") + finish.G.ToString("X2") + finish.B.ToString("X2") + finish.A.ToString("X2");
            var str = cssColor.Trim('#');
            if (str.Length == 6)
                str += "FF";
            if (str.Length != 8)
            {
                throw new Exception(cssColor);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }
            var r1 = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g1 = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b1 = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a1 = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);
            Color color = new Color32(r1, g1, b1, a1);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
    }
}