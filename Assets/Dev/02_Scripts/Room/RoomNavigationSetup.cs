using MyFriendDD.Dog;
using MyFriendDD.Voice;
using UnityEngine;
using UnityEngine.AI;

namespace MyFriendDD.Room
{
    /// <summary>
    /// 씬 로드 시 정적으로 구워둔 NavMesh 위에 개를 배치하고,
    /// 개(DogController)와 음성 인식(DogVoiceCommandHandler)을 활성화한다.
    ///
    /// 사용법:
    /// 1) 빈 GameObject에 이 스크립트를 추가하고 Dog/Voice 필드를 연결한다.
    /// 2) Dog 오브젝트의 DogController, VoiceManager 오브젝트의 DogVoiceCommandHandler
    ///    컴포넌트 체크박스는 미리 꺼둔다 (이 스크립트가 Start()에서 켜준다).
    /// </summary>
    public class RoomNavigationSetup : MonoBehaviour
    {
        [Header("Dog")]
        [SerializeField] private DogController dog;

        [Header("Voice (선택)")]
        [SerializeField] private DogVoiceCommandHandler voiceHandler;

        [Header("배치")]
        [Tooltip("개가 NavMesh 위에 있지 않다면 가까운 NavMesh 위치로 보정한다.")]
        [SerializeField] private bool snapDogToNavMesh = true;
        [SerializeField] private float snapSearchRadius = 2f;

        private void Start()
        {
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
