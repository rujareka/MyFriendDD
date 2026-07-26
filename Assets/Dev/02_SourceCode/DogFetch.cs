using UnityEngine;

/// <summary>
/// 강아지 공 가져오기(Fetch) 로직. NavMesh/이벤트버스 없이 단순 transform 이동으로 동작.
///
/// 흐름: Idle(대기) -> 공이 착지하면 Chasing(쫓아감) -> 물면 Returning(플레이어에게 복귀)
///       -> 도착하면 내려놓고 다시 Idle.
///
/// 애니메이션은 직접 고르시면 됩니다. 이 스크립트는 아래 Animator 파라미터만 사용(있으면):
///   Bool    IsMoving  - 이동 중 여부 (뛰는/걷는 애니메이션 블렌드용)
///   Trigger Bite      - 공을 입에 무는 순간
///   Trigger Deliver   - 플레이어에게 전달하는 순간
/// Animator를 비워두면 애니메이션 없이 이동 로직만 동작합니다.
/// </summary>
public class DogFetch : MonoBehaviour
{
    private enum State { Idle, Chasing, Returning }

    [Header("참조")]
    [SerializeField] private FetchBall ball;
    [SerializeField] private Transform player;      // 비우면 Camera.main 사용
    [SerializeField] private Transform mouthAnchor; // 비우면 이 트랜스폼(루트)에 공을 부착
    [SerializeField] private Animator animator;      // 선택 사항

    [Header("이동")]
    [SerializeField] private float runSpeed = 3f;
    [SerializeField] private float turnSpeed = 5f;
    [SerializeField] private float grabDistance = 0.4f;
    [SerializeField] private float deliverDistance = 0.8f;

    private Transform target;
    private State state = State.Idle;

    /// <summary>
    /// 쫓아갈 공을 바꾼다 (예: Tools 메뉴에서 새 공을 스폰했을 때).
    /// 지금 다른 공을 물고 복귀 중(Returning)이면 그 공을 놓치는 버그를 막기 위해 무시한다.
    /// </summary>
    public void SetBall(FetchBall newBall)
    {
        if (state == State.Returning)
        {
            Debug.LogWarning("[DogFetch] 지금 다른 공을 물고 복귀 중이라 새 공으로 바꾸지 않았습니다.");
            return;
        }

        ball = newBall;
    }

    private void Awake()
    {
        target = player != null ? player : (Camera.main != null ? Camera.main.transform : null);

        // Animator 필드를 안 채워도 몸에 붙은 Animator를 자동으로 찾는다.
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        DisableRootMotion();
    }

    private void Update()
    {
        // 애니메이션을 나중에 추가/교체해도(Apply Root Motion이 다시 켜져도)
        // 이동은 항상 이 스크립트가 transform으로 직접 담당해야 하므로 매 프레임 강제로 꺼둔다.
        DisableRootMotion();
        switch (state)
        {
            case State.Idle:
                if (ball != null && ball.HasLanded)
                {
                    ball.ConsumeLanded();
                    state = State.Chasing;
                }
                break;

            case State.Chasing:
                MoveTowards(ball.transform.position);
                if (Vector3.Distance(transform.position, ball.transform.position) <= grabDistance)
                {
                    animator?.SetTrigger("Bite");
                    ball.PickUp(mouthAnchor != null ? mouthAnchor : transform);
                    state = State.Returning;
                }
                break;

            case State.Returning:
                if (target == null) { state = State.Idle; break; }
                Vector3 groundTarget = target.position;
                groundTarget.y = transform.position.y;
                MoveTowards(groundTarget);
                if (Vector3.Distance(transform.position, groundTarget) <= deliverDistance)
                {
                    animator?.SetTrigger("Deliver");
                    ball.Drop();
                    state = State.Idle;
                }
                break;
        }

        // 공 쫓아 뛸 때(Chasing)와 플레이어에게 복귀할 때(Returning)만 true.
        // Idle(대기 중)에는 항상 false이므로 애니메이터가 그동안엔 정지/대기 상태를 유지한다.
        if (animator != null)
            animator.SetBool("IsMoving", state == State.Chasing || state == State.Returning);
    }

    private void DisableRootMotion()
    {
        if (animator != null && animator.applyRootMotion)
            animator.applyRootMotion = false;
    }

    private void MoveTowards(Vector3 targetPosition)
    {
        Vector3 dir = targetPosition - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(dir), Time.deltaTime * turnSpeed);
        transform.position += dir.normalized * runSpeed * Time.deltaTime;
    }
}
