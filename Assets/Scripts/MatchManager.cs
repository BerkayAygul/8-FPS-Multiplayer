using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static MatchManager instance;

    public List<PlayerInformation> allPlayersList = new List<PlayerInformation>();
    #region comment
    // We are going to keep track of what is our position in allPlayersList.
    #endregion
    private int index;

    #region comment
    /* We are going to use an enum to determine which kind of event we are sending.
    ** The enum's type is going to be byte, which is quite small than int. It is useful for sending information over networks in general. */
    #endregion
    public enum EventCodes : byte
    {
        #region comment
        /* We are going to create two new functions called Send and Receive for each event.
        ** We are going to control the sending functions. For example, when somebody joins the match, we will send the new player information over the network,
        ** When someone dies we will update the killing stats, the list players event will be a full list of all the players that are in our system and we will
        ** send that out from the master or the host of our game to the other players so they know they all have the same full list of players. 
        ** When we Receive new events, OnEvent function will listen for events that are coming in. Whenever we Receive an event, we will handle it with
        ** each of these Receive functions. */
        #endregion
        NewPlayerEvent,
        ListPlayersEvent,
        UpdateStatsEvent
    }

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        #region comment
        /* Auto return to the menu if we are not connected to the network. We do not want to change between scenes every time. 
        ** This will help us cut down on our time when we are in Unity Editor. Because we can go back to the menu scene instantly
        ** when we start a different scene. */
        #endregion
        if (!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene(0);
        }
        else
        {
            #region comment
            // Send the player's nickname as the argument value for this function.
            #endregion
            NewPlayerEventSend(PhotonNetwork.LocalPlayer.NickName);
        }
    }

    void Update()
    {

    }

    #region comment
    /* Events in Photon Network are basically ways of sending out a whole variety of different kinds of information at any particular time.
    ** We send the information out from one of our players playing the game and then that information can be picked up by the other players and
    ** something can be done with that information.
    ** We are going to use Photon.Realtime; and ExitGames.Client.Photon; (Exit Games are the people who made Photon) libraries. 
    ** We are going to derive from  MonoBehaviourPunCallbacks, IOnEventCallback classes. */
    #endregion
    #region comment
    // What this function does is whenever we have an event that's sent by any other client, this method will read that event.
    #endregion
    public void OnEvent(EventData photonEvent)
    {
        #region comment
        /* The event code number will be assigned from the Enum values, logically the number of codes should be equal to the number of the Enum.
        ** Also, byte type is between zero and two hundred and fifty five and any value that are above two hundred are actually specifically reserved
        ** by the Photon system for handling different things. So we do not ever want to deal with those number of codes. */
        #endregion
        if(photonEvent.Code < 200)
        {
            #region comment
            /* Use casting to turn photonEvent.Code to our Enum.
            ** So now we know when a code number is sent, we can convert it to EventCodes Enum and store the code number.*/
            #endregion
            EventCodes theEvent = (EventCodes)photonEvent.Code;

            #region comment
            /* Whatever the custom data is received by the event, we are going to convert that into an array of objects.
            ** Objects can basically be a whole bunch of data, and we will convert that individual data into some information that we can use. */
            #endregion
            object[] receivedData = (object[])photonEvent.CustomData;

            #region comment
            /* We are going to use a switch because to make different things depending on which event case is true.
            ** For example, if the event that we received is NewPlayerEvent, then we can do a specific thing.
            ** We are going to send our received data to each */
            #endregion
            switch (theEvent)
            {
                case EventCodes.NewPlayerEvent:
                    NewPlayerEventReceive(receivedData);
                    break;

                case EventCodes.ListPlayersEvent:
                    ListPlayerEventReceive(receivedData);
                    break;
                case EventCodes.UpdateStatsEvent:
                    UpdateStatsEventReceive(receivedData);
                    break;
            }
        }
    }

    #region comment
    /* OnEnable() and OnDisable() functions are two built in functions in Unity. Whenever the GameObject that is attached with those two functions or
    ** the specific class component gets enabled or disabled, then it will call those functions. */
    #endregion
    public override void OnEnable()
    {
        #region comment
        /* The reason of why are we doing this is when we load into our game, our MatchManager game object gets enabled. 
        ** AddCallbackTarget is basically saying we want to add ourselves to the list. So when an event callback happens, when an event gets declared,
        ** this script wants to listen for those events. So it will basically say, when an event happens tell me about it. 
        ** Then it will read the event in OnEvent() function. */
        #endregion
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        #region comment
        // Remove the event from the list. We will get errors if we do not remove events from the network list. 
        #endregion
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void NewPlayerEventSend(string username)
    {
        #region comment
        // package is going to be everything we want to send. This object array has four slots for the player's username, actor number, kills and deaths. 
        #endregion
        object[] package = new object[4];
        package[0] = username;
        #region comment
        // Actor number of the player that is playing the game right now.
        #endregion
        package[1] = PhotonNetwork.LocalPlayer.ActorNumber;
        #region comment
        // Zero player kills and deaths at the start.
        #endregion
        package[2] = 0;
        package[3] = 0;

        #region comment
        /* This is how to send an event in Photon Network. We are sending the event code for the new player event, the package and we send this information 
        ** only to the MasterClient. Also, we want to make this information reliable so it will definitely be sent correctly over the network, 
        ** and reach to the master client. */
        #endregion
        PhotonNetwork.RaiseEvent
            (
            (byte)EventCodes.NewPlayerEvent,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient},
            new SendOptions { Reliability = true }
            );
    }
    public void NewPlayerEventReceive(object[] receivedData)
    {
        #region comment
        /* Send the new player's data to this function and do the correct type castings.
        ** We know which information is in which index of the array. We can check the package array's values in NewPlayerEventSend() function. */
        #endregion
        #region comment
        /* On Start() we send the new player's nickname to NewPlayerEventSend() function, 
        ** we make a package about the player's information in NewPlayerEventSend() function, send the package with RaiseEvent(), 
        ** then PhotonNetwork raises an event, OnEvent() function starts and gets the same information with photonEvent,
        ** we look at the event code and start this NewPlayerEventReceive() function with a switch statement and send the player's information to this
        ** function, then we add the player's information to the list. */
        #endregion
        #region comment
        /* We are sending this information to the MasterClient, for test issues, if we do not create the room as the master in Unity Editor, 
        ** we can not see the list of the players on MatchManager game object. Unless we share the list of the players with another structure */
        #endregion
        PlayerInformation playerInfo = new PlayerInformation((string)receivedData[0], (int)receivedData[1], (int)receivedData[2], (int)receivedData[3]);

        allPlayersList.Add(playerInfo);

        #region comment
        // Whenever the master receives information about a new player, we are going to call ListPlayerEventSend() and update everybody else.
        #endregion
        ListPlayerEventSend();
    }

    public void ListPlayerEventSend()
    {
        #region comment
        /* We have our players being added correctly, but we want to share that list of the players information with every player who isn't the master client. 
        ** We are going package up all the players that in our allPlayersList and send that information over the network. 
        ** The length of the package must be the count of all players in the list. */
        #endregion
        object[] package = new object[allPlayersList.Count];

        #region comment
        /* We are basically looping through all players, then add each player as array to the package array.
        ** package[0] will contain the four values about the first player, package[1] will contain the four values about the second player and so on. 
        ** Then we are going to send this information over the network, to all players. */  

        #endregion
        for(int i=0; i<allPlayersList.Count; i++)
        {
            object[] playersInPackage = new object[4];

            playersInPackage[0] = allPlayersList[i].playerUsername;
            playersInPackage[1] = allPlayersList[i].playerActorNumber;
            playersInPackage[2] = allPlayersList[i].playerKills;
            playersInPackage[3] = allPlayersList[i].playerDeaths;

            package[i] = playersInPackage;
        }

        PhotonNetwork.RaiseEvent
            (
            (byte)EventCodes.ListPlayersEvent,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
            );
    }

    public void ListPlayerEventReceive(object[] receivedData)
    {
        #region comment
        // When ListPlayerSendEvent is called in NewPlayerEventReceive(), this function will work when OnEvent() listens ListPlayerEventSend() event.
        #endregion
        #region comment
        // First of all, we are going to clear all the current information stored about the players. We can reveice the information with an object value.
        #endregion
        allPlayersList.Clear();

        #region comment
        /* We are going to loop through the data we received, and depack(reverse) the package with the logic in ListPlayerEventSend() function.
        ** Then we are going to add the players to the allPlayersList. */ 
        #endregion
        for (int i = 0; i < receivedData.Length; i++)
        {
            object[] playersInPackage = (object[])receivedData[i];

            PlayerInformation player = new PlayerInformation
                (
                (string)playersInPackage[0],
                (int)playersInPackage[1],
                (int)playersInPackage[2],
                (int)playersInPackage[3]
               );

            allPlayersList.Add(player);

            #region comment
            /* We are going to make a extra check in here. If the local player's actor number is equal to the player that was just added to the list,
            ** Then that means this local player is the player that is just got added to the list. We are going to keep track of our position with this value. */
            #endregion
            if(PhotonNetwork.LocalPlayer.ActorNumber == player.playerActorNumber)
            {
                index = i;
            }
        }
    }

    public void UpdateStatsEventSend()
    {

    }

    public void UpdateStatsEventReceive(object[] receivedData)
    {

    }

    #region comment
    /* We are going to have our match manager keeping track of information about our players, for example, information about how many kills and how many deaths
    ** they have had, is the match currently ongoing, has the match been finished, has somebody won the match... 
    ** First we are going to create how the player information is stored. We are going to create create a class for that. We do not have to create a
    ** whole new script back in Unity. */
    #endregion
    #region comment
    // Use [System.Serializable] to see the class on the game object that this class is attached in Unity.
    #endregion
    [System.Serializable]
    public class PlayerInformation
    {
        public string playerUsername;
        #region comment
        // Actor number is going to be a number that the network refers to each individual player.
        #endregion
        public int playerActorNumber;
        public int playerKills;
        public int playerDeaths;

        #region comment
        // We are going to use a constructor to Receive new information about the player 
        #endregion
        public PlayerInformation(string playerUsername, int playerActorNumber, int playerKills, int playerDeaths)
        {
            this.playerUsername = playerUsername;
            this.playerActorNumber = playerActorNumber;
            this.playerKills = playerKills;
            this.playerDeaths = playerDeaths;
        }
    }
}
