using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RobotRush.Types;

namespace RobotRush
{
	/// <summary>
	/// This script controls the game, starting it, following game progress, and finishing it with game over.
	/// </summary>
	public class RRGGameController:MonoBehaviour
	{
		public GameObject Ads;
		[Header("<Player Options>")]
		[Tooltip("The player object, assigned from the scene")]
		public RRGPlayer playerObject;
        
		[Tooltip("A list of all the enemies/objects spawned in the game")]
		public RRGEnemy[] enemies;

        [Tooltip("The distance at which this object is spawned relative to the spawnAroundObject")]
        public Vector2 spawnDistance = new Vector2(5, 10);

        [Tooltip("How may seconds to wait between enemy spawns")]
		public float spawnDelay = 1;
		internal float spawnDelayCount = 0;

        [Tooltip("A list of all the items spawned in the game")]
        public RRGItem[] items;

        [Tooltip("How may seconds to wait between item spawns")]
        public float itemDelay = 1;
        internal float itemDelayCount = 0;

        [Tooltip("A list of powerups that can be activated")]
        public Powerup[] powerups;

        [Tooltip("A list of rounds in the game, including the number of kills needed to win the game, the speed of enemies, and the spawn rate of enemies")]
		public Round[] rounds;
		
		[Tooltip("The index number of the current level we are on")]
		public int currentLevel = 0;

		// How many enemies are left in this level, alive
		internal int enemyCount = 0;
		
		// How many enemies have been killed in this level
		internal int killCount = 0;

		// Has the boss been spawned in this level?
		internal bool bossSpawned = false;

		// The current boss in the level
		internal Transform currentBoss;
        
		[Tooltip("The bonus effect that shows how much bonus we got when we hit a target")]
		public Transform bonusEffect;
		
		[Tooltip("The score of the game. Score is earned by shooting enemies")]
		public float score = 0;
		internal float scoreCount = 0;
		
		[Tooltip("The text object that displays the score, assigned from the scene")]
		public Text scoreText;
        internal string scoreTextPadding;
		internal float highScore = 0;
		internal float scoreMultiplier = 1;

		[Tooltip("The effect displayed before starting the game")]
		public Transform readyGoEffect;
		
		[Tooltip("How long to wait before starting gameplay. In this time we usually display the readyGoEffect")]
		public float startDelay = 1;

		[Tooltip("The overall game speed. This affects the entire game (Time.timeScale)")]
		public float gameSpeed = 1;

		// Is the game over?
		internal bool  isGameOver = false;

		[Tooltip("The button that we press to make the player shoot")]
		public string shootButton = "Fire1";

        [Tooltip("Wait until the player rotated to the target position before shooting")]
        public bool rotateBeforeShooting = false;

        [Tooltip("Auto shoot while the player is holding down the shoot button, like a machine gun")]
        public bool autoShoot = false;

        [Tooltip("The level of the main menu that can be loaded after the game ends")]
		public string mainMenuLevelName = "RRGMenu";
		
		[Tooltip("The keyboard/gamepad button that will restart the game after game over")]
		public string confirmButton = "Submit";
		
		[Tooltip("The keyboard/gamepad button that pauses the game")]
		public string pauseButton = "Cancel";
		internal bool  isPaused = false;

		[Header("<User Interface>")]
		[Tooltip("Various canvases for the UI, assign them from the scene")]
		public Transform gameCanvas;
        public Transform pauseCanvas;
		public Transform gameOverCanvas;
        public Transform victoryCanvas;
        public Transform levelUpCanvas;
        internal Transform progressCanvas;
        internal Transform healthCanvas;
        public Transform transitionCanvas;

		[Header("<Sound Options>")]
		[Tooltip("")]
		// Various sounds and their source
		public AudioClip soundLevelUp;
		public AudioClip soundGameOver;
        public AudioClip soundVictory;
        public string soundSourceTag = "Sound";
		internal GameObject soundSource;
        
        // A general use index
        internal int index = 0;
		internal int indexB = 0;
		internal int indexSpawn = 0;

		void Awake()
		{
			// Activate the pause canvas early on, so it can detect info about sound volume state
			if ( pauseCanvas )    pauseCanvas.gameObject.SetActive(true);
		}

		/// <summary>
		/// Start is only called once in the lifetime of the behaviour.
		/// The difference between Awake and Start is that Start is only called if the script instance is enabled.
		/// This allows you to delay any initialization code, until it is really needed.
		/// Awake is always called before any Start functions.
		/// This allows you to order initialization of scripts
		/// </summary>
		void Start()
		{
			Ads = GameObject.Find("ads");
            if (rounds.Length == 0) Debug.LogWarning("You must assign at least one round in the gamecontroller");
            
            // Calculate the score text padding ( the zeros filling up the score value, so instead of 12 for example it will be 0000012 )
            if (scoreText) scoreTextPadding = scoreText.text;

            //Update the score and enemy count
            UpdateScore();
			UpdateKillCount();

			//Hide the game over and pause screens
			if ( gameOverCanvas )    gameOverCanvas.gameObject.SetActive(false);
            if (victoryCanvas) victoryCanvas.gameObject.SetActive(false);
            if ( pauseCanvas )    pauseCanvas.gameObject.SetActive(false);

			//Get the highscore for the player
			highScore = PlayerPrefs.GetFloat(SceneManager.GetActiveScene().name + "HighScore", 0);
            
			//Assign the sound source for easier access
			if ( GameObject.FindGameObjectWithTag(soundSourceTag) )    soundSource = GameObject.FindGameObjectWithTag(soundSourceTag);

            if (progressCanvas == null) progressCanvas = gameCanvas.Find("Progress");
            if (healthCanvas == null) healthCanvas = gameCanvas.Find("Health");

            //Go through all the powerups and reset their timers
            for (index = 0; index < powerups.Length; index++)
            {
                //Set the maximum duration of the powerup
                powerups[index].durationMax = powerups[index].duration;

                //Reset the duration counter
                powerups[index].duration = 0;

                //Deactivate the icon of the powerup
                powerups[index].icon.gameObject.SetActive(false);
            }

            // Check what level we are on
            UpdateLevel();
            
            // Create the ready?GO! effect
            if (readyGoEffect) readyGoEffect.gameObject.SetActive(true);

            // If we have a shop in the scene, assign the player from it.
            if ( GameObject.FindObjectOfType<RRGShop>() )
            {
                playerObject = GameObject.FindObjectOfType<RRGShop>().currentPlayer;
            }

            // Create the player object
            playerObject = Instantiate(playerObject);


        }

		/// <summary>
		/// Update is called every frame, if the MonoBehaviour is enabled.
		/// </summary>
		void  Update()
		{
			// Make the score count up to its current value
			if ( score < scoreCount )
			{
				// Count up to the courrent value
				score = Mathf.Lerp( score, scoreCount, Time.deltaTime * 10);
				
				// Update the score text
				UpdateScore();
			}

			// Delay the start of the game
			if ( startDelay > 0 )
			{
				startDelay -= Time.deltaTime;
			}
			else
			{
				//If the game is over, listen for the Restart and MainMenu buttons
				if ( isGameOver == true )
				{
					//The jump button restarts the game
					if ( Input.GetButtonDown(confirmButton) )
					{
						Restart();
					}
					
					//The pause button goes to the main menu
					if ( Input.GetButtonDown(pauseButton) )
					{
						MainMenu();
					}
				}
				else
				{
					//Toggle pause/unpause in the game
					if ( Input.GetButtonDown(pauseButton) )
					{
						if ( isPaused == true )    Unpause();
						else    Pause(true);
					}

                    // Get the pointer position on the ground when we click/tap
                    if ( !EventSystem.current.IsPointerOverGameObject())
                    {
                        if ( (autoShoot == true && Input.GetButton(shootButton)) || (autoShoot == false && Input.GetButtonDown(shootButton)) )
                        {
                            RotateAndShoot();
                        }
                    }

                    // Spanwing items
                    if (itemDelayCount > 0) itemDelayCount -= Time.deltaTime;
                    else
                    {
                        // Reset the item delay count
                        itemDelayCount = itemDelay;

                        // Spawn an item within the game area
                        SpawnItem(items);
                    }

                    // If we haven't spawned all the enemies yet
                    if ( enemyCount > 0 )
					{
						// Count down to the next object spawn
						if ( spawnDelayCount > 0 )    spawnDelayCount -= Time.deltaTime;
						else 
						{
							// Reset the spawn delay count
							spawnDelayCount = spawnDelay;
                            
                            Spawn(enemies);
							
							enemyCount--;
						}
					}
					else if ( rounds[currentLevel].enemyBoss && bossSpawned == false )
					{
						// Spawn the enemy boss
						StartCoroutine("SpawnBoss");

						// The boss has been spawned
						bossSpawned = true;
					}

					// If the boss is killed, level up!
					if ( killCount >= rounds[currentLevel].enemyCount + 1 && bossSpawned == true && currentBoss == null )    
					{
                        if (currentLevel < rounds.Length - 1)
                        {
                            LevelUp();

                        }
                        else
                        {
                            StartCoroutine("Victory", 1.0f);
                        }
					}
				}
			}
		}

        /// <summary>
        /// enemies an object based on the index chosen from the array
        /// </summary>
        /// <param name="spawnArray"></param>
        /// <param name="spawnIndex"></param>
        /// <param name="spawnGap"></param>
        public void SpawnItem(RRGItem[] currentSpawnList)
        {
            int spawnIndex = Mathf.FloorToInt(Random.Range(0, currentSpawnList.Length));

            // Create a new Object spawn based on the index which loops in the list
            Transform newSpawn = Instantiate(currentSpawnList[spawnIndex].transform) as Transform;

            // Spawn an Object at the target position
            if (playerObject.transform) newSpawn.position = playerObject.transform.position;// + new Vector3(Random.Range(-5, 5), 0, Random.Range(-5, 5));

            // Rotate the object randomly, and then move it forward to a random distance from the spawn point
            newSpawn.eulerAngles = Vector3.up * Random.Range(0, 360);
            newSpawn.Translate(Vector3.forward * Random.Range(spawnDistance.x, spawnDistance.y), Space.Self);

            // Then rotate it back to face the spawn point
            newSpawn.eulerAngles += Vector3.up * 180;

            // Set the speed of the spawned object
            //newSpawn.SendMessage("SetSpeed", rounds[currentLevel].enemySpeed);
        }

        /// <summary>
        /// enemies an object based on the index chosen from the array
        /// </summary>
        /// <param name="spawnArray"></param>
        /// <param name="spawnIndex"></param>
        /// <param name="spawnGap"></param>
        public void Spawn(RRGEnemy[] currentSpawnList)
        {
            int spawnIndex = Mathf.FloorToInt(Random.Range(0, currentSpawnList.Length));

            // Create a new Object spawn based on the index which loops in the list
            Transform newSpawn = Instantiate(currentSpawnList[spawnIndex].transform) as Transform;

            // Spawn an Object at the target position
            if (playerObject.transform) newSpawn.position = playerObject.transform.position;// + new Vector3(Random.Range(-5, 5), 0, Random.Range(-5, 5));

            // Rotate the object randomly, and then move it forward to a random distance from the spawn point
            newSpawn.eulerAngles = Vector3.up * Random.Range(0, 360);
            newSpawn.Translate(Vector3.forward * Random.Range(spawnDistance.x, spawnDistance.y), Space.Self);

            // Then rotate it back to face the spawn point
            newSpawn.eulerAngles += Vector3.up * 180;
            
            // Set the speed of the spawned object
            newSpawn.SendMessage("SetSpeed", rounds[currentLevel].enemySpeed);
        }

        /// <summary>
        /// Creates a new boss enemy at the end of the middle lane
        /// </summary>
        IEnumerator SpawnBoss()
		{
			// Create a new random target from the target list
			Transform newBoss = Instantiate(rounds[currentLevel].enemyBoss) as Transform;

			// Assign the boss to a variable so we can check later if it was killed
			currentBoss = newBoss;

			// Disable the boss object until it's time to enable it
			newBoss.gameObject.SetActive(false);

			yield return new WaitForSeconds(rounds[currentLevel].bossDelay);
			
			// Enable the boss object
			newBoss.gameObject.SetActive(true);

            // Spawn an Object at the target position
            if (playerObject.transform) newBoss.position = playerObject.transform.position;// + new Vector3(Random.Range(-5, 5), 0, Random.Range(-5, 5));

            // Rotate the object randomly, and then move it forward to a random distance from the spawn point
            newBoss.eulerAngles = Vector3.up * Random.Range(0, 360);
            newBoss.Translate(Vector3.forward * Random.Range(spawnDistance.x, spawnDistance.y), Space.Self);

            // Then rotate it back to face the spawn point
            newBoss.eulerAngles += Vector3.up * 180;

            // Set the speed of the spawned object
            newBoss.SendMessage("SetSpeed", rounds[currentLevel].enemySpeed);
        }

        /// <summary>
        /// Commands the player to rotate to a target point, and shoot at it
        /// </summary>
        /// <param name="changeValue">Change value, negative for left and positive for right</param>
        public void RotateAndShoot()
		{
            // Cast a ray from the mouse position to the world
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            float rayDistance;

            // The ground plain that we use to detect the mouse/tap position
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out rayDistance))
            {
                playerObject.SetTarget(ray.GetPoint(rayDistance));
            }

            // If we don't need to rotate to the target position before shooting, shoot immediately!
            if ( rotateBeforeShooting == false ) playerObject.Shoot();
        }
		
		/// <summary>
		/// Give a bonus when the target is hit. The bonus is multiplied by the number of targets on screen
		/// </summary>
		/// <param name="hitSource">The target that was hit</param>
		public void HitBonus( Transform hitSource )
		{
			// If we have a bonus effect
			if ( bonusEffect )
			{
				// Create a new bonus effect at the hitSource position
				Transform newBonusEffect = Instantiate(bonusEffect, hitSource.position, Quaternion.identity) as Transform;

				// Display the bonus value multiplied score multiplier
				newBonusEffect.Find("Text").GetComponent<Text>().text = "+" + (hitSource.GetComponent<RRGEnemy>().bonus * scoreMultiplier).ToString();

				// Rotate the bonus text slightly
				newBonusEffect.eulerAngles = Vector3.forward * Random.Range(-10,10);
			}
            
            // Add the bonus to the score
            ChangeScore(hitSource.GetComponent<RRGEnemy>().bonus * Mathf.RoundToInt(scoreMultiplier));
		}

        /// <summary>
        /// Activates a power up from a list of available power ups
        /// </summary>
        /// <param name="setValue">The index numebr of the powerup to activate</param>
        IEnumerator ActivatePowerup(int powerupIndex)
        {
            //If there is already a similar powerup running, refill its duration timer
            if (powerups[powerupIndex].duration > 0)
            {
                //Refil the duration of the powerup to maximum
                powerups[powerupIndex].duration = powerups[powerupIndex].durationMax;
            }
            else //Otherwise, activate the power up functions
            {
                //Activate the powerup icon
                if (powerups[powerupIndex].icon) powerups[powerupIndex].icon.gameObject.SetActive(true);

                //Run up to two start functions from the gamecontroller
                if (powerups[powerupIndex].startFunction != string.Empty) SendMessage(powerups[powerupIndex].startFunction, powerups[powerupIndex].startParamater);

                //Fill the duration timer to maximum
                powerups[powerupIndex].duration = powerups[powerupIndex].durationMax;

                //Count down the duration of the powerup
                while (powerups[powerupIndex].duration > 0)
                {
                    yield return new WaitForSeconds(Time.deltaTime);

                    powerups[powerupIndex].duration -= Time.deltaTime;

                    //Animate the powerup timer graphic using fill amount
                    if (powerups[powerupIndex].icon) powerups[powerupIndex].icon.GetComponent<Image>().fillAmount = powerups[powerupIndex].duration / powerups[powerupIndex].durationMax;
                }

                //Run up to two end functions from the gamecontroller
                if (powerups[powerupIndex].endFunction != string.Empty) SendMessage(powerups[powerupIndex].endFunction, powerups[powerupIndex].endParamater);

                //Deactivate the powerup icon
                if (powerups[powerupIndex].icon) powerups[powerupIndex].icon.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Sends a SetSpeedMultiplier command to the player, which makes it either faster or slower
        /// </summary>
        public void SetSpeedMultiplier(float setValue)
        {
            if (playerObject) playerObject.SendMessage("SetSpeedMultiplier", setValue, SendMessageOptions.DontRequireReceiver);
        }

        public void KillAll()
        {
            foreach (RRGEnemy enemy in GameObject.FindObjectsOfType<RRGEnemy>())
            {
                enemy.SendMessage("Die");
            }
        }
        
		/// <summary>
		/// Change the score
		/// </summary>
		/// <param name="changeValue">Change value</param>
		public void  ChangeScore( int changeValue )
		{
			scoreCount += changeValue;

			//Update the score
			UpdateScore();
		}
		
		/// <summary>
		/// Updates the score value and checks if we got to the next level
		/// </summary>
		void  UpdateScore()
		{
			//Update the score text
			if ( scoreText )    scoreText.text = Mathf.CeilToInt(score).ToString(scoreTextPadding);
		}

		/// <summary>
		/// Set the score multiplier ( Get double score for hitting and destroying targets )
		/// </summary>
		public void SetScoreMultiplier( int setValue )
		{
			// Set the score multiplier
			scoreMultiplier = setValue;
		}

		/// <summary>
		/// Changes the kill count for this level
		/// </summary>
		/// <param name="changeValue">Change value.</param>
		public void ChangeKillCount( int changeValue )
		{
			killCount += changeValue;

			UpdateKillCount();
		}

        /// <summary>
        /// Updates the kill count, and checking if we need to level up
        /// </summary>
        public void UpdateKillCount()
		{
            if (rounds.Length == 0) return;

			// If all enemies are killed, level up!
			if ( killCount >= rounds[currentLevel].enemyCount && rounds[currentLevel].enemyBoss == null )    
			{
                if (currentLevel < rounds.Length - 1)
                {
                    LevelUp();
                }
                else
                {
                    StartCoroutine("Victory", 1.0f);
                }
			}

			// Update the progress bar to show how far we are from the next level
			if ( progressCanvas )
			{
				progressCanvas.Find("Full").GetComponent<Image>().fillAmount = killCount * 1.0f/rounds[currentLevel].enemyCount * 1.0f;
			}
		}

        /// <summary>
        /// rounds up, and increases the difficulty of the game
        /// </summary>
        public void LevelUp()
		{
			currentLevel++;

			// Now boss is spawned yet in the new level
			bossSpawned = false;

			// Reset the kill count
			killCount = 0;

			// Reset the spawn delay
			spawnDelay = 0;

			// Update the level attributes
			UpdateLevel();

			//Run the level up effect, displaying a sound
			LevelUpEffect();
		}

        /// <summary>
        /// Updates the level and sets some values like maximum targets, throw angle, and level text
        /// </summary>
        public void UpdateLevel()
		{
            if (rounds.Length == 0) return;

			// Display the current level we are on
			if ( progressCanvas )    progressCanvas.Find("Text").GetComponent<Text>().text = (currentLevel + 1).ToString();

			// Set the enemy count based on the current level
			enemyCount = rounds[currentLevel].enemyCount;

			// Change the speed of the game
			gameSpeed = rounds[currentLevel].enemySpeed;

			// Change the spawn delay of the enemies
			spawnDelay = rounds[currentLevel].spawnDelay;
		}

        /// <summary>
        /// Shows the effect associated with leveling up ( a sound and text bubble )
        /// </summary>
        public void LevelUpEffect()
		{
			// If a level up effect exists, update it and play its animation
			if ( levelUpCanvas )
			{
				// Update the text of the level
				levelUpCanvas.Find("Text").GetComponent<Text>().text = rounds[currentLevel].roundName;

				// Play the level up animation
				if ( levelUpCanvas.GetComponent<Animation>() )    levelUpCanvas.GetComponent<Animation>().Play();
			}

			//If there is a source and a sound, play it from the source
			if ( soundSource && soundLevelUp )    
			{
				soundSource.GetComponent<AudioSource>().pitch = 1;

				soundSource.GetComponent<AudioSource>().PlayOneShot(soundLevelUp);
			}
		}

		/// <summary>
		/// Shuffles the specified text list, and returns it
		/// </summary>
		/// <param name="texts">A list of texts</param>
		float[] Shuffle( float[] positions )
		{
			// Go through all the positions and shuffle them
			for ( index = 0 ; index < positions.Length ; index++ )
			{
				// Hold the text in a temporary variable
				float tempNumber = positions[index];
				
				// Choose a random index from the text list
				int randomIndex = UnityEngine.Random.Range( index, positions.Length);
				
				// Assign a random text from the list
				positions[index] = positions[randomIndex];
				
				// Assign the temporary text to the random question we chose
				positions[randomIndex] = tempNumber;
			}
			
			return positions;
		}
        
		/// <summary>
		/// Pause the game, and shows the pause menu
		/// </summary>
		/// <param name="showMenu">If set to <c>true</c> show menu.</param>
		public void  Pause( bool showMenu )
		{
			isPaused = true;
			
			//Set timescale to 0, preventing anything from moving
			Time.timeScale = 0;
			
			//Show the pause screen and hide the game screen
			if ( showMenu == true )
			{
				if ( pauseCanvas )    pauseCanvas.gameObject.SetActive(true);
				if ( gameCanvas )    gameCanvas.gameObject.SetActive(false);
			}
		}
		
		/// <summary>
		/// Resume the game
		/// </summary>
		public void  Unpause()
		{
			isPaused = false;
			
			//Set timescale back to the current game speed
			Time.timeScale = 1;
			
			//Hide the pause screen and show the game screen
			if ( pauseCanvas )    pauseCanvas.gameObject.SetActive(false);
			if ( gameCanvas )    gameCanvas.gameObject.SetActive(true);
		}

		/// <summary>
		/// Runs the game over event and shows the game over screen
		/// </summary>
		IEnumerator GameOver(float delay)
		{
			Ads.GetComponent<InterstitialAdExample>().ShowAd();
			isGameOver = true;

            //Go through all the powerups and nullify their timers, making them end
            for (index = 0; index < powerups.Length; index++)
            {
                //Set the duration of the powerup to 0
                powerups[index].duration = 0;
            }

            yield return new WaitForSeconds(delay);
			
			//Remove the pause and game screens
			if ( pauseCanvas )    Destroy(pauseCanvas.gameObject);
			if ( gameCanvas )    Destroy(gameCanvas.gameObject);
            
            //Add the score to the total money we have
            PlayerPrefs.SetInt("Money", PlayerPrefs.GetInt("Money", 0) + Mathf.RoundToInt(score));

            //Show the game over screen
            if ( gameOverCanvas )    
			{
				//Show the game over screen
				gameOverCanvas.gameObject.SetActive(true);
				
				//Write the score text
				gameOverCanvas.Find("TextScore").GetComponent<Text>().text = "SCORE " + Mathf.RoundToInt(score).ToString();
				
				//Check if we got a high score
				if ( score > highScore )    
				{
					highScore = score;
					
					//Register the new high score
					PlayerPrefs.SetFloat(SceneManager.GetActiveScene().name + "HighScore", score);
				}
				
				//Write the high sscore text
				gameOverCanvas.Find("TextHighScore").GetComponent<Text>().text = "HIGH SCORE " + Mathf.RoundToInt(highScore).ToString();

				//If there is a source and a sound, play it from the source
				if ( soundSource && soundGameOver )    
				{
					soundSource.GetComponent<AudioSource>().pitch = 1;
					
					soundSource.GetComponent<AudioSource>().PlayOneShot(soundGameOver);
				}
			}
		}

        /// <summary>
        /// Runs the game over event and shows the game over screen
        /// </summary>
        IEnumerator Victory(float delay)
        {
            isGameOver = true;

            //Go through all the powerups and nullify their timers, making them end
            for (index = 0; index < powerups.Length; index++)
            {
                //Set the duration of the powerup to 0
                powerups[index].duration = 0;
            }

            yield return new WaitForSeconds(delay);

            //Remove the pause and game screens
            if (pauseCanvas) Destroy(pauseCanvas.gameObject);
            if (gameCanvas) Destroy(gameCanvas.gameObject);

            //Add the score to the total money we have
            PlayerPrefs.SetInt("Money", PlayerPrefs.GetInt("Money", 0) + Mathf.RoundToInt(score));

            //Show the game over screen
            if (victoryCanvas)
            {
                //Show the game over screen
                victoryCanvas.gameObject.SetActive(true);

                //Write the score text
                victoryCanvas.Find("TextScore").GetComponent<Text>().text = "SCORE " + Mathf.RoundToInt(score).ToString();

                //Check if we got a high score
                if (score > highScore)
                {
                    highScore = score;

                    //Register the new high score
                    PlayerPrefs.SetFloat(SceneManager.GetActiveScene().name + "HighScore", score);
                }

                //Write the high sscore text
                victoryCanvas.Find("TextHighScore").GetComponent<Text>().text = "HIGH SCORE " + Mathf.RoundToInt(highScore).ToString();

                //If there is a source and a sound, play it from the source
                if (soundSource && soundGameOver)
                {
                    soundSource.GetComponent<AudioSource>().pitch = 1;

                    soundSource.GetComponent<AudioSource>().PlayOneShot(soundVictory);
                }
            }
        }

        /// <summary>
        /// Restart the current level
        /// </summary>
        void  Restart()
		{
			SceneManager.LoadScene(SceneManager.GetActiveScene().name);
		}
		
		/// <summary>
		/// Restart the current level
		/// </summary>
		void  MainMenu()
		{
			SceneManager.LoadScene(mainMenuLevelName);
		}

		void OnDrawGizmos()
		{
            if (playerObject)
            {
                // Draw a sphere showing the spawn distance range for enemies
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(playerObject.transform.position, spawnDistance.x);
                Gizmos.DrawWireSphere(playerObject.transform.position, spawnDistance.y);

            }

        }
    }
}