using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Mini-Copter Options", "qxzxf", "2.4.1")]
    [Description("Provide a number of additional options for Mini-Copters, including storage and seats.")]
    class MiniCopterOptions : CovalencePlugin
    {
        #region Prefab Modifications

        private readonly string minicopterPrefab = "assets/content/vehicles/minicopter/minicopter.entity.prefab";
        private readonly string storagePrefab = "assets/prefabs/deployable/hot air balloon/subents/hab_storage.prefab";
        private readonly string storageLargePrefab = "assets/content/vehicles/boats/rhib/subents/rhib_storage.prefab";
        private readonly string autoturretPrefab = "assets/prefabs/npc/autoturret/autoturret_deployed.prefab";
        private readonly string switchPrefab = "assets/prefabs/deployable/playerioents/simpleswitch/switch.prefab";
        private readonly string searchLightPrefab = "assets/prefabs/deployable/search light/searchlight.deployed.prefab";
        private readonly string batteryPrefab = "assets/prefabs/deployable/playerioents/batteries/smallrechargablebattery.deployed.prefab";
        private readonly string flasherBluePrefab = "assets/prefabs/deployable/playerioents/lights/flasherlight/electric.flasherlight.deployed.prefab";
        private readonly string lockPrefab = "assets/prefabs/locks/keypad/lock.code.prefab";
        private readonly string spherePrefab = "assets/prefabs/visualization/sphere.prefab";

        private const string resizableLootPanelName = "generic_resizable";
        private const int MinStorageCapacity = 6;
        private const int MaxStorageCapacity = 48;

        private readonly object False = false;

        private Configuration config;
        private MiniCopterDefaults copterDefaults;
        private bool lastRanAtNight;
        private int setupTimeHooksAttempts;
        private TOD_Sky time;
        private float sunrise;
        private float sunset;
        private float lastNightCheck;

        void Init() {
            if (config.storageContainers > 3) {
                PrintWarning($"Storage Containers configuration value {config.storageContainers} exceeds the maximum, setting to 3.");
                config.storageContainers = 3;
            } else if (config.storageContainers < 0) {
                PrintWarning($"Storage Containers cannot be a negative value, setting to 0.");
                config.storageContainers = 0;
            }

            if (config.storageLargeContainers > 2) {
                PrintWarning($"Large Storage Containers configuration value {config.storageLargeContainers} exceeds the maximum, setting to 2.");
                config.storageLargeContainers = 2;
            } else if (config.storageLargeContainers < 0) {
                PrintWarning($"Large Storage Containers cannot be a negative value, setting to 0.");
                config.storageLargeContainers = 0;
            }

            if (config.largeStorageSize > MaxStorageCapacity) {
                PrintWarning($"Large Storage Containers Capacity configuration value {config.largeStorageSize} exceeds the maximum, setting to {MaxStorageCapacity}.");
                config.largeStorageSize = MaxStorageCapacity;
            } else if (config.largeStorageSize < MinStorageCapacity) {
                PrintWarning($"Storage Containers Capacity cannot be a smaller than {MinStorageCapacity}, setting to {MinStorageCapacity}.");
            }

            Unsubscribe(nameof(OnEntitySpawned));

            if (!config.autoturret) {
                Unsubscribe(nameof(OnTurretTarget));
            }
        }

        bool IsNight() {
            if (time == null)
                return false;

            float hour = time.Cycle.Hour;
            return hour > sunset || hour < sunrise;
        }

        void OnHour() {
            float hour = time.Cycle.Hour;
            bool isNight = IsNight();

            //Puts($"OnHour: hour is now {hour}, and it is night {isNight}");
            if ((isNight == lastRanAtNight) || (lastNightCheck == hour))
                return;

            //Puts($"OnHour Called: Night:{isNight} LastRanAtNight:{lastRanAtNight}");
            lastNightCheck = hour;

            var minis = BaseNetworkable.serverEntities.OfType<Minicopter>().ToArray();
            //Puts($"OnHour Called: Minis to modify {minis.Count}");\
            foreach (var mini in minis) {
                var tailLight = mini.GetComponentInChildren<FlasherLight>();
                if (tailLight != null) {
                    tailLight.SetFlag(IOEntity.Flag_HasPower, isNight);
                }
            }

            lastRanAtNight ^= true;
        }

        void SetupTimeHooks() {
            time = TOD_Sky.Instance;

            if (time == null) {
                if (setupTimeHooksAttempts++ >= 10) {
                    PrintError("Unable to detect time system. Tail light will not follow time of day.");;
                    return;
                }

                timer.Once(1, SetupTimeHooks);
                return;
            }

            sunrise = time.SunriseTime;
            sunset = time.SunsetTime;

            time.Components.Time.OnHour += OnHour;
        }

        StorageContainer[] GetStorage(Minicopter copter) => copter.GetComponentsInChildren<StorageContainer>()
            .Where(x => x.name == storagePrefab || x.name == storageLargePrefab)
            .ToArray();

        void AddLargeStorageBox(Minicopter copter) {
            //sides,negative left | up and down | in and out

            if (config.storageLargeContainers == 1) {
                AddStorageBox(copter, storageLargePrefab, new Vector3(0.0f, 0.07f, -1.05f), Quaternion.Euler(0, 180f, 0));
            } else if (config.storageLargeContainers >= 2) {
                AddStorageBox(copter, storageLargePrefab, new Vector3(-0.48f, 0.07f, -1.05f), Quaternion.Euler(0, 180f, 0));
                AddStorageBox(copter, storageLargePrefab, new Vector3(0.48f, 0.07f, -1.05f), Quaternion.Euler(0, 180f, 0));
            }

        }

        void AddRearStorageBox(Minicopter copter) {
            AddStorageBox(copter, storagePrefab, new Vector3(0, 0.75f, -1f));
        }

        void AddSideStorageBoxes(Minicopter copter) {
            AddStorageBox(copter, storagePrefab, new Vector3(0.6f, 0.24f, -0.35f));
            if (!IsBatteryEnabled()) {
                AddStorageBox(copter, storagePrefab, new Vector3(-0.6f, 0.24f, -0.35f));
            }
        }

        void AddStorageBox(Minicopter copter, string prefab, Vector3 position) {
            AddStorageBox(copter, prefab, position, Quaternion.identity);
        }

        void SetupStorage(StorageContainer box) {
            if (box.PrefabName.Equals(storageLargePrefab)) {
                box.isLockable = config.largeStorageLockable;
                box.inventory.capacity = config.largeStorageSize;
                box.panelName = resizableLootPanelName;
            }
        }

        void AddStorageBox(Minicopter copter, string prefab, Vector3 position, Quaternion rotation) {
            StorageContainer box = GameManager.server.CreateEntity(prefab, position, rotation) as StorageContainer;

            box.Spawn();
            box.SetParent(copter);

            SetupStorage(box);
            box.SendNetworkUpdateImmediate();
        }

        void SetupInvincibility(BaseCombatEntity entity) {
            entity._maxHealth = 99999999f;
            entity._health = 99999999f;
            entity.SendNetworkUpdate();
        }

        void SetupTailLight(FlasherLight tailLight) {
            tailLight.pickup.enabled = false;
            DestroyGroundComp(tailLight);
            tailLight.SetFlag(IOEntity.Flag_HasPower, IsNight());
        }

        void AddTailLight(Minicopter copter) {
            FlasherLight tailLight = GameManager.server.CreateEntity(flasherBluePrefab, new Vector3(0, 1.2f, -2.0f), Quaternion.Euler(33, 180, 0)) as FlasherLight;
            SetupTailLight(tailLight);
            tailLight.SetParent(copter);
            tailLight.Spawn();
            SetupInvincibility(tailLight);
        }

        void SetupSphereEntity(SphereEntity sphereEntity) {
            sphereEntity.EnableSaving(true);
            sphereEntity.EnableGlobalBroadcast(false);
        }

        void SetupSearchLight(SearchLight searchLight) {
            searchLight.pickup.enabled = false;
            DestroyMeshCollider(searchLight);
            DestroyGroundComp(searchLight);
        }

        void AddSearchLight(Minicopter copter) {
            SphereEntity sphereEntity = GameManager.server.CreateEntity(spherePrefab, new Vector3(0, -100, 0), Quaternion.identity) as SphereEntity;
            SetupSphereEntity(sphereEntity);
            sphereEntity.SetParent(copter);
            sphereEntity.Spawn();

            SearchLight searchLight = GameManager.server.CreateEntity(searchLightPrefab, sphereEntity.transform.position) as SearchLight;
            SetupSearchLight(searchLight);
            searchLight.Spawn();
            SetupInvincibility(searchLight);
            searchLight.SetFlag(BaseEntity.Flags.Reserved5, true);
            searchLight.SetFlag(BaseEntity.Flags.Busy, true);
            searchLight.SetParent(sphereEntity);
            searchLight.transform.localPosition = Vector3.zero;
            searchLight.transform.localRotation = Quaternion.Euler(-20, 180, 180);

            sphereEntity.currentRadius = 0.1f;
            sphereEntity.lerpRadius = 0.1f;
            sphereEntity.UpdateScale();
            sphereEntity.SendNetworkUpdateImmediate();

            timer.Once(3f, () => {
                if (sphereEntity != null)
                    sphereEntity.transform.localPosition = new Vector3(0, 0.24f, 1.8f);
            });
        }

        void AddLock(BaseEntity entity) {
            CodeLock codeLock = GameManager.server.CreateEntity(lockPrefab) as CodeLock;

            codeLock.Spawn();
            codeLock.code = "789456789123";
            codeLock.SetParent(entity, entity.GetSlotAnchorName(BaseEntity.Slot.Lock));
            codeLock.transform.localScale += new Vector3(-50, -50, -50);
            entity.SetSlot(BaseEntity.Slot.Lock, codeLock);
            codeLock.SetFlag(BaseEntity.Flags.Locked, true);
        }

        void SetupAutoTurret(AutoTurret turret) {
            turret.pickup.enabled = false;
            turret.sightRange = config.turretRange;
            DestroyMeshCollider(turret);
            DestroyGroundComp(turret);
        }

        void AddTurret(Minicopter copter) {
            AutoTurret turret = GameManager.server.CreateEntity(autoturretPrefab, new Vector3(0, 0, 2.47f)) as AutoTurret;
            SetupAutoTurret(turret);
            turret.SetParent(copter);
            turret.Spawn();

            BasePlayer player = BasePlayer.FindByID(copter.OwnerID);
            if (player != null) {
                turret.authorizedPlayers.Add(new ProtoBuf.PlayerNameID() {
                    userid = player.userID,
                    username = player.displayName,
                });
                turret.SendNetworkUpdate();
            }

            AddSwitch(turret);
        }

        bool IsBatteryEnabled() => config.autoturretBattery && config.autoturret;

        void SetupBattery(ElectricBattery battery) {
            battery.maxOutput = 12;
            battery.pickup.enabled = false;
            DestroyGroundComp(battery);
            DestroyColliders(battery);
        }

        ElectricBattery AddBattery(Minicopter copter) {
            var batteryPosition = copter.transform.TransformPoint(new Vector3(-0.7f, 0.2f, -0.2f));
            var batteryRotation = copter.transform.rotation;

            ElectricBattery battery = GameManager.server.CreateEntity(batteryPrefab, batteryPosition, batteryRotation) as ElectricBattery;
            SetupBattery(battery);
            battery.Spawn();
            battery.SetParent(copter, worldPositionStays: true);
            SetupInvincibility(battery);

            return battery;
        }

        void SetupSwitch(ElectricSwitch electricSwitch) {
            electricSwitch.pickup.enabled = false;
            DestroyMeshCollider(electricSwitch);
            DestroyGroundComp(electricSwitch);
        }

        void AddSwitch(AutoTurret turret) {
            ElectricBattery battery = null;
            if (IsBatteryEnabled()) {
                battery = AddBattery(turret.GetParentEntity() as Minicopter);
            }

            var switchPosition = turret.transform.TransformPoint(new Vector3(0f, -0.65f, 0.325f));
            var switchRotation = turret.transform.rotation;

            ElectricSwitch electricSwitch = GameManager.server.CreateEntity(switchPrefab, switchPosition, switchRotation) as ElectricSwitch;
            SetupSwitch(electricSwitch);
            electricSwitch.Spawn();
            SetupInvincibility(electricSwitch);

            // Spawning the switch at the desired world position and then parenting it, allows it to render correctly initially.
            electricSwitch.SetParent(turret, worldPositionStays: true);

            if (!IsBatteryEnabled()) {
                RunWire(electricSwitch, 0, turret, 0, 12);
            } else if (battery != null) {
                RunWire(battery, 0, electricSwitch, 0);
                RunWire(electricSwitch, 0, turret, 0);
            }
        }

        // https://umod.org/community/rust/12554-trouble-spawning-a-switch?page=1#post-5
        private void RunWire(IOEntity source, int s_slot, IOEntity destination, int d_slot, int power = 0) {
            destination.inputs[d_slot].connectedTo.Set(source);
            destination.inputs[d_slot].connectedToSlot = s_slot;
            destination.inputs[d_slot].connectedTo.Init();
            source.outputs[s_slot].connectedTo.Set(destination);
            source.outputs[s_slot].connectedToSlot = d_slot;
            source.outputs[s_slot].connectedTo.Init();
            source.MarkDirtyForceUpdateOutputs();
            if (power > 0) {
                destination.UpdateHasPower(power, 0);
                source.UpdateHasPower(power, 0);
            }
            source.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            destination.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
        }

        void DestroyGroundComp(BaseEntity ent) {
            UnityEngine.Object.DestroyImmediate(ent.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.DestroyImmediate(ent.GetComponent<GroundWatch>());
        }

        void DestroyMeshCollider(BaseEntity ent) {
            foreach (var mesh in ent.GetComponentsInChildren<MeshCollider>()) {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        void DestroyColliders(BaseEntity ent) {
            foreach (var collider in ent.GetComponentsInChildren<Collider>()) {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        IOEntity GetBatteryConnected(Minicopter ent) {
            return ent.GetComponentInChildren<ElectricBattery>()?.inputs[0]?.connectedTo.ioEnt;
        }

        void RestoreMiniCopter(Minicopter copter, bool removeStorage = false) {
            if (copterDefaults != null) {
                // Allow setting fuelPerSec to negative to make the plugin do nothing.
                // Don't update fuelPerSec if not currently the configured value, because another plugin probably updated it.
                if (config.fuelPerSec >= 0 && copter.fuelPerSec == config.fuelPerSec) {
                    copter.fuelPerSec = copterDefaults.fuelPerSecond;
                }
                copter.liftFraction = copterDefaults.liftFraction;
                copter.torqueScale = copterDefaults.torqueScale;
            }

            if (removeStorage) {
                foreach (var child in copter.children.FindAll(child => child.name == storagePrefab || child.name == storageLargePrefab || child.name == autoturretPrefab))
                    child.Kill();
            }
        }

        void ModifyMiniCopter(Minicopter copter) {

            // Allow setting fuelPerSec to negative to make the plugin do nothing.
            // Don't update fuelPerSec if not currently set to vanilla default, because another plugin probably updated it.
            if (config.fuelPerSec >= 0 && copterDefaults != null && copter.fuelPerSec == copterDefaults.fuelPerSecond) {
                copter.fuelPerSec = config.fuelPerSec;
            }
            copter.liftFraction = config.liftFraction;
            copter.torqueScale = new Vector3(config.torqueScalePitch, config.torqueScaleYaw, config.torqueScaleRoll);

            // Override the inertia tensor since if the server had rebooted while there were attachments, it would have been snapshotted to an unreasonable value.
            // This is the vanila amount while the prevent building object is inactive.
            // To determine this value, simply deactivate the prevent building object, call rigidBody.ResetInertiaTensor(), then print the value.
            copter.rigidBody.inertiaTensor = new Vector3(407.1f, 279.6f, 173.2f);

            if (config.autoturret) {
                // Setup existing turret, or add a new one.
                var turret = copter.GetComponentInChildren<AutoTurret>();
                if (turret != null) {
                    SetupAutoTurret(turret);

                    // Setup existing switch, but don't add a new one since that may add a duplicate battery.
                    var turretSwitch = turret.GetComponentInChildren<ElectricSwitch>();
                    if (turretSwitch != null) {
                        SetupSwitch(turretSwitch);
                    }
                } else {
                    AddTurret(copter);
                }
            }

            var existingStorage = GetStorage(copter);
            if (existingStorage.Length > 0) {
                // Existing storage found, update its state and don't add any more storage.
                foreach (var storage in existingStorage) {
                    SetupStorage(storage);
                }
            } else {
                // Add storage since none was found.
                AddLargeStorageBox(copter);

                switch (config.storageContainers) {
                    case 1:
                        AddRearStorageBox(copter);
                        break;
                    case 2:
                        AddSideStorageBoxes(copter);
                        break;
                    case 3:
                        AddRearStorageBox(copter);
                        AddSideStorageBoxes(copter);
                        break;
                }
            }

            if (IsBatteryEnabled()) {
                // Setup battery if present, but don't add a new one since that's handled when adding a switch.
                var battery = copter.GetComponentInChildren<ElectricBattery>();
                if (battery != null) {
                    SetupBattery(battery);
                    SetupInvincibility(battery);
                }
            }

            if (config.addSearchLight) {
                // Setup existing search light, or add a new one.
                var searchLight = copter.GetComponentInChildren<SearchLight>();
                if (searchLight != null) {
                    SetupSearchLight(searchLight);
                    SetupInvincibility(searchLight);

                    var sphereEntity = searchLight.GetParentEntity() as SphereEntity;
                    if (sphereEntity != null)
                        SetupSphereEntity(sphereEntity);
                } else {
                    AddSearchLight(copter);
                }
            }

            if (config.lightTail) {
                // Setup existing tail light, or add a new one.
                var tailLight = copter.GetComponentInChildren<FlasherLight>();
                if (tailLight != null) {
                    SetupTailLight(tailLight);
                    SetupInvincibility(tailLight);
                } else {
                    AddTailLight(copter);
                }
            }
        }

        bool CanModifyMiniCopter(Minicopter copter) {
            var hookResult = Interface.CallHook("OnMiniCopterOptions", copter);
            if (hookResult is bool && !(bool)hookResult)
                return false;

            return true;
        }

        void ScheduleModifyMiniCopter(Minicopter copter) {
            // Delay to allow plugins to detect the Mini and save its ID so they can block modification via hooks.
            NextTick(() =>
            {
                if (copter == null || copter.IsDestroyed)
                    return;

                if (!CanModifyMiniCopter(copter))
                    return;

                ModifyMiniCopter(copter);
            });
        }

        void StoreMiniCopterDefaults() {
            var copter = GameManager.server.FindPrefab(minicopterPrefab)?.GetComponent<Minicopter>();
            if (copter == null)
                return;

            //Puts($"Defaults for copters saved as \nfuelPerSecond = {copter.fuelPerSec}\nliftFraction = {copter.liftFraction}\ntorqueScale = {copter.torqueScale}");
            copterDefaults = new MiniCopterDefaults {
                fuelPerSecond = copter.fuelPerSec,
                liftFraction = copter.liftFraction,
                torqueScale = copter.torqueScale
            };
        }

        #endregion

        #region Hooks

        void OnServerInitialized(bool init) {
            StoreMiniCopterDefaults();

            if (config.lightTail) {
                SetupTimeHooks();
            }

            if (!config.addSearchLight)
                Unsubscribe(nameof(OnServerCommand));

            foreach (var copter in BaseNetworkable.serverEntities.OfType<Minicopter>()) {

                if (init) {
                    // Destroy problematic components immediately on server boot, since OnEntitySpawned will run changes on a delay.
                    foreach (var meshCollider in copter.GetComponentsInChildren<MeshCollider>()) {
                        UnityEngine.Object.DestroyImmediate(meshCollider);
                    }

                    foreach (var groundWatch in copter.GetComponentsInChildren<GroundWatch>()) {
                        UnityEngine.Object.DestroyImmediate(groundWatch);
                    }
                }

                OnEntitySpawned(copter);
            }

            Subscribe(nameof(OnEntitySpawned));
        }

        void Unload() {
            if (config.lightTail && time != null) {
                time.Components.Time.OnHour -= OnHour;
            }

            // If the plugin is unloaded before OnServerInitialized() ran, don't revert minicopters to 0 values.
            if (copterDefaults != default(MiniCopterDefaults)) {
                foreach (var copter in BaseNetworkable.serverEntities.OfType<Minicopter>()) {
                    if (config.restoreDefaults && CanModifyMiniCopter(copter)) {
                        RestoreMiniCopter(copter, config.reloadStorage);
                    }
                }
            }
        }

        void OnEntitySpawned(Minicopter copter) {
            // Only add storage on spawn so we don't stack or mess with
            // existing player storage containers.
            ScheduleModifyMiniCopter(copter);
        }

        void OnEntityKill(BaseNetworkable entity) {
            if (!config.dropStorage || !(entity is Minicopter))
                return;

            StorageContainer[] containers = entity.GetComponentsInChildren<StorageContainer>();
            foreach (StorageContainer container in containers) {
                container.DropItems();
            }

            AutoTurret[] turrets = entity.GetComponentsInChildren<AutoTurret>();
            foreach (AutoTurret turret in turrets) {
                turret.DropItems();
            }
        }

        void OnSwitchToggled(ElectricSwitch electricSwitch, BasePlayer player) {
            if (IsBatteryEnabled())
            {
                // Do nothing since the switch is supposed to be wired into the turret,
                // so the game should handle this automatically.
                return;
            }

            AutoTurret turret = electricSwitch.GetParentEntity() as AutoTurret;
            if (turret == null)
                return;

            var mini = turret.GetParentEntity() as Minicopter;
            if (mini == null)
            {
                // Ignore if the turret isn't on a mini, to avoid plugin conflicts.
                return;
            }

            if (electricSwitch.IsOn()) {
                turret.SetFlag(IOEntity.Flag_HasPower, true);
                turret.InitiateStartup();
            } else {
                turret.SetFlag(IOEntity.Flag_HasPower, false);
                turret.InitiateShutdown();
            }
        }

        object OnTurretTarget(AutoTurret turret, BaseCombatEntity target) {
            if (target == null)
                return null;

            var mini = turret.GetParentEntity() as Minicopter;
            if ((object)mini == null)
                return null;

            if (!config.autoTurretTargetsAnimals && target is BaseAnimalNPC)
                return False;

            var basePlayer = target as BasePlayer;
            if ((object)basePlayer != null) {
                if (!config.autoTurretTargetsNPCs && basePlayer.IsNpc)
                    return False;

                if (!config.autoTurretTargetsPlayers && basePlayer.userID.IsSteamId())
                    return False;

                if (basePlayer.InSafeZone() && (basePlayer.IsNpc || !basePlayer.IsHostile()))
                    return False;
            }

            return null;
        }

        object OnServerCommand(ConsoleSystem.Arg arg) {
            if (arg.Connection == null || arg.cmd.FullName != "inventory.lighttoggle")
                return null;

            var player = arg.Player();
            if (player == null)
                return null;

            var mini = player.GetMountedVehicle() as Minicopter;
            if (mini == null)
                return null;

            if (!mini.IsDriver(player))
                return null;

            foreach (var child in mini.children) {
                var sphere = child as SphereEntity;
                if ((object)sphere == null)
                    continue;

                foreach (var grandChild in sphere.children) {
                    var light = grandChild as SearchLight;
                    if ((object)light == null)
                        continue;

                    light.SetFlag(IOEntity.Flag_HasPower, !light.IsPowered());

                    // Prevent other lights from toggling.
                    return False;
                }
            }

            return null;
        }

        void OnEntityDismounted(BaseNetworkable entity, BasePlayer player) {
            if (config.flyHackPause > 0 && entity.GetParentEntity() is Minicopter)
                player.PauseFlyHackDetection(config.flyHackPause);
        }

        object CanMountEntity(BasePlayer player, BaseMountable entity) {
            if (!(entity is Minicopter) && !(entity.GetParentEntity() is Minicopter))
                return null;

            if (!IsBatteryEnabled())
                return null;

            Minicopter mini = entity.GetParentEntity() as Minicopter;
            if (mini != null) {
                IOEntity battery = GetBatteryConnected(mini);
                if (battery != null) {
                    player.ChatMessage(string.Format(GetMsg("Err - Diconnect Battery", player.UserIDString), battery.GetDisplayName()));
                    return False;
                }
            }
            return null;
        }

        void OnItemDeployed(Deployer deployer, StorageContainer container, BaseLock baseLock) {
            if (container == null || baseLock == null)
                return;

            var parent = container.GetParentEntity();
            if (parent == null || !(parent is Minicopter))
                return;

            if (container.PrefabName != storageLargePrefab)
                return;

            baseLock.transform.localPosition = new Vector3(0.0f, 0.3f, 0.298f);
            baseLock.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));
            baseLock.SendNetworkUpdateImmediate();
        }

        #endregion

        #region Configuration

        class MiniCopterDefaults
        {
            public float fuelPerSecond;
            public float liftFraction;
            public Vector3 torqueScale;
        }

        private class Configuration : SerializableConfiguration {

            // Populated with Rust defaults.
            [JsonProperty("Fuel per Second")]
            public float fuelPerSec = 0.25f;

            [JsonProperty("Lift Fraction")]
            public float liftFraction = 0.25f;

            [JsonProperty("Pitch Torque Scale")]
            public float torqueScalePitch = 400f;

            [JsonProperty("Yaw Torque Scale")]
            public float torqueScaleYaw = 400f;

            [JsonProperty("Roll Torque Scale")]
            public float torqueScaleRoll = 200f;

            [JsonProperty("Storage Containers")]
            public int storageContainers = 0;

            [JsonProperty("Large Storage Containers")]
            public int storageLargeContainers = 0;

            [JsonProperty("Restore Defaults")]
            public bool restoreDefaults = true;

            [JsonProperty("Reload Storage")]
            public bool reloadStorage = false;

            [JsonProperty("Drop Storage Loot On Death")]
            public bool dropStorage = true;

            [JsonProperty("Large Storage Lockable")]
            public bool largeStorageLockable = true;

            [JsonProperty("Large Storage Size (Max 48)")]
            public int largeStorageSize = 48;

            [JsonProperty("Large Storage Size (Max 42)")]
            public int largeStorageSizeDeprecated {
                set { largeStorageSize = value; }
            }

            [JsonProperty("Seconds to pause flyhack when dismount from heli.")]
            public int flyHackPause = 1;

            [JsonProperty("Add auto turret to heli")]
            public bool autoturret = false;

            [JsonProperty("Auto turret uses battery")]
            public bool autoturretBattery = true;

            [JsonProperty("Auto turret targets players")]
            public bool autoTurretTargetsPlayers = true;

            [JsonProperty("Auto turret targets NPCs")]
            public bool autoTurretTargetsNPCs = true;

            [JsonProperty("Auto turret targets animals")]
            public bool autoTurretTargetsAnimals = true;

            [JsonProperty("Mini Turret Range (Default 30)")]
            public float turretRange = 30f;

            [JsonProperty("Light: Add Searchlight to heli")]
            public bool addSearchLight = true;

            [JsonProperty("Light: Add Nightitme Tail Light")]
            public bool lightTail = false;
        }

        private Configuration GetDefaultConfig() => new Configuration();

        #region Configuration Helpers

        private class SerializableConfiguration {
            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonHelper.Deserialize(ToJson()) as Dictionary<string, object>;
        }

        private static class JsonHelper {
            public static object Deserialize(string json) => ToObject(JToken.Parse(json));

            private static object ToObject(JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        return token.Children<JProperty>()
                                    .ToDictionary(prop => prop.Name,
                                                  prop => ToObject(prop.Value));

                    case JTokenType.Array:
                        return token.Select(ToObject).ToList();

                    default:
                        return ((JValue)token).Value;
                }
            }
        }

        private bool MaybeUpdateConfig(SerializableConfiguration config) {
            var currentWithDefaults = config.ToDictionary();
            var currentRaw = Config.ToDictionary(x => x.Key, x => x.Value);
            return MaybeUpdateConfigDict(currentWithDefaults, currentRaw);
        }

        private bool MaybeUpdateConfigDict(Dictionary<string, object> currentWithDefaults, Dictionary<string, object> currentRaw) {
            bool changed = false;

            foreach (var key in currentWithDefaults.Keys)
            {
                object currentRawValue;
                if (currentRaw.TryGetValue(key, out currentRawValue))
                {
                    var defaultDictValue = currentWithDefaults[key] as Dictionary<string, object>;
                    var currentDictValue = currentRawValue as Dictionary<string, object>;

                    if (defaultDictValue != null)
                    {
                        if (currentDictValue == null)
                        {
                            currentRaw[key] = currentWithDefaults[key];
                            changed = true;
                        }
                        else if (MaybeUpdateConfigDict(defaultDictValue, currentDictValue))
                            changed = true;
                    }
                }
                else
                {
                    currentRaw[key] = currentWithDefaults[key];
                    changed = true;
                }
            }

            return changed;
        }

        protected override void LoadDefaultConfig() => config = GetDefaultConfig();

        protected override void LoadConfig() {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new JsonException();
                }

                if (MaybeUpdateConfig(config))
                {
                    LogWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch (Exception e)
            {
                LogError(e.Message);
                LogWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() {
            Log($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }

        #endregion

        #endregion

        #region Helpers

        private string GetEnglishName(string shortName) { return ItemManager.FindItemDefinition(shortName)?.displayName?.english ?? shortName; }

        void PrintComponents(BaseEntity ent) {
            foreach (var sl in ent.GetComponents<Component>()) {
                Puts($"-P- {sl.GetType().Name} | {sl.name}");
                foreach (var s in sl.GetComponentsInChildren<Component>()) {
                    Puts($"-C- {s.GetType().Name} | {s.name}");
                }
            }
        }

        #endregion

        #region Languages

        protected override void LoadDefaultMessages() {
            lang.RegisterMessages(new Dictionary<string, string> {
                ["Err - Diconnect Battery"] = "First disconnect battery input from {0}",
                ["Err - Can only push minicopter"] = "You have to look at a minicopter. Pushing {0} not allowed"
            }, this);
        }

        string GetMsg(string key, string userIdString) => lang.GetMessage(key, this, userIdString);

        #endregion

    }
}
