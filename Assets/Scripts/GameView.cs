using UnityEngine;
using System.Collections;

/// <summary>
/// This class is responsible for rendering the game's overlay. We should minimize the number
/// of OnGUI calls per scene.
/// </summary>
public class GameView : MonoBehaviour 
{
	public Texture2D plainTex;
	
	private Server server;
	
	#region Unity Events
	
	void Start()
	{
		server = Server.Get();
	}
	
	void OnGUI()
	{
		// TODO: Maintain a spaceship list that only changes in response to messages
		Spaceship[] spaceships = (Spaceship[])Object.FindObjectsOfType(typeof(Spaceship));
		foreach (Spaceship s in spaceships)
		{
			if (s.Initialized)
			{
				PlayerAttributes player = server.GetPlayer(s.playerID);
				Vector3 screenPos = Camera.main.WorldToScreenPoint(s.transform.position);
				screenPos.y = Screen.height - screenPos.y;
				
				// Draw the HP box frame
				int width = 50;
				int height = 8;
				Rect rBox = new Rect(screenPos.x - width/2, screenPos.y - 20 - height/2, width, height);
				GUI.color = Color.white;
				GUI.DrawTexture(rBox, plainTex);
				
				// Draw the name
				GUI.Label(new Rect(rBox.xMin, rBox.yMin-22, 300,20), player.PlayerName);
				
				GUI.color = Color.black;
				rBox.xMin++; rBox.yMin++; 
				rBox.xMax--; rBox.yMax--;
				GUI.DrawTexture(rBox, plainTex);
				
				// Now draw the HP
				GUI.color = Color.green;
				rBox.xMin++; rBox.yMin++; 
				rBox.xMax--; rBox.yMax--;
				float w = (float)rBox.width * (float)s.CurrentEnergy / (float)s.maxEnergy;
				rBox.xMax = rBox.xMin + w;
				GUI.DrawTexture(rBox, plainTex);			
			}
		}
		
		int buttonWidth = 200;
		int buttonHeight = 50;
		GUI.color = Color.white;
		if (GUI.Button(new Rect(20, Screen.height - buttonHeight - 20, buttonWidth, buttonHeight), "Quit"))
		{
			SendMessage("OnShutdown");
		}		
	}
	
	#endregion
}
