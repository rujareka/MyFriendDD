using Meta.WitAi;
using Meta.WitAi.Json;
using Oculus.Voice;
using UnityEngine;
using MyFriendDD.Dog;

namespace MyFriendDD.Voice
{
    /// <summary>
    /// Wit.ai(Meta Voice SDK) 인식 결과를 받아서 "이리와" 같은 호출 인텐트가
    /// 매칭되면 DogController.CallToPlayer()를 실행한다.
    ///
    /// 사전 준비:
    /// 1) Wit.ai(myfrienddd 앱)에 "call_dog" 인텐트를 만들고
    ///    "이리와", "여기로 와", "이리로 와줘", "이쪽으로 와" 등의 발화를 학습/트레이닝한다.
    /// 2) 씬의 GameObject에 AppVoiceExperience 컴포넌트를 추가하고
    ///    Wit Configuration에 Assets/Voice.asset을 연결한다.
    /// 3) 이 스크립트를 같은 GameObject에 추가하고 dog 필드에 개 오브젝트를 연결한다.
    /// </summary>
    [RequireComponent(typeof(AppVoiceExperience))]
    public class DogVoiceCommandHandler : MonoBehaviour
    {
        [Header("Wit.ai Intent")]
        [Tooltip("Wit.ai에서 학습한 인텐트 이름 (예: call_dog)")]
        [SerializeField] private string callIntentName = "call_dog";
        [Range(0f, 1f)]
        [SerializeField] private float confidenceThreshold = 0.6f;

        [Header("Target")]
        [SerializeField] private DogController dog;

        [Header("Listening")]
        [Tooltip("응답을 받은 뒤 자동으로 마이크를 다시 활성화해 계속 듣습니다.")]
        [SerializeField] private bool keepListening = true;

        private AppVoiceExperience _voiceService;

        private void Awake()
        {
            _voiceService = GetComponent<AppVoiceExperience>();
        }

        private void OnEnable()
        {
            _voiceService.VoiceEvents.OnResponse.AddListener(HandleResponse);
            _voiceService.VoiceEvents.OnStoppedListening.AddListener(HandleStoppedListening);
            _voiceService.VoiceEvents.OnError.AddListener(HandleError);
        }

        private void OnDisable()
        {
            _voiceService.VoiceEvents.OnResponse.RemoveListener(HandleResponse);
            _voiceService.VoiceEvents.OnStoppedListening.RemoveListener(HandleStoppedListening);
            _voiceService.VoiceEvents.OnError.RemoveListener(HandleError);
        }

        private void Start()
        {
            // 시작하자마자 계속 듣기 시작 (필요하면 버튼/트리거로 대체 가능)
            _voiceService.Activate();
        }

        private void HandleResponse(WitResponseNode response)
        {
            string intentName = response.GetIntentName();
            float confidence = response.GetFirstIntent()?["confidence"]?.AsFloat ?? 0f;

            Debug.Log($"[{nameof(DogVoiceCommandHandler)}] intent={intentName}, confidence={confidence:F2}");

            if (!string.IsNullOrEmpty(intentName)
                && intentName == callIntentName
                && confidence >= confidenceThreshold)
            {
                if (dog != null)
                {
                    dog.CallToPlayer();
                }
                else
                {
                    Debug.LogWarning($"[{nameof(DogVoiceCommandHandler)}] dog 참조가 비어 있습니다.", this);
                }
            }
        }

        private void HandleStoppedListening()
        {
            if (keepListening && !_voiceService.Active)
            {
                _voiceService.Activate();
            }
        }

        private void HandleError(string error, string message)
        {
            Debug.LogWarning($"[{nameof(DogVoiceCommandHandler)}] Wit error: {error} / {message}");
        }
    }
}
