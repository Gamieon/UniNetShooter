using UnityEngine;
using System.Collections;

/// <summary>
/// This class represents a projectile fired from a ship.
/// </summary>
public class Projectile : MonoBehaviour {
	
	public float lifeTime = 2.0f;
	public float energyCost = 10.0f;
	public float damage = 10.0f;

	InputDirector inputDirector;
	
	/// <summary>
	/// The time this projectile was fired
	/// </summary>
	float startTime;
	
	#region Unity Events
	
	// Use this for initialization
	void Start () 
	{
		// Cache the input director
		inputDirector = InputDirector.Get();
		// Set the start time
		startTime = Time.time;
	}
	
	// Update is called once per frame
	void Update () 
	{
		// Only hosts can destroy projectiles
		if (inputDirector.IsHosting())
		{
			if (Time.time > startTime + lifeTime) {
				inputDirector.DestroyObject(gameObject);
			}
		}
	}
	
	void OnTriggerEnter(Collider other)
	{
		if (inputDirector.IsHosting() // We're hosting the game
			&& other.tag == "Spaceship"  // The other object is a spaceship
			&& networkView.group != other.networkView.group // The other object did not come fromm us
			) 
		{
			// Tell the spaceship what happened
			Spaceship s = other.GetComponent<Spaceship>();
			s.OnHitByProjectile(this);
			// We always destroy ourselves on contact
			inputDirector.DestroyObject(gameObject);
		}
	}
	
	#endregion
}
