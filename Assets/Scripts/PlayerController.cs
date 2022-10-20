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

    public float moveSpeed = 5f;
    private Vector3 moveDirection;
    private Vector3 movement;

    /* A CharacterController allows you to easily do movement constrained by collisions without having to deal with a rigidbody.
       A CharacterController is not affected by forces and will only move when you call the Move function. 
       It will then carry out the movement but be constrained by collisions.
       We are going to use this to move our player around instead of the transform.position.
       One other thing about the Character Controller is it allows us to deal with things like slopes very handly.
    */
    public CharacterController characterController;
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


        // Time.deltaTime is how long it takes for each frame update to happen. 
        // If our game is running at 60 frames a second, this will be one divided by 60.
        /* If we said we could make our move speed much smaller, that would work but at different frame rates, the player would move
        ** at different speeds. */
        /* If we look sideways and press forward, the player will still go in the same direction that we had before, which is wrong.
        ** We are going to use transform.forward and transform.right to fix this. */
        // We are moving a lot faster when we move in a diagonal action. We can fix that by normalizing the vector.
        moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        movement = ((transform.forward * moveDirection.z) + (transform.right * moveDirection.x)).normalized;
        characterController.Move(movement * moveSpeed * Time.deltaTime);
        //transform.position += movement * moveSpeed * Time.deltaTime;
        
    }
}
