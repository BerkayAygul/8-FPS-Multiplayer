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

    public void DestroyPlayer()
    {
        #region comment
        // Create the death effect when the player is dead.
        #endregion
        PhotonNetwork.Instantiate(deathEffect.name, player.transform.position, Quaternion.identity);

        #region comment
        // Destroy the player over the network then spawn the player.
        #endregion
        PhotonNetwork.Destroy(player);
        SpawnPlayer();
    }
}
