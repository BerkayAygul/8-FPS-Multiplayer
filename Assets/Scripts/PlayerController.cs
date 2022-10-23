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
    public float runSpeed = 8f;
    private float activeMoveSpeed;
    private Vector3 moveDirection;
    private Vector3 movement;

    /* A CharacterController allows you to easily do movement constrained by collisions without having to deal with a rigidbody.
       A CharacterController is not affected by forces and will only move when you call the Move function. 
       It will then carry out the movement but be constrained by collisions.
       We are going to use this to move our player around instead of the transform.position.
       One other thing about the Character Controller is it allows us to deal with things like slopes very handly.
       Another thing about the Character Controller is it has a limit for the amount of slope (degree) it can handle (can be adjusted).
       45 degrees (default) is a reasonable slope limit that a player is able to walk up.
    */
    public CharacterController characterController;

    // When the camera updates the player, the head of the player moves slightly and gets a kind of jittery movement effect. 

    /* When the player gets killed, we have no camera displaying on our screen. 
       So the camera is no longer the child object of the player. 
       We will just tell our camera to move to the view point every frame (will be updated later). */
    /* When the player dies, it is going to be deleted from the scene then we are going to create a new player,
       so we don't want to have to assign the camera manually to that player whenever we create a new one, because
       we won't be able to do that when the game is running. So we are going to create a private camera reference. */
    private Camera camera;

    private float yVelocity;

    public float jumpForce = 10f;
    public float gravityMod = 2.5f;

    // This is going to be the point in space that we want this invisible line to go down.
    public Transform groundCheckPoint;
    // We are going to need to store whether we are on the ground or not.
    private bool isGrounded;
    // By using UI, we are going to create a new user layer called "Ground_Layer" and assign it to Environment's layer.
    // Then we are going to assign Ground_Layer to the newly created LayerMask.
    public LayerMask groundLayers;

    void Start()
    {
        // The mouse shouldn't fly around the place where we're moving around. It should only move anywhere within the window.
        // Confined = the cursor can't move outside the window.
        // Locked = the cursor gets locked to the center of the screen and makes the mouse disappear completely.
        // Note: If we press the escape button, the cursor will be free in the editor (not in the final build).
        Cursor.lockState = CursorLockMode.Locked;

        camera = Camera.main;
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

        if(Input.GetKey(KeyCode.LeftShift))
        {
            activeMoveSpeed = runSpeed;
        }
        else
        {
            activeMoveSpeed = moveSpeed;
        }


        // When we start applying gravity to our player, we are going to make some changes to the y value of the movement.    
        // We don't want movement to get multiplied by the value of the y-axis of activeMoveSpeed.
        // We shouldn't use activeMoveSpeed in characterController.Move().
        // ----------------------------------------------------------------------------------------------------------------------------
        // Add gravity to y axis of movement.
        // A problem causes fall down speed to be really slow.
        /* When we are moving the player around in our movement phase here, we are controlling the z and x axes.
           But for both of those they have a default value for Y of zero. So what's happening here is both of these are
           setting the y movement value to be zero. Then we're taking away gravity.
           As things happen in the real world, when you start to fall, you start falling slowly,
           but you fall faster and faster over time.
           So what we need to do is make sure that the Y value for these isn't reset. 
           We have to store the current Y movement that we have, which will be the velocity of our player.
           We are going to create a disposable float value that we will call y velocity, before applying any movement. */
        /* If we open debug, we can see that y velocity is continually growing faster and faster, we do not want our player
           constantly falling down to the ground like that. We are going to add an extra check to fix that issue. */
        float yVelocity = movement.y;
        movement = ((transform.forward * moveDirection.z) + (transform.right * moveDirection.x)).normalized * activeMoveSpeed;
        movement.y = yVelocity;
        if(characterController.isGrounded)
        {
            // y movement should be 0 if the player is on the ground.
            movement.y = 0;
        }

        // Jump
        /* The ‘get button’ functions work similarly to the ‘get key’ functions, except you cannot use KeyCodes, 
           own buttons in the input manager.This is the recommended by Unity and can be quite powerful as it allows 
           developers to map custom joystick / D-pad buttons. */
        /* It feels odd how fast the player falls. Although we have represented real world gravity, it still feels slow.
           So what are we going to do is modify the amount of gravity that's beign applied to our player. 
           Multiply movement.y with gravityMod*/
        /* The player is able to jump whenever he presses the button. 
           The player should only jump whenever the player is on the ground.
           It's not always 100 percent consistent with is.grounded check (we can check is.grounded with Debug.Log).
           We are also going to create a recast, which will basically create a line that goes straight down from the player
           (a certain distance from the player), and it will check within this distance is the ground here. */
        /* A raycast does an invisible line and checks if it interacts with anything along that line.
           If it hits anything along the invisible line, isGrounded is true, otherwise it is false.
           First parameter = where do we want our raycast to start,
           second parameter = what position our raycast is going to start,
           third parameter = how long we want it to go (adjust the value if the player can't jump on the edges),
           fourth parameter = the layer we are want to check.
         */
        
        isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, 1.6f, groundLayers);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            movement.y = jumpForce;
        }

        movement.y += Physics.gravity.y * Time.deltaTime * gravityMod;
        characterController.Move(movement * Time.deltaTime);
        //transform.position += movement * moveSpeed * Time.deltaTime;
        
    }

    // We need to make sure that the player has moves before we update the camera.
    // LateUpdate() basically happens after Update().
    private void LateUpdate()
    {
        camera.transform.position = playerViewPoint.position;
        camera.transform.rotation = playerViewPoint.rotation;
    }
}
