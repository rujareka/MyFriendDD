using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

/// <summary>
/// 강아지 "이리 와" 로직. 키보드 위쪽 화살표를 누르면 대기 중인 강아지가
/// 플레이어에게 걸어온다. 공 가져오기/간식 먹기 중에는 반응하지 않는다.
///
/// 흐름: Idle(대기) -> 위쪽 화살표 입력 -> Coming(플레이어에게 이동) -> 도착하면 다시 Idle.
///
/// 애니메이션은 직접 고르시면 됩니다. 이 스크립트는 아래 Animator 파라미터만 사용(있으면):
///   Bool IsMoving - 이동 중 여부 (뛰는/걷는 애니메이션 블렌드용)
/// Animator를 비워두면 애니메이션 없이 이동 로직만 동작합니다.
///
/// DogFetch/DogEat과 같은 GameObject에 같이 붙을 수 있으므로, 서로 IsBusy를
/// 확인해서 동시에 NavMeshAgent 목적지를 다투지 않도록 한다.
///
/// 이동 속도/회전 속도/가속도는 이 오브젝트의 NavMeshAgent 컴포넌트 값을 그대로 따른다.
///
/// 프로젝트의 Active Input Handling이 New Input System 전용(activeInputHandler: 1)이라
/// 레거시 Input.GetKeyDown 대신 Keyboard.current를 사용한다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class DogCome : MonoBehaviour
{
    private enum State { Idle, Coming }

    [Header("참조")]
    [SerializeField] private Transform player; // 비우면 Camera.main 사용
    [SerializeField] private Animator animator; // 선택 사항
    [SerializeField] private DogFetch dogFetch; // 선택 사항 (공 가져오기와 동시 실행 방지용)
    [SerializeField] private DogEat dogEat;     // 선택 사항 (간식 먹기와 동시 실행 방지용)
    [SerializeField] private NavMeshAgent agent; // 비우면 이 오브젝트에서 자동으로 찾음

    [Header("거리 판정")]
    [SerializeField] private float arriveDistance = 1f;

    private Transform target;
    private State state = State.Idle;

    /// <summary>지금 플레이어에게 오는 중인가 - DogFetch/DogEat이 확인용으로 사용</summary>
    public bool IsBusy => state != State.Idle;

    private void Awake()
    {
        target = player != null ? player : (Camera.main != null ? Camera.main.transform : null);

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (dogFetch == null)
            dogFetch = GetComponent<DogFetch>();
        if (dogEat == null)
            dogEat = GetComponent<DogEat>();
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

        if (state == State.Idle)
        {
            bool fetchBusy = dogFetch != null && dogFetch.IsBusy;
            bool eatBusy = dogEat != null && dogEat.IsBusy;
            bool upArrowPressed = Keyboard.current != null && Keyboard.current.upArrowKey.wasPressedThisFrame;
            if (!fetchBusy && !eatBusy && target != null && upArrowPressed)
            {
                state = State.Coming;
                agent.isStopped = false;
            }
        }
        else // State.Coming
        {
            if (target == null)
            {
                state = State.Idle;
                agent.isStopped = true;
            }
            else
            {
                Vector3 groundTarget = target.position;
                groundTarget.y = transform.position.y;
                agent.SetDestination(groundTarget);

                if (Vector3.Distance(transform.position, groundTarget) <= arriveDistance)
                {
                    state = State.Idle;
                    agent.isStopped = true;
                }
            }
        }

        // 플레이어에게 걸어오는 동안(Coming)만 true. Idle(대기 중)에는 항상 false이므로
        // 애니메이터가 그동안엔 정지/대기 상태를 유지한다.
        if (animator != null)
            animator.SetBool("IsMoving", state == State.Coming);
    }

    private void DisableRootMotion()
    {
        if (animator != null && animator.applyRootMotion)
            animator.applyRootMotion = false;
    }
}
