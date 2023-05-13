using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;

public class UIController : MonoBehaviour
{
    #region comment
    /* In PlayerController.cs we can write FindObjectOfType<UIController>().overheatedMessage.gameObject.SetActive(true);
       That will work perfectly fine but we do not want to have to do this every single time we play our game.
       We could get the object of the UIController just like we did for the camera but because we are going to 
       have players being destroyed and instantiated and we are going to have multiple players in our game,
       we don't want them to have to be constantly doing this in their Start() function.
       Instead, we can make UI Controller a singleton object.*/
    #endregion
    public static UIController instance;

    public TMP_Text overheatedMessage;
    public Slider overheatSlider;

    public GameObject deathScreen;
    public TMP_Text deathText;

    public Slider healthSlider;

    public TMP_Text killStatText;
    public TMP_Text deathStatText;

    public GameObject leaderboardTableDisplay;
    public LeaderboardPlayerInformation LeaderboardPlayerInformation;

    public GameObject matchEndScreen;

    public TMP_Text matchTimerText;

    public GameObject inGameMenu;

    private void Awake()
    {
        instance = this;    
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            ShowAndHideInGameMenu();
        }

        #region comment
        // When we are in the game, we know that cursor is invisible and locked. So, we need to free the cursor to use the in-game menu.
        #endregion
        #region comment
        // To prevent mouse clicking errors, we are going to make some changes in Update() in PlayerController.cs, where we handle the mouse locking.
        #endregion
        if(inGameMenu.activeInHierarchy && Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    #region comment
    // We are going to use this function to show or hide the in-game menu by pressing the escape key on keyboard. 
    #endregion
    public void ShowAndHideInGameMenu()
    {
        if(!inGameMenu.activeInHierarchy)
        {
            inGameMenu.SetActive(true);
        }
        else
        {
            inGameMenu.SetActive(false);
        }
    }

    #region comment
    // We already know that in our MatchManager.cs we have a system that directs the player to the main menu when the player leaves the game.
    #endregion
    public void ReturnToMainMenu()
    {
        #region comment
        // Make sure that the player doesn't sync scene with anyone else.
        #endregion
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.LeaveRoom();
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
