// This is a temporary script used to get players to join automatically in another
// hosted game if it's on the LAN.

using UnityEngine;
using System.Collections;

public class DebuggingAid : MonoBehaviour 
{
	MasterServerDirector masterServerDirector;
	bool isSearchingForLANGames;
	string serverIP = "";
	string playerName = "";
	
	// Use this for initialization
	void Start () 
	{
		// Get the player name
		playerName = ConfigurationDirector.GetPlayerName();
		
		// Create a persistent player object. This, and the InputDirector object that is
		// created in the process, will exist for the rest of the application's lifetime.
		// We don't actually need it here, so just discard the return value.
		Player.Create();
		
		// Grab the master server director so we can listen for LAN games
		masterServerDirector = InputDirector.Get().gameObject.GetComponent<MasterServerDirector>();
	}
	
	// Update is called once per frame
	void Update () 
	{
		// Connect to the first LAN game we find automatically
		if (isSearchingForLANGames && masterServerDirector.LANGameCount > 0)
		{			
			string strIP = masterServerDirector.GetLANGameByIndex(0).ipAddress;
			Debug.Log("We found a server at " + strIP);
			InputDirector inputDirector = InputDirector.Get();
			isSearchingForLANGames = false;
			masterServerDirector.EnableLANGameSearch(false);
			ConfigurationDirector.SetPlayerName(playerName);
			inputDirector.ConnectToServer(strIP, ConfigurationDirector.GetServerPort(), "");
		}
	}
	
	void OnGUI() 
	{
		if (GUI.Button(new Rect(10,10,300,50), "Host a game"))
		{
			// Host a game
			ConfigurationDirector.SetPlayerName(playerName);
			isSearchingForLANGames = false;
			masterServerDirector.EnableLANGameSearch(false);			
			InputDirector inputDirector = InputDirector.Get();
			inputDirector.HostServer(
				ConfigurationDirector.GetMaxPlayerCount(),
				ConfigurationDirector.GetServerPort(),
			    false, // Not a dedicated server
				"", // No password
				InputDirector.HostServerType.LAN, // LAN game (Don't submit to Unity's game listings)
				VersionDirector.GetGameTypeName(),
				"My Game",
				"Woot"
				);
		}
		
		serverIP = GUI.TextField(new Rect(350, 300, 300, 50), serverIP);		
		if (GUI.Button(new Rect(10,300,300,50), "Connect to IP"))
		{
			// Connect to a game
			ConfigurationDirector.SetPlayerName(playerName);
			isSearchingForLANGames = false;
			masterServerDirector.EnableLANGameSearch(false);			
			InputDirector inputDirector = InputDirector.Get();
			inputDirector.ConnectToServer(serverIP, ConfigurationDirector.GetServerPort(), "");
		}
		
		GUI.Label(new Rect(600,10,100,50), "Name");
		playerName = GUI.TextField(new Rect(700, 10, 300, 50), playerName);
		
		isSearchingForLANGames = GUI.Toggle(new Rect(10,150,200,50), isSearchingForLANGames, "Search for LAN games");
		masterServerDirector.EnableLANGameSearch(isSearchingForLANGames);	
	}
}
