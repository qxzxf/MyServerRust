using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CheckPlayersRCC", "qxzxf", "0.0.2")]
    public class CheckPlayersRCC : RustPlugin
    {
        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Thanks for using the plugin from developer OxideBro. Default configuration loaded");
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();

            if (config.PluginVersion < Version)
                UpdateConfigValues();

            Config.WriteObject(config, true);
        }

        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.DefaultConfig();
            if (config.PluginVersion < new VersionNumber(0, 1, 0))
            {
                PrintWarning("Config update detected! Updating config values...");
                PrintWarning("Config update completed!");
            }
            config.PluginVersion = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        [ConsoleCommand("rccignore")]
        void cmdIgnoreListAdd(ConsoleSystem.Arg args)
        {
            if (args.Connection != null && args.Player().Connection.authLevel < 2) return;
            if (!args.HasArgs(1)) return;
            ulong steamid;
            if (!ulong.TryParse(args.Args[0], out steamid))
            {
                args.ReplyWith("You did not specify SteamID, use rccignore STEAMID");
                return;
            }
            if (config.IgnoreList.Contains(steamid))
            {
                args.ReplyWith($"This SteamID  {steamid} appears in the IgnoreList");
                return;
            }

            config.IgnoreList.Add(steamid);
            SaveConfig();
            args.ReplyWith($"Steamid {steamid} was successfully added to the ignorlist ");
        }

        class PluginConfig
        {
            [JsonProperty("Configuration Version")]
            public VersionNumber PluginVersion = new VersionNumber();

            [JsonProperty("RustCheatCheck API ключ")]
            public string RCCApiKey = "";

            [JsonProperty("VK API ключ")]
            public string VkAPIKey = "";

            [JsonProperty("VK диалог")]
            public string VkDialog = "";

            [JsonProperty("Discord WebHook")]
            public string DiscordWebHook = "";

            [JsonProperty("Список игнорируемых игроков (Добавляйте вручную, либо команда rccignore STEAMID)")]
            public List<ulong> IgnoreList = new List<ulong>();

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    PluginVersion = new VersionNumber(),
                    DiscordWebHook = "",
                    IgnoreList = new List<ulong>(),
                    RCCApiKey = "",
                    VkAPIKey = "",
                    VkDialog = ""
                };
            }
        }

        private void OnServerInitialized()
        {
            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player == null || config.IgnoreList.Contains(player.userID)) return;
            if (player.IsReceivingSnapshot)
            {
                timer.Once(1f, () => OnPlayerConnected(player));
                return;
            }
            GetPlayerInfo(player, r =>
            {
                if (r != null)
                {
                    var bansSelect = r.bans.Select(p => $"{p.serverName} ({p.reason})");

                    var messages = $"@online На сервер зашёл игрок {player.displayName} ({player.UserIDString}) с банами на других серверах : {string.Join(", ", bansSelect)}";
                    VkLog(messages);
                    DiscordLog(player, r);
                }
            });
        }

        public class last_check
        {
            string moderSteamID;
            string serverName;
            int time;
        }

        public class bans
        {
            public int banID;
            public string reason;
            public string serverName;
            public long banDate;
            public long unbanDate;
        }

        public class RCCStatus
        {
            public string status;
            public ulong steamid;
            public int rcc_checks;
            public List<last_check> last_check;
            public List<string> last_ip;
            public string last_nick;
            public List<string> another_accs;
            public List<bans> bans;
        }

        private void GetPlayerInfo(BasePlayer player, Action<RCCStatus> callback)
        {
            if (string.IsNullOrEmpty(config.RCCApiKey))
                return;

            webrequest.Enqueue($"https://rustcheatcheck.ru/panel/api?action=getInfo&key={config.RCCApiKey}&player={player.UserIDString}", "", (code, response) =>
             {
                
                 switch (code)
                 {
                     case 200:
                         if (response.Contains("errorreason"))
                         {
                             callback?.Invoke(null);
                             return;
                         }
                         var parseInfo = JsonConvert.DeserializeObject<RCCStatus>(response);

                         if (parseInfo.bans == null || parseInfo.bans.Count == 0)
                         {
                             callback?.Invoke(null);
                             return;
                         }
                         callback?.Invoke(parseInfo);
                         return;
                     default:
                         PrintError("Connection error to rustcheatcheck.ru, code: {0}", code);
                         callback?.Invoke(null);
                         return;
                 }
             }, this, RequestMethod.GET);
            callback?.Invoke(null);
        }

        void VkLog(string Message)
        {
            if (string.IsNullOrEmpty(config.VkAPIKey) || string.IsNullOrEmpty(config.VkDialog))
            {
                PrintWarning("You have not configured the configuration, in the paragraph with VK");
                return;
            }
            int RandomID = UnityEngine.Random.Range(0, 9999);
            webrequest.Enqueue($"https://api.vk.com/method/messages.send?chat_id={config.VkDialog}&random_id={RandomID}&message={URLEncode(Message)}&access_token={config.VkAPIKey}&v=5.92", null, (code, response) => { }, this);
        }

        void DiscordLog(BasePlayer player, RCCStatus info)
        {
            if (string.IsNullOrEmpty(config.DiscordWebHook))
            {
                PrintWarning("You have not configured the configuration, under Discord");
                return;
            }
            List<Fields> fields = new List<Fields>();
            foreach (var ban in info.bans)
                fields.Add(new Fields(ban.serverName, ban.reason, true));

            var messages = $"На сервер зашёл игрок {player.displayName} ({player.UserIDString}) с банами на других серверах";

            if (info.another_accs != null && info.another_accs.Count > 0)
                messages += $"\nВозможные аккаунты: {string.Join(", ", info.another_accs)}";

            FancyMessage newMessage = new FancyMessage("@everyone", false, new FancyMessage.Embeds[1] { new FancyMessage.Embeds(messages, 15158332, fields) });
            Request(config.DiscordWebHook, newMessage.toJSON());
        }

        public string URLEncode(string input)
        {
            if (input.Contains("#")) input = input.Replace("#", "%23");
            if (input.Contains("$")) input = input.Replace("$", "%24");
            if (input.Contains("+")) input = input.Replace("+", "%2B");
            if (input.Contains("/")) input = input.Replace("/", "%2F");
            if (input.Contains(":")) input = input.Replace(":", "%3A");
            if (input.Contains(";")) input = input.Replace(";", "%3B");
            if (input.Contains("?")) input = input.Replace("?", "%3F");
            if (input.Contains("@")) input = input.Replace("@", "%40");
            return input;
        }

        public class FancyMessage
        {
            public string content { get; set; }
            public bool tts { get; set; }
            public Embeds[] embeds { get; set; }

            public class Embeds
            {
                public string title { get; set; }
                public int color { get; set; }
                public List<Fields> fields { get; set; }
                public Embeds(string title, int color, List<Fields> fields)
                {
                    this.title = title;
                    this.color = color;
                    this.fields = fields;
                }
            }

            public FancyMessage(string content, bool tts, Embeds[] embeds)
            {
                this.content = content;
                this.tts = tts;
                this.embeds = embeds;
            }

            public string toJSON() => JsonConvert.SerializeObject(this);
        }

        public class Fields
        {
            public string name { get; set; }
            public string value { get; set; }
            public bool inline { get; set; }
            public Fields(string name, string value, bool inline)
            {
                this.name = name;
                this.value = value;
                this.inline = inline;
            }
        }

        private void Request(string url, string payload, Action<int> callback = null)
        {
            webrequest.Enqueue(url, payload, (code, response) =>
            {
                if (code != 200 && code != 204)
                {
                    if (response != null)
                    {
                        try
                        {
                            JObject json = JObject.Parse(response);
                            if (code == 429)
                            {
                                float seconds = float.Parse(Math.Ceiling((double)(int)json["retry_after"] / 1000).ToString());
                            }
                            else
                            {
                                PrintWarning($"Discord rejected that payload! Responded with \"{json["message"].ToString()}\" Code: {code}");
                            }
                        }
                        catch
                        {
                            PrintWarning($"Failed to get a valid response from discord! Error: \"{response}\" Code: {code}");
                        }
                    }
                    else
                    {
                        PrintWarning($"Discord didn't respond (down?) Code: {code}");
                    }
                }
                try
                {
                    callback?.Invoke(code);
                }
                catch (Exception ex)
                {
                    Interface.Oxide.LogException("[DiscordMessages] Request callback raised an exception!", ex);
                }
            }, this, Core.Libraries.RequestMethod.POST, new Dictionary<string, string> { ["Content-Type"] = "application/json" });
        }
    }
}