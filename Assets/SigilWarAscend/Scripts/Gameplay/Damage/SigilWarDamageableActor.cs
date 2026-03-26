using Fusion;
using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	/// <summary>
	/// Shared damage receiver for non-player combatants.
	/// Enemy and Boss prefabs can both use this so they follow the same damage/death flow as players.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class SigilWarDamageableActor : NetworkBehaviour, ISigilDamageable
	{
		[Header("Setup")]
		public SigilWarActorType ActorType = SigilWarActorType.Enemy;
		public bool DespawnWhenDeathFinished = true;

		[Header("References")]
		public SigilWarHealth Health;
		public Collider Hurtbox;
		public Animator Animator;

		[Networked]
		private NetworkBool _deathHandled { get; set; }

		public bool IsAlive => Health != null && Health.IsAlive;
		public PlayerRef LastDamageDealer => Health != null ? Health.LastDamageDealer : PlayerRef.None;

		public override void Spawned()
		{
			if (Health == null)
			{
				Health = GetComponent<SigilWarHealth>();
			}
		}

		public override void FixedUpdateNetwork()
		{
			if (HasStateAuthority == false || Health == null)
				return;

			if (Health.IsAlive)
			{
				_deathHandled = false;
				return;
			}

			if (_deathHandled)
				return;

			if (Health.IsFinished == false)
				return;

			_deathHandled = true;

			if (DespawnWhenDeathFinished && Object != null && Runner != null)
			{
				Runner.Despawn(Object);
			}
		}

		public override void Render()
		{
			if (Hurtbox != null)
			{
				Hurtbox.enabled = IsAlive;
			}

			if (Animator != null && HasAnimatorParameter("Dead", AnimatorControllerParameterType.Bool))
			{
				Animator.SetBool("Dead", IsAlive == false);
			}
		}

		public void TakeDamage(int damage, PlayerRef damageDealer = default)
		{
			if (Health != null)
			{
				Health.TakeHit(damage, damageDealer);
			}
		}

		private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType expectedType)
		{
			if (Animator == null || string.IsNullOrEmpty(parameterName))
				return false;

			foreach (var parameter in Animator.parameters)
			{
				if (parameter.name == parameterName && parameter.type == expectedType)
					return true;
			}

			return false;
		}
	}
}
