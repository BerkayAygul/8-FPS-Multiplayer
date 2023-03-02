using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviour
{
    #region comment
    // This is a class that we want to have acces from other scripts.
    #endregion
    public static PlayerSpawner instance;

    private void Awake()
    {
        instance = this;
    }

    #region comment
    /* We want to instantiate something from the Resources folder. We need the name of the object. But rather than store it as a string,
    ** we are going to create a reference for the Player Prefab. 
    */
    #endregion
    public GameObject playerPrefab;

    #region comment
    // We are going to store player because we want this script to control our players dying and beign destroyed, so we can remove them.
    #endregion
    private GameObject player;

    public GameObject deathEffect;

    public float respawnTime = 5f;

    void Start()
    {
        #region comment
        /* We need to check and make sure we are connected to the network. Because we won't be able to spawn the player
        ** if we are not connected to the network. This is only really going to be a problem when we are testing ourselves.
        ** But it is good to just do it here as well. */
        #endregion
        if(PhotonNetwork.IsConnected)
        {
            SpawnPlayer();
        }
    }

    public void SpawnPlayer()
    {
        Transform spawnPoint = SpawnManager.instance.GetSpawnPoint();
        #region comment
        // Instantiate Player Prefab over the network.
        #endregion
        player = PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
    }

    public void DestroyPlayer(string whoKilledThePlayer)
    {
        UIController.instance.deathText.text = "You were killed by " + whoKilledThePlayer;

        #region comment
        // We need to send the dead player's actor number to update the player's death stat.
        #endregion
        MatchManager.instance.UpdateStatsEventSend(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);

        #region comment
        // The coroutine will start running and wait a certain amount of time in a seperate thread on the CPU, and Unity will continue doing other things.
        // We need to make sure that the player doesn't actually destroyed twice or something else happen in the time between dying and respawning.
        #endregion
        if(player != null)
        {
            #region comment
            // We need to remember that we are only doing this if the photonView.Ismine in PlayerController.
            #endregion
            StartCoroutine(DeathCoroutine());
        }
    }   

    public IEnumerator DeathCoroutine()
    {
        #region comment
        // Create the death effect when the player is dead.
        #endregion
        PhotonNetwork.Instantiate(deathEffect.name, player.transform.position, Quaternion.identity);

        #region comment
        // Destroy the player over the network then spawn the player.
        #endregion
        PhotonNetwork.Destroy(player);

        UIController.instance.deathScreen.SetActive(true);

        #region comment
        // yield means wait and yield return means what value we are sending back to the yield.
        #endregion
        yield return new WaitForSeconds(respawnTime);

        UIController.instance.deathScreen.SetActive(false);

        SpawnPlayer();
    }
}
