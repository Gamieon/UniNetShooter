using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// This class represents the user's existence in the application. All non-application-specific player management
/// happens here. This component must be a subclass of the actual application-specific user component.
/// </summary>
public abstract class Client : MonoBehaviour 
{	
	/// <summary>
	/// The input director that we use to communicate with the world
	/// </summary>
	protected InputDirector inputDirector;	
	
	/// <summary>
	/// The server. Even if we are hosting, this object maintains the player list and
	/// is responsible for server "registration" management; that is, players formally
	/// requesting to join a game once connected.
	/// </summary>
	protected Server server;
	
	/// <summary>
	/// Our own unique player ID
	/// </summary>
	protected string selfID;
		
	#region Properties
	
	/// <summary>
	/// Returns your unique ID
	/// </summary>
	public string ID { get { return selfID; }  }
	
	#endregion
	
	#region Abstracts	
	
	/// <summary>
	/// This message is sent from a server to either itself or a client after they've registered 
	/// with the server. This is not a buffered call, and it's a one-time call per game instance.
	/// </summary>
	/// <param name='newID'>
	/// The new player ID
	/// </param>
	[RPC]
	public abstract void OnRegisteredWithServer(string newID);
	
	#endregion
	
	#region Unity Events
	
	// Use this for initialization
	void Awake () 
	{
		inputDirector = InputDirector.Get();
		server = Server.Get();
	}	
	
	void OnLevelWasLoaded(int level)
	{
		// Allow receiving data again
		Network.isMessageQueueRunning = true;
		// Now the level has been loaded and we can start sending out data to clients
		Network.SetSendingEnabled(0, true);	
		// Notify all objects that the level was loaded
		foreach (GameObject o in FindObjectsOfType(typeof(GameObject)))
			o.SendMessage("OnNetworkLoadedLevel", SendMessageOptions.DontRequireReceiver);				
	}
	
	#endregion
	
	#region Input Director and Application Events
	
	/// <summary>
	/// This message is sent by the Input Director component if this application
	/// has just put up a network host.
	/// </summary>
	/// <param name='error'>
	/// Error.
	/// </param>
	void OnHostServerComplete(NetworkConnectionError error)
	{
		// Now handle player stuff
		if (NetworkConnectionError.NoError == error) 
		{
			// Reset our own ID
			selfID = null;
			// Register with ourselves
			server.Register();
			// We know we're good to go, so lets just call this ourselves.
			OnRegisteredWithServer(networkView.owner.ToString());
		}
	}
		
	/// <summary>
	/// This message is sent by the Input Director component if this application
	/// has just connected to a server
	/// </summary>
	/// <param name='error'>
	/// Error.
	/// </param>
	void OnConnectToServerComplete(NetworkConnectionError error)
	{
		// Now handle player stuff
		if (NetworkConnectionError.NoError == error) 
		{
			// Reset our own ID
			selfID = null;
			// Register with the server. This will send out an RPC call.
			server.Register();
		}
	}
	
	/// <summary>
	/// Called on a client when the client is disconnected form the server
	/// </summary>
	/// <param name='disconnectionMode'>
	/// Disconnection mode.
	/// </param>
	void OnDisconnectionFromServer(NetworkDisconnection disconnectionMode)
	{
		selfID = null;
	}
	
	#endregion
	
	#region RPCs
	
	/// <summary>
	/// This message is sent from a server to a client if they were rejected by the server
	/// </summary>
	/// <param name='reason'>
	/// Reason.
	/// </param>
	[RPC]
	void OnServerRegistrationFailed(string reason)
	{
		// Disconnect
		inputDirector.DisconnectFromServer();
		// Now load the scene that shows the player why they can't connect
		ConnectionFailureSceneDirector.Initialize("Registration failed: " + reason, "Startup");
	}
		
	/// <summary>
	/// This message is sent from a server to a client to load a level
	/// </summary>
	/// <param name='level'>
	/// Level.
	/// </param>
	/// <param name='levelPrefix'>
	/// Level prefix.
	/// </param>
	[RPC]
	void OnLoadNetworkLevel(string level, int levelPrefix)
	{
		// http://unity3d.com/support/documentation/Components/net-NetworkLevelLoad.html
		Debug.Log("In OnLoadNetworkLevel");
	
		// There is no reason to send any more data over the network on the default channel,
		// because we are about to load the level, thus all those objects will get deleted anyway
		Network.SetSendingEnabled(0, false);	
	
		// We need to stop receiving because first the level must be loaded first.
		// Once the level is loaded, rpc's and other state update attached to objects in the level are allowed to fire
		Network.isMessageQueueRunning = false;
		
		// All network views loaded from a level will get a prefix into their NetworkViewID.
		// This will prevent old updates from clients leaking into a newly created scene.
		Network.SetLevelPrefix(levelPrefix);
		Application.LoadLevel(level);
	}
	
	#endregion
}
