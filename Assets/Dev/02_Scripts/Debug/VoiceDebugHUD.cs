using Meta.WitAi;
using Meta.WitAi.Json;
using MyFriendDD.Dog;
using MyFriendDD.Room;
using Oculus.Voice;
using UnityEngine;

namespace MyFriendDD.Debugging
{
    /// <summary>
    /// 헤드셋 안에서는 Unity 콘솔을 볼 수 없기 때문에, 음성인식/개 상태를
    /// 눈으로 바로 확인할 수 있도록 3D 텍스트로 띄워주는 디버그용 HUD.
    /// 확인 끝나면 이 오브젝트를 비활성화하거나 지우면 됩니다.
    /// </summary>
    [RequireComponent(typeof(TextMesh))]
    public class VoiceDebugHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AppVoiceExperience voiceService;
        [SerializeField] private DogController dog;
        [Tooltip("AR 방 스캔 게이팅을 쓰는 경우에만 연결 (안 쓰면 비워둬도 됨)")]
        [SerializeField] private RoomNavigationSetup roomSetup;

        [Header("Options")]
        [SerializeField] private bool faceCamera = true;

        private TextMesh _text;
        private string _micState = "-";
        private string _lastHeard = "-";
        private string _lastIntent = "-";

        private void Awake()
        {
            _text = GetComponent<TextMesh>();
            _text.characterSize = 0.1f;
            _text.fontSize = 48;
            _text.anchor = TextAnchor.MiddleCenter;
        }

        private void OnEnable()
        {
            if (voiceService == null)
            {
                Debug.LogWarning($"[{nameof(VoiceDebugHUD)}] voiceService가 연결되지 않았습니다.", this);
                Refresh();
                return;
            }

            voiceService.VoiceEvents.OnStartListening.AddListener(HandleStartListening);
            voiceService.VoiceEvents.OnStoppedListening.AddListener(HandleStoppedListening);
            voiceService.VoiceEvents.OnFullTranscription.AddListener(HandleTranscription);
            voiceService.VoiceEvents.OnResponse.AddListener(HandleResponse);
            voiceService.VoiceEvents.OnError.AddListener(HandleError);
            Refresh();
        }

        private void OnDisable()
        {
            if (voiceService == null) return;

            voiceService.VoiceEvents.OnStartListening.RemoveListener(HandleStartListening);
            voiceService.VoiceEvents.OnStoppedListening.RemoveListener(HandleStoppedListening);
            voiceService.VoiceEvents.OnFullTranscription.RemoveListener(HandleTranscription);
            voiceService.VoiceEvents.OnResponse.RemoveListener(HandleResponse);
            voiceService.VoiceEvents.OnError.RemoveListener(HandleError);
        }

        private void Update()
        {
            if (faceCamera && Camera.main != null)
            {
                transform.forward = transform.position - Camera.main.transform.position;
            }
        }

        private void HandleStartListening()
        {
            _micState = "듣는 중...";
            Refresh();
        }

        private void HandleStoppedListening()
        {
            _micState = "대기 중";
            Refresh();
        }

        private void HandleTranscription(string transcription)
        {
            _lastHeard = transcription;
            Refresh();
        }

        private void HandleResponse(WitResponseNode response)
        {
            string intent = response.GetIntentName();
            float confidence = response.GetFirstIntent()?["confidence"]?.AsFloat ?? 0f;
            _lastIntent = string.IsNullOrEmpty(intent) ? "(intent 없음)" : $"{intent} ({confidence:F2})";
            Refresh();
        }

        private void HandleError(string error, string message)
        {
            _lastIntent = $"ERROR: {error} / {message}";
            Refresh();
        }

        private void Refresh()
        {
            bool micReady = voiceService != null;
            bool dogActive = dog != null && dog.enabled;
            string navState = roomSetup == null ? "미사용" : (roomSetup.IsReady ? "준비됨" : "대기 중");

            _text.text =
                $"Voice Service: {(micReady ? "연결됨" : "연결 안됨!")}\n" +
                $"Mic: {_micState}\n" +
                $"인식된 말: {_lastHeard}\n" +
                $"Intent: {_lastIntent}\n" +
                $"Room NavMesh: {navState}\n" +
                $"Dog Active: {(dogActive ? "YES" : "no (비활성)")}";
        }
    }
}
