using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Transform playerViewPoint;
    public float mouseSensitivity = 1f;
    #region comment
    // How much rotation we want our viewpoint to have. How much rotation we wanted to have to look up and down.
    // We can limit how much movement we have up and down. 
    #endregion
    private float verticalRotationStore;
    #region comment
    // When we move horizontally, it is the X movement of the mouse and when we move vertically it is the Y movement of the mouse. 
    #endregion
    private Vector2 mouseInput;
    public bool invertLook;

    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    private float activeMoveSpeed;
    private Vector3 moveDirection;
    private Vector3 movement;

    #region comment
    /* A CharacterController allows you to easily do movement constrained by collisions without having to deal with a rigidbody.
       CharacterController is not affected by forces and will only move when you call the Move function. 
       It will then carry out the movement but be constrained by collisions.
       We are going to use this to move our player around instead of the transform.position.
       One other thing about the Character Controller is it allows us to deal with things like slopes very handly.
       Another thing about the Character Controller is it has a limit for the amount of slope (degree) it can handle (can be adjusted).
       45 degrees (default) is a reasonable slope limit that a player is able to walk up.
    */
    #endregion
    public CharacterController characterController;

    #region comment
    /* When the player gets killed, we have no camera displaying on our screen. 
       So the camera is no longer the child object of the player. 
       We will just tell our camera to move to the view point every frame (will be updated later). 
    */
    /* When the player dies, it is going to be deleted from the scene then we are going to create a new player,
       so we don't want to have to assign the camera manually to that player whenever we create a new one, because
       we won't be able to do that when the game is running. So we are going to create a private camera reference. 
    */
    #endregion
    private Camera camera;

    private float yVelocity;

    public float jumpForce = 10f;
    public float gravityMod = 2.5f;

    #region comment
    // This is going to be the point in space that we want this invisible line to go down. 
    #endregion
    public Transform groundCheckPoint;
    #region comment
    // We are going to need to store whether we are on the ground or not. 
    #endregion
    private bool isGrounded;
    #region comment
    // By using UI, we are going to create a new user layer called "Ground_Layer" and assign it to Environment's layer.
    // Then we are going to assign Ground_Layer to the newly created LayerMask. 
    #endregion
    public LayerMask groundLayers;

    public GameObject bulletImpact;
    #region comment
    // How long it's going to take between each shot; 
    #endregion
    public float timeBetweenShots = 0.1f;
    #region comment
    // We use this to handle shooting more than one shot; 
    #endregion
    private float shotCounter;

    void Start()
    {
        #region comment
        // The mouse shouldn't fly around the place where we're moving around. It should only move anywhere within the window.
        // Confined = the cursor can't move outside the window.
        // Locked = the cursor gets locked to the center of the screen and makes the mouse disappear completely.
        // Note: If we press the escape button, the cursor will be free in the editor (not in the final build).

        #endregion        
        Cursor.lockState = CursorLockMode.Locked;

        camera = Camera.main;
    }

    void Update()
    {
        mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;

        #region comment
        // Quarternion.Euler basically means we can treat this as a vector 3 object, so we can just affect it based on the x, y, z.
        // eulerAngles are basically means the vector 3 version of the rotation angles.
        // We do not want to change x and z values. Leave it whatever the transform rotation currently is.
        // The player will be able to look right and left. 
        #endregion
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);

        #region comment
        // We do not want to change y and z values. Leave it whatever the transform rotation currently is.
        // The player will be able to look up and down.
        // The view is currently inverted, so we have to fix this by taking the minus value of the verticalRotationStore value.
        /* The camera can flip, so we have to fix this by clamping the value between some values (to set it between certain degrees).
        // But because we are dealing with quaternions, there is another issue.
        // When we look up a little bit, once the x rotation value gets below zero, the rotation steps back to 60. */
        /* We can't directly clamp the x value of playerviewpoint. Behind the scenes of unity and its internal systems, we can
        // see the actual values of rotation by using Debug.log. We have to clamp the value by using verticalRotationStore. */
        // Flipping the camera should be optional, so we will use a bool value to make it optional. 
        #endregion

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


        #region comment
        /* Wrong use:
           playerViewPoint.rotation = Quaternion.Euler(Mathf.Clamp(playerViewPoint.rotation.eulerAngles.x - mouseInput.y, -60f, 60f)
        */
        #endregion


        #region comment
        // Time.deltaTime is how long it takes for each frame update to happen. 
        // If our game is running at 60 frames a second, this will be one divided by 60.
        /* If we said we could make our move speed much smaller, that would work but at different frame rates, the player would move
        ** at different speeds. */
        /* If we look sideways and press forward, the player will still go in the same direction that we had before, which is wrong.
        ** We are going to use transform.forward and transform.right to fix this. */
        // We are moving a lot faster when we move in a diagonal action. We can fix that by normalizing the vector. 
        #endregion
        moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

        if(Input.GetKey(KeyCode.LeftShift))
        {
            activeMoveSpeed = runSpeed;
        }
        else
        {
            activeMoveSpeed = moveSpeed;
        }

        #region comment

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
           We are going to create a disposable float value that we will call y velocity, before applying any movement. 
        */
        /* If we open debug, we can see that y velocity is continually growing faster and faster, we do not want our player
           constantly falling down to the ground like that. We are going to add an extra check to fix that issue. 
        */
        #endregion
        float yVelocity = movement.y;
        movement = ((transform.forward * moveDirection.z) + (transform.right * moveDirection.x)).normalized * activeMoveSpeed;
        movement.y = yVelocity;
        if(characterController.isGrounded)
        {
            #region comment
            // y movement should be 0 if the player is on the ground. 
            #endregion
            movement.y = 0;
        }

        #region comment
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
        #endregion

        isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, .5f, groundLayers);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            movement.y = jumpForce;
        }

        movement.y += Physics.gravity.y * Time.deltaTime * gravityMod;
        characterController.Move(movement * Time.deltaTime);
        #region comment
        //transform.position += movement * moveSpeed * Time.deltaTime; 
        #endregion

        #region comment
        // If the player presses the left mouse button, shoot. 
        #endregion
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }

        #region comment
        // If we are holding the left mouse click down. 
        #endregion
        if (Input.GetMouseButton(0))
        {
            shotCounter -= Time.deltaTime;

            if(shotCounter <= 0)
            {
                #region comment
                // shotCounter resets to the value of timeBetweenShots; 
                #endregion
                Shoot();
            }
        }

        #region comment
        // If the player presses the escape button, free the cursor.
        // If the player presses the left mouse button, lock the cursor. 
        #endregion
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else if(Cursor.lockState == CursorLockMode.None)
        {
            if(Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    #region comment
    // When the camera updates the player, the head of the player moves slightly and gets a kind of jittery movement effect. 
    // We need to make sure that the player has moves before we update the camera.
    // LateUpdate() basically happens after Update(). 
    #endregion
    private void LateUpdate()
    {
        camera.transform.position = playerViewPoint.position;
        camera.transform.rotation = playerViewPoint.rotation;
    }

    #region comment
    /* Think of how fast a bullet moves. Generally speaking, in reality, when you shoot a bullet, you would not see the actual bullet.
       You see the bullet being fired and you see where it hit and both of these things happen almost in the same time.
       That is how most games deal with it, there are no bullets flying through the air.
       The game calculates where the point is that you hit against.
       We are going to do a raycast to implement this idea.
    */
    #endregion
    private void Shoot()
    {
        #region comment
        // Pick a point within the camera, go from that point and go straight forward from the direction the camera is facing.
        // x: .5f, y: .5f, halfway across on the x-axis and halfway up on the y-axis. That will get us the exact center point. 
        #endregion
        Ray bulletRay = camera.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
        #region comment
        // The origin point for that ray. 
        #endregion
        bulletRay.origin = camera.transform.position;

        #region comment
        /* Basically, a RaycastHit is the result of the Raycast.
           If the raycast detects something ahead of us when we shoot forward, it will store whatever information it finds in this
           raycastHit object and it will send it back out.
        */
        #endregion
        if (Physics.Raycast(bulletRay, out RaycastHit raycastHit))
        {
            #region comment
            //Debug.Log("Hit - " + raycastHit.collider.gameObject.name); 
            #endregion

            #region comment
            // Create a bullet impact when we detect a hit.
            // We want the bullet impact to go wherever we hit.
            /* The rotation we want to have is whatever the surface is that we hit.
               So, we want to face the direction that the surface we hit has. We are going to use the normal of the objects for that.
               A normal is the direction that each particular face of an object is pointing.
               raycastHit.normal would not work because it gives us a vector three value, but for rotations we need to use a quaternion.
               We can use quaternion.LookRotation() and basically it tells us which direction the object should look when it gets
               instantiated. So, the LookRotation() will be whatever our raycastHit.normal is and we need to tell it what is the
               upwards direction of the world that we're in at the moment.
            */

            /* We get a flickering effect because when we shoot our little objects out, it hits against the wall and it goes to the
               exact same point in space as the surface of the object. When unity wants to draw the objects in the world, it says:
               "This impact effect is right here but also the object that is hit is at the exact same position." So unity doesn't
               know which one should go in front of the other. To fix that, we can move the object slightly away from the surface by
               multiplying the normal value by a tiny value and then adding it with raycastHit.point.
            */

            // We don't want our impact effects to stay in the world forever because they are taking up memory.

            /* If we start adding a whole bunch of the bullet impact objects, we can keep layering them on top of each other and that
               is because when we created them, the quads themselves have a mesh collider attached to them. We need to remove it.
            */
            #endregion

            GameObject bulletImpactObject = Instantiate(bulletImpact, raycastHit.point + (raycastHit.normal * .002f), Quaternion.LookRotation(raycastHit.normal, Vector3.up));
            Destroy(bulletImpactObject, 10f);
        }

        shotCounter = timeBetweenShots;
    }
}
