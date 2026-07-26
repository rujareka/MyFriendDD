using UnityEngine;
using UnityEngine.AI;

namespace MyFriendDD.Dog
{
    /// <summary>
    /// NavMeshAgent 기반으로 개를 이동시키는 컨트롤러.
    /// CallToPlayer()를 호출하면 지정된 플레이어(또는 target) 위치로 달려온다.
    /// AC_Dogs_Type_01 애니메이터 컨트롤러에는 파라미터가 없으므로
    /// 상태 이름을 직접 CrossFade 하는 방식으로 애니메이션을 전환한다.
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

        [Header("Animation State Names (AC_Dogs_Type_01)")]
        [Tooltip("Animator 상태 이름은 컨트롤러의 실제 State 이름과 정확히 일치해야 합니다.")]
        [SerializeField] private string idleStateName = "1 type_Idle Breathing";
        [SerializeField] private string runStateName = "1 type_Run Loop";
        [SerializeField] private float animationBlend = 0.15f;

        private NavMeshAgent _agent;
        private Animator _animator;
        private bool _isMoving;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();

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

            if (moving == _isMoving) return;

            _isMoving = moving;
            _animator.CrossFadeInFixedTime(moving ? runStateName : idleStateName, animationBlend);
        }

        /// <summary>
        /// 음성 명령("이리와", "여기로와" 등)이 인식되면 이 메서드를 호출합니다.
        /// </summary>
        public void CallToPlayer()
        {
            if (player == null)
            {
                Debug.LogWarning($"[{nameof(DogController)}] player Transform이 지정되지 않았습니다.", this);
                return;
            }

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
