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
}
