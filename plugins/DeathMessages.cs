using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("DeathMessages", "qxzxf", "2.3.1")]
    class DeathMessages : RustPlugin
    {
        private static DeathMessages ins;

        private List<DeathMessage> _notes = new List<DeathMessage>();
        private Dictionary<ulong, HitInfo> _lastHits = new Dictionary<ulong, HitInfo>();

        #region Classes / Enums

        public class ColorsPrivilage
        {
            [JsonProperty("Цвет имени если игрока убили")]
            public string ColorDeath;
            [JsonProperty("Цвет имени если игрок убил")]
            public string ColorAttacker;
        }

        enum AttackerType
        {
            Player,
            Helicopter,
            Animal,
            Turret,
            Guntrap,
            Structure,
            Trap,
            Invalid,
            NPC,
            BradleyAPC,
            Zombie,
            ZombieDeath
        }

        enum VictimType
        {
            Player,
            Helicopter,
            Animal,
            Invalid,
            NPC,
            BradleyAPC,
            Zombie,
            ZombieDeath
        }

        enum DeathReason
        {
            Turret,
            Guntrap,
            Helicopter,
            HelicopterDeath,
            BradleyAPC,
            BradleyAPCDeath,
            Structure,
            Trap,
            Animal,
            AnimalDeath,
            Generic,
            Zombie,
            ZombieDeath,
            Hunger,
            Thirst,
            Cold,
            Drowned,
            Heat,
            Bleeding,
            Poison,
            Suicide,
            Bullet,
            Arrow,
            Flamethrower,
            Slash,
            Blunt,
            Fall,
            Radiation,
            Stab,
            Explosion,
            Unknown
        }

        class Attacker
        {
            public Attacker(BaseEntity entity)
            {
                Entity = entity;
                Type = InitializeType();
                Name = InitializeName();
            }

            public BaseEntity Entity { get; }

            public string Name { get; }

            public AttackerType Type { get; }

            private AttackerType InitializeType()
            {
                if (Entity == null)
                    return AttackerType.Invalid;

                if (Entity is BaseAnimalNPC)
                    return AttackerType.Animal;

                if (Entity.name.Contains("machete.weapon"))
                    return AttackerType.Zombie;

                if (Entity is NPCPlayer)
                    return AttackerType.NPC;

                if (Entity.IsNpc)
                    return AttackerType.NPC;

                if (Entity is BasePlayer)
                    return AttackerType.Player;

                if (Entity is BaseHelicopter)
                    return AttackerType.Helicopter;

                if (Entity is BradleyAPC)
                    return AttackerType.BradleyAPC;



                if (Entity.name.Contains("barricades/") || Entity.name.Contains("wall.external.high"))
                    return AttackerType.Structure;

                if (Entity.name.Contains("beartrap.prefab") || Entity.name.Contains("landmine.prefab") || Entity.name.Contains("spikes.floor.prefab"))
                    return AttackerType.Trap;

                if (Entity.name.Contains("autoturret_deployed.prefab") || Entity.name.Contains("flameturret.deployed.prefab") || Entity.name.Contains("sentry.scientist.static"))
                    return AttackerType.Turret;
                if (Entity.name.Contains("guntrap_deployed.prefab") || Entity.name.Contains("guntrap.deployed.prefab"))
                    return AttackerType.Guntrap;

                return AttackerType.Invalid;
            }

            private string InitializeName()
            {
                if (Entity == null)
                    return null;
                int name;
                switch (Type)
                {
                    case AttackerType.Player:
                        return Entity.ToPlayer().displayName;
                    case AttackerType.NPC:
                        return string.IsNullOrEmpty(Entity.ToPlayer()?.displayName) ? _config.NPCName : int.TryParse(Entity.ToPlayer().displayName, out name) ? _config.NPCName : Entity.ToPlayer().displayName + $"( {_config.NPCName})";
                    case AttackerType.Helicopter:
                        return "Patrol Helicopter";
                    case AttackerType.BradleyAPC:
                    case AttackerType.Turret:
                    case AttackerType.Guntrap:
                    case AttackerType.Trap:
                    case AttackerType.Animal:
                    case AttackerType.Structure:
                        return FormatName(Entity.name);
                }

                return string.Empty;
            }
        }

        class Victim
        {
            public Victim(BaseCombatEntity entity)
            {
                Entity = entity;
                Type = InitializeType();
                Name = InitializeName();
            }

            public BaseCombatEntity Entity { get; }

            public string Name { get; }

            public VictimType Type { get; }

            private VictimType InitializeType()
            {
                if (Entity == null)
                    return VictimType.Invalid;

                if (Entity is BaseAnimalNPC)
                    return VictimType.Animal;

                if (Entity.IsNpc)
                    return VictimType.NPC;

                if (Entity.name.Contains("machete.weapon"))
                    return VictimType.Zombie;

                if (Entity is NPCPlayer)
                    return VictimType.NPC;




                if (Entity is BasePlayer)
                    return VictimType.Player;

                if (Entity is BaseHelicopter)
                    return VictimType.Helicopter;

                if (Entity is BradleyAPC)
                    return VictimType.BradleyAPC;

                return VictimType.Invalid;
            }



            private string InitializeName()
            {
                int name;
                switch (Type)
                {
                    case VictimType.Zombie:
                        return "ZombieName";

                    case VictimType.Player:
                        return Entity.ToPlayer().displayName;

                    case VictimType.NPC:
                        return string.IsNullOrEmpty(Entity.ToPlayer()?.displayName) ? _config.NPCName : int.TryParse(Entity.ToPlayer().displayName, out name) ? _config.NPCName : Entity.ToPlayer().displayName + $" ({_config.NPCName})";

                    case VictimType.Helicopter:
                        return "Patrol Helicopter";

                    case VictimType.BradleyAPC:
                        return "BradleyAPCName";

                    case VictimType.Animal:
                        return FormatName(Entity.name);
                }
                return string.Empty;
            }
        }

        class DeathMessage
        {

            public string UINotes;

            public DeathMessage(Attacker attacker, Victim victim, string weapon, string damageType, string bodyPart, double distance)
            {
                Attacker = attacker;
                Victim = victim;
                Weapon = weapon;
                DamageType = damageType;
                BodyPart = bodyPart;
                Distance = distance;
                Reason = InitializeReason();
                Message = InitializeDeathMessage();

                if (_config.Distance <= 0)
                {
                    Players = BasePlayer.activePlayerList.ToList();
                }
                else
                {
                    var position = attacker?.Entity?.transform?.position;
                    if (position == null)
                        position = victim?.Entity?.transform?.position;

                    if (position != null)
                        Players = BasePlayer.activePlayerList.Where(x => x.Distance((UnityEngine.Vector3)position) <= _config.Distance).ToList();
                    else
                        Players = new List<BasePlayer>();
                }

                if (victim.Type == VictimType.Player && !Players.Contains(victim.Entity.ToPlayer()))
                    Players.Add(victim.Entity.ToPlayer());

                if (attacker.Type == AttackerType.Player && !Players.Contains(attacker.Entity.ToPlayer()))
                    Players.Add(attacker.Entity.ToPlayer());

                UINotes = CuiHelper.GetGuid();
            }

            public List<BasePlayer> Players { get; }

            public Attacker Attacker { get; }

            public Victim Victim { get; }

            public string Weapon { get; }

            public string BodyPart { get; }

            public string DamageType { get; }

            public double Distance { get; }

            public DeathReason Reason { get; }

            public string Message { get; }

            private DeathReason InitializeReason()
            {
                if (Attacker.Type == AttackerType.Turret)
                    return DeathReason.Turret;

                if (Attacker.Type == AttackerType.Guntrap)
                    return DeathReason.Guntrap;

                if (Attacker.Type == AttackerType.Zombie)
                    return DeathReason.Zombie;

                else if (Attacker.Type == AttackerType.Helicopter)
                    return DeathReason.Helicopter;

                else if (Attacker.Type == AttackerType.BradleyAPC)
                    return DeathReason.BradleyAPC;

                else if (Victim.Type == VictimType.Helicopter)
                    return DeathReason.HelicopterDeath;

                else if (Victim.Type == VictimType.BradleyAPC)
                    return DeathReason.BradleyAPCDeath;

                else if (Attacker.Type == AttackerType.Structure)
                    return DeathReason.Structure;

                else if (Attacker.Type == AttackerType.Trap)
                    return DeathReason.Trap;

                else if (Attacker.Type == AttackerType.Animal)
                    return DeathReason.Animal;

                else if (Victim.Type == VictimType.Animal)
                    return DeathReason.AnimalDeath;

                else if (Weapon == "F1 Grenade" || Weapon == "Survey Charge" || Weapon == "Timed Explosive Charge" || Weapon == "Satchel Charge" || Weapon == "Beancan Grenade")
                    return DeathReason.Explosion;

                else if (Weapon == "Flamethrower")
                    return DeathReason.Flamethrower;

                else if (Victim.Type == VictimType.Player || Victim.Type == VictimType.NPC)
                    return GetDeathReason(DamageType);

                if (Victim.Type == VictimType.Zombie)
                    return DeathReason.ZombieDeath;

                return DeathReason.Unknown;
            }

            private DeathReason GetDeathReason(string damage)
            {
                var reasons = (Enum.GetValues(typeof(DeathReason)) as DeathReason[]).Where(x => x.ToString().Contains(damage));

                if (reasons.Count() == 0)
                    return DeathReason.Unknown;
                return reasons.First();
            }

            private string InitializeDeathMessage()
            {
                string message = string.Empty;
                string reason = string.Empty;

                if (Victim.Type == VictimType.Player && Victim.Entity.ToPlayer().IsSleeping() && _config.Messages.ContainsKey(Reason + " Sleeping"))
                    reason = Reason + " Sleeping";
                else
                    reason = Reason.ToString();

                message = GetMessage(reason, _config.Messages);

                var attackerName = Attacker.Name;
                if (string.IsNullOrEmpty(attackerName) && Attacker.Entity == null && Weapon.Contains("Heli"))
                    attackerName = _config.HelicopterName;

                if (string.IsNullOrEmpty(attackerName) && Attacker.Entity == null && Weapon.Contains("Bradl"))
                    attackerName = _config.BradleyAPCName;


                switch (Attacker.Type)
                {
                    case AttackerType.ZombieDeath:
                        attackerName = _config.ZombieName;
                        break;

                    case AttackerType.Zombie:
                        attackerName = _config.ZombieName;
                        break;

                    case AttackerType.Helicopter:
                        attackerName = _config.HelicopterName;
                        break;

                    case AttackerType.BradleyAPC:
                        attackerName = _config.BradleyAPCName;
                        break;

                    case AttackerType.NPC:
                        attackerName = _config.NPCName;
                        break;

                    case AttackerType.Turret:
                        attackerName = GetMessage(attackerName, _config.Turrets);
                        break;
                    case AttackerType.Guntrap:
                        attackerName = GetMessage(attackerName, _config.Turrets);
                        break;

                    case AttackerType.Trap:
                        attackerName = GetMessage(attackerName, _config.Traps);
                        break;

                    case AttackerType.Animal:
                        attackerName = GetMessage(attackerName, _config.Animals);
                        break;

                    case AttackerType.Structure:
                        attackerName = GetMessage(attackerName, _config.Structures);
                        break;

                }

                var victimName = Victim.Name;
                switch (Victim.Type)
                {
                    case VictimType.Helicopter:
                        victimName = _config.HelicopterName;
                        break;

                    case VictimType.BradleyAPC:
                        victimName = _config.BradleyAPCName;
                        break;

                    case VictimType.Zombie:
                        victimName = _config.ZombieName;
                        break;

                    case VictimType.Animal:
                        victimName = GetMessage(victimName, _config.Animals);
                        break;
                }
                var reply = 0;
                var victimColor = _config.ColorPrivilage["deathmessages.default"].ColorDeath;
                var attackerColor = _config.ColorPrivilage["deathmessages.default"].ColorAttacker;
                foreach (var color in _config.ColorPrivilage)
                {
                    if (Attacker.Entity != null && Attacker.Entity.ToPlayer())
                        if (ins.permission.UserHasPermission(Attacker.Entity.ToPlayer().UserIDString, color.Key))
                            attackerColor = color.Value.ColorAttacker;
                    if (Victim.Entity != null && Victim.Entity.ToPlayer())
                        if (ins.permission.UserHasPermission(Victim.Entity.ToPlayer().UserIDString, color.Key))
                            victimColor = color.Value.ColorDeath;
                }
                message = message.Replace("{victim}", $"<color={victimColor}>{victimName}</color>");
                message = message.Replace("{attacker}", $"<color={attackerColor}>{attackerName}</color>");
                message = message.Replace("{distance}", $"<color={_config.ColorDistance}>{Math.Round(Distance, 0)}</color>");
                message = message.Replace("{weapon}", $"<color={_config.ColorWeapon}>{GetMessage(Weapon, _config.Weapons)}</color>");
                message = message.Replace("{bodypart}", $"<color={_config.ColorBodyPart}>{GetMessage(BodyPart, _config.BodyParts)}</color>");
                return message;
            }
        }

        #endregion

        #region Oxide Hooks
        private static PluginConfig _config;

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за приобритение плагина на сайте rustmods..ru. Если вы приобрели этот плагин на другом ресурсе знайте - это лишает вас гарантированных обновлений!");
            _config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<PluginConfig>();
            if (_config.PluginVersion < Version)
                UpdateConfigValues();
            Config.WriteObject(_config, true);
        }

        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.DefaultConfig();
            if (_config.PluginVersion < new VersionNumber(0, 1, 0))
            {
                PrintWarning("Config update detected! Updating config values...");
                PrintWarning("Config update completed!");
            }
            _config.PluginVersion = Version;
        }


        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        class PluginConfig
        {
            [JsonProperty("Configuration Version")]
            public VersionNumber PluginVersion = new VersionNumber();

            [JsonProperty("A. Время показа сообщения (сек)")]
            public int Cooldown { get; set; }
            [JsonProperty("B. Размер текста")]
            public int FontSize { get; set; }
            [JsonProperty("C. Показывать убиства животных")]
            public bool ShowDeathAnimals { get; set; }
            [JsonProperty("C1. Показывать убиства NPC")]
            public bool ShowDeathNPC { get; set; }
            [JsonProperty("D. Показывать убийства спящих")]
            public bool ShowDeathSleepers { get; set; }
            [JsonProperty("E. Хранение логов")]
            public bool Log { get; set; }
            [JsonProperty("H. Цвет оружия")]
            public string ColorWeapon { get; set; }
            [JsonProperty("I. Цвет дистанции")]
            public string ColorDistance { get; set; }
            [JsonProperty("J. Цвет части тела")]
            public string ColorBodyPart { get; set; }
            [JsonProperty("K. Дистанция")]
            public double Distance { get; set; }
            [JsonProperty("L. Название вертолета")]
            public string HelicopterName { get; set; }
            [JsonProperty("M. Название Bradlay (Танк)")]
            public string BradleyAPCName { get; set; }
            [JsonProperty("N. Имя NPC")]
            public string NPCName { get; set; }
            [JsonProperty("O. Имя Zombie")]
            public string ZombieName { get; set; }
            [JsonProperty("P. Выводить убийства в консоль")]
            public bool ShowColsole { get; set; }
            [JsonProperty("Оружие")]
            public Dictionary<string, string> Weapons { get; set; }
            [JsonProperty("Цвета имени в UI")]
            public Dictionary<string, ColorsPrivilage> ColorPrivilage
            {
                get; set;
            }

            [JsonProperty("Позиция: AnchorMin (Это изнаальная позиция точки, от неё в лево будет уходить основное UI по оффсетам, дефолт 1 1 - Верхний правый угол)")]
            public string AnchorMin = "1 1";
            [JsonProperty("Позиция: AnchorMax (Это изнаальная позиция точки, от неё в лево будет уходить основное UI по оффсетам, дефолт 1 1 - Верхний правый угол)")]
            public string AnchorMax = "1 1";
            [JsonProperty("Конструкции")]
            public Dictionary<string, string> Structures { get; set; }
            [JsonProperty("Ловушки")]
            public Dictionary<string, string> Traps { get; set; }
            [JsonProperty("Турели")]
            public Dictionary<string, string> Turrets { get; set; }
            [JsonProperty("Животные")]
            public Dictionary<string, string> Animals { get; set; }
            [JsonProperty("Сообщения ({attacker} - инициатор,  {victim} - жертва, {weapon} - оружие,  {distance} - дистанция, {bodypart} - часть тела")]
            public Dictionary<string, string> Messages { get; set; }
            [JsonProperty("Части тела")]
            public Dictionary<string, string> BodyParts { get; set; }


            [JsonIgnore]
            [JsonProperty("Server Initialized")]
            public bool Init = false;

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    PluginVersion = new VersionNumber(),
                    Cooldown = 7,
                    FontSize = 15,
                    Distance = -1,
                    Log = true,
                    ShowDeathAnimals = true,
                    ShowDeathNPC = true,
                    ShowDeathSleepers = true,
                    ShowColsole = false,
                    ColorDistance = "#ff9c00",
                    ColorWeapon = "#ffffff",
                    ColorBodyPart = "#ffffff",
                    HelicopterName = "Вертолет",
                    BradleyAPCName = "Танк",
                    NPCName = "НПЦ",
                    ZombieName = "Зомби",
                    AnchorMin = "1 1",
                    AnchorMax = "1 1",
                    ColorPrivilage = new Dictionary<string, ColorsPrivilage>
                    {
                        ["deathmessages.default"] = new ColorsPrivilage
                        {
                            ColorAttacker = "#ff9c00",
                            ColorDeath = "#ff9c00"
                        },
                        ["deathmessages.vip"] = new ColorsPrivilage
                        {
                            ColorAttacker = "#F70233",
                            ColorDeath = "#757575"
                        },
                        ["deathmessages.elite"] = new ColorsPrivilage
                        {
                            ColorAttacker = "#DF0BBA",
                            ColorDeath = "#D1D1D1"
                        },
                    },

                    Weapons = new Dictionary<string, string>
                {
                    { "Assault Rifle", "Assault Rifle" },
                    { "Assault Rifle - ICE", "Assault Rifle" },
                    { "Beancan Grenade", "Beancan" },
                    { "Nailgun", "Гвоздострел" },
                    { "Bolt Action Rifle", "Bolt Action Rifle" },
                    { "Bone Club", "Bone Club" },
                    { "Bone Knife", "Bone Knife" },
                    { "Crossbow", "Crossbow" },
                    { "Flamethrower", "Flamethrower" },
                       { "Explosivesatchel", "Explosivesatchel" },
                    { "Custom SMG", "SMG" },
                    { "Double Barrel Shotgun", "Double Shotgun" },
                    { "Compound Bow", "Compound Bow" },
                    { "Eoka Pistol", "Eoka" },
                    { "F1 Grenade", "F1" },
                    { "Flame Thrower", "Flame Thrower" },
                    { "Hunting Bow", "Hunting Bow" },
                    { "Longsword", "Longsword" },
                    { "LR-300 Assault Rifle", "LR-300" },
                    { "M249", "М249" },
                    { "M92 Pistol", "M92" },
                    { "Mace", "Mace" },
                    { "Machete", "Machete" },
                    { "MP5A4", "MP5A4" },
                    { "Pump Shotgun", "Shotgun" },
                    { "Python Revolver", "Python Revolver" },
                    { "Revolver", "Revolver" },
                    { "Salvaged Cleaver", "Salvaged Cleaver" },
                    { "Salvaged Sword", "Salvaged Sword" },
                    { "Semi-Automatic Pistol", "Semi-Automatic Pistol" },
                    { "Semi-Automatic Rifle", "Semi-Automatic Rifle" },
                    { "Stone Spear", "Stone Spear" },
                    { "Thompson", "Thompson" },
                    { "Waterpipe Shotgun", "Waterpipe Shotgun" },
                    { "Wooden Spear", "Wooden Spear" },
                    { "Hatchet", "Hatchet" },
                    { "Pick Axe", "Pick Axe" },
                    { "Salvaged Axe", "Salvaged Axe" },
                    { "Salvaged Hammer", "Salvaged Hammer" },
                    { "Salvaged Icepick", "Salvaged Icepick" },
                    { "Satchel Charge", "Satchel Charge" },
                    { "Stone Hatchet", "Stone Hatchet" },
                    { "Stone Pick Axe", "Stone Pick Axe" },
                    { "Survey Charge", "Survey Charge" },
                    { "Timed Explosive Charge", "С4" },
                    { "Torch", "Torch" },
                    { "Stone Pickaxe", "Stone Pickaxe" },
                    { "RocketSpeed", "Скоростная ракета" },
                    { "Incendiary Rocket", "Зажигательная ракета" },
                    { "Rocket", "Обычная ракета" },
                    { "RocketHeli", "Напалм" },
                    { "RocketBradley", "Напалм" },
                    { "Spas-12 Shotgun", "Spas-12 Shotgun" },
                    {"Multiple Grenade Launcher", "Multiple Grenade Launcher" },
                    {"40mm.grenade.he", "Multiple Grenade Launcher" },
                },

                    Structures = new Dictionary<string, string>
                {
                    { "Wooden Barricade", "Деревянная баррикада" },
                    { "Barbed Wooden Barricade", "Колючая деревянная баррикада" },
                    { "Metal Barricade", "Металлическая баррикада" },
                    { "High External Wooden Wall", "Высокая внешняя деревянная стена" },
                    { "High External Stone Wall", "Высокая внешняя каменная стена" },
                    { "High External Wooden Gate", "Высокие внешние деревянные ворота" },
                    { "High External Stone Gate", "Высокие внешние каменные ворота" }
                },

                    Traps = new Dictionary<string, string>
                {
                    { "Snap Trap", "Капкан" },
                    { "Land Mine", "Мина" },
                    { "Wooden Floor Spikes", "Деревянные колья" }
                },

                    Turrets = new Dictionary<string, string>
                {
                    { "Flame Turret", "Огнеметная турель" },
                    { "Auto Turret", "Автотурель" },
                    { "Guntrap", "Автодробовик" },
                    { "Static Turret", "Автоматическая туррель" },
                },

                    Animals = new Dictionary<string, string>
                {
                    { "Boar", "Кабан" },
                    { "Horse", "Лошадь" },
                    { "Wolf", "Волк" },
                    { "Stag", "Олень" },
                    { "Chicken", "Курица" },
                    { "Bear", "Медведь" }
                },

                    BodyParts = new Dictionary<string, string>
                {
                    { "body", "Тело" },
                    { "pelvis", "Таз" },
                    { "hip", "Бедро" },
                    { "left knee", "Левое колено" },
                    { "right knee", "Правое колено" },
                    { "left foot", "Левая стопа" },
                    { "right foot", "Правая стопа" },
                    { "left toe", "Левый палец" },
                    { "right toe", "Правый палец" },
                    { "groin", "Пах" },
                    { "lower spine", "Нижний позвоночник" },
                    { "stomach", "Желудок" },
                    { "chest", "Грудь" },
                    { "neck", "Шея" },
                    { "left shoulder", "Левое плечо" },
                    { "right shoulder", "Правое плечо" },
                    { "left arm", "Левая рука" },
                    { "right arm", "Правая рука" },
                    { "left forearm", "Левое предплечье" },
                    { "right forearm", "Правое предплечье" },
                    { "left hand", "Левая ладонь" },
                    { "right hand", "Правая ладонь" },
                    { "left ring finger", "Левый безымянный палец" },
                    { "right ring finger", "Правый безымянный палец" },
                    { "left thumb", "Левый большой палец" },
                    { "right thumb", "Правый большой палец" },
                    { "left wrist", "Левое запястье" },
                    { "right wrist", "Правое запястье" },
                    { "head", "Голова" },
                    { "jaw", "Челюсть" },
                    { "left eye", "Левый глаз" },
                    { "right eye", "Правый глаз" }
                },

                    Messages = new Dictionary<string, string>
                {
                    { "Arrow", "{attacker} убил {victim} ({weapon}, {distance} м.)" },
                    { "Blunt",  "{attacker} убил {victim} ({weapon})" },
                    { "Bullet", "{attacker} убил {victim} ({weapon}, {distance} м.)" },
                    { "Flamethrower", "{attacker} сжег заживо игрока {victim} ({weapon})" },
                    { "Drowned", "{victim} утонул." },
                    { "Explosion", "{attacker} взорвал игрока {victim} ({weapon})" },
                    { "Fall", "{victim} разбился." },
                    { "Generic", "Смерть забрала {victim} с собой." },
                    { "Heat", "{victim} сгорел заживо." },
                    { "Helicopter", "{attacker} прямым попаданием убил {victim}." },
                    { "BradleyAPC", "{attacker} прямым попаданием убил {victim}." },
                    { "BradleyAPCDeath", "{victim} был уничтожен игроком {attacker} ({weapon})" },
                    { "HelicopterDeath", "{victim} был сбит игроком {attacker} ({weapon})" },
                    { "Animal", "{attacker} добрался до {victim}" },
                    { "ZombieDeath", "{attacker} убил {victim} ({weapon}, {distance} м.)" },
                    { "Zombie", "{attacker} приследовал {victim}." },
                    { "AnimalDeath", "{attacker} убил {victim} ({weapon}, {distance} м.)" },
                    { "Hunger", "{victim} умер от голода." },
                    { "Poison", "{victim} умер от отравления." },
                    { "Radiation", "{victim} умер от радиационного отравления" },
                    { "Slash", "{attacker} убил {victim} ({weapon})" },
                    { "Stab", "{attacker} убил {victim} ({weapon})" },
                    { "Structure", "{victim} умер от сближения с {attacker}" },
                    { "Suicide", "{victim} совершил самоубийство." },
                    { "Thirst", "{victim} умер от обезвоживания" },
                    { "Trap", "{victim} попался на ловушку {attacker}" },
                    { "Cold", "{victim} умер от холода" },
                    { "Turret", "{victim} был убит автоматической турелью" },
                    { "Guntrap", "{victim} был убит ловушкой-дробовиком" },
                    { "Unknown", "У {victim} что-то пошло не так." },
                    { "Bleeding", "{victim} умер от кровотечения" },
                    { "Blunt Sleeping", "{attacker} убил {victim} ({weapon})" },
                    { "Bullet Sleeping", "{attacker} убил {victim} ({weapon}, {distance} метров)" },
                    { "Flamethrower Sleeping", "{attacker} сжег игрока {victim} ({weapon})" },
                    { "Explosion Sleeping", "{attacker} убил {victim} ({weapon})" },
                    { "Generic Sleeping", "Смерть забрала {victim} с собой пока он спал." },
                    { "Helicopter Sleeping", "{victim} был убит {attacker} пока он спал." },
                    { "BradleyAPC Sleeping", "{victim} был убит {attacker} пока он спал." },
                    { "Animal Sleeping", "{victim} убил {attacker} пока он спал." },
                    { "Slash Sleeping", "{attacker} убил {victim} ({weapon})" },
                    { "Stab Sleeping", "{attacker} убил {victim} ({weapon})" },
                    { "Unknown Sleeping", "У игрока {victim} что-то пошло не так." },
                    { "Turret Sleeping", "{attacker} был убит автоматической турелью." }
                }
                };
            }
        }

        private void OnServerInitialized()
        {
            ins = this;
            PermissionService.RegisterPermissions(this, _config.ColorPrivilage.Keys.ToList());
        }

        private Dictionary<uint, BasePlayer> LastHeli = new Dictionary<uint, BasePlayer>();

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BasePlayer)
                _lastHits[entity.ToPlayer().userID] = info;
            if (entity is BaseHelicopter && info.InitiatorPlayer != null)
                LastHeli[entity.net.ID] = info.InitiatorPlayer;
        }

        private void OnEntityDeath(BaseCombatEntity victim, HitInfo info)
        {
            try
            {
                if (info == null)
                    if (!(victim is BasePlayer) || !victim.ToPlayer().IsWounded() || !_lastHits.TryGetValue(victim.ToPlayer().userID, out info))
                        return;
                if (victim is BaseCorpse) return;
                var _weapon = FirstUpper(info?.Weapon?.GetItem()?.info?.displayName?.english) ?? FormatName(info?.WeaponPrefab?.name);
                var _damageType = FirstUpper(victim.lastDamage.ToString());

                var _victim = new Victim(victim);
                if (_victim == null)
                    return;
                var _attacker = new Attacker(info.Initiator);
                if (_attacker == null)
                    return;
                if (_victim.Type == VictimType.Invalid)
                    return;
                if (_victim.Type == VictimType.Helicopter)
                {
                    if (LastHeli.ContainsKey(victim.net.ID))
                        _attacker = new Attacker(LastHeli[victim.net.ID]);
                }

                if ((_victim.Type == VictimType.Zombie && _attacker.Type == AttackerType.NPC))
                    return;

                if (!_config.ShowDeathAnimals && _victim.Type == VictimType.Animal || _attacker.Type == AttackerType.Animal) return;

                if (!_config.ShowDeathNPC && _victim.Type == VictimType.NPC || _attacker.Type == AttackerType.NPC)
                    return;

                if (_victim.Type == VictimType.Player && _victim.Entity.ToPlayer().IsSleeping() && !_config.ShowDeathSleepers)
                    return;

                var _bodyPart = victim?.skeletonProperties?.FindBone(info.HitBone)?.name?.english ?? "";
                var _distance = info.ProjectileDistance;

                if (_config.Log && _victim.Type == VictimType.Player && _attacker.Type == AttackerType.Player)
                {
                    LogToFile("log", $"[{DateTime.Now.ToShortTimeString()}] {info.Initiator} убил {victim} ({_weapon} [{_bodyPart}] с дистанции {_distance})", this, true);
                }

                if (_config.ShowColsole && _attacker.Type == AttackerType.Player)
                {
                    Puts($"[{DateTime.Now.ToShortTimeString()}] {info.Initiator} убил {victim} ({_weapon} [{_bodyPart}] с дистанции {_distance})");
                }

                AddNote(new DeathMessage(_attacker, _victim, _weapon, _damageType, _bodyPart, _distance));
            }
            catch (NullReferenceException)
            {
            }
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
                DestroyUI(player);
        }

        #endregion

        #region Core

        private void AddNote(DeathMessage note)
        {
            _notes.Insert(0, note);
            RefreshUI(note);
            timer.Once(_config.Cooldown, () =>
            {
                _notes.Remove(note);
                foreach (var player in note.Players)
                    CuiHelper.DestroyUi(player, note.UINotes);
            });
        }

        #endregion

        #region UI

        private void RefreshUI(DeathMessage note)
        {
            foreach (var player in note.Players)
                InitilizeUI(player);
        }

        private void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "ui.deathmessages");
        }

        private void InitilizeUI(BasePlayer player)
        {
            var notes = _notes.Where(x => x.Players.Contains(player));

            if (notes.Count() == 0)
                return;

            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = _config.AnchorMin, AnchorMax = _config.AnchorMax }
            }, "Hud", "ui.deathmessages");

            double index = 0;
            foreach (var note in notes)
            {
                CuiHelper.DestroyUi(player, note.UINotes);
                var label = InitilizeLabel(container, note.UINotes, note.Message, $"{index - (20 + _config.Distance)}", $"{index}");
                index -= 20 + _config.Distance;
            }
            CuiHelper.AddUi(player, container);
        }

        private string InitilizeLabel(CuiElementContainer container, string Name, string text, string offsetMin, string Offsetmax)
        {
            container.Add(new CuiElement
            {
                Name = Name,
                Parent = "ui.deathmessages",
                FadeOut = 0.3f,
                Components =
                {
                    new CuiTextComponent { Align = UnityEngine.TextAnchor.MiddleRight, FontSize = _config.FontSize, Text = text, Font = "robotocondensed-regular.ttf", FadeIn = 0.3f},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"-500 {offsetMin}", OffsetMax = $"-5 {Offsetmax}" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1.0 -0.5" }
                }
            });
            return Name;
        }

        #endregion

        #region Helpers

        private static string FirstUpper(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            return string.Join(" ", str.Split(' ').Select(x => x.Substring(0, 1).ToUpper() + x.Substring(1, x.Length - 1)).ToArray());
        }

        private static string FormatName(string prefab)
        {
            if (string.IsNullOrEmpty(prefab))
                return string.Empty;
            var reply = 1;
            if (reply == 0) { }
            var formatedPrefab = FirstUpper(prefab.Split('/').Last().Replace(".prefab", "").Replace(".entity", "").Replace(".weapon", "").Replace(".deployed", "").Replace("_", "."));
            switch (formatedPrefab)
            {
                case "Autoturret.deployed": return "Auto Turret";
                case "Flameturret": return "Flame Turret";
                case "Guntrap.deployed": return "Guntrap";
                case "Beartrap": return "Snap Trap";
                case "Landmine": return "Land Mine";
                case "Spikes.floor": return "Wooden Floor Spikes";
                case "Barricade.wood": return "Wooden Barricade";
                case "Barricade.woodwire": return "Barbed Wooden Barricade";
                case "Barricade.metal": return "Metal Barricade";
                case "Wall.external.high.wood": return "High External Wooden Wall";
                case "Wall.external.high.stone": return "High External Stone Wall";
                case "Gates.external.high.stone": return "High External Wooden Gate";
                case "Gates.external.high.wood": return "High External Stone Gate";
                case "Stone.hatchet": return "Stone Hatchet";
                case "Stone.pickaxe": return "Stone Pickaxe";
                case "Survey.charge": return "Survey Charge";
                case "Explosive.satchel": return "Satchel Charge";
                case "Explosive.timed": return "Timed Explosive Charge";
                case "Grenade.beancan": return "Beancan Grenade";
                case "Grenade.f1": return "F1 Grenade";
                case "Hammer.salvaged": return "Salvaged Hammer";
                case "Axe.salvaged": return "Salvaged Axe";
                case "Icepick.salvaged": return "Salvaged Icepick";
                case "Spear.stone": return "Stone Spear";
                case "Spear.wooden": return "Wooden Spear";
                case "Knife.bone": return "Bone Knife";
                case "Rocket.basic": return "Rocket";
                case "Flamethrower": return "Flamethrower";
                case "Rocket.hv": return "RocketSpeed";
                case "Rocket.heli": return "RocketHeli";
                case "Rocket.bradley": return "RocketBradley";
                case "sentry.scientist.static": return "Static Turret";
                default: return formatedPrefab;
            }
        }

        private static string GetMessage(string name, Dictionary<string, string> source)
        {
            if (source.ContainsKey(name))
                return source[name];

            return name;
        }
        #endregion

        #region Permissions
        public static class PermissionService
        {
            public static Permission permission = Interface.GetMod().GetLibrary<Permission>();

            public static bool HasPermission(ulong playerid = 0, string permissionName = "")
            {
                return !string.IsNullOrEmpty(permissionName) && permission.UserHasPermission(playerid.ToString(), permissionName);
            }

            public static void RegisterPermissions(Plugin owner, List<string> permissions)
            {
                if (owner == null) throw new ArgumentNullException("owner");
                if (permissions == null) throw new ArgumentNullException("commands");

                foreach (var permissionName in permissions.Where(permissionName => !permission.PermissionExists(permissionName)))
                {
                    permission.RegisterPermission(permissionName, owner);
                }
            }
        }
        #endregion
    }
}