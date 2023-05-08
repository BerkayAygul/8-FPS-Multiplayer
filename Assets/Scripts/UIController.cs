using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

    private void Awake()
    {
        instance = this;    
    }
}
