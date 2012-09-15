using UnityEngine;
using System.Collections;

/// <summary>
/// This class manages all configurable settings for a game. Everything from
/// the player profile to game hosting settings are handled here. All the current
/// values are hard coded for demonstration purposes; you would replace these with
/// PlayerPrefs function calls in the long term.
/// </summary>
static public class ConfigurationDirector
{
	/// <summary>
	/// Gets the max player count.
	/// </summary>
	/// <returns>
	/// The player count.
	/// </returns>
	static public int GetMaxPlayerCount()
	{
		return 32;
	}
	
	/// <summary>
	/// Gets the server port.
	/// </summary>
	/// <returns>
	/// The server port.
	/// </returns>
	static public int GetServerPort()
	{
		// Host on this port
		return 21182;
	}
	
	/// <summary>
	/// Gets the name of the player.
	/// </summary>
	/// <returns>
	/// The player name.
	/// </returns>
	static public string GetPlayerName()
	{
		return PlayerPrefs.GetString("PlayerName", "Player");
	}
	
	/// <summary>
	/// Sets the name of the player.
	/// </summary>
	/// <param name='value'>
	/// Value.
	/// </param>
	static public void SetPlayerName(string value)
	{
		PlayerPrefs.SetString("PlayerName", value);
	}
	
	/// <summary>
	/// Gets the game rules. This is expressed as a simple named string, though you could
	/// opt to make it an enumeration.
	/// </summary>
	/// <returns>
	/// The game rules.
	/// </returns>
	static public string GetGameRules()
	{
		// The name "FreeForAll" has no special significance to this engine; it's simply
		// the name chosen for this specific video game.
		return "FreeForAll";
	}
	
	/// <summary>
	/// Gets the multicast port for LAN game searching.
	/// </summary>
	/// <returns>
	/// The multicast port.
	/// </returns>
	static public int GetMulticastPort()
	{
		// Search for games on this port using UDP protocol
		return 22043;
	}
}
