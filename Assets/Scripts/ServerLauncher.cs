using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;

public class ServerLauncher : MonoBehaviourPunCallbacks
{
    #region comment
    /* There is a couple of things to be aware of when we are using our Photon networking to connect.
    ** One of them is that we need to include Photon.Pun library.
    ** Also, we need to use MonoBehaviourPunCallbacks base class instead of MonoBehaviour.
    ** MonoBehaviourPunCallbacks allows us to access those different default functions for working with Photon network. */
    #endregion

    #region comment
    // We know there's only one of these in our game at any time because there is only one main menu that would be used to connect to things.
    #endregion
    public static ServerLauncher instance;

    private void Awake()
    {
        instance = this;
    }

    #region comment
    /* We are going to add some references that we can use. Then we are going to attach ServerLauncher.cs to Menu Canvas.
    ** Then we will attach the related objects from Unity Inspector.
    ** Also, we need to disable Menu Buttons and Loading Panel by default. */
    #endregion
    public GameObject loadingPanel;
    public GameObject menuButtons;
    #region comment
    // We need to use TMPro library to reach TextMeshPro texts.
    #endregion
    public TMP_Text loadingText;

    #region comment
    // Whatever name the player has typed in the TMP_InputField will be the name of the room.
    #endregion
    public GameObject createRoomPanel;
    public TMP_InputField roomNameInput;

    public GameObject insideRoomPanel;
    public TMP_Text roomNameText;
    public TMP_Text playerNameLabel;
    private List<TMP_Text> allPlayerNicknames = new List<TMP_Text>();

    public GameObject errorPanel;
    public TMP_Text errorText;

    public GameObject roomBrowserPanel;
    public RoomBrowse theRoomButton;
    #region comment
    // We can change the length of a list dynamically. We are going to add our buttons to this list when a room gets created.
    #endregion
    public List<RoomBrowse> allRoomButtons = new List<RoomBrowse>();

    private Dictionary<string, RoomInfo> cachedRoomsList = new Dictionary<string, RoomInfo>();

    public GameObject createNicknamePanel;
    public TMP_InputField nicknameInput;
    #region comment
    // If the player has set a nickname, we don't want to go through the create nickname panel again.
    #endregion
    //private bool hasSetNickname;
    #region comment
    /* We are going to make nickname a static variable so no matter what this bool value will be the same.
    ** When we go back to the main menu, this static bool will always retain the value that we had before. */
    #endregion
    public static bool hasSetNickname;

    #region comment
    // This will be the name of the level we want to go.
    #endregion
    public string levelNameToPlay;
    public GameObject startGameButton;

    #region comment
    /* We are going to be building this game and connect a lot. So, to cut down on our time we are going to create a test button. */
    #endregion
    public GameObject roomTestButton;

    #region comment
    /* We are going to add our available playable maps to a list by giving scene names as inputs. Then add a bool value to make it optional.
    ** We can play on other maps randomly when we start the game. 
    ** To make other maps playable after the end of a match, we are going to make some changes in MatchManager.cs, EndGameCoroutine()
    ** We are going to make some changes in StartGame(). */
    #endregion
    public string[] allMapsList;
    public bool changeMapBetweenMatches = true;

    void Start()
    {
        #region comment
        /* What we want to do is, as soon as the game starts, we want to put the loading panel up on to the players and make the text say
        ** "We're connecting to the network". And then obviously behind the scenes we want to start connecting to the network.
        ** And then once we become connected, we want to close the loading panel and open up the menu buttons.
        ** Now as we go forward, we are going to add more and more menus into this whole scene that we have here. So we want a way that we can
        ** make sure that when we open a new menu that all the other menus are closed as well.
        ** So, first thing we are going to do is create a void CloseMenus() */
        #endregion
        CloseMenus();

        loadingPanel.SetActive(true);
        loadingText.text = "Connecting To Network...";

        #region comment
        /* Uses the settings that we set up in PhotonServerSettings in Photon > PhotonUnityNetworking > Resources folder to connect.
        ** We set up our App Id Pun value that we get from Photon Pun website. */
        #endregion
        PhotonNetwork.ConnectUsingSettings();

#if UNITY_EDITOR
        roomTestButton.SetActive(true);
#endif
        #region comment
        // Make sure that players can move the mouse cursor when the game ends and when they go back to the main menu.
        #endregion
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

    }
    void CloseMenus()
    {
        loadingPanel.SetActive(false);
        menuButtons.SetActive(false);
        createRoomPanel.SetActive(false);
        insideRoomPanel.SetActive(false);
        errorPanel.SetActive(false);
        roomBrowserPanel.SetActive(false);
        createNicknamePanel.SetActive(false);
    }

    #region comment
    /* We need to override the function that exists in Photon's memory and do something in here that we want to do.
    ** Let's say that our player wants to connect to the Photon PUN network. So we get connected to the master server.
    ** But, when we get connection to the master server, it still doesn't know what is inside the master server.
    ** So basically it says, I'm connected, so what are we going to do now?
    ** Once it gets here, we want to connect to some kind of a lobby. This lobby will allows us to find and connect any rooms on the server.
    ** And each of these rooms are where matches takes place. We are going to set up it up so we can have eight players in a room.
    */
    #endregion
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
        loadingText.text = "Joining Lobby...";

        #region comment
        /* One thing we want to be able to do is when we click the start game button, load into a different scene.
        ** Also when we click the start game button, we want to tell all the other players that we're all loading into this next scene.
        ** After we join the lobby we are going to say AutomaticallySyncScene = true, and this will allow Photon Network to be able to
        ** tell us which scene we should be going to. So whether we are joining a room or if we are the one controlling the room and
        ** telling to start the game, this will control the scene that we are actually going to. Note that this isn't going to be enough
        ** to play the game concurrently because all the other players are going to play their own seperate version of the game. */
        #endregion
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    #region comment
    // After we join the lobby, we can navigate ourselves through several buttons.
    #endregion
    public override void OnJoinedLobby()
    {
        cachedRoomsList.Clear();
        CloseMenus();
        menuButtons.SetActive(true);

        #region comment
        // Players will have a random numerical nickname when they join the lobby.
        #endregion
        PhotonNetwork.NickName = Random.Range(0, 1000).ToString();

        if(!hasSetNickname)
        {
            CloseMenus();
            createNicknamePanel.SetActive(true);

            if(PlayerPrefs.HasKey("playerName"))
            {
                nicknameInput.text = PlayerPrefs.GetString("playerName");
            }
        }
        #region comment
        // If we did set up a nickname but we want to change it, we need to make sure it's staying the correct value.
        #endregion
        else
        {
            PhotonNetwork.NickName = PlayerPrefs.GetString("playerName");
        }

    }

    public override void OnLeftLobby()
    {
        cachedRoomsList.Clear();
    }

    #region comment
    /* We are going to use this function to create open the create room panel.
    ** We are going to assign this as an OnClick() to the Create Room Button on the Menu Panel. */
    #endregion
    public void OpenRoomCreate()
    {
        CloseMenus();
        createRoomPanel.SetActive(true);
    }

    #region comment
    /* We are going to use this function to create a new room.
    ** We are going to assign this as an OnClick() to the Create Room Button on the Create Room Panel. 
    ** If the player hasn't typed in a name, then we shouldn't let them create a room.
    ** Also, we want to limit the maximum number of players in a room to eight. */
    #endregion
    public void CreateRoom()
    {
        if(!string.IsNullOrEmpty(roomNameInput.text))
        {
            #region comment
            // using Photon.Realtime
            #endregion
            RoomOptions options = new RoomOptions();
            options.MaxPlayers = 8;

            PhotonNetwork.CreateRoom(roomNameInput.text, options);

            CloseMenus();
            loadingText.text = "Creating Room...";
            loadingPanel.SetActive(true);
        }
    }

    #region comment
    // As soon as we join, get the current joined room name and show it to the player.
    #endregion
    public override void OnJoinedRoom()
    {
        CloseMenus();
        insideRoomPanel.SetActive(true);

        roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        ListAllPlayers();

        #region comment
        /* The first player that joins a room is called the master player, and if that player leaves, then master gets sent to another
        ** player that's in the room. Only the master player can actually start the game. If anyone else tries to start the game, 
        ** it'll just load them into their own world. So, we are going to create a referance to the start game button. We need to check
        ** and see if we are the master player. If we are, we're allowed to show that button and if we're not, we should hide the button. */
        #endregion
        if(PhotonNetwork.IsMasterClient)
        {
            startGameButton.SetActive(true);
        }
        else
        {
            startGameButton.SetActive(false);
        }
    }

    #region comment
    /* There is a possibility that something can go wrong. So when we connect to a room, sometimes the room won't be created correctly. 
    ** For example, one situvation that can happen is if we create a room and somebody already has that room name then we shouldn't be
    ** able to create that room. As soon as the Photon is concerned, we are trying to create a room that already exists so it won't
    ** create that room for us and it won't allow us to do anything and we get stuck. So what we are going to do is to create a 
    ** error panel. We are going to show the error message to the player that is sent from Photon. */
    #endregion
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        errorText.text = "Failed to create a room: " + message;
        CloseMenus();
        errorPanel.SetActive(true);
    }

    #region comment
    // The player should read the error message and then leave the error panel
    #endregion
    public void CloseErrorPanel()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    #region comment
    // The player should be able to leave the room.
    #endregion
    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        CloseMenus();
        loadingText.text = "Leaving Room";
        loadingPanel.SetActive(true);
    }
    #region comment
    // The player should go back to the main menu panel after they leave the room.
    #endregion
    public override void OnLeftRoom()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public void OpenRoomBrowser()
    {
        CloseMenus();
        roomBrowserPanel.SetActive(true);
    }

    public void CloseRoomBrowser()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    #region comment
    /* This function will be called at any time there is a change to any list of rooms when we are in the lobby.
    ** As we can see, the function takes a list of the room info's that are currently available. */
    #endregion
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        UpdateCachedRoomList(roomList);
    }

    public void UpdateCachedRoomList(List<RoomInfo> roomList)
    {
        #region comment
        // Get room info list and for every element, check if the room is removed. If the room isn't removed get it's name to dictionary.
        #endregion
        for (int i = 0; i < roomList.Count; i++)
        {
            RoomInfo info = roomList[i];
            if (info.RemovedFromList)
            {
                cachedRoomsList.Remove(info.Name);
            }
            else
            {
                cachedRoomsList[info.Name] = info;
            }
        }
        RoomListButtonUpdate(cachedRoomsList);
    }

    public void RoomListButtonUpdate(Dictionary<string, RoomInfo> cachedRoomList)
    {
        #region comment
        // Have to destroy every button object then add it again otherwise buttons will duplicate
        #endregion
        foreach (RoomBrowse rb in allRoomButtons)
        {
            Destroy(rb.gameObject);
        }
        allRoomButtons.Clear();

        theRoomButton.gameObject.SetActive(false);
        #region comment
        // Get the cached room dictionary and foreach room in this dictionary create a new button and add it to allRoomButtons list. 
        #endregion
        foreach (KeyValuePair<string, RoomInfo> roomInfo in cachedRoomList)
        {
            RoomBrowse newButton = Instantiate(theRoomButton, theRoomButton.transform.parent);
            newButton.SetButtonDetails(roomInfo.Value);
            newButton.gameObject.SetActive(true);
            allRoomButtons.Add(newButton);
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        cachedRoomsList.Clear();
    }
    #region comment
    // This is what we are going to use to connect to a room.
    #endregion
    public void JoinRoom(RoomInfo inputInfo)
    {
        PhotonNetwork.JoinRoom(inputInfo.Name);
        CloseMenus();
        loadingText.text = "Joining Room";
        loadingPanel.SetActive(true);
    }

    #region comment
    // Use this function in OnJoinedRoom() to display the players.
    #endregion
    private void ListAllPlayers()
    {
        #region comment
        // After joining the room, delete the array elements.
        #endregion
        foreach (TMP_Text playerNickname in allPlayerNicknames)
        {
            Destroy(playerNickname.gameObject);
        }
        allPlayerNicknames.Clear();

        #region comment
        /* The reason we do this is simply because we don't want to loop through this array constantly because this is something that
        ** we have to get information from the network. So that will add a delay everytime we're trying to get that information from
        ** the network. Instead, we just get the information once and store it in here then act on that information. 
        ** Get the list of the players and add it to a created Photon.Realtime.Player array. */
        #endregion
        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            TMP_Text newPlayerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent);
            newPlayerLabel.text = players[i].NickName;
            newPlayerLabel.gameObject.SetActive(true);

            #region comment
            // Add every new player label to the created array.
            #endregion
            allPlayerNicknames.Add(newPlayerLabel);
        }
    }

    #region comment
    /* The player list gets updated for only the person who joins the room later because we only use OnJoinedRoom().
    ** We are going to fix this buy using OnPlayerEnteredRoom() and OnPlayerLeftRoom() functions. */
    #endregion
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        TMP_Text newPlayerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent);
        newPlayerLabel.text = newPlayer.NickName;
        newPlayerLabel.gameObject.SetActive(true);
        allPlayerNicknames.Add(newPlayerLabel);

    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        #region comment
        /* Having to search through both of the PhotonNetwork.PlayerList and allPlayerNicknames lists and compare them to find who left
        ** the room takes a little bit of time and there is a much more simple way to do that. 
        ** When someone leaves the room, relist all the players. Because we destroy them all and add them again in this function. */
        #endregion
        ListAllPlayers();
    }

    public void SetNickname()
    {
        if(!string.IsNullOrEmpty(nicknameInput.text))
        {
            PhotonNetwork.NickName = nicknameInput.text;

            #region comment
            /* We do not want write our nickname everytime we open the game so we need to store nicknames in our system.
            ** To save small things like strings, we can use PlayerPrefs. */
            #endregion
            PlayerPrefs.SetString("playerName", nicknameInput.text);

            CloseMenus();
            menuButtons.SetActive(true);
            hasSetNickname = true;
        }
    }

    public void StartGame()
    {
        //PhotonNetwork.LoadLevel(levelNameToPlay);

        #region comment
        // Now that we have another map, we can make all of the maps playable randomly like this.
        #endregion
        PhotonNetwork.LoadLevel(allMapsList[Random.Range(0, allMapsList.Length)]);
    }

    #region comment
    // If the master player leaves the room or the game, we need to assign another player as master player and the master can start the game.
    #endregion
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            startGameButton.SetActive(true);
        }
        else
        {
            startGameButton.SetActive(false);
        }
    }

    public void QuickJoin()
    {
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 8;

        PhotonNetwork.CreateRoom("Test");
        CloseMenus();
        loadingText.text = "Creating Room";
        loadingPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
