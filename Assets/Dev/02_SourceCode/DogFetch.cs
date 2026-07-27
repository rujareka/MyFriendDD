using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 강아지 공 가져오기(Fetch) 로직. 이동은 NavMeshAgent에게 맡기고(길찾기/장애물 회피),
/// 이 스크립트는 상태 전이와 목적지 지정만 담당한다.
///
/// 흐름: Idle(대기) -> 공이 착지하면 Chasing(쫓아감) -> 물면 Returning(플레이어에게 복귀)
///       -> 도착하면 내려놓고 다시 Idle.
///
/// 애니메이션은 직접 고르시면 됩니다. 이 스크립트는 아래 Animator 파라미터만 사용(있으면):
///   Bool    IsMoving  - 이동 중 여부 (뛰는/걷는 애니메이션 블렌드용)
///   Trigger Bite      - 공을 입에 무는 순간
///   Trigger Deliver   - 플레이어에게 전달하는 순간
/// Animator를 비워두면 애니메이션 없이 이동 로직만 동작합니다.
///
/// 이동 속도/회전 속도/가속도는 이 오브젝트의 NavMeshAgent 컴포넌트 값을 그대로 따른다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class DogFetch : MonoBehaviour
{
    private enum State { Idle, Chasing, Returning }

    [Header("참조")]
    [SerializeField] private FetchBall ball;
    [SerializeField] private Transform player;      // 비우면 Camera.main 사용
    [SerializeField] private Transform mouthAnchor; // 비우면 이 트랜스폼(루트)에 공을 부착
    [SerializeField] private Animator animator;      // 선택 사항
    [SerializeField] private DogEat dogEat;          // 선택 사항 (간식 먹기와 동시 실행 방지용)
    [SerializeField] private DogCome dogCome;        // 선택 사항 ("이리 와"와 동시 실행 방지용)
    [SerializeField] private NavMeshAgent agent;     // 비우면 이 오브젝트에서 자동으로 찾음

    [Header("거리 판정")]
    [SerializeField] private float grabDistance = 0.4f;
    [SerializeField] private float deliverDistance = 0.8f;

    private Transform target;
    private State state = State.Idle;

    /// <summary>지금 다른 볼일(공 쫓기/복귀)로 바쁜가 - DogEat이 확인용으로 사용</summary>
    public bool IsBusy => state != State.Idle;

    /// <summary>입 위치(비어 있으면 null) - DogEat이 같은 지점을 재사용하는 용도</summary>
    public Transform MouthAnchor => mouthAnchor;

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
        if (dogEat == null)
            dogEat = GetComponent<DogEat>();
        if (dogCome == null)
            dogCome = GetComponent<DogCome>();
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        // Idle 상태에서는 목적지가 없으니 제자리에 세워둔다.
        agent.isStopped = true;

        DisableRootMotion();
    }

    private void Update()
    {
        // 애니메이션을 나중에 추가/교체해도(Apply Root Motion이 다시 켜져도)
        // 이동은 항상 NavMeshAgent가 담당해야 하므로 매 프레임 강제로 꺼둔다.
        DisableRootMotion();
        switch (state)
        {
            case State.Idle:
                bool eatBusy = dogEat != null && dogEat.IsBusy;
                bool comeBusy = dogCome != null && dogCome.IsBusy;
                if (!eatBusy && !comeBusy && ball != null && ball.HasLanded)
                {
                    ball.ConsumeLanded();
                    state = State.Chasing;
                    agent.isStopped = false;
                }
                break;

            case State.Chasing:
                agent.SetDestination(ball.transform.position);
                if (Vector3.Distance(transform.position, ball.transform.position) <= grabDistance)
                {
                    animator?.SetTrigger("Bite");
                    ball.PickUp(mouthAnchor != null ? mouthAnchor : transform);
                    state = State.Returning;
                }
                break;

            case State.Returning:
                if (target == null) { state = State.Idle; agent.isStopped = true; break; }
                Vector3 groundTarget = target.position;
                groundTarget.y = transform.position.y;
                agent.SetDestination(groundTarget);
                if (Vector3.Distance(transform.position, groundTarget) <= deliverDistance)
                {
                    animator?.SetTrigger("Deliver");
                    ball.Drop();
                    state = State.Idle;
                    agent.isStopped = true;
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
}
