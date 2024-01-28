using System.Collections;
using UnityEngine;
using RobotRush.Types;

namespace RobotRush
{
	/// <summary>
	/// This script defines an enemy, which spawn and moves along a lane. The enemy has health and can be killed with shot from the player. 
	/// If the enemy passes the player or touches it the game ends
	/// </summary>
	public class RRGEnemy : MonoBehaviour
	{
		internal Transform thisTransform;
        static RRGGameController gameController;

		[Tooltip("The enemy's health. If health reaches 0, this enemy dies")]
		public int health = 3;
		internal int healthMax;

        public int damage = 1;

		[Tooltip("How long the hurt effect is active when the enemy gets hurt")]
		public float hurtTime = 1;

		[Tooltip("The enemy is hurt. While hurt, the enemy has a unique animation")]
		internal bool isHurt = false;

        [Tooltip("The bonus we get when killing this enemy")]
        public int bonus = 10;

        [Tooltip("The effect that appears on the enemy when it has low health, near death")]
        public ParticleSystem lowHealthEffect;

        [Tooltip("The effect that is created at the location of this enemy when it dies")]
		public Transform deathEffect;

		[Tooltip("The movement speed of the enemy. This is controlled through the Levels in the Game Controller")]
		public float moveSpeed = 1;
        static float speedMultiplier = 1;
        
		[Tooltip("Various animation clips")]
		public AnimationClip spawnAnimation;
		public AnimationClip hurtAnimation;
		public AnimationClip moveAnimation;
		public AnimationClip attackAnimation;

		[Tooltip("Various sounds that play when the enemy touches the target, or when it gets hurt")]
		public AudioClip soundHitTarget;
		public AudioClip soundHurt;
		public string soundSourceTag = "Sound";
		internal GameObject soundSource;

		// The enemy is still spawning, it won't move until it finises spawning
		internal bool isSpawning = true;
	
		/// <summary>
		/// Start is only called once in the lifetime of the behaviour.
		/// The difference between Awake and Start is that Start is only called if the script instance is enabled.
		/// This allows you to delay any initialization code, until it is really needed.
		/// Awake is always called before any Start functions.
		/// This allows you to order initialization of scripts
		/// </summary>
		void Start()
		{
			thisTransform = transform;

            // Register the game controller for easier access
            if (gameController == null) gameController = GameObject.FindObjectOfType<RRGGameController>();

			//Assign the sound source for easier access
			if ( GameObject.FindGameObjectWithTag(soundSourceTag) )    soundSource = GameObject.FindGameObjectWithTag(soundSourceTag);

			// Add all the needed animation clips if they are missing from the animation component.
			if ( spawnAnimation && GetComponent<Animation>().GetClip(spawnAnimation.name) == null )    GetComponent<Animation>().AddClip( spawnAnimation, spawnAnimation.name);
			if ( hurtAnimation && GetComponent<Animation>().GetClip(hurtAnimation.name) == null )    GetComponent<Animation>().AddClip( hurtAnimation, moveAnimation.name);
			if ( moveAnimation && GetComponent<Animation>().GetClip(moveAnimation.name) == null )    GetComponent<Animation>().AddClip( moveAnimation, moveAnimation.name);
			if ( attackAnimation && GetComponent<Animation>().GetClip(attackAnimation.name) == null )    GetComponent<Animation>().AddClip( attackAnimation, attackAnimation.name);

			// Set the maximum health value of the enemy
			healthMax = health;

            // Set the initial health 
            StartCoroutine("ChangeHealth", 0);

            // Play the spawn animation, and then retrun to the move animation
            StartCoroutine( PlayAnimation( spawnAnimation, moveAnimation));
		}

		void Update()
		{
			// Move the enemy based on its speed
			if ( isSpawning == false )    thisTransform.Translate( Vector3.forward * moveSpeed * speedMultiplier * Time.deltaTime, Space.Self );
		}
	
		/// <summary>
		/// Is executed when this obstacle touches another object with a trigger collider
		/// </summary>
		/// <param name="other"><see cref="Collider"/></param>
		void OnTriggerEnter(Collider other)
		{	
			// Check if the object that was touched has the correct tag
			if( other.GetComponent<RRGPlayer>() )
			{
				// Change the health of the target
                other.SendMessage( "ChangeHealth", -damage, SendMessageOptions.DontRequireReceiver);
                
				// Play the attack animation, and then retrun to the move animation
				StartCoroutine( PlayAnimation( attackAnimation, moveAnimation));

				// If there is a sound source and audio to play, play the sound from the audio source
				if ( soundSource && soundHitTarget )    soundSource.GetComponent<AudioSource>().PlayOneShot(soundHitTarget);

                Invoke("Die", 0.5f);
            }
		}

		/// <summary>
		/// Sets the speed of the enemy
		/// </summary>
		/// <param name="setValue">value of speed</param>
		void SetSpeed( float setValue )
		{
			// Change the movement speed of the enemy
			moveSpeed = setValue;

			// Set the animation according to the move speed
			GetComponent<Animation>()[moveAnimation.name].speed = 1 + moveSpeed * 0.2f * speedMultiplier;
		}

        /// <summary>
        /// Sets the speed multiplier of all the enemies
        /// </summary>
        /// <param name="setValue">value of speed</param>
        void SetSpeedMultiplier(float setValue)
        {
            // Change the movement speed of the enemy
            speedMultiplier = setValue;

            // Set the animation according to the move speed
            GetComponent<Animation>()[moveAnimation.name].speed = 1 + moveSpeed * 0.2f * speedMultiplier;
        }

        /// <summary>
        /// Plays an animation and when it finishes it reutrns to a default animation
        /// </summary>
        /// <returns>The animation.</returns>
        /// <param name="firstAnimation">First animation.</param>
        /// <param name="defaultAnimation">Default animation to be played after first animation is done</param>
        IEnumerator PlayAnimation( AnimationClip firstAnimation, AnimationClip defaultAnimation )
		{
			if( GetComponent<Animation>() )
			{
				// If there is a spawn animation, play it
				if( firstAnimation )
				{
					// Stop the animation
					GetComponent<Animation>().Stop();
					
					// Play the animation
					GetComponent<Animation>().Play(firstAnimation.name);
				}
			
				// Wait for some time
				yield return new WaitForSeconds(firstAnimation.length);

				// If the spawning animation finished, we are no longer spawning and can start moving
				if ( isSpawning == true && firstAnimation == spawnAnimation )    isSpawning = false;

				// If there is a walk animation, play it
				if( defaultAnimation )
				{
					// Stop the animation
					GetComponent<Animation>().Stop();
					
					// Play the animation
					GetComponent<Animation>().CrossFade(defaultAnimation.name);
				}
			}
		}

		/// <summary>
		/// Changes the health of this enemy. If health reaches 0, it dies
		/// </summary>
		/// <param name="changeValue">Change value.</param>
		IEnumerator ChangeHealth( int changeValue )
		{
			health += changeValue; 

			// If health is low, show the low health effect
			if ( lowHealthEffect )
			{
                if (health <= 0.3f * healthMax || health == 1 )
                {
                    lowHealthEffect.Play();
                }
                else if (lowHealthEffect.isEmitting == true)
                {
                    lowHealthEffect.Stop();
                }
            }

            // If health reaches 0, we die
            if ( health <= 0 )    Die();

			// If the change in health is negative, then we get hurt
			if ( changeValue < 0 )
			{
				// If there is a hurt animation, play it
				if( GetComponent<Animation>() && hurtAnimation )
				{
					// Stop the animation
					//animation.Stop();
					
					// Play the animation
					GetComponent<Animation>().CrossFade(hurtAnimation.name);
				}

				// If there is a sound source and audio to play, play the sound from the audio source
				if ( soundSource && soundHurt )    soundSource.GetComponent<AudioSource>().PlayOneShot(soundHurt);

				// Wait for some time
				yield return new WaitForSeconds(hurtTime);

				// If there is a walk animation, play it
				if( GetComponent<Animation>() && moveAnimation )
				{
					// Stop the animation
					//animation.Stop();
					
					// Play the animation
					GetComponent<Animation>().CrossFade(moveAnimation.name);
				}
			}
		}

		/// <summary>
		/// Kills the enemy, and creates a death effect
		/// </summary>
		void Die()
		{
			// Create a death effect, if we have one assigned
			if( deathEffect )    Instantiate(deathEffect, transform.position, Quaternion.identity);

            // Increase the kill count in the game controller
            gameController.ChangeKillCount(1);

            // Give hit bonus for this target
            gameController.HitBonus(thisTransform);
            
			// Remove the object from the game
			Destroy(gameObject);
		}
	}
}