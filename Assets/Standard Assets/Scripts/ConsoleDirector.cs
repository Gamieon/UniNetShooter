using UnityEngine;
using System.Collections;

/// <summary>
/// This is the console director. Right now it's only good for logging messages.
/// Eventually you could use this to log messages in a global console. Consider
/// this class a very "under construction" class.
/// </summary>
static public class ConsoleDirector  
{
	static public void Log (string value) 
	{
		Debug.Log(value);
	}
}
