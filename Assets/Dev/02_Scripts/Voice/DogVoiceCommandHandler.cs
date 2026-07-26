using System;
using System.Collections.Generic;
using Meta.WitAi;
using Meta.WitAi.Json;
using Oculus.Voice;
using UnityEngine;
using UnityEngine.Events;

namespace MyFriendDD.Voice
{
    /// <summary>
    /// Wit.ai에서 학습한 인텐트 이름과, 매칭됐을 때 실행할 동작(UnityEvent)을 한 쌍으로 묶은 항목.
    /// Inspector에서 항목을 추가/삭제하는 것만으로 새 음성 명령을 붙일 수 있다.
    /// (예: intentName = "call_dog" -> DogController.CallToPlayer(),
    ///      intentName = "sit_dog"  -> DogController.Sit())
    /// </summary>
    [Serializable]
    public class VoiceIntentCommand
    {
        [Tooltip("Wit.ai에서 학습한 인텐트 이름")]
        public string intentName;

        [Range(0f, 1f)]
        public float confidenceThreshold = 0.6f;

        [Tooltip("이 인텐트가 매칭되면(그리고 confidence가 threshold 이상이면) 실행할 동작")]
        public UnityEvent onMatched;
    }

    /// <summary>
    /// Wit.ai(Meta Voice SDK) 인식 결과를 받아서 등록된 인텐트가 매칭되면
    /// 해당 UnityEvent(예: DogController.CallToPlayer(), DogController.Sit())를 실행한다.
    ///
    /// 사전 준비:
    /// 1) Wit.ai(myfrienddd 앱)에 필요한 인텐트(call_dog, sit_dog 등)를 만들고
    ///    관련 발화("이리와", "앉아" 등)를 학습/트레이닝한다.
    /// 2) 씬의 GameObject에 AppVoiceExperience 컴포넌트를 추가하고
    ///    Wit Configuration에 Assets/Voice.asset을 연결한다.
    /// 3) 이 스크립트를 같은 GameObject에 추가하고 Commands 리스트에
    ///    인텐트 이름과 실행할 동작(DogController의 메서드)을 연결한다.
    /// </summary>
    [RequireComponent(typeof(AppVoiceExperience))]
    public class DogVoiceCommandHandler : MonoBehaviour
    {
        [Header("Commands")]
        [SerializeField] private List<VoiceIntentCommand> commands = new();

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

            if (string.IsNullOrEmpty(intentName)) return;

            foreach (var command in commands)
            {
                if (command.intentName == intentName && confidence >= command.confidenceThreshold)
                {
                    command.onMatched?.Invoke();
                    return; // 한 번의 응답에 하나의 명령만 실행
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
