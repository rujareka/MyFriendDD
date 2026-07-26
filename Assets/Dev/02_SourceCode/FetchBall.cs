using UnityEngine;

/// <summary>
/// 던지고 받는 공. Grabbable/Rigidbody는 그대로 두고, 이 스크립트는
/// "손에서 놓인 뒤 바닥에 착지했는지"만 판정해서 DogFetch에 알려준다.
///
/// 판정 방법: Rigidbody.isKinematic이 true(잡힌 상태) -> false(놓임)로 바뀌는 순간을
/// 감지해서 그때부터 속도를 지켜보다가, 일정 속도 이하가 일정 시간 유지되면 "착지"로 본다.
/// (Grabbable 내부 이벤트 타이밍에 의존하지 않아서 안정적임)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class FetchBall : MonoBehaviour
{
    [SerializeField] private float restSpeedThreshold = 0.15f; // 이 속도(m/s) 이하면 "멈춤" 후보
    [SerializeField] private float restTime = 0.3f;             // 이만큼 유지되면 착지로 확정

    private Rigidbody rb;
    private bool wasKinematicLastFrame;
    private bool monitoring;
    private bool landed;
    private float restTimer;
    private bool suppressNextRelease;

    /// <summary>손에서 놓인 뒤 착지(정지)했는가</summary>
    public bool HasLanded => landed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        wasKinematicLastFrame = rb.isKinematic;
    }

    private void Update()
    {
        bool isKinematicNow = rb.isKinematic;

        // 잡혀있던(kinematic) 상태에서 방금 놓인(dynamic) 순간 감지
        if (wasKinematicLastFrame && !isKinematicNow)
        {
            if (suppressNextRelease)
            {
                suppressNextRelease = false; // Drop()으로 우리가 직접 놓은 것 -> 재추적 시작 안 함
            }
            else
            {
                monitoring = true;
                landed = false;
                restTimer = 0f;
            }
        }
        wasKinematicLastFrame = isKinematicNow;

        if (monitoring && !landed)
        {
            if (rb.linearVelocity.magnitude < restSpeedThreshold)
            {
                restTimer += Time.deltaTime;
                if (restTimer >= restTime)
                    landed = true;
            }
            else
            {
                restTimer = 0f;
            }
        }
    }

    /// <summary>강아지가 이 공을 쫓기 시작할 때 호출 (같은 착지를 중복으로 다시 잡지 않도록)</summary>
    public void ConsumeLanded()
    {
        landed = false;
        monitoring = false;
    }

    /// <summary>
    /// 던져져서 착지한 게 아니라 이미 바닥에 놓인 채로 등장한 공(예: 메뉴에서 스폰)을
    /// 곧바로 "착지함" 상태로 표시한다. 이게 없으면 kinematic->dynamic 전환이 한 번도
    /// 일어나지 않아서 HasLanded가 영원히 false로 남는다.
    /// </summary>
    public void MarkAsLanded()
    {
        monitoring = false;
        landed = true;
    }

    /// <summary>강아지가 입으로 물었을 때</summary>
    public void PickUp(Transform mouth)
    {
        ConsumeLanded();
        rb.isKinematic = true;
        transform.SetParent(mouth);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    /// <summary>강아지가 플레이어 앞에 내려놓을 때</summary>
    public void Drop()
    {
        transform.SetParent(null);
        suppressNextRelease = true;
        rb.isKinematic = false;
    }
}
