using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using RobotRush.Types;

namespace RobotRush
{
	public class RRGPlayer : MonoBehaviour 
	{
        internal Transform thisTransform;
        internal Vector3 targetPosition;
        internal RRGGameController gameController;

        [Tooltip("The object that is fired by the player and hits enemies")]
        public RRGShot shotObject;

		[Tooltip("The source from which shots are fired")]
		public Transform shotSource;

        [Tooltip("The muzzle effect when shooting")]
        public Transform shootEffect;

        [Tooltip("The fire rate of the player's weapon. How often it shoots")]
        public float fireRate = 0.2f;
        internal float fireRateCount = 0;

        [Tooltip("How fast the player rotates towards the target point")]
        public float rotateSpeed = 10;
        internal bool isRotating = false;

        // The player is dead now. When dead, the player can't move or shoot.
        internal bool isDead = false;

        [Tooltip("The enemy's health. If health reaches 0, this enemy dies")]
        public int health = 3;
        internal int healthMax;

        [Tooltip("How long the hurt effect is active when the enemy gets hurt")]
        public float hurtTime = 1;

        [Tooltip("The enemy is hurt. While hurt, the enemy has a unique animation")]
        internal bool isHurt = false;

        [Tooltip("The effect that appears on the enemy when it has low health, near death")]
        public ParticleSystem lowHealthEffect;

        [Tooltip("The effect that is created at the location of this object when it is destroyed")]
		public Transform deathEffect;

        [Tooltip("Various animation clips")]
		public AnimationClip spawnAnimation;
		public AnimationClip idleAnimation;
		public AnimationClip dieAnimation;

		[Tooltip("Various sounds and their source")]
		public AudioClip soundDie;
		public AudioClip soundShoot;

		[Tooltip("The source from which sounds are played")]
		public string soundSourceTag = "GameController";
		internal GameObject soundSource;

		[Tooltip("A random range for the pitch of the audio source, to make the sound more varied")]
		public Vector2 pitchRange = new Vector2( 0.9f, 1.1f);

        internal int index = 0;


		// Use this for initialization
		void Start() 
		{

			thisTransform = transform;

            if (gameController == null) gameController = GameObject.FindObjectOfType<RRGGameController>();
            
            // Add all the needed animation clips if they are missing from the animation component.
            if ( spawnAnimation && GetComponent<Animation>().GetClip(spawnAnimation.name) == null )    GetComponent<Animation>().AddClip( spawnAnimation, spawnAnimation.name);
			if ( idleAnimation && GetComponent<Animation>().GetClip(idleAnimation.name) == null )    GetComponent<Animation>().AddClip( idleAnimation, idleAnimation.name);
			if ( dieAnimation && GetComponent<Animation>().GetClip(dieAnimation.name) == null )    GetComponent<Animation>().AddClip( dieAnimation, dieAnimation.name);

			//Assign the sound source for easier access
			if ( GameObject.FindGameObjectWithTag(soundSourceTag) )    soundSource = GameObject.FindGameObjectWithTag(soundSourceTag);

            // Set the maximum health value of the enemy
            healthMax = health;

            // 
            if ( gameController && gameController.healthCanvas )
            {
                for ( index = 0; index < healthMax - 1; index++)
                {
                    Transform newIcon = Instantiate(gameController.healthCanvas.Find("Icon")) as Transform;

                    newIcon.SetParent(gameController.healthCanvas);

                    newIcon.localScale = Vector3.one;
                    //if (gameCanvas) Destroy(gameCanvas.gameObject);

                }
            }

            // Set the initial health 
            StartCoroutine("ChangeHealth", 0);

        }

        void Update() 
		{
			// If no other animation is playing, play the idle animation
			if ( GetComponent<Animation>() && !GetComponent<Animation>().isPlaying && idleAnimation )
			{
				GetComponent<Animation>().Play(idleAnimation.name); 
			}

            // Make the player look at the target position
            //thisTransform.LookAt(targetPosition);

            // Move the player towards the target position
            //thisTransform.position = Vector3.MoveTowards(thisTransform.position, targetPosition, moveSpeed * speedMultiplier * Time.deltaTime);

            // Move our position a step closer to the target.

            if ( isRotating == true && Vector3.Angle(thisTransform.forward, targetPosition - thisTransform.position) > rotateSpeed * Time.deltaTime )
            {
                thisTransform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, targetPosition - transform.position, rotateSpeed * Time.deltaTime, 0.0f));
            }
            else if ( isRotating == true )
            {
                isRotating = false;

                thisTransform.LookAt(targetPosition);

                // If we must rotate to the target position before shooting, shoot when we reach it, now!
                if (gameController.rotateBeforeShooting == true) Shoot();

            }

            if (fireRateCount > 0) fireRateCount -= Time.deltaTime;


        }

        /// <summary>
		/// Sets the target position for the player to move towards.
		/// </summary>
		/// <param name="targetValue">Target position</param>
		public void SetTarget(Vector3 targetValue)
        {
            isRotating = true;

            targetPosition = targetValue;
        }

        
        /// <summary>
        /// Shoot a bullet of a type. Each type of shot can only hit one enemy type
        /// </summary>
        public void Shoot()
		{
			if ( isDead == false && shotObject && fireRateCount <= 0 )
			{
				// Create a new shot at the position of the mouse/tap
				RRGShot newShot = Instantiate( shotObject ) as RRGShot;

                // Set the position of the shot at the shot source object position, or else at the player position
                if ( shotSource )    newShot.transform.position = shotSource.position;
				else    newShot.transform.position = thisTransform.position;

                //newShot.transform.LookAt(new Vector3(targetPosition.x, shotSource.position.y, targetPosition.z));
                newShot.transform.rotation = thisTransform.rotation;

                if (shootEffect) Instantiate(shootEffect, newShot.transform.position, newShot.transform.rotation);

                // Reset the fire rate
                fireRateCount = fireRate;
            }
        }

        /// <summary>
        /// Changes the health of this enemy. If health reaches 0, it dies
        /// </summary>
        /// <param name="changeValue">Change value.</param>
        IEnumerator ChangeHealth(int changeValue)
        {
            health += changeValue;

            // If health is low, show the low health effect
            if (lowHealthEffect)
            {
                if (health <= 0.3f * healthMax || health == 1)
                {
                    lowHealthEffect.Play();
                }
                else if (lowHealthEffect.isEmitting == true)
                {
                    lowHealthEffect.Stop();
                }
            }

            // Update the health icons
            if (gameController)
            {
                for (int index = 0; index < gameController.healthCanvas.childCount; index++)
                {

                    if (index >= health) gameController.healthCanvas.GetChild(index).GetComponent<Image>().color = Color.gray;
                    else gameController.healthCanvas.GetChild(index).GetComponent<Image>().color = Color.white;
                }
            }

            // If health reaches 0, we die
            if (health <= 0) StartCoroutine("Die");

            // If the change in health is negative, then we get hurt
            if (changeValue < 0)
            {
                // Wait for some time
                yield return new WaitForSeconds(hurtTime);
                
            }
        }

        /// <summary>
        /// Kills the object, and creates a death effect
        /// </summary>
        IEnumerator Die()
		{
			if ( isDead == false )
			{
                isDead = true;

                gameController.SendMessage("GameOver", 1.5f);

				// If there is a sound source and audio to play, play the sound from the audio source
				if ( soundSource && soundDie )    soundSource.GetComponent<AudioSource>().PlayOneShot(soundDie);

				if ( GetComponent<Animation>() && dieAnimation )
				{
					// Play the death animation
					GetComponent<Animation>().Play(dieAnimation.name);

					// Wait until the end of the animation
					yield return new WaitForSeconds(dieAnimation.length);
				}

				// If there is a death effect, create it at the position of the player
				if( deathEffect )    Instantiate(deathEffect, transform.position, Quaternion.identity);

				// Remove the object from the game
				Destroy(gameObject);
			}
		}

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.red;

            Gizmos.DrawSphere(targetPosition, 0.2f);
        }
    }
}
