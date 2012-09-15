using UnityEngine;
using System.Collections;

/// <summary>
/// This class manages what happens in the "ConnectionFailureScene" scene.
/// </summary>
public class ConnectionFailureSceneDirector : MonoBehaviour 
{
	/// <summary>
	/// The reason for the connection failure 
	/// </summary>
	private string reason;
	
	/// <summary>
	/// This is called from anywhere in the program when a connection to the host
	/// has failed.
	/// </summary>
	/// <param name='reason'>
	/// The reason for the failure.
	/// </param>
	/// <param name='subsequentScene'>
	/// The scene to go to after the user clicks the Back button.
	/// </param>
	static public void Initialize(string reason, string subsequentScene)
	{
		PlayerPrefs.SetString("ConnectionFailureSceneReason", reason);
		PlayerPrefs.SetString("ConnectionFailureSubsequentScene", subsequentScene);
		Application.LoadLevel("ConnectionFailureScene");
	}	

	// Use this for initialization
	void Start () 
	{
		// Cache the reason for the disconnection
		reason = PlayerPrefs.GetString("ConnectionFailureSceneReason");	
	}
		
	void OnGUI()
	{
		// Show the reason for the connection failure
		GUI.Label(new Rect(0,0,Screen.width,Screen.height / 2), reason);
		
		// Back button takes the player back to the main menu
		float sx = Screen.width;
		float sy = Screen.height;
		if (GUI.Button(new Rect(sx * 0.425f, sy * 0.55f, sx * 0.15f, sy * 0.1f), "Back"))
		{
			// Go go to the next scene
			Application.LoadLevel(PlayerPrefs.GetString("ConnectionFailureSubsequentScene"));
		}	
	}
}
