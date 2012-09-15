// LAN UDP-Broadcast Service Script
// 12-11-2009
// Made by Jordin Kee aka Jordos
// You may use and/or modify this script as you like. Crediting is welcome, but not required.
// Use this at your own risk, I do not guarantee it is bugfree. In fact I do not guarantee anything :)

// This script can be used as a service to perform UDP Broadcasting over a LAN. This is usefull to search for servers on a LAN, without knowing their IP.
// Next, this script is designed for a situation where the player is not able to choose to either start a server, or join one.
// Instead, it is determined by the application (i.e. this script). This, because of the projet I made it for.
// If you don't like this, but still want to use the script, read on...
// This script uses the .Net UDPClient class to perform sending and receiving (http://msdn.microsoft.com/en-us/library/system.net.sockets.udpclient.aspx)

// How to use this script:
// This script must be seen as a service, therefore it must be controlled by another object, say "NetworkController". This script serves 2 goals:
// 1. Search for an existing server and determine out of the results, whether this player should join a server, or start one itself
// 2. Send out messages saying it has started a server and is ready to receive connections
// NetworkController calls 'StartSearchBroadcasting'. This function takes 2 delegates (WTF? Delegate? --> http://msdn.microsoft.com/en-us/library/900fyy8e(VS.71).aspx)
// One is called when the script has found a server. It passes the IP address of the server, so the NetworkController can join it.
// The other is called when there is no server found or it is determined that this player should start one. The NetworkController can create a server.
// In this last case service 2 should be used, namely StartAnnounceBroadcasting(). This will broadcast messages that this player has a server ready.
// When other players start a search, they will receive this message and stop searching immediately.
// Make sure NetworkController also calls StopBroadcasting(), as not doing this may result in crashes when the game is started again.

// How the script works:
// When a search is started, the script begins recieving messages. These messages are stored in a list and this list is refreshed, so that old messages are deleted.
// What the script also does, is sending out messages that this player is willing to start a server, but has none ready at the moment.
// The search stops as soon as a 'I have server ready' message is received, send out by the StartAnnounceBroadcasting() of another player.
// After a specified amount of time, the search is ended. The received messages are scanned. If there are none, this player must start a server.
// If there are messages 'i am willing to start a server' from other players, this means that multiple players are searching, but non has started a server.
// In this case the script determines which of these players must start the server. This is based on the IP of the players (the highest willl be the server).
// All players agree on this, as they use the same script. The player that is choosen will call the 'MustStartServer' delegate, 
// the others will continue searching, waiting for the message that a server is ready.

// If you want to use this script, but want players to choose between creating/joining a server for themselves:
// When the player chooses to create a server, call the StartAnnounceBroadcasting()
// When the player chooses to join, call StartSearchBroadcasting(), but strip the part of sending out messages (see Update() method)

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

public class LANBroadcastService : MonoBehaviour
{
    public delegate void delJoinServer(string strIP); // Definition of JoinServer Delegate, takes a string as argument that holds the ip of the server
    public delegate void delStartServer(); // Definition of StartServer Delegate
    public enum enuState { NotActive, Searching, Announcing }; // Definition of State Enumeration.
    public struct ReceivedMessage { public float fTime; public string strIP; public bool bIsReady;} // Definition of a Received Message struct. This is the form in which we will store messages

    private string strMessage = ""; // A simple message string, that can be read by other objects (eg. NetworkController), to show what this object is doing.
    private enuState currentState = enuState.NotActive;
    private UdpClient objUDPClient; // The UDPClient we will use to send and receive messages
    private List<ReceivedMessage> lstReceivedMessages; // The list we store all received messages in, when searching
    private delJoinServer delWhenServerFound; // Reference to the delegate that will be called when a server is found, set by StartSearchBroadcasting()
    private delStartServer delWhenServerMustStarted; // Reference to the delegate that will be called when a server must be created, set by StartSearchBroadcasting()
    private string strServerNotReady = "wanttobeaserver"; // The actual content of the 'i am willing to start a server' message
    private string strServerReady = "iamaserver"; // The actual content of the 'i have a server ready' message
    private float fTimeLastMessageSent;
    private float fIntervalMessageSending = 1f; // The interval in seconds between the sending of messages
    private float fTimeMessagesLive = 3; // The time a message 'lives' in our list, before it gets deleted
    private float fTimeToSearch = 5; // The time the script will search, before deciding what to do
    private float fTimeSearchStarted;
	private string myIPAddress;
	private float lastUpdateTime;

    public string Message { get { return strMessage; } } // Property to read the strMessage
	public enuState CurrentState { get { return currentState; } } 
	public List<ReceivedMessage> ReceivedMessages { get { return lstReceivedMessages; } }

    void Awake()
    {
        // Create our list
        lstReceivedMessages = new List<ReceivedMessage>();
		// Cache our IP address
		IPHostEntry host;
		myIPAddress = "0.0.0.0";
		host = Dns.GetHostEntry(Dns.GetHostName());
		foreach (IPAddress ip in host.AddressList)
		{
		    if (ip.AddressFamily.ToString() == "InterNetwork")
		    {
		        myIPAddress = ip.ToString();
		    }
		}
		Debug.Log("Local IP Address: " + myIPAddress);
    }

    void Update()
    {
        // Check if we need to send messages and the interval has espired
        if ((currentState == enuState.Searching || currentState == enuState.Announcing)
            && Time.time > fTimeLastMessageSent + fIntervalMessageSending)
        {
            // Determine out of our current state what the content of the message will be
            byte[] objByteMessageToSend = System.Text.Encoding.ASCII.GetBytes(currentState == enuState.Announcing ? strServerReady : strServerNotReady);
            // Send out the message
            objUDPClient.Send(objByteMessageToSend, objByteMessageToSend.Length, new IPEndPoint(IPAddress.Broadcast, ConfigurationDirector.GetMulticastPort()));
            // Restart the timer
            fTimeLastMessageSent = Time.time;

            // Refresh the list of received messages (remove old messages)
            if (currentState == enuState.Searching)
            {
                // This rather complex piece of code is needed to be able to loop through a list while deleting members of that same list
				for (int i=0; i < lstReceivedMessages.Count; i++)
				{
					if (Time.time > lstReceivedMessages[i].fTime + fTimeMessagesLive)	
					{
						// If this message is too old, delete it and restart the foreach loop	
						lstReceivedMessages.RemoveAt(i--);
					}
				}
            }
        }

        if (currentState == enuState.Searching)
        {
            // Check the list of messages to see if there is any 'i have a server ready' message present
            foreach (ReceivedMessage objMessage in lstReceivedMessages)
            {
                // If we have a server that is ready, call the right delegate and stop searching
                if (objMessage.bIsReady)
                {
                    StopSearching();
                    strMessage = "We will join";
					if (null != delWhenServerFound) {
                    	delWhenServerFound(objMessage.strIP);
					}
                    break;
                }
            }
            // Check if we're ready searching.
            if (currentState == enuState.Searching && Time.time > fTimeSearchStarted + fTimeToSearch)
            {
                // We are. Now determine who's gonna be the server.

                // This string holds the ip of the new server. We will start off pointing ourselves as the new server
                string strIPOfServer = myIPAddress;
                // Next, we loop through the other messages, to see if there are other players that have more right to be the server (based on IP)
                foreach (ReceivedMessage objMessage in lstReceivedMessages)
                {
                    if (ScoreOfIP(objMessage.strIP) > ScoreOfIP(strIPOfServer))
                    {
                        // The score of this received message is higher, so this will be our new server
                        strIPOfServer = objMessage.strIP;
                    }
                }
                // If after the loop the highest IP is still our own, call delegate to start a server and stop searching
                if (strIPOfServer == myIPAddress)
                {
                    StopSearching();
                    strMessage = "We will start server.";
					if (null != delWhenServerMustStarted) {
                    	delWhenServerMustStarted();
					}
                }
                // If it's not, someone else must start the server. We will simply have to wait as the server is clearly not ready yet
                else
                {
                    strMessage = "Found server. Waiting for server to get ready...";
                    // Clear the list and do the search again.
                    lstReceivedMessages.Clear();
                    fTimeSearchStarted = Time.time;
                }
            }
        }
		lastUpdateTime = Time.time;
    }

    // Method to start an Asynchronous receive procedure. The UDPClient is told to start receiving.
    // When it received something, the UDPClient is told to call the EndAsyncReceive() method.
    private void BeginAsyncReceive()
    {
        objUDPClient.BeginReceive(new AsyncCallback(EndAsyncReceive), null);
    }
    // Callback method from the UDPClient.
    // This is called when the asynchronous receive procedure received a message
    private void EndAsyncReceive(IAsyncResult objResult)
    {
        // Create an empty EndPoint, that will be filled by the UDPClient, holding information about the sender
        IPEndPoint objSendersIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
        // Read the message
        byte[] objByteMessage = objUDPClient.EndReceive(objResult, ref objSendersIPEndPoint);
        // If the received message has content and it was not sent by ourselves...
        if (objByteMessage.Length > 0 &&
            !objSendersIPEndPoint.Address.ToString().Equals(myIPAddress))
        {
            // Translate message to string
            string strReceivedMessage = System.Text.Encoding.ASCII.GetString(objByteMessage);
            // Create a ReceivedMessage struct to store this message in the list
            ReceivedMessage objReceivedMessage = new ReceivedMessage();
            objReceivedMessage.fTime = lastUpdateTime;
            objReceivedMessage.strIP = objSendersIPEndPoint.Address.ToString();
            objReceivedMessage.bIsReady = strReceivedMessage == strServerReady ? true : false;
            lstReceivedMessages.Add(objReceivedMessage);
        }
        // Check if we're still searching and if so, restart the receive procedure
        if (currentState == enuState.Searching) BeginAsyncReceive();
    }
    // Method to start this object announcing this is a server, used by the script itself
    private void StartAnnouncing()
    {
        currentState = enuState.Announcing;
        strMessage = "Announcing we are a server...";
    }
    // Method to stop this object announcing this is a server, used by the script itself
    private void StopAnnouncing()
    {
        currentState = enuState.NotActive;
        strMessage = "Announcements stopped.";
    }
    // Method to start this object searching for LAN Broadcast messages sent by players, used by the script itself
    private void StartSearching()
    {
        lstReceivedMessages.Clear();
        fTimeSearchStarted = Time.time;
        BeginAsyncReceive();
        currentState = enuState.Searching;
        strMessage = "Searching for other players...";
    }
    // Method to stop this object searching for LAN Broadcast messages sent by players, used by the script itself
    private void StopSearching()
    {
        currentState = enuState.NotActive;
        strMessage = "Search stopped.";
    }

    // Method to be called by some other object (eg. a NetworkController) to start a broadcast search
    // It takes two delegates; the first for when this object finds a server that can be connected to, 
    // the second for when this player is determined to start a server itself.
    public void StartSearchBroadCasting(delJoinServer connectToServer, delStartServer startServer)
    {
        // Set the delegate references, so other functions within this class can call it
        delWhenServerFound = connectToServer;
        delWhenServerMustStarted = startServer;
        // Start a broadcasting session (this basically prepares the UDPClient)
        StartBroadcastingSession();
        // Start a search
        StartSearching();
    }
    // Method to be called by some other object (eg. a NetworkController) to start a broadcast announcement. Announcement means; tell everyone you have a server.
    public void StartAnnounceBroadCasting()
    {
        // Start a broadcasting session (this basically prepares the UDPClient)
        StartBroadcastingSession();
        // Start an announcement
        StartAnnouncing();
    }
    // Method to start a general broadcast session. It prepares the object to do broadcasting work. Used by the script itself.
    private void StartBroadcastingSession()
    {
        // If the previous broadcast session was for some reason not closed, close it now
        if (currentState != enuState.NotActive) StopBroadCasting();
        // Create the client
        objUDPClient = new UdpClient(ConfigurationDirector.GetMulticastPort());
        objUDPClient.EnableBroadcast = true;
        // Reset sending timer
        fTimeLastMessageSent = Time.time;
    }
    // Method to be called by some other object (eg. a NetworkController) to stop this object doing any broadcast work and free resources.
    // Must be called before the game quits!
    public void StopBroadCasting()
    {
        if (currentState == enuState.Searching) StopSearching();
        else if (currentState == enuState.Announcing) StopAnnouncing();
        if (objUDPClient != null)
        {
            objUDPClient.Close();
            objUDPClient = null;
        }
    }
    // Method that calculates a 'score' out of an IP adress. This is used to determine which of multiple clients will be the server. Used by the script itself.
    private long ScoreOfIP(string strIP)
    {
        long lReturn = 0;
        string strCleanIP = strIP.Replace(".", "");
        lReturn = long.Parse(strCleanIP);
        return lReturn;
    }
}
