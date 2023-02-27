using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using TMPro;

public class RoomBrowse : MonoBehaviour
{
    public TMP_Text roomButtonText;

    #region comment
    // This is how we are going to store information about the room that our players are joining.
    #endregion
    private RoomInfo roomInfo;

    #region comment
    /* This is going to be a function that we call from our ServerLauncher.cs
    ** We are going to create copies of room buttons and store each one with room information.
    ** We want to tell this function basically what information we want to pull in.
    */
    #endregion
    public void SetButtonDetails(RoomInfo inputInfo)
    {
        roomInfo = inputInfo;

        #region comment
        // From the information that we just received, get the name of the room here.
        #endregion
        roomButtonText.text = roomInfo.Name;
    }

    #region comment
    // We are going to send room information of each button to ServerLauncher.cs
    #endregion
    public void OpenRoom()
    {
        ServerLauncher.instance.JoinRoom(roomInfo);
    }

}
