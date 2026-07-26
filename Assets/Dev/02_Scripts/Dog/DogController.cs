using UnityEngine;
using UnityEngine.AI;

namespace MyFriendDD.Dog
{
    /// <summary>
    /// NavMeshAgent 기반으로 개를 이동시키는 컨트롤러.
    /// CallToPlayer()를 호출하면 지정된 플레이어(또는 target) 위치로 달려온다.
    /// 애니메이션은 "Come" 애니메이터 컨트롤러의 IsMoving(bool) 파라미터로 제어한다.
    /// (Idle Breathing ↔ Run Loop, 조건부 전환. AC_Dogs_Type_01처럼 Exit Time으로
    /// 자동 순환하지 않으므로 스크립트가 시키는 대로만 움직인다.)
    /// 개 오브젝트의 Animator 컴포넌트에 Controller로 "Come"이 지정되어 있어야 한다.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class DogController : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("비워두면 Awake 시 Camera.main(플레이어)을 자동으로 사용합니다.")]
        [SerializeField] private Transform player;

        [Header("Movement")]
        [SerializeField] private float stoppingDistance = 0.8f;
        [SerializeField] private float runSpeed = 3.5f;
        [SerializeField] private float walkSpeed = 1.5f;
        [SerializeField] private bool runToPlayer = true;
        [SerializeField] private float angularSpeed = 360f;
        [SerializeField] private float acceleration = 8f;

        [Header("Animation (Come.controller)")]
        [Tooltip("Come 애니메이터 컨트롤러에 있는 bool 파라미터 이름")]
        [SerializeField] private string isMovingParam = "IsMoving";

        private enum DogState { Idle, Moving }

        private NavMeshAgent _agent;
        private Animator _animator;
        private DogState _state = DogState.Idle;
        private int _isMovingHash;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();
            _isMovingHash = Animator.StringToHash(isMovingParam);

            _agent.stoppingDistance = stoppingDistance;
            _agent.angularSpeed = angularSpeed;
            _agent.acceleration = acceleration;
            _agent.speed = runToPlayer ? runSpeed : walkSpeed;

            if (player == null && Camera.main != null)
            {
                player = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (_agent.pathPending) return;

            bool moving = _agent.remainingDistance > _agent.stoppingDistance
                          && _agent.velocity.sqrMagnitude > 0.01f;

            DogState next = moving ? DogState.Moving : DogState.Idle;
            if (next == _state) return;

            _state = next;
            _animator.SetBool(_isMovingHash, moving);
        }

        /// <summary>
        /// 음성 명령("이리와", "여기로와" 등)이 인식되면 이 메서드를 호출합니다.
        /// </summary>
        [ContextMenu("Test/Call To Player")]
        public void CallToPlayer()
        {
            if (player == null)
            {
                Debug.LogWarning($"[{nameof(DogController)}] player Transform이 지정되지 않았습니다.", this);
                return;
            }

            if (!_agent.isOnNavMesh)
            {
                Debug.LogWarning($"[{nameof(DogController)}] NavMeshAgent가 아직 NavMesh 위에 있지 않습니다. " +
                                  "(방 스캔/NavMesh 베이크가 끝났는지 확인하세요)", this);
                return;
            }

            _state = DogState.Moving; // Update()가 실제 속도를 보고 다시 확정한다
            _animator.SetBool(_isMovingHash, true);
            _agent.isStopped = false;
            _agent.speed = runToPlayer ? runSpeed : walkSpeed;
            _agent.SetDestination(player.position);
        }

        /// <summary>
        /// 대상을 직접 지정하며 호출하는 오버로드.
        /// </summary>
        public void CallToPlayer(Transform target)
        {
            player = target;
            CallToPlayer();
        }
    }
}
