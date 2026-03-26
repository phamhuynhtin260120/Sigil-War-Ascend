using Fusion;
using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	/// <summary>
	/// Sigil War specific health component based on the Fusion shooter sample.
	/// It keeps networked HP state so the player controller can drive respawn and UI health bars.
	/// </summary>
	public sealed class SigilWarHealth : NetworkBehaviour
	{
		[Header("Setup")]
		public int InitialHealth = 100;
		public float DeathTime = 3f;

		[Header("References")]
		public GameObject VisualRoot;
		public GameObject DeathRoot;

		[Header("Presentation")]
		public bool HideVisualRootWhenDead;

		[Header("Debug")]
		public bool EnableDebugLogs;
		public bool LogDamageFlow = true;
		public bool LogLifecycleFlow = true;

		private const string LogPrefix = "[SigilWarHealth]";

		public int MaxHealth => InitialHealth;
		public int CurrentHealth => NetworkHealth;
		public float HealthNormalized => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;
		public bool IsAlive => CurrentHealth > 0;
		public bool IsFinished => CurrentHealth <= 0 && DeathCooldown.Expired(Runner);
		
		[Networked]
		public int NetworkHealth { get; private set; }
		[Networked]
		public TickTimer DeathCooldown { get; private set; }
		[Networked]
		public PlayerRef LastDamageDealer { get; private set; }

		public void TakeHit(int damage, PlayerRef damageDealer = default)
		{
			if (IsAlive == false)
				return;

			if (HasStateAuthority)
			{
				ApplyDamage(damage, damageDealer);
			}
			else
			{
				RPC_TakeHit(damage, damageDealer);
			}
		}

		public void Revive()
		{
			NetworkHealth = InitialHealth;
			DeathCooldown = default;
			LastDamageDealer = PlayerRef.None;
			LogLifecycle($"{gameObject.name} | Revive | HP={NetworkHealth}/{InitialHealth}");
		}

		public override void Spawned()
		{
			if (HasStateAuthority)
			{
				Revive();
			}
		}

		public override void Render()
		{
			if (VisualRoot != null)
			{
				VisualRoot.SetActive(IsAlive || HideVisualRootWhenDead == false);
			}

			if (DeathRoot != null)
			{
				DeathRoot.SetActive(IsAlive == false);
			}
		}

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		private void RPC_TakeHit(int damage, PlayerRef damageDealer = default)
		{
			ApplyDamage(damage, damageDealer);
		}

		private void ApplyDamage(int damage, PlayerRef damageDealer)
		{
			if (IsAlive == false)
				return;

			int healthBefore = NetworkHealth;
			NetworkHealth = Mathf.Max(0, NetworkHealth - damage);

			if (damageDealer != PlayerRef.None)
			{
				LastDamageDealer = damageDealer;
			}

			LogDamage(
				$"{gameObject.name} | Damage={damage} dealer={FormatPlayerRef(damageDealer)} | HP {healthBefore}→{NetworkHealth} | " +
				$"authority={HasStateAuthority}");

			if (NetworkHealth == 0)
			{
				DeathCooldown = TickTimer.CreateFromSeconds(Runner, DeathTime);
				LogLifecycle(
					$"{gameObject.name} | Death | DeathTime={DeathTime:F2}s | lastDealer={FormatPlayerRef(LastDamageDealer)}");
			}
		}

		private void LogDamage(string message)
		{
			if (EnableDebugLogs == false || LogDamageFlow == false)
				return;

			Debug.Log($"{LogPrefix}[Damage] {message}", this);
		}

		private void LogLifecycle(string message)
		{
			if (EnableDebugLogs == false || LogLifecycleFlow == false)
				return;

			Debug.Log($"{LogPrefix}[Lifecycle] {message}", this);
		}

		private static string FormatPlayerRef(PlayerRef playerRef)
		{
			return playerRef == PlayerRef.None ? "None" : $"Player{playerRef.PlayerId}";
		}
	}
}
