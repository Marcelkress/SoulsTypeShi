using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using System.ComponentModel;
using UnityEngine.Events;
using Unity.Mathematics;

public class Player_Controller : MonoBehaviour
{   
    private Animator anim;

    // The input component on the player gameobject
    private PlayerInput playerInput;

    // The player actions
    private InputAction lookAction;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction attackAction;
    private InputAction lockOnAction;
    private Rigidbody rb;

    [Header("Attack")]
    [SerializeField] private float moveDelay;
    private bool canMove;
    private bool isAttacking;
    public int weaponDamage;
    public UnityEvent PlayerAttack;

    [Header("Move")]
    [SerializeField] private float walkSpeed;
    private Vector3 moveVector;
    [SerializeField] private float sprintSpeed;
    private bool sprinting;
    private float currentSpeed;
    [SerializeField] private float transTimeToSprint;
    private bool changingFloat;
    [SerializeField] private Transform characterTransform;
    [SerializeField] private float rotSpeed = .2f;
    private float animFloatX;
    private float animFloatY;
    private bool canRot;

    [Header("Look")]
    [SerializeField] private GameObject cameraBoom;
    [SerializeField] private float sensitivity;
    private Vector2 lookVector;
    private float yRotation;
    private float xRotation;
    
    [SerializeField] private int yMin = -85;
    [SerializeField] private int yMax = 85;

    [Header("Lock On")]
    [SerializeField] private Transform lockOnObject;
    [SerializeField] private float lockOnRadius, lockOnDistance;
    [SerializeField] private LayerMask targetLayer;
    private bool lockedOn = false;

    [Header("Jump")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float castDistance;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float waitForJump = .2f;
    private float timePassed;
    private bool canJump;


    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        

        lookAction = playerInput.actions["Look"];
        lookAction.performed += Look;

        moveAction = playerInput.actions["Move"];
        moveAction.performed += Move;
        moveAction.canceled += Move;

        jumpAction = playerInput.actions["Jump"];
        jumpAction.started += Jump;

        sprintAction = playerInput.actions["Sprint"];
        sprintAction.started += Sprint;
        sprintAction.canceled += Sprint;

        attackAction = playerInput.actions["Attack"];
        attackAction.started += Attack;

        lockOnAction = playerInput.actions["LockOn"];
        lockOnAction.started += LockOn;

    }

    private void Start()
    {
        canMove = true;
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        currentSpeed = walkSpeed;
        Cursor.lockState = CursorLockMode.Locked;
        changingFloat = false;
        canRot = true;
    }
 
    private void Look(InputAction.CallbackContext context)
    {
        lookVector = context.ReadValue<Vector2>() * sensitivity;
    }

    private void Move(InputAction.CallbackContext context)
    {
        moveVector = context.ReadValue<Vector2>();
    }
    private void Sprint(InputAction.CallbackContext context)
    {
        if(sprinting == false)
        {
            sprinting = true;
            currentSpeed = sprintSpeed;
            StartCoroutine(ChangeFloatOverTime(.5f, 1, transTimeToSprint));
        }
        else
        {
            sprinting = false;
            currentSpeed = walkSpeed;
            StartCoroutine(ChangeFloatOverTime(1, .5f, transTimeToSprint));
        }
    }

    private void FixedUpdate()
    {   
        PerformMove();

        if(moveVector != Vector3.zero || lockOnObject != null)
        {
            RotatePlayer();
        }

        SetAnimationParams();
    }

    private void SetAnimationParams()
    {
        if(IsGrounded() == false)
        {
            anim.SetFloat("X", 0f);
            anim.SetFloat("Y", 0f);
        }
        else if(sprinting == false)
        {
            anim.SetFloat("X", Mathf.Clamp(animFloatX, -.5f, .5f));

            if(changingFloat == false)
            {
                animFloatX = moveVector.x;
                animFloatY = moveVector.y;
                anim.SetFloat("Y", Mathf.Clamp(animFloatY, -.5f, .5f));
            }   
        }
        else if (sprinting == true)
        {
            animFloatX = moveVector.x;
            anim.SetFloat("X", animFloatX);

            if(changingFloat == false)
            {
                animFloatY = moveVector.y;
                animFloatX = moveVector.x;
                anim.SetFloat("Y", animFloatY);
            }
        }
    }

    private IEnumerator ChangeFloatOverTime(float initialValue, float targetValue, float duration)
    {
        changingFloat = true;

        float elapsedTime = 0f;
        animFloatY = initialValue;

        while (elapsedTime < duration)
        {
            animFloatY = Mathf.Lerp(initialValue, targetValue, elapsedTime / duration);
            anim.SetFloat("Y", animFloatY);

            elapsedTime += Time.deltaTime;

            yield return null;
        }
        
        yield return changingFloat = false;  
    }

    private void Update()
    {
        PerformLook();

        // Canceling sprint if player stops moving
        if(moveVector == Vector3.zero)
        {
            currentSpeed = walkSpeed;
            sprinting = false;
        }

        // Jumping cooldown
        timePassed += Time.deltaTime;
        if(timePassed > waitForJump)
        {
            canJump = true;
        }
    }

    private void PerformMove()
    {
        if(canMove == false)
            return;

        // Transform the movement vector to be relative to the camera's orientation
        Vector3 forward = cameraBoom.transform.forward;
        Vector3 right = cameraBoom.transform.right;

        // Ensure the forward and right vectors are horizontal
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        // Calculate the desired movement direction
        Vector3 desiredMoveDirection = forward * moveVector.y + right * moveVector.x;

        // Apply the movement
        rb.MovePosition(transform.position + currentSpeed * desiredMoveDirection * Time.deltaTime);
    }

    private void PerformLook()
    {
        if(lockOnObject != null)
        {
            cameraBoom.transform.LookAt(lockOnObject);
            return;
        }

        float mouseX = lookVector.x;
        float mouseY = lookVector.y;

        // Apply horizontal rotation to the player
        //cameraBoom.transform.Rotate(0f, mouseX, 0f);
        xRotation += mouseX;

        // Apply vertical rotation to the camera
        yRotation -= mouseY;
        yRotation = Mathf.Clamp(yRotation, yMin, yMax);
        cameraBoom.transform.localRotation = Quaternion.Euler(yRotation, xRotation, 0f);
    }

    private void Jump(InputAction.CallbackContext context)
    {
        if(IsGrounded() == true && canJump == true)
        {
            anim.SetTrigger("Jump");
            Vector3 jumpVector = new(rb.velocity.x, jumpForce, rb.velocity.z);
            rb.AddForce(jumpVector, ForceMode.Impulse);
            timePassed = 0f;
        }
    }

    private void Attack(InputAction.CallbackContext context)
    {
        if (isAttacking)
            return;

        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
            return;

        canRot = false;
        canMove = false;
        anim.SetTrigger("Attack");
        
        PlayerAttack?.Invoke();

        isAttacking = true;
        if (this != null) // Null check before starting the coroutine
        {
            StartCoroutine(ResetAttackState(moveDelay));
        }
    }

    private void RotatePlayer()
    {   
        if(lockOnObject != null)
        {
            Vector3 pointVector = lockOnObject.position - transform.position;
            pointVector.y = 0;
            pointVector.Normalize();

            StartCoroutine(SmoothRotate(pointVector));
            //characterTransform.LookAt(lockOnObject);
            return;
        }

        if(canRot == false)
            return;

        Vector3 forward = cameraBoom.transform.forward;
        Vector3 right = cameraBoom.transform.right;

        // Ensure the forward and right vectors are horizontal
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 desiredRotDir = forward * moveVector.y + right * moveVector.x;

        // If there's no input, don't change the rotation
        if (desiredRotDir.sqrMagnitude > 0.0f)
        {
            // Create the desired rotation
            // Apply the rotation to the character
            StartCoroutine(SmoothRotate(desiredRotDir));
        }
    }

    private IEnumerator SmoothRotate(Vector3 targetVector)
    {
        float elapsedTime = 0f;

        Quaternion targetRotation = Quaternion.LookRotation(targetVector);
        Quaternion initialRotation = characterTransform.localRotation;

        targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);

        while (elapsedTime < rotSpeed)
        {
            characterTransform.localRotation = Quaternion.Slerp(initialRotation, targetRotation, elapsedTime / rotSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the final rotation is set
        characterTransform.localRotation = targetRotation;
    }
    private IEnumerator ResetAttackState(float delay)
    {
        yield return new WaitForSeconds(delay);
        canRot = true;
        canMove = true;
        isAttacking = false;
    }

    private bool IsGrounded()
    {
        if(Physics.Raycast(transform.position, Vector3.down, castDistance, groundLayer))
            return true;
        else
            return false;
    }

    private void LockOn(InputAction.CallbackContext context)
    {
        Transform target = FindTargetWithinRadius();

        if (target != null && lockedOn == false)
        {
            Debug.Log("Target found: " + target.name);
            lockOnObject = target;
            lockedOn = true;
            anim.SetBool("LockedOn", lockedOn);
        }
        else
        {
            Debug.Log("no target :(");
            lockOnObject = null;
            lockedOn = false;
            anim.SetBool("LockedOn", lockedOn);

        }
    }

    private Transform FindTargetWithinRadius()
    {
        Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        Ray ray = Camera.main.ScreenPointToRay(screenCenter);
        RaycastHit[] hits = Physics.SphereCastAll(ray, lockOnRadius, lockOnDistance, targetLayer);

        Transform closestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (RaycastHit hit in hits)
        {
            float distance = Vector3.Distance(Camera.main.transform.position, hit.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = hit.transform;
            }
        }

        return closestTarget;
    }

}