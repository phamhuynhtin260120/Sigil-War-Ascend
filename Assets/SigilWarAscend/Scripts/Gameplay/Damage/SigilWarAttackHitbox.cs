using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	[DisallowMultipleComponent]
	public sealed class SigilWarAttackHitbox : MonoBehaviour
	{
		[Header("References")]
		public SigilWarPlayer Owner;
		public Collider Trigger;

		private readonly HashSet<Object> _damagedTargets = new HashSet<Object>();
		private bool _isActive;
		private int _damage;

		private void Reset()
		{
			Trigger = GetComponent<Collider>();
			if (Owner == null)
			{
				Owner = GetComponentInParent<SigilWarPlayer>();
			}
		}

		private void Awake()
		{
			if (Trigger == null)
			{
				Trigger = GetComponent<Collider>();
			}

			if (Owner == null)
			{
				Owner = GetComponentInParent<SigilWarPlayer>();
			}

			SetActive(false);
		}

		public void OpenWindow(SigilWarPlayer owner, int damage)
		{
			Owner = owner;
			_damage = Mathf.Max(0, damage);
			_damagedTargets.Clear();
			SetActive(true);
			Debug.Log($"[SigilWarAttackHitbox] Open | hitbox={name}, owner={Owner?.name}, ownerRef={Owner?.OwnerPlayerRef}, damage={_damage}, time={Time.time:F3}");
		}

		public void CloseWindow()
		{
			if (_isActive)
			{
				Debug.Log($"[SigilWarAttackHitbox] Close | hitbox={name}, owner={Owner?.name}, time={Time.time:F3}");
			}

			_damagedTargets.Clear();
			SetActive(false);
		}

		private void OnTriggerEnter(Collider other)
		{
			if (_isActive == false || _damage <= 0 || Owner == null || Owner.HasStateAuthority == false)
				return;

			if (other == null)
				return;

			SigilWarPlayer hitPlayer = other.GetComponentInParent<SigilWarPlayer>();
			if (hitPlayer != null && hitPlayer == Owner)
				return;

			MonoBehaviour[] behaviours = other.GetComponentsInParent<MonoBehaviour>(true);
			for (int i = 0; i < behaviours.Length; i++)
			{
				if (behaviours[i] is not ISigilDamageable damageable)
					continue;

				Object targetObject = behaviours[i];
				if (_damagedTargets.Contains(targetObject))
					return;

				_damagedTargets.Add(targetObject);
				Debug.Log($"[SigilWarAttackHitbox] Hit | hitbox={name}, owner={Owner?.name}, target={targetObject.name}, damage={_damage}, time={Time.time:F3}");
				damageable.TakeDamage(_damage, Owner.OwnerPlayerRef);
				return;
			}
		}

		private void SetActive(bool value)
		{
			_isActive = value;
			if (Trigger != null)
			{
				Trigger.enabled = value;
			}
		}
	}
}
