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
    // We need to add each player information row to a list.
    #endregion
    private List<LeaderboardPlayerInformation> leaderboardPlayerInformationList = new List<LeaderboardPlayerInformation>();

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

    #region comment
    // Waiting state is when the match has not quite started just yet.
    #endregion
    public enum GameState : byte
    {
        GameWaitingState,
        GamePlayingState,
        GameEndingState
    }

    #region comment
    // When the match completes, we want to go out to the main menu or continue the session on another map.
    #endregion
    public int killCountToWin = 3;
    #region comment
    // We are going to make some changes in LateUpdate() function in PlayerController.cs with regard to current game status to change the camera view. 
    #endregion
    public Transform mapOverviewCameraPoint;
    public GameState currentGameState = GameState.GameWaitingState;
    public float waitStateTime = 5f;

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
            #region comment
            // Once the match manager is connected and working correctly, we are going to set our game status to playing state.
            #endregion
            currentGameState = GameState.GamePlayingState;
        }
    }

    void Update()
    {
        #region comment
        // If the table is already open, then close it. If it is not open, then open the table.
        #endregion
        #region comment
        // We do not want to show the leaderboard panel if our game has ended.
        #endregion
        if(Input.GetKeyDown(KeyCode.Tab) && currentGameState != GameState.GameEndingState)
        {
            if(UIController.instance.leaderboardTableDisplay.activeInHierarchy)
            {
                UIController.instance.leaderboardTableDisplay.SetActive(false);
            }
            else
            {
                ShowLeaderboard();
            }
        }
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
        #region comment
        /* We are going to add one more information to this package to keep information about the game state. 
        ** Because the package's first position contains the game state information, package[i + 1] will contain information of the players.
        ** We need to do the opposite of this for recieving this event where we unpack the package. */
        #endregion
        object[] package = new object[allPlayersList.Count + 1];

        package[0] = currentGameState;

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

            package[i + 1] = playersInPackage;
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
        /* We know that the package's first element contains information about the current game state as we set this up in ListPlayerEventSend() function.
        ** Now, when we unpack the package, instead of starting at zero, we need to start at position one.
        ** Also instead of storing the index at i, we need to store it at i-1 */
        #endregion
        currentGameState = (GameState)receivedData[0];

        #region comment
        /* We are going to loop through the data we received, and depack(reverse) the package with the logic in ListPlayerEventSend() function.
        ** Then we are going to add the players to the allPlayersList. */ 
        #endregion
        for (int i = 1; i < receivedData.Length; i++)
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
                index = i-1;
            }
        }

        #region comment
        // Only the master player will send this information to other players. Then the other players receive this information, will check their own state.
        #endregion
        CurrentGameStateCheck();
    }

    public void UpdateStatsEventSend(int playerActorNumber, int statTypeToUpdate, int amountToChange)
    {
        #region comment
        /* The amount of kills and deaths the players have is going to change. We need to pass in three information to this function. 
        ** We need to know which player we are updating information about, and we need to update kills and deaths and how much we add to them. */
        #endregion
        #region comment
        /* We need the actor number because we want to see which player killed the local player. 
        ** We use an int value for the stat to update because we will just say zero will be a kill, one will be a death.
        ** We might need to add bonus kill points, so we use an int value for the amount to change. */
        #endregion
        #region comment
        // Rather than assign the length of the array and package up on each piece one by one, we can add the values to the array just like this.
        #endregion
        #region comment
        /* We are going to call this function from PlayerController.cs, in TakeDamage().
        ** We are going to add an actor number value information of the damager player to DealDamage() RPC function, 
        ** and send that actor number information as a reference to the RPC function call. Then we are going to pass the damager player's actor number to
        ** TakeDamage() function and update the damager player's kill stat. */
        #endregion
        #region comment
        // To update the dead player's death stat, we need to send the dead player's actor number, in PlayerSpawner.cs, in DestroyPlayer().
        #endregion
        object[] package = new object[] { playerActorNumber, statTypeToUpdate, amountToChange };

        PhotonNetwork.RaiseEvent
            (
            (byte)EventCodes.UpdateStatsEvent,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
            );
    }

    public void UpdateStatsEventReceive(object[] receivedData)
    {
        int playerActorNumber = (int)receivedData[0];
        int statTypeToUpdate = (int)receivedData[1];
        int amountToChange = (int)receivedData[2];

        #region comment
        /* After we recieve the package, we are going to loop through all our players in our list, and if one of them equals the actor that is happened, 
        ** we will add a change to them. case 0: is kill stat, case 1: is death stat. */
        #endregion
        for(int i = 0; i<allPlayersList.Count; i++)
        {
            if(allPlayersList[i].playerActorNumber == playerActorNumber)
            {
                switch(statTypeToUpdate)
                {
                    case 0:
                        allPlayersList[i].playerKills += amountToChange;
                        Debug.Log("Player " + allPlayersList[i].playerUsername + " : kills " + allPlayersList[i].playerKills);
                        break;
                    case 1:
                        allPlayersList[i].playerDeaths += amountToChange;
                        Debug.Log("Player " + allPlayersList[i].playerUsername + " : deaths " + allPlayersList[i].playerDeaths);
                        break;
                }

                #region comment
                // If it is the player that's having a value changed, then update the player's stat text. 
                #endregion
                if(i==index)
                {
                    UpdateStatsDisplay();
                }

                #region comment
                // This will basically update the leaderboard table if the player is already looking at the leaderboard table.
                #endregion
                if(UIController.instance.leaderboardTableDisplay.activeInHierarchy)
                {
                    ShowLeaderboard();
                }

                #region comment
                // After we found the correct player and make the changes, we do not need to continue the loop for any longer.
                #endregion
                break;
            }
        }

        #region comment
        // Check the kill score of the players if someone has met the requirements to win the match.
        #endregion
        MatchEndScoreCheck();
    }

    public void UpdateStatsDisplay()
    {
        #region comment
        // We know where we are in the list with the index variable. We need to be sure the player count has not been changed in a weird way.
        #endregion
        if(allPlayersList.Count > index)
        {
            UIController.instance.killStatText.text = "Kills: " + allPlayersList[index].playerKills;
            UIController.instance.deathStatText.text = "Deaths: " + allPlayersList[index].playerDeaths;
        }
        else
        {
            UIController.instance.killStatText.text = "Kills: 0 ";
            UIController.instance.deathStatText.text = "Deaths: 0";
        }
    }

    void ShowLeaderboard()
    {
        #region comment
        /* When pressed the tab key, active the leaderboard table. Also, we need to update the table in UpdateStatsEventReceive() if the tab key
        ** is already pressed down. */
        #endregion
        UIController.instance.leaderboardTableDisplay.SetActive(true);

        #region comment
        /* When we open up the leaderboard, we want to make sure that the leaderboard is showing the correct players every time, in case players join the game 
        ** lately. First of all we are going to remove the old leaderboard and clean the list. Then we are going to make sure of that the default example
        ** player leaderboard row is always hidden. */
        #endregion
        foreach (LeaderboardPlayerInformation leaderboardPlayer in leaderboardPlayerInformationList)
        {
            Destroy(leaderboardPlayer.gameObject);
        }
        leaderboardPlayerInformationList.Clear();

        UIController.instance.LeaderboardPlayerInformation.gameObject.SetActive(false);

        #region comment
        // Return the sorted list from SortLeaderBoardPlayers() and pass it. 
        #endregion
        List<PlayerInformation> SortedPlayersList = SortLeaderboardPlayers(allPlayersList);

        #region comment
        // Using the sorted players list, we can receive player information to add them to the leaderboard table by rows, and then activate each row.
        #endregion
        foreach (PlayerInformation playerToAdd in SortedPlayersList)
        {
            LeaderboardPlayerInformation newPlayerLeaderboardRow = Instantiate(UIController.instance.LeaderboardPlayerInformation, UIController.instance.LeaderboardPlayerInformation.transform.parent);

            newPlayerLeaderboardRow.SetPlayerLeaderboardInformation(playerToAdd.playerUsername, playerToAdd.playerKills, playerToAdd.playerDeaths);

            newPlayerLeaderboardRow.gameObject.SetActive(true);

            leaderboardPlayerInformationList.Add(newPlayerLeaderboardRow);
        }
    }

    #region comment
    /* This function will sort the players by kills in the leaderboard and return the table. That is why we created it with List<PlayerInformation> value.
    ** We are going to pass the allPlayersList to this function and look at player kill values. */
    #endregion
    private List<PlayerInformation> SortLeaderboardPlayers(List<PlayerInformation> _allPlayersList)
    {
        #region comment
        // We need to create a new list and make this list sorted by player kills.
        #endregion
        List<PlayerInformation> sortedPlayersList = new List<PlayerInformation>();

        #region comment
        // At the start, because our sorted list is empty, we can start the loop like this.
        #endregion
        while(sortedPlayersList.Count < _allPlayersList.Count)
        {
            #region comment
            // We are going to set the default highest kill score is -1;
            #endregion
            int highestKillScore = -1;

            #region comment
            // Select the first player in the allPlayersList.
            #endregion
            PlayerInformation selectedPlayer = _allPlayersList[0];

            #region comment
            /* For each player in the allPlayersList, get the highest scored player and add it to the list. If that player is already in the sorted list, 
            ** select the next highest scored player and add it to the sorted list. */
            #endregion
            foreach (PlayerInformation player in _allPlayersList)
            {
                if(!sortedPlayersList.Contains(player))
                {
                    if (player.playerKills > highestKillScore)
                    {
                        selectedPlayer = player;
                        highestKillScore = player.playerKills;
                    }
                }
            }

            sortedPlayersList.Add(selectedPlayer);
        }

        #region comment
        // After all players are sorted, return the sorted list and use this sorted list in ShowLeaderboard() function.
        #endregion
        return sortedPlayersList;
    }

    #region comment
    // Whenever a player leaves the room, this event will be triggered and the player will go to the main menu.
    #endregion
    public override void OnLeftRoom()
    {
        #region comment
        // base will call the simple version of this function and make the things that is suppose the happen on default.
        #endregion
        base.OnLeftRoom();

        SceneManager.LoadScene(0);
    }

    #region comment
    // Whenever our players update their stats in UpdateStatsEventReceive(), we are going to check if someone reached the requirement to win the match.
    #endregion
    void MatchEndScoreCheck()
    {
        bool IsWinnerFound = false;

        foreach (PlayerInformation player in allPlayersList)
        {
            if(player.playerKills >= killCountToWin && killCountToWin > 0)
            {
                IsWinnerFound = true;
                break;
            }
        }

        if(IsWinnerFound == true)
        {
            #region comment
            /* As long as we haven't already ended the game already, end the game.
            ** then the master client must inform all players in the room about the state of the game and a really good time to do this is
            ** every time a new player arrives because we want every new player to be in sync with the master client. */
            #endregion
            if(PhotonNetwork.IsMasterClient && currentGameState != GameState.GameEndingState)
            {
                currentGameState = GameState.GameEndingState;
                #region comment
                // This event is only being sent by the master client to update everyone. We are going to make some changes in this function.
                #endregion
                ListPlayerEventSend();
            }
        }
    }

    #region comment
    // We are going to call this function in ListPlayerEventReceive() to check the current state of the game after we found the winner player.
    #endregion
    void CurrentGameStateCheck()
    {
        if(currentGameState == GameState.GameEndingState)
        {
            EndGame();
        }
    }

    void EndGame()
    {
        #region comment
        // We should make sure that the game has ended.
        #endregion
        currentGameState = GameState.GameEndingState;

        #region comment
        /* When the game ends, we want to destroy players and other things on the network, then show the end screen, show the leaderboard panel,
        ** free the mouse cursor and make it visible, then wait for a few seconds then tell the players to go to main menu. */
        #endregion
        if(PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
        }

        UIController.instance.matchEndScreen.SetActive(true);
        ShowLeaderboard();

        #region comment
        // We need to make sure of freeing the mouse cursor when players go back to the main menu in Start() function in ServerLauncher.cs
        #endregion
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(EndGameCoroutine());
    }

    private IEnumerator EndGameCoroutine()
    {
        yield return new WaitForSeconds(waitStateTime);

        #region comment
        // We do not want our players in the room to synch scene with the master client player. Remember that we made this true in ServerLauncher.cs
        #endregion
        PhotonNetwork.AutomaticallySyncScene = false;
        #region comment
        // This will trigger OnLeftRoom() event that we override above. Players will go back to main menu after this is called.
        #endregion
        PhotonNetwork.LeaveRoom();
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
