using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LeaderboardPlayerInformation : MonoBehaviour
{
    #region comment
    // We are going to use this script to get player information values from MatchManager.cs and then add this information to the leaderboard table.
    #endregion
    public TMP_Text playerNameText;
    public TMP_Text playerKillsText;
    public TMP_Text playerDeathsText;
    public void SetPlayerLeaderboardInformation(string playerName, int playerKills, int playerDeaths)
    {
        playerNameText.text = playerName;
        playerKillsText.text = playerKills.ToString();
        playerDeathsText.text = playerDeaths.ToString();
    }
}
