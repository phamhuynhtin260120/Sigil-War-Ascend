using SigilWarAscend.UI;
using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	[DisallowMultipleComponent]
	public sealed class SigilWarPlayerPresentation : MonoBehaviour
	{
		public SigilWarPlayer Player;

		private int _animIDSpeed;
		private int _animIDGrounded;
		private int _animIDJump;
		private int _animIDFreeFall;
		private int _animIDMotionSpeed;
		private int _animIDDead;
		private int _animIDHit;

		private void Awake()
		{
			AssignAnimationIDs();
		}

		private void Reset()
		{
			Player = GetComponent<SigilWarPlayer>();
		}

		public void Render()
		{
			if (Player == null)
				return;

			if (Player.Animator != null && Player.KCC != null)
			{
				Player.Animator.SetFloat(_animIDSpeed, Player.KCC.RealSpeed, 0.15f, Time.deltaTime);
				Player.Animator.SetFloat(_animIDMotionSpeed, 1f);
				Player.Animator.SetBool(_animIDJump, Player.IsJumpingValue);
				Player.Animator.SetBool(_animIDGrounded, Player.KCC.IsGrounded);
				Player.Animator.SetBool(_animIDFreeFall, Player.KCC.RealVelocity.y < -10f);
				Player.Animator.SetBool(_animIDDead, Player.Health != null && Player.Health.IsAlive == false);
			}

			if (Player.FootstepSound != null && Player.KCC != null)
			{
				Player.FootstepSound.enabled = Player.Health != null && Player.Health.IsAlive && Player.KCC.IsGrounded && Player.KCC.RealSpeed > 1f;
				float sprintSpeed = Player.Movement != null ? Player.Movement.SprintSpeed : 5f;
				Player.FootstepSound.pitch = Player.KCC.RealSpeed > sprintSpeed - 1f ? 1.5f : 1f;
			}

			if (Player.DustParticles != null && Player.KCC != null)
			{
				var emission = Player.DustParticles.emission;
				emission.enabled = Player.Health != null && Player.Health.IsAlive && Player.KCC.IsGrounded && Player.KCC.RealSpeed > 1f;
			}

			if (Player.Hitbox != null && Player.Health != null)
			{
				Player.Hitbox.enabled = Player.Health.IsAlive;
			}
		}

		public void OnJumpingChanged()
		{
			if (Player == null || Player.KCC == null)
				return;

			if (Player.IsJumpingValue)
			{
				if (Player.JumpAudioClip != null)
				{
					AudioSource.PlayClipAtPoint(Player.JumpAudioClip, Player.KCC.Position, 1f);
				}
			}
			else
			{
				if (Player.LandAudioClip != null)
				{
					AudioSource.PlayClipAtPoint(Player.LandAudioClip, Player.KCC.Position, 1f);
				}
			}
		}

		public void OnCollectedPickupsChanged()
		{
			if (Player == null || Player.CollectedPickups <= 0)
				return;

			if (Player.PickupAudioClip != null && Player.KCC != null)
			{
				AudioSource.PlayClipAtPoint(Player.PickupAudioClip, Player.KCC.Position, 1f);
			}

			Player.CameraController?.NotifyPickup();
		}

		public void PlayHitReaction()
		{
			if (Player == null || Player.Animator == null || Player.Health == null || Player.Health.IsAlive == false)
				return;

			if (HasAnimatorParameter("Hit", AnimatorControllerParameterType.Trigger) == false)
				return;

			Player.Animator.SetTrigger(_animIDHit);
			Player.CameraController?.NotifyHit();
		}

		public void OnNicknameChanged()
		{
			if (Player == null || Player.Nameplate == null)
				return;

			if (Player.IsLocalPlayer)
			{
				Player.Nameplate.enabled = false;
				return;
			}

			Player.Nameplate.enabled = true;
			Player.Nameplate.SetNickname(Player.Nickname);
		}

		public void ResetOnRespawn()
		{
			if (Player == null)
				return;

			if (Player.Hitbox != null)
			{
				Player.Hitbox.enabled = Player.Health != null && Player.Health.IsAlive;
			}

			if (Player.Animator != null)
			{
				Player.Animator.Rebind();
				Player.Animator.Update(0f);
				Player.Animator.SetBool(_animIDDead, false);
				Player.Animator.SetBool(_animIDJump, false);
				Player.Animator.SetBool(_animIDFreeFall, false);
			}

			if (Player.DustParticles != null)
			{
				Player.DustParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			}

			if (Player.FootstepSound != null)
			{
				Player.FootstepSound.Stop();
			}

			OnNicknameChanged();

			SigilWarHealthBar[] healthBars = GetComponentsInChildren<SigilWarHealthBar>(true);
			for (int i = 0; i < healthBars.Length; i++)
			{
				healthBars[i].RefreshNow();
			}
		}

		private void AssignAnimationIDs()
		{
			if (Player == null)
			{
				Player = GetComponent<SigilWarPlayer>();
			}

			if (Player == null || Player.Animator == null)
				return;

			_animIDSpeed = Player.Animator.StringToHash("Speed");
			_animIDGrounded = Player.Animator.StringToHash("Grounded");
			_animIDJump = Player.Animator.StringToHash("Jump");
			_animIDFreeFall = Player.Animator.StringToHash("FreeFall");
			_animIDMotionSpeed = Player.Animator.StringToHash("MotionSpeed");
			_animIDDead = Player.Animator.StringToHash("Dead");
			_animIDHit = Player.Animator.StringToHash("Hit");
		}

		private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType expectedType)
		{
			if (Player == null || Player.Animator == null || string.IsNullOrEmpty(parameterName))
				return false;

			foreach (AnimatorControllerParameter parameter in Player.Animator.parameters)
			{
				if (parameter.name == parameterName && parameter.type == expectedType)
					return true;
			}

			return false;
		}
	}
}
