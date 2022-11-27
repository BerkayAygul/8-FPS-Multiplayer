using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    #region comment
    // We do this so we can reach to the spawn points from PlayerController.cs
    #endregion
    public static SpawnManager instance;

    private void Awake()
    {
        instance = this;
    }

    #region comment
    /* We need a reference to all spawn points. The only thing we are concerned with is the transform of the spawn points.
    ** Because we want to know that position they are in and what rotation they have.
    ** We are going to assign these transform to a transform array. 
    ** We are going to use these transform points in PlayerController.cs */
    #endregion
    public Transform[] spawnPoints;

    void Start()
    {
        #region comment
        /* As soon as the game starts, deactivate those objects (spawn point bodies that we see in the scene) 
        ** so we can no longer see them in the world. We do not remove them so we can use those points to spawn players. */
        #endregion
        foreach (Transform spawnLocation in spawnPoints)
        {
            spawnLocation.gameObject.SetActive(false);
        }
    }

    #region comment
    // We want to get a random transform from the array and reach it from PlayerController.cs
    #endregion
    public Transform GetSpawnPoint()
    {
        #region comment
        /* There is an important thing to be aware of with Random.Range(). 
         * When we are dealing with float values:
        ** float Random.Range(float minInclusive, float maxInclusive)
        ** When we are dealing with int values:
        ** int Random.Range(int minInclusive, int maxExclusive)
        ** What minInclusive and maxExclusive mean is it could potentially be zero but it will never actually be the max value.
        ** So, Random.Range() will never reach the length of spawnPoints[] and this is good for us since we do not have 
        ** any element with that number.
        ** For example, the array has 7 elements but since index numbers starts with 0, we actually have 8 spawn points assigned to them. */
        #endregion
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }
}
