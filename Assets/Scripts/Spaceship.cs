using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// This class represents a spaceship on the playfield. Although there is only one per player,
/// this is not to be confused with the Player component itself.
/// </summary>
public class Spaceship : MonoBehaviour
{	
	[HideInInspector]
	public float acceleration = 0;
	[HideInInspector]
	public float torque = 0;
		
	InputDirector inputDirector;
	Transform t;
	Rigidbody rb;
	
	/// <summary>
	/// This is assigned by OnSetSpaceshipAttributes. We only use it for debugging
	/// in Unity; by looking at this value in the inspector, we can be sure of the
	/// owning player for this ship.
	/// </summary>
	public string playerID;
	
	public float maxAcceleration = 4000;
	public float maxTorque = 1500;
	
	public float maxVelocity = 100;
	public float sqrMaxVelocity = 100 * 100;

	public float maxEnergy = 1000;
	public float energyRestoreRate = 50;

	public GameObject bulletPrefab;
	
	public Transform shipTransform;

	public bool Initialized { get { return (null != playerID); } }
	
	public float MaxAcceleration { get { return maxAcceleration; } }
	public float MaxTorque { get { return maxTorque; } }
	
	public float currentEnergy = 1000; // Public for debugging only
	public float CurrentEnergy { get { return currentEnergy; } }
	
	
	#region Unity Events
	
	/// <summary>
	/// Ensure that currentEnergy stays in sync with the other players. This is one
	/// abstraction that cannot be hidden with an underlying script.
	/// </summary>
	/// <param name='stream'>
	/// Stream.
	/// </param>
	/// <param name='info'>
	/// Info.
	/// </param>
	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info) 
	{
        if (stream.isWriting) {
            float energy = currentEnergy;
            stream.Serialize(ref energy);
        } else {
			float energy = 0;
            stream.Serialize(ref energy);
            currentEnergy = energy;
        }
    }
	
	// Use this for initialization
	void Start () 
	{
		inputDirector = InputDirector.Get();
		t = gameObject.transform; // Cache the transform
		rb = gameObject.rigidbody; // Cache the rigidbody
		rb.angularDrag = 5.0f;
	}
	
	void Update ()
	{
		float newEnergy = currentEnergy + energyRestoreRate * Time.deltaTime;
		if (newEnergy > maxEnergy) {
			newEnergy = maxEnergy;	
		}
		if (currentEnergy < newEnergy)
		{
			currentEnergy = newEnergy;
		}
	}

	void FixedUpdate ()
	{
		// Linear movement
		if (acceleration != 0) {
			rb.AddForce(shipTransform.forward * rigidbody.mass * acceleration * Time.fixedDeltaTime);	
		}
		
		// No drag...but in case we ever change our minds...
		//float idealDrag = maxAcceleration / terminalVelocity;
		//rigidbody.drag = idealDrag / ( idealDrag * Time.fixedDeltaTime + 1 );	
		
		// Angular movement
		if (torque != 0) {
			rigidbody.AddTorque(Vector3.up * rigidbody.mass * torque * Time.fixedDeltaTime);
		}
		
		// Cap velocity
		Vector3 vel = rb.velocity;
		if (vel.sqrMagnitude > sqrMaxVelocity) {
			rb.velocity = vel.normalized * maxVelocity;	
		}
	}
	
	#endregion
	
	#region Events
	
	/// <summary>
	/// This message is sent by a Projectile object when it collides with this object
	/// </summary>
	/// <param name='p'>
	/// The projectile
	/// </param>
	public void OnHitByProjectile(Projectile p)
	{
		// Reduce our energy by the projectile damage amount
		currentEnergy = currentEnergy - p.damage;
		if (currentEnergy < 0) {
			Debug.Log("Boom!");	
			// TODO: Forward this event to the rules component for further game-rules-level processing		
		}
		// TODO: Forward this event to the rules component for further game-rules-level processing		
	}
	
	#endregion
	
	#region RPCs
	
	[RPC]
	void OnSetSpaceshipAttributes(string owningPlayerID, string name)
	{	
		Debug.Log("in OnSetSpaceshipAttributes: " + owningPlayerID + " " + name);
		playerID = owningPlayerID;
		gameObject.name = name;
	}	
	
	#endregion
	
	/// <summary>
	/// This function is called by the Player class to fire a projectile.
	/// </summary>
	public void Fire()
	{
		Projectile p = bulletPrefab.GetComponent<Projectile>();
		if (currentEnergy > p.energyCost)
		{
			GameObject bullet = (GameObject)inputDirector.InstantiateObject(bulletPrefab, t.position + shipTransform.forward * 3.0f, bulletPrefab.transform.rotation, networkView.group);
			bullet.rigidbody.AddForce(shipTransform.forward * bullet.rigidbody.mass * 4000.0f);
			currentEnergy -= p.energyCost;
		}
	}
	
}
