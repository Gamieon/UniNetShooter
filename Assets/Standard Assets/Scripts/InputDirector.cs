using UnityEngine;
using System.Collections;

/// <summary>
/// The input director is an object that persists throughout the lifetime of this Unity application.
/// Its purpose is to hide the abstraction of the application being a local or a network game from
/// areas of the application which should not care:
/// 
/// Benefits:
/// 
/// - When creating or destroying objects, the application does not need to care about the particulars
/// of ensuring the action is propogated throughout the network.
/// 
/// - When sending messages to objects, the application does not need to care about the particulars
/// of ensuring the action is propogated throughout the network.
/// 
/// - MonoBehavior events like players connecting and disconnecting are all managed by this one object,
/// and can be rebroadcast to other scripts in the same game object that the InputDirector is attached to.
/// 
/// </summary>
[RequireComponent (typeof (NetworkView))]
[RequireComponent (typeof (MasterServerDirector))]
[RequireComponent (typeof (Server))]
public class InputDirector : MonoBehaviour 
{
	/// <summary>
	/// This describes the state of the input director and the game.
	/// </summary>
	public enum InputTransportMode 
	{ 
		/// <summary>
		/// Nothing is going on. This is true when we're in the main menu.
		/// </summary>
		Idle, 
		
		/// <summary>
		/// A local instance of the game is in progress; no network activity
		/// will be taking place.
		/// </summary>
		Local, 
		
		/// <summary>
		/// This instance of the game is acting as the host
		/// </summary>
		Server, 
		
		/// <summary>
		/// This instance of the game is connecting to a host
		/// </summary>
		Connecting, 
		
		/// <summary>
		/// This instance of the game is connected to a network host
		/// </summary>
		Client 
	
	}
	
	/// <summary>
	/// This describes the type of server being hosted
	/// </summary>
	public enum HostServerType 
	{ 
		/// <summary>
		/// The server is not being advertised to outside players who are looking for games.
		/// </summary>
		Private, 
		
		/// <summary>
		/// The server is being advertised, but only on the LAN. This should be used for
		/// LAN parties or when all the players are on the same network.
		/// </summary>
		LAN,
		
		/// <summary>
		/// This is a public game open to everyone on the Internet.
		/// </summary>
		Public 
	};
	
	/// <summary>
	/// The master server director which is responsible for acquiring online game listings
	/// </summary>
	private MasterServerDirector masterServerDirector;	
	/// <summary>
	/// The current state of the input director
	/// </summary>
	private InputTransportMode mode = InputTransportMode.Idle;
	/// <summary>
	/// The prefix for the next level to load (prevents network messages from one 
	/// level trickling into another)
	/// </summary>
	private int nextLevelPrefix = 0; 

	/// <summary>
	/// True if our input transport mode is Server and we're not actually engaging in the game
	/// (but we still need to run it)
	/// </summary>
	private bool isDedicatedServer; 
	/// <summary>
	/// The state of how we interact with the master game-search server
	/// </summary>
	private HostServerType gameHostType;
	
	/// <summary>
	/// The type name of the game; usually the title followed by version (e.g. Vengeance2.3)
	/// </summary>
	private string gameTypeName;
	/// <summary>
	/// The name of the game as it appears in game searches
	/// </summary>
	private string gameName;
	/// <summary>
	/// A description of the game as it appears in game searches
	/// </summary>
	private string gameComment;
	
	/// <summary>
	/// The one and only input director. This will exist throughout the application's lifetime.
	/// </summary>
	static private InputDirector _inputDirector;
		
	/// <summary>
	/// Returns the input director in the scene. If it does not exist, it is created.
	/// The input director object persists through all scenes.
	/// </summary>
	static public InputDirector Create()
	{
		if (null == _inputDirector) {
			GameObject o = new GameObject();
			o.name = "InputDirector";
			// Ensure this object is never destroyed
			DontDestroyOnLoad(o);
			// Cache the director
			_inputDirector = o.AddComponent<InputDirector>();
		}
		return _inputDirector;
	}	
	
	/// <summary>
	/// Returns the input director in the scene. If it does not exist, it is created.
	/// The input director object persists through all scenes.
	/// </summary>
	static public InputDirector Get()
	{
		return _inputDirector;
	}	
	
	#region Properties
	
	/// <summary>
	/// Determines whether this instance is hosting a game.
	/// </summary>
	/// <returns>
	/// <c>true</c> if this instance is hosting a game; otherwise, <c>false</c>.
	/// </returns>
	public bool IsHosting()
	{
		bool result = false;
		// If we are in a local one-player game (not on the internet), or are the server of an
		// internet game, then we consider ourselves the host.
		if (InputTransportMode.Local == mode || InputTransportMode.Server == mode)
		{
			result = true;
		}
		return result;
	}
	
	/// <summary>
	/// Determines whether this instance is network-enabled.
	/// </summary>
	/// <returns>
	/// <c>true</c> if this instance is network-enabled; otherwise, <c>false</c>.
	/// </returns>
	public bool IsNetworking()
	{
		bool result = true;
		// Simple test. We're always networking unless we're a local one-player game.
		if (InputTransportMode.Local == mode)
		{
			result = false;
		}
		return result;
	}
	
	/// <summary>
	/// Determines whether the specified object belongs to our instance of the game.
	/// </summary>
	/// <returns>
	/// <c>true</c> if this game object belongs to us for controlling. This only applies to objects with networkView components being used.
	/// </returns>
	/// <param name='o'>
	/// If set to <c>true</c> o.
	/// </param>
	public bool IsOurs(GameObject o)
	{
		if (IsNetworking()) {
			return o.networkView.isMine;
		} else {
			// If we're not networked, we must be hosting a one-player game...so the object o is always ours.
			return true;
		}
	}
	
	/// <summary>
	/// Determines whether this instance is running a dedicated server.
	/// </summary>
	/// <returns>
	/// <c>true</c> if this instance is a dedicated server; otherwise, <c>false</c>.
	/// </returns>
	public bool IsDedicatedServer()
	{
		if (InputTransportMode.Server == mode) {
			return isDedicatedServer;
		} else {
			// If we're not a server, we're not dedicated, period.
			return false;
		}
	}
		
	/// <summary>
	/// Gets the input transport mode. Refer to the enumeration for possible values.
	/// </summary>
	/// <returns>
	/// The mode.
	/// </returns>
	public InputTransportMode GetMode()
	{
		return mode;
	}
	
	// This function changes the mode of the input director
	public void SetMode(InputTransportMode value)
	{
		// Unhost if we're currently hosting but the input mode is changing
		if (InputTransportMode.Server == mode && value != mode) {
			UnhostServer();
		}
	
		// TODO: Check the existing mode and throw exceptions if not supported. 
		// For example, going from Idle to Client without Connecting first shouldn't
		// be possible.
		SetModeInternal(value);
	}	
	
	/// <summary>
	/// Gets the IP address from a given client ID
	/// </summary>
	/// <returns>
	/// The IP address.
	/// </returns>
	/// <param name='ID'>
	/// I.
	/// </param>
	public string GetIPAddress(string ID)
	{
		foreach (NetworkPlayer p in Network.connections) {
			if (p.ToString() == ID) {
				return p.ipAddress;
			}
		}
		return "";
	}
	
	#endregion
	
	#region Internal methods

	private void SetModeInternal(InputTransportMode value)
	{
		mode = value;
		Debug.Log("InputDirector mode is now " + value.ToString());
	}
	
	#endregion
	
	#region Unity Events

	public void Start()
	{
		// First, add the ability for this object to communicate with a master game server
		// to find other players online.
		masterServerDirector = GetComponent<MasterServerDirector>();
		// Now turn off state synchronization because it does not apply to this object.
		// See http://docs.unity3d.com/Documentation/Components/net-StateSynchronization.html for what
		// it should apply to.
		networkView.stateSynchronization = NetworkStateSynchronization.Off;
	}
	
	/// <summary>
	/// This is called by Unity when the network server has initialized.
	/// </summary>
	void OnServerInitialized()
	{
	    Debug.Log("Server initialized for host type " + gameHostType.ToString() + "." );
	    // Update our mode
	    SetModeInternal(InputTransportMode.Server);
	    // Register the game on Unity's master server
	    if (HostServerType.Private != gameHostType) {
	   		masterServerDirector.RegisterHost(gameTypeName, gameName, gameComment, isDedicatedServer, (HostServerType.Public == gameHostType) ? true : false);
	   	}
	    // Let all components know the server is initialized
	    gameObject.SendMessage("OnHostServerComplete", NetworkConnectionError.NoError, SendMessageOptions.DontRequireReceiver);
	}
	
	/// <summary>
	/// This is called by Unity when a player connects to a server. Only servers get this message.
	/// </summary>
	/// <param name='player'>
	/// The player who connected.
	/// </param>
	void OnPlayerConnected(NetworkPlayer player)
	{
		// player.ToString() is the unique ID of the player. When printing NetworkPlayer.ToString()
		// you will see a number, but we should not assume it will always be a number.
	    Debug.Log("Player " + player.ToString() + " connected from " + player.ipAddress + ":" + player.port + ".");
		// Let all components know a player connected
		gameObject.SendMessage("OnPlayerConnectedToServer", player, SendMessageOptions.DontRequireReceiver);
	    // TODO: Anything else?		
	}
	
	/// <summary>
	/// This is called by Unity when a player disconnects from a server. Only servers get this message.
	/// </summary>
	/// <param name='player'>
	/// The player who disconnected.
	/// </param>
	void OnPlayerDisconnected(NetworkPlayer player)
	{
		// player.ToString() is the unique ID of the player. When printing NetworkPlayer.ToString()
		// you will see a number, but we should not assume it will always be a number.
	    Debug.Log("Player " + player.ToString() + " disconnected.");
	    		
		// Let all components know a player disconnected.
		gameObject.SendMessage("OnPlayerDisconnectedFromServer", player, SendMessageOptions.DontRequireReceiver);
		
	    // Delete all player objects and RPC buffer entries. This will also destroy them for all other players.
	    Network.RemoveRPCs(player);
	    Network.DestroyPlayerObjects(player);
	    		
	    // TODO: Anything else?
	}
	
	/// <summary>
	/// This is called by Unity when we, a client, failed to connect to a remote server.
	/// </summary>
	/// <param name='error'>
	/// The error.
	/// </param>
	void OnFailedToConnect(NetworkConnectionError error)
	{
		Debug.Log("Failed to connect to server!");
		SetModeInternal(InputTransportMode.Idle);
		// Shut down our connection
		DisconnectFromServer();
		// Let all components know what happened
		SendMessage("OnConnectToServerComplete", error, SendMessageOptions.DontRequireReceiver);
	}
	
	/// <summary>
	/// This is called by Unity when we, a client, connected to the remote server. Only clients get this message.
	/// </summary>
	void OnConnectedToServer()
	{
		// Update our mode
		SetModeInternal(InputTransportMode.Client);
		// Let all components know what happened. We don't need to do anything here.
		SendMessage("OnConnectToServerComplete", NetworkConnectionError.NoError, SendMessageOptions.DontRequireReceiver);	
	}	
	
	/// <summary>
	/// This is called by Unity when we, a client, were disconnected from the remote server.
	/// It can also be called when the remote server is shut down. Only clients get this message.
	/// </summary>
	/// <param name='disconnectionMode'>
	/// Disconnection mode.
	/// </param>
	void OnDisconnectedFromServer(NetworkDisconnection disconnectionMode)
	{
		Debug.Log("Disconnected from the server.");
		SetModeInternal(InputTransportMode.Idle);
	    // TODO: Better handling per
	    // http://unity3d.com/support/documentation/ScriptReference/Network.OnDisconnectedFromServer.html
	    // Let all components know what happened
	    SendMessage("OnDisconnectionFromServer", disconnectionMode, SendMessageOptions.DontRequireReceiver);
	}
	
	#endregion	

	#region Connection methods	

	/// <summary>
	/// This function will host a server. The caller specifies the max number of players
	/// and the listening port. This will notify the notification object when the game is
	/// hosted.
	/// </summary>
	/// <param name='connections'>
	/// The maximum number of connections allowed. This is typically the maximum player count.
	/// </param>
	/// <param name='listenPort'>
	/// Port we listen for connections on
	/// </param>
	/// <param name='dedicatedServer'>
	/// True if this is a dedicated server.
	/// </param>
	/// <param name='password'>
	/// Server password.
	/// </param>
	/// <param name='hostType'>
	/// Host scope (see the enumeration for possible values)
	/// </param>
	/// <param name='typeName'>
	/// The name of the type of game (typically the game's title followed by version number)
	/// </param>
	/// <param name='name'>
	/// The name of the hosted game (e.g. "Ted's game")
	/// </param>
	/// <param name='comment'>
	/// A description of the game
	/// </param>
	public void HostServer(int connections, int listenPort, bool dedicatedServer,
		string password, HostServerType hostType, string typeName, string name, string comment)
	{
		if (InputTransportMode.Idle == mode) 
		{
			bool useNat = !Network.HavePublicAddress();
			isDedicatedServer = dedicatedServer;
			gameHostType = hostType; 
			gameTypeName = typeName;
			gameName = name;
			gameComment = comment;
			// Although we're hosting, set our status to Connecting because doing anything else
			// right now would be invalid.
			SetModeInternal(InputTransportMode.Connecting);
			Network.InitializeSecurity();
			Network.incomingPassword = password;
			NetworkConnectionError result = Network.InitializeServer(connections, listenPort, useNat);
			if (NetworkConnectionError.NoError == result)
			{
				// This might not mean the server is initialized; it could mean it's initializing. We want
				// to wait for a OnServerInitialized message.
				Debug.Log("Network.InitializeServer was successful");
			}
			else {
				// Unity doesn't send messages for failing to initialize the server, so we'll send one.
				SendMessage("OnHostServerComplete", result);
				// TODO: Throw an exception?
			}
		}
		else {
			// TODO: Throw an exception
		}
	}
	
	/// <summary>
	/// This function will unhost a server
	/// </summary>
	public void UnhostServer()
	{
		if (InputTransportMode.Server == mode) {
			// Remove from the Unity master server game list
			masterServerDirector.UnregisterHost();	
			// Now disconnect
			Network.Disconnect();
			// Update our mode
			SetModeInternal(InputTransportMode.Idle);
		}
		else {
			// TODO: Throw an exception
		}	
	}
	
	/// <summary>
	/// This function will have the game instance attempt to connect to a server
	/// </summary>
	/// <param name='IP'>
	/// The server's network IP address
	/// </param>
	/// <param name='remotePort'>
	/// The port of the server listening for connections
	/// </param>
	/// <param name='password'>
	/// Server password.
	/// </param>
	public void ConnectToServer(string IP, int remotePort, string password)
	{
		// Only do this if we're idle. If we were connected to a server, we should have disconnected by now.
		if (InputTransportMode.Idle == mode) 
		{
			SetModeInternal(InputTransportMode.Connecting);
			NetworkConnectionError result = Network.Connect(IP, remotePort, password);
			if (NetworkConnectionError.NoError == result)
			{
				// This might not mean the connection is established; it could mean it's initializing. We want
				// to wait for a OnConnectedToServer message.
				Debug.Log("Network.Connect was successful.");
			}
			else {
				// TODO: Throw an exception?
			}		
		}
		else {
			// TODO: Throw an exception?
		}	
	}
	
	/// <summary>
	/// This function will disconnect us from the server if we're still connected
	/// </summary>
	public void DisconnectFromServer()
	{
		if (InputTransportMode.Connecting == mode || InputTransportMode.Client == mode)
		{
			Network.Disconnect();
		    // Update our mode
		    SetModeInternal(InputTransportMode.Idle);
		}
	}
	
	/// <summary>
	/// This function is called by the server to disconnect a client
	/// </summary>
	/// <param name='ID'>
	/// The client's ID.
	/// </param>
	public void DisconnectClientFromServer(string ID)
	{
		for (var i=0; i < Network.connections.Length; i++) {
			if (Network.connections[i].ToString() == ID) {
				Network.CloseConnection(Network.connections[i],true);
				return;
			}
		}
	}
	
	#endregion
	
	#region Server and Client Methods
	
	/// <summary>
	/// This function will create a new object in the game based on a prefab
	/// </summary>
	/// <returns>
	/// The created object.
	/// </returns>
	/// <param name='prefab'>
	/// The prefab from which to create the object.
	/// </param>
	/// <param name='position'>
	/// The position for the new object.
	/// </param>
	/// <param name='rotation'>
	/// The rotation for the new object.
	/// </param>
	/// <param name='group'>
	/// The network group to assign the object to. See the Unity documentation for more info. If you're
	/// not sure what to put here or don't plan on grouping network objects, just leave this value 0.
	/// </param>
	public GameObject InstantiateObject(GameObject prefab, Vector3 position, Quaternion rotation, int group)
	{
		GameObject result = null;
		switch (mode) {
			case InputTransportMode.Local:
				// Regular instantiation
				result = (GameObject)GameObject.Instantiate(prefab, position, rotation);
				break;
			case InputTransportMode.Server:
			case InputTransportMode.Client:
				// Network instantiation. This object will appear in everyone's world.
				result = (GameObject)Network.Instantiate(prefab, position, rotation, group);
				break;
			default:
				// Not supported
				// TODO: Throw an exception
				break;
		}
		return result;
	}
	
	/// <summary>
	/// This function will destroy a game object, regardless of whether this application owns the object.
	/// </summary>
	/// <param name='doomedObject'>
	/// Doomed object.
	/// </param>
	public void DestroyObject(GameObject doomedObject)
	{
		switch (mode) {
			case InputTransportMode.Local:
				// Regular destruction
				Destroy(doomedObject);
				break;
			case InputTransportMode.Server:
			case InputTransportMode.Client:
				// Network destruction
				// TODO: Figure out why When when destroying client objects do we still get messages
				// like "View ID AllocatedID: 50 not found during lookup. Strange behaviour may occur"?
				// Probably because I pulled the rug from right under the client's feet and they're trying
				// to send state information. I tried sending an RPC first, but Destroy seems to have a higher
				// priority and completes before my RPC gets out. Need to confirm the right way to destroy
				// objects that were instantiated by other players.
				Network.Destroy(doomedObject);
				break;
			default:
				// Not supported
				// TODO: Throw an exception
				break;
		}
	}
	
	/// <summary>
	/// This function will send a buffered command to an object. In a local game, a message
	/// will be sent. In a network game, an RPC call will be made. In a network game,
	/// the object should have been created with InputDirector.InstantiateObject()
	/// </summary>
	/// <param name='receiver'>
	/// The receiving object.
	/// </param>
	/// <param name='command'>
	/// The command string.
	/// </param>
	/// <param name='args'>
	/// Additional arguments.
	/// </param>
	public void SendBufferedCommand(GameObject receiver, string command, params object[] args)
	{
		switch (mode) {
			case InputTransportMode.Local:
				// Process the command locally. The receiver is required to handle the message.
				if (args.Length == 1) {
					receiver.SendMessage(command, args);
				} else {
					Debug.LogError("SendBufferedCommand called in a local session with multiple arguments!");
				}
				break;
			case InputTransportMode.Server:
			case InputTransportMode.Client:
				// Use an RPC command. This requires that the receiver has a network view.
				receiver.networkView.RPC(command, RPCMode.AllBuffered, args);
				break;
			default:
				// Not supported
				// TODO: Throw an exception
				break;
		}
	}	
		
	/// <summary>
	/// This function will send a non-buffered command to an object. In a local game, a message
	/// will be sent. In a network game, an RPC call will be made. In a network game,
	/// the object should have been created with InputDirector.InstantiateObject()
	/// </summary>
	/// <param name='receiver'>
	/// The receiving object.
	/// </param>
	/// <param name='command'>
	/// The command string.
	/// </param>
	/// <param name='args'>
	/// Additional arguments.
	/// </param>
	public void SendCommand(GameObject receiver, string command, params object[] args)	
	{
		switch (mode) {
			case InputTransportMode.Local:
				if (args.Length == 1) {
					receiver.SendMessage(command, args[0], SendMessageOptions.DontRequireReceiver);
				} else {
					Debug.LogError("SendCommand called in a local session with multiple arguments!");
				}
				break;			
			case InputTransportMode.Server:
			case InputTransportMode.Client:
				receiver.networkView.RPC(command, RPCMode.All, args);
				break;
			default:
				// Not supported
				break;			
		}		
	}
	
	/// <summary>
	/// This function will broadcast a buffered command to all clients and the originator.
	/// </summary>
	/// <param name='command'>
	/// The command.
	/// </param>
	/// <param name='args'>
	/// Command arguments.
	/// </param>
	public void BroadcastBufferedCommand(string command, params object[] args)
	{
		switch (mode) {
			case InputTransportMode.Server:
			case InputTransportMode.Client:
				networkView.RPC(command, RPCMode.AllBuffered, args);
				break;
			default:
				// Not supported
				break;			
		}
	}
	
	/// <summary>
	/// This function will broadcast a non-buffered command to all clients and the originator.
	/// </summary>
	/// <param name='command'>
	/// The command.
	/// </param>
	/// <param name='args'>
	/// Command arguments.
	/// </param>
	public void BroadcastCommand(string command, params object[] args)
	{
		switch (mode) {
			case InputTransportMode.Server:
			case InputTransportMode.Client:
				networkView.RPC(command, RPCMode.All, args);
				break;
			default:
				// Not supported
				break;			
		}
	}
	
	#endregion
	
	#region Server Methods
	
	/// <summary>
	/// This function is called by us, the server, to change the current level during a network game
	/// </summary>
	/// <param name='level'>
	/// Level.
	/// </param>
	public void LoadScene(string level)
	{
		if (InputTransportMode.Local == mode) 
		{
			Application.LoadLevel(level);
		} 
		else if (InputTransportMode.Server == mode) 
		{
			// Send a buffered RPC to load the level so everyone who joins also gets the command
			networkView.RPC("OnLoadNetworkLevel", RPCMode.AllBuffered, level, nextLevelPrefix++);
		} 
		else {
			// TODO: Throw an exception?
		}
	}
	
	/// <summary>
	/// This function will send a non-buffered command to a single player. It is not
	/// designed to be used by individual clients.
	/// </summary>
	/// <param name='networkPlayerID'>
	/// Network player ID (should originally have been acquired from NetworkPlayer.ToString())
	/// </param>
	/// <param name='command'>
	/// Command.
	/// </param>
	/// <param name='args'>
	/// Arguments.
	/// </param>
	public void SendCommand(string networkPlayerID, string command, params object[] args)
	{
		switch (mode) {
			case InputTransportMode.Server:
				foreach (NetworkPlayer p in Network.connections) {
					if (p.ToString() == networkPlayerID) {
						networkView.RPC(command, p, args);
						return;
					}
				}
				// TODO: Throw an exception?			
				break;
			default:
				break;
		}
	}
	
	
	#endregion
	
	#region Client Methods
	
	/// <summary>
	/// This function is called by us, the client, to send a non-buffered command to the server
	/// </summary>
	/// <param name='command'>
	/// The command string.
	/// </param>
	public void SendCommandToServer(string command)
	{
		switch (mode) {
			case InputTransportMode.Client:
				networkView.RPC(command, RPCMode.Server);
				break;
			default:
				// TODO: Throw an exception?
				break;
		}
	}
	
	#endregion

}
