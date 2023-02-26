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
        NewPlayerEvent,
        ListPlayersEvent,
        ChangeStatEvent
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
        public string playerName;
        #region comment
        // Actor number is going to be a number that the network refers to each individual player.
        #endregion
        public int playerActorNumber;
        public int playerKills;
        public int playerDeaths;

        #region comment
        // We are going to use a constructor to recieve new information about the player 
        #endregion
        public PlayerInformation(string playerName, int playerActorNumber, int playerKills, int playerDeaths)
        {
            this.playerName = playerName;
            this.playerActorNumber = playerActorNumber;
            this.playerKills = playerKills;
            this.playerDeaths = playerDeaths;
        }
    }
}
