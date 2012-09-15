using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// The "master server" is the ivory tower where all servers of this game report to let
/// everyone know that this game is available and looking for players. This class manages
/// this application's communication with the master server; including functionality for
/// both searching and hosting.
/// </summary>
[RequireComponent (typeof (LANBroadcastService))]
public class MasterServerDirector : MonoBehaviour 
{
	/// <summary>
	/// Describes a game that was found when a search was done.
	/// </summary>
	public class FoundGame
	{
		/// <summary>
		/// The game's listening IP address.
		/// </summary>
		public string ipAddress;
		/// <summary>
		/// True if the game is a dedicated server
		/// </summary>
		public bool isDedicated;
		/// <summary>
		/// True if the game is on the LAN
		/// </summary>
		public bool isOnLAN;
		/// <summary>
		/// The number of players on the server
		/// </summary>
		public int playerCount;
		/// <summary>
		/// The max number of players allowed on the server
		/// </summary>
		public int maxPlayerCount;
		/// <summary>
		/// The ping object that measures latency
		/// </summary>
		public Ping ping;
	}
	
	/// <summary>
	/// This component enables LAN-based game searching
	/// </summary>
	LANBroadcastService lanBroadcastService;
	/// <summary>
	/// True if we are searching for LAN games
	/// </summary>
	bool isLANGameSearchEnabled;
	
	/// <summary>
	/// The list of all found WAN games (we use Unity's framework to populate this)
	/// </summary>
	List<FoundGame> foundWANGames;
	/// <summary>
	/// The list of all found LAN games (we use the LANBroadcastService component to populate this)
	/// </summary>
	List<FoundGame> foundLANGames;
	
	public int WANGameCount { get { return foundWANGames.Count; } }
	public int LANGameCount { get { return foundLANGames.Count; } }
	public FoundGame GetWANGameByIndex(int index)
	{
		return foundWANGames[index];
	}
	public FoundGame GetLANGameByIndex(int index)
	{
		return foundLANGames[index];
	}
		
	/// <summary>
	/// When hosting a game, we need to include special data in the comment to find things
	/// like whether the game is a dedicated server or not. This function, given the user
	/// input comment, will return the data formatted comment to give to the master server
	/// director.
	/// </summary>
	/// <returns>
	/// The formatted game comment (e.g. 'HTed's 24/7 server!')
	/// </returns>
	/// <param name='comment'>
	/// The original game comment (e.g. 'Ted's 24/7 server!')
	/// </param>
	/// <param name='dedicatedServer'>
	/// Dedicated server.
	/// </param>
	static string FormatGameComment(string comment, bool dedicatedServer)
	{
		return (dedicatedServer ? "D" : "H") + comment;
	}
		
	/// <summary>
	/// When hosting a game, we need to include special data in the comment to find things
	/// like whether the game is a dedicated server or not. This function, given a comment
	/// from a game search result from the master server director, will give us the comment
	/// to display to the user.
	/// </summary>
	/// <returns>
	/// The original game comment (e.g. 'Ted's 24/7 server!')
	/// </returns>
	/// <param name='comment'>
	/// The formatted game comment (e.g. 'HTed's 24/7 server!')
	/// </param>
	static string UnformatGameComment(string comment)
	{
		return comment.Substring(1,comment.Length-1);
	}
	
	/// <summary>
	/// Returns true if the game being hosted is a dedicated server
	/// </summary>
	/// <returns>
	/// <c>true</c> if this instance is dedicated server; otherwise, <c>false</c>.
	/// </returns>
	/// <param name='comment'>
	/// The game's formatted game comment.
	/// </param>
	static bool IsDedicatedServerByComment(string comment)
	{
		return (comment.Length > 0 && comment[0] == 'D') ? true : false;
	}
	
	static string IPFromStringArray(string[] value)
	{
		string result = "";
		foreach (string s in value) {
	        result = s + " ";
		}		
		return result;
	}
		
	#region Unity Events
	
	void Awake()
	{
		foundLANGames = new List<FoundGame>();
		lanBroadcastService	= gameObject.GetComponent<LANBroadcastService>();
		// Start looking for hosts on the LAN
		Debug.Log("Polling for LAN games on port " + ConfigurationDirector.GetMulticastPort());
	}
	
	void Update()
	{
		// Check for any new listings from the Unity master server
		HostData[] hostData = MasterServer.PollHostList();
		if (hostData.Length > 0)
		{
			foreach (HostData host in hostData)
			{
				FoundGame foundGame = new FoundGame();
				foundGame.ipAddress = IPFromStringArray(host.ip);
				if (IsDedicatedServerByComment(host.comment)) {
					foundGame.isDedicated = true;
					foundGame.playerCount = host.connectedPlayers - 1;
					foundGame.maxPlayerCount = host.playerLimit - 1;
				} else {
					foundGame.isDedicated = false;
					foundGame.playerCount = host.connectedPlayers;
					foundGame.maxPlayerCount = host.playerLimit;
				}			
				foundGame.ping = new Ping(foundGame.ipAddress);
				foundGame.isOnLAN = false;
				foundWANGames.Add(foundGame);
			}		
			MasterServer.ClearHostList();
		}
		
		// Update our LAN listings from the LAN broadcast service. This list has a list of all received
		// UDP packets in the last five seconds.
		foundLANGames.Clear();
		foreach (LANBroadcastService.ReceivedMessage m in lanBroadcastService.ReceivedMessages)
		{
			if (m.bIsReady) {
				// This is a host announcing its presence
				FoundGame foundGame = new FoundGame();
				foundGame.ipAddress = m.strIP;
				foundGame.ping = new Ping(foundGame.ipAddress);
				foundGame.isOnLAN = true;
				foundLANGames.Add(foundGame);
			}
		}
	}
	
	void OnFailedToConnectToMasterServer(NetworkConnectionError error)
	{
		// TODO: We should probably do stuff here someday
		Debug.Log("Failed to connect to the master server! " + error.ToString());
	}
	
	void OnMasterServerEvent(MasterServerEvent msEvent)
	{
		// TODO: We should probably do stuff here someday
		Debug.Log("In OnMasterServerEvent: " + msEvent.ToString());
	}	
	
	#endregion
	
	/// <summary>
	/// Enables the LAN game search.
	/// </summary>
	/// <param name='enable'>
	/// Enable.
	/// </param>
	public void EnableLANGameSearch(bool enable)
	{
		if (isLANGameSearchEnabled != enable)
		{
			isLANGameSearchEnabled = enable;
			if (enable) {
				lanBroadcastService.StartSearchBroadCasting(null,null);
			} else {
				lanBroadcastService.StopBroadCasting();
			}
		}
	}
	
	/// <summary>
	/// Registers our game with Unity's master game server list and the LAN
	/// </summary>
	/// <param name='gameTypeName'>
	/// Game type name.
	/// </param>
	/// <param name='gameName'>
	/// Game name.
	/// </param>
	/// <param name='comment'>
	/// Comment.
	/// </param>
	/// <param name='dedicatedServer'>
	/// Dedicated server.
	/// </param>
	/// <param name='internetServer'>
	/// Internet server.
	/// </param>
	public void RegisterHost(string gameTypeName, string gameName, string comment, bool dedicatedServer, bool internetServer)
	{
		// TODO: Pass in server information (pass in the game type name, name, comment, and dedicated server flags)
		lanBroadcastService.StopBroadCasting();
		lanBroadcastService.StartAnnounceBroadCasting();
		if (internetServer) {
			MasterServer.RegisterHost(gameTypeName, gameName, FormatGameComment(comment, dedicatedServer));
		}
	}
	
	/// <summary>
	/// Removes our game from Unity's master game server list and the LAN
	/// </summary>
	public void UnregisterHost()
	{
		lanBroadcastService.StopBroadCasting();
		MasterServer.UnregisterHost(); // This is safe even if we aren't an internet host
	}
	
	/// <summary>
	/// Requests the master game server listing from Unity
	/// </summary>
	public void RequestHostList()
	{
		MasterServer.ClearHostList();
		foundWANGames = new List<FoundGame>(); // Clear our known list
		MasterServer.RequestHostList(VersionDirector.GetGameTypeName());
	}

}
