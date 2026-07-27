using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 강아지 간식 먹기 로직. 이동은 NavMeshAgent에게 맡기고(길찾기/장애물 회피),
/// 이 스크립트는 상태 전이와 목적지 지정만 담당한다.
///
/// 흐름: Idle(대기) -> 간식을 손으로 집으면 Chasing(쫓아감) -> 도착하면 먹고 다시 Idle.
/// 쫓아가는 동안에는 간식의 실시간 위치(손에 들려 움직이는 위치)를 계속 따라간다.
///
/// 애니메이션은 직접 고르시면 됩니다. 이 스크립트는 아래 Animator 파라미터만 사용(있으면):
///   Bool    IsMoving - 이동 중 여부 (뛰는/걷는 애니메이션 블렌드용)
///   Trigger Eat      - 간식을 먹는 순간
/// Animator를 비워두면 애니메이션 없이 이동 로직만 동작합니다.
///
/// DogFetch(공 가져오기)와 같은 GameObject에 같이 붙을 수 있으므로, 서로 IsBusy를
/// 확인해서 동시에 NavMeshAgent 목적지를 다투지 않도록 한다.
///
/// 이동 속도/회전 속도/가속도는 이 오브젝트의 NavMeshAgent 컴포넌트 값을 그대로 따른다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class DogEat : MonoBehaviour
{
    private enum State { Idle, Chasing }

    [Header("참조")]
    [SerializeField] private SnackItem snack;
    [SerializeField] private Animator animator; // 선택 사항
    [SerializeField] private DogFetch dogFetch;  // 선택 사항 (공 가져오기와 동시 실행 방지용)
    [SerializeField] private DogCome dogCome;    // 선택 사항 ("이리 와"와 동시 실행 방지용)
    [SerializeField] private NavMeshAgent agent; // 비우면 이 오브젝트에서 자동으로 찾음
    [SerializeField] private Transform mouthAnchor; // 비우면 DogFetch의 mouthAnchor를 재사용, 그것도 없으면 이 트랜스폼(루트)

    [Header("거리 판정")]
    [SerializeField] private float eatDistance = 0.4f;

    private State state = State.Idle;

    /// <summary>지금 다른 볼일(간식 먹으러 이동 중)로 바쁜가 - DogFetch가 확인용으로 사용</summary>
    public bool IsBusy => state != State.Idle;

    /// <summary>쫓아갈 간식을 바꾼다 (예: Tools 메뉴에서 새 간식을 스폰했을 때).</summary>
    public void SetSnack(SnackItem newSnack)
    {
        snack = newSnack;
    }

    private void Awake()
    {
        // Animator/DogFetch 필드를 안 채워도 몸에 붙은 컴포넌트를 자동으로 찾는다.
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (dogFetch == null)
            dogFetch = GetComponent<DogFetch>();
        if (dogCome == null)
            dogCome = GetComponent<DogCome>();
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (mouthAnchor == null)
            mouthAnchor = dogFetch != null ? dogFetch.MouthAnchor : null;

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
                bool fetchBusy = dogFetch != null && dogFetch.IsBusy;
                bool comeBusy = dogCome != null && dogCome.IsBusy;
                if (!fetchBusy && !comeBusy && snack != null && snack.IsHeld)
                {
                    snack.ConsumeHeld();
                    state = State.Chasing;
                    agent.isStopped = false;
                }
                break;

            case State.Chasing:
                if (snack == null) { state = State.Idle; agent.isStopped = true; break; }

                // 간식은 플레이어 손 높이(공중)에 들려 있을 수 있어서, NavMeshAgent가
                // 그 높이 그대로 목적지로 받으면 NavMesh 위 가장 가까운 지점으로 엉뚱하게
                // 스냅될 수 있다. 강아지 발 높이로 눌러서 수평 위치만 목적지로 넘긴다.
                Vector3 flatTarget = snack.transform.position;
                flatTarget.y = transform.position.y;
                agent.SetDestination(flatTarget);

                // 도착 판정은 몸통 중심이 아니라 입(mouthAnchor) 기준으로 본다.
                // 그래야 몸통이 간식 근처에 오자마자가 아니라 입이 실제로 닿을 만큼
                // 가까워졌을 때 먹는다. 높이차는 위와 같은 이유로 무시(수평 거리만).
                Vector3 mouthPos = mouthAnchor != null ? mouthAnchor.position : transform.position;
                Vector3 flatDelta = snack.transform.position - mouthPos;
                flatDelta.y = 0f;
                if (flatDelta.magnitude <= eatDistance)
                {
                    animator?.SetTrigger("Eat");
                    snack.Eat();
                    snack = null;
                    state = State.Idle;
                    agent.isStopped = true;
                }
                break;
        }

        // 간식 쫓아 뛸 때(Chasing)만 true. Idle(대기 중)에는 항상 false이므로
        // 애니메이터가 그동안엔 정지/대기 상태를 유지한다.
        if (animator != null)
            animator.SetBool("IsMoving", state == State.Chasing);
    }

    private void DisableRootMotion()
    {
        if (animator != null && animator.applyRootMotion)
            animator.applyRootMotion = false;
    }
}
