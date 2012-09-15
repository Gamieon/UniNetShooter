using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// This component manages application-specific server work. When you host
/// a game, the input director doesn't care about who is already in the game
/// or banned IP addresses; but this component does.
/// </summary>
public class Server : MonoBehaviour 
{
	#region Member Variables
	
	/// <summary>
	/// The input director that we use to communicate with the world
	/// </summary>
	protected InputDirector inputDirector;		
	
	/// <summary>
	/// The list of players in the game 
	/// </summary>
	public Dictionary<string, PlayerAttributes> playerList; // Public for debugging purposes

	/// <summary>
	/// The list of banned IP addresses
	/// </summary>
	protected Dictionary<string,bool> bannedIPAddresses;
	
	#endregion
	
	#region Properties
		
	/// <summary>
	/// Returns the number of players
	/// </summary>
	public int PlayerCount { get { return playerList.Count; } }
	
	/// <summary>
	/// Gets the player.
	/// </summary>
	/// <returns>
	/// The player.
	/// </returns>
	/// <param name='playerID'>
	/// Player ID.
	/// </param>
	public PlayerAttributes GetPlayer(string playerID) { return playerList[playerID]; }
	
	/// <summary>
	/// Determines whether a specified ID address is spanned.
	/// </summary>
	/// <returns>
	/// <c>true</c> if the IP address is banned; otherwise, <c>false</c>.
	/// </returns>
	/// <param name='IPAddress'>
	/// The IP address.
	/// </param>
	public bool IsBanned(string IPAddress)
	{
		return bannedIPAddresses.ContainsKey(IPAddress);
	}
	
	#endregion
	
	/// <summary>
	/// The one and only server. This will exist throughout the application's lifetime.
	/// </summary>
	static private Server _server;
	
	// Returns the one and only server component. This component is used by clients to register 
	// with game servers, and used by game servers to track player lists. This component is
	// always attached to the input director.
	static public Server Get()
	{
		if (null == _server)
		{
			InputDirector inputDirector = InputDirector.Get();
			if (null != inputDirector)
			{
				_server = InputDirector.Get().gameObject.GetComponent<Server>();
			}
		}
		return _server;
	}
		
	// Use this for initialization
	void Start()
	{
		playerList = new Dictionary<string, PlayerAttributes>();
		bannedIPAddresses = new Dictionary<string, bool>();
		inputDirector = GetComponent<InputDirector>();
	}
		
	// This is called by clients and servers alike to register with the master player
	// list, and to get an updated list from the main server.
	public void Register()
	{
		if (inputDirector.GetMode() == InputDirector.InputTransportMode.Server) 
		{
			// If this is called by the server, then it must mean the game is just starting
			// and they are the only player in it. So, we need to clear the player list. The
			// calling client component is responsible for acting like registration was successful.
			playerList.Clear();
		}
		else
		{
			// If we're a client, send a message to the server that we want to register. We will get our
			// network view ID, as it is on the server, back in a response message.
			networkView.RPC("OnServerRegister", RPCMode.Server, VersionDirector.GetVersion());
		}
	}
		
	// This is called by the server when a player is disconnected and we want to
	// unregister the player from the server list.
	public void Unregister(NetworkPlayer p)
	{
		// Tell everyone, including ourselves, that the client left. We don't need to
		// buffer this because when the client disconnected, we removed all its RPC
		// buffer entries. New players that come in later will never know that this
		// player existed.
		networkView.RPC("OnPlayerUnregistered", RPCMode.All, p.ToString());
	}
	
	// This is called by the server to kick a player
	public void Kick(string ID, bool ban)
	{
		if (inputDirector.IsHosting() && inputDirector.IsNetworking())
		{
			string IPAddress = inputDirector.GetIPAddress(ID);
			inputDirector.DisconnectClientFromServer(ID);
			if (ban) {
				bannedIPAddresses.Add(IPAddress, true);
				ConsoleDirector.Log("Banned player " + ID + " with address " + IPAddress);
			} else {
				ConsoleDirector.Log("Kicked player " + ID);
			}
		}
	}
	
	// Called on a server when a player disconnects from the server
	void OnPlayerDisconnectedFromServer(NetworkPlayer player)
	{
		// Unregister the player with the server
		Unregister(player);
	}	
	
	// This message is sent from a client to a server. This includes the version of the game
	[RPC]
	void OnServerRegister(string requestedVersion, NetworkMessageInfo info)
	{
		Debug.Log("OnServerRegister called from player " + info.sender.ToString() + " with version " + requestedVersion);
		// This is where games can do per-game authentication with players before
		// letting them actually play. Here, we:
		//
		// - Ensure the versions are consistent
		// - Ensure the player is not banned
		//
		// If all is well, reply to the player with their new ID; make them responsible
		// for broadcasting their presence to everyone so that it's their network
		// view ID that gets put into the RPC buffer.
		if (VersionDirector.GetVersion() != requestedVersion) {
			networkView.RPC("OnServerRegistrationFailed", info.sender, "Version mismatch");
		} else if (IsBanned(info.sender.ipAddress)) {
			networkView.RPC("OnServerRegistrationFailed", info.sender, "You are banned");
		} else {
			// Inform the sender that the registration was successful
			networkView.RPC("OnRegisteredWithServer", info.sender, info.sender.ToString());
		}
	}
	
	// This message is sent from a client or server to everyone to make their presence known to
	// all in the game. This is a buffered call so that incoming players can seamlessly get the
	// player list.
	[RPC]
	void OnPlayerRegistered(string ID, string playerName)
	{
		Debug.Log("OnPlayerRegistered called. Adding " + playerName + " (" + ID + ") to the list");
	
		// Add the player to our list
		PlayerAttributes a = new PlayerAttributes();
		a.ID = ID;
		a.PlayerName = playerName;
		playerList.Add(ID, a);
		
		// Log the connection
		ConsoleDirector.Log(playerName + " has joined the game.");
	}	
	
	// This message is sent from the server to everyone to make it known that a player has left
	// the game.
	[RPC]
	void OnPlayerUnregistered(string ID)
	{
		// Remove the player from the list
		PlayerAttributes e;
		if (playerList.TryGetValue(ID, out e))
		{
			ConsoleDirector.Log(e.PlayerName + " has left the game.");
			playerList.Remove(ID);
		}
	}	
}
