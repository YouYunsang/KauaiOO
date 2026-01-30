using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerPushInteraction : MonoBehaviour
{
    [Header("Push Settings")]
    public float interactDistance = 1.2f;    // 박스 감지 거리
    public float pushOffset = 0.8f;          // 박스와 캐릭터 사이 고정 거리
    public LayerMask interactLayer;          // 박스 레이어
    public float pushSpeedMultiplier = 0.5f; // 밀기 시 속도 배율

    [Header("Interaction Cast (Patched)")]
    [Tooltip("레이 대신 SphereCast를 사용해 높이/오프셋 변화에도 안정적으로 박스를 감지합니다.")]
    public float interactRadius = 0.20f;     // SphereCast 반경(감지 두께)
    [Tooltip("Capsule bounds의 어떤 높이에서 캐스트를 시작할지(0=바닥, 1=머리).")]
    [Range(0f, 1f)]
    public float interactHeight01 = 0.45f;   // 0.35~0.55 추천
    public bool debugDraw = false;

    private PlayerMovement movement;
    private Animator animator;

    private Rigidbody playerRb;
    private CapsuleCollider capsule;

    private bool isPushing = false;
    private Vector3 pushDirection;
    private Transform currentBox;
    private Rigidbody currentBoxRb;

    private bool isAligning = false;
    private Vector3 alignTargetPos;

    private bool isPushTransition = false; // PushStart/Stop 재생 중인지

    void Start()
    {
        movement = GetComponent<PlayerMovement>();
        animator = GetComponent<Animator>();

        playerRb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        if (capsule == null) capsule = GetComponentInChildren<CapsuleCollider>();
    }

    void Update()
    {
        // E 버튼 상호작용 체크
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TogglePush();
        }

        // 정렬 중이면: 도착 시 밀기 시작
        if (isAligning)
        {
            // PlayerMovement가 auto move를 끝냈으면 도착한 것
            if (!movement.isAutoMoving)
            {
                isAligning = false;
                BeginPushingAfterAlign();
            }
            return;
        }

        // 밀기 애니메이션 속도 제어
        if (isPushing)
        {
            float animSpeed = movement.GetInputMagnitude() > 0.1f ? 1f : 0f;
            animator.SetFloat("pushAnimSpeed", animSpeed);
        }
    }

    void TogglePush()
    {
        if (isAligning) return;
        if (isPushTransition) return;

        if (isPushing)
        {
            StopPushing_Transition();
        }
        else
        {
            TryStartPushing();
        }
    }

    /// <summary>
    /// PATCHED:
    /// - transform.position 기준 Raycast -> CapsuleCollider/Rigidbody 기준 SphereCast
    /// - 애니메이션 Root Transform(Y) 옵션(Original/Offset 등) 변화에도 감지 지점이 흔들리지 않게 함
    /// </summary>
    void TryStartPushing()
    {
        Vector3 origin = GetInteractionOrigin();
        Vector3 dir = transform.forward;

        if (debugDraw)
        {
            Debug.DrawRay(origin, dir * interactDistance, Color.red, 0.2f);
        }

        RaycastHit hit;
        bool didHit = Physics.SphereCast(
            origin,
            interactRadius,
            dir,
            out hit,
            interactDistance,
            interactLayer,
            QueryTriggerInteraction.Ignore // 박스는 일반 콜라이더라고 했으니 Ignore가 안전
        );

        if (!didHit) return;

        currentBox = hit.transform;
        currentBoxRb = currentBox.GetComponent<Rigidbody>();

        // 박스는 이 시점에 절대 움직이면 안 됨 → kinematic 유지
        if (currentBoxRb != null)
            currentBoxRb.isKinematic = true;

        // 밀기 방향 확정(그리드 정렬)
        pushDirection = DeterminePushDirection(transform.forward);

        // 플레이어가 서야 하는 위치(박스는 고정, 플레이어만 이동)
        Vector3 targetPos = currentBox.position - (pushDirection * pushOffset);

        // PATCHED: Y를 transform.position.y가 아니라 "물리 위치" 기준으로 고정
        float stableY = GetStablePlayerY();
        alignTargetPos = new Vector3(targetPos.x, stableY, targetPos.z);

        // 오토워크 시작
        isAligning = true;
        movement.StartAutoMove(alignTargetPos);

        // (선택) 정렬 중 달리기 애니
        animator.SetBool("isRunning", true);
    }

    void BeginPushingAfterAlign()
    {
        if (currentBox == null) return;

        // 방향 고정
        transform.rotation = Quaternion.LookRotation(pushDirection);

        // 밀기 상태 진입
        isPushing = true;

        if (currentBoxRb != null)
        {
            currentBoxRb.isKinematic = false; // 밀기 중 물리 ON
        }

        movement.isPushing = true;
        movement.pushDirection = pushDirection;
        movement.currentSpeedMultiplier = pushSpeedMultiplier;

        isPushTransition = true;
        movement.LockMovement();   // PushStart 동안 이동 금지

        animator.SetBool("isPushing", true);
        animator.SetTrigger("onPushStart");
        animator.SetBool("isRunning", false);
        animator.SetFloat("pushAnimSpeed", 0f);
    }

    public void OnPushStartFinished()
    {
        isPushTransition = false;
        movement.UnlockMovement();  // PushLoop 중 다시 조작 가능
    }

    void StopPushing_Transition()
    {
        isPushTransition = true;
        movement.LockMovement(); // PushStop 동안 이동 금지

        // 박스 즉시 고정
        if (currentBoxRb != null) currentBoxRb.isKinematic = true;

        // 밀기 입력 상태 해제
        isPushing = false;
        movement.isPushing = false;
        movement.currentSpeedMultiplier = 1f;

        animator.SetTrigger("onPushStop");
        animator.SetBool("isPushing", false);
    }

    public void OnPushStopFinished()
    {
        currentBox = null;
        currentBoxRb = null;

        isPushTransition = false;
        movement.UnlockMovement();
    }

    void FixedUpdate()
    {
        // 밀기 중 박스 위치 강제 동기화(간격 유지)
        if (isPushing && currentBox != null && currentBoxRb != null)
        {
            Vector3 playerPos = GetStablePlayerPosition();
            Vector3 targetBoxPos = playerPos + (pushDirection * pushOffset);

            // PATCHED: 플레이어 기준은 물리 위치, 박스 Y는 기존 유지
            currentBoxRb.MovePosition(new Vector3(targetBoxPos.x, currentBox.position.y, targetBoxPos.z));
        }
    }

    Vector3 DeterminePushDirection(Vector3 forward)
    {
        if (Mathf.Abs(forward.x) > Mathf.Abs(forward.z))
            return new Vector3(Mathf.Sign(forward.x), 0, 0); // 동/서
        else
            return new Vector3(0, 0, Mathf.Sign(forward.z)); // 남/북
    }

    // =========================
    // PATCHED Helpers
    // =========================

    /// <summary>
    /// Interaction 캐스트 시작점을 transform.position이 아니라 Capsule bounds 기반으로 계산.
    /// 애니메이션 Root Transform Y 옵션(Original/Offset) 변경의 영향을 최소화.
    /// </summary>
    private Vector3 GetInteractionOrigin()
    {
        // CapsuleCollider가 있으면 그 bounds를 기준으로 안정적인 origin 생성
        if (capsule != null)
        {
            Bounds b = capsule.bounds;
            float y = Mathf.Lerp(b.min.y, b.max.y, interactHeight01);
            return new Vector3(b.center.x, y, b.center.z);
        }

        // fallback (권장 X)
        return transform.position + Vector3.up * 0.5f;
    }

    /// <summary>
    /// 정렬/박스 동기화에서 사용할 플레이어 기준 위치.
    /// Rigidbody 기반이면 rb.position이 가장 안정적.
    /// </summary>
    private Vector3 GetStablePlayerPosition()
    {
        if (playerRb != null) return playerRb.position;
        return transform.position;
    }

    /// <summary>
    /// 오토무브/정렬 시 Y 고정 기준. transform.position.y 대신 물리 위치 y를 사용.
    /// </summary>
    private float GetStablePlayerY()
    {
        if (playerRb != null) return playerRb.position.y;
        return transform.position.y;
    }
}
