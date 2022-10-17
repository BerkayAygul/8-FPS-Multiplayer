using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Transform playerViewPoint;
    public float mouseSensitivity = 1f;
    // How much rotation we want our viewpoint to have. How much rotation we wanted to have to look up and down.
    // We can limit how much movement we have up and down.
    private float verticalRotationStore;
    // When we move horizontally, it is the X movement of the mouse and when we move vertically it is the Y movement of the mouse.
    private Vector2 mouseInput;
    public bool invertLook;

    void Start()
    {
        // The mouse shouldn't fly around the place where we're moving around. It should only move anywhere within the window.
        // Confined = the cursor can't move outside the window.
        // Locked = the cursor gets locked to the center of the screen and makes the mouse disappear completely.
        // Note: If we press the escape button, the cursor will be free in the editor (not in the final build).
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;

        // Quarternion.Euler basically means we can treat this as a vector 3 object, so we can just affect it based on the x, y, z.
        // eulerAngles are basically means the vector 3 version of the rotation angles.
        // We do not want to change x and z values. Leave it whatever the transform rotation currently is.
        // The player will be able to look right and left.
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);

        // We do not want to change y and z values. Leave it whatever the transform rotation currently is.
        // The player will be able to look up and down.
        // The view is currently inverted, so we have to fix this by taking the minus value of the verticalRotationStore value.
        /* The camera can flip, so we have to fix this by clamping the value between some values (to set it between certain degrees).
        // But because we are dealing with quaternions, there is another issue.
        // When we look up a little bit, once the x rotation value gets below zero, the rotation steps back to 60. */
        /* We can't directly clamp the x value of playerviewpoint. Behind the scenes of unity and its internal systems, we can
        // see the actual values of rotation by using Debug.log. We have to clamp the value by using verticalRotationStore. */
        // Flipping the camera should be optional, so we will use a bool value to make it optional.

        verticalRotationStore = verticalRotationStore + mouseInput.y;
        verticalRotationStore = Mathf.Clamp(verticalRotationStore, -60, 60);

        if(invertLook == true)
        {
            playerViewPoint.rotation = Quaternion.Euler(verticalRotationStore, playerViewPoint.rotation.eulerAngles.y, playerViewPoint.rotation.eulerAngles.z);
        }
        else
        {
            playerViewPoint.rotation = Quaternion.Euler(-verticalRotationStore, playerViewPoint.rotation.eulerAngles.y, playerViewPoint.rotation.eulerAngles.z);
        }


        /* Wrong use:
         * playerViewPoint.rotation = Quaternion.Euler(Mathf.Clamp(playerViewPoint.rotation.eulerAngles.x - mouseInput.y, -60f, 60f)
        */

        
    }
}
