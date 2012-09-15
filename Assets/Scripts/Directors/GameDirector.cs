using UnityEngine;
using System.Collections;

/// <summary>
/// This class is responsible for dealing with getting a player set up to begin playing a round of the game.
/// </summary>
public class GameDirector : MonoBehaviour 
{
	/// <summary>
	/// The one and only input director where all game commands go through
	/// </summary>
	private InputDirector inputDirector;
	
	/// <summary>
	/// The one and only GameRules script that applies to the current game
	/// </summary>
	private GameRules gameRules;
	
	/// <summary>
	/// Gets the game rules
	/// </summary>
	/// <value>
	/// The game rules component
	/// </value>
	public GameRules Rules 
	{
		get { return gameRules; }	
	}
	
	/// <summary>
	/// Get the current instance of the game director. It does not persist through the application's lifetime.
	/// </summary>
	static public GameDirector Get()
	{
		GameDirector gameDirector = (GameDirector)GameObject.FindObjectOfType(typeof(GameDirector));
		return gameDirector;
	}
	
	#region Unity Events
	
	// Use this for initialization
	void Start () 
	{
		// If we're a dedicated server, don't render anything
		if (inputDirector.IsDedicatedServer()) {
			Camera.main.enabled = false;
		}		
		
		// Now start the game if we're playing alone
		if (!inputDirector.IsNetworking()) 
		{	
			BeginGame(ConfigurationDirector.GetGameRules());
		}		
	}
	
	#endregion
	
	#region Client and Input Director events
	
	/// <summary>
	/// This message is sent by the Client component in the InputDirector object after the level has been
	/// loaded and all communications with the network game channels have been restored. This is code that
	/// should NOT be occurring in Start() because network communications would still be down at that time.
	/// </summary>
	void OnNetworkLoadedLevel()
	{
		// First off, get our input director. This is in a separate object.
		inputDirector = InputDirector.Get();			
		
		// In a network game, the server set up its instance of the rules, and starts its game here.
		// The client will wait from a buffered message from the server with the game rules enumeration.
		// Then it will start its game.
		if (inputDirector.IsHosting())
		{
			Debug.Log("Game is hot.");
			string gameRules = ConfigurationDirector.GetGameRules();
		
			// Inform the server and the clients of the game rules. It's important to do it now before 
			// any other buffered messages (like spawning AI cycles) happens, or else unpredictable things 
			// can happen.
			inputDirector.BroadcastBufferedCommand("OnDefineGameRules", gameRules);
		}
	}
	
	/// <summary>
	/// This message is sent from the Player component on clients. The originating message is a buffered
	/// RPC message from the server to all clients right after OnNetworkLoadedlevel for the purpose of
	/// informing clients what kind of game is being played. Hosts do not get this message.
	/// </summary>
	/// <param name='gameRules'>
	/// Game rules.
	/// </param>
	void OnGameRulesDefined(string gameRules)
	{
		// Now begin the game on our instance
		BeginGame(gameRules);
	}	
	
	#endregion
			
	/// <summary>
	/// This is called directly from a solo game or one where you are hosting; it is also called on
	/// clients after the server tells them what the game rules are. This function will add a component
	/// to the game director which controls how the game is run. Without calling this, the playfield 
	/// would be empty and nothing would ever happen.
	/// </summary>
	/// <param name='rulesName'>
	/// The name of the game rules set.
	/// </param>
	private void BeginGame(string rulesName)
	{
		Debug.Log("in BeginGame with rules: " + gameRules);
		
		if ("FreeForAll" == rulesName)
		{
			gameRules = (GameRules)gameObject.AddComponent<GameRules_FFA>();
		} 
		else
		{
			// This should never happen
			Debug.LogError("Unhandled game rule type: " + gameRules + "!");
			return;
		}
		
		// Send a message to the rules component to begin the game. This is where the scores are
		// reset, players are spawned, etc. This is the part where this player, whether they be the
		// host or a client who just joined, creates themselves on the playfield.
		SendMessage("OnBeginGame");
	}	
		
	/// <summary>
	/// This message is sent by the UI of this component to terminate a game in progress.
	/// </summary>
	void OnShutdown()
	{
		if (inputDirector.IsHosting()) {
			inputDirector.UnhostServer();
		} else {
			inputDirector.DisconnectFromServer();
		}
		// Return to the main menu
		Application.LoadLevel("Startup");
		ConsoleDirector.Log("Game session terminated");
	}
	
}
