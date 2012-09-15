using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// This class is responsible for setting up a free-for-all game
/// </summary>
public class GameRules_FFA : GameRules 
{
	/// <summary>
	/// The one and only input director where all game commands go through
	/// </summary>
	private InputDirector inputDirector;
	
	/// <summary>
	/// Our player in the game
	/// </summary>
	private Player selfPlayer;
	
	#region Game Director Events
	
	/// <summary>
	/// This message is sent by the GameDirector component to begin a game. This is where
	/// the players need to spawn, dynamic obstacles get set up, etc.
	/// </summary>
	void OnBeginGame() 
	{
		// Standard initialization for all game rules scripts.
		inputDirector = InputDirector.Get();
		selfPlayer = Player.Get();
		
		// If we're playing offline, then we need to do single-player setup stuff here.		
		if (!inputDirector.IsNetworking()) 
		{
			selfPlayer.gameObject.SendMessage("OnSpawnSpaceship", new Vector3(Random.value * 250 - 500, 0, Random.value * 250 - 500));
		}
		// If we're hosting a network game, then we need to decide now where all
		// the players are going to spawn and spawn them.
		else if (inputDirector.IsHosting()) 
		{
			// Spawn our own spaceship
			if (!inputDirector.IsDedicatedServer()) 
			{
				selfPlayer.gameObject.SendMessage("OnSpawnSpaceship", new Vector3(Random.value * 250 - 500, 0, Random.value * 250 - 500));
			}
		}
		else	
		{
			// If we get here, we're a client. Spawn our first spaceship in this scene.
			selfPlayer.gameObject.SendMessage("OnSpawnSpaceship", new Vector3(Random.value * 250 - 500, 0, Random.value * 250 - 500));
		}	
	}
	
	#endregion

}
