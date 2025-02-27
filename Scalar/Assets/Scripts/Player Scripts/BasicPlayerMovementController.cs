using UnityEngine;

public class BasicPlayerMovementController : MonoBehaviour
{
    public PlayerController playerController;
    public WallRunController wallRunController;
    public TeleportController teleportController;
    public PlayerScalingController playerScalingController;
    public canPlayerMantle canPlayerMantle;
    public Transform orientation;
    public Transform headPosition;
    public Transform cameraPosition;
    public Rigidbody rb;
    public ParticleSystem speedParticles;

    [Header("Keybinds")]
    public KeyCode jumpKey;
    public KeyCode walkKey;

    [Header("Basic Movement")]
    public float airMultiplier;

    public bool IsMoving { get; private set; } = false;
    public bool IsGrounded { get; private set; } = false;
    public bool IsCrouching { get; private set; } = false;
    public float CurrentMovementSpeed { get; private set; } = 0f;

    private float horizontalMovement = 0f;
    private float verticalMovement = 0f;
    private float yAxisOfMovement = 0f;
    private float movementMultiplier = 20f;
    public float noClipSpeed = 50f;

    public bool noClip = false;

    [Header("Sprinting")]
    public float walkSpeed;
    public float sprintSpeed;
    public float trueSprintSpeed = 12;
    public float acceleration;

    [Header("Jumping")]
    public float jumpForce;

    [Header("Drag and Gravity")]
    public float groundDrag;
    public float airDrag;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public LayerMask groundMask;
    public float groundDistance;

    private Vector3 moveDirection = Vector3.zero;
    private Vector3 slopeMoveDirection = Vector3.zero;
    private RaycastHit slopeHit; // out
    //private Vector3 jumpMoveDirection; May need this in the future
    public Vector3 conveyorForce = new Vector3(0, 0, 0);


    private void Update()
    {
        if (noClip)
        {
            horizontalMovement = Input.GetAxisRaw("Horizontal");
            verticalMovement = Input.GetAxisRaw("Vertical");
            yAxisOfMovement = Input.GetAxisRaw("Up and Down");

            moveDirection = orientation.forward * verticalMovement + orientation.right * horizontalMovement + orientation.up * yAxisOfMovement;

            rb.drag = groundDrag;

            return;
        }
        IsGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask); //is grounded check

        //runs myinput function and control drag (more over this further down)
        SetMoveDirectionFromInput();
        ControlDrag();
        ControlSpeed();

        //allows the player to jump if they are on the ground and presses the jump key
        if (Input.GetKeyDown(jumpKey) && IsGrounded)
        {
            Jump();
        }

        if (!IsGrounded && canPlayerMantle.canMantle)
        {
            Mantle();
        }

        //sets the direction following the slope
        slopeMoveDirection = Vector3.ProjectOnPlane(moveDirection, slopeHit.normal);

        //move direction so the player can control inair movement
        //jumpMoveDirection = moveDirection * 0.1f;

        //gets the player's height by getting the scale of the Rigidbody and doubling as default is 1
        groundDistance = playerController.PlayerHeight / 8;
        airMultiplier =  1 * playerController.PlayerHeight / 2 + 1;
    }

    private void FixedUpdate()
    {
        wallRunController.isNoClipEnabled = noClip; // toggles gravity and wall running.
        playerScalingController.IsNoClipEnabled = noClip; // toggles scaling on and off.
        teleportController.IsPlayerNoClipping = noClip;

        if (noClip)
        {
            rb.AddForce(moveDirection.normalized * CurrentMovementSpeed * noClipSpeed * playerController.PlayerHeight / 2, ForceMode.Acceleration);

            Transform collider = transform.Find("Capsule");
            CapsuleCollider capsuleCollider = collider.GetComponent<CapsuleCollider>();

            capsuleCollider.enabled = false;

            return;
        }
        else
        {
            Transform collider = transform.Find("Capsule");
            CapsuleCollider capsuleCollider = collider.GetComponent<CapsuleCollider>();

            capsuleCollider.enabled = true;

            //calls the Move player function
            MovePlayer();
        }
    }

    void Mantle()
    {
        rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y <= 0 ? 4f : rb.velocity.y , rb.velocity.z);
        rb.AddForce(orientation.forward * 6f, ForceMode.Impulse);
        rb.AddForce(orientation.up * 2f, ForceMode.Impulse);
    }

    //when called it will grab movement input
    void SetMoveDirectionFromInput()
    {
        //grabs key inputs
        horizontalMovement = Input.GetAxisRaw("Horizontal");
        verticalMovement = Input.GetAxisRaw("Vertical");

        //combines both inputs into one direction
        moveDirection = orientation.forward * verticalMovement + orientation.right * horizontalMovement;
    }

    //when called then drag will be controlled
    void ControlDrag()
    {
        //drag is controlled if in air or ground so the player is not slow falling
        if (IsGrounded)
        {
            rb.drag = groundDrag;
        }
        else
        {
            rb.drag = airDrag / playerController.PlayerHeight / 2;
        }
    }

    void ControlSpeed()
    {
        if (!wallRunController.isWallRunning || IsGrounded) //DO NOT TOUCH IF STATEMENT UNLESS YOU WANT TO REWORK BOTH THIS AND WALL RUNNING SCRIPTS
        {
            float ySpeed = rb.velocity.y;

            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);

            // clamps to max speed
            if (rb.velocity.magnitude > sprintSpeed * playerController.PlayerHeight / 2)
            {
                rb.velocity = rb.velocity.normalized * sprintSpeed * playerController.PlayerHeight / 2;
            }

            rb.velocity = new Vector3(rb.velocity.x, ySpeed, rb.velocity.z);

            if (rb.velocity.magnitude > walkSpeed) speedParticles.Play(); else speedParticles.Stop();

            if (IsGrounded && Input.GetAxis("Vertical") > 0) // if moving forward
            {
                CurrentMovementSpeed = Mathf.Lerp(CurrentMovementSpeed, sprintSpeed, acceleration * Time.deltaTime / 2);

                IsMoving = true;
            }
            else if (IsGrounded && Mathf.Abs(Input.GetAxis("Vertical")) > 0 && Mathf.Abs(Input.GetAxis("Horizontal")) > 0) // else if moving another direction
            {
                CurrentMovementSpeed = Mathf.Lerp(CurrentMovementSpeed, walkSpeed, acceleration * Time.deltaTime / 2);

                IsMoving = true;
            }
            else if (Input.GetAxis("Vertical") == 0 && Input.GetAxis("Horizontal") == 0)
            {
                CurrentMovementSpeed = walkSpeed / 2;

                IsMoving = false;
            }
            sprintSpeed = trueSprintSpeed;
        }
        else if (wallRunController.isWallRunning)
        {
            // CurrentMovementSpeed = walkSpeed * airDrag;
        }
    }

    void Jump() //when called then the player will jump in the air
    {
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.AddForce(transform.up * jumpForce * playerController.PlayerHeight / 2, ForceMode.Impulse); //add a jump force to the rigid body component.
    }

    //when called it will send a raycast out and return is true if the vector does not return stright up
    private bool IsOnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerController.PlayerHeight)) // might need to make this value scale with player later
        {
            if (slopeHit.normal != Vector3.up)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        return false;
    }

    // when called then the rigid body will get force applyed
    void MovePlayer()
    {
        if (IsGrounded && !IsOnSlope()) // if on the ground but not on a slope
        {
            // walk speed on flat ground
            rb.AddForce(moveDirection.normalized * CurrentMovementSpeed * movementMultiplier * playerController.PlayerHeight / 2, ForceMode.Acceleration);
        }
        else if (IsGrounded && IsOnSlope()) // if the player is on the ground and on a slope
        {
            // walk speed on slope
            rb.AddForce(slopeMoveDirection.normalized * CurrentMovementSpeed * movementMultiplier * playerController.PlayerHeight / 2, ForceMode.Acceleration);
        }
        else if (!IsGrounded) // if the player is not on the ground
        {
            if (!wallRunController.isWallRunning)
            {
                // jumping in mid air force with a downwards force
                rb.AddForce(moveDirection.normalized * CurrentMovementSpeed * (playerController.PlayerHeight > 2 ? 0.9f : playerController.PlayerHeight), ForceMode.Acceleration);
            }
            else
            {
                // jumping in mid air force with a downwards force on a wall
                rb.AddForce(moveDirection.normalized * CurrentMovementSpeed * airMultiplier * (playerController.PlayerHeight > 2 ? 0.9f : playerController.PlayerHeight) / 100, ForceMode.Acceleration);
            }


            if (noClip) return; // stops gravity.

            //Increased gravity
            if (!wallRunController.isWallRunning && !teleportController.IsPlayerNoClipping)
                rb.AddForce(Physics.gravity * playerController.PlayerHeight * 9f);
            else if (!teleportController.IsPlayerNoClipping)
                rb.AddForce(Physics.gravity * playerController.PlayerHeight * 9f);
        }

        rb.transform.position += conveyorForce;
        conveyorForce = new Vector3(0, 0, 0);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(groundCheck.position, groundDistance);
    }

}
// this seems to be the end... or it's just the begining.
