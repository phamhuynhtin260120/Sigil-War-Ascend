using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SigilWarAscend.Gameplay
{
	[DisallowMultipleComponent]
	public sealed class SigilWarAttackHitboxSetup : MonoBehaviour
	{
		[Header("References")]
		public SigilWarPlayer Player;
		public SigilWarPlayerCombat Combat;
		public Transform PreferredParent;
		public string PreferredParentName = "SwordPosition";

		[Header("Template")]
		public Vector3 BaseLocalEulerAngles;
		public Vector3[] LocalPositions =
		{
			new Vector3(0f, 0f, 0.25f),
			new Vector3(0f, 0f, 0.55f),
			new Vector3(0f, 0f, 0.85f),
		};
		public Vector3[] BoxSizes =
		{
			new Vector3(0.20f, 0.20f, 0.45f),
			new Vector3(0.22f, 0.22f, 0.50f),
			new Vector3(0.24f, 0.24f, 0.55f),
		};

		private void Reset()
		{
			Player = GetComponent<SigilWarPlayer>();
			Combat = GetComponent<SigilWarPlayerCombat>();
			if (PreferredParent == null)
			{
				PreferredParent = FindPreferredParent();
			}
		}

		[ContextMenu("Create Sample Attack Hitboxes")]
		public void CreateSampleAttackHitboxes()
		{
			if (Player == null)
			{
				Player = GetComponent<SigilWarPlayer>();
			}

			if (Combat == null)
			{
				Combat = GetComponent<SigilWarPlayerCombat>();
			}

			Transform parent = PreferredParent != null ? PreferredParent : FindPreferredParent();
			if (parent == null)
			{
				parent = transform;
			}

			Transform root = FindOrCreateChild(parent, "AttackHitboxes");
			SigilWarAttackHitbox[] createdHitboxes = new SigilWarAttackHitbox[3];

			for (int i = 0; i < createdHitboxes.Length; i++)
			{
				string objectName = $"AttackHitbox_{i + 1}";
				Transform child = FindOrCreateChild(root, objectName);
				child.localRotation = Quaternion.Euler(BaseLocalEulerAngles);
				child.localPosition = i < LocalPositions.Length ? LocalPositions[i] : Vector3.zero;
				child.localScale = Vector3.one;

				BoxCollider collider = child.GetComponent<BoxCollider>();
				if (collider == null)
				{
					collider = child.gameObject.AddComponent<BoxCollider>();
				}

				collider.isTrigger = true;
				collider.enabled = false;
				collider.size = i < BoxSizes.Length ? BoxSizes[i] : new Vector3(0.2f, 0.2f, 0.5f);
				collider.center = Vector3.zero;

				SigilWarAttackHitbox hitbox = child.GetComponent<SigilWarAttackHitbox>();
				if (hitbox == null)
				{
					hitbox = child.gameObject.AddComponent<SigilWarAttackHitbox>();
				}

				hitbox.Owner = Player;
				hitbox.Trigger = collider;
				createdHitboxes[i] = hitbox;
			}

			AssignToCombat(createdHitboxes);

#if UNITY_EDITOR
			EditorUtility.SetDirty(gameObject);
			if (Player != null)
			{
				EditorUtility.SetDirty(Player);
			}
			if (Combat != null)
			{
				EditorUtility.SetDirty(Combat);
			}
#endif
		}

		[ContextMenu("Clear Sample Attack Hitboxes")]
		public void ClearSampleAttackHitboxes()
		{
			Transform parent = PreferredParent != null ? PreferredParent : FindPreferredParent();
			if (parent == null)
				return;

			Transform root = parent.Find("AttackHitboxes");
			if (root == null)
				return;

#if UNITY_EDITOR
			if (Application.isPlaying == false)
			{
				Undo.DestroyObjectImmediate(root.gameObject);
			}
			else
#endif
			{
				Destroy(root.gameObject);
			}
		}

		private void AssignToCombat(SigilWarAttackHitbox[] createdHitboxes)
		{
			if (Combat == null || Combat.AttackStages == null)
				return;

			for (int i = 0; i < Combat.AttackStages.Length && i < createdHitboxes.Length; i++)
			{
				SigilWarAttackStage stage = Combat.AttackStages[i];
				stage.Hitboxes = new[] { createdHitboxes[i] };
				Combat.AttackStages[i] = stage;
			}
		}

		private Transform FindPreferredParent()
		{
			if (string.IsNullOrEmpty(PreferredParentName))
				return null;

			Transform[] children = GetComponentsInChildren<Transform>(true);
			for (int i = 0; i < children.Length; i++)
			{
				if (children[i].name == PreferredParentName)
					return children[i];
			}

			return null;
		}

		private Transform FindOrCreateChild(Transform parent, string childName)
		{
			Transform child = parent.Find(childName);
			if (child != null)
				return child;

			GameObject childObject = new GameObject(childName);
			child = childObject.transform;

#if UNITY_EDITOR
			if (Application.isPlaying == false)
			{
				Undo.RegisterCreatedObjectUndo(childObject, $"Create {childName}");
			}
#endif

			child.SetParent(parent, false);
			return child;
		}
	}
}
