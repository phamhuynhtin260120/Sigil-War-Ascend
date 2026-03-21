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
		public Transform ScalingRoot;
		public GameObject VisualRoot;
		public GameObject DeathRoot;

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
		private int _lastVisibleHealth;

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
		}

		public override void Spawned()
		{
			if (HasStateAuthority)
			{
				Revive();
			}

			_lastVisibleHealth = CurrentHealth;
		}

		public override void Render()
		{
			if (VisualRoot != null)
			{
				VisualRoot.SetActive(IsAlive);
			}

			if (DeathRoot != null)
			{
				DeathRoot.SetActive(IsAlive == false);
			}

			if (_lastVisibleHealth > CurrentHealth && ScalingRoot != null)
			{
				ScalingRoot.localScale = new Vector3(0.85f, 1.15f, 0.85f);
			}

			_lastVisibleHealth = CurrentHealth;
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

			NetworkHealth = Mathf.Max(0, NetworkHealth - damage);

			if (damageDealer != PlayerRef.None)
			{
				LastDamageDealer = damageDealer;
			}

			if (NetworkHealth == 0)
			{
				DeathCooldown = TickTimer.CreateFromSeconds(Runner, DeathTime);
			}
		}
	}
}
