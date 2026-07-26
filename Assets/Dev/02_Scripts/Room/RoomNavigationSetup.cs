using Meta.XR.MRUtilityKit;
using MyFriendDD.Dog;
using MyFriendDD.Voice;
using UnityEngine;
using UnityEngine.AI;

namespace MyFriendDD.Room
{
    /// <summary>
    /// AR(패스스루) 씬에서는 앱이 켜지는 순간 방 형태를 알 수 없기 때문에,
    /// MRUK가 방을 스캔하고 SceneNavigation이 NavMesh를 다 구울 때까지 기다렸다가
    /// 개의 이동(DogController)과 음성 인식(DogVoiceCommandHandler)을 활성화한다.
    ///
    /// 사용법:
    /// 1) 씬에 MRUK, SceneNavigation 컴포넌트를 배치한다.
    /// 2) 빈 GameObject에 이 스크립트를 추가하고 필드를 연결한다.
    /// 3) Dog 오브젝트의 DogController, VoiceManager 오브젝트의 DogVoiceCommandHandler
    ///    컴포넌트 체크박스는 미리 꺼둔다 (이 스크립트가 준비되면 켜준다).
    ///
    /// sceneNavigation을 비워두면(에디터에서 AR 없이 테스트할 때 등) NavMesh를
    /// 이미 다 구운 것으로 간주하고 Start()에서 바로 활성화한다.
    /// </summary>
    public class RoomNavigationSetup : MonoBehaviour
    {
        [Header("MRUK")]
        [Tooltip("씬에 배치한 MRUK SceneNavigation 컴포넌트. 비워두면 즉시 활성화된다(비-AR 테스트용).")]
        [SerializeField] private SceneNavigation sceneNavigation;

        [Header("Dog")]
        [SerializeField] private DogController dog;

        [Header("Voice (선택)")]
        [SerializeField] private DogVoiceCommandHandler voiceHandler;

        [Header("배치")]
        [Tooltip("NavMesh가 준비된 뒤 개가 NavMesh 위에 있지 않다면 가까운 NavMesh 위치로 보정한다.")]
        [SerializeField] private bool snapDogToNavMesh = true;
        [SerializeField] private float snapSearchRadius = 2f;

        private bool _ready;

        private void OnEnable()
        {
            if (sceneNavigation != null)
            {
                sceneNavigation.OnNavMeshInitialized.AddListener(HandleNavMeshReady);
            }
        }

        private void OnDisable()
        {
            if (sceneNavigation != null)
            {
                sceneNavigation.OnNavMeshInitialized.RemoveListener(HandleNavMeshReady);
            }
        }

        private void Start()
        {
            // AR 방 스캔 없이(에디터 등에서) 정적 NavMesh만 쓰는 경우를 위한 폴백.
            if (sceneNavigation == null)
            {
                HandleNavMeshReady();
            }
        }

        private void HandleNavMeshReady()
        {
            if (_ready) return;
            _ready = true;

            Debug.Log($"[{nameof(RoomNavigationSetup)}] Room NavMesh ready.");

            if (dog != null)
            {
                if (snapDogToNavMesh)
                {
                    var agent = dog.GetComponent<NavMeshAgent>();
                    if (agent != null && !agent.isOnNavMesh
                        && NavMesh.SamplePosition(dog.transform.position, out var hit, snapSearchRadius, NavMesh.AllAreas))
                    {
                        agent.Warp(hit.position);
                    }
                }

                dog.enabled = true;
            }

            if (voiceHandler != null)
            {
                voiceHandler.enabled = true;
            }
        }
    }
}
