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
        #region comment
        // We are going to add a new event type for starting a new match.
        #endregion
        #region comment
        /* When a new player enters the room in middle of the match, his timer starts counting down from the start.
        ** So, the timer must be synced over the network with regard to the master player. */
        #endregion
        NewPlayerEvent,
        ListPlayersEvent,
        UpdateStatsEvent,
        StartNextMatchEvent,
        SyncTimeEvent
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

    #region comment
    /* We are going to make sure we have the option of whether we want to have our game continue going or not.
    ** So, We are going to add a bool value and use it in EndGameCoroutine() */
    #endregion
    public bool perpetualContinue;

    #region comment
    /* Instead of ending the game by a max score, we can create a time length to end the game.
    ** We are going to use these values in SetupTimer(). 
    ** Call the related function in Start() and countdown the time in Update() */
    #endregion
    public float matchTimeLength = 60;
    private float currentMatchTime;

    #region comment
    // We are going to use this variable in an event to send the current time to everyone and synch the timer over the network, on a constant basis.
    #endregion
    private float sendTimer = 1f;

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

            SetupTimer();

            #region comment
            /* We are going to deactivate the timer by default, so the players don't see the timer immediately at the start of the match. 
            ** For the most of the time, it is going to be activated anyway but we are going to activate it in SyncTimeReceive(). */
            #endregion
            if (!PhotonNetwork.IsMasterClient)
            {
                UIController.instance.matchTimerText.gameObject.SetActive(false);
            }
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

        #region comment
        /* If the match is started to play and the remaining time is greater than zero, countdown the time.
        ** If the remaining time is zero, end the match. */
        #endregion
        #region comment
        /* The master client will countdown the time and inform every other player in the room about the time. */
        #endregion
        #region comment
        /* For development testing, a one second timer difference can happen between the Unity and built game. 
        ** The difference is just the time it takes to be sent over the network and back into the machine.
        ** It is not really a problem because players won't be playing two games on the same machine. */
        #endregion
        #region comment
        /* currentMatchTime > 0f creates a problem that we can't get into the if statement at all and doesn't end the game when the timer reaches to 00:00
        ** We need to make it >= 0f. */
        #endregion
        if (PhotonNetwork.IsMasterClient)
        {
            if (currentMatchTime >= 0f && currentGameState == GameState.GamePlayingState)
            {
                currentMatchTime -= Time.deltaTime;

                if (currentMatchTime <= 0f)
                {
                    currentMatchTime = 0f;

                    currentGameState = GameState.GameEndingState;

                    #region comment
                        // We need to remember that ListPlayerEvent keeps the players updated about the game state.
                        #endregion
                    ListPlayerEventSend();
                    #region comment
                    // ListPlayerEvent already checks the current game state.
                    #endregion
                    //CurrentGameStateCheck(); 
                }

                #region comment
                // Update the timer display as long as we are counting the remaining time in every frame.
                #endregion
                UpdateTimerDisplay();

                #region comment
                // The reason we don't just directly set this to be one is because things might get out of sync slightly. 
                #endregion
                sendTimer -= Time.deltaTime;
                if(sendTimer <= 0)
                {
                    sendTimer += 1;

                    SyncTimeEventSend();
                }
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
                #region comment
                // We do not need to send or receive a package data, we just need to restart the match.
                #endregion
                case EventCodes.StartNextMatchEvent:
                    StartNextMatchEventReceive();
                    break;
                case EventCodes.SyncTimeEvent:
                    SyncTimeEventReceive(receivedData);
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

        #region comment
        /* When the player dies, the player's camera didn't get set to be the map camera quickly,
        ** because the player dies before the event is recieved.
        ** If the player gets destroyed, we are guaranteeing that the camera will move to that overview position. */
        #endregion
        Camera.main.transform.position = mapOverviewCameraPoint.position;
        Camera.main.transform.rotation = mapOverviewCameraPoint.rotation;

        StartCoroutine(EndGameCoroutine());
    }

    private IEnumerator EndGameCoroutine()
    {
        yield return new WaitForSeconds(waitStateTime);

        #region comment
        // If we do not want to continue the game instantly, we make the player leave the room.
        #endregion
        if(!perpetualContinue)
        {
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
        // The master client is going to start the next match.
        #endregion
        else
        {
            if(PhotonNetwork.IsMasterClient)
            {
                #region comment
                /* We are not going to change our map if we do not want to. We are just going to restart our game. 
                ** If we want to change our map after the end of the game, we can make it randomly. */
                #endregion
                if(!ServerLauncher.instance.changeMapBetweenMatches)
                {
                    StartNextMatchEventSend();
                }
                else
                {
                    int newMapToLoad = Random.Range(0, ServerLauncher.instance.allMapsList.Length);

                    #region comment
                    /* We are going to make a check because, if we just reload the same map again we do not need to waste our time with that.
                    ** When we randomly get the same map again, call the next match function. (or we can change to another map, if want to). */
                    #endregion
                    if(ServerLauncher.instance.allMapsList[newMapToLoad] == SceneManager.GetActiveScene().name)
                    {
                        StartNextMatchEventSend();
                    }
                    else
                    {
                        PhotonNetwork.LoadLevel(ServerLauncher.instance.allMapsList[newMapToLoad]);
                    }
                }
            }
        }
    }

    public void StartNextMatchEventSend()
    {
        #region comment
        // We do not need to send a package, so we are going to replace it with null. 
        #endregion
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.StartNextMatchEvent,
            null,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
            );
    }

    public void StartNextMatchEventReceive()
    {
        #region comment
        // We need to do some changes when we start a new game instantly.
        #endregion
        currentGameState = GameState.GamePlayingState;

        UIController.instance.matchEndScreen.SetActive(false);
        UIController.instance.leaderboardTableDisplay.SetActive(false);

        #region comment
        /* This will be running on all our clients, so every player is going to set their own records
        ** back to zero. So we do not have to send that information out. */
        #endregion
        foreach (PlayerInformation player in allPlayersList)
        {
            player.playerKills = 0;
            player.playerDeaths = 0;
        }

        #region comment
        // We need to update the stats manually for all players.
        #endregion
        UpdateStatsDisplay();

        #region comment
        // We need to spawn all players manually. We are going to make some changes in SpawnPlayer().
        #endregion
        PlayerSpawner.instance.SpawnPlayer();
    }

    public void SetupTimer()
    {
        #region comment
        /* We need to be make sure of the timer is only set up if the match length is greater than zero. 
        ** So, if the creator of the room didn't set up a match timer, maximum kill count score is going to work. */
        #endregion
        if(matchTimeLength > 0)
        {
            currentMatchTime = matchTimeLength;
            UpdateTimerDisplay();
        }
    }

    #region comment
    /* We need to decide how we are going to send this to timer text in UI controller.
    ** We are going to use something called a TimeSpan. TimeSpan is something that is built into the C# systems
    ** that allows us to create something that stores a time. Although our currentMatchTime is a float value,
    ** we can convert it into a TimeSpan value and that will allow us to display the match time.
    ** This function is called in Start() */
    #endregion
    public void UpdateTimerDisplay()
    {
        #region comment
        /* Instead of creating a new System.TimeSpan object with a name, we are going to use a var value.
        ** Whenever we create a variable that is just for temporary use like this, we can create a var value.
        ** var value will become whatever we are setting it to. */
        #endregion
        var timeToDisplay = System.TimeSpan.FromSeconds(currentMatchTime);
        #region comment
        // Formatting the text like 00:00
        #endregion
        UIController.instance.matchTimerText.text = timeToDisplay.Minutes.ToString("00") + ":" + timeToDisplay.Seconds.ToString("00");
    }

    #region comment
    /* Rather than have each individual player count down the time, we can just have the master client to handle counting down.
    ** Then each player will just set themselves to be whatever time the master client tells them. We are going to use this in Update(). */
    #endregion
    public void SyncTimeEventSend()
    {
        #region comment
        /* We are going to send a package which contains the current time. 
        ** We are going to convert the time value to an int value because all we really need to know about is
        ** what is the whole number (second).
        ** We are also going to send off our current game state so that we are able to keep our state constantly updated as well. */
        #endregion
        object[] package = new object[] { (int)currentMatchTime, currentGameState };

        PhotonNetwork.RaiseEvent
            (
            (byte)EventCodes.SyncTimeEvent,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
            );
    }

    public void SyncTimeEventReceive(object[] receivedData)
    {
        #region comment
        // Although our currentMatchTime is a float value, we can pull the received time as an int and it will just set it to a float value.
        #endregion
        currentMatchTime = (int)receivedData[0];
        currentGameState = (GameState)receivedData[1];

        #region comment
        // After we receive the time, we are going to update the timer display to reflect what we have.
        #endregion
        UpdateTimerDisplay();

        #region comment
        // We already deactivated the timer in Start(). We should activate it in here.
        #endregion
        UIController.instance.matchTimerText.gameObject.SetActive(true);
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
