using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 720f;

    [Header("Jump Settings")]
    public float jumpForce = 2.7f;
    public float groundCheckDistance = 0.4f;
    public LayerMask groundLayer;

    [Header("Control Lock")]
    public bool movementLocked = false;

    // 외부(Interaction 스크립트)에서 제어할 상태 변수들
    [HideInInspector] public bool isPushing = false;
    [HideInInspector] public Vector3 pushDirection;
    [HideInInspector] public float currentSpeedMultiplier = 1f;

    private Rigidbody rb;
    private Animator animator;
    private Vector3 inputVector;
    private bool isGrounded;
    private bool jumpRequested;

    public bool isAutoMoving = false;
    public float autoMoveStopDistance = 0.05f;

    private Vector3 autoMoveTarget;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        HandleInput();
        CheckGroundStatus();
    }

    void HandleInput()
    {
        // 잠금 중: 어떤 이동 입력도 처리하지 않음
        if (movementLocked)
        {
            inputVector = Vector3.zero;
            return;
        }

        if (isAutoMoving)
        {
            Vector3 toTarget = autoMoveTarget - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= autoMoveStopDistance)
            {
                // 도착 처리
                inputVector = Vector3.zero;
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                isAutoMoving = false;
                return;
            }

            inputVector = toTarget.normalized;
            return;
        }

        float horizontal = 0;
        float vertical = 0;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) vertical += 1;
            if (Keyboard.current.sKey.isPressed) vertical -= 1;
            if (Keyboard.current.aKey.isPressed) horizontal -= 1;
            if (Keyboard.current.dKey.isPressed) horizontal += 1;

            // 점프 입력 (밀기 중에는 점프 불가)
            if (!isPushing && Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
            {
                jumpRequested = true;
            }
        }

        if (isPushing)
        {
            // 밀기 상태: 고정된 방향의 전진 입력만 유효화
            Vector3 rawInput = new Vector3(horizontal, 0, vertical);
            float dot = Vector3.Dot(rawInput, pushDirection);

            // 전진(0.1f 이상)일 때만 이동 벡터 생성, 후진 및 옆 이동 무시
            float moveAmount = (dot > 0.1f) ? 1f : 0f;
            inputVector = pushDirection * moveAmount;
        }
        else
        {
            // 일반 상태: 자유로운 8방향 이동
            inputVector = new Vector3(horizontal, 0, vertical).normalized;
        }
    }

    void FixedUpdate()
    {
        Move();

        if (jumpRequested)
        {
            animator.SetTrigger("onJump");
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpRequested = false;
        }
    }

    void CheckGroundStatus()
    {
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance, groundLayer);
        animator.SetBool("isGrounded", isGrounded);
    }

    void Move()
    {
        float speed = moveSpeed * currentSpeedMultiplier;

        if (inputVector.magnitude >= 0.1f)
        {
            // 밀기 중이 아닐 때만 이동 방향으로 회전
            if (!isPushing)
            {
                Quaternion targetRotation = Quaternion.LookRotation(inputVector);
                rb.rotation = Quaternion.RotateTowards(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }

            Vector3 moveVelocity = inputVector * speed;
            rb.linearVelocity = new Vector3(moveVelocity.x, rb.linearVelocity.y, moveVelocity.z);

            if (!isPushing) animator.SetBool("isRunning", true);
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            if (!isPushing) animator.SetBool("isRunning", false);
        }
    }

    // 현재 입력 강도를 반환 (Interaction 스크립트에서 애니메이션 속도 조절용으로 사용)
    public float GetInputMagnitude()
    {
        return inputVector.magnitude;
    }

    public void StartAutoMove(Vector3 targetPos)
    {
        autoMoveTarget = targetPos;
        isAutoMoving = true;
    }

    public void StopAutoMove()
    {
        isAutoMoving = false;
        inputVector = Vector3.zero;
        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
    }

    public void LockMovement()
    {
        movementLocked = true;
        inputVector = Vector3.zero;
        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        animator.SetBool("isRunning", false);
    }

    public void UnlockMovement()
    {
        movementLocked = false;
    }
}