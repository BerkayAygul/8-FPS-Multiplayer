using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks
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
    // Call the value from GunAttributes.cs instead. Each weapon has different values.
    #endregion
    //public float timeBetweenShots = 0.1f;
    #region comment
    // We use this to handle shooting more than one shot; 
    #endregion
    private float shotCounter;

    #region comment
    // Instead of using ammo, we are going to use weapon overheating.
    // 1- The maximum level our heat can go up to. 
    // 2- Heat adding up per shot.
    // 3- How fast the meter will go back down to zero.
    // 4- If the gun overheats, how fast the meter will go back down to zero.
    // 5- This is going to keep track of what we currently have.
    // 6- This is going to keep track of if the weapon is overheated. 
    #endregion
    public float maxOverheat = 10f;
    #region comment
    // How long it's going to take between each shot;
    // Call the value from GunAttributes.cs instead. Each weapon has different values.
    #endregion
    //public float overheatPerShot = 1f;
    public float coolDownRate = 4f;
    public float overheatCoolDownRate = 5f;
    private float overheatCounter;
    private bool overheated;

    #region comment 
    //These values are going to be used to switch between weapons.
    #endregion
    public GunAttributes[] allGuns;
    private int selectedGun;

    #region comment
    /* VSync keeps the frame rate at a steady amount relative to what the screen is displaying.
       When it is turned on, it is very easy to keep track of what is displayed.
       If it is turned off, suddenly the frame rate can get really high and we might not able to see muzzle flashes on the correct basis.
       We are going to use a float value to wait a tiny fraction of time and then deactivate the muzzle flash.
       The display time value of the muzzle flash should last roughly one sixtieth of a second because our game is targeting running
       60 frames a second.
     */
    #endregion
    public float muzzleFlashDisplayTime = 0.01666667f;
    private float muzzleFlashCounter;

    public GameObject playerHitImpact;

    public int playerMaxHealth = 100;
    #region comment
    // We are going to use this value to track the player's current health. We need to make sure of that current health is at max value when at Start() function.
    #endregion
    private int currentPlayerHealth;

    #region comment
    // Player animator reference, we have two parameters that are called speed and grounded.
    #endregion
    public Animator playerAnimator;

    #region comment
    // We are going to disable the player model on local so players do not see their own bodies. Assign characterMedium as the reference of this value.
    #endregion
    public GameObject playerModel;

    public Transform modelGunPoint;
    public Transform gunHolder;

    #region comment
    // We are going to make a list of our available player skins. We are going to use this in Start().
    #endregion
    public Material[] playerSkinsList;

    #region comment
    // Aim down sight speed. How quickly the player zooms in and out. We are going to make the weapon aim down sight system in Update(). 
    #endregion
    public float adsSpeed;
    public Transform adsZoomOutPoint;
    public Transform adsZoomInPoint;
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

        #region comment
        // Slider is going to be at the maximum value (no overheat).
        #endregion
        UIController.instance.overheatSlider.maxValue = maxOverheat;

        #region comment
        // All weapons are disabled up on start. Use this function to activate the first gun.
        #endregion
        #region comment
        // We do not need this anymore since we use a RPC call.
        #endregion
        //SwitchWeapon();
        photonView.RPC("SetGun", RpcTarget.All, selectedGun);

        currentPlayerHealth = playerMaxHealth;

        #region comment
        // Use photonview.Ismine to show the health of the players individually.
        #endregion
        if (photonView.IsMine)
        {
            UIController.instance.healthSlider.maxValue = playerMaxHealth;
            UIController.instance.healthSlider.value = playerMaxHealth;

            #region comment
            // If I own the player model, disable it so that I can't see it.
            #endregion
            playerModel.SetActive(false);
        }
        else
        {
            gunHolder.parent = modelGunPoint;
            gunHolder.localPosition = Vector3.zero;
            gunHolder.localRotation = Quaternion.identity;
        }

        #region comment
        /* Whenever the player starts playing, we want to get a spawn point from the spawn manager and move the player to that point. */
        #endregion
        #region comment
        /* We do not need to use these anymore since we are using PlayerSpawner.cs.
        ** We do not want each player to get their spawn point themselves. */
        #endregion
        /*
        /* Transform playerSpawnPoint = SpawnManager.instance.GetSpawnPoint();
        ** transform.position = playerSpawnPoint.position;
        ** transform.rotation = playerSpawnPoint.rotation; */

        #region comment
        /* To make it simple, we are going to assign player skins according to each player's actor number. 
        ** A room can contain a maximum of eight players so we should have eight different skins. 
        ** Because the player actor number starts from one, so we will obviously have a problem with the eighth (the last) player.
        ** Also, we can have a situation where if someone leaves the current ongoing match and someone else joins in, then they
        ** will become the ninth actor and it will go like that so on. 
        ** So we are going to make a modulo calculation to assign skins.
        ** Skins will be repeatedly taken afterwards the last player skin in the list is taken. */
        #endregion
        playerModel.GetComponent<Renderer>().material = playerSkinsList[photonView.Owner.ActorNumber % playerSkinsList.Length];
    }

    void Update()
    {
        #region comment
        // We are going to use Photon View information before doing anything. So we can be sure of controlling one player.
        #endregion
        if (photonView.IsMine)
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

            if (invertLook == true)
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

            if (Input.GetKey(KeyCode.LeftShift))
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
            if (characterController.isGrounded)
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
            #region comment
            //transform.position += movement * moveSpeed * Time.deltaTime; 
            #endregion
            movement.y += Physics.gravity.y * Time.deltaTime * gravityMod;
            characterController.Move(movement * Time.deltaTime);

            #region comment
            // Deactivate the muzzle flash object before firing.
            // We are doing this here specifically because we want to activate muzzle flash for one whole frame of the game.
            #endregion
            if (allGuns[selectedGun].muzzleFlash.activeInHierarchy)
            {
                muzzleFlashCounter -= Time.deltaTime;
                if (muzzleFlashCounter <= 0)
                {
                    allGuns[selectedGun].muzzleFlash.SetActive(false);
                }
            }

            #region comment
            // We want to shoot only if the weapon isn't overheated. 
            #endregion
            if (overheated == false)
            {
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
                if (Input.GetMouseButton(0) && allGuns[selectedGun].isAssaultRifle)
                {
                    shotCounter -= Time.deltaTime;

                    if (shotCounter <= 0)
                    {
                        #region comment
                        // shotCounter resets to the value of timeBetweenShots; 
                        #endregion
                        Shoot();
                    }
                }
                overheatCounter -= coolDownRate * Time.deltaTime;
            }
            #region comment
            // If the weapon is overheated. 
            #endregion
            else
            {
                overheatCounter -= overheatCoolDownRate * Time.deltaTime;
                if (overheatCounter <= 0)
                {
                    #region comment
                    // In case it goes down zero. Does not work because before we start shooting, the counter is already going below zero.
                    // overheatCounter = 0; 
                    #endregion
                    overheated = false;
                    #region comment
                    // Deactivate the message if the weapon isn't overheated anymore.
                    #endregion
                    UIController.instance.overheatedMessage.gameObject.SetActive(false);
                }
            }

            #region comment
            // No matter what, it musn't go below zero. 
            #endregion
            if (overheatCounter < 0)
            {
                overheatCounter = 0f;
            }

            #region comment
            // Current overheat value.
            #endregion
            UIController.instance.overheatSlider.value = overheatCounter;

            #region comment
            // If the player scrolls the mouse wheel upwards or downwards, change the selectedGun value and call SwitchWeapon() function.
            #endregion
            if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
            {
                selectedGun++;
                if (selectedGun >= allGuns.Length)
                {
                    selectedGun = 0;
                }
                //SwitchWeapon();
                photonView.RPC("SetGun", RpcTarget.All, selectedGun);
            }
            else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
            {
                selectedGun--;
                if (selectedGun <= 0)
                {
                    selectedGun = allGuns.Length - 1;
                }
                //SwitchWeapon();
                photonView.RPC("SetGun", RpcTarget.All, selectedGun);
            }

            #region comment
            /* We want to make our player be able to switch weapons with keyboard keys since it will be hard to switch weapons with mouse scroll
            ** if the player is using a laptop. */
            #endregion
            for (int i = 0; i < allGuns.Length; i++)
            {
                #region comment
                // We do not want the player to press 0 on keyboard. So if the player presses number 1 key, we get the 0th element in the array. 
                #endregion
                if (Input.GetKeyDown((i + 1).ToString()))
                {
                    selectedGun = i;
                    //SwitchWeapon();
                    photonView.RPC("SetGun", RpcTarget.All, selectedGun);
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
            else if (Cursor.lockState == CursorLockMode.None)
            {
                #region comment
                // To prevent mouse clicking issues between the game and in-game menu, we should not lock the mouse cursor when the in-game menu is open.
                #endregion
                if (Input.GetMouseButtonDown(0) && !UIController.instance.inGameMenu.activeInHierarchy)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }

            #region comment
            /* Aim down sight when pressing the right mouse key. 
            ** What will happen here is it'll just snap into position straight away. It will go from one zoom 
            ** to the other instantly. So that would not feel great. Instead, we want to gently zoom into it. 
            ** So we are going to use Mathf.Lerp() and move between one point to another point gradually slowing over time. 
            ** Start from the camera field of view, go to the given adsZoom value, by adsSpeed * Time.deltaTime speed. */
            #endregion
            #region comment
            /* We are also going to create two different view points for players to view the weapon in the middle of the screen when the player zoom. 
            ** Then we are going to create references to both of those points. Then we are going to move our gun position to one of those points. */
            #endregion
            if (Input.GetMouseButton(1))
            {
                camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, allGuns[selectedGun].adsZoom, adsSpeed * Time.deltaTime);
                gunHolder.position = Vector3.Lerp(gunHolder.position, adsZoomInPoint.position, adsSpeed * Time.deltaTime);
            }
            #region comment
            // Go back to the normal field of view, 60 is the default field of view value.
            #endregion
            else
            {
                camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, 60f, adsSpeed * Time.deltaTime);
                gunHolder.position = Vector3.Lerp(gunHolder.position, adsZoomOutPoint.position, adsSpeed * Time.deltaTime);
            }

            #region comment
            // We are going to use our isGrounded bool value for the grounded parameter's bool value to control the animation.
            #endregion
            playerAnimator.SetBool("grounded", isGrounded);
            #region comment
            /* The animator has a transation from idle to running if the speed paramater is greater then 0.01.
            ** We can't just use a axis value of the moveDirection because, for example if we move to the left on the x axis, the speed would be less than zero. 
            ** So obviously, it would never trigger our run animation. 
            ** However, moveDirection is a Vector 3 value, we can instead use the magnitude value of the vector.*/
            #endregion
            playerAnimator.SetFloat("speed", moveDirection.magnitude);
        }
    }

    #region comment
    // When the camera updates the player, the head of the player moves slightly and gets a kind of jittery movement effect. 
    // We need to make sure that the player has moves before we update the camera.
    // LateUpdate() basically happens after Update(). 
    #endregion
    private void LateUpdate()
    {
        #region comment
        // We are going to use photon view information to move the camera for each indivual player.
        #endregion
        if(photonView.IsMine)
        {
            #region comment
            // If the game is playing, camera view should be on the player view point. Else, camera view should overview the map.
            #endregion
            if(MatchManager.instance.currentGameState == MatchManager.GameState.GamePlayingState)
            {
                camera.transform.position = playerViewPoint.position;
                camera.transform.rotation = playerViewPoint.rotation;
            }
            else
            {
                #region comment
                /* We know that PlayerController.cs is going to be destroyed when we get to the end game state, but we need to note that we are doing this in
                ** LateUpdate() function, this is going to work before things on the network get destroyed. */
                #endregion
                camera.transform.position = MatchManager.instance.mapOverviewCameraPoint.position;
                camera.transform.rotation = MatchManager.instance.mapOverviewCameraPoint.rotation;
            }
        }
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
            // If we hit a player, we need to create a different impact effect. Note that we do not need a rotation for this at the moment.
            #endregion
            if(raycastHit.collider.gameObject.tag == "Player")
            {
                #region comment
                // Debug.Log("Hit " + raycastHit.collider.gameObject.GetPhotonView().Owner.NickName);
                #endregion
                PhotonNetwork.Instantiate(playerHitImpact.name, raycastHit.point, Quaternion.identity);

                #region comment
                // Call this function for every player. We can send the information of the player who is responsible for this function.
                #endregion
                #region comment
                // After we set up update stat event in MatchManager.cs, we need to send in damager player's actor number to DealDamage() RPC function.
                #endregion
                raycastHit.collider.gameObject.GetPhotonView().RPC("DealDamage", RpcTarget.All, photonView.Owner.NickName, allGuns[selectedGun].gunDamage, PhotonNetwork.LocalPlayer.ActorNumber);
            }
            #region comment
            // If we hit anything different than players, create a bullet impact effect.
            #endregion
            else
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


        }

        shotCounter = allGuns[selectedGun].timeBetweenShots;
        overheatCounter += allGuns[selectedGun].heatPerShot;

        if(overheatCounter >= maxOverheat)
        {
            #region comment
            // In case it did go above the maximum value. 
            #endregion
            overheatCounter = maxOverheat;
            overheated = true;

            #region comment
            // If the weapon is overheated, alert the player.
            #endregion
            UIController.instance.overheatedMessage.gameObject.SetActive(true);
        }
        #region comment
        // Create a muzzle flash after firing the weapon (just before the function ends) at the display time value.
        #endregion
        allGuns[selectedGun].muzzleFlash.SetActive(true);
        muzzleFlashCounter = muzzleFlashDisplayTime;
    }

    #region comment
    // Before switching the weapon, deactive the current weapon. Then activate the newly selected gun.
    #endregion
    private void SwitchWeapon()
    {
        foreach (GunAttributes gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }

        allGuns[selectedGun].gameObject.SetActive(true);
        #region comment
        // We want to make sure of muzzle flash doesn't occur when we switch the weapon.
        #endregion
        allGuns[selectedGun].muzzleFlash.SetActive(false);
    }

    #region comment
    /* We are going to call this function when we do some damage to a player. 
    ** When we call this function PUNRPC will run this function on every copy of the player on the network. 
    ** The way we call this function is slightly different. */
    #endregion
    [PunRPC]
    public void DealDamage(string whoDealtDamage, int damageAmount, int damagerPlayerActorNumber)
    {
        if(photonView.IsMine)
        {
            #region comment
            // When we add a parameter variable, the RPC doesn't automatically know how many variables it should be passing in. We need to add it to the RPC.
            #endregion
            #region comment
            // After we set up update player stats event, we need to get the damager player's actor number and send it to TakeDamage().
            #endregion
            TakeDamage(whoDealtDamage, damageAmount, damagerPlayerActorNumber);
        }
    }

    public void TakeDamage(string whoDealtDamage, int damageAmount, int damagerPlayerActorNumber)
    {
        #region comment
        // We do not want to take damage to all the machines all the time.
        // If the photon view is the player's and the player is hit, destroy the player.
        #endregion
        if (photonView.IsMine)
        {
            currentPlayerHealth -= damageAmount;

            if(currentPlayerHealth <= 0)
            {
                currentPlayerHealth = 0;
                PlayerSpawner.instance.DestroyPlayer(whoDealtDamage);

                #region comment
                /* When the player is dead, we know which player dealth the damage by the actor number, we update the damager player's kill stat.
                ** To update the death stat of the dead player, we need to make changes in PlayerSpawner.cs */
                #endregion
                MatchManager.instance.UpdateStatsEventSend(damagerPlayerActorNumber, 0, 1);
            }

            UIController.instance.healthSlider.value = currentPlayerHealth;
        }
    }

    #region comment
    // We are going to use RPC to switch weapons. So everyone on the network can see if a player switch his weapons.
    #endregion
    [PunRPC]
    public void SetGun(int gunToSwitchTo)
    {
        #region comment
        // We are doing this to make sure we don't receive any weird errors.
        #endregion
        if(gunToSwitchTo < allGuns.Length)
        {
            selectedGun = gunToSwitchTo;
            SwitchWeapon();
        }
    }
}
