using UnityEngine;
using System.Collections;

/// <summary>
/// This class represents the user directly interacting with this application. It has all the attributes
/// of the Client class, but also has game-specific traits. Player game elements are created from this
/// class as well.
/// </summary>
public class Player : Client 
{
	/// <summary>
	/// The one and only player (this is you). This will exist throughout the application's lifetime.
	/// </summary>
	static private Player _player;
	
	/// <summary>
	/// Create the one and only instance of you in this game.
	/// </summary>
	static public Player Create()
	{
		if (null == _player)
		{
			InputDirector inputDirector = InputDirector.Create();
			_player = inputDirector.gameObject.AddComponent<Player>();
			_player.inputDirector = inputDirector;
		}
		return _player;
	}	
	
	/// <summary>
	/// Get the only and only instance of you as a player in the game.
	/// </summary>
	static public Player Get()
	{
		return _player;
	}
	
	/// <summary>
	/// The input director cached for convenience
	/// </summary>
	InputDirector inputDirector;
	/// <summary>
	/// The self spaceship.
	/// </summary>
	Spaceship selfSpaceship;
	/// <summary>
	/// The reported game rules
	/// </summary>
	string serverGameRules;
	
	/// <summary>
	/// Gets a value indicating whether this instance is registered with server.
	/// </summary>
	/// <value>
	/// <c>true</c> if this instance is registered with server; otherwise, <c>false</c>.
	/// </value>
	bool IsRegisteredWithServer { get { return (null != selfID); } }	
	
	#region Unity Events

	// Update is called once per frame
	void Update () 
	{
		if (null != selfSpaceship)		
		{
			Camera.main.transform.localEulerAngles = new Vector3(90,-selfSpaceship.transform.localEulerAngles.y,0);
				
			if (Input.GetKeyDown(KeyCode.W)) {
				selfSpaceship.acceleration = selfSpaceship.MaxAcceleration;
			}
			if (Input.GetKeyUp(KeyCode.W)) {
				selfSpaceship.acceleration = 0;			
			}
			if (Input.GetKeyDown(KeyCode.S)) {
				selfSpaceship.acceleration = -selfSpaceship.MaxAcceleration;
			}
			if (Input.GetKeyUp(KeyCode.S)) {
				selfSpaceship.acceleration = 0;
			}
			if (Input.GetKeyDown(KeyCode.A)) {
				selfSpaceship.torque = -selfSpaceship.MaxTorque;
			}
			if (Input.GetKeyUp(KeyCode.A)) {
				selfSpaceship.torque = 0;
			}
			if (Input.GetKeyDown(KeyCode.D)) {
				selfSpaceship.torque = selfSpaceship.MaxTorque;
			}
			if (Input.GetKeyUp(KeyCode.D)) {
				selfSpaceship.torque = 0;
			}
			if (Input.GetKeyDown(KeyCode.Space)) {
				selfSpaceship.Fire();	
			}
		}
	}
	
	#endregion
	
	#region Input Director and Application Events
	
	/// <summary>
	/// This is called to spawn a spaceship in the game at the specified position
	/// </summary>
	void OnSpawnSpaceship(Vector3 pos)
	{
		Debug.Log("in OnSpawnSpaceship at " + pos.ToString() + " - selfID = " + selfID);
		// Create the cycle and assign its name and color
		GameObject spaceshipPrefab = (GameObject)Resources.Load("Prefabs/PlayerShip");
		GameObject selfSpaceshipObject = inputDirector.InstantiateObject(spaceshipPrefab, pos, spaceshipPrefab.transform.rotation, System.Convert.ToInt32(selfID));
		selfSpaceship = selfSpaceshipObject.GetComponent<Spaceship>();
		selfSpaceshipObject.networkView.RPC("OnSetSpaceshipAttributes", RPCMode.AllBuffered,
			selfID,	ConfigurationDirector.GetPlayerName() + "'s ship");
			
		// Now attach our camera to it.
		Camera.main.transform.parent = selfSpaceshipObject.transform;
		Camera.main.transform.localPosition = new Vector3(0,150,0);
	}
	
	/// <summary>
	/// Called on a client when the client is disconnected form the server
	/// </summary>
	/// <param name='disconnectionMode'>
	/// Disconnection mode.
	/// </param>
	void OnDisconnectionFromServer(NetworkDisconnection disconnectionMode)
	{
		// Load the scene that shows the player why they can't connect
		ConnectionFailureSceneDirector.Initialize("Lost connection to server", "Startup");
	}
	
	#endregion
	
	#region RPCs
	
	/// <summary>
	/// This message is sent from a server to either itself or a client after they've registered 
	/// with the server. This is not a buffered call, and it's a one-time call per game instance.
	/// </summary>
	/// <param name='newID'>
	/// The new player ID
	/// </param>	
	[RPC]
	public override void OnRegisteredWithServer(string newID)
	{
		Debug.Log("OnRegisteredWithServer called. New player ID is " + newID + ". Rules are " + serverGameRules);
		
		// Assign our new ID
		selfID = newID;
		
		// Now notify all players that we have successfully registered so that they know we exist.
		inputDirector.BroadcastBufferedCommand("OnPlayerRegistered", newID, ConfigurationDirector.GetPlayerName());
		
		// If this is the host, then we must enter the game now.
		if (inputDirector.IsHosting()) 
		{
			inputDirector.LoadScene("Game");
		}
		else
		{
			// Don't tell the game director until we're registered
			if (IsRegisteredWithServer && null != serverGameRules)
			{
				GameDirector.Get().SendMessage("OnGameRulesDefined", serverGameRules);
			}			
		}
	}
	
	// This is a buffered message sent from the server to all clients to tell them what
	// kind of game is being played. This must always be the first buffered RPC to be sent 
	// after a level has been loaded so that the players know the "rules" of the game.
	[RPC]
	void OnDefineGameRules(string gameRules)
	{
		serverGameRules = gameRules;
		// Don't tell the game director until we're registered
		if (IsRegisteredWithServer && null != serverGameRules)
		{
			GameDirector.Get().SendMessage("OnGameRulesDefined", serverGameRules);
		}
	}
		
	#endregion
}
