using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunAttributes : MonoBehaviour
{
    #region comment
    // After attaching this script, change these values from the inspector. 
    #endregion
    public bool isPistol;
    public bool isAssaultRifle;
    public bool isSniperRifle;
    public float timeBetweenShots = .1f;
    public float heatPerShot = 1f;

    #region comment
    // Create a muzzle flash after firing the weapon.
    #endregion
    public GameObject muzzleFlash;

    public int gunDamage;

    #region comment
    /* Value for aiming down sight the weapon (zoom float value).
    ** We are going to assign this value for each different weapon we want to zoom. The lesser value zooms more. 
    ** We are going to use this value in PlayerController.cs */
    #endregion
    public float adsZoom;
}
