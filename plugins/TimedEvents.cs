using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Timed Events", "qxzxf", "1.1.5")]
    [Description("Triggers various types of events like Airdrops, Helicopters and same")]
    public class TimedEvents : RustPlugin
    {
        #region Vars

        private const string prefabCH47 = "assets/prefabs/npc/ch47/ch47scientists.entity.prefab";
        private const string prefabPlane = "assets/prefabs/npc/cargo plane/cargo_plane.prefab";
        private const string prefabShip = "assets/content/vehicles/boats/cargoship/cargoshiptest.prefab";
        private const string prefabPatrol = "assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab";

        #endregion
        
        #region Oxide Hooks

        private void OnServerInitialized()
        {
            SpawnTank(true);
            SpawnShip(true);
            SpawnPatrol(true);
            SpawnPlane(true);
            SpawnCH47(true);
        }
        
        private object OnEventTrigger(TriggeredEventPrefab info)
        {
            var prefabs = UnityEngine.Object.FindObjectsOfType<TriggeredEventPrefab>();
            foreach (var obj in prefabs)
            {
                var name = obj.targetPrefab.resourcePath;
                if (name.Contains("patrol") && config.patrol.disableDefault == true)
                {
                    return true;
                }

                if (name.Contains("ship") && config.ship.disableDefault == true)
                {
                    return true;
                }

                if (name.Contains("plane") && config.plane.disableDefault == true)
                {
                    return true;
                }

                if (name.Contains("ch47") && config.ch47.disableDefault == true)
                {
                    return true;
                }
            }

            return null;
        }
        #endregion

        #region Core

        private void SpawnTank(bool skipSpawn = false)
        {
            if (Online() >= config.tank.playersMin && skipSpawn == false)
            {
                BradleySpawner.singleton?.SpawnBradley();
            }

            var time = Core.Random.Range(config.tank.timerMin, config.tank.timerMax);
            timer.Once(time, () => SpawnTank());
        }

        private void SpawnShip(bool skipSpawn = false)
        {
            if (Online() >= config.ship.playersMin && skipSpawn == false)
            {
                var amount = Core.Random.Range(config.ship.spawnMin, config.ship.spawnMax);

                for (var i = 0; i < amount; i++)
                {
                    var x = TerrainMeta.Size.x;
                    var vector3 = Vector3Ex.Range(-1f, 1f);
                    vector3.y = 0.0f;
                    vector3.Normalize();
                    var worldPos = vector3 * (x * 1f);
                    worldPos.y = TerrainMeta.WaterMap.GetHeight(worldPos);
                    var entity = GameManager.server.CreateEntity(prefabShip, worldPos);
                    entity?.Spawn();
                }
            }

            var time = Core.Random.Range(config.ship.timerMin, config.ship.timerMax);
            timer.Once(time, () => SpawnShip());
        }

        private void SpawnPatrol(bool skipSpawn = false)
        {
            if (Online() >= config.patrol.playersMin && skipSpawn == false)
            {
                var amount = Core.Random.Range(config.patrol.spawnMin, config.patrol.spawnMax);
            
                for (var i = 0; i < amount; i++)
                {
                    var position = new Vector3(ConVar.Server.worldsize, 100, ConVar.Server.worldsize) - new Vector3(50f, 0f, 50f);
                    var entity = GameManager.server.CreateEntity(prefabPatrol, position);
                    entity?.Spawn();
                }
            }
            
            var time = Core.Random.Range(config.patrol.timerMin, config.patrol.timerMax);
            timer.Once(time, () => SpawnPatrol());
        }

        private void SpawnPlane(bool skipSpawn = false)
        {
            if (Online() >= config.plane.playersMin && skipSpawn == false)
            {
                var amount = Core.Random.Range(config.plane.spawnMin, config.plane.spawnMax);
            
                for (var i = 0; i < amount; i++)
                {
                    var position = new Vector3(ConVar.Server.worldsize, 100, ConVar.Server.worldsize) - new Vector3(50f, 0f, 50f);
                    var entity = GameManager.server.CreateEntity(prefabPlane, position);
                    entity?.Spawn();
                }
            }
            
            var time = Core.Random.Range(config.plane.timerMin, config.plane.timerMax);
            timer.Once(time, () => SpawnPlane());
        }
        
        private void SpawnCH47(bool skipSpawn = false)
        {
            if (Online() >= config.ch47.playersMin && skipSpawn == false)
            {
                var amount = Core.Random.Range(config.ch47.spawnMin, config.ch47.spawnMax);
            
                for (var i = 0; i < amount; i++)
                {
                    var position = new Vector3(ConVar.Server.worldsize, 100, ConVar.Server.worldsize) - new Vector3(50f, 0f, 50f);
                    var entity = GameManager.server.CreateEntity(prefabCH47, position) as CH47HelicopterAIController;
                    entity?.TriggeredEventSpawn();
                    entity?.Spawn();
                }
            }
            
            var time = Core.Random.Range(config.ch47.timerMin, config.ch47.timerMax);
            timer.Once(time, () => SpawnCH47());
        }

        private int Online()
        {
            return BasePlayer.activePlayerList.Count;
        }

        #endregion

        #region Configuration

        private static ConfigData config;
        
        private class ConfigData
        {    
            [JsonProperty(PropertyName = "1. Cargo plane settings:")]
            public EventSettings plane;
            
            [JsonProperty(PropertyName = "2. Patrol Helicopter settings:")]
            public EventSettings patrol;
            
            [JsonProperty(PropertyName = "3. Bradley APC settings:")]
            public EventSettings tank;
            
            [JsonProperty(PropertyName = "4. CH47 settings:")]
            public EventSettings ch47;
            
            [JsonProperty(PropertyName = "5. Cargo ship settings:")]
            public EventSettings ship;
        }
        
        private class EventSettings
        {
            [JsonProperty(PropertyName = "1. Disable default spawns")]
            public bool disableDefault;
                
            [JsonProperty(PropertyName = "2. Minimal respawn time (in seconds)")]
            public int timerMin;
                
            [JsonProperty(PropertyName = "3. Maximal respawn time (in seconds)")]
            public int timerMax;
                
            [JsonProperty(PropertyName = "4. Minimal amount that spawned by once")]
            public int spawnMin;
                
            [JsonProperty(PropertyName = "5. Maximal amount that spawned by once")]
            public int spawnMax;
                
            [JsonProperty(PropertyName = "6. Minimal players to start event")]
            public int playersMin;
        }
        
        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                plane = new EventSettings
                {
                    disableDefault = false,
                    playersMin = 0,
                    timerMax = 7200,
                    timerMin = 3600,
                    spawnMax = 1,
                    spawnMin = 1
                },
                patrol = new EventSettings
                {
                    disableDefault = false,
                    playersMin = 0,
                    timerMax = 7200,
                    timerMin = 3600,
                    spawnMax = 1,
                    spawnMin = 1
                },
                tank = new EventSettings
                {
                    disableDefault = false,
                    playersMin = 0,
                    timerMax = 7200,
                    timerMin = 3600,
                    spawnMax = 1,
                    spawnMin = 1
                },
                ch47 = new EventSettings
                {
                    disableDefault = false,
                    playersMin = 0,
                    timerMax = 7200,
                    timerMin = 3600,
                    spawnMax = 1,
                    spawnMin = 1
                },
                ship = new EventSettings
                {
                    disableDefault = false,
                    playersMin = 0,
                    timerMax = 7200,
                    timerMin = 3600,
                    spawnMax = 1,
                    spawnMin = 1
                }
            };
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
   
            try
            {
                config = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintError("Configuration file is corrupt(or not exists), creating new one!");
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion
    }
}