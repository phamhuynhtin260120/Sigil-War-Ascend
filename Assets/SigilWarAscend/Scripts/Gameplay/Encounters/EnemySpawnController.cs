using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	[Serializable]
	public sealed class EnemySpawnGroup
	{
		public LaneType Lane;
		public Transform[] SpawnPoints;
	}

	public sealed class EnemySpawnController : MonoBehaviour
	{
		[Header("Enemy Spawn")]
		public NetworkObject EnemyPrefab;
		public EnemySpawnGroup[] SpawnGroups;
		public int SpawnsPerWavePerLane = 1;
		public int MaxAliveEnemies = 30;

		[Header("Phase Rules")]
		public bool SpawnDuringLanePhase = true;
		public bool SpawnDuringPortalPhase = true;
		public bool SpawnDuringCorePhase;
		public float LaneSpawnInterval = 15f;
		public float PortalSpawnInterval = 10f;
		public float CoreSpawnInterval = 20f;
		public bool ClearEnemiesOnMatchEnd = true;

		private readonly List<NetworkObject> _aliveEnemies = new List<NetworkObject>();
		private readonly Dictionary<LaneType, int> _laneSpawnCounters = new Dictionary<LaneType, int>();

		private SigilWarGameManager _gameManager;
		private TickTimer _spawnTimer;

		public void Initialize(SigilWarGameManager gameManager)
		{
			_gameManager = gameManager;
			_spawnTimer = default;
			CleanupAliveEnemies();
		}

		public void HandlePhaseStarted(MatchPhase phase)
		{
			CleanupAliveEnemies();

			if (phase == MatchPhase.MatchEnded)
			{
				if (ClearEnemiesOnMatchEnd)
				{
					DespawnAll();
				}

				_spawnTimer = default;
				return;
			}

			if (TryGetSpawnInterval(phase, out float interval))
			{
				_spawnTimer = TickTimer.CreateFromSeconds(_gameManager.Runner, interval);
				Log($"Prepare spawn timer for {phase} | interval={interval:0.00}s");
			}
			else
			{
				_spawnTimer = default;
			}
		}

		public void Tick(MatchPhase phase)
		{
			if (_gameManager == null || _gameManager.HasStateAuthority == false)
				return;

			CleanupAliveEnemies();

			if (EnemyPrefab == null || TryGetSpawnInterval(phase, out float interval) == false)
				return;

			if (_aliveEnemies.Count >= MaxAliveEnemies)
				return;

			if (_spawnTimer.IsRunning == false)
			{
				_spawnTimer = TickTimer.CreateFromSeconds(_gameManager.Runner, interval);
				return;
			}

			if (_spawnTimer.Expired(_gameManager.Runner) == false)
				return;

			SpawnWave();
			_spawnTimer = TickTimer.CreateFromSeconds(_gameManager.Runner, interval);
		}

		private void SpawnWave()
		{
			if (SpawnGroups == null || SpawnGroups.Length == 0)
				return;

			for (int i = 0; i < SpawnGroups.Length; i++)
			{
				var group = SpawnGroups[i];
				if (group == null || group.SpawnPoints == null || group.SpawnPoints.Length == 0)
					continue;

				for (int spawnIndex = 0; spawnIndex < SpawnsPerWavePerLane; spawnIndex++)
				{
					if (_aliveEnemies.Count >= MaxAliveEnemies)
						return;

					SpawnEnemyFromGroup(group);
				}
			}
		}

		private void SpawnEnemyFromGroup(EnemySpawnGroup group)
		{
			var spawnPoint = GetNextSpawnPoint(group);
			if (spawnPoint == null)
				return;

			var enemy = _gameManager.Runner.Spawn(EnemyPrefab, spawnPoint.position, spawnPoint.rotation, PlayerRef.None);
			if (enemy != null)
			{
				_aliveEnemies.Add(enemy);
				Log($"Spawn enemy at lane={group.Lane}, position={spawnPoint.position}, alive={_aliveEnemies.Count}");
			}
		}

		private Transform GetNextSpawnPoint(EnemySpawnGroup group)
		{
			if (group.SpawnPoints == null || group.SpawnPoints.Length == 0)
				return null;

			int counter = 0;
			_laneSpawnCounters.TryGetValue(group.Lane, out counter);
			int index = Mathf.Abs(counter) % group.SpawnPoints.Length;
			_laneSpawnCounters[group.Lane] = counter + 1;
			return group.SpawnPoints[index];
		}

		private bool TryGetSpawnInterval(MatchPhase phase, out float interval)
		{
			switch (phase)
			{
				case MatchPhase.LanePhase:
					interval = LaneSpawnInterval;
					return SpawnDuringLanePhase && interval > 0f;
				case MatchPhase.PortalPhase:
					interval = PortalSpawnInterval;
					return SpawnDuringPortalPhase && interval > 0f;
				case MatchPhase.CorePhase:
					interval = CoreSpawnInterval;
					return SpawnDuringCorePhase && interval > 0f;
				default:
					interval = 0f;
					return false;
			}
		}

		private void CleanupAliveEnemies()
		{
			for (int i = _aliveEnemies.Count - 1; i >= 0; i--)
			{
				if (_aliveEnemies[i] == null || _aliveEnemies[i].Runner == null)
				{
					_aliveEnemies.RemoveAt(i);
				}
			}
		}

		private void DespawnAll()
		{
			if (_gameManager == null || _gameManager.HasStateAuthority == false)
				return;

			for (int i = _aliveEnemies.Count - 1; i >= 0; i--)
			{
				var enemy = _aliveEnemies[i];
				if (enemy == null)
					continue;

				if (enemy.Runner != null)
				{
					enemy.Runner.Despawn(enemy);
				}
			}

			_aliveEnemies.Clear();
			Log("Despawn all enemies");
		}

		private void Log(string message)
		{
			if (_gameManager == null)
				return;

			_gameManager.LogEnemySpawn(message);
		}
	}
}
