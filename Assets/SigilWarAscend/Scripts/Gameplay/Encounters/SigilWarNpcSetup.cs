using Fusion;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SigilWarAscend.Gameplay
{
	[DisallowMultipleComponent]
	public sealed class SigilWarNpcSetup : MonoBehaviour
	{
		public SigilWarActorType ActorType = SigilWarActorType.Enemy;
		public string VisualRootNameHint = "Body";
		public string HurtboxName = "Hurtbox";

		[ContextMenu("Create Minimal NPC Combat Setup")]
		public void CreateMinimalNpcCombatSetup()
		{
			NetworkObject networkObject = GetOrAddComponent<NetworkObject>(gameObject);
			NetworkTransform networkTransform = GetOrAddComponent<NetworkTransform>(gameObject);
			SigilWarHealth health = GetOrAddComponent<SigilWarHealth>(gameObject);
			SigilWarDamageableActor damageableActor = GetOrAddComponent<SigilWarDamageableActor>(gameObject);
			SigilWarMeleeNpc meleeNpc = GetOrAddComponent<SigilWarMeleeNpc>(gameObject);

			Animator animator = GetComponentInChildren<Animator>(true);
			GameObject visualRoot = FindVisualRoot(animator);
			Transform hurtboxTransform = FindOrCreateHurtbox();
			CapsuleCollider hurtbox = GetOrAddComponent<CapsuleCollider>(hurtboxTransform.gameObject);

			hurtbox.isTrigger = false;
			hurtbox.height = 2.0f;
			hurtbox.radius = 0.45f;
			hurtbox.center = new Vector3(0f, 1f, 0f);

			health.VisualRoot = visualRoot;
			health.DeathRoot = null;
			damageableActor.ActorType = ActorType;
			damageableActor.Health = health;
			damageableActor.Hurtbox = hurtbox;
			damageableActor.Animator = animator;

			meleeNpc.DamageableActor = damageableActor;
			meleeNpc.Health = health;
			meleeNpc.Animator = animator;

			if (ActorType == SigilWarActorType.Boss)
			{
				health.InitialHealth = Mathf.Max(health.InitialHealth, 300);
				meleeNpc.MoveSpeed = 2.0f;
				meleeNpc.AttackDamage = 25;
				meleeNpc.AttackRange = 3.0f;
				meleeNpc.ChaseRange = 18f;
			}
			else
			{
				health.InitialHealth = Mathf.Max(health.InitialHealth, 60);
				meleeNpc.MoveSpeed = 2.8f;
				meleeNpc.AttackDamage = 12;
				meleeNpc.AttackRange = 2.2f;
				meleeNpc.ChaseRange = 12f;
			}

#if UNITY_EDITOR
			EditorUtility.SetDirty(networkObject);
			EditorUtility.SetDirty(networkTransform);
			EditorUtility.SetDirty(health);
			EditorUtility.SetDirty(damageableActor);
			EditorUtility.SetDirty(meleeNpc);
			EditorUtility.SetDirty(this);
#endif
		}

		private GameObject FindVisualRoot(Animator animator)
		{
			if (animator != null)
				return animator.gameObject;

			Transform[] transforms = GetComponentsInChildren<Transform>(true);
			for (int i = 0; i < transforms.Length; i++)
			{
				if (transforms[i].name == VisualRootNameHint)
					return transforms[i].gameObject;
			}

			return gameObject;
		}

		private Transform FindOrCreateHurtbox()
		{
			Transform child = transform.Find(HurtboxName);
			if (child != null)
				return child;

			GameObject hurtboxObject = new GameObject(HurtboxName);
#if UNITY_EDITOR
			if (Application.isPlaying == false)
			{
				Undo.RegisterCreatedObjectUndo(hurtboxObject, $"Create {HurtboxName}");
			}
#endif
			child = hurtboxObject.transform;
			child.SetParent(transform, false);
			child.localPosition = Vector3.zero;
			child.localRotation = Quaternion.identity;
			child.localScale = Vector3.one;
			return child;
		}

		private static T GetOrAddComponent<T>(GameObject target) where T : Component
		{
			T component = target.GetComponent<T>();
			if (component != null)
				return component;

#if UNITY_EDITOR
			if (Application.isPlaying == false)
			{
				return Undo.AddComponent<T>(target);
			}
#endif

			return target.AddComponent<T>();
		}
	}
}
