using UnityEngine;
using System.Collections;

namespace RobotRush
{
	/// <summary>
	/// This script makes gibs explode from a center point. Gibs are parts of an object that fly off it when it dies ( ex: a decapitated head )
	/// </summary>
	public class RRGExplodeGibs : MonoBehaviour 
	{
		internal Transform thisTransform;

		[Tooltip("A list of all the gibs that will be thrown")]
		public Transform[] gibObjects;

		[Tooltip("The center of explosion. Objects will be thrown away from this point")]
		public Vector3 explosionCenter;

		[Tooltip("The power of the explosion.")]
		public float explosionPower = 10;

		// Use this for initialization
		void Start() 
		{
			thisTransform = transform;

			// Go through all the gibs and through them away from the center point
			foreach ( Transform gibObject in gibObjects )
			{
				// Throw the gibs away from the center
				gibObject.GetComponent<Rigidbody>().AddExplosionForce( explosionPower, thisTransform.position + explosionCenter, 100);

				// Make the gib spin based on its speed
				gibObject.GetComponent<Rigidbody>().AddTorque( Vector3.one * 500);
			}
		}
	}
}
