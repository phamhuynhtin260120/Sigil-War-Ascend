using Fusion;
using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	/// <summary>
	/// Sigil War specific health component based on the Fusion shooter sample.
	/// It keeps death state networked so the player controller can drive respawn and win logic.
	/// </summary>
	public sealed class SigilWarHealth : NetworkBehaviour
	{
		[Header("Setup")]
		public int InitialHealth = 3;
		public float DeathTime = 3f;

		[Header("References")]
		public Transform ScalingRoot;
		public GameObject VisualRoot;
		public GameObject DeathRoot;

		public bool IsAlive => NetworkHealth > 0;
		public bool IsFinished => NetworkHealth <= 0 && DeathCooldown.Expired(Runner);

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

			_lastVisibleHealth = NetworkHealth;
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

			if (_lastVisibleHealth > NetworkHealth && ScalingRoot != null)
			{
				ScalingRoot.localScale = new Vector3(0.85f, 1.15f, 0.85f);
			}

			_lastVisibleHealth = NetworkHealth;
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
