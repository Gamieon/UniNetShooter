using UnityEngine;
using System.Collections;

/// <summary>
/// This class describes fundamental player attributes
/// </summary>
public class PlayerAttributes
{
	/// <summary>
	/// This is the player's network view ID from the server's perspective
	/// </summary>
	public string ID; 

	/// <summary>
	/// The player's name
	/// </summary>
	public string PlayerName;
		
	/// <summary>
	/// An object containing extended application-specific attributes
	/// </summary>
	public object Extended;
}
