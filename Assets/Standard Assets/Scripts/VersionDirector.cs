using UnityEngine;
using System.Collections;

/// <summary>
/// This class is responsible for maintaining the application version
/// and name for use with the master server.
/// </summary>
public class VersionDirector
{
	/// <summary>
	/// Gets the version of the application.
	/// </summary>
	/// <returns>
	/// The version.
	/// </returns>
	static public string GetVersion()
	{
		return "0.1";
	}
	
	/// <summary>
	/// Gets the name of the game type.
	/// </summary>
	/// <returns>
	/// The game type name.
	/// </returns>
	static public string GetGameTypeName()
	{
		return "UniNetShooter " + GetVersion();
	}
}
