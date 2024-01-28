using UnityEngine;
using System;

namespace RobotRush.Types
{
	/// <summary>
	/// This script defines a round in the game. When the player kills all enemies, the round is increased and the difficulty is changed accordingly
	/// This class is used in the Game Controller script
	/// </summary>
	[Serializable]
	public class Round
	{
		[Tooltip("The name of the current round")]
		public string roundName = "ROUND 1";

		[Tooltip("The number of kills needed to win this round")]
		public int enemyCount = 10;

		[Tooltip("The speed of the enemies in this round")]
		public float enemySpeed = 1;

		[Tooltip("How quickly enemies are spawned in this round")]
		public float spawnDelay = 1;

		[Tooltip("The boss enemy that appears at the end of the round. You can leave this empty if you don't want a boss in this round")]
		public Transform enemyBoss;

		[Tooltip("How many seconds to wait before spawning the boss. The delay count stars after the last enemy is spawned")]
		public float bossDelay = 2;
	}
}