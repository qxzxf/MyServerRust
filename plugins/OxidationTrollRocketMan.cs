

using System;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("OxidationTrollRocketMan", "qxzxf", "1.0.2")]
	[Description("Project Oxidation :: Troll's kit :: The rocketman")]

	public class OxidationTrollRocketMan : RustPlugin
	{
		private static readonly string UsePermission
			= "oxidationtrollrocketman.use";


		private void Loaded()
			=> permission.RegisterPermission(UsePermission, this);

		private void Unload()
		{
			foreach (OxidationRocketManAI Behaviour in UnityEngine.Object.FindObjectsOfType<OxidationRocketManAI>())
				if (Behaviour != null) UnityEngine.Object.Destroy(Behaviour);
		}

		private object CanDismountEntity(BasePlayer Player, BaseMountable Entity)
		{
			if (Player == null || Entity == null) return null;

			OxidationRocketManAI Behaviour = Player.GetComponent<OxidationRocketManAI>();
			if (Behaviour == null) return null;
			return false;
		}

		private void OnPlayerCorpseSpawned(BasePlayer Player, LootableCorpse Corpse)
		{
			if (Player == null || Corpse == null) return;

			OxidationRocketManAI Behaviour = Player.GetComponent<OxidationRocketManAI>();
			if (Behaviour == null) return;

			for(int x = 0; x < Corpse.containers.Length; x++)
				while(Corpse.containers[x].itemList.Count > 0)
					Corpse.containers[x].itemList[0].DropAndTossUpwards(Corpse.transform.position, 2f);

			Corpse.SendNetworkUpdateImmediate();
		}

		//
		// --- COMMANDS --------------------------------------------------------
		//
		[ChatCommand("troll.rocketman")]
		private void OxidationTrollRocketManCommand(BasePlayer Player, string Command, string[] args)
		{
			if (Player == null || args.Length == 0) return;
			if (!Player.IsAdmin && !CanUseCommand(Player.UserIDString)) return;

			BasePlayer Victim = covalence.Players.FindPlayer(args[0])?.Object as BasePlayer;
			if (Victim == null) return;

			OxidationRocketManAI Behaviour = Victim.gameObject.AddComponent<OxidationRocketManAI>();
		}

		[ConsoleCommand("troll.rocketman")]
		private void OxidationTrollRocketManRcon(ConsoleSystem.Arg arg)
		{
			if (!arg.IsAdmin || ! arg.HasArgs(1)) return;

			BasePlayer Victim = arg.GetPlayer(0);
			if (Victim == null || !Victim.IsConnected)
			{
				SendReply(arg, $"Player not found or not connected to the server.");
				return;
			}

			OxidationRocketManAI Behaviour = Victim.gameObject.AddComponent<OxidationRocketManAI>();
			SendReply(arg, $"Command executed.");
		}

		//
		// --- HELPERS ---------------------------------------------------------
		//
		private bool CanUseCommand(string SteamID)
			=> permission.UserHasPermission(SteamID, UsePermission);

		//
		// --- BEHAVIOURS ------------------------------------------------------
		//
		internal class OxidationRocketManAI : MonoBehaviour
		{
			protected BasePlayer Victim;
			internal void Awake()
				=> Victim = GetComponent<BasePlayer>();

			internal void OnEnable()
				=> Victim.PauseFlyHackDetection(10f);

			internal void OnDisable()
				=> Victim.ResetAntiHack();

			internal void Start()
			{
				try
				{
					BaseEntity Rocket = GameManager.server.CreateEntity(
						"assets/prefabs/ammo/rocket/rocket_basic.prefab",
						Victim.transform.position + new Vector3(0, 2f, 0));
					if (Rocket == null) throw new Exception();

					ServerProjectile RocketP = Rocket.GetComponent<ServerProjectile>();
					if (RocketP == null) throw new Exception();

					RocketP.gravityModifier *= -1f;
					RocketP.InitializeVelocity(Vector3.forward * 4f);

					TimedExplosive RocketE = Rocket.GetComponent<TimedExplosive>();
					if (RocketE == null) throw new Exception();

					RocketE.timerAmountMax = 10f;
					RocketE.timerAmountMin = 8f;

					Rocket.enableSaving = false;
					Rocket.Spawn();

					BaseChair Seat = GameManager.server.CreateEntity(
						"assets/prefabs/misc/summer_dlc/beach_chair/beachchair.deployed.prefab",
						Rocket.transform.position) as BaseChair;
					if (Seat == null) throw new Exception();

					Seat.SetParent(Rocket);
					Seat.transform.localPosition = new Vector3(0.00f, -0.10f, 0.00f);
					Seat.transform.localRotation = Quaternion.Euler(0.00f, 180.00f, 0.00f);

					Seat.enableSaving = false;
					Seat.skinID = 13003;
					Seat.Spawn();

					Victim.inventory.containerBelt.capacity = 0;
					Victim.SendNetworkUpdate();
					Seat.MountPlayer(Victim);
				}
				catch
				{
					UnityEngine.Object.Destroy(this);
				}
			}

			internal void FixedUpdate()
			{
				if (!Victim.isMounted) UnityEngine.Object.Destroy(this);
			}

			internal void OnDestroy()
			{
				Victim.inventory.containerBelt.capacity = 6;
				Victim.DieInstantly();
			}
		}
	}
}