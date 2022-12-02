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

    public GameObject errorPanel;
    public TMP_Text errorText;

    public GameObject roomBrowserPanel;
    public RoomBrowse theRoomButton;
    #region comment
    // We can change the length of a list dynamically. We are going to add our buttons to this list when a room gets created.
    #endregion
    public List<RoomBrowse> allRoomButtons = new List<RoomBrowse>();
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
    }
    void CloseMenus()
    {
        loadingPanel.SetActive(false);
        menuButtons.SetActive(false);
        createRoomPanel.SetActive(false);
        insideRoomPanel.SetActive(false);
        errorPanel.SetActive(false);
        roomBrowserPanel.SetActive(false);
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
    }

    #region comment
    // After we join the lobby, we can navigate ourselves through several buttons.
    #endregion
    public override void OnJoinedLobby()
    {
        CloseMenus();
        menuButtons.SetActive(true);
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
    ** As we can see, the function takes a list of the room info's that are currently available. 
    ** We want to make sure that every time we go out and list out all the rooms for the players to see, we want to destroy the previous    
    ** versions of these buttons. */
    #endregion
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomBrowse rb in allRoomButtons)
        {
            Destroy(rb.gameObject);
        }

        allRoomButtons.Clear();

        #region comment
        // We are always going to create a copy of this inactive room button.
        #endregion
        theRoomButton.gameObject.SetActive(false);

        #region comment
        /* If there are maximum players in a room, then don't list the room.
        ** If everybody leaves the room, the room becomes empty and it's no longer accessiable, so we are going to remove it from the list 
        ** So, if our rooms are not removed from the list, then we are allowed to display it. Create a new room and set it's details. */ 
        #endregion
        for (int i = 0; i < roomList.Count; i++)
        {
            if (roomList[i].PlayerCount != roomList[i].MaxPlayers && !roomList[i].RemovedFromList)
            {
                RoomBrowse newButton = Instantiate(theRoomButton, theRoomButton.transform.parent);
                newButton.SetButtonDetails(roomList[i]);
                newButton.gameObject.SetActive(true);

                #region comment
                // Add the new room button to the list
                #endregion
                allRoomButtons.Add(newButton);
            }
        }
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
}
