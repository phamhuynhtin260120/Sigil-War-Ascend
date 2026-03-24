using Fusion;
using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	public sealed class BossSpawnController : MonoBehaviour
	{
		[Header("Boss Spawn")]
		public NetworkObject BossPrefab;
		public Transform[] SpawnPoints;
		public MatchPhase SpawnPhase = MatchPhase.PortalPhase;
		public float SpawnDelayFromPhaseStart = 5f;
		public bool DespawnBossOnMatchEnd = true;

		private SigilWarGameManager _gameManager;
		private NetworkObject _activeBoss;
		private TickTimer _spawnTimer;
		private int _spawnCounter;

		public void Initialize(SigilWarGameManager gameManager)
		{
			_gameManager = gameManager;
			CleanupBossReference();
			_spawnTimer = default;
		}

		public void HandlePhaseStarted(MatchPhase phase)
		{
			CleanupBossReference();

			if (phase == MatchPhase.MatchEnded)
			{
				if (DespawnBossOnMatchEnd)
				{
					DespawnBoss();
				}

				_spawnTimer = default;
				return;
			}

			if (phase != SpawnPhase || BossPrefab == null)
			{
				_spawnTimer = default;
				return;
			}

			if (_activeBoss != null)
			{
				Log("Boss already active, skip spawn timer");
				return;
			}

			_spawnTimer = TickTimer.CreateFromSeconds(_gameManager.Runner, Mathf.Max(0f, SpawnDelayFromPhaseStart));
			Log($"Prepare boss spawn for {phase} | delay={SpawnDelayFromPhaseStart:0.00}s");
		}

		public void Tick(MatchPhase phase)
		{
			if (_gameManager == null || _gameManager.HasStateAuthority == false)
				return;

			CleanupBossReference();

			if (phase != SpawnPhase || BossPrefab == null || _activeBoss != null)
				return;

			if (_spawnTimer.IsRunning == false)
			{
				_spawnTimer = TickTimer.CreateFromSeconds(_gameManager.Runner, Mathf.Max(0f, SpawnDelayFromPhaseStart));
				return;
			}

			if (_spawnTimer.Expired(_gameManager.Runner) == false)
				return;

			SpawnBoss();
			_spawnTimer = default;
		}

		private void SpawnBoss()
		{
			var spawnPoint = GetNextSpawnPoint();
			if (spawnPoint == null)
				return;

			_activeBoss = _gameManager.Runner.Spawn(BossPrefab, spawnPoint.position, spawnPoint.rotation, PlayerRef.None);
			if (_activeBoss != null)
			{
				Log($"Spawn boss at {spawnPoint.position}");
			}
		}

		private Transform GetNextSpawnPoint()
		{
			if (SpawnPoints == null || SpawnPoints.Length == 0)
				return null;

			int index = Mathf.Abs(_spawnCounter) % SpawnPoints.Length;
			_spawnCounter++;
			return SpawnPoints[index];
		}

		private void DespawnBoss()
		{
			CleanupBossReference();
			if (_activeBoss == null || _activeBoss.Runner == null)
				return;

			_activeBoss.Runner.Despawn(_activeBoss);
			_activeBoss = null;
			Log("Despawn active boss");
		}

		private void CleanupBossReference()
		{
			if (_activeBoss != null && _activeBoss.Runner == null)
			{
				_activeBoss = null;
			}
		}

		private void Log(string message)
		{
			if (_gameManager == null)
				return;

			_gameManager.LogBossSpawn(message);
		}
	}
}
