using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace SigilWarAscend.Gameplay
{
	[Serializable]
	public sealed class ItemSpawnPointGroup
	{
		public string Name = "Default";
		public Transform[] SpawnPoints;
	}

	public sealed class ItemSpawnController : MonoBehaviour
	{
		[Header("Item Spawn")]
		public NetworkObject ItemPrefab;
		public ItemSpawnPointGroup[] SpawnPointGroups;
		public int ItemsPerWave = 1;
		public int MaxAliveItems = 10;

		[Header("Phase Rules")]
		public bool SpawnDuringPreparationPhase;
		public bool SpawnDuringLanePhase = true;
		public bool SpawnDuringPortalPhase = true;
		public bool SpawnDuringCorePhase = true;
		public float PreparationSpawnInterval = 15f;
		public float LaneSpawnInterval = 20f;
		public float PortalSpawnInterval = 15f;
		public float CoreSpawnInterval = 12f;
		public bool ClearItemsOnMatchEnd = true;

		private readonly List<NetworkObject> _aliveItems = new List<NetworkObject>();
		private readonly Dictionary<int, int> _groupSpawnCounters = new Dictionary<int, int>();

		private SigilWarGameManager _gameManager;
		private TickTimer _spawnTimer;

		public void Initialize(SigilWarGameManager gameManager)
		{
			_gameManager = gameManager;
			_spawnTimer = default;
			CleanupAliveItems();
		}

		public void HandlePhaseStarted(MatchPhase phase)
		{
			CleanupAliveItems();

			if (phase == MatchPhase.MatchEnded)
			{
				if (ClearItemsOnMatchEnd)
				{
					DespawnAll();
				}

				_spawnTimer = default;
				return;
			}

			if (TryGetSpawnInterval(phase, out float interval))
			{
				_spawnTimer = TickTimer.CreateFromSeconds(_gameManager.Runner, interval);
				Log($"Prepare item spawn timer for {phase} | interval={interval:0.00}s");
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

			CleanupAliveItems();

			if (ItemPrefab == null || TryGetSpawnInterval(phase, out float interval) == false)
				return;

			if (_aliveItems.Count >= MaxAliveItems)
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
			if (SpawnPointGroups == null || SpawnPointGroups.Length == 0)
				return;

			for (int spawnIndex = 0; spawnIndex < ItemsPerWave; spawnIndex++)
			{
				if (_aliveItems.Count >= MaxAliveItems)
					return;

				SpawnItem();
			}
		}

		private void SpawnItem()
		{
			var spawnPoint = GetNextSpawnPoint();
			if (spawnPoint == null)
				return;

			var item = _gameManager.Runner.Spawn(ItemPrefab, spawnPoint.position, spawnPoint.rotation, PlayerRef.None);
			if (item != null)
			{
				_aliveItems.Add(item);
				Log($"Spawn item at position={spawnPoint.position}, alive={_aliveItems.Count}");
			}
		}

		private Transform GetNextSpawnPoint()
		{
			if (SpawnPointGroups == null || SpawnPointGroups.Length == 0)
				return null;

			for (int groupIndex = 0; groupIndex < SpawnPointGroups.Length; groupIndex++)
			{
				var group = SpawnPointGroups[groupIndex];
				if (group == null || group.SpawnPoints == null || group.SpawnPoints.Length == 0)
					continue;

				int counter = 0;
				_groupSpawnCounters.TryGetValue(groupIndex, out counter);
				int pointIndex = Mathf.Abs(counter) % group.SpawnPoints.Length;
				_groupSpawnCounters[groupIndex] = counter + 1;
				return group.SpawnPoints[pointIndex];
			}

			return null;
		}

		private bool TryGetSpawnInterval(MatchPhase phase, out float interval)
		{
			switch (phase)
			{
				case MatchPhase.Preparation:
					interval = PreparationSpawnInterval;
					return SpawnDuringPreparationPhase && interval > 0f;
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

		private void CleanupAliveItems()
		{
			for (int i = _aliveItems.Count - 1; i >= 0; i--)
			{
				if (_aliveItems[i] == null || _aliveItems[i].Runner == null)
				{
					_aliveItems.RemoveAt(i);
				}
			}
		}

		private void DespawnAll()
		{
			if (_gameManager == null || _gameManager.HasStateAuthority == false)
				return;

			for (int i = _aliveItems.Count - 1; i >= 0; i--)
			{
				var item = _aliveItems[i];
				if (item == null)
					continue;

				if (item.Runner != null)
				{
					item.Runner.Despawn(item);
				}
			}

			_aliveItems.Clear();
			Log("Despawn all items");
		}

		private void Log(string message)
		{
			if (_gameManager == null)
				return;

			_gameManager.LogItemSpawn(message);
		}
	}
}
